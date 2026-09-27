using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Plugin.SeasonalPicks.Configuration;
using Jellyfin.Plugin.SeasonalPicks.Services;
using SkiaSharp;
using Xunit;

namespace Jellyfin.Plugin.SeasonalPicks.Tests;

public class CoverArtTests
{
    [Theory]
    [InlineData("Christmas", CoverTheme.Christmas)]
    [InlineData("Winter Warmers", CoverTheme.Snow)]
    [InlineData("Halloween", CoverTheme.Halloween)]
    [InlineData("Easter", CoverTheme.Easter)]
    [InlineData("Valentine's", CoverTheme.Hearts)]
    [InlineData("Summer", CoverTheme.Summer)]
    [InlineData("My Birthday", CoverTheme.Sparkle)]
    [InlineData("New Year", CoverTheme.Fireworks)]
    [InlineData("Bonfire Night", CoverTheme.Bonfire)]
    [InlineData("Star Wars Day", CoverTheme.Lightsabers)]
    [InlineData("Ghostbusters Day", CoverTheme.Ghosts)]
    [InlineData("Back to Hogwarts", CoverTheme.Castle)]
    [InlineData("Star Trek Day", CoverTheme.Space)]
    public void Theme_is_guessed_from_the_season_name(string name, CoverTheme expected)
    {
        Assert.Equal(expected, CoverArt.ThemeFor("auto", name));
        Assert.Equal(expected, CoverArt.ThemeFor(null, name));
    }

    [Fact]
    public void A_chosen_theme_wins_over_the_name()
    {
        Assert.Equal(CoverTheme.Hearts, CoverArt.ThemeFor("hearts", "Halloween"));
        Assert.Equal(CoverTheme.None, CoverArt.ThemeFor("None", "Christmas"));
        Assert.Equal(CoverTheme.Christmas, CoverArt.ThemeFor("not-a-theme", "Christmas")); // unknown value falls back to auto
    }

    public static IEnumerable<object[]> AllThemes()
        => Enum.GetValues<CoverTheme>().SelectMany(t => new[] { new object[] { t, false }, new object[] { t, true } });

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void Every_theme_renders_at_both_sizes(CoverTheme theme, bool wide)
    {
        var posters = FakePosters();
        var names = new Dictionary<CoverTheme, (string Name, string Colour)>
        {
            [CoverTheme.Snow] = ("Christmas", "#C8102E"),
            [CoverTheme.Halloween] = ("Halloween", "#FF7518"),
            [CoverTheme.Easter] = ("Easter", "#8CC152"),
            [CoverTheme.Hearts] = ("Valentine's", "#E0457B"),
            [CoverTheme.Summer] = ("Summer", "#F2B705"),
            [CoverTheme.Fireworks] = ("New Year", "#D4AF37"),
            [CoverTheme.Space] = ("Star Wars Day", "#FFE81F"),
            [CoverTheme.Shamrock] = ("St Patrick's Day", "#169B62"),
            [CoverTheme.Christmas] = ("Christmas", "#C8102E"),
            [CoverTheme.Lightsabers] = ("Star Wars Day", "#FFE81F"),
            [CoverTheme.Ghosts] = ("Ghostbusters Day", "#4CAF50"),
            [CoverTheme.Castle] = ("Back to Hogwarts", "#740001"),
            [CoverTheme.Bonfire] = ("Bonfire Night", "#FF6D00"),
            [CoverTheme.Sparkle] = ("Suggested", "#3A6EA5"),
            [CoverTheme.None] = ("A Much Longer Season Name", "#3A6EA5")
        };
        var (name, colour) = names[theme];

        var bytes = CoverService.Generate(posters, colour, name, "Suggested", wide, theme);

        Assert.NotNull(bytes);
        using var img = SKBitmap.Decode(bytes);
        Assert.Equal(wide ? 1280 : 1000, img.Width);

        // Set SP_COVER_OUT to a folder to look at the results.
        var outDir = Environment.GetEnvironmentVariable("SP_COVER_OUT");
        if (!string.IsNullOrEmpty(outDir))
        {
            Directory.CreateDirectory(outDir);
            File.WriteAllBytes(Path.Combine(outDir, $"{theme}-{(wide ? "wide" : "square")}.jpg"), bytes!);
        }
    }

    [Fact]
    public void Every_default_season_has_a_cover()
    {
        // Renders each built-in season exactly as the server would (its name, colour and theme).
        // With SP_COVER_OUT set, also writes them out plus a season list for the preview page.
        var outDir = Environment.GetEnvironmentVariable("SP_COVER_OUT");
        var posters = FakePosters();
        var lines = new List<string>();
        foreach (var season in SeasonRule.Defaults())
        {
            var theme = CoverArt.ThemeFor(season.Theme, season.Name);
            foreach (var wide in new[] { false, true })
            {
                var bytes = CoverService.Generate(posters, season.Color, season.Name, "Suggested", wide, theme);
                Assert.NotNull(bytes);
                if (!string.IsNullOrEmpty(outDir))
                {
                    Directory.CreateDirectory(outDir);
                    File.WriteAllBytes(Path.Combine(outDir, $"season-{season.CoverKey}-{(wide ? "wide" : "square")}.jpg"), bytes!);
                }
            }

            var when = season.IsEasterBased
                ? $"easter{season.StartEasterOffset:+0;-0} to easter{season.EndEasterOffset:+0;-0}"
                : $"{season.StartDay}/{season.StartMonth} – {season.EndDay}/{season.EndMonth}";
            lines.Add($"{season.CoverKey}\t{season.Name}\t{theme}\t{when}");
        }

        if (!string.IsNullOrEmpty(outDir))
        {
            File.WriteAllLines(Path.Combine(outDir, "seasons.tsv"), lines);
        }
    }

    [Fact]
    public void Same_season_gives_the_same_decoration_layout()
    {
        var a = CoverService.Generate(Array.Empty<string>(), "#C8102E", "Christmas", "For You", false, CoverTheme.Snow);
        var b = CoverService.Generate(Array.Empty<string>(), "#C8102E", "Christmas", "For You", false, CoverTheme.Snow);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Titles_use_the_bundled_noto_sans_font()
    {
        var typeface = CoverArt.TitleTypeface.Value;
        Assert.True(CoverArt.UsingBundledFont);
        Assert.Equal("Noto Sans", typeface.FamilyName);
        Assert.True(typeface.FontWeight >= 700);

        // It has the glyphs a season name needs, including apostrophes and accented letters.
        using var font = new SKFont(typeface, 40);
        Assert.All("Valentine's Noël Día", ch => Assert.True(ch == ' ' || font.GetGlyph(ch) != 0, $"missing glyph '{ch}'"));
    }

    [Fact]
    public void Old_saved_seasons_default_to_automatic_theme()
    {
        Assert.Equal("auto", new SeasonRule().Theme);
    }

    /// <summary>Stand-in posters: coloured panels with a big letter, like busy real posters behind the title.</summary>
    private static List<string> FakePosters()
    {
        var dir = Path.Combine(Path.GetTempPath(), "sp-fake-posters");
        Directory.CreateDirectory(dir);
        var colours = new[] { "#2E4057", "#8D6A9F", "#D1495B", "#EDAE49", "#00798C", "#30638E", "#66A182", "#A23B72", "#3B1F2B", "#C73E1D" };
        var list = new List<string>();
        for (var i = 0; i < colours.Length; i++)
        {
            var path = Path.Combine(dir, $"p{i}.jpg");
            if (!File.Exists(path))
            {
                using var surface = SKSurface.Create(new SKImageInfo(400, 600));
                surface.Canvas.Clear(SKColor.Parse(colours[i]));
                using var paint = new SKPaint { Color = SKColors.White.WithAlpha(160), IsAntialias = true };
                using var font = new SKFont(SKTypeface.Default, 300);
                surface.Canvas.DrawText(((char)('A' + i)).ToString(), 200, 420, SKTextAlign.Center, font, paint);
                using var light = new SKPaint { Color = SKColors.White.WithAlpha(60) };
                surface.Canvas.DrawCircle(80 + (i * 20), 100, 60, light);
                using var snap = surface.Snapshot();
                using var data = snap.Encode(SKEncodedImageFormat.Jpeg, 85);
                File.WriteAllBytes(path, data.ToArray());
            }

            list.Add(path);
        }

        return list;
    }
}
