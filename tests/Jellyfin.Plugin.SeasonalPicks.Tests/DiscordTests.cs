using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Jellyfin.Plugin.SeasonalPicks.Configuration;
using Jellyfin.Plugin.SeasonalPicks.Services;
using Xunit;

namespace Jellyfin.Plugin.SeasonalPicks.Tests;

public class DiscordTests
{
    private static readonly DiscordFilm Film = new(Guid.Parse("11111111-2222-3333-4444-555555555555"), "Hocus Pocus", 1993, "Halloween favourite");

    [Fact]
    public void Last_day_of_a_season_and_of_a_one_day_event()
    {
        var halloween = new SeasonRule { Name = "Halloween", StartMonth = 10, StartDay = 1, EndMonth = 10, EndDay = 31 };
        Assert.Equal(new DateTime(2026, 10, 31), DiscordService.LastDay(halloween, new DateTime(2026, 10, 1)));

        var starWars = SeasonRule.Defaults().Single(s => s.Name == "Star Wars Day"); // 4 and 5 May
        Assert.Equal(new DateTime(2026, 5, 5), DiscordService.LastDay(starWars, new DateTime(2026, 5, 4)));

        var oneDay = new SeasonRule { Name = "Pi Day", StartMonth = 3, StartDay = 14, EndMonth = 3, EndDay = 14 };
        var day = new DateTime(2026, 3, 14);
        Assert.Equal(day, DiscordService.LastDay(oneDay, day));
    }

    [Fact]
    public void Last_day_of_a_season_that_wraps_the_new_year()
    {
        var winter = new SeasonRule { Name = "Winter", StartMonth = 12, StartDay = 20, EndMonth = 1, EndDay = 5 };
        Assert.Equal(new DateTime(2027, 1, 5), DiscordService.LastDay(winter, new DateTime(2026, 12, 20)));
    }

    [Theory]
    [InlineData("#FF7518", 0xFF7518)]
    [InlineData("3a6ea5", 0x3A6EA5)]
    [InlineData("orange", 0x00A4DC)]
    [InlineData(null, 0x00A4DC)]
    public void Season_colour_becomes_a_discord_colour(string? hex, int expected)
    {
        Assert.Equal(expected, DiscordMessages.Color(hex));
    }

    [Fact]
    public void Season_start_says_until_when_and_who_got_a_playlist()
    {
        var msg = DiscordMessages.SeasonStarted("Halloween", "#FF7518", new DateTime(2026, 10, 1), new DateTime(2026, 10, 31), "Halloween Picks", 3, new[] { Film }, "https://jf.example.com/");
        var content = msg["content"]!.GetValue<string>();
        Assert.Contains("Halloween is here!", content);
        Assert.Contains("Until 31 October", content);
        Assert.Contains("3 users have a new **Halloween Picks** playlist", content);
        Assert.Contains("(https://jf.example.com/web/#/details?id=11111111222233334444555555555555)", msg["embeds"]![0]!["description"]!.GetValue<string>());
        Assert.Equal(0xFF7518, msg["embeds"]![0]!["color"]!.GetValue<int>());
    }

    [Fact]
    public void One_day_event_and_no_films()
    {
        var day = new DateTime(2026, 5, 4);
        var msg = DiscordMessages.SeasonStarted("Star Wars Day", "#FFE81F", day, day, null, 0, Array.Empty<DiscordFilm>(), string.Empty);
        Assert.Equal("🎉 **It's Star Wars Day!**", msg["content"]!.GetValue<string>());
        Assert.Null(msg["embeds"]);
    }

    [Fact]
    public void Daily_picks_get_a_card_each_with_a_poster()
    {
        var films = Enumerable.Range(0, 12).Select(i => Film with { Name = "Film " + i }).ToList();
        var msg = DiscordMessages.DailyPicks("mark", new[] { "Halloween" }, films, "#FF7518", "https://jf.example.com");
        var embeds = msg["embeds"]!.AsArray();
        Assert.Equal(10, embeds.Count);
        Assert.Equal("Film 0 (1993)", embeds[0]!["title"]!.GetValue<string>());
        Assert.EndsWith("/Images/Primary?maxHeight=300", embeds[0]!["thumbnail"]!["url"]!.GetValue<string>());
        Assert.Equal("🍿 **Today's picks for mark** · Halloween", msg["content"]!.GetValue<string>());
    }

    [Fact]
    public void Never_pings_anyone_and_escapes_markdown()
    {
        var msg = DiscordMessages.DailyPicks("@everyone", Array.Empty<string>(), new[] { Film with { Reason = "*bold*" } }, "#000000", string.Empty);
        Assert.Empty(msg["allowed_mentions"]!["parse"]!.AsArray());
        Assert.Equal("\\*bold\\*", msg["embeds"]![0]!["description"]!.GetValue<string>());
        Assert.Null(msg["embeds"]![0]!["url"]);
    }

    [Fact]
    public void Discord_settings_survive_an_xml_round_trip()
    {
        var cfg = new PluginConfiguration { DiscordWebhookUrl = "https://discord.com/api/webhooks/1/x", DiscordPicksUserId = "abc", DiscordPicksCount = 7, DiscordAnnounceSeasons = false };
        var xml = new XmlSerializer(typeof(PluginConfiguration));
        using var ms = new MemoryStream();
        xml.Serialize(ms, cfg);
        ms.Position = 0;
        var back = (PluginConfiguration)xml.Deserialize(ms)!;
        Assert.Equal(cfg.DiscordWebhookUrl, back.DiscordWebhookUrl);
        Assert.Equal("abc", back.DiscordPicksUserId);
        Assert.Equal(7, back.DiscordPicksCount);
        Assert.False(back.DiscordAnnounceSeasons);
    }

    [Fact]
    public async Task A_failed_post_keeps_what_was_already_posted()
    {
        var state = new DiscordState();
        var outbox = new[]
        {
            new DiscordOutgoing(DiscordMessages.Test(), s => s.AnnouncedSeasons.Add("Halloween")),
            new DiscordOutgoing(DiscordMessages.Test(), s => s.PicksPostedOn = "2026-11-03"),
        };
        var saves = 0;
        var calls = 0;
        Task Send(JsonObject m, CancellationToken c) => ++calls == 2 ? throw new HttpRequestException("down") : Task.CompletedTask;

        await Assert.ThrowsAsync<HttpRequestException>(() => DiscordService.SendEachAsync(Send, outbox, state, _ => saves++, CancellationToken.None));
        Assert.Equal(new[] { "Halloween" }, state.AnnouncedSeasons);
        Assert.Null(state.PicksPostedOn);
        Assert.Equal(1, saves);
    }

    [Fact]
    public void Renamed_or_deleted_seasons_are_not_said_to_be_over()
    {
        var halloween = new SeasonRule { Name = "Halloween" };
        var cfg = new PluginConfiguration { Seasons = new[] { halloween, new SeasonRule { Name = "Christmas" } } };
        var state = new DiscordState { AnnouncedSeasons = new List<string> { "Halloween", "Spooky Season", "Christmas" } };

        var (starting, ending) = DiscordService.SeasonChanges(cfg, state, new[] { halloween });
        Assert.Empty(starting);
        Assert.Equal(new[] { "Christmas" }, ending);
    }

    [Fact]
    public void Season_end_mentions_playlists_only_when_there_were_some()
    {
        Assert.Contains("playlists have been removed", DiscordMessages.SeasonEnded("Halloween", true)["content"]!.GetValue<string>());
        Assert.Contains("See you next time", DiscordMessages.SeasonEnded("Halloween", false)["content"]!.GetValue<string>());
    }

    [Fact]
    public void Ten_long_cards_stay_under_discords_total_limit()
    {
        var films = Enumerable.Range(0, 10)
            .Select(i => Film with { Name = new string('*', 400) + i, Reason = new string('_', 400) })
            .ToList();
        var msg = DiscordMessages.DailyPicks("mark", Array.Empty<string>(), films, "#FF7518", string.Empty);
        var embeds = msg["embeds"]!.AsArray().Select(e => e!.AsObject()).ToList();

        // Before 1.5.1 these came to about 8500 characters and Discord refused the message. Now they all fit.
        Assert.Equal(10, embeds.Count);
        Assert.True(embeds.Sum(DiscordMessages.EmbedTextLength) <= DiscordMessages.MaxEmbedText);
        Assert.All(embeds, e =>
        {
            Assert.True(e["title"]!.GetValue<string>().Length <= 256);
            Assert.True(e["description"]!.GetValue<string>().Length <= 301);
        });
    }

    [Theory]
    [InlineData("a*b", 10, @"a\*b")]
    [InlineData("abcdef", 3, "abc…")]
    [InlineData("ab*cd", 3, "ab…")] // cutting "ab\*cd" to "ab\" would escape the "…"
    [InlineData("a**b", 4, @"a\*…")]
    [InlineData(@"a\b", 3, @"a\\…")] // a real backslash, escaped, is kept whole
    public void Escaped_text_is_cut_without_a_lone_backslash(string text, int length, string expected)
    {
        Assert.Equal(expected, DiscordMessages.EscapeAndTruncate(text, length));
    }

    [Fact]
    public void Film_titles_are_escaped()
    {
        var msg = DiscordMessages.DailyPicks("mark", Array.Empty<string>(), new[] { Film with { Name = "*batteries not included", Year = null } }, "#000000", string.Empty);
        Assert.Equal(@"\*batteries not included", msg["embeds"]![0]!["title"]!.GetValue<string>());
    }

    [Theory]
    [InlineData(3600, 60)]
    [InlineData(0, 0.5)]
    [InlineData(2, 2)]
    public async Task Discords_wait_is_kept_between_half_a_second_and_a_minute(int headerSeconds, double expected)
    {
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(headerSeconds));
        Assert.Equal(TimeSpan.FromSeconds(expected), await DiscordNotifier.RetryAfterAsync(response, CancellationToken.None));
    }

    [Fact]
    public async Task Discords_wait_from_the_body_is_kept_to_a_minute()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent("{\"retry_after\": 9000}") };
        Assert.Equal(TimeSpan.FromSeconds(60), await DiscordNotifier.RetryAfterAsync(response, CancellationToken.None));
    }

    [Theory]
    [InlineData("https://discord.com/api/webhooks/1/x", true)]
    [InlineData("https://ptb.discord.com/api/webhooks/1/x", true)]
    [InlineData("https://discordapp.com/api/webhooks/1/x", true)]
    [InlineData("https://ptb.discordapp.com/api/webhooks/1/x", false)]
    [InlineData("http://discord.com/api/webhooks/1/x", false)]
    [InlineData("https://discord.com.evil.example/api/webhooks/1/x", false)]
    public void Only_discord_webhooks_are_accepted(string url, bool ok)
    {
        Assert.Equal(ok, DiscordNotifier.IsWebhookUrl(url));
    }
}
