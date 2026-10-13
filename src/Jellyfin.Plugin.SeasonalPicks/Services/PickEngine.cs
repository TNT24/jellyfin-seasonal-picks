using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.SeasonalPicks.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>A movie the user could be offered, with its computed weight and why.</summary>
public sealed class Candidate
{
    public required BaseItem Item { get; init; }

    /// <summary>Same value for every copy of a film (see <see cref="PickEngine.MovieKey"/>).</summary>
    public required string Key { get; init; }

    public required PickKind Pool { get; init; }

    public double Weight { get; set; }

    /// <summary>The best-matching active season, if any.</summary>
    public SeasonRule? Season { get; set; }

    public double SeasonStrength { get; set; }

    /// <summary>Every active season this movie matches, with its strength (drives the per-season playlists).</summary>
    public IReadOnlyDictionary<SeasonRule, double> SeasonMatches { get; init; } = new Dictionary<SeasonRule, double>();

    /// <summary>Unwatched and released within NewReleaseDays.</summary>
    public bool IsNewRelease { get; init; }

    /// <summary>First listed genre, used for the per-genre cap.</summary>
    public string? Genre { get; init; }

    public required string Reason { get; init; }
}

/// <summary>What the user's history says about one film (merged across its copies).</summary>
public readonly record struct WatchInfo(bool Played, DateTime? LastPlayed, bool InProgress, bool IsFavorite, bool Liked)
{
    public static WatchInfo From(UserItemData? ud) => ud is null
        ? default
        : new WatchInfo(ud.Played || ud.PlayCount > 0, ud.LastPlayedDate, ud.PlaybackPositionTicks > 0, ud.IsFavorite, ud.Likes == true);

    public static WatchInfo Merge(WatchInfo a, WatchInfo b) => new(
        a.Played || b.Played,
        a.LastPlayed is null ? b.LastPlayed : b.LastPlayed is null ? a.LastPlayed : (a.LastPlayed > b.LastPlayed ? a.LastPlayed : b.LastPlayed),
        a.InProgress || b.InProgress,
        a.IsFavorite || b.IsFavorite,
        a.Liked || b.Liked);
}

/// <summary>Where a film sits in a collection (box set) the user has.</summary>
public sealed class SeriesPosition
{
    public required string CollectionName { get; init; }

    /// <summary>An earlier film in the collection hasn't been watched (so don't suggest this one yet).</summary>
    public bool EarlierUnwatched { get; init; }

    /// <summary>Everything before it has been watched and it hasn't: the natural next one.</summary>
    public bool IsNext { get; init; }
}

/// <summary>Everything the per-movie rules need, computed once per refresh. Pure data, so tests can build one.</summary>
public sealed class PickContext
{
    public required PluginConfiguration Config { get; init; }

    public required DateTime Now { get; init; }

    public required IReadOnlyList<SeasonRule> Active { get; init; }

    public required IReadOnlyList<SeasonRule> InactiveHidden { get; init; }

    public required IReadOnlyDictionary<SeasonRule, Regex?> Keywords { get; init; }

    /// <summary>Hidden or snoozed films (by <see cref="PickEngine.MovieKey"/>, so every copy is covered).</summary>
    public IReadOnlySet<string> ExcludedKeys { get; init; } = new HashSet<string>();

    /// <summary>Films picked last time, down-weighted so the list rotates.</summary>
    public IReadOnlySet<string> PreviousKeys { get; init; } = new HashSet<string>();

    public IReadOnlyDictionary<string, SeriesPosition> Series { get; init; } = new Dictionary<string, SeriesPosition>();

    public DateTime RecentCutoff => Now.AddDays(-Math.Max(0, Config.RecentlyWatchedDays));

    public DateTime RediscoverCutoff => Now.AddDays(-Math.Max(Config.RediscoverAfterDays, Config.RecentlyWatchedDays));

    /// <param name="localNow">The server's local date, which decides which seasons are on.</param>
    public static PickContext Create(PluginConfiguration cfg, DateTime now, DateTime localNow, IReadOnlySet<string>? excludedKeys = null, IReadOnlySet<string>? previousKeys = null, IReadOnlyDictionary<string, SeriesPosition>? series = null)
    {
        var active = PickEngine.ActiveSeasons(cfg, localNow);
        var inactiveHidden = cfg.Seasons.Where(s => s.Enabled && s.HideOutOfSeason && !active.Contains(s)).ToList();
        return new PickContext
        {
            Config = cfg,
            Now = now,
            Active = active,
            InactiveHidden = inactiveHidden,
            Keywords = PickEngine.KeywordPatterns(active.Concat(inactiveHidden)),
            ExcludedKeys = excludedKeys ?? new HashSet<string>(),
            PreviousKeys = previousKeys ?? new HashSet<string>(),
            Series = series ?? new Dictionary<string, SeriesPosition>()
        };
    }
}

/// <summary>
/// Scores a user's movies. Library reads only: no playlist or file writes.
/// The rules themselves are static and pure (<see cref="Evaluate"/>, <see cref="Select"/>) so they can be unit tested.
/// </summary>
public class PickEngine
{
    public const double StrongMatch = 1.0;
    public const double GenreMatch = 0.6;

    private readonly ILibraryManager _library;
    private readonly IUserDataManager _userData;

    public PickEngine(ILibraryManager library, IUserDataManager userData)
    {
        _library = library;
        _userData = userData;
    }

    public static IReadOnlyList<SeasonRule> ActiveSeasons(PluginConfiguration cfg, DateTime now)
        => cfg.Seasons.Where(s => s.Enabled && s.IsActive(now)).ToList();

    /// <summary>Identifies a film across copies/libraries: TMDb id, then IMDb id, then the item id.</summary>
    public static string MovieKey(BaseItem item)
    {
        var tmdb = item.GetProviderId(MetadataProvider.Tmdb);
        if (!string.IsNullOrWhiteSpace(tmdb))
        {
            return "tmdb:" + tmdb.Trim();
        }

        var imdb = item.GetProviderId(MetadataProvider.Imdb);
        return !string.IsNullOrWhiteSpace(imdb) ? "imdb:" + imdb.Trim().ToLowerInvariant() : "id:" + item.Id.ToString("N");
    }

    /// <summary>Keys of the given item ids (items that no longer exist are skipped).</summary>
    public HashSet<string> KeysFor(IEnumerable<Guid> ids)
        => ids.Select(id => _library.GetItemById(id)).OfType<BaseItem>().Select(MovieKey).ToHashSet();

    public PickContext CreateContext(User user, PluginConfiguration cfg, UserPickState state, DateTime now)
    {
        var excluded = state.Hidden.Concat(state.Snoozed.Where(kv => kv.Value > now).Select(kv => kv.Key));
        return PickContext.Create(
            cfg,
            now,
            now.ToLocalTime(), // watch dates are UTC; season windows follow the server's local calendar (set TZ on the container)
            KeysFor(excluded),
            KeysFor(state.PreviousPickIds),
            cfg.CollectionAware ? BuildSeries(user, cfg) : null);
    }

    public List<Candidate> BuildCandidates(User user, PluginConfiguration cfg, UserPickState state, DateTime now)
        => BuildCandidates(user, cfg, CreateContext(user, cfg, state, now));

    public List<Candidate> BuildCandidates(User user, PluginConfiguration cfg, PickContext ctx)
    {
        var list = GetMovies(user, cfg);
        var userData = _userData.UserDataFor(list, user);
        var entries = list.Select(i => (i, userData.TryGetValue(i.Id, out var ud) ? WatchInfo.From(ud) : default));

        var result = new List<Candidate>();
        foreach (var (item, watch) in MergeCopies(entries))
        {
            if (Evaluate(item, watch, ctx) is { } c)
            {
                result.Add(c);
            }
        }

        return result;
    }

    /// <summary>The user's movies in the chosen libraries, minus the mirror folder's own copies.</summary>
    private List<BaseItem> GetMovies(User user, PluginConfiguration cfg)
    {
        var query = new InternalItemsQuery(user)
        {
            IncludeItemTypes = new[] { BaseItemKind.Movie },
            Recursive = true,
            IsVirtualItem = false,
        };
        if (cfg.ExcludeTags.Length > 0)
        {
            query.ExcludeTags = cfg.ExcludeTags;
        }

        IEnumerable<BaseItem> items = _library.GetItemList(query);

        // Never pick from the mirror folder itself (those are copies of the real movies).
        var mirror = NormalizeDir(cfg.EnableMirror ? cfg.MirrorPath : null);
        if (mirror is not null)
        {
            items = items.Where(i => i.Path is null || !i.Path.StartsWith(mirror, StringComparison.OrdinalIgnoreCase));
        }

        // Filter by library after the user-scoped query so user access rules still apply.
        // Matched on the libraries' folder paths, looked up once (not a collection-folder lookup per movie).
        var libraryIds = cfg.LibraryIds.Select(id => Guid.TryParse(id, out var g) ? g : Guid.Empty).Where(g => g != Guid.Empty).ToHashSet();
        if (libraryIds.Count > 0)
        {
            var roots = _library.GetVirtualFolders()
                .Where(v => Guid.TryParse(v.ItemId, out var g) && libraryIds.Contains(g))
                .SelectMany(v => v.Locations)
                .Select(NormalizeDir)
                .OfType<string>()
                .ToList();
            items = items.Where(i => i.Path is not null && roots.Any(r => i.Path.StartsWith(r, StringComparison.Ordinal)));
        }

        return items.ToList();
    }

    /// <summary>
    /// For each collection (box set) the user can see: which films are "next" and which come after an unwatched one.
    /// Films are ordered by release date.
    /// </summary>
    private Dictionary<string, SeriesPosition> BuildSeries(User user, PluginConfiguration cfg)
    {
        var result = new Dictionary<string, SeriesPosition>();
        var sets = _library.GetItemList(new InternalItemsQuery(user) { IncludeItemTypes = new[] { BaseItemKind.BoxSet }, Recursive = true });
        foreach (var set in sets.OfType<BoxSet>())
        {
            var movies = set.GetLinkedChildren().OfType<Movie>().ToList();
            if (movies.Count < 2)
            {
                continue;
            }

            var userData = _userData.UserDataFor(movies.Cast<BaseItem>().ToList(), user);
            var films = MergeCopies(movies.Select(m => ((BaseItem)m, userData.TryGetValue(m.Id, out var ud) ? WatchInfo.From(ud) : default)));
            foreach (var (key, pos) in SeriesPositions(set.Name, films))
            {
                // A film in several collections: "next" in any wins; "wait" only when no collection says next.
                if (!result.TryGetValue(key, out var existing) || (pos.IsNext && !existing.IsNext) || (pos.EarlierUnwatched && !existing.IsNext))
                {
                    result[key] = pos;
                }
            }
        }

        return result;
    }

    /// <summary>Pure: order a collection by release and mark each unwatched film as next or waiting.</summary>
    public static IEnumerable<(string Key, SeriesPosition Position)> SeriesPositions(string collectionName, IEnumerable<(BaseItem Item, WatchInfo Watch)> films)
    {
        var ordered = films
            .OrderBy(f => f.Item.PremiereDate ?? (f.Item.ProductionYear is int y ? new DateTime(y, 1, 1) : DateTime.MaxValue))
            .ThenBy(f => f.Item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var anyEarlierUnwatched = false;
        for (var i = 0; i < ordered.Count; i++)
        {
            var (item, watch) = ordered[i];
            if (!watch.Played)
            {
                yield return (MovieKey(item), new SeriesPosition
                {
                    CollectionName = collectionName,
                    EarlierUnwatched = anyEarlierUnwatched,
                    IsNext = i > 0 && !anyEarlierUnwatched
                });
                anyEarlierUnwatched = true;
            }
        }
    }

    /// <summary>
    /// Pure: one entry per film. Copies (same TMDb/IMDb id) collapse into the widest-resolution copy,
    /// with their watch history merged so a copy watched elsewhere still counts.
    /// </summary>
    public static List<(BaseItem Item, WatchInfo Watch)> MergeCopies(IEnumerable<(BaseItem Item, WatchInfo Watch)> entries)
    {
        return entries
            .GroupBy(e => MovieKey(e.Item))
            .Select(g =>
            {
                var best = g.OrderByDescending(e => e.Item.Width).ThenBy(e => e.Item.Id).First().Item;
                var watch = g.Select(e => e.Watch).Aggregate(WatchInfo.Merge);
                return (best, watch);
            })
            .ToList();
    }

    /// <summary>Pure: whether a film can be offered, from which pool, how strongly, and why. Null = not offered.</summary>
    public static Candidate? Evaluate(BaseItem item, WatchInfo watch, PickContext ctx)
    {
        var cfg = ctx.Config;
        var key = MovieKey(item);
        if (ctx.ExcludedKeys.Contains(key))
        {
            return null;
        }

        if (cfg.MinCommunityRating > 0 && item.CommunityRating is float r && r < cfg.MinCommunityRating)
        {
            return null;
        }

        // Half-watched movies already sit in Continue Watching.
        if (!watch.Played && watch.InProgress)
        {
            return null;
        }

        if (watch.LastPlayed is DateTime lp && lp > ctx.RecentCutoff)
        {
            return null;
        }

        var text = BuildSearchText(item);

        // Out of season: a film belongs to the strongest season it strongly (keyword/tag) matches, by boost.
        // If that season is off and hides its films, so is the film. "Christmas Vacation" matches Christmas and
        // Summer ("vacation"); it belongs to Christmas, so it's hidden in July and shown in December.
        if (OwningSeason(item, text, ctx) is { } owner && ctx.InactiveHidden.Contains(owner))
        {
            return null;
        }

        ctx.Series.TryGetValue(key, out var series);

        // Don't suggest part 3 before part 1.
        if (!watch.Played && series is { EarlierUnwatched: true })
        {
            return null;
        }

        var matches = new Dictionary<SeasonRule, double>();
        foreach (var s in ctx.Active)
        {
            var strength = MatchStrength(item, text, s, ctx.Keywords);
            if (strength > 0)
            {
                matches[s] = strength;
            }
        }

        var best = matches.OrderByDescending(m => m.Value * m.Key.Boost).Select(m => (KeyValuePair<SeasonRule, double>?)m).FirstOrDefault();

        PickKind pool;
        double weight = item.CommunityRating is float cr ? 0.5 + (cr / 10.0) : 1.0;
        string reason;
        var isNewRelease = false;

        if (!watch.Played)
        {
            pool = PickKind.Unwatched;
            isNewRelease = IsNewRelease(item, cfg, ctx.Now);
            if (series is { IsNext: true })
            {
                weight *= 2;
                reason = "Next in " + series.CollectionName;
            }
            else if (isNewRelease)
            {
                weight *= 1.3;
                reason = "New release" + (item.ProductionYear is int ry ? $" ({ry})" : string.Empty);
            }
            else if (item.DateCreated > ctx.Now.AddDays(-30))
            {
                weight *= 1.5;
                reason = "Recently added";
            }
            else if (item.CommunityRating >= 7.5f)
            {
                reason = "Highly rated, not watched yet";
            }
            else
            {
                reason = "Not watched yet";
            }
        }
        else if (watch.LastPlayed is null || watch.LastPlayed < ctx.RediscoverCutoff)
        {
            pool = PickKind.Rediscover;
            if (watch.IsFavorite)
            {
                weight *= 2;
            }
            else if (watch.Liked)
            {
                weight *= 1.5;
            }

            var years = watch.LastPlayed is DateTime l ? (ctx.Now - l).TotalDays / 365.0 : 3;
            weight *= 1 + (Math.Min(years, 5) * 0.2);
            reason = watch.IsFavorite ? "Rediscover a favourite" : "Rediscover: " + Ago(watch.LastPlayed, ctx.Now);
        }
        else
        {
            // Watched too recently to rediscover, too long ago to be "recent": cooling down.
            return null;
        }

        if (ctx.PreviousKeys.Contains(key))
        {
            weight *= 0.25;
        }

        return new Candidate
        {
            Item = item,
            Key = key,
            Pool = pool,
            Weight = weight,
            Season = best?.Key,
            SeasonStrength = best?.Value ?? 0,
            SeasonMatches = matches,
            IsNewRelease = isNewRelease,
            Genre = item.Genres.FirstOrDefault(g => !string.IsNullOrWhiteSpace(g)),
            Reason = reason
        };
    }

    /// <summary>
    /// Fills the seasonal quota first, then splits the rest between rediscover and unwatched, backfilling across pools.
    /// No first-listed genre may take more than MaxPerGenrePercent of the list, unless there's nothing else to fill it with.
    /// </summary>
    public static List<PickEntry> Select(List<Candidate> candidates, PluginConfiguration cfg, bool anySeasonActive, Random rng)
    {
        var count = Math.Max(1, cfg.PickCount);
        var cap = cfg.MaxPerGenrePercent is > 0 and < 100
            ? Math.Max(1, (int)Math.Ceiling(count * cfg.MaxPerGenrePercent / 100.0))
            : int.MaxValue;
        var genreCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var taken = new HashSet<string>();
        var picks = new List<PickEntry>();

        void Take(IEnumerable<Candidate> pool, int n, Func<Candidate, double> weight, bool asSeasonal, bool capped)
        {
            var added = 0;
            foreach (var c in Ordered(pool.Where(c => !taken.Contains(c.Key)), weight, rng))
            {
                if (added >= n)
                {
                    break;
                }

                var g = c.Genre ?? string.Empty;
                if (capped && c.Genre is not null && genreCounts.GetValueOrDefault(g) >= cap)
                {
                    continue;
                }

                genreCounts[g] = genreCounts.GetValueOrDefault(g) + 1;
                taken.Add(c.Key);
                added++;
                picks.Add(ToEntry(c, asSeasonal ? c.Season : null));
            }
        }

        // Mild in-season lift for the general picks too.
        double General(Candidate c) => c.Weight * (1 + (c.SeasonStrength * 0.5));

        if (anySeasonActive)
        {
            // Seasonal picks aren't capped (Halloween is allowed to be mostly horror) but they count towards the cap.
            var seasonalQuota = (int)Math.Round(count * Math.Clamp(cfg.SeasonalPercent, 0, 100) / 100.0, MidpointRounding.AwayFromZero);
            Take(candidates.Where(c => c.Season is not null), seasonalQuota, c => c.Weight * c.SeasonStrength * c.Season!.Boost, true, false);
        }

        // New releases (unwatched, released recently) get their own share of what's left.
        var newQuota = (int)Math.Round((count - picks.Count) * Math.Clamp(cfg.NewReleasePercent, 0, 100) / 100.0, MidpointRounding.AwayFromZero);
        Take(candidates.Where(c => c.IsNewRelease), newQuota, General, false, true);

        var remaining = count - picks.Count;
        var rediscoverQuota = (int)Math.Round(remaining * Math.Clamp(cfg.RediscoverPercent, 0, 100) / 100.0, MidpointRounding.AwayFromZero);
        Take(candidates.Where(c => c.Pool == PickKind.Rediscover), rediscoverQuota, General, false, true);
        Take(candidates.Where(c => c.Pool == PickKind.Unwatched), count - picks.Count, General, false, true);

        // Backfill if a pool ran short (small library, or everything watched): first within the cap, then without.
        Take(candidates, count - picks.Count, General, false, true);
        Take(candidates, count - picks.Count, General, false, false);

        // Shuffle so seasonal picks are spread through the playlist.
        return picks.OrderBy(_ => rng.Next()).ToList();
    }

    /// <summary>Picks for one season's own playlist: every candidate that matches it, strongest first by weight.</summary>
    public static List<PickEntry> SelectForSeason(List<Candidate> candidates, SeasonRule season, int count, Random rng)
    {
        return Ordered(
                candidates.Where(c => c.SeasonMatches.ContainsKey(season)),
                c => c.Weight * c.SeasonMatches[season] * season.Boost,
                rng)
            .Take(Math.Max(1, count))
            .Select(c => ToEntry(c, season))
            .ToList();
    }

    /// <summary>One replacement pick, preferring the same kind (and season) as the pick being swapped out.</summary>
    public static PickEntry? PickReplacement(List<Candidate> candidates, PickKind kind, ISet<string> excludeKeys, Random rng, SeasonRule? season = null)
    {
        var pool = candidates.Where(c => !excludeKeys.Contains(c.Key)).ToList();
        if (season is not null)
        {
            var inSeason = pool.Where(c => c.SeasonMatches.ContainsKey(season)).ToList();
            return inSeason.Count == 0 ? null : SelectForSeason(inSeason, season, 1, rng).FirstOrDefault();
        }

        var sameKind = kind switch
        {
            PickKind.Seasonal => pool.Where(c => c.Season is not null).ToList(),
            PickKind.NewRelease => pool.Where(c => c.IsNewRelease).ToList(),
            _ => pool.Where(c => c.Pool == kind).ToList()
        };

        var seasonal = kind == PickKind.Seasonal && sameKind.Count > 0;
        var c = Ordered(sameKind.Count > 0 ? sameKind : pool, x => seasonal ? x.Weight * x.SeasonStrength * x.Season!.Boost : x.Weight, rng).FirstOrDefault();
        return c is null ? null : ToEntry(c, seasonal ? c.Season : null);
    }

    private static PickEntry ToEntry(Candidate c, SeasonRule? asSeason) => new()
    {
        ItemId = c.Item.Id,
        Kind = asSeason is not null ? PickKind.Seasonal : c.IsNewRelease ? PickKind.NewRelease : c.Pool,
        Reason = asSeason is not null ? asSeason.Name + (c.Pool == PickKind.Rediscover ? " rewatch" : " pick") : c.Reason
    };

    /// <summary>Released within NewReleaseDays (by release date, or 1 Jan of the release year when that's all there is).</summary>
    public static bool IsNewRelease(BaseItem item, PluginConfiguration cfg, DateTime now)
    {
        if (cfg.NewReleaseDays <= 0)
        {
            return false;
        }

        var released = item.PremiereDate ?? (item.ProductionYear is int y && y > 1 ? new DateTime(y, 1, 1, 0, 0, 0, DateTimeKind.Utc) : (DateTime?)null);
        return released is DateTime r && r > now.AddDays(-cfg.NewReleaseDays);
    }

    /// <summary>Efraimidis-Spirakis: a random order where heavier candidates tend to come first. Take the first n for a weighted sample.</summary>
    private static IEnumerable<Candidate> Ordered(IEnumerable<Candidate> pool, Func<Candidate, double> weight, Random rng)
    {
        return pool
            .Select(c => (c, key: Math.Pow(rng.NextDouble(), 1.0 / Math.Max(weight(c), 1e-6))))
            .OrderByDescending(x => x.key)
            .Select(x => x.c)
            .ToList();
    }

    /// <summary>One whole-word regex per season covering all its keywords, built once per refresh.</summary>
    public static Dictionary<SeasonRule, Regex?> KeywordPatterns(IEnumerable<SeasonRule> seasons)
    {
        var result = new Dictionary<SeasonRule, Regex?>();
        foreach (var s in seasons)
        {
            var words = s.Keywords.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => Regex.Escape(k.Trim())).ToList();
            result[s] = words.Count == 0
                ? null
                : new Regex(@"\b(?:" + string.Join("|", words) + @")\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        return result;
    }

    /// <summary>The highest-boost season (on, or off-and-hiding) this film strongly matches; ties go to a season that's on.</summary>
    private static SeasonRule? OwningSeason(BaseItem item, string text, PickContext ctx)
    {
        return ctx.Active.Select(s => (Season: s, On: true))
            .Concat(ctx.InactiveHidden.Select(s => (Season: s, On: false)))
            .Where(x => MatchStrength(item, text, x.Season, ctx.Keywords) >= StrongMatch)
            .OrderByDescending(x => x.Season.Boost)
            .ThenByDescending(x => x.On)
            .Select(x => x.Season)
            .FirstOrDefault();
    }

    private static double MatchStrength(BaseItem item, string text, SeasonRule s, IReadOnlyDictionary<SeasonRule, Regex?> keywords)
    {
        if (s.Tags.Length > 0 && item.Tags.Any(t => s.Tags.Contains(t, StringComparer.OrdinalIgnoreCase)))
        {
            return StrongMatch;
        }

        if (keywords.TryGetValue(s, out var pattern) && pattern is not null && pattern.IsMatch(text))
        {
            return StrongMatch;
        }

        if (s.Genres.Length > 0 && item.Genres.Any(g => s.Genres.Contains(g, StringComparer.OrdinalIgnoreCase)))
        {
            return GenreMatch;
        }

        return 0;
    }

    private static string BuildSearchText(BaseItem item) => item.Name + "\n" + item.OriginalTitle + "\n" + item.Overview;

    private static string Ago(DateTime? when, DateTime now)
    {
        if (when is not DateTime w)
        {
            return "watched a while ago";
        }

        var days = (now - w).TotalDays;
        if (days >= 730)
        {
            return $"last watched {(int)(days / 365)} years ago";
        }

        if (days >= 365)
        {
            return "last watched over a year ago";
        }

        return $"last watched {(int)(days / 30)} months ago";
    }

    private static string? NormalizeDir(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return path.TrimEnd('/', '\\') + Path.DirectorySeparatorChar;
    }
}
