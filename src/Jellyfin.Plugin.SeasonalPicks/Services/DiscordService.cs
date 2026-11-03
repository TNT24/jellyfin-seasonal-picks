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

    /// <summary>Announced seasons that got playlists, so the "is over" message only mentions playlists when there were some.</summary>
    public List<string> SeasonsWithPlaylists { get; set; } = new();

    /// <summary>Server-local date (yyyy-MM-dd) the daily picks were last posted.</summary>
    public string? PicksPostedOn { get; set; }
}

/// <summary>A message to post, and what to remember once it has been posted.</summary>
internal sealed record DiscordOutgoing(JsonObject Message, Action<DiscordState> Record);

/// <summary>
/// Posts to Discord after a refresh of everyone: seasons starting and ending, and one user's picks.
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
            var outbox = new List<DiscordOutgoing>();

            if (cfg.DiscordAnnounceSeasons)
            {
                var states = users.Select(u => _store.Load(u.Id)).ToList();
                var playlistsOn = cfg.EnablePlaylists && cfg.EnableSeasonPlaylists;
                var (starting, ending) = SeasonChanges(cfg, state, active);

                foreach (var season in starting)
                {
                    var name = season.Name;
                    var withPlaylist = playlistsOn ? states.Count(s => s.SeasonPlaylistIds.ContainsKey(name)) : 0;
                    var sample = (picksUser?.State is { } ps && ps.SeasonPicks.TryGetValue(name, out var own) && own.Count > 0 ? own : null)
                        ?? states.Select(s => s.SeasonPicks.TryGetValue(name, out var p) ? p : null).FirstOrDefault(p => p is { Count: > 0 })
                        ?? new List<PickEntry>();
                    outbox.Add(new DiscordOutgoing(
                        DiscordMessages.SeasonStarted(
                            name,
                            season.Color,
                            today,
                            LastDay(season, today),
                            playlistsOn ? PicksService.SeasonPlaylistName(cfg, name) : null,
                            withPlaylist,
                            Films(sample),
                            cfg.PublicServerUrl),
                        s =>
                        {
                            AddOnce(s.AnnouncedSeasons, name);
                            if (withPlaylist > 0)
                            {
                                AddOnce(s.SeasonsWithPlaylists, name);
                            }
                        }));
                }

                // Playlists can also appear later in a season (turned on mid-season); remember that for its "is over" message.
                if (playlistsOn)
                {
                    foreach (var season in active.Where(s => state.AnnouncedSeasons.Contains(s.Name) && states.Any(u => u.SeasonPlaylistIds.ContainsKey(s.Name))))
                    {
                        AddOnce(state.SeasonsWithPlaylists, season.Name);
                    }
                }
                else
                {
                    // Season playlists turned off: they're removed now, so don't say so again when the season ends.
                    state.SeasonsWithPlaylists.Clear();
                }

                foreach (var name in ending)
                {
                    outbox.Add(new DiscordOutgoing(
                        DiscordMessages.SeasonEnded(name, state.SeasonsWithPlaylists.Contains(name)),
                        s => Forget(s, name)));
                }

                // Seasons that were renamed or deleted: forget them without saying they're over.
                foreach (var name in state.AnnouncedSeasons.Where(n => !cfg.Seasons.Any(s => s.Name == n)).ToList())
                {
                    Forget(state, name);
                }
            }
            else
            {
                // Announcements off: forget everything, so turning them back on never says a long-gone season is over.
                state.AnnouncedSeasons.Clear();
                state.SeasonsWithPlaylists.Clear();
            }

            if (picksUser is { } pu && state.PicksPostedOn != Day(today))
            {
                var films = Films(pu.State.Picks.Take(Math.Clamp(cfg.DiscordPicksCount, 1, 10)));
                if (films.Count > 0)
                {
                    outbox.Add(new DiscordOutgoing(
                        DiscordMessages.DailyPicks(pu.User.Username, pu.State.ActiveSeasons, films, active.FirstOrDefault()?.Color ?? cfg.DefaultCoverColor, cfg.PublicServerUrl),
                        s => s.PicksPostedOn = Day(today)));
                }
            }

            SaveState(state);
            if (outbox.Count == 0)
            {
                return;
            }

            var sent = await SendEachAsync((m, c) => _discord.SendAsync(cfg.DiscordWebhookUrl, m, c), outbox, state, SaveState, ct).ConfigureAwait(false);
            _logger.LogInformation("SeasonalPicks: posted {Count} Discord message(s)", sent);
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
        => _discord.SendAsync(cfg.DiscordWebhookUrl, DiscordMessages.Test(), ct);

    /// <summary>
    /// Seasons to announce as starting (active, not yet announced) and as over (announced, no longer active, still in
    /// the settings; renamed or deleted seasons aren't said to be over).
    /// </summary>
    internal static (IReadOnlyList<SeasonRule> Starting, IReadOnlyList<string> Ending) SeasonChanges(PluginConfiguration cfg, DiscordState state, IReadOnlyList<SeasonRule> active)
    {
        var starting = active.Where(s => !state.AnnouncedSeasons.Contains(s.Name)).ToList();
        var ending = state.AnnouncedSeasons
            .Where(n => !active.Any(s => s.Name == n) && cfg.Seasons.Any(s => s.Name == n))
            .ToList();
        return (starting, ending);
    }

    /// <summary>
    /// Posts each message in turn and records it (and saves) straight after it's posted, so if one fails, the ones
    /// before it aren't posted again next time. Throws the failure after recording what got through.
    /// </summary>
    /// <returns>How many were posted.</returns>
    internal static async Task<int> SendEachAsync(Func<JsonObject, CancellationToken, Task> send, IReadOnlyList<DiscordOutgoing> outbox, DiscordState state, Action<DiscordState> save, CancellationToken ct)
    {
        var sent = 0;
        foreach (var item in outbox)
        {
            await send(item.Message, ct).ConfigureAwait(false);
            item.Record(state);
            save(state);
            sent++;
        }

        return sent;
    }

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

    private static string Day(DateTime date) => date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    private static void AddOnce(List<string> list, string name)
    {
        if (!list.Contains(name))
        {
            list.Add(name);
        }
    }

    private static void Forget(DiscordState state, string name)
    {
        state.AnnouncedSeasons.Remove(name);
        state.SeasonsWithPlaylists.Remove(name);
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
