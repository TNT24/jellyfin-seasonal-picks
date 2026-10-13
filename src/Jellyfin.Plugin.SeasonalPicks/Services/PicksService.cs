using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Plugin.SeasonalPicks.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Playlists;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>A dry run: what a user would get, without writing anything.</summary>
public sealed class PreviewResult
{
    public required IReadOnlyList<string> ActiveSeasons { get; init; }

    public required int UnwatchedCandidates { get; init; }

    public required int RediscoverCandidates { get; init; }

    public required int SeasonalCandidates { get; init; }

    public required IReadOnlyList<PickEntry> Picks { get; init; }

    public required IReadOnlyDictionary<string, List<PickEntry>> SeasonPicks { get; init; }
}

/// <summary>
/// Orchestrates a refresh: score, pick, then write the outputs (playlists, mirror folder, covers, stored state).
/// </summary>
public class PicksService
{
    private readonly PickEngine _engine;
    private readonly PickStore _store;
    private readonly MirrorWriter _mirror;
    private readonly CoverService _covers;
    private readonly ILibraryManager _library;
    private readonly IUserManager _users;
    private readonly IPlaylistManager _playlists;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<PicksService> _logger;

    /// <summary>One mirror sync at a time (the nightly run, a swap and a finished movie can overlap).</summary>
    private readonly SemaphoreSlim _mirrorGate = new(1, 1);

    public PicksService(PickEngine engine, PickStore store, MirrorWriter mirror, CoverService covers, ILibraryManager library, IUserManager users, IPlaylistManager playlists, IFileSystem fileSystem, ILogger<PicksService> logger)
    {
        _engine = engine;
        _store = store;
        _mirror = mirror;
        _covers = covers;
        _library = library;
        _users = users;
        _playlists = playlists;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    private static PluginConfiguration Config => Plugin.Instance?.Configuration ?? new PluginConfiguration();

    public UserPickState GetState(Guid userId) => _store.Load(userId);

    public bool IsEnabledFor(User user)
    {
        var ids = Config.EnabledUserIds;
        return ids.Length == 0 || ids.Any(id => Guid.TryParse(id, out var g) && g == user.Id);
    }

    private bool WritesPlaylists(User user, PluginConfiguration cfg) => cfg.EnablePlaylists && IsEnabledFor(user);

    private static bool IsMirrorUser(User user, PluginConfiguration cfg)
        => cfg.EnableMirror && Guid.TryParse(cfg.MirrorUserId, out var id) && id == user.Id;

    public async Task RefreshAllAsync(IProgress<double> progress, CancellationToken ct)
    {
        var cfg = Config;
        var users = _users.AllUsers().Where(u => !u.HasPermission(PermissionKind.IsDisabled) && IsEnabledFor(u)).ToList();

        var mirrorUser = cfg.EnableMirror ? ResolveMirrorUser(cfg) : null;
        var total = users.Count + (mirrorUser is not null ? 1 : 0);
        var done = 0;

        foreach (var user in users)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await RefreshUserAsync(user, cfg.EnablePlaylists, ct, syncMirror: false).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "SeasonalPicks: refresh failed for {User}", user.Username);
            }

            progress.Report(100.0 * ++done / Math.Max(total, 1));
        }

        if (mirrorUser is not null)
        {
            // The mirror follows the chosen user's picks; generate them if that user isn't in the enabled set.
            var state = users.Any(u => u.Id == mirrorUser.Id)
                ? _store.Load(mirrorUser.Id)
                : await RefreshUserAsync(mirrorUser, false, ct, syncMirror: false).ConfigureAwait(false);
            await SyncMirrorAsync(cfg, state, ct).ConfigureAwait(false);
            progress.Report(100);
        }
    }

    public async Task<UserPickState> RefreshUserAsync(User user, bool writePlaylist, CancellationToken ct, bool syncMirror = true)
    {
        var cfg = Config;
        var gate = _store.LockFor(user.Id);
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var now = DateTime.UtcNow;
            var state = _store.Load(user.Id);
            PruneSnoozed(state, now);

            var ctx = _engine.CreateContext(user, cfg, state, now);
            var candidates = _engine.BuildCandidates(user, cfg, ctx);
            var picks = PickEngine.Select(candidates, cfg, ctx.Active.Count > 0, Random.Shared);

            state.PreviousPickIds = state.Picks.Select(p => p.ItemId).ToList();
            state.Picks = picks;
            state.ActiveSeasons = ctx.Active.Select(s => s.Name).ToList();
            state.SeasonPicks = cfg.EnableSeasonPlaylists
                ? ctx.Active.ToDictionary(s => s.Name, s => PickEngine.SelectForSeason(candidates, s, cfg.SeasonPlaylistCount, Random.Shared))
                : new Dictionary<string, List<PickEntry>>();
            state.GeneratedAt = now;

            if (writePlaylist)
            {
                await WritePlaylistsAsync(user, state, cfg, ct).ConfigureAwait(false);
            }

            _store.Save(user.Id, state);
            _logger.LogInformation(
                "SeasonalPicks: {Count} picks for {User} from {Candidates} candidates (seasons: {Seasons})",
                picks.Count,
                user.Username,
                candidates.Count,
                state.ActiveSeasons.Count > 0 ? string.Join(", ", state.ActiveSeasons) : "none");

            if (syncMirror && IsMirrorUser(user, cfg))
            {
                await SyncMirrorAsync(cfg, state, ct).ConfigureAwait(false);
            }

            return state;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>What <see cref="RefreshUserAsync"/> would pick with the given settings. Writes nothing.</summary>
    public PreviewResult Preview(User user, PluginConfiguration cfg)
    {
        var now = DateTime.UtcNow;
        var state = _store.Load(user.Id);
        var ctx = _engine.CreateContext(user, cfg, state, now);
        var candidates = _engine.BuildCandidates(user, cfg, ctx);
        return new PreviewResult
        {
            ActiveSeasons = ctx.Active.Select(s => s.Name).ToList(),
            UnwatchedCandidates = candidates.Count(c => c.Pool == PickKind.Unwatched),
            RediscoverCandidates = candidates.Count(c => c.Pool == PickKind.Rediscover),
            SeasonalCandidates = candidates.Count(c => c.Season is not null),
            Picks = PickEngine.Select(candidates, cfg, ctx.Active.Count > 0, Random.Shared),
            SeasonPicks = cfg.EnableSeasonPlaylists
                ? ctx.Active.ToDictionary(s => s.Name, s => PickEngine.SelectForSeason(candidates, s, cfg.SeasonPlaylistCount, Random.Shared))
                : new Dictionary<string, List<PickEntry>>()
        };
    }

    /// <summary>Remove one pick ("not now" = 30-day snooze, or hide forever) and put a similar one in its slot.</summary>
    public async Task<UserPickState> SwapAsync(User user, Guid itemId, bool forever, CancellationToken ct)
    {
        var cfg = Config;
        var gate = _store.LockFor(user.Id);
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var now = DateTime.UtcNow;
            var state = _store.Load(user.Id);
            PruneSnoozed(state, now);

            if (forever)
            {
                if (!state.Hidden.Contains(itemId))
                {
                    state.Hidden.Add(itemId);
                }
            }
            else
            {
                state.Snoozed[itemId] = now.AddDays(30);
            }

            var key = _library.GetItemById(itemId) is { } item ? PickEngine.MovieKey(item) : null;
            if (key is not null && ReplaceKey(user, state, cfg, key, now))
            {
                await AfterPicksChangedAsync(user, state, cfg, ct).ConfigureAwait(false);
            }

            _store.Save(user.Id, state);
            return state;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// The user finished (or marked played) a movie. If it was one of their picks, swap it for a new one now,
    /// so the TV playlist doesn't keep offering something they just watched. Returns true when a pick was replaced.
    /// </summary>
    public async Task<bool> ReplaceWatchedAsync(User user, BaseItem watched, CancellationToken ct)
    {
        var cfg = Config;
        var gate = _store.LockFor(user.Id);
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var state = _store.Load(user.Id);
            var now = DateTime.UtcNow;
            var key = PickEngine.MovieKey(watched); // matches the pick even when a different copy (e.g. the mirror's) was played
            if (!ReplaceKey(user, state, cfg, key, now))
            {
                return false;
            }

            await AfterPicksChangedAsync(user, state, cfg, ct).ConfigureAwait(false);
            _store.Save(user.Id, state);
            _logger.LogInformation("SeasonalPicks: {User} watched {Movie}; swapped in a new pick", user.Username, watched.Name);
            return true;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task UnhideAllAsync(User user, CancellationToken ct)
    {
        var gate = _store.LockFor(user.Id);
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var state = _store.Load(user.Id);
            state.Hidden.Clear();
            state.Snoozed.Clear();
            _store.Save(user.Id, state);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>Replace every pick of the film <paramref name="key"/> (main list and season lists). False if it wasn't picked.</summary>
    private bool ReplaceKey(User user, UserPickState state, PluginConfiguration cfg, string key, DateTime now)
    {
        var lists = new List<(List<PickEntry> List, SeasonRule? Season)> { (state.Picks, null) };
        foreach (var (name, list) in state.SeasonPicks)
        {
            if (cfg.Seasons.FirstOrDefault(s => s.Name == name) is { } season)
            {
                lists.Add((list, season));
            }
        }

        var keysById = lists.SelectMany(l => l.List).Select(p => p.ItemId).Distinct()
            .ToDictionary(id => id, id => _library.GetItemById(id) is { } i ? PickEngine.MovieKey(i) : "id:" + id.ToString("N"));
        if (!keysById.ContainsValue(key))
        {
            return false;
        }

        var candidates = _engine.BuildCandidates(user, cfg, state, now);
        foreach (var (list, season) in lists)
        {
            var index = list.FindIndex(p => keysById[p.ItemId] == key);
            if (index < 0)
            {
                continue;
            }

            var exclude = list.Select(p => keysById[p.ItemId]).ToHashSet();
            var replacement = PickEngine.PickReplacement(candidates, list[index].Kind, exclude, Random.Shared, season);
            if (replacement is null)
            {
                list.RemoveAt(index);
            }
            else
            {
                list[index] = replacement;
            }
        }

        return true;
    }

    /// <summary>After a swap: update the playlists, their covers and (for the mirror user) the mirror folder.</summary>
    private async Task AfterPicksChangedAsync(User user, UserPickState state, PluginConfiguration cfg, CancellationToken ct)
    {
        if (WritesPlaylists(user, cfg))
        {
            await WritePlaylistsAsync(user, state, cfg, ct).ConfigureAwait(false);
        }

        if (IsMirrorUser(user, cfg))
        {
            await SyncMirrorAsync(cfg, state, ct).ConfigureAwait(false);
        }
    }

    private static string BaseName(PluginConfiguration cfg)
        => string.IsNullOrWhiteSpace(cfg.PlaylistName) ? PluginConfiguration.DefaultName : cfg.PlaylistName.Trim();

    public static string SeasonPlaylistName(PluginConfiguration cfg, string season)
    {
        var template = string.IsNullOrWhiteSpace(cfg.SeasonPlaylistName) ? "{season} Picks" : cfg.SeasonPlaylistName.Trim();
        return template.Contains("{season}", StringComparison.Ordinal)
            ? template.Replace("{season}", season, StringComparison.Ordinal)
            : season + " " + template; // keep names unique when two seasons overlap
    }

    /// <summary>The main For You playlist, one playlist per active season, their covers and descriptions; removes ended season playlists.</summary>
    private async Task WritePlaylistsAsync(User user, UserPickState state, PluginConfiguration cfg, CancellationToken ct)
    {
        var active = cfg.Seasons.Where(s => state.ActiveSeasons.Contains(s.Name)).ToList();
        var baseName = BaseName(cfg);
        var name = cfg.PlaylistNameShowsSeason && state.ActiveSeasons.Count > 0
            ? $"{baseName} - {string.Join(" & ", state.ActiveSeasons)}"
            : baseName;

        // Prefer the remembered id so renames (season suffix, settings change) don't create duplicates.
        state.PlaylistId = await UpsertPlaylistAsync(
            user,
            state.PlaylistId,
            name,
            p => p.Name.Equals(baseName, StringComparison.OrdinalIgnoreCase) || p.Name.StartsWith(baseName + " - ", StringComparison.OrdinalIgnoreCase),
            state.Picks,
            cfg,
            ct).ConfigureAwait(false);

        if (cfg.EnableSeasonalCovers && state.PlaylistId is Guid pid && _library.GetItemById(pid) is { } playlist)
        {
            await _covers.ApplyAsync(playlist, cfg, active.FirstOrDefault(), baseName, state.Picks.Select(p => p.ItemId).ToList(), false, ct).ConfigureAwait(false);
        }

        // Season playlists.
        var keep = new HashSet<string>(StringComparer.Ordinal);
        if (cfg.EnableSeasonPlaylists)
        {
            foreach (var season in active)
            {
                if (!state.SeasonPicks.TryGetValue(season.Name, out var picks) || picks.Count == 0)
                {
                    continue; // nothing in the library matches: no empty playlist
                }

                var seasonName = SeasonPlaylistName(cfg, season.Name);
                var id = await UpsertPlaylistAsync(
                    user,
                    state.SeasonPlaylistIds.TryGetValue(season.Name, out var known) ? known : null,
                    seasonName,
                    p => p.Name.Equals(seasonName, StringComparison.OrdinalIgnoreCase),
                    picks,
                    cfg,
                    ct).ConfigureAwait(false);
                if (id is not Guid sid)
                {
                    continue;
                }

                state.SeasonPlaylistIds[season.Name] = sid;
                keep.Add(season.Name);
                if (cfg.EnableSeasonalCovers && _library.GetItemById(sid) is { } seasonPlaylist)
                {
                    await _covers.ApplyAsync(seasonPlaylist, cfg, season, seasonName, picks.Select(p => p.ItemId).ToList(), false, ct).ConfigureAwait(false);
                }
            }
        }

        // Season over (or feature switched off): remove the playlists this plugin made for it.
        foreach (var (seasonName, id) in state.SeasonPlaylistIds.Where(kv => !keep.Contains(kv.Key)).ToList())
        {
            DeleteOwnPlaylist(user, id);
            state.SeasonPlaylistIds.Remove(seasonName);
        }
    }

    /// <summary>Create or update a private playlist; returns its id. Also writes the reasons into its description.</summary>
    private async Task<Guid?> UpsertPlaylistAsync(User user, Guid? knownId, string name, Func<Playlist, bool> fallbackMatch, IReadOnlyList<PickEntry> picks, PluginConfiguration cfg, CancellationToken ct)
    {
        var ids = picks.Select(p => p.ItemId).ToList();
        var playlists = _playlists.GetPlaylists(user.Id).ToList();
        var existing = (knownId is Guid pid ? playlists.FirstOrDefault(p => p.Id == pid) : null)
            ?? playlists.FirstOrDefault(p => p.OwnerUserId == user.Id && p.Name is not null && fallbackMatch(p));

        Guid? id;
        if (existing is null)
        {
            var created = await _playlists.CreatePlaylist(new PlaylistCreationRequest
            {
                Name = name,
                ItemIdList = ids,
                MediaType = MediaType.Video,
                UserId = user.Id,
                Public = false
            }).ConfigureAwait(false);
            id = Guid.TryParse(created.Id, out var g) ? g : null;
        }
        else
        {
            await _playlists.UpdatePlaylist(new PlaylistUpdateRequest
            {
                Id = existing.Id,
                UserId = user.Id,
                Name = name,
                Ids = ids
            }).ConfigureAwait(false);
            id = existing.Id;
        }

        if (id is Guid playlistId)
        {
            await WriteReasonsAsync(playlistId, cfg.PlaylistShowsReasons ? Describe(picks) : null, ct).ConfigureAwait(false);
        }

        return id;
    }

    /// <summary>"Title (Year): reason" per line. Playlist entries can't carry text, so the playlist's description does.</summary>
    private string Describe(IReadOnlyList<PickEntry> picks)
    {
        var sb = new StringBuilder();
        foreach (var p in picks)
        {
            if (_library.GetItemById(p.ItemId) is not { } item)
            {
                continue;
            }

            sb.Append(item.Name);
            if (item.ProductionYear is int year)
            {
                sb.Append(" (").Append(year).Append(')');
            }

            sb.Append(": ").Append(p.Reason).Append('\n');
        }

        return sb.ToString().TrimEnd();
    }

    private async Task WriteReasonsAsync(Guid playlistId, string? overview, CancellationToken ct)
    {
        try
        {
            if (_library.GetItemById(playlistId) is not Playlist playlist || string.Equals(playlist.Overview ?? string.Empty, overview ?? string.Empty, StringComparison.Ordinal))
            {
                return;
            }

            playlist.Overview = overview;
            await playlist.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "SeasonalPicks: could not write the playlist description");
        }
    }

    /// <summary>Deletes a playlist only if this user owns it. Its folder is removed only when it's inside Jellyfin's own playlists folder.</summary>
    private void DeleteOwnPlaylist(User user, Guid id)
    {
        try
        {
            if (_library.GetItemById(id) is not Playlist playlist || playlist.OwnerUserId != user.Id)
            {
                return;
            }

            var root = _playlists.GetPlaylistsFolder()?.Path;
            var inPlaylistsFolder = root is not null && playlist.Path is not null
                && playlist.Path.StartsWith(root.TrimEnd('/', '\\') + Path.DirectorySeparatorChar, StringComparison.Ordinal);
            _library.DeleteItem(playlist, new DeleteOptions { DeleteFileLocation = inPlaylistsFolder }, true);
            _logger.LogInformation("SeasonalPicks: removed ended season playlist {Name} for {User}", playlist.Name, user.Username);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SeasonalPicks: could not remove season playlist {Id}", id);
        }
    }

    /// <summary>Mirror folder = the mirror user's main picks. Then rescan it and refresh its cover.</summary>
    private async Task SyncMirrorAsync(PluginConfiguration cfg, UserPickState state, CancellationToken ct)
    {
        await _mirrorGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var ids = state.Picks.Select(p => p.ItemId).ToList();
            var changed = _mirror.Sync(cfg, ids);
            var library = FindMirrorLibrary(cfg);
            if (changed && cfg.MirrorTriggerLibraryScan)
            {
                await ScanMirrorAsync(library, ct).ConfigureAwait(false);
            }

            if (cfg.EnableSeasonalCovers && library is not null)
            {
                var season = PickEngine.ActiveSeasons(cfg, DateTime.Now).FirstOrDefault();
                // The library tile is labelled with the library's own name (e.g. "Suggested").
                var label = string.IsNullOrWhiteSpace(library.Name) ? BaseName(cfg) : library.Name;
                await _covers.ApplyAsync(library, cfg, season, label, ids, true, ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "SeasonalPicks: mirror folder sync failed");
        }
        finally
        {
            _mirrorGate.Release();
        }
    }

    /// <summary>The Jellyfin library whose folder is the mirror path (so its tile gets the seasonal cover).</summary>
    private BaseItem? FindMirrorLibrary(PluginConfiguration cfg)
    {
        var mirror = cfg.MirrorPath.TrimEnd('/', '\\');
        var folder = _library.GetVirtualFolders()
            .FirstOrDefault(v => v.Locations.Any(l => string.Equals(l.TrimEnd('/', '\\'), mirror, StringComparison.Ordinal)));
        return folder is not null && Guid.TryParse(folder.ItemId, out var id) ? _library.GetItemById(id) : null;
    }

    /// <summary>Rescan just the mirror library; a full scan only if no library points at the folder yet.</summary>
    private async Task ScanMirrorAsync(BaseItem? library, CancellationToken ct)
    {
        if (library is not Folder folder)
        {
            _library.QueueLibraryScan();
            return;
        }

        try
        {
            await folder.ValidateChildren(new Progress<double>(), new MetadataRefreshOptions(new DirectoryService(_fileSystem)), true, false, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "SeasonalPicks: mirror library rescan failed; falling back to a full library scan");
            _library.QueueLibraryScan();
        }
    }

    private User? ResolveMirrorUser(PluginConfiguration cfg)
    {
        if (Guid.TryParse(cfg.MirrorUserId, out var id) && _users.GetUserById(id) is User u)
        {
            return u;
        }

        _logger.LogWarning("SeasonalPicks: mirror folder is enabled but no valid mirror user is set; skipping");
        return null;
    }

    private static void PruneSnoozed(UserPickState state, DateTime now)
    {
        foreach (var key in state.Snoozed.Where(kv => kv.Value <= now).Select(kv => kv.Key).ToList())
        {
            state.Snoozed.Remove(key);
        }
    }
}
