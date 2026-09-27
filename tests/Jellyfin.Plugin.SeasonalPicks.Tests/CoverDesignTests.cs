using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Jellyfin.Plugin.SeasonalPicks.Configuration;
using Jellyfin.Plugin.SeasonalPicks.Services;
using SkiaSharp;
using Xunit;

namespace Jellyfin.Plugin.SeasonalPicks.Tests;

public class CoverDesignTests
{
    [Fact]
    public void Default_design_looks_the_same_as_before()
    {
        var withoutStyle = CoverService.Generate(Array.Empty<string>(), "#FF7518", "Halloween", "Suggested", false, CoverTheme.Halloween);
        var withDefaults = CoverService.Generate(Array.Empty<string>(), "#FF7518", "Halloween", "Suggested", false, CoverTheme.Halloween, new CoverStyle());
        Assert.Equal(withoutStyle, withDefaults);
    }

    public static TheoryData<string, CoverStyle> Designs() => new()
    {
        { "title-top-big", new CoverStyle { TitlePosition = "Top", TitleSize = 140, TitleColor = "#FFE81F" } },
        { "title-bottom-small", new CoverStyle { TitlePosition = "Bottom", TitleSize = 60, ShowAccent = false } },
        { "custom-text", new CoverStyle { Title = "May the 4th", Label = "Be with you" } },
        { "no-label", new CoverStyle { ShowLabel = false } },
        { "plain", new CoverStyle { Background = "Plain" } },
        { "light", new CoverStyle { Darkness = 0, ColourWash = 0, Decorations = 40 } },
        { "dark", new CoverStyle { Darkness = 100, ColourWash = 100 } },
        { "no-decorations", new CoverStyle { Decorations = 0 } },
        { "out-of-range", new CoverStyle { TitleSize = 999, Darkness = -5, Decorations = 300, TitleColor = "not a colour", TitlePosition = "sideways" } }
    };

    [Theory]
    [MemberData(nameof(Designs))]
    public void Every_design_option_renders(string name, CoverStyle style)
    {
        foreach (var wide in new[] { false, true })
        {
            var bytes = CoverService.Generate(Array.Empty<string>(), "#FFE81F", "Star Wars Day", "Suggested", wide, CoverTheme.Lightsabers, style);
            Assert.NotNull(bytes);
            using var img = SKBitmap.Decode(bytes);
            Assert.Equal(wide ? 1280 : 1000, img.Width);
            Save($"design-{name}-{(wide ? "wide" : "square")}.jpg", bytes!);
        }
    }

    [Fact]
    public void Picture_background_is_drawn_behind_the_design()
    {
        var picture = Path.Combine(Path.GetTempPath(), "sp-background-test.png");
        using (var surface = SKSurface.Create(new SKImageInfo(800, 500)))
        {
            // Bright green: easy to find in the result.
            surface.Canvas.Clear(new SKColor(0, 255, 0));
            using var snap = surface.Snapshot();
            using var data = snap.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(picture, data.ToArray());
        }

        var style = new CoverStyle { Background = "Picture", ColourWash = 0, Darkness = 0, Decorations = 0, ShowLabel = false };
        var withPicture = CoverService.Generate(Array.Empty<string>(), "#3A6EA5", "Picture", "Suggested", false, CoverTheme.None, style, picture);
        var withoutFile = CoverService.Generate(Array.Empty<string>(), "#3A6EA5", "Picture", "Suggested", false, CoverTheme.None, style, null);

        using var a = SKBitmap.Decode(withPicture);
        using var b = SKBitmap.Decode(withoutFile);
        var corner = a.GetPixel(10, 10);
        Assert.True(corner.Green > 200 && corner.Red < 60, $"corner was {corner}");
        Assert.True(b.GetPixel(10, 10).Green < 200, "without a file it falls back to the posters/colour");
        Save("design-picture-square.jpg", withPicture!);
    }

    [Fact]
    public void Cover_designs_survive_saving_and_old_settings_get_the_standard_design()
    {
        var cfg = new PluginConfiguration();
        cfg.Seasons[0].Cover = new CoverStyle { Title = "Spooky", TitleSize = 120, Background = "Plain", Darkness = 70 };
        cfg.DefaultCover.TitlePosition = "Top";
        cfg.DefaultCoverTheme = "Snow";

        var xml = new XmlSerializer(typeof(PluginConfiguration));
        using var sw = new StringWriter();
        xml.Serialize(sw, cfg);
        var back = (PluginConfiguration)xml.Deserialize(new StringReader(sw.ToString()))!;

        Assert.Equal("Spooky", back.Seasons[0].Cover.Title);
        Assert.Equal(120, back.Seasons[0].Cover.TitleSize);
        Assert.Equal("Plain", back.Seasons[0].Cover.Background);
        Assert.Equal(70, back.Seasons[0].Cover.Darkness);
        Assert.Equal("Top", back.DefaultCover.TitlePosition);
        Assert.Equal("Snow", back.DefaultCoverTheme);

        // Settings saved before designs existed have no <Cover> element.
        const string old = "<?xml version=\"1.0\"?><PluginConfiguration xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" +
            "<Seasons><SeasonRule><Name>Halloween</Name><Theme>Halloween</Theme></SeasonRule></Seasons></PluginConfiguration>";
        var loaded = (PluginConfiguration)xml.Deserialize(new StringReader(old))!;
        Assert.NotNull(loaded.Seasons.Single().Cover);
        Assert.Equal(100, loaded.Seasons.Single().Cover.TitleSize);
        Assert.Equal("Posters", loaded.Seasons.Single().Cover.Background);
        Assert.Equal("Sparkle", loaded.DefaultCoverTheme);
    }

    [Fact]
    public void Uploads_follow_a_renamed_season_without_replacing_newer_ones()
    {
        var covers = new CoverService(null!, null!, null!);
        var png = new byte[] { 1, 2, 3 };
        var from = "sprenamefrom" + Environment.ProcessId;
        var to = "sprenameto" + Environment.ProcessId;
        try
        {
            covers.SaveUpload(from, png, "image/png");
            covers.SaveBackground(from, png, "image/jpeg");
            covers.RenameKey(from, to);

            Assert.Null(covers.FindUploaded(from));
            Assert.Null(covers.FindBackground(from));
            Assert.EndsWith(to + ".png", covers.FindUploaded(to));
            Assert.EndsWith(to + ".jpg", covers.FindBackground(to));

            // Something already uploaded under the new name is kept.
            covers.SaveUpload(from, new byte[] { 9 }, "image/png");
            covers.RenameKey(from, to);
            Assert.Equal(png, File.ReadAllBytes(covers.FindUploaded(to)!));
        }
        finally
        {
            covers.DeleteUpload(from);
            covers.DeleteUpload(to);
            covers.DeleteBackground(from);
            covers.DeleteBackground(to);
        }
    }

    private static void Save(string file, byte[] bytes)
    {
        var outDir = Environment.GetEnvironmentVariable("SP_COVER_OUT");
        if (!string.IsNullOrEmpty(outDir))
        {
            Directory.CreateDirectory(outDir);
            File.WriteAllBytes(Path.Combine(outDir, file), bytes);
        }
    }
}
