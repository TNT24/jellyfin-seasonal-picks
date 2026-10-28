using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.SeasonalPicks.Configuration;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>What has already been posted, so a season is announced once and picks once a day.</summary>
public class DiscordState
{
    public List<string> AnnouncedSeasons { get; set; } = new();

    /// <summary>Server-local date (yyyy-MM-dd) the daily picks were last posted.</summary>
    public string? PicksPostedOn { get; set; }
}

/// <summary>
/// Posts to Discord after the nightly refresh: seasons starting and ending, and one user's picks.
/// Never throws: Discord being down mustn't fail a refresh.
/// </summary>
public class DiscordService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly DiscordNotifier _discord;
    private readonly PickStore _store;
    private readonly ILibraryManager _library;
    private readonly ILogger<DiscordService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public DiscordService(DiscordNotifier discord, PickStore store, ILibraryManager library, ILogger<DiscordService> logger)
    {
        _discord = discord;
        _store = store;
        _library = library;
        _logger = logger;
    }

    private static string StatePath => Path.Combine(Plugin.Instance?.DataFolderPath ?? Path.Combine(AppContext.BaseDirectory, "SeasonalPicks"), "discord.json");

    /// <param name="users">The users that were just refreshed.</param>
    /// <param name="picksUser">The user whose picks are posted, with their fresh picks; null = don't post picks.</param>
    public async Task AfterRefreshAsync(PluginConfiguration cfg, IReadOnlyList<User> users, (User User, UserPickState State)? picksUser, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cfg.DiscordWebhookUrl))
        {
            return;
        }

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var today = DateTime.Now.Date;
            var state = LoadState();
            var active = PickEngine.ActiveSeasons(cfg, today);
            var messages = new List<JsonObject>();

            if (cfg.DiscordAnnounceSeasons)
            {
                var states = users.Select(u => _store.Load(u.Id)).ToList();
                foreach (var season in active.Where(s => !state.AnnouncedSeasons.Contains(s.Name)))
                {
                    var withPlaylist = states.Count(s => s.SeasonPlaylistIds.ContainsKey(season.Name));
                    var sample = (picksUser?.State is { } ps && ps.SeasonPicks.TryGetValue(season.Name, out var own) ? own : null)
                        ?? states.Select(s => s.SeasonPicks.TryGetValue(season.Name, out var p) ? p : null).FirstOrDefault(p => p is { Count: > 0 })
                        ?? new List<PickEntry>();
                    messages.Add(DiscordMessages.SeasonStarted(
                        season.Name,
                        season.Color,
                        today,
                        LastDay(season, today),
                        cfg.EnablePlaylists && cfg.EnableSeasonPlaylists ? PicksService.SeasonPlaylistName(cfg, season.Name) : null,
                        withPlaylist,
                        Films(sample),
                        cfg.PublicServerUrl));
                }

                foreach (var name in state.AnnouncedSeasons.Where(n => !active.Any(s => s.Name == n)))
                {
                    messages.Add(DiscordMessages.SeasonEnded(name, cfg.EnablePlaylists && cfg.EnableSeasonPlaylists));
                }
            }

            var postPicks = picksUser is not null && state.PicksPostedOn != today.ToString("yyyy-MM-dd");
            if (postPicks && picksUser is { } pu)
            {
                var films = Films(pu.State.Picks.Take(Math.Clamp(cfg.DiscordPicksCount, 1, 10)));
                if (films.Count > 0)
                {
                    messages.Add(DiscordMessages.DailyPicks(pu.User.Username, pu.State.ActiveSeasons, films, active.FirstOrDefault()?.Color ?? cfg.DefaultCoverColor, cfg.PublicServerUrl));
                }
            }

            if (messages.Count == 0)
            {
                return;
            }

            await _discord.SendAsync(cfg.DiscordWebhookUrl, messages, ct).ConfigureAwait(false);

            // Only remember what was actually sent, so a failed post is tried again tomorrow.
            state.AnnouncedSeasons = cfg.DiscordAnnounceSeasons ? active.Select(s => s.Name).ToList() : state.AnnouncedSeasons;
            if (postPicks)
            {
                state.PicksPostedOn = today.ToString("yyyy-MM-dd");
            }

            SaveState(state);
            _logger.LogInformation("SeasonalPicks: posted {Count} Discord message(s)", messages.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "SeasonalPicks: could not post to Discord");
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task TestAsync(PluginConfiguration cfg, CancellationToken ct)
        => _discord.SendAsync(cfg.DiscordWebhookUrl, new[] { DiscordMessages.Test() }, ct);

    /// <summary>The season's last day on or after <paramref name="today"/> (today itself for a one-day event).</summary>
    internal static DateTime LastDay(SeasonRule season, DateTime today)
    {
        var day = today.Date;
        for (var i = 0; i < 366 && season.IsActive(day.AddDays(1)); i++)
        {
            day = day.AddDays(1);
        }

        return day;
    }

    private List<DiscordFilm> Films(IEnumerable<PickEntry> picks)
        => picks
            .Select(p => (Pick: p, Item: _library.GetItemById(p.ItemId)))
            .Where(x => x.Item is not null)
            .Select(x => new DiscordFilm(x.Item!.Id, x.Item.Name, x.Item.ProductionYear, x.Pick.Reason))
            .ToList();

    private DiscordState LoadState()
    {
        try
        {
            return File.Exists(StatePath)
                ? JsonSerializer.Deserialize<DiscordState>(File.ReadAllText(StatePath), JsonOptions) ?? new DiscordState()
                : new DiscordState();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "SeasonalPicks: could not read the Discord state, starting fresh");
            return new DiscordState();
        }
    }

    private static void SaveState(DiscordState state)
    {
        var path = StatePath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(state, JsonOptions));
        File.Move(path + ".tmp", path, true);
    }
}
