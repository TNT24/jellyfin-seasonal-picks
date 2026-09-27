using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

public enum PickKind
{
    Seasonal,
    Unwatched,
    Rediscover,

    // Appended (not inserted) so saved state files keep their meaning.
    NewRelease
}

public class PickEntry
{
    public Guid ItemId { get; set; }

    public PickKind Kind { get; set; }

    public string Reason { get; set; } = string.Empty;
}

/// <summary>Everything remembered per user between refreshes.</summary>
public class UserPickState
{
    public DateTime? GeneratedAt { get; set; }

    public List<PickEntry> Picks { get; set; } = new();

    public List<string> ActiveSeasons { get; set; } = new();

    /// <summary>Picks from the previous refresh, down-weighted so the list rotates.</summary>
    public List<Guid> PreviousPickIds { get; set; } = new();

    /// <summary>"Never suggest this" from the web screen.</summary>
    public List<Guid> Hidden { get; set; } = new();

    /// <summary>"Not now" from the web screen: item id to the date it may return.</summary>
    public Dictionary<Guid, DateTime> Snoozed { get; set; } = new();

    public Guid? PlaylistId { get; set; }

    /// <summary>Per active season (by name): the picks in that season's own playlist.</summary>
    public Dictionary<string, List<PickEntry>> SeasonPicks { get; set; } = new();

    /// <summary>Per season (by name): the playlist this plugin created, so it can be updated and removed when the season ends.</summary>
    public Dictionary<string, Guid> SeasonPlaylistIds { get; set; } = new();
}

/// <summary>Per-user JSON files under the plugin's data folder.</summary>
public class PickStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();
    private readonly ILogger<PickStore> _logger;

    public PickStore(ILogger<PickStore> logger)
    {
        _logger = logger;
    }

    private static string Folder
    {
        get
        {
            var root = Plugin.Instance?.DataFolderPath ?? Path.Combine(AppContext.BaseDirectory, "SeasonalPicks");
            var dir = Path.Combine(root, "users");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public SemaphoreSlim LockFor(Guid userId) => _locks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));

    public UserPickState Load(Guid userId)
    {
        var path = Path.Combine(Folder, userId.ToString("N") + ".json");
        if (!File.Exists(path))
        {
            return new UserPickState();
        }

        try
        {
            return JsonSerializer.Deserialize<UserPickState>(File.ReadAllText(path), JsonOptions) ?? new UserPickState();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SeasonalPicks: could not read state for user {UserId}, starting fresh", userId);
            return new UserPickState();
        }
    }

    public void Save(Guid userId, UserPickState state)
    {
        var path = Path.Combine(Folder, userId.ToString("N") + ".json");
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(state, JsonOptions));
        File.Move(tmp, path, true);
    }
}
