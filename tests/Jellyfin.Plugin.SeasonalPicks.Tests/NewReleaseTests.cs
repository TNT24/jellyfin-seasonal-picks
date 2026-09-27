using System;
using System.Linq;
using Jellyfin.Plugin.SeasonalPicks.Configuration;
using Jellyfin.Plugin.SeasonalPicks.Services;
using MediaBrowser.Controller.Entities.Movies;
using Xunit;

namespace Jellyfin.Plugin.SeasonalPicks.Tests;

public class NewReleaseTests
{
    private static readonly DateTime Now = new(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);

    private static Movie Released(string name, DateTime? premiere, int? year = null, string genre = "Drama")
    {
        var m = PickEngineTests.Film(name, "People talk.", new[] { genre });
        m.PremiereDate = premiere;
        m.ProductionYear = year ?? premiere?.Year;
        return m;
    }

    private static PickContext Ctx(PluginConfiguration? cfg = null) => PickContext.Create(cfg ?? new PluginConfiguration(), Now, Now);

    [Fact]
    public void Recent_unwatched_film_is_a_new_release()
    {
        var c = PickEngine.Evaluate(Released("Fresh", Now.AddMonths(-2)), default, Ctx());
        Assert.True(c!.IsNewRelease);
        Assert.Equal("New release (2026)", c.Reason);
    }

    [Fact]
    public void Old_film_is_not_a_new_release()
    {
        Assert.False(PickEngine.Evaluate(Released("Classic", new DateTime(1995, 12, 15)), default, Ctx())!.IsNewRelease);
    }

    [Fact]
    public void Year_only_films_count_from_the_first_of_january()
    {
        Assert.True(PickEngine.IsNewRelease(Released("Y", null, 2026), new PluginConfiguration(), Now));
        Assert.False(PickEngine.IsNewRelease(Released("Y", null, 2025), new PluginConfiguration(), Now)); // 1 Jan 2025 is over a year ago
    }

    [Fact]
    public void Watched_new_release_is_not_offered_as_new()
    {
        var watched = new WatchInfo(true, Now.AddDays(-400), false, false, false);
        var c = PickEngine.Evaluate(Released("Seen", Now.AddMonths(-3)), watched, Ctx());
        Assert.True(c is null || !c.IsNewRelease);
    }

    [Fact]
    public void Turning_it_off_turns_it_off()
    {
        var cfg = new PluginConfiguration { NewReleaseDays = 0 };
        Assert.False(PickEngine.Evaluate(Released("Fresh", Now.AddMonths(-2)), default, Ctx(cfg))!.IsNewRelease);
    }

    [Fact]
    public void New_releases_get_their_share_of_the_list()
    {
        var cfg = new PluginConfiguration { PickCount = 20, NewReleasePercent = 20, MaxPerGenrePercent = 0 };
        var genres = new[] { "Drama", "Comedy", "Action", "Thriller" };
        var films = Enumerable.Range(0, 60).Select(i => Released("Old " + i, new DateTime(2000 + (i % 20), 5, 1), genre: genres[i % 4]))
            .Concat(Enumerable.Range(0, 8).Select(i => Released("New " + i, Now.AddDays(-10 - (i * 20)), genre: genres[i % 4])));
        var candidates = films.Select(f => PickEngine.Evaluate(f, default, Ctx(cfg))).OfType<Candidate>().ToList();

        for (var seed = 0; seed < 10; seed++)
        {
            var picks = PickEngine.Select(candidates, cfg, false, new Random(seed));
            Assert.Equal(20, picks.Count);
            Assert.True(picks.Count(p => p.Kind == PickKind.NewRelease) >= 4, $"seed {seed}");
        }
    }

    [Fact]
    public void A_swapped_out_new_release_is_replaced_by_another_new_release()
    {
        var a = Released("New A", Now.AddDays(-20));
        var b = Released("New B", Now.AddDays(-40));
        var old = Released("Old", new DateTime(1990, 1, 1));
        var candidates = new[] { a, b, old }.Select(f => PickEngine.Evaluate(f, default, Ctx())).OfType<Candidate>().ToList();

        var r = PickEngine.PickReplacement(candidates, PickKind.NewRelease, new System.Collections.Generic.HashSet<string> { candidates[0].Key }, new Random(1));

        Assert.Equal(b.Id, r!.ItemId);
        Assert.Equal(PickKind.NewRelease, r.Kind);
    }
}
