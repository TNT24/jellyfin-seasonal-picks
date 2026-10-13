using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Jellyfin.Plugin.SeasonalPicks.Configuration;
using Jellyfin.Plugin.SeasonalPicks.Services;
using Xunit;

namespace Jellyfin.Plugin.SeasonalPicks.Tests;

public class SeasonTests
{
    [Theory]
    [InlineData(2024, 3, 31)]
    [InlineData(2025, 4, 20)]
    [InlineData(2026, 4, 5)]
    [InlineData(2027, 3, 28)]
    [InlineData(2038, 4, 25)] // latest possible
    [InlineData(2285, 3, 22)] // earliest possible
    public void Easter_sunday(int year, int month, int day)
    {
        Assert.Equal(new DateTime(year, month, day), SeasonRule.EasterSunday(year));
    }

    [Theory]
    [InlineData(2026, 3, 21, false)] // Easter 2026 is Apr 5: window Mar 22 - Apr 6
    [InlineData(2026, 3, 22, true)]
    [InlineData(2026, 4, 6, true)]
    [InlineData(2026, 4, 7, false)]
    [InlineData(2027, 3, 14, true)]  // Easter 2027 is Mar 28: window Mar 14 - Mar 29
    [InlineData(2027, 4, 5, false)]
    public void Easter_window_follows_the_real_date(int y, int m, int d, bool expected)
    {
        var easter = SeasonRule.Defaults().Single(s => s.Name == "Easter");
        Assert.Equal(expected, easter.IsActive(new DateTime(y, m, d)));
    }

    private static readonly string[] FilmDays =
        { "Star Wars Day", "Ghostbusters Day", "Back to Hogwarts", "St Patrick's Day", "Bonfire Night", "Pancake Day", "Chinese New Year" };

    [Fact]
    public void Holidays_hide_their_films_out_of_season_but_film_days_do_not()
    {
        foreach (var s in SeasonRule.Defaults())
        {
            Assert.Equal(!FilmDays.Contains(s.Name), s.HideOutOfSeason);
        }
    }

    [Fact]
    public void Summer_is_gone_from_the_defaults()
    {
        Assert.DoesNotContain(SeasonRule.Defaults(), s => s.Name == "Summer");
    }

    [Theory]
    [InlineData(2026, 12, 31, true)]
    [InlineData(2027, 1, 2, true)]
    [InlineData(2027, 1, 3, false)]
    [InlineData(2026, 12, 26, false)]
    public void New_year_runs_over_the_turn_of_the_year(int y, int m, int d, bool expected)
    {
        Assert.Equal(expected, SeasonRule.Defaults().Single(s => s.Name == "New Year").IsActive(new DateTime(y, m, d)));
    }

    [Theory]
    [InlineData("Halloween", 2026, 10, 1, 0)]
    [InlineData("Halloween", 2026, 10, 7, 0)]
    [InlineData("Halloween", 2026, 10, 8, 1)]
    [InlineData("Halloween", 2026, 10, 31, 4)]
    [InlineData("Halloween", 2026, 11, 8, 0)]   // not active
    [InlineData("Christmas", 2026, 12, 25, 5)]
    [InlineData("New Year", 2027, 1, 2, 0)]     // opened on Dec 27 the year before
    [InlineData("Easter", 2027, 3, 21, 1)]      // Easter Sunday 2027 is Mar 28, so the window opened on Mar 14
    public void Week_of_season_counts_from_the_window_opening(string name, int y, int m, int d, int expected)
    {
        Assert.Equal(expected, SeasonRule.Defaults().Single(s => s.Name == name).WeekOfSeason(new DateTime(y, m, d)));
    }

    [Fact]
    public void Star_wars_is_pushed_on_may_the_fourth_and_still_offered_the_rest_of_the_year()
    {
        var film = PickEngineTests.Film("Star Wars: A New Hope", "Luke Skywalker joins the Rebellion.", new[] { "Science Fiction" }, tmdb: "11");
        var onTheDay = PickEngine.Evaluate(film, default, Ctx(new DateTime(2026, 5, 4)));
        Assert.Equal("Star Wars Day", onTheDay!.Season!.Name);

        var inMarch = PickEngine.Evaluate(film, default, Ctx(new DateTime(2026, 3, 1)));
        Assert.NotNull(inMarch); // not hidden
        Assert.Null(inMarch!.Season);
    }

    [Fact]
    public void On_its_day_a_film_day_fills_the_seasonal_share()
    {
        var cfg = new PluginConfiguration { PickCount = 10, SeasonalPercent = 40 };
        var day = new DateTime(2026, 5, 4);
        var films = Enumerable.Range(0, 6).Select(i => PickEngineTests.Film("Star Wars Episode " + i, "The Jedi fight on.", new[] { "Science Fiction" }))
            .Concat(Enumerable.Range(0, 30).Select(i => PickEngineTests.Film("Drama " + i, "People talk.", new[] { i % 2 == 0 ? "Drama" : "Comedy" })));
        var candidates = films.Select(f => PickEngine.Evaluate(f, default, PickContext.Create(cfg, day, day))).OfType<Candidate>().ToList();

        var picks = PickEngine.Select(candidates, cfg, true, new Random(4));

        Assert.True(picks.Count(p => p.Reason == "Star Wars Day pick") >= 4);
    }

    // ---------- belongs to the strongest season ----------

    private static PickContext Ctx(DateTime d) => PickContext.Create(new PluginConfiguration(), d, d);

    [Fact]
    public void Christmas_vacation_is_a_christmas_film_in_December()
    {
        var film = PickEngineTests.Film("National Lampoon's Christmas Vacation", "The Griswolds plan a big family Christmas.", new[] { "Comedy" });
        var c = PickEngine.Evaluate(film, default, Ctx(new DateTime(2026, 12, 10)));
        Assert.NotNull(c);
        Assert.Equal("Christmas", c!.Season!.Name);
    }

    [Fact]
    public void Halloween_films_are_hidden_in_July()
    {
        var film = PickEngineTests.Film("Hocus Pocus", "Three witches return to Salem.", new[] { "Comedy", "Family" });
        Assert.Null(PickEngine.Evaluate(film, default, Ctx(new DateTime(2026, 7, 10))));
        Assert.NotNull(PickEngine.Evaluate(film, default, Ctx(new DateTime(2026, 10, 10))));
    }

    [Fact]
    public void Plain_horror_is_not_hidden_out_of_October()
    {
        // Genre-only matches are weak: they boost in season but never hide.
        var film = PickEngineTests.Film("Some Slasher", "A masked killer stalks a town.", new[] { "Horror" });
        Assert.NotNull(PickEngine.Evaluate(film, default, Ctx(new DateTime(2026, 7, 10))));
    }

    [Fact]
    public void Easter_films_are_picked_around_easter_and_hidden_otherwise()
    {
        var film = PickEngineTests.Film("Hop", "The son of the Easter Bunny heads to Hollywood.", new[] { "Comedy" });
        var inSeason = PickEngine.Evaluate(film, default, Ctx(new DateTime(2026, 4, 1)));
        Assert.Equal("Easter", inSeason!.Season!.Name);
        Assert.Null(PickEngine.Evaluate(film, default, Ctx(new DateTime(2026, 9, 1))));
    }

    [Theory]
    [InlineData("Home Alone", "An eight-year-old is left behind.", "Christmas")]
    [InlineData("Beetlejuice", "A recently deceased couple.", "Halloween")]
    [InlineData("Ghostbusters II", "The team faces a river of slime.", "Ghostbusters Day")]
    [InlineData("Harry Potter and the Philosopher's Stone", "A boy learns he is a wizard.", "Back to Hogwarts")]
    public void Famous_titles_are_recognised(string title, string overview, string season)
    {
        var dates = new System.Collections.Generic.Dictionary<string, DateTime>
        {
            ["Christmas"] = new(2026, 12, 10),
            ["Halloween"] = new(2026, 10, 10),
            ["Back to Hogwarts"] = new(2026, 9, 1),
            ["Ghostbusters Day"] = new(2026, 6, 8)
        };
        var c = PickEngine.Evaluate(PickEngineTests.Film(title, overview, new[] { "Drama" }), default, Ctx(dates[season]));
        Assert.Equal(season, c!.Season!.Name);
    }

    // ---------- settings saved by older versions ----------

    [Fact]
    public void Old_settings_get_easter_hiding_and_the_longer_word_lists_once()
    {
        // A server's saved settings from 1.0.0: four seasons, only Christmas hiding, the short lists, plus a user keyword.
        var old = new PluginConfiguration
        {
            Seasons = new[]
            {
                new SeasonRule { Name = "Halloween", HideOutOfSeason = false, Keywords = new[] { "halloween", "my own word" } },
                new SeasonRule { Name = "Christmas", HideOutOfSeason = true, Keywords = new[] { "christmas" } },
                new SeasonRule { Name = "Valentine's", HideOutOfSeason = false },
                new SeasonRule { Name = "Summer", HideOutOfSeason = false },
                new SeasonRule { Name = "My Birthday", HideOutOfSeason = false, Keywords = new[] { "cake" } }
            }
        };
        old = RoundTrip(old); // as the server would load it

        Assert.True(old.MigrateToLatest());

        // 4 kept (Summer removed) + Easter + New Year + 3 film days + St Patrick's Day + Bonfire Night + Pancake Day + Chinese New Year.
        Assert.Equal(13, old.Seasons.Length);
        Assert.DoesNotContain(old.Seasons, s => s.Name == "Summer");
        Assert.Contains(old.Seasons, s => s.Name == "Easter" && s.IsEasterBased);
        Assert.Contains(old.Seasons, s => s.Name == "New Year");
        Assert.All(old.Seasons, s => Assert.Equal(!FilmDays.Contains(s.Name), s.HideOutOfSeason));
        var halloween = old.Seasons.Single(s => s.Name == "Halloween");
        Assert.Contains("my own word", halloween.Keywords);
        Assert.Contains("hocus pocus", halloween.Keywords);
        Assert.Equal(halloween.Keywords.Length, halloween.Keywords.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(new[] { "cake" }, old.Seasons.Single(s => s.Name == "My Birthday").Keywords);

        // Second load: nothing changes, nothing duplicates.
        var again = RoundTrip(old);
        Assert.False(again.MigrateToLatest());
        Assert.Equal(13, again.Seasons.Length);
        Assert.Contains(again.Seasons, s => s.Name == "Chinese New Year" && s.IsLunarBased); // lunar dates survive saving
    }

    [Fact]
    public void A_season_deleted_earlier_is_not_brought_back()
    {
        // Settings already at v3 where the user had removed Valentine's.
        var cfg = new PluginConfiguration
        {
            ConfigVersion = 3,
            Seasons = SeasonRule.Defaults().Where(s => s.Name is "Halloween" or "Christmas" or "Easter").ToArray()
        };

        cfg.MigrateToLatest();

        Assert.DoesNotContain(cfg.Seasons, s => s.Name == "Valentine's");
        Assert.Contains(cfg.Seasons, s => s.Name == "Star Wars Day");
    }

    [Fact]
    public void Version_1_0_8_settings_lose_the_removed_days_and_get_the_new_designs()
    {
        var cfg = new PluginConfiguration
        {
            ConfigVersion = 5,
            Seasons = new[]
            {
                new SeasonRule { Name = "Star Wars Day", Theme = "Space" },
                new SeasonRule { Name = "Ghostbusters Day", Theme = "Hearts" }, // chosen by the user: kept
                new SeasonRule { Name = "Mario Day" },
                new SeasonRule { Name = "Star Trek Day" },
                new SeasonRule { Name = "Back to the Future Day" },
                new SeasonRule { Name = "Groundhog Day" },
                new SeasonRule { Name = "Hobbit Day" },
                new SeasonRule { Name = "Bonfire Night", Theme = "Fireworks" }
            }
        };

        Assert.True(cfg.MigrateToLatest());

        Assert.Equal(new[] { "Star Wars Day", "Ghostbusters Day", "Bonfire Night", "Pancake Day", "Chinese New Year" }, cfg.Seasons.Select(s => s.Name));
        Assert.Equal("Lightsabers", cfg.Seasons[0].Theme);
        Assert.Equal("Hearts", cfg.Seasons[1].Theme);
        Assert.Equal("Bonfire", cfg.Seasons[2].Theme);
    }

    [Fact]
    public void Version_1_2_settings_get_pancake_day_and_chinese_new_year_once()
    {
        var cfg = new PluginConfiguration { ConfigVersion = 6, Seasons = new[] { new SeasonRule { Name = "Halloween" } } };
        Assert.True(cfg.MigrateToLatest());
        Assert.Equal(new[] { "Halloween", "Pancake Day", "Chinese New Year" }, cfg.Seasons.Select(s => s.Name));

        // Deleted afterwards: not brought back.
        cfg.Seasons = cfg.Seasons.Take(1).ToArray();
        Assert.False(cfg.MigrateToLatest());
        Assert.Single(cfg.Seasons);
    }

    [Theory]
    [InlineData(2024, 2, 10)]
    [InlineData(2025, 1, 29)]
    [InlineData(2026, 2, 17)]
    [InlineData(2027, 2, 6)]
    [InlineData(2028, 1, 26)]
    public void Lunar_new_year_is_on_the_right_day(int year, int month, int day)
    {
        Assert.Equal(new DateTime(year, month, day), SeasonRule.LunarNewYear(year));
    }

    [Theory]
    [InlineData(2027, 2, 6, true, 0)]    // New Year's Day
    [InlineData(2027, 2, 13, true, 1)]   // second week
    [InlineData(2027, 2, 20, true, 2)]   // Lantern Festival, the last day
    [InlineData(2027, 2, 21, false, 0)]
    [InlineData(2027, 2, 5, false, 0)]
    public void Chinese_new_year_runs_to_the_lantern_festival(int y, int m, int d, bool active, int week)
    {
        var season = SeasonRule.Defaults().Single(s => s.Name == "Chinese New Year");
        Assert.Equal(active, season.IsActive(new DateTime(y, m, d)));
        Assert.Equal(week, season.WeekOfSeason(new DateTime(y, m, d)));
    }

    [Theory]
    [InlineData(2026, 2, 17, true)]   // Easter 2026 is 5 April
    [InlineData(2026, 2, 16, false)]
    [InlineData(2027, 2, 9, true)]    // Easter 2027 is 28 March
    public void Pancake_day_is_shrove_tuesday(int y, int m, int d, bool expected)
    {
        Assert.Equal(expected, SeasonRule.Defaults().Single(s => s.Name == "Pancake Day").IsActive(new DateTime(y, m, d)));
    }

    [Fact]
    public void A_fresh_install_keeps_film_days_unhidden()
    {
        var cfg = new PluginConfiguration(); // ConfigVersion 0, default seasons
        cfg.MigrateToLatest();
        Assert.False(cfg.Seasons.Single(s => s.Name == "Star Wars Day").HideOutOfSeason);
        Assert.True(cfg.Seasons.Single(s => s.Name == "Christmas").HideOutOfSeason);
        Assert.Equal(SeasonRule.Defaults().Length, cfg.Seasons.Length);
    }

    [Theory]
    [InlineData("For You", "Suggested")]      // old default is renamed
    [InlineData("for you ", "Suggested")]
    [InlineData("", "Suggested")]
    [InlineData("Movie Night", "Movie Night")] // a name the user chose is kept
    public void Old_default_name_becomes_suggested(string saved, string expected)
    {
        var cfg = new PluginConfiguration { PlaylistName = saved, ConfigVersion = 2 };
        Assert.True(cfg.MigrateToLatest());
        Assert.Equal(expected, cfg.PlaylistName);
    }

    [Fact]
    public void Easter_dates_survive_being_saved()
    {
        var back = RoundTrip(new PluginConfiguration());
        var easter = back.Seasons.Single(s => s.Name == "Easter");
        Assert.Equal(-14, easter.StartEasterOffset);
        Assert.Equal(1, easter.EndEasterOffset);
        Assert.Null(back.Seasons.Single(s => s.Name == "Christmas").StartEasterOffset);
    }

    private static PluginConfiguration RoundTrip(PluginConfiguration cfg)
    {
        var xml = new XmlSerializer(typeof(PluginConfiguration));
        using var ms = new MemoryStream();
        xml.Serialize(ms, cfg);
        ms.Position = 0;
        return (PluginConfiguration)xml.Deserialize(ms)!;
    }
}
