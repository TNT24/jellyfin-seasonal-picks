using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Jellyfin.Plugin.SeasonalPicks.Configuration;
using Jellyfin.Plugin.SeasonalPicks.Services;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;
using Xunit;

namespace Jellyfin.Plugin.SeasonalPicks.Tests;

public class PersistenceTests
{
    [Fact]
    public void Config_survives_an_xml_round_trip_without_duplicating_seasons()
    {
        // Jellyfin stores plugin settings with XmlSerializer.
        var cfg = new PluginConfiguration
        {
            MaxPerGenrePercent = 25,
            CollectionAware = false,
            SeasonPlaylistName = "Best of {season}",
            ExcludeTags = new[] { "kids" }
        };
        var xml = new XmlSerializer(typeof(PluginConfiguration));
        using var ms = new MemoryStream();
        xml.Serialize(ms, cfg);
        ms.Position = 0;

        var back = (PluginConfiguration)xml.Deserialize(ms)!;

        Assert.Equal(SeasonRule.Defaults().Length, back.Seasons.Length);
        Assert.Equal(25, back.MaxPerGenrePercent);
        Assert.False(back.CollectionAware);
        Assert.Equal("Best of {season}", back.SeasonPlaylistName);
        Assert.Equal(new[] { "kids" }, back.ExcludeTags);
    }

    [Fact]
    public void Old_config_files_get_the_new_defaults()
    {
        const string old = "<?xml version=\"1.0\"?><PluginConfiguration xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><PickCount>12</PickCount></PluginConfiguration>";
        var back = (PluginConfiguration)new XmlSerializer(typeof(PluginConfiguration)).Deserialize(new StringReader(old))!;
        Assert.Equal(12, back.PickCount);
        Assert.Equal(30, back.MaxPerGenrePercent);
        Assert.True(back.ReplaceWatchedPicks);
        Assert.True(back.EnableSeasonPlaylists);
    }

    [Fact]
    public void Pick_state_round_trips_including_season_lists()
    {
        var store = new PickStore(NullLogger<PickStore>.Instance);
        var user = Guid.NewGuid();
        var item = Guid.NewGuid();
        var playlist = Guid.NewGuid();
        var state = new UserPickState
        {
            GeneratedAt = DateTime.UtcNow,
            Picks = new List<PickEntry> { new() { ItemId = item, Kind = PickKind.Seasonal, Reason = "Halloween pick" } },
            Snoozed = new Dictionary<Guid, DateTime> { [item] = DateTime.UtcNow.AddDays(30) },
            SeasonPicks = new Dictionary<string, List<PickEntry>> { ["Halloween"] = new() { new() { ItemId = item, Kind = PickKind.Seasonal } } },
            SeasonPlaylistIds = new Dictionary<string, Guid> { ["Halloween"] = playlist }
        };

        store.Save(user, state);
        var back = store.Load(user);

        Assert.Equal(item, back.Picks.Single().ItemId);
        Assert.Equal(PickKind.Seasonal, back.Picks.Single().Kind);
        Assert.True(back.Snoozed.ContainsKey(item));
        Assert.Equal(item, back.SeasonPicks["Halloween"].Single().ItemId);
        Assert.Equal(playlist, back.SeasonPlaylistIds["Halloween"]);
    }

    [Fact]
    public void State_files_from_before_season_playlists_still_load()
    {
        var store = new PickStore(NullLogger<PickStore>.Instance);
        var user = Guid.NewGuid();
        store.Save(user, new UserPickState());
        var dir = Path.Combine(AppContext.BaseDirectory, "SeasonalPicks", "users");
        File.WriteAllText(Path.Combine(dir, user.ToString("N") + ".json"), "{\"Picks\":[],\"Hidden\":[],\"PlaylistId\":null}");

        var back = store.Load(user);

        Assert.NotNull(back.SeasonPicks);
        Assert.NotNull(back.SeasonPlaylistIds);
    }

    [Theory]
    [InlineData(false, 1000, 1000)]
    [InlineData(true, 1280, 720)]
    public void Cover_renders_as_a_jpeg_of_the_right_size(bool wide, int width, int height)
    {
        var bytes = CoverService.Generate(Array.Empty<string>(), "#FF7518", "Halloween", "For You", wide);

        Assert.NotNull(bytes);
        Assert.Equal(0xFF, bytes![0]);
        Assert.Equal(0xD8, bytes[1]); // JPEG magic
        using var img = SKBitmap.Decode(bytes);
        Assert.Equal(width, img.Width);
        Assert.Equal(height, img.Height);
    }
}
