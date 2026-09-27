using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.SeasonalPicks.Configuration;
using Jellyfin.Plugin.SeasonalPicks.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Plugin.SeasonalPicks.Tests;

public class PickEngineTests
{
    private static readonly DateTime July = new(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime October = new(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime December = new(2026, 12, 15, 12, 0, 0, DateTimeKind.Utc);

    internal static Movie Film(string name, string overview = "", string[]? genres = null, string? tmdb = null, float? rating = 7f, int width = 1920, int? year = null)
    {
        var m = new Movie
        {
            Id = Guid.NewGuid(),
            Name = name,
            Overview = overview,
            Genres = genres ?? new[] { "Drama" },
            Tags = Array.Empty<string>(),
            CommunityRating = rating,
            DateCreated = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Width = width,
            ProductionYear = year,
            PremiereDate = year is int y ? new DateTime(y, 6, 1, 0, 0, 0, DateTimeKind.Utc) : null
        };
        if (tmdb is not null)
        {
            m.SetProviderId(MetadataProvider.Tmdb, tmdb);
        }

        return m;
    }

    private static PickContext Ctx(DateTime now, PluginConfiguration? cfg = null, IReadOnlyDictionary<string, SeriesPosition>? series = null, IReadOnlySet<string>? excluded = null)
        => PickContext.Create(cfg ?? new PluginConfiguration(), now, now, excluded, null, series);

    private static readonly WatchInfo Unwatched = default;

    // ---------- seasons ----------

    [Theory]
    [InlineData(10, 1, true)]
    [InlineData(10, 31, true)]
    [InlineData(11, 1, false)]
    public void Halloween_window(int month, int day, bool expected)
    {
        var halloween = SeasonRule.Defaults().First(s => s.Name == "Halloween");
        Assert.Equal(expected, halloween.IsActive(new DateTime(2026, month, day)));
    }

    [Theory]
    [InlineData(12, 25, true)]
    [InlineData(1, 3, true)]
    [InlineData(1, 6, false)]
    [InlineData(12, 19, false)]
    public void Window_that_wraps_the_new_year(int month, int day, bool expected)
    {
        var s = new SeasonRule { StartMonth = 12, StartDay = 20, EndMonth = 1, EndDay = 5 };
        Assert.Equal(expected, s.IsActive(new DateTime(2026, month, day)));
    }

    // ---------- hide out of season (the Christmas Vacation bug) ----------

    [Fact]
    public void Christmas_film_matching_an_active_season_is_still_hidden_in_July()
    {
        var film = Film("National Lampoon's Christmas Vacation", "The Griswolds plan a big family Christmas.", new[] { "Comedy" });
        Assert.Null(PickEngine.Evaluate(film, Unwatched, Ctx(July)));
    }

    [Fact]
    public void Christmas_film_with_ghosts_is_hidden_in_October()
    {
        var film = Film("A Christmas Carol", "Scrooge is visited by three ghosts.", new[] { "Fantasy" });
        Assert.Null(PickEngine.Evaluate(film, Unwatched, Ctx(October)));
    }

    [Fact]
    public void Christmas_film_is_a_seasonal_candidate_in_December()
    {
        var film = Film("National Lampoon's Christmas Vacation", "The Griswolds plan a big family Christmas.", new[] { "Comedy" });
        var c = PickEngine.Evaluate(film, Unwatched, Ctx(December));
        Assert.NotNull(c);
        Assert.Equal("Christmas", c!.Season!.Name);
        Assert.Equal(PickEngine.StrongMatch, c.SeasonStrength);
    }

    [Fact]
    public void Keywords_match_whole_words_only()
    {
        var film = Film("Ghostbusters", "Scientists start a business.", new[] { "Comedy" });
        var c = PickEngine.Evaluate(film, Unwatched, Ctx(October));
        Assert.NotNull(c);
        Assert.Null(c!.Season);
    }

    [Fact]
    public void Horror_genre_is_a_weak_Halloween_match()
    {
        var c = PickEngine.Evaluate(Film("Some Slasher", "A masked killer.", new[] { "Horror" }), Unwatched, Ctx(October));
        Assert.Equal(PickEngine.GenreMatch, c!.SeasonStrength);
    }

    // ---------- watch history ----------

    [Fact]
    public void Recently_watched_is_excluded()
    {
        var w = new WatchInfo(true, July.AddDays(-10), false, false, false);
        Assert.Null(PickEngine.Evaluate(Film("A"), w, Ctx(July)));
    }

    [Fact]
    public void Cooling_down_is_excluded()
    {
        var w = new WatchInfo(true, July.AddDays(-200), false, false, false); // past 60 days, before 365
        Assert.Null(PickEngine.Evaluate(Film("A"), w, Ctx(July)));
    }

    [Fact]
    public void Old_watch_is_a_rediscovery()
    {
        var w = new WatchInfo(true, July.AddDays(-800), false, true, false);
        var c = PickEngine.Evaluate(Film("A"), w, Ctx(July));
        Assert.Equal(PickKind.Rediscover, c!.Pool);
        Assert.Equal("Rediscover a favourite", c.Reason);
    }

    [Fact]
    public void Half_watched_is_left_to_continue_watching()
    {
        Assert.Null(PickEngine.Evaluate(Film("A"), new WatchInfo(false, null, true, false, false), Ctx(July)));
    }

    [Fact]
    public void Hidden_film_is_excluded_by_key_so_other_copies_are_too()
    {
        var copy = Film("A", tmdb: "42");
        Assert.Null(PickEngine.Evaluate(copy, Unwatched, Ctx(July, excluded: new HashSet<string> { "tmdb:42" })));
    }

    [Fact]
    public void Low_rated_film_is_excluded_when_a_minimum_is_set()
    {
        var cfg = new PluginConfiguration { MinCommunityRating = 6 };
        Assert.Null(PickEngine.Evaluate(Film("A", rating: 5.2f), Unwatched, Ctx(July, cfg)));
        Assert.NotNull(PickEngine.Evaluate(Film("B", rating: null), Unwatched, Ctx(July, cfg)));
    }

    // ---------- duplicate copies ----------

    [Fact]
    public void Copies_merge_into_one_film_keeping_the_widest_and_the_watch_history()
    {
        var hd = Film("Heat", tmdb: "949", width: 1920);
        var uhd = Film("Heat", tmdb: "949", width: 3840);
        var other = Film("Collateral", tmdb: "1538");
        var watched = new WatchInfo(true, July.AddDays(-5), false, false, false);

        var merged = PickEngine.MergeCopies(new[] { (hd as BaseItem, watched), (uhd as BaseItem, Unwatched), (other as BaseItem, Unwatched) });

        Assert.Equal(2, merged.Count);
        var heat = merged.Single(m => m.Item.Name == "Heat");
        Assert.Same(uhd, heat.Item);
        Assert.True(heat.Watch.Played); // watched on the HD copy still counts
    }

    [Fact]
    public void Films_without_provider_ids_are_not_merged()
    {
        var merged = PickEngine.MergeCopies(new[] { (Film("A") as BaseItem, Unwatched), (Film("A") as BaseItem, Unwatched) });
        Assert.Equal(2, merged.Count);
    }

    // ---------- collections ----------

    [Fact]
    public void Collection_order_marks_next_and_waiting_films()
    {
        var p1 = Film("Part 1", tmdb: "1", year: 1979);
        var p2 = Film("Part 2", tmdb: "2", year: 1986);
        var p3 = Film("Part 3", tmdb: "3", year: 1992);
        var watched = new WatchInfo(true, July.AddDays(-900), false, false, false);

        // Given out of order: sorting is by release date.
        var positions = PickEngine.SeriesPositions("Alien Collection", new[] { (p3 as BaseItem, Unwatched), (p1 as BaseItem, watched), (p2 as BaseItem, Unwatched) })
            .ToDictionary(x => x.Key, x => x.Position);

        Assert.False(positions.ContainsKey("tmdb:1")); // watched films get no position
        Assert.True(positions["tmdb:2"].IsNext);
        Assert.True(positions["tmdb:3"].EarlierUnwatched);
    }

    [Fact]
    public void Next_in_collection_is_favoured_and_later_parts_wait()
    {
        var p2 = Film("Part 2", tmdb: "2");
        var p3 = Film("Part 3", tmdb: "3");
        var series = new Dictionary<string, SeriesPosition>
        {
            ["tmdb:2"] = new() { CollectionName = "Alien Collection", IsNext = true },
            ["tmdb:3"] = new() { CollectionName = "Alien Collection", EarlierUnwatched = true }
        };
        var ctx = Ctx(July, series: series);

        var next = PickEngine.Evaluate(p2, Unwatched, ctx);
        Assert.Equal("Next in Alien Collection", next!.Reason);
        Assert.Null(PickEngine.Evaluate(p3, Unwatched, ctx));

        // A rewatch of a later part is fine even if an earlier one is unwatched.
        Assert.NotNull(PickEngine.Evaluate(p3, new WatchInfo(true, July.AddDays(-900), false, false, false), ctx));
    }

    // ---------- selection ----------

    private static List<Candidate> Pool(DateTime now, PluginConfiguration cfg, IEnumerable<Movie> films)
        => films.Select(f => PickEngine.Evaluate(f, Unwatched, Ctx(now, cfg))).OfType<Candidate>().ToList();

    [Fact]
    public void Genre_cap_spreads_the_list_when_there_is_variety()
    {
        var cfg = new PluginConfiguration { PickCount = 10, MaxPerGenrePercent = 30 };
        var genres = new[] { "Drama", "Comedy", "Thriller", "Action", "Sci-Fi" };
        var films = Enumerable.Range(0, 50).Select(i => Film("F" + i, genres: new[] { genres[i % 5] }));
        var candidates = Pool(July, cfg, films);

        for (var run = 0; run < 20; run++)
        {
            var picks = PickEngine.Select(candidates, cfg, false, new Random(run));
            Assert.Equal(10, picks.Count);
            var byGenre = picks.GroupBy(p => candidates.First(c => c.Item.Id == p.ItemId).Genre);
            Assert.All(byGenre, g => Assert.True(g.Count() <= 3, $"{g.Key}: {g.Count()}"));
        }
    }

    [Fact]
    public void Genre_cap_never_leaves_the_list_short()
    {
        var cfg = new PluginConfiguration { PickCount = 10, MaxPerGenrePercent = 30 };
        var candidates = Pool(July, cfg, Enumerable.Range(0, 20).Select(i => Film("D" + i, genres: new[] { "Drama" })));
        Assert.Equal(10, PickEngine.Select(candidates, cfg, false, new Random(1)).Count);
    }

    [Fact]
    public void Picks_are_unique_and_the_seasonal_quota_is_filled()
    {
        var cfg = new PluginConfiguration { PickCount = 10, SeasonalPercent = 40 };
        var horror = Enumerable.Range(0, 10).Select(i => Film("Haunted House " + i, "A haunted house.", new[] { "Horror" }));
        var other = Enumerable.Range(0, 30).Select(i => Film("Drama " + i, "People talk.", new[] { i % 2 == 0 ? "Drama" : "Comedy" }));
        var candidates = Pool(October, cfg, horror.Concat(other));

        var picks = PickEngine.Select(candidates, cfg, true, new Random(3));

        Assert.Equal(10, picks.Count);
        Assert.Equal(10, picks.Select(p => p.ItemId).Distinct().Count());
        Assert.True(picks.Count(p => p.Kind == PickKind.Seasonal) >= 4);
        Assert.All(picks.Where(p => p.Kind == PickKind.Seasonal), p => Assert.Equal("Halloween pick", p.Reason));
    }

    [Fact]
    public void Season_playlist_only_holds_that_seasons_films()
    {
        var cfg = new PluginConfiguration();
        var films = new[]
        {
            Film("Haunted", "A haunted house.", new[] { "Horror" }),
            Film("Witches", "Three witches.", new[] { "Comedy" }),
            Film("Talk", "People talk.", new[] { "Drama" })
        };
        var candidates = Pool(October, cfg, films);
        var halloween = cfg.Seasons.First(s => s.Name == "Halloween");

        var list = PickEngine.SelectForSeason(candidates, halloween, 20, new Random(1));

        Assert.Equal(2, list.Count);
        Assert.DoesNotContain(list, p => p.ItemId == films[2].Id);
    }

    [Fact]
    public void Replacement_stays_in_season_and_skips_current_picks()
    {
        var cfg = new PluginConfiguration();
        var a = Film("Haunted", "A haunted house.", tmdb: "10");
        var b = Film("Zombie Town", "Zombies attack.", tmdb: "11");
        var c = Film("Talk", "People talk.", tmdb: "12");
        var candidates = Pool(October, cfg, new[] { a, b, c });
        var halloween = cfg.Seasons.First(s => s.Name == "Halloween");

        var r = PickEngine.PickReplacement(candidates, PickKind.Seasonal, new HashSet<string> { "tmdb:10" }, new Random(1), halloween);
        Assert.Equal(b.Id, r!.ItemId);

        Assert.Null(PickEngine.PickReplacement(candidates, PickKind.Seasonal, new HashSet<string> { "tmdb:10", "tmdb:11" }, new Random(1), halloween));
    }

    // ---------- names, events ----------

    [Theory]
    [InlineData("{season} Picks", "Halloween Picks")]
    [InlineData("Best of {season}", "Best of Halloween")]
    [InlineData("Picks", "Halloween Picks")]
    [InlineData("", "Halloween Picks")]
    public void Season_playlist_name(string template, string expected)
    {
        Assert.Equal(expected, PicksService.SeasonPlaylistName(new PluginConfiguration { SeasonPlaylistName = template }, "Halloween"));
    }

    [Fact]
    public void Only_finished_or_marked_played_movies_count_as_watched()
    {
        var movie = new Movie();
        Assert.True(WatchedListener.IsWatchedEvent(UserDataSaveReason.PlaybackFinished, true, movie));
        Assert.True(WatchedListener.IsWatchedEvent(UserDataSaveReason.TogglePlayed, true, movie));
        Assert.False(WatchedListener.IsWatchedEvent(UserDataSaveReason.TogglePlayed, false, movie)); // marked unplayed
        Assert.False(WatchedListener.IsWatchedEvent(UserDataSaveReason.PlaybackProgress, true, movie));
        Assert.False(WatchedListener.IsWatchedEvent(UserDataSaveReason.Import, true, movie));
        Assert.False(WatchedListener.IsWatchedEvent(UserDataSaveReason.PlaybackFinished, true, new Folder()));
    }
}
