using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.SeasonalPicks.Configuration;
using Jellyfin.Plugin.SeasonalPicks.Services;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.SeasonalPicks.Api;

[ApiController]
[Route("SeasonalPicks")]
public class SeasonalPicksController : ControllerBase
{
    // Jellyfin.Api's InternalClaimTypes.UserId (that assembly isn't referenced by plugins).
    private const string UserIdClaim = "Jellyfin-UserId";

    private readonly PicksService _picks;
    private readonly IUserManager _users;
    private readonly ILibraryManager _library;
    private readonly CoverService _covers;

    public SeasonalPicksController(PicksService picks, IUserManager users, ILibraryManager library, CoverService covers)
    {
        _picks = picks;
        _users = users;
        _library = library;
        _covers = covers;
    }

    /// <summary>The calling user's current picks. Generates them on first use.</summary>
    [HttpGet("Picks")]
    [Authorize]
    public async Task<ActionResult<PicksResponse>> GetPicks(CancellationToken ct)
    {
        var user = CurrentUser();
        if (user is null)
        {
            return Unauthorized();
        }

        var state = _picks.GetState(user.Id);
        if (state.GeneratedAt is null || state.Picks.Count == 0)
        {
            state = await _picks.RefreshUserAsync(user, WritesPlaylist(user), ct).ConfigureAwait(false);
        }

        return ToResponse(state);
    }

    /// <summary>Reroll the calling user's picks now.</summary>
    [HttpPost("Refresh")]
    [Authorize]
    public async Task<ActionResult<PicksResponse>> Refresh(CancellationToken ct)
    {
        var user = CurrentUser();
        if (user is null)
        {
            return Unauthorized();
        }

        return ToResponse(await _picks.RefreshUserAsync(user, WritesPlaylist(user), ct).ConfigureAwait(false));
    }

    /// <summary>Swap one pick out: "not now" (30 days) or forever.</summary>
    [HttpPost("Picks/{itemId}/Swap")]
    [Authorize]
    public async Task<ActionResult<PicksResponse>> Swap([FromRoute] Guid itemId, [FromQuery] bool forever, CancellationToken ct)
    {
        var user = CurrentUser();
        if (user is null)
        {
            return Unauthorized();
        }

        return ToResponse(await _picks.SwapAsync(user, itemId, forever, ct).ConfigureAwait(false));
    }

    /// <summary>Bring back everything the calling user hid or snoozed.</summary>
    [HttpPost("Unhide")]
    [Authorize]
    public async Task<ActionResult> Unhide(CancellationToken ct)
    {
        var user = CurrentUser();
        if (user is null)
        {
            return Unauthorized();
        }

        await _picks.UnhideAllAsync(user, ct).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>Admin: refresh every enabled user and the mirror folder now.</summary>
    [HttpPost("RefreshAll")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public async Task<ActionResult> RefreshAll(CancellationToken ct)
    {
        await _picks.RefreshAllAsync(new Progress<double>(), ct).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Admin: dry run for one user. The body may hold unsaved settings from the config page (same shape as the
    /// plugin configuration); empty = the saved settings. Nothing is written.
    /// </summary>
    [HttpPost("Preview")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public async Task<ActionResult<PreviewResponse>> Preview([FromQuery] Guid userId)
    {
        var user = _users.GetUserById(userId);
        if (user is null)
        {
            return NotFound();
        }

        PluginConfiguration? cfg = null;
        using (var reader = new StreamReader(Request.Body))
        {
            var body = await reader.ReadToEndAsync().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    cfg = JsonSerializer.Deserialize<PluginConfiguration>(body, PreviewJson);
                }
                catch (JsonException ex)
                {
                    return BadRequest("Settings could not be read: " + ex.Message);
                }
            }
        }

        cfg ??= Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var result = _picks.Preview(user, cfg);
        return new PreviewResponse
        {
            User = user.Username,
            ActiveSeasons = result.ActiveSeasons.ToArray(),
            UnwatchedCandidates = result.UnwatchedCandidates,
            RediscoverCandidates = result.RediscoverCandidates,
            SeasonalCandidates = result.SeasonalCandidates,
            Picks = ToDtos(result.Picks),
            SeasonPlaylists = result.SeasonPicks
                .Select(kv => new SeasonPreviewDto { Name = PicksService.SeasonPlaylistName(cfg, kv.Key), Picks = ToDtos(kv.Value) })
                .ToArray()
        };
    }

    private static readonly JsonSerializerOptions PreviewJson = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    /// <summary>Admin: upload a cover for a season key (or "default"), as base64 JSON like Jellyfin's own image upload.</summary>
    [HttpPost("Covers/{key}")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult UploadCover([FromRoute] string key, [FromBody] CoverUpload upload)
    {
        if (!CoverService.IsValidKey(key) || DecodeUpload(upload) is not { } bytes)
        {
            return BadRequest();
        }

        _covers.SaveUpload(key, bytes, upload.MimeType ?? "image/jpeg");
        return NoContent();
    }

    /// <summary>Admin: upload a picture to go behind a season's generated cover (decorations and title are drawn over it).</summary>
    [HttpPost("Covers/{key}/Background")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult UploadBackground([FromRoute] string key, [FromBody] CoverUpload upload)
    {
        if (!CoverService.IsValidKey(key) || DecodeUpload(upload) is not { } bytes)
        {
            return BadRequest();
        }

        _covers.SaveBackground(key, bytes, upload.MimeType ?? "image/jpeg");
        return NoContent();
    }

    [HttpDelete("Covers/{key}/Background")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult DeleteBackground([FromRoute] string key)
    {
        if (!CoverService.IsValidKey(key))
        {
            return BadRequest();
        }

        _covers.DeleteBackground(key);
        return NoContent();
    }

    /// <summary>Admin: a season was renamed, so move its uploaded cover and background picture to the new name.</summary>
    [HttpPost("Covers/{key}/Rename")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult RenameCover([FromRoute] string key, [FromQuery] string to)
    {
        if (!CoverService.IsValidKey(key) || string.IsNullOrEmpty(to) || !CoverService.IsValidKey(to) || key == CoverService.DefaultKey || to == CoverService.DefaultKey)
        {
            return BadRequest();
        }

        if (key != to)
        {
            _covers.RenameKey(key, to);
        }

        return NoContent();
    }

    /// <summary>
    /// Admin: draw a cover design as it stands on the settings page (saved or not), using the admin's own picks as posters.
    /// With useUpload, a finished uploaded cover is returned instead when there is one. Header X-SP-Source says which.
    /// </summary>
    [HttpPost("Covers/Render")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public async Task<ActionResult> RenderCover([FromQuery] bool wide, [FromQuery] bool useUpload)
    {
        var user = CurrentUser();
        if (user is null)
        {
            return Unauthorized();
        }

        CoverRenderRequest? req;
        using (var reader = new StreamReader(Request.Body))
        {
            try
            {
                req = JsonSerializer.Deserialize<CoverRenderRequest>(await reader.ReadToEndAsync().ConfigureAwait(false), PreviewJson);
            }
            catch (JsonException ex)
            {
                return BadRequest("Design could not be read: " + ex.Message);
            }
        }

        if (req is null || !CoverService.IsValidKey(req.Key ?? string.Empty))
        {
            return BadRequest();
        }

        Response.Headers.CacheControl = "no-cache";
        if (useUpload && _covers.FindUploaded(req.Key!) is { } uploaded)
        {
            Response.Headers["X-SP-Source"] = "upload";
            return PhysicalFile(uploaded, CoverService.MimeFor(uploaded));
        }

        var cfg = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var label = string.IsNullOrWhiteSpace(req.Label)
            ? string.IsNullOrWhiteSpace(cfg.PlaylistName) ? PluginConfiguration.DefaultName : cfg.PlaylistName.Trim()
            : req.Label.Trim();
        var ids = _picks.GetState(user.Id).Picks.Select(p => p.ItemId).ToList();
        try
        {
            var name = string.IsNullOrWhiteSpace(req.Name) ? null : req.Name.Trim();
            var bytes = _covers.Design(ids, req.Key!, name, req.Color ?? cfg.DefaultCoverColor, req.Theme, req.Cover, label, wide);
            if (bytes is null)
            {
                return NotFound();
            }

            Response.Headers["X-SP-Source"] = "design";
            return File(bytes, "image/jpeg");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Problem("Cover generation failed: " + ex.Message);
        }
    }

    private static byte[]? DecodeUpload(CoverUpload upload)
    {
        if (string.IsNullOrEmpty(upload.Data))
        {
            return null;
        }

        try
        {
            var data = upload.Data;
            var comma = data.IndexOf(',', StringComparison.Ordinal);
            return Convert.FromBase64String(data.StartsWith("data:", StringComparison.Ordinal) && comma > 0 ? data[(comma + 1)..] : data);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    [HttpDelete("Covers/{key}")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult DeleteCover([FromRoute] string key)
    {
        if (!CoverService.IsValidKey(key))
        {
            return BadRequest();
        }

        _covers.DeleteUpload(key);
        return NoContent();
    }

    /// <summary>Admin: the cover that would be used for a season, built from the admin's own current picks.</summary>
    [HttpGet("Covers/{key}/Preview")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult PreviewCover([FromRoute] string key, [FromQuery] bool wide)
    {
        var cfg = Plugin.Instance?.Configuration;
        var user = CurrentUser();
        if (cfg is null || user is null || !CoverService.IsValidKey(key))
        {
            return BadRequest();
        }

        var season = cfg.Seasons.FirstOrDefault(s => s.CoverKey == key);
        var title = string.IsNullOrWhiteSpace(cfg.PlaylistName) ? PluginConfiguration.DefaultName : cfg.PlaylistName.Trim();
        var ids = _picks.GetState(user.Id).Picks.Select(p => p.ItemId).ToList();
        var cover = _covers.Resolve(cfg, season, title, ids, wide);
        if (cover is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "no-cache";
        return File(cover.Value.Data, cover.Value.MimeType);
    }

    /// <summary>The web client script (loaded by index.html via File Transformation).</summary>
    [HttpGet("ClientScript")]
    [AllowAnonymous]
    [Produces("application/javascript")]
    public ActionResult ClientScript()
    {
        var cfg = Plugin.Instance?.Configuration;
        var stream = typeof(Plugin).Assembly.GetManifestResourceStream($"{typeof(Plugin).Namespace}.Web.forYou.js");
        if (stream is null)
        {
            return NotFound();
        }

        using var reader = new StreamReader(stream);
        var js = reader.ReadToEnd()
            .Replace("__SP_HOME_ROW__", cfg?.EnableHomeRow == false ? "false" : "true", StringComparison.Ordinal)
            .Replace("__SP_ENABLED__", cfg?.EnableWebScreen == false ? "false" : "true", StringComparison.Ordinal)
            .Replace("__SP_TITLE__", JsonSerializer.Serialize(string.IsNullOrWhiteSpace(cfg?.PlaylistName) ? PluginConfiguration.DefaultName : cfg.PlaylistName.Trim()), StringComparison.Ordinal);

        Response.Headers.CacheControl = "no-cache";
        return Content(js, "application/javascript");
    }

    private User? CurrentUser()
    {
        var claim = HttpContext.User.FindFirst(UserIdClaim)?.Value;
        return Guid.TryParse(claim, out var id) ? _users.GetUserById(id) : null;
    }

    private bool WritesPlaylist(User user) => (Plugin.Instance?.Configuration.EnablePlaylists ?? true) && _picks.IsEnabledFor(user);

    private PicksResponse ToResponse(UserPickState state)
    {
        return new PicksResponse
        {
            GeneratedAt = state.GeneratedAt,
            ActiveSeasons = state.ActiveSeasons.ToArray(),
            ActiveSeasonColors = state.ActiveSeasons
                .Select(n => Plugin.Instance?.Configuration.Seasons.FirstOrDefault(s => s.Name == n)?.Color ?? "#3A6EA5")
                .ToArray(),
            PlaylistId = state.PlaylistId?.ToString("N"),
            HiddenCount = state.Hidden.Count + state.Snoozed.Count,
            Picks = ToDtos(state.Picks)
        };
    }

    private PickDto[] ToDtos(IEnumerable<PickEntry> entries)
    {
        return entries
            .Select(p => (p, item: _library.GetItemById(p.ItemId)))
            .Where(x => x.item is not null)
            .Select(x => new PickDto
            {
                Id = x.item!.Id.ToString("N"),
                Name = x.item.Name,
                Year = x.item.ProductionYear,
                Rating = x.item.CommunityRating,
                OfficialRating = x.item.OfficialRating,
                RuntimeMinutes = x.item.RunTimeTicks is long t ? (int)(t / TimeSpan.TicksPerMinute) : null,
                Overview = x.item.Overview,
                Genres = x.item.Genres,
                Kind = x.p.Kind.ToString(),
                Reason = x.p.Reason
            })
            .ToArray();
    }
}

public class PreviewResponse
{
    [JsonPropertyName("user")]
    public string User { get; set; } = string.Empty;

    [JsonPropertyName("activeSeasons")]
    public string[] ActiveSeasons { get; set; } = Array.Empty<string>();

    [JsonPropertyName("unwatchedCandidates")]
    public int UnwatchedCandidates { get; set; }

    [JsonPropertyName("rediscoverCandidates")]
    public int RediscoverCandidates { get; set; }

    [JsonPropertyName("seasonalCandidates")]
    public int SeasonalCandidates { get; set; }

    [JsonPropertyName("picks")]
    public PickDto[] Picks { get; set; } = Array.Empty<PickDto>();

    [JsonPropertyName("seasonPlaylists")]
    public SeasonPreviewDto[] SeasonPlaylists { get; set; } = Array.Empty<SeasonPreviewDto>();
}

public class SeasonPreviewDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("picks")]
    public PickDto[] Picks { get; set; } = Array.Empty<PickDto>();
}

public class PicksResponse
{
    [JsonPropertyName("generatedAt")]
    public DateTime? GeneratedAt { get; set; }

    [JsonPropertyName("activeSeasons")]
    public string[] ActiveSeasons { get; set; } = Array.Empty<string>();

    [JsonPropertyName("activeSeasonColors")]
    public string[] ActiveSeasonColors { get; set; } = Array.Empty<string>();

    [JsonPropertyName("playlistId")]
    public string? PlaylistId { get; set; }

    [JsonPropertyName("hiddenCount")]
    public int HiddenCount { get; set; }

    [JsonPropertyName("picks")]
    public PickDto[] Picks { get; set; } = Array.Empty<PickDto>();
}

public class PickDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("year")]
    public int? Year { get; set; }

    [JsonPropertyName("rating")]
    public float? Rating { get; set; }

    [JsonPropertyName("officialRating")]
    public string? OfficialRating { get; set; }

    [JsonPropertyName("runtimeMinutes")]
    public int? RuntimeMinutes { get; set; }

    [JsonPropertyName("overview")]
    public string? Overview { get; set; }

    [JsonPropertyName("genres")]
    public string[] Genres { get; set; } = Array.Empty<string>();

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>A cover design from the settings page. Name empty = the out-of-season cover.</summary>
public class CoverRenderRequest
{
    /// <summary>Upload key: the season's name slug, or "default".</summary>
    public string? Key { get; set; }

    public string? Name { get; set; }

    public string? Color { get; set; }

    public string? Theme { get; set; }

    /// <summary>The playlist name to show as the label; empty = the saved one.</summary>
    public string? Label { get; set; }

    public CoverStyle? Cover { get; set; }
}

public class CoverUpload
{
    public string? MimeType { get; set; }

    /// <summary>Base64 image, optionally as a data: URL.</summary>
    public string Data { get; set; } = string.Empty;
}
