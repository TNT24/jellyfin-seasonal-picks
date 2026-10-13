using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Plugin.SeasonalPicks.Configuration;
using SkiaSharp;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>Decoration drawn over the poster collage.</summary>
public enum CoverTheme
{
    /// <summary>Soft sparkles (the out-of-season cover).</summary>
    Sparkle,
    Snow,
    Halloween,
    Easter,
    Hearts,
    Summer,
    Fireworks,
    Shamrock,
    Space,
    Christmas,
    Lightsabers,
    Ghosts,
    Castle,
    Bonfire,
    Pancakes,
    LunarNewYear,
    None
}

/// <summary>
/// Themed cover decorations and the centred title, drawn with plain shapes (no image assets to ship).
/// Layout is seeded from the caption, so a season's cover looks the same on every refresh apart from the posters.
/// </summary>
internal static partial class CoverArt
{
    /// <summary>
    /// Noto Sans Bold, shipped inside the plugin: it's Jellyfin's own UI font, and it means titles render even in a
    /// container with no system fonts. Falls back to whatever bold font the system has.
    /// </summary>
    internal static readonly Lazy<SKTypeface> TitleTypeface = new(LoadTitleTypeface);

    internal static bool UsingBundledFont { get; private set; }

    private static SKTypeface LoadTitleTypeface()
    {
        try
        {
            using var stream = typeof(CoverArt).Assembly.GetManifestResourceStream($"{typeof(Plugin).Namespace}.Fonts.NotoSans-Bold.ttf");
            if (stream is not null)
            {
                using var data = SKData.Create(stream);
                if (SKTypeface.FromData(data) is { } bundled)
                {
                    UsingBundledFont = true;
                    return bundled;
                }
            }
        }
        catch (Exception)
        {
            // Fall through to a system font.
        }

        return SKTypeface.FromFamilyName(null, SKFontStyle.Bold) ?? SKTypeface.Default;
    }

    /// <summary>Explicit theme name from the settings, else a guess from the season's name.</summary>
    public static CoverTheme ThemeFor(string? theme, string? seasonName)
    {
        if (!string.IsNullOrWhiteSpace(theme) && !theme.Equals("auto", StringComparison.OrdinalIgnoreCase)
            && Enum.TryParse<CoverTheme>(theme, true, out var chosen))
        {
            return chosen;
        }

        var n = (seasonName ?? string.Empty).ToLowerInvariant();
        if (n.Length == 0)
        {
            return CoverTheme.Sparkle;
        }

        // First match wins, so the more specific names come first.
        var byName = new (string[] Words, CoverTheme Theme)[]
        {
            (new[] { "star wars", "jedi" }, CoverTheme.Lightsabers),
            (new[] { "ghostbuster" }, CoverTheme.Ghosts),
            (new[] { "hogwarts", "harry potter", "wizard" }, CoverTheme.Castle),
            (new[] { "bonfire", "guy fawkes" }, CoverTheme.Bonfire),
            (new[] { "pancake", "shrove" }, CoverTheme.Pancakes),
            (new[] { "chinese new year", "lunar new year", "spring festival", "lantern festival" }, CoverTheme.LunarNewYear),
            (new[] { "christmas", "xmas" }, CoverTheme.Christmas),
            (new[] { "winter", "snow", "holiday" }, CoverTheme.Snow),
            (new[] { "halloween", "spooky", "horror" }, CoverTheme.Halloween),
            (new[] { "easter", "spring" }, CoverTheme.Easter),
            (new[] { "valentine", "love", "romance" }, CoverTheme.Hearts),
            (new[] { "new year", "firework" }, CoverTheme.Fireworks),
            (new[] { "patrick", "irish", "ireland" }, CoverTheme.Shamrock),
            (new[] { "space", "sci-fi", "star" }, CoverTheme.Space)
        };
        foreach (var (words, mapped) in byName)
        {
            if (words.Any(n.Contains))
            {
                return mapped;
            }
        }

        if (n.Contains("summer") || n.Contains("beach"))
        {
            return CoverTheme.Summer;
        }

        return CoverTheme.Sparkle;
    }

    /// <summary>
    /// The scenes a decoration can draw, in the order the weeks of a season go through them.
    /// Longer seasons have several so the cover changes as the season goes on; the rest have one.
    /// </summary>
    public static string[] Scenes(CoverTheme theme) => theme switch
    {
        CoverTheme.Halloween => new[] { "Pumpkin patch", "Graveyard", "Haunted house", "Witch's cauldron" },
        CoverTheme.Christmas => new[] { "Christmas tree", "Fireplace", "Santa's sleigh", "Baubles and holly" },
        CoverTheme.Snow => new[] { "Snowfall", "Snowy forest" },
        CoverTheme.Easter => new[] { "Bunny and eggs", "Basket of chicks" },
        CoverTheme.Hearts => new[] { "Floating hearts", "Hearts and roses", "Heart balloons" },
        CoverTheme.LunarNewYear => new[] { "Red lanterns", "Lantern festival" },
        _ => new[] { "Standard" }
    };

    /// <summary>The scene to draw: the one the design pins (1-based), else the week of the season picks it.</summary>
    public static int SceneFor(CoverTheme theme, int pinned, int week)
    {
        var count = Scenes(theme).Length;
        return pinned > 0 ? (pinned - 1) % count : Math.Max(0, week) % count;
    }

    public static void DrawTheme(SKCanvas canvas, CoverTheme theme, int w, int h, SKColor tint, string seed, int scene = 0)
    {
        // Each scene gets its own layout; scene 0 keeps the seed it always had.
        var rng = new Random(StableHash(scene == 0 ? seed : seed + "#" + scene));
        switch (theme)
        {
            case CoverTheme.Halloween when scene == 1: Graveyard(canvas, w, h, rng); break;
            case CoverTheme.Halloween when scene == 2: HauntedHouse(canvas, w, h, rng); break;
            case CoverTheme.Halloween when scene == 3: Cauldron(canvas, w, h, rng); break;
            case CoverTheme.Christmas when scene == 1: Fireplace(canvas, w, h, rng); break;
            case CoverTheme.Christmas when scene == 2: SantaSleigh(canvas, w, h, rng); break;
            case CoverTheme.Christmas when scene == 3: BaublesAndHolly(canvas, w, h, rng); break;
            case CoverTheme.Snow when scene == 1: SnowyForest(canvas, w, h, rng); break;
            case CoverTheme.Easter when scene == 1: ChickBasket(canvas, w, h, rng); break;
            case CoverTheme.Hearts when scene == 1: HeartsAndRoses(canvas, w, h, tint, rng); break;
            case CoverTheme.Hearts when scene == 2: HeartBalloons(canvas, w, h, tint, rng); break;
            case CoverTheme.LunarNewYear when scene == 1: LanternFestival(canvas, w, h, rng); break;
            case CoverTheme.LunarNewYear: RedLanterns(canvas, w, h, rng); break;
            case CoverTheme.Pancakes: PancakeStack(canvas, w, h, rng); break;
            case CoverTheme.Snow: Snow(canvas, w, h, rng); break;
            case CoverTheme.Halloween: Halloween(canvas, w, h, rng); break;
            case CoverTheme.Easter: Easter(canvas, w, h, rng); break;
            case CoverTheme.Hearts: Hearts(canvas, w, h, tint, rng); break;
            case CoverTheme.Summer: Summer(canvas, w, h, rng); break;
            case CoverTheme.Fireworks: Fireworks(canvas, w, h, tint, rng); break;
            case CoverTheme.Shamrock: Shamrocks(canvas, w, h, rng); break;
            case CoverTheme.Space: Space(canvas, w, h, tint, rng); break;
            case CoverTheme.Sparkle: Sparkle(canvas, w, h, rng); break;
            case CoverTheme.Christmas: Snow(canvas, w, h, rng, treeCorner: true); ChristmasTree(canvas, w, h, rng); break;
            case CoverTheme.Lightsabers: StarBackdrop(canvas, w, h, rng); Lightsabers(canvas, w, h, rng); break;
            case CoverTheme.Ghosts: Ghosts(canvas, w, h, rng); break;
            case CoverTheme.Castle: Castle(canvas, w, h, rng); break;
            case CoverTheme.Bonfire: BonfireScene(canvas, w, h, tint, rng); break;
        }
    }

    /// <summary>Big title with a small label above it, over a soft dark glow so it reads on any posters.</summary>
    public static void DrawTitle(SKCanvas canvas, int w, int h, SKColor tint, string caption, string? label, CoverStyle? style = null)
    {
        style ??= new CoverStyle();
        float cx = w / 2f, s = Math.Min(w, h);
        var cy = (style.TitlePosition ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "top" => h * 0.26f,
            "bottom" => h * 0.74f,
            _ => h / 2f
        };

        using (var scrim = new SKPaint
        {
            Shader = SKShader.CreateRadialGradient(
                new SKPoint(cx, cy), Math.Max(w, h) * 0.55f,
                new[] { SKColors.Black.WithAlpha(150), SKColors.Black.WithAlpha(65), SKColors.Transparent },
                new[] { 0f, 0.55f, 1f },
                SKShaderTileMode.Clamp)
        })
        {
            canvas.DrawRect(0, 0, w, h, scrim);
        }

        var typeface = TitleTypeface.Value;
        using var titleFont = new SKFont(typeface, s * 0.2f * Math.Clamp(style.TitleSize, 50, 150) / 100f);
        var maxWidth = w * 0.88f;
        while (titleFont.MeasureText(caption) > maxWidth && titleFont.Size > 24)
        {
            titleFont.Size -= 4;
        }

        titleFont.MeasureText(caption, out var bounds);
        var baseline = cy - bounds.MidY;

        var colour = SKColor.TryParse(style.TitleColor, out var chosen) ? chosen.WithAlpha(255) : SKColors.White;
        using var shadow = SKImageFilter.CreateDropShadow(0, s * 0.006f, s * 0.012f, s * 0.012f, SKColors.Black.WithAlpha(220));
        using var titlePaint = new SKPaint { Color = colour, IsAntialias = true, ImageFilter = shadow };
        canvas.DrawText(caption, cx, baseline, SKTextAlign.Center, titleFont, titlePaint);

        // Accent line under the title in a lighter season colour.
        if (style.ShowAccent)
        {
            var lineW = Math.Min(bounds.Width * 0.5f, w * 0.35f);
            var lineY = baseline + bounds.Bottom + (s * 0.045f);
            using var accent = new SKPaint { Color = Lighten(tint, 0.35f), IsAntialias = true, ImageFilter = shadow };
            canvas.DrawRoundRect(new SKRect(cx - (lineW / 2), lineY, cx + (lineW / 2), lineY + (s * 0.012f)), s * 0.006f, s * 0.006f, accent);
        }

        if (!string.IsNullOrWhiteSpace(label) && !string.Equals(label, caption, StringComparison.OrdinalIgnoreCase))
        {
            using var labelFont = new SKFont(typeface, titleFont.Size * 0.3f);
            labelFont.Size = Math.Min(labelFont.Size, s * 0.055f);
            using var labelPaint = new SKPaint { Color = SKColors.White.WithAlpha(225), IsAntialias = true, ImageFilter = shadow };
            var text = label.ToUpper(CultureInfo.InvariantCulture);
            canvas.DrawText(text, cx, baseline + bounds.Top - (s * 0.04f), SKTextAlign.Center, labelFont, labelPaint);
        }
    }

    // ---------- themes ----------

    private static void Snow(SKCanvas c, int w, int h, Random rng, bool treeCorner = false)
    {
        float s = Math.Min(w, h);

        // Falling snow.
        using var dot = new SKPaint { IsAntialias = true };
        for (var i = 0; i < 170; i++)
        {
            dot.Color = SKColors.White.WithAlpha((byte)rng.Next(120, 235));
            c.DrawCircle(R(rng, 0, w), R(rng, 0, h * 0.9f), R(rng, s * 0.002f, s * 0.008f), dot);
        }

        // Snowflakes, kept off the title and apart from each other.
        using var flake = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, Color = SKColors.White.WithAlpha(215) };
        var placed = new List<(float X, float Y, float R)>();
        for (var i = 0; i < 11; i++)
        {
            var r = R(rng, s * 0.022f, s * 0.05f);
            for (var attempt = 0; attempt < 20; attempt++)
            {
                if (FindClear(rng, placed, r, 0, w, 0, h * 0.8f, 1.3f) is not { } at)
                {
                    break;
                }

                var onTree = treeCorner && at.X > w - (s * 0.42f) - r && at.Y > h - (s * 0.5f) - r;
                if (onTree || (Math.Abs(at.X - (w / 2f)) < (w * 0.45f) + r && Math.Abs(at.Y - (h / 2f)) < (h * 0.19f) + r))
                {
                    continue;
                }

                flake.StrokeWidth = r * 0.12f;
                Snowflake(c, at.X, at.Y, r, R(rng, 0, 60), flake);
                placed.Add((at.X, at.Y, r));
                break;
            }
        }

        // Snowdrifts along the bottom.
        using var drift = new SKPaint { IsAntialias = true, Color = new SKColor(0xF4, 0xF8, 0xFF, 235) };
        using var driftShade = new SKPaint { IsAntialias = true, Color = new SKColor(0xC9, 0xD8, 0xEE, 220) };
        c.DrawPath(Hills(w, h, h * 0.9f, h * 0.035f, 3, rng), driftShade);
        c.DrawPath(Hills(w, h, h * 0.93f, h * 0.03f, 2, rng), drift);
    }

    private static void Snowflake(SKCanvas c, float x, float y, float r, float rotation, SKPaint p)
    {
        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(rotation);
        for (var i = 0; i < 6; i++)
        {
            c.DrawLine(0, 0, 0, -r, p);
            c.DrawLine(0, -r * 0.55f, -r * 0.25f, -r * 0.8f, p);
            c.DrawLine(0, -r * 0.55f, r * 0.25f, -r * 0.8f, p);
            c.RotateDegrees(60);
        }

        c.Restore();
    }

    private static void Halloween(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);

        // Stars and a cratered moon.
        Stars(c, w, h * 0.35f, 60, s, rng);
        var moon = new SKPoint(w * 0.82f, h * 0.2f);
        var mr = s * 0.11f;
        Moon(c, moon.X, moon.Y, mr, rng);

        // Bats, spread out: each keeps clear of the others and of the moon.
        using var bat = new SKPaint { IsAntialias = true, Color = new SKColor(0x12, 0x08, 0x10, 235) };
        var placed = new List<(float X, float Y, float R)> { (moon.X, moon.Y, mr * 1.1f) };
        for (var i = 0; i < 6; i++)
        {
            var br = R(rng, s * 0.045f, s * 0.085f);
            if (FindClear(rng, placed, br, w * 0.06f, w * 0.94f, h * 0.06f, h * 0.3f, 1.15f) is { } at)
            {
                Bat(c, at.X, at.Y, br, R(rng, -20, 20), bat);
                placed.Add((at.X, at.Y, br));
            }
        }

        // Dark hill, then jack-o'-lanterns on it.
        using (var hill = new SKPaint { IsAntialias = true, Color = new SKColor(0x10, 0x0A, 0x14, 240) })
        {
            c.DrawPath(Hills(w, h, h * 0.86f, h * 0.04f, 2, rng), hill);
        }

        var count = w > h ? 4 : 3;
        for (var i = 0; i < count; i++)
        {
            var size = s * (i % 2 == 0 ? 0.13f : 0.1f);
            var x = (w / (float)count * (i + 0.5f)) + R(rng, -w * 0.04f, w * 0.04f);
            Pumpkin(c, x, h * 0.9f, size);
        }
    }

    private static void Bat(SKCanvas c, float x, float y, float r, float rotation, SKPaint p)
    {
        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(rotation);

        // One wing: arched top edge out to the tip, then scalloped between the "finger" points back to the body.
        void Wing(SKPath path, float side)
        {
            path.MoveTo(0, -r * 0.1f);
            path.CubicTo(side * r * 0.3f, -r * 0.5f, side * r * 0.7f, -r * 0.65f, side * r, -r * 0.45f);
            var points = new[] { (X: 0.95f, Y: 0.02f), (X: 0.66f, Y: 0.16f), (X: 0.4f, Y: 0.2f), (X: 0.14f, Y: 0.3f) };
            var prev = (X: 1f, Y: -0.45f);
            foreach (var pt in points)
            {
                var mid = ((prev.X + pt.X) / 2, ((prev.Y + pt.Y) / 2) - 0.14f);
                path.QuadTo(side * r * mid.Item1, r * mid.Item2, side * r * pt.X, r * pt.Y);
                prev = pt;
            }

            path.LineTo(0, r * 0.38f);
            path.Close();
        }

        using var bat = new SKPath();
        Wing(bat, 1);
        Wing(bat, -1);
        c.DrawPath(bat, p);

        // Head and ears.
        c.DrawCircle(0, -r * 0.08f, r * 0.17f, p);
        using var ears = new SKPath();
        foreach (var side in new[] { -1f, 1f })
        {
            ears.MoveTo(side * r * 0.14f, -r * 0.12f);
            ears.LineTo(side * r * 0.1f, -r * 0.42f);
            ears.LineTo(side * r * 0.01f, -r * 0.2f);
            ears.Close();
        }

        c.DrawPath(ears, p);

        // Body, a little rounder than the wing roots.
        c.DrawOval(new SKRect(-r * 0.13f, -r * 0.02f, r * 0.13f, r * 0.42f), p);

        // Wing bones from the shoulder out to the tip and each finger, then a faint lighter rim around the silhouette.
        using (var bone = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = r * 0.028f, Color = new SKColor(0x5B, 0x3D, 0x60, 210) })
        {
            foreach (var side in new[] { -1f, 1f })
            {
                var shoulder = new SKPoint(side * r * 0.12f, -r * 0.08f);
                var elbow = new SKPoint(side * r * 0.45f, -r * 0.42f);
                c.DrawLine(shoulder, elbow, bone);
                c.DrawLine(elbow, new SKPoint(side * r * 0.98f, -r * 0.43f), bone);
                foreach (var (fx, fy) in new[] { (0.95f, 0.02f), (0.66f, 0.16f), (0.4f, 0.2f) })
                {
                    c.DrawLine(elbow, new SKPoint(side * r * fx, r * fy), bone);
                }
            }
        }

        using (var rim = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeJoin = SKStrokeJoin.Round, StrokeWidth = r * 0.03f, Color = new SKColor(0x7A, 0x55, 0x80, 150) })
        {
            c.DrawPath(bat, rim);
        }

        // Feet.
        using (var feet = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = r * 0.035f, Color = p.Color })
        {
            c.DrawLine(-r * 0.05f, r * 0.38f, -r * 0.08f, r * 0.5f, feet);
            c.DrawLine(r * 0.05f, r * 0.38f, r * 0.08f, r * 0.5f, feet);
        }

        // Glowing eyes.
        using (var eye = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(0xFF, 0xD5, 0x4F),
            ImageFilter = SKImageFilter.CreateDropShadow(0, 0, r * 0.04f, r * 0.04f, new SKColor(0xFF, 0x9F, 0x00, 230))
        })
        {
            c.DrawCircle(-r * 0.065f, -r * 0.1f, r * 0.035f, eye);
            c.DrawCircle(r * 0.065f, -r * 0.1f, r * 0.035f, eye);
        }

        c.Restore();
    }

    private static void Pumpkin(SKCanvas c, float cx, float bottom, float r)
    {
        var cy = bottom - r;
        using var body = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(new SKPoint(cx - (r * 0.3f), cy - (r * 0.4f)), r * 1.5f, new[] { new SKColor(0xFF, 0xA2, 0x4A), new SKColor(0xF2, 0x7A, 0x1A), new SKColor(0xB4, 0x4E, 0x0C) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp)
        };
        using var rib = new SKPaint { IsAntialias = true, Color = new SKColor(0xC4, 0x55, 0x0B), Style = SKPaintStyle.Stroke, StrokeWidth = r * 0.05f };
        using var stem = new SKPaint { IsAntialias = true, Color = new SKColor(0x4E, 0x6B, 0x21) };

        c.DrawRoundRect(new SKRect(cx - (r * 0.1f), cy - (r * 1.05f), cx + (r * 0.1f), cy - (r * 0.7f)), r * 0.05f, r * 0.05f, stem);
        foreach (var dx in new[] { -0.45f, 0.45f, 0f })
        {
            var rect = new SKRect(cx + (dx * r) - (r * 0.62f), cy - (r * 0.8f), cx + (dx * r) + (r * 0.62f), cy + (r * 0.8f));
            c.DrawOval(rect, body);
            c.DrawOval(rect, rib);
        }

        // Lit face.
        using var face = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(0xFF, 0xD5, 0x4F),
            ImageFilter = SKImageFilter.CreateDropShadow(0, 0, r * 0.08f, r * 0.08f, new SKColor(0xFF, 0xB3, 0x00, 200))
        };
        c.DrawPath(Triangle(cx - (r * 0.35f), cy - (r * 0.1f), r * 0.2f, false), face);
        c.DrawPath(Triangle(cx + (r * 0.35f), cy - (r * 0.1f), r * 0.2f, false), face);
        c.DrawPath(Triangle(cx, cy + (r * 0.1f), r * 0.12f, false), face);
        using var mouth = new SKPath();
        mouth.MoveTo(cx - (r * 0.55f), cy + (r * 0.3f));
        mouth.QuadTo(cx, cy + (r * 0.85f), cx + (r * 0.55f), cy + (r * 0.3f));
        mouth.LineTo(cx + (r * 0.3f), cy + (r * 0.38f));
        mouth.LineTo(cx + (r * 0.2f), cy + (r * 0.3f));
        mouth.LineTo(cx + (r * 0.05f), cy + (r * 0.4f));
        mouth.LineTo(cx - (r * 0.1f), cy + (r * 0.3f));
        mouth.LineTo(cx - (r * 0.25f), cy + (r * 0.4f));
        mouth.Close();
        c.DrawPath(mouth, face);
    }

    private static void Easter(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var pastels = new[]
        {
            new SKColor(0xF8, 0xB1, 0xC8), new SKColor(0xB5, 0xE3, 0xF5), new SKColor(0xFF, 0xE5, 0x8F),
            new SKColor(0xC9, 0xB6, 0xF2), new SKColor(0xB8, 0xEB, 0xB0), new SKColor(0xFF, 0xC8, 0x9E)
        };

        // A few blossoms floating above.
        for (var i = 0; i < 9; i++)
        {
            Blossom(c, R(rng, 0, w), R(rng, 0, h * 0.4f), R(rng, s * 0.015f, s * 0.03f), pastels[rng.Next(pastels.Length)]);
        }

        // Grass.
        using var back = new SKPaint { IsAntialias = true, Color = new SKColor(0x4C, 0x9A, 0x3C) };
        using var front = new SKPaint { IsAntialias = true, Color = new SKColor(0x6C, 0xC0, 0x4A) };
        Grass(c, w, h, h * 0.86f, s * 0.07f, back, rng);

        // A bunny sitting in the grass on the right; eggs spread across the rest.
        var bunnyUnit = s * 0.34f;
        var bunnyX = w - (bunnyUnit * 0.45f) - (w * 0.03f);
        Bunny(c, bunnyX, h * 0.955f, bunnyUnit);

        // Eggs nestled in the grass in pairs and singles, each with its own pattern, daisies between them.
        var eggsRight = bunnyX - (bunnyUnit * 0.5f);
        var count = w > h ? 7 : 4;
        var eggColours = new[]
        {
            new SKColor(0xF4, 0x8F, 0xB1), new SKColor(0x81, 0xD4, 0xFA), new SKColor(0xFF, 0xD5, 0x4F),
            new SKColor(0xB3, 0x9D, 0xDB), new SKColor(0xA5, 0xD6, 0xA7), new SKColor(0xFF, 0xAB, 0x91)
        };
        for (var i = 0; i < count; i++)
        {
            var ew = s * (i % 2 == 0 ? R(rng, 0.1f, 0.12f) : R(rng, 0.075f, 0.09f));
            var x = (eggsRight / count * (i + 0.5f)) + R(rng, -eggsRight * 0.015f, eggsRight * 0.015f);
            var foot = h * (i % 2 == 0 ? 0.925f : 0.945f);
            Egg(c, x, foot, ew, R(rng, -15, 15), eggColours[i % eggColours.Length], eggColours[(i + 3) % eggColours.Length], i);
        }

        Grass(c, w, h, h * 0.955f, s * 0.03f, front, rng);

        // Daisies in the gaps between neighbouring eggs.
        for (var k = 1; k < count; k += 2)
        {
            Daisy(c, eggsRight / count * k, h * 0.975f, s * 0.018f);
        }
    }

    /// <summary>A sitting rabbit seen from the side, facing left. (x, ground) is under its haunch; u is its height.</summary>
    private static void Bunny(SKCanvas c, float x, float ground, float u)
    {
        var fur = new SKColor(0xF7, 0xF3, 0xEE);
        var shade = new SKColor(0xD8, 0xCC, 0xC2);
        var pink = new SKColor(0xF4, 0xA6, 0xB8);

        using var shadowFilter = SKImageFilter.CreateDropShadow(0, u * 0.01f, u * 0.02f, u * 0.02f, SKColors.Black.WithAlpha(90));
        using var furPaint = new SKPaint
        {
            IsAntialias = true,
            ImageFilter = shadowFilter,
            Shader = SKShader.CreateLinearGradient(new SKPoint(x - (u * 0.4f), ground - u), new SKPoint(x + (u * 0.3f), ground), new[] { fur, fur, shade }, new[] { 0f, 0.55f, 1f }, SKShaderTileMode.Clamp)
        };
        using var pinkPaint = new SKPaint { IsAntialias = true, Color = pink };

        // Ears (drawn first so the head overlaps their base).
        foreach (var (ex, ey, rot, scale) in new[] { (-0.2f, -0.8f, -8f, 1f), (-0.3f, -0.77f, -26f, 0.92f) })
        {
            c.Save();
            c.Translate(x + (u * ex), ground + (u * ey));
            c.RotateDegrees(rot);
            c.DrawOval(new SKRect(-u * 0.055f * scale, -u * 0.2f * scale, u * 0.055f * scale, u * 0.2f * scale), furPaint);
            c.DrawOval(new SKRect(-u * 0.028f * scale, -u * 0.15f * scale, u * 0.028f * scale, u * 0.15f * scale), pinkPaint);
            c.Restore();
        }

        // Body, head and hind foot as one smooth silhouette: chest rising from the front paw to the throat, the back
        // curving over to a round rump, the long hind foot flat on the ground.
        using (var shape = new SKPath())
        {
            shape.MoveTo(x - (u * 0.27f), ground);
            shape.CubicTo(x - (u * 0.36f), ground - (u * 0.02f), x - (u * 0.35f), ground - (u * 0.1f), x - (u * 0.3f), ground - (u * 0.16f));
            shape.CubicTo(x - (u * 0.37f), ground - (u * 0.28f), x - (u * 0.35f), ground - (u * 0.4f), x - (u * 0.24f), ground - (u * 0.46f));
            shape.LineTo(x - (u * 0.1f), ground - (u * 0.48f));
            shape.CubicTo(x + (u * 0.04f), ground - (u * 0.49f), x + (u * 0.2f), ground - (u * 0.42f), x + (u * 0.25f), ground - (u * 0.26f));
            shape.CubicTo(x + (u * 0.29f), ground - (u * 0.14f), x + (u * 0.27f), ground - (u * 0.03f), x + (u * 0.2f), ground);
            shape.Close();
            shape.AddOval(new SKRect(x - (u * 0.44f), ground - (u * 0.67f), x - (u * 0.14f), ground - (u * 0.41f)));
            shape.AddOval(new SKRect(x - (u * 0.14f), ground - (u * 0.06f), x + (u * 0.22f), ground + (u * 0.01f)));
            using var merged = new SKPath();
            shape.Simplify(merged);
            c.DrawPath(merged, furPaint);
        }

        // Soft shading for the thigh and the front leg, inside the outline.
        using (var crease = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = u * 0.012f, Color = shade.WithAlpha(200) })
        using (var thigh = new SKPath())
        {
            thigh.MoveTo(x - (u * 0.02f), ground - (u * 0.05f));
            thigh.CubicTo(x - (u * 0.06f), ground - (u * 0.24f), x + (u * 0.14f), ground - (u * 0.32f), x + (u * 0.21f), ground - (u * 0.16f));
            c.DrawPath(thigh, crease);
            c.DrawLine(x - (u * 0.21f), ground - (u * 0.03f), x - (u * 0.22f), ground - (u * 0.18f), crease);
        }

        // Tail.
        using (var tail = new SKPaint { IsAntialias = true, Color = SKColors.White })
        {
            c.DrawCircle(x + (u * 0.26f), ground - (u * 0.17f), u * 0.06f, tail);
        }

        // Cheek blush, eye with a highlight, nose and whiskers.
        using (var blush = new SKPaint { IsAntialias = true, Color = pink.WithAlpha(110) })
        {
            c.DrawCircle(x - (u * 0.31f), ground - (u * 0.47f), u * 0.035f, blush);
        }

        using (var eye = new SKPaint { IsAntialias = true, Color = new SKColor(0x2B, 0x1D, 0x16) })
        using (var shine = new SKPaint { IsAntialias = true, Color = SKColors.White })
        {
            c.DrawCircle(x - (u * 0.3f), ground - (u * 0.56f), u * 0.024f, eye);
            c.DrawCircle(x - (u * 0.307f), ground - (u * 0.567f), u * 0.008f, shine);
        }

        c.DrawCircle(x - (u * 0.415f), ground - (u * 0.52f), u * 0.018f, pinkPaint);
        using (var whisker = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = u * 0.005f, Color = new SKColor(0x9E, 0x8E, 0x84, 200) })
        {
            foreach (var dy in new[] { -0.03f, 0f, 0.03f })
            {
                c.DrawLine(x - (u * 0.4f), ground - (u * 0.51f), x - (u * 0.55f), ground - (u * (0.51f - dy)), whisker);
            }
        }
    }

    /// <summary>
    /// A painted egg standing on (cx, bottom): egg-shaped (narrower at the top), shaded so it looks round, with a
    /// pattern (0 zigzag bands, 1 polka dots, 2 stripes, 3 little flowers) and a shine.
    /// </summary>
    private static void Egg(SKCanvas c, float cx, float bottom, float width, float rotation, SKColor color, SKColor band, int pattern = 0)
    {
        var height = width * 1.35f;
        float hw = width / 2, hh = height / 2;
        c.Save();
        c.Translate(cx, bottom - hh);
        c.RotateDegrees(rotation);
        using var egg = new SKPath();
        egg.MoveTo(0, -hh);
        egg.CubicTo(hw * 0.92f, -hh, hw, -hh * 0.05f, hw, hh * 0.2f);
        egg.CubicTo(hw, hh * 0.75f, hw * 0.55f, hh, 0, hh);
        egg.CubicTo(-hw * 0.55f, hh, -hw, hh * 0.75f, -hw, hh * 0.2f);
        egg.CubicTo(-hw, -hh * 0.05f, -hw * 0.92f, -hh, 0, -hh);
        egg.Close();

        using (var fill = new SKPaint { IsAntialias = true, Color = color, ImageFilter = SKImageFilter.CreateDropShadow(width * 0.04f, width * 0.06f, width * 0.06f, width * 0.06f, SKColors.Black.WithAlpha(110)) })
        {
            c.DrawPath(egg, fill);
        }

        c.Save();
        c.ClipPath(egg, antialias: true);
        var deep = Darken(band, 0.82f);
        switch (pattern % 4)
        {
            case 1:
                using (var dot = new SKPaint { IsAntialias = true })
                {
                    var row = 0;
                    for (var y = -hh * 0.8f; y < hh; y += height * 0.16f, row++)
                    {
                        for (var x = -hw + (row % 2 == 0 ? 0 : width * 0.11f); x < hw; x += width * 0.22f)
                        {
                            dot.Color = row % 2 == 0 ? deep : SKColors.White.WithAlpha(230);
                            c.DrawCircle(x, y, width * 0.055f, dot);
                        }
                    }
                }

                break;
            case 2:
                using (var stripe = new SKPaint { IsAntialias = true, Color = deep })
                using (var line = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(220) })
                {
                    foreach (var y in new[] { -hh * 0.5f, -hh * 0.05f, hh * 0.4f })
                    {
                        c.DrawRect(-hw, y, width, height * 0.09f, stripe);
                        c.DrawRect(-hw, y - (height * 0.035f), width, height * 0.018f, line);
                    }
                }

                break;
            case 3:
                using (var belt = new SKPaint { IsAntialias = true, Color = deep })
                {
                    c.DrawRect(-hw, hh * 0.15f, width, height * 0.07f, belt);
                }

                foreach (var (fx, fy) in new[] { (-0.18f, -0.45f), (0.2f, -0.2f), (-0.22f, 0.0f), (0.12f, 0.55f), (-0.15f, 0.62f) })
                {
                    Blossom(c, width * fx, hh * fy, width * 0.13f, Lighten(deep, 0.25f));
                }

                break;
            default:
                using (var zig = new SKPaint { IsAntialias = true, Color = deep, Style = SKPaintStyle.Stroke, StrokeWidth = width * 0.09f, StrokeJoin = SKStrokeJoin.Round })
                {
                    foreach (var y in new[] { -height * 0.15f, height * 0.18f })
                    {
                        using var z = new SKPath();
                        z.MoveTo(-width, y);
                        for (var x = -width; x < width; x += width * 0.18f)
                        {
                            z.LineTo(x + (width * 0.09f), y - (width * 0.08f));
                            z.LineTo(x + (width * 0.18f), y);
                        }

                        c.DrawPath(z, zig);
                    }
                }

                using (var dots = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(220) })
                {
                    c.DrawCircle(-width * 0.18f, -hh * 0.62f, width * 0.05f, dots);
                    c.DrawCircle(width * 0.15f, -hh * 0.66f, width * 0.04f, dots);
                    c.DrawCircle(0, height * 0.02f, width * 0.05f, dots);
                    c.DrawCircle(width * 0.2f, hh * 0.7f, width * 0.04f, dots);
                }

                break;
        }

        // Shading over the pattern so the egg looks round, then a shine on the upper left.
        using (var shade = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(-hw * 0.35f, -hh * 0.4f), height * 0.95f, new[] { SKColors.White.WithAlpha(70), SKColors.Transparent, SKColors.Black.WithAlpha(90) }, new[] { 0f, 0.45f, 1f }, SKShaderTileMode.Clamp) })
        {
            c.DrawPath(egg, shade);
        }

        c.Restore();
        using (var shine = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(170) })
        {
            c.Save();
            c.RotateDegrees(-25, -hw * 0.4f, -hh * 0.5f);
            c.DrawOval(new SKRect(-hw * 0.55f, -hh * 0.62f, -hw * 0.25f, -hh * 0.36f), shine);
            c.Restore();
        }

        c.Restore();
    }

    /// <summary>A small white daisy with a yellow middle.</summary>
    private static void Daisy(SKCanvas c, float x, float y, float r)
    {
        using var petal = new SKPaint { IsAntialias = true, Color = SKColors.White };
        for (var i = 0; i < 8; i++)
        {
            c.Save();
            c.RotateDegrees(i * 45, x, y);
            c.DrawOval(new SKRect(x - (r * 0.22f), y - r, x + (r * 0.22f), y), petal);
            c.Restore();
        }

        using var middle = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xC1, 0x07) };
        c.DrawCircle(x, y, r * 0.3f, middle);
    }

    private static void Blossom(SKCanvas c, float x, float y, float r, SKColor color)
    {
        using var petal = new SKPaint { IsAntialias = true, Color = color.WithAlpha(230) };
        for (var i = 0; i < 5; i++)
        {
            var a = i * Math.PI * 2 / 5;
            c.DrawCircle(x + (float)(Math.Cos(a) * r * 0.6), y + (float)(Math.Sin(a) * r * 0.6), r * 0.5f, petal);
        }

        using var centre = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xE0, 0x6B) };
        c.DrawCircle(x, y, r * 0.3f, centre);
    }

    private static void Grass(SKCanvas c, int w, int h, float top, float blade, SKPaint p, Random rng)
    {
        c.DrawRect(0, top, w, h - top, p);
        using var path = new SKPath();
        for (var x = -blade; x < w + blade; x += blade * 0.35f)
        {
            var bh = blade * R(rng, 0.6f, 1.3f);
            var lean = R(rng, -blade * 0.3f, blade * 0.3f);
            path.MoveTo(x, top + 1);
            path.QuadTo(x + (blade * 0.1f) + (lean * 0.5f), top - (bh * 0.6f), x + (blade * 0.1f) + lean, top - bh);
            path.QuadTo(x + (blade * 0.18f), top - (bh * 0.4f), x + (blade * 0.3f), top + 1);
            path.Close();
        }

        c.DrawPath(path, p);
    }

    private static void Hearts(SKCanvas c, int w, int h, SKColor tint, Random rng)
    {
        float s = Math.Min(w, h);
        var colours = new[] { new SKColor(0xFF, 0x4D, 0x7A), new SKColor(0xFF, 0x9E, 0xB8), SKColors.White, Lighten(tint, 0.3f) };
        using var p = new SKPaint { IsAntialias = true };
        // Every heart keeps off the title, and the big ones keep clear of each other.
        var placed = new List<(float X, float Y, float R)>();
        for (var i = 0; i < 34; i++)
        {
            var big = i < 5;
            var size = big ? R(rng, s * 0.09f, s * 0.13f) : R(rng, s * 0.02f, s * 0.05f);
            SKPoint? spot = null;
            for (var attempt = 0; attempt < 30 && spot is null; attempt++)
            {
                var at = FindClear(rng, placed, size * 0.6f, 0, w, 0, h, big ? 1.2f : 0.9f);
                if (at is { } p0 && !(Math.Abs(p0.X - (w / 2f)) < (w * 0.44f) + size && Math.Abs(p0.Y - (h / 2f)) < (h * 0.19f) + (size * 0.6f)))
                {
                    spot = p0;
                }
            }

            if (spot is not { } pos)
            {
                continue;
            }

            placed.Add((pos.X, pos.Y, size * 0.6f));
            var colour = colours[rng.Next(colours.Length)];
            if (big)
            {
                GlossyHeart(c, pos.X, pos.Y, size, colour, R(rng, -20, 20));
                continue;
            }

            p.Color = colour.WithAlpha((byte)rng.Next(130, 220));
            c.Save();
            c.Translate(pos.X, pos.Y);
            c.RotateDegrees(R(rng, -25, 25));
            using var heart = Heart(size);
            c.DrawPath(heart, p);
            c.Restore();
        }
    }

    private static SKPath Heart(float size)
    {
        var p = new SKPath();
        var s = size;
        p.MoveTo(0, s * 0.35f);
        p.CubicTo(-s * 0.9f, -s * 0.2f, -s * 0.45f, -s * 0.85f, 0, -s * 0.4f);
        p.CubicTo(s * 0.45f, -s * 0.85f, s * 0.9f, -s * 0.2f, 0, s * 0.35f);
        p.Close();
        return p;
    }

    private static void Summer(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var sun = new SKPoint(w * 0.18f, h * 0.2f);
        var sr = s * 0.1f;

        using (var glow = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(sun, sr * 3f, new[] { new SKColor(0xFF, 0xE0, 0x66, 150), SKColors.Transparent }, SKShaderTileMode.Clamp)
        })
        {
            c.DrawCircle(sun, sr * 3f, glow);
        }

        using (var ray = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xD5, 0x4F, 220), StrokeWidth = sr * 0.14f, StrokeCap = SKStrokeCap.Round })
        {
            for (var i = 0; i < 12; i++)
            {
                var a = i * Math.PI / 6;
                c.DrawLine(
                    sun.X + (float)(Math.Cos(a) * sr * 1.3), sun.Y + (float)(Math.Sin(a) * sr * 1.3),
                    sun.X + (float)(Math.Cos(a) * sr * 1.75), sun.Y + (float)(Math.Sin(a) * sr * 1.75),
                    ray);
            }
        }

        using (var disc = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xD2, 0x3F) })
        {
            c.DrawCircle(sun, sr, disc);
        }

        // Layered waves with white crests.
        var waves = new[]
        {
            (Top: h * 0.83f, Colour: new SKColor(0x1E, 0x88, 0xC9, 225)),
            (Top: h * 0.88f, Colour: new SKColor(0x29, 0xB6, 0xD8, 235)),
            (Top: h * 0.93f, Colour: new SKColor(0x5F, 0xD3, 0xE8, 245))
        };
        foreach (var (top, colour) in waves)
        {
            var amp = s * 0.018f;
            var len = s * R(rng, 0.2f, 0.28f);
            var phase = R(rng, 0, len);
            using var wave = new SKPath();
            wave.MoveTo(0, h);
            for (float x = 0; x <= w; x += 4)
            {
                wave.LineTo(x, top + (float)(Math.Sin((x + phase) / len * Math.PI * 2) * amp));
            }

            wave.LineTo(w, h);
            wave.Close();
            using var fill = new SKPaint { IsAntialias = true, Color = colour };
            c.DrawPath(wave, fill);

            using var crest = new SKPath();
            for (float x = 0; x <= w; x += 4)
            {
                var y = top + (float)(Math.Sin((x + phase) / len * Math.PI * 2) * amp);
                if (x == 0)
                {
                    crest.MoveTo(x, y);
                }
                else
                {
                    crest.LineTo(x, y);
                }
            }

            using var foam = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.006f, Color = SKColors.White.WithAlpha(190) };
            c.DrawPath(crest, foam);
        }
    }

    private static void Fireworks(SKCanvas c, int w, int h, SKColor tint, Random rng)
    {
        float s = Math.Min(w, h);
        var colours = new[]
        {
            new SKColor(0xFF, 0xD7, 0x40), new SKColor(0xFF, 0x52, 0x7A), new SKColor(0x64, 0xD8, 0xFF),
            new SKColor(0xB3, 0x88, 0xFF), new SKColor(0x69, 0xF0, 0xAE), SKColors.White
        };

        // Bursts in the sky above the title, alternating high and low, each with a short fading trail beneath.
        var count = w > h ? 5 : 4;
        for (var i = 0; i < count; i++)
        {
            var cx = (w / (float)count * (i + 0.5f)) + R(rng, -w * 0.03f, w * 0.03f);
            var cy = h * (i % 2 == 0 ? 0.12f : 0.18f);
            var radius = s * R(rng, 0.08f, 0.1f);
            var colour = colours[(i * 2 + rng.Next(2)) % colours.Length];
            Burst(c, cx, cy, radius, colour, rng);

            using var trail = new SKPaint
            {
                IsAntialias = true,
                StrokeWidth = s * 0.004f,
                StrokeCap = SKStrokeCap.Round,
                Shader = SKShader.CreateLinearGradient(new SKPoint(cx, cy + (radius * 1.4f)), new SKPoint(cx, cy + (radius * 0.35f)), new[] { SKColors.Transparent, colour.WithAlpha(150) }, SKShaderTileMode.Clamp)
            };
            c.DrawLine(cx, cy + (radius * 1.4f), cx, cy + (radius * 0.35f), trail);
        }

        // Sparks drifting down, off the title.
        using var spark = new SKPaint { IsAntialias = true };
        for (var i = 0; i < 70; i++)
        {
            float px = R(rng, 0, w), py = R(rng, 0, h * 0.8f);
            if (Math.Abs(px - (w / 2f)) < w * 0.46f && Math.Abs(py - (h / 2f)) < h * 0.2f)
            {
                continue;
            }

            spark.Color = colours[rng.Next(colours.Length)].WithAlpha((byte)rng.Next(120, 230));
            c.DrawCircle(px, py, R(rng, s * 0.002f, s * 0.005f), spark);
        }

        Skyline(c, w, h, s, rng, h * 0.08f, h * 0.18f, new SKColor(0xFF, 0xD5, 0x80, 170));
        ClockTower(c, w * 0.5f, h, s * 0.3f);
    }

    /// <summary>A clock tower standing on the bottom edge, its lit face showing midnight. height is to the spire tip.</summary>
    private static void ClockTower(SKCanvas c, float x, float bottom, float height)
    {
        var tw = height * 0.2f;
        using var stone = new SKPaint { IsAntialias = true, Color = new SKColor(0x0B, 0x0B, 0x16) };
        using var tower = new SKPath();
        tower.AddRect(new SKRect(x - (tw / 2), bottom - (height * 0.72f), x + (tw / 2), bottom));
        tower.AddRect(new SKRect(x - (tw * 0.6f), bottom - (height * 0.75f), x + (tw * 0.6f), bottom - (height * 0.71f)));
        tower.MoveTo(x - (tw * 0.55f), bottom - (height * 0.75f));
        tower.LineTo(x, bottom - height);
        tower.LineTo(x + (tw * 0.55f), bottom - (height * 0.75f));
        tower.Close();
        using (var merged = new SKPath())
        {
            tower.Simplify(merged);
            c.DrawPath(merged, stone);
        }

        // The clock face, glowing, with both hands straight up at twelve.
        var face = new SKPoint(x, bottom - (height * 0.6f));
        var r = tw * 0.36f;
        using (var glow = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xF1, 0xC4), ImageFilter = SKImageFilter.CreateDropShadow(0, 0, r * 0.3f, r * 0.3f, new SKColor(0xFF, 0xD5, 0x80, 200)) })
        {
            c.DrawCircle(face, r, glow);
        }

        using var ink = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, Color = new SKColor(0x1A, 0x14, 0x10) };
        for (var i = 0; i < 12; i++)
        {
            var a = i * Math.PI / 6;
            ink.StrokeWidth = r * (i % 3 == 0 ? 0.08f : 0.04f);
            c.DrawLine(face.X + (float)(Math.Sin(a) * r * 0.78f), face.Y - (float)(Math.Cos(a) * r * 0.78f), face.X + (float)(Math.Sin(a) * r * 0.92f), face.Y - (float)(Math.Cos(a) * r * 0.92f), ink);
        }

        ink.StrokeWidth = r * 0.09f;
        c.DrawLine(face.X, face.Y, face.X, face.Y - (r * 0.5f), ink);
        ink.StrokeWidth = r * 0.05f;
        c.DrawLine(face.X, face.Y, face.X, face.Y - (r * 0.72f), ink);
        c.DrawCircle(face, r * 0.07f, ink);

        // Lit windows down the shaft.
        using var lit = new SKPaint { Color = new SKColor(0xFF, 0xD5, 0x80, 170) };
        for (var wy = bottom - (height * 0.42f); wy < bottom - (height * 0.06f); wy += height * 0.09f)
        {
            c.DrawRect(x - (tw * 0.08f), wy, tw * 0.16f, height * 0.045f, lit);
        }
    }

    private static void Burst(SKCanvas c, float cx, float cy, float radius, SKColor colour, Random rng)
    {
        using (var glow = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(new SKPoint(cx, cy), radius * 1.3f, new[] { colour.WithAlpha(70), SKColors.Transparent }, SKShaderTileMode.Clamp)
        })
        {
            c.DrawCircle(cx, cy, radius * 1.3f, glow);
        }

        var rays = rng.Next(28, 40);
        using var ray = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = radius * 0.025f };
        using var tip = new SKPaint { IsAntialias = true, Color = colour };
        for (var i = 0; i < rays; i++)
        {
            var a = (i * Math.PI * 2 / rays) + R(rng, -0.05f, 0.05f);
            var inner = radius * R(rng, 0.15f, 0.3f);
            var outer = radius * R(rng, 0.8f, 1f);
            var x0 = cx + (float)(Math.Cos(a) * inner);
            var y0 = cy + (float)(Math.Sin(a) * inner);
            var x1 = cx + (float)(Math.Cos(a) * outer);
            var y1 = cy + (float)(Math.Sin(a) * outer) + (radius * 0.08f); // a little droop
            using var fade = SKShader.CreateLinearGradient(new SKPoint(x0, y0), new SKPoint(x1, y1), new[] { colour.WithAlpha(0), colour.WithAlpha(230) }, SKShaderTileMode.Clamp);
            ray.Shader = fade;
            c.DrawLine(x0, y0, x1, y1, ray);
            ray.Shader = null;
            c.DrawCircle(x1, y1, radius * 0.03f, tip);
        }
    }

    private static void Space(SKCanvas c, int w, int h, SKColor tint, Random rng)
    {
        StarBackdrop(c, w, h, rng);
        Planet(c, w, h);
    }

    /// <summary>Darkened sky, two soft nebula clouds, a starfield and a few glinting stars.</summary>
    private static void StarBackdrop(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);

        // Deep-space darkening and two soft nebula clouds.
        using (var dark = new SKPaint { Color = new SKColor(0x03, 0x04, 0x12, 120) })
        {
            c.DrawRect(0, 0, w, h, dark);
        }

        foreach (var (px, py, col) in new[] { (0.2f, 0.25f, new SKColor(0x8E, 0x44, 0xAD)), (0.8f, 0.7f, new SKColor(0x21, 0x96, 0xF3)) })
        {
            using var nebula = new SKPaint
            {
                IsAntialias = true,
                Shader = SKShader.CreateRadialGradient(new SKPoint(w * px, h * py), s * 0.45f, new[] { col.WithAlpha(90), SKColors.Transparent }, SKShaderTileMode.Clamp)
            };
            c.DrawRect(0, 0, w, h, nebula);
        }

        // Starfield.
        using var star = new SKPaint { IsAntialias = true };
        for (var i = 0; i < 260; i++)
        {
            star.Color = SKColors.White.WithAlpha((byte)rng.Next(90, 255));
            c.DrawCircle(R(rng, 0, w), R(rng, 0, h), R(rng, s * 0.0012f, s * 0.004f), star);
        }

        // A few bright stars with glints.
        using var glint = new SKPaint { IsAntialias = true, Color = SKColors.White, StrokeWidth = s * 0.0025f, StrokeCap = SKStrokeCap.Round };
        for (var i = 0; i < 6; i++)
        {
            float gx = R(rng, 0, w), gy = R(rng, 0, h * 0.75f), gr = s * R(rng, 0.015f, 0.03f);
            if (Math.Abs(gx - (w / 2f)) < (w * 0.46f) + gr && Math.Abs(gy - (h / 2f)) < (h * 0.2f) + gr)
            {
                continue; // keep off the title
            }

            c.DrawLine(gx - gr, gy, gx + gr, gy, glint);
            c.DrawLine(gx, gy - gr, gx, gy + gr, glint);
            c.DrawCircle(gx, gy, s * 0.004f, glint);
        }
    }

    private static void Planet(SKCanvas c, int w, int h)
    {
        float s = Math.Min(w, h);

        // Ringed planet low on one side, lit from the top left.
        var pc = new SKPoint(w * 0.83f, h * 0.84f);
        var pr = s * 0.17f;
        var ringRect = new SKRect(pc.X - (pr * 1.9f), pc.Y - (pr * 0.45f), pc.X + (pr * 1.9f), pc.Y + (pr * 0.45f));
        using var ring = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = pr * 0.12f, Color = new SKColor(0xE8, 0xD8, 0xB0, 200) };

        c.Save();
        c.RotateDegrees(-16, pc.X, pc.Y);
        c.ClipRect(new SKRect(ringRect.Left - 10, ringRect.Top - 10, ringRect.Right + 10, pc.Y), SKClipOperation.Intersect);
        c.DrawOval(ringRect, ring); // back half of the ring, behind the planet
        c.Restore();

        using (var planet = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(new SKPoint(pc.X - (pr * 0.4f), pc.Y - (pr * 0.4f)), pr * 1.5f, new[] { new SKColor(0xB3, 0xBC, 0xF0), new SKColor(0x3F, 0x51, 0xB5), new SKColor(0x0D, 0x0B, 0x24) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp)
        })
        {
            c.DrawCircle(pc, pr, planet);
        }

        c.Save();
        c.RotateDegrees(-16, pc.X, pc.Y);
        c.ClipRect(new SKRect(ringRect.Left - 10, pc.Y, ringRect.Right + 10, ringRect.Bottom + 10), SKClipOperation.Intersect);
        c.DrawOval(ringRect, ring); // front half, over the planet
        c.Restore();
    }

    // ---------- St Patrick's Day: a rainbow over the title, a pot of gold and shamrocks ----------

    private static void Shamrocks(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var greens = new[] { new SKColor(0x2E, 0x9E, 0x4F), new SKColor(0x4C, 0xC2, 0x6A), new SKColor(0x1B, 0x7A, 0x3C), new SKColor(0x8B, 0xD8, 0x7F) };

        Rainbow(c, w, h, s);
        Cloud(c, w * 0.03f, h * 0.34f, s * 0.08f);
        Cloud(c, w * 0.97f, h * 0.34f, s * 0.08f);

        // Gold glints, off the title.
        using (var gold = new SKPaint { IsAntialias = true })
        {
            for (var i = 0; i < 40; i++)
            {
                float gx = R(rng, 0, w), gy = R(rng, 0, h);
                if (Math.Abs(gx - (w / 2f)) < w * 0.46f && Math.Abs(gy - (h / 2f)) < h * 0.2f)
                {
                    continue;
                }

                gold.Color = new SKColor(0xFF, 0xD5, 0x4F, (byte)rng.Next(110, 220));
                c.DrawCircle(gx, gy, R(rng, s * 0.002f, s * 0.006f), gold);
            }
        }

        // Shamrocks around the bottom and sides, clear of the title, the pot and each other.
        var pot = new SKPoint(w / 2f, h * 0.985f);
        var sign = PubSign(c, w, h, s);
        var placed = new List<(float X, float Y, float R)> { (pot.X, pot.Y - (s * 0.12f), s * 0.17f), (sign.MidX, sign.MidY, (sign.Width * 0.62f) + (s * 0.02f)) };
        for (var i = 0; i < 16; i++)
        {
            var size = i < 5 ? s * R(rng, 0.07f, 0.1f) : s * R(rng, 0.035f, 0.055f);
            for (var attempt = 0; attempt < 40; attempt++)
            {
                if (FindClear(rng, placed, size * 1.1f, size, w - size, h * 0.4f, h - (size * 0.5f), 1.05f) is not { } at)
                {
                    break;
                }

                if (Math.Abs(at.X - (w / 2f)) < (w * 0.46f) + size && Math.Abs(at.Y - (h / 2f)) < (h * 0.2f) + size)
                {
                    continue;
                }

                Shamrock(c, at.X, at.Y, size, R(rng, -40, 40), greens[rng.Next(greens.Length)]);
                placed.Add((at.X, at.Y, size * 1.1f));
                break;
            }
        }

        PotOfGold(c, pot.X, pot.Y, s * 0.12f, rng);
    }

    /// <summary>
    /// A small pub sign hanging from a wrought-iron bracket on the right edge, below the title: green board, gold
    /// border and lettering. Returns the board's bounds so other decorations can keep clear of it.
    /// </summary>
    private static SKRect PubSign(SKCanvas c, int w, int h, float s)
    {
        var armY = h * 0.69f;
        var boardW = s * 0.2f;
        var boardH = s * 0.11f;
        var right = w - (s * 0.02f);
        var board = new SKRect(right - (s * 0.02f) - boardW, armY + (s * 0.03f), right - (s * 0.02f), armY + (s * 0.03f) + boardH);

        // Bracket: an arm out from the wall braced by a curve above it (clear of the board), and two short chains.
        using (var iron = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = s * 0.008f, Color = new SKColor(0x1C, 0x1C, 0x1C) })
        {
            c.DrawLine(w, armY, board.Left - (s * 0.01f), armY, iron);
            using var scroll = new SKPath();
            scroll.MoveTo(w, armY - (s * 0.06f));
            scroll.CubicTo(w - (s * 0.06f), armY - (s * 0.06f), w - (s * 0.08f), armY - (s * 0.01f), w - (s * 0.12f), armY);
            iron.StrokeWidth = s * 0.005f;
            c.DrawPath(scroll, iron);
            iron.StrokeWidth = s * 0.004f;
            c.DrawLine(board.Left + (boardW * 0.15f), armY, board.Left + (boardW * 0.15f), board.Top, iron);
            c.DrawLine(board.Right - (boardW * 0.15f), armY, board.Right - (boardW * 0.15f), board.Top, iron);
        }

        using (var green = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, board.Top), new SKPoint(0, board.Bottom), new[] { new SKColor(0x1B, 0x6B, 0x3A), new SKColor(0x0D, 0x40, 0x22) }, SKShaderTileMode.Clamp), ImageFilter = SKImageFilter.CreateDropShadow(0, s * 0.006f, s * 0.008f, s * 0.008f, SKColors.Black.WithAlpha(140)) })
        {
            c.DrawRoundRect(board, s * 0.01f, s * 0.01f, green);
        }

        using (var trim = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.004f, Color = new SKColor(0xD4, 0xAF, 0x37) })
        {
            var inner = board;
            inner.Inflate(-s * 0.008f, -s * 0.008f);
            c.DrawRoundRect(inner, s * 0.006f, s * 0.006f, trim);
        }

        using var font = new SKFont(TitleTypeface.Value, boardH * 0.3f);
        using var letters = new SKPaint { IsAntialias = true, Color = new SKColor(0xF2, 0xD0, 0x6B) };
        c.DrawText("PADDY'S", board.MidX, board.Top + (boardH * 0.46f), SKTextAlign.Center, font, letters);
        font.Size = boardH * 0.26f;
        c.DrawText("PUB", board.MidX, board.Top + (boardH * 0.82f), SKTextAlign.Center, font, letters);
        return board;
    }

    /// <summary>A rainbow arching across the sky above the title, its feet hidden in clouds at the edges.</summary>
    private static void Rainbow(SKCanvas c, int w, int h, float s)
    {
        var bands = new[] { new SKColor(0xE5, 0x39, 0x35), new SKColor(0xFB, 0x8C, 0x00), new SKColor(0xFD, 0xD8, 0x35), new SKColor(0x43, 0xA0, 0x47), new SKColor(0x1E, 0x88, 0xE5), new SKColor(0x8E, 0x24, 0xAA) };
        var band = s * 0.022f;
        var foot = h * 0.34f;
        c.Save();
        c.ClipRect(new SKRect(0, 0, w, foot));
        using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = band * 1.04f };
        for (var i = 0; i < bands.Length; i++)
        {
            var rx = (w * 0.56f) - (i * band);
            var ry = (h * 0.29f) - (i * band);
            paint.Color = bands[i].WithAlpha(185);
            c.DrawOval(new SKRect((w / 2f) - rx, foot - ry, (w / 2f) + rx, foot + ry), paint);
        }

        c.Restore();
    }

    /// <summary>A soft white cloud made of overlapping puffs, merged into one shape.</summary>
    private static void Cloud(SKCanvas c, float x, float y, float r)
    {
        using var puffs = new SKPath();
        foreach (var (dx, dy, pr) in new[] { (-0.8f, 0.15f, 0.55f), (-0.2f, -0.2f, 0.75f), (0.5f, -0.05f, 0.6f), (1.0f, 0.2f, 0.45f), (0.1f, 0.3f, 0.6f) })
        {
            puffs.AddCircle(x + (dx * r), y + (dy * r), pr * r);
        }

        using var merged = new SKPath();
        puffs.Simplify(merged);
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateLinearGradient(new SKPoint(0, y - r), new SKPoint(0, y + (r * 0.9f)), new[] { SKColors.White, new SKColor(0xD8, 0xE0, 0xEC) }, SKShaderTileMode.Clamp),
            ImageFilter = SKImageFilter.CreateDropShadow(0, r * 0.06f, r * 0.1f, r * 0.1f, SKColors.Black.WithAlpha(70))
        };
        c.DrawPath(merged, paint);
    }

    /// <summary>A black pot heaped with gold coins, sitting on the bottom edge at (x, bottom); r is about its radius.</summary>
    private static void PotOfGold(SKCanvas c, float x, float bottom, float r, Random rng)
    {
        var rimY = bottom - (r * 1.25f);

        // Coins heaped above the rim: back rows first, each coin a shaded disc with a raised edge.
        using var coin = new SKPaint { IsAntialias = true };
        for (var row = 0; row < 4; row++)
        {
            var rowY = rimY - (r * 0.42f) + (row * r * 0.12f);
            var half = r * (0.45f + (row * 0.15f));
            for (var cx = x - half; cx <= x + half; cx += r * 0.2f)
            {
                var cr = r * 0.14f;
                var px = cx + R(rng, -r * 0.03f, r * 0.03f);
                var py = rowY - (float)(Math.Cos((px - x) / (r * 1.1f) * Math.PI / 2) * r * 0.12f);
                using var coinShade = SKShader.CreateRadialGradient(new SKPoint(px - (cr * 0.3f), py - (cr * 0.3f)), cr * 1.3f, new[] { new SKColor(0xFF, 0xF1, 0x76), new SKColor(0xFF, 0xC1, 0x07), new SKColor(0xB8, 0x86, 0x0B) }, new[] { 0f, 0.55f, 1f }, SKShaderTileMode.Clamp);
                coin.Shader = coinShade;
                c.DrawOval(new SKRect(px - cr, py - (cr * 0.75f), px + cr, py + (cr * 0.75f)), coin);
            }
        }

        coin.Shader = null;

        // Pot body and rim.
        using var pot = new SKPath();
        pot.MoveTo(x - (r * 0.95f), rimY);
        pot.CubicTo(x - (r * 1.35f), rimY + (r * 0.5f), x - (r * 1.0f), bottom, x - (r * 0.5f), bottom);
        pot.LineTo(x + (r * 0.5f), bottom);
        pot.CubicTo(x + (r * 1.0f), bottom, x + (r * 1.35f), rimY + (r * 0.5f), x + (r * 0.95f), rimY);
        pot.Close();
        using (var body = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(x - (r * 0.4f), rimY + (r * 0.3f)), r * 1.5f, new[] { new SKColor(0x4A, 0x4A, 0x58), new SKColor(0x1C, 0x1C, 0x24), new SKColor(0x08, 0x08, 0x0C) }, new[] { 0f, 0.45f, 1f }, SKShaderTileMode.Clamp) })
        {
            c.DrawPath(pot, body);
        }

        using (var rim = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, rimY - (r * 0.1f)), new SKPoint(0, rimY + (r * 0.1f)), new[] { new SKColor(0x55, 0x55, 0x62), new SKColor(0x22, 0x22, 0x2A) }, SKShaderTileMode.Clamp) })
        {
            c.DrawRoundRect(new SKRect(x - (r * 1.02f), rimY - (r * 0.09f), x + (r * 1.02f), rimY + (r * 0.09f)), r * 0.09f, r * 0.09f, rim);
        }

        // Glints on the gold.
        using var glint = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = r * 0.02f, Color = SKColors.White.WithAlpha(230) };
        foreach (var (gx, gy) in new[] { (-0.3f, -0.62f), (0.25f, -0.5f), (0.02f, -0.75f) })
        {
            float px = x + (gx * r), py = rimY + (gy * r * 0.6f), gr = r * 0.07f;
            c.DrawLine(px - gr, py, px + gr, py, glint);
            c.DrawLine(px, py - gr, px, py + gr, glint);
        }
    }

    private static void Shamrock(SKCanvas c, float x, float y, float size, float rotation, SKColor colour)
    {
        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(rotation);

        using (var stem = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = size * 0.1f, Color = Darken(colour, 0.8f) })
        using (var stemPath = new SKPath())
        {
            stemPath.MoveTo(0, 0);
            stemPath.QuadTo(size * 0.15f, size * 0.55f, size * 0.05f, size * 1.05f);
            c.DrawPath(stemPath, stem);
        }

        // Three heart-shaped leaves with their points meeting in the middle.
        using var leaf = new SKPaint { IsAntialias = true, Color = colour };
        using var vein = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = size * 0.035f, Color = Lighten(colour, 0.35f).WithAlpha(180) };
        foreach (var angle in new[] { 0f, 120f, 240f })
        {
            c.Save();
            c.RotateDegrees(angle + 180);
            c.Translate(0, -size * 0.35f);
            using var heart = Heart(size * 0.95f);
            c.DrawPath(heart, leaf);
            c.DrawLine(0, size * 0.32f, 0, -size * 0.25f, vein);
            c.Restore();
        }

        c.Restore();
    }

    private static SKColor Darken(SKColor col, float factor)
        => new((byte)(col.Red * factor), (byte)(col.Green * factor), (byte)(col.Blue * factor));

    // ---------- Out of season: a night at the pictures (film strip, popcorn, clapperboard) ----------

    private static void Sparkle(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);

        // Soft bokeh and twinkles in the top band only, none touching.
        var placed = new List<(float X, float Y, float R)>();
        using (var bokeh = new SKPaint { IsAntialias = true })
        {
            for (var i = 0; i < 12; i++)
            {
                var r = s * R(rng, 0.02f, 0.05f);
                if (FindClear(rng, placed, r, 0, w, r, (h * 0.3f) - r, 1.1f) is { } at)
                {
                    bokeh.Color = SKColors.White.WithAlpha((byte)rng.Next(18, 40));
                    c.DrawCircle(at, r, bokeh);
                    placed.Add((at.X, at.Y, r));
                }
            }
        }

        using (var star = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(210) })
        {
            for (var i = 0; i < 9; i++)
            {
                var r = R(rng, s * 0.01f, s * 0.022f);
                if (FindClear(rng, placed, r, 0, w, r, (h * 0.3f) - r, 1.2f) is not { } at)
                {
                    continue;
                }

                using var p = new SKPath();
                p.MoveTo(at.X, at.Y - r);
                p.QuadTo(at.X, at.Y, at.X + r, at.Y);
                p.QuadTo(at.X, at.Y, at.X, at.Y + r);
                p.QuadTo(at.X, at.Y, at.X - r, at.Y);
                p.QuadTo(at.X, at.Y, at.X, at.Y - r);
                p.Close();
                c.DrawPath(p, star);
                placed.Add((at.X, at.Y, r));
            }
        }

        FilmStrip(c, w, h, s, rng);
        Popcorn(c, s * 0.15f, h * 0.995f, s * 0.2f, rng);
        Clapperboard(c, w - (s * 0.16f), h * 0.99f, s * 0.22f);
    }

    /// <summary>A strip of film running across the bottom, tilted a little, with sprocket holes and coloured frames.</summary>
    private static void FilmStrip(SKCanvas c, int w, int h, float s, Random rng)
    {
        var height = s * 0.13f;
        c.Save();
        c.RotateDegrees(-4, w / 2f, h * 0.86f);
        var strip = new SKRect(-w * 0.1f, (h * 0.86f) - (height / 2), w * 1.1f, (h * 0.86f) + (height / 2));
        using (var film = new SKPaint { IsAntialias = true, Color = new SKColor(0x12, 0x10, 0x14), ImageFilter = SKImageFilter.CreateDropShadow(0, s * 0.006f, s * 0.01f, s * 0.01f, SKColors.Black.WithAlpha(150)) })
        {
            c.DrawRect(strip, film);
        }

        using var hole = new SKPaint { IsAntialias = true, Color = new SKColor(0xE8, 0xE2, 0xD6) };
        var holeW = height * 0.1f;
        for (var x = strip.Left; x < strip.Right; x += height * 0.2f)
        {
            c.DrawRoundRect(new SKRect(x, strip.Top + (height * 0.05f), x + holeW, strip.Top + (height * 0.15f)), holeW * 0.25f, holeW * 0.25f, hole);
            c.DrawRoundRect(new SKRect(x, strip.Bottom - (height * 0.15f), x + holeW, strip.Bottom - (height * 0.05f)), holeW * 0.25f, holeW * 0.25f, hole);
        }

        var tones = new[] { (new SKColor(0xFF, 0xB7, 0x4D), new SKColor(0x8E, 0x24, 0xAA)), (new SKColor(0x4F, 0xC3, 0xF7), new SKColor(0x1A, 0x23, 0x7E)), (new SKColor(0xFF, 0x8A, 0x65), new SKColor(0x3E, 0x27, 0x23)), (new SKColor(0xA5, 0xD6, 0xA7), new SKColor(0x1B, 0x5E, 0x20)) };
        var frameW = height * 0.95f;
        var i = rng.Next(tones.Length);
        for (var x = strip.Left + (height * 0.05f); x < strip.Right; x += frameW + (height * 0.08f))
        {
            var (top, bottom) = tones[i++ % tones.Length];
            var frame = new SKRect(x, strip.Top + (height * 0.22f), x + frameW, strip.Bottom - (height * 0.22f));
            using var paint = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, frame.Top), new SKPoint(0, frame.Bottom), new[] { top, bottom }, SKShaderTileMode.Clamp) };
            c.DrawRoundRect(frame, height * 0.03f, height * 0.03f, paint);
        }

        c.Restore();
    }

    /// <summary>A striped popcorn box heaped with popcorn, standing on (x, bottom); size is the box height.</summary>
    private static void Popcorn(SKCanvas c, float x, float bottom, float size, Random rng)
    {
        var top = bottom - size;
        var topHalf = size * 0.42f;
        var baseHalf = size * 0.3f;

        // Popcorn first, so the box's rim covers the bottom of the heap.
        using var kernel = new SKPaint { IsAntialias = true };
        for (var row = 0; row < 3; row++)
        {
            var half = topHalf * (1 - (row * 0.25f));
            for (var px = x - half; px <= x + half; px += size * 0.09f)
            {
                var pr = size * R(rng, 0.065f, 0.085f);
                var py = top - (row * size * 0.09f) - R(rng, 0, size * 0.03f);
                using var kernelShade = SKShader.CreateRadialGradient(new SKPoint(px - (pr * 0.3f), py - (pr * 0.3f)), pr * 1.3f, new[] { new SKColor(0xFF, 0xFD, 0xF2), new SKColor(0xFF, 0xF0, 0xC8), new SKColor(0xE8, 0xC8, 0x80) }, new[] { 0f, 0.6f, 1f }, SKShaderTileMode.Clamp);
                kernel.Shader = kernelShade;
                c.DrawCircle(px + R(rng, -size * 0.015f, size * 0.015f), py, pr, kernel);
            }
        }

        kernel.Shader = null;

        using var box = new SKPath();
        box.MoveTo(x - topHalf, top);
        box.LineTo(x + topHalf, top);
        box.LineTo(x + baseHalf, bottom);
        box.LineTo(x - baseHalf, bottom);
        box.Close();
        using (var white = new SKPaint { IsAntialias = true, Color = new SKColor(0xF7, 0xF4, 0xEE), ImageFilter = SKImageFilter.CreateDropShadow(0, size * 0.02f, size * 0.03f, size * 0.03f, SKColors.Black.WithAlpha(130)) })
        {
            c.DrawPath(box, white);
        }

        // Red stripes running down the box, narrowing with it.
        c.Save();
        c.ClipPath(box, antialias: true);
        using (var red = new SKPaint { IsAntialias = true, Color = new SKColor(0xD3, 0x2F, 0x2F) })
        {
            const int Stripes = 7;
            for (var k = 0; k < Stripes; k += 2)
            {
                float t0 = (float)k / Stripes, t1 = (float)(k + 1) / Stripes;
                using var stripe = new SKPath();
                stripe.MoveTo(x - topHalf + (t0 * topHalf * 2), top);
                stripe.LineTo(x - topHalf + (t1 * topHalf * 2), top);
                stripe.LineTo(x - baseHalf + (t1 * baseHalf * 2), bottom);
                stripe.LineTo(x - baseHalf + (t0 * baseHalf * 2), bottom);
                stripe.Close();
                c.DrawPath(stripe, red);
            }
        }

        c.Restore();
    }

    /// <summary>A film clapperboard standing on (x, bottom), its clapper stick raised; size is its width.</summary>
    private static void Clapperboard(SKCanvas c, float x, float bottom, float size)
    {
        var bh = size * 0.68f;
        var board = new SKRect(x - (size / 2), bottom - bh, x + (size / 2), bottom);
        using (var slate = new SKPaint { IsAntialias = true, Color = new SKColor(0x1E, 0x1E, 0x24), ImageFilter = SKImageFilter.CreateDropShadow(0, size * 0.02f, size * 0.03f, size * 0.03f, SKColors.Black.WithAlpha(130)) })
        {
            c.DrawRoundRect(board, size * 0.03f, size * 0.03f, slate);
        }

        // Chalk lines dividing the board into fields.
        using (var chalk = new SKPaint { IsAntialias = true, StrokeWidth = size * 0.012f, Color = new SKColor(0xF0, 0xF0, 0xF0, 200) })
        {
            var row1 = board.Top + (bh * 0.42f);
            var row2 = board.Top + (bh * 0.7f);
            c.DrawLine(board.Left + (size * 0.05f), row1, board.Right - (size * 0.05f), row1, chalk);
            c.DrawLine(board.Left + (size * 0.05f), row2, board.Right - (size * 0.05f), row2, chalk);
            c.DrawLine(board.MidX, row1, board.MidX, row2, chalk);
        }

        // Striped bands: one along the top of the board, one on the raised stick hinged at the left.
        void Band(SKRect rect)
        {
            using var black = new SKPaint { IsAntialias = true, Color = new SKColor(0x1E, 0x1E, 0x24) };
            using var white = new SKPaint { IsAntialias = true, Color = new SKColor(0xF5, 0xF5, 0xF5) };
            c.DrawRect(rect, black);
            c.Save();
            c.ClipRect(rect);
            for (var sx = rect.Left - rect.Height; sx < rect.Right; sx += rect.Height * 1.6f)
            {
                using var stripe = new SKPath();
                stripe.MoveTo(sx, rect.Bottom);
                stripe.LineTo(sx + (rect.Height * 0.8f), rect.Bottom);
                stripe.LineTo(sx + (rect.Height * 1.6f), rect.Top);
                stripe.LineTo(sx + (rect.Height * 0.8f), rect.Top);
                stripe.Close();
                c.DrawPath(stripe, white);
            }

            c.Restore();
        }

        var bandH = size * 0.14f;
        Band(new SKRect(board.Left, board.Top - bandH, board.Right, board.Top));
        c.Save();
        c.RotateDegrees(-18, board.Left, board.Top - bandH);
        Band(new SKRect(board.Left, board.Top - (bandH * 2.1f), board.Right, board.Top - (bandH * 1.1f)));
        c.Restore();
        using var hinge = new SKPaint { IsAntialias = true, Color = new SKColor(0x9E, 0x9E, 0x9E) };
        c.DrawCircle(board.Left + (size * 0.03f), board.Top - bandH, size * 0.025f, hinge);
    }

    // ---------- helpers ----------

    /// <summary>Night sky over the background, <paramref name="top"/> fading to <paramref name="bottom"/>.</summary>
    private static void NightSky(SKCanvas c, int w, int h, SKColor top, SKColor bottom)
    {
        using var sky = new SKPaint { Shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, h), new[] { top, bottom }, SKShaderTileMode.Clamp) };
        c.DrawRect(0, 0, w, h, sky);
    }

    private static void Stars(SKCanvas c, int w, float maxY, int count, float s, Random rng)
    {
        using var star = new SKPaint { IsAntialias = true };
        for (var i = 0; i < count; i++)
        {
            star.Color = SKColors.White.WithAlpha((byte)rng.Next(70, 230));
            c.DrawCircle(R(rng, 0, w), R(rng, 0, maxY), R(rng, s * 0.001f, s * 0.0035f), star);
        }
    }

    /// <summary>A full moon lit from the upper left, with a glow, darker seas and craters.</summary>
    private static void Moon(SKCanvas c, float x, float y, float r, Random rng)
    {
        using (var glow = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(new SKPoint(x, y), r * 2.6f, new[] { new SKColor(0xFF, 0xF3, 0xC4, 110), SKColors.Transparent }, SKShaderTileMode.Clamp)
        })
        {
            c.DrawCircle(x, y, r * 2.6f, glow);
        }

        using (var body = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(new SKPoint(x - (r * 0.35f), y - (r * 0.35f)), r * 1.5f, new[] { new SKColor(0xFF, 0xF8, 0xDC), new SKColor(0xF3, 0xE3, 0xA6), new SKColor(0xCF, 0xB8, 0x72) }, new[] { 0f, 0.55f, 1f }, SKShaderTileMode.Clamp)
        })
        {
            c.DrawCircle(x, y, r, body);
        }

        using var disc = new SKPath();
        disc.AddCircle(x, y, r);
        c.Save();
        c.ClipPath(disc, antialias: true);

        // Seas: big soft blotches.
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.06f))
        using (var sea = new SKPaint { IsAntialias = true, Color = new SKColor(0xB8, 0xA2, 0x66, 70), MaskFilter = blur })
        {
            foreach (var (dx, dy, sr) in new[] { (-0.2f, -0.25f, 0.32f), (0.25f, 0.05f, 0.28f), (-0.1f, 0.35f, 0.22f) })
            {
                c.DrawOval(new SKRect(x + (r * (dx - sr)), y + (r * (dy - (sr * 0.8f))), x + (r * (dx + sr)), y + (r * (dy + (sr * 0.8f)))), sea);
            }
        }

        // Craters: soft shallow hollows, each with a faint lit lower-right edge, kept apart from each other.
        using var softEdge = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.015f);
        using var pit = new SKPaint { IsAntialias = true, Color = new SKColor(0xB0, 0x98, 0x5C, 85), MaskFilter = softEdge };
        using var lit = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xF8, 0xE0, 70), MaskFilter = softEdge };
        var craters = new List<(float X, float Y, float R)>();
        for (var i = 0; i < 7; i++)
        {
            var cr = r * R(rng, 0.05f, 0.12f);
            if (FindClear(rng, craters, cr, x - (r * 0.75f), x + (r * 0.75f), y - (r * 0.75f), y + (r * 0.75f), 1.4f) is not { } at
                || Math.Sqrt(((at.X - x) * (at.X - x)) + ((at.Y - y) * (at.Y - y))) > r * 0.8f - cr)
            {
                continue;
            }

            c.DrawCircle(at.X + (cr * 0.18f), at.Y + (cr * 0.18f), cr, lit);
            c.DrawCircle(at.X, at.Y, cr, pit);
            craters.Add((at.X, at.Y, cr));
        }

        c.Restore();
    }

    /// <summary>Soft mist drifting along the ground, from <paramref name="top"/> down.</summary>
    private static void Mist(SKCanvas c, int w, int h, float top, float s, Random rng, byte alpha = 60)
    {
        using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.03f);
        using var mist = new SKPaint { IsAntialias = true, MaskFilter = blur };
        for (var i = 0; i < 10; i++)
        {
            mist.Color = new SKColor(0xD8, 0xE0, 0xF0, (byte)(alpha * R(rng, 0.6f, 1.2f)));
            float mx = R(rng, 0, w), my = R(rng, top, h), mw = s * R(rng, 0.2f, 0.45f), mh = s * R(rng, 0.025f, 0.055f);
            c.DrawOval(new SKRect(mx - mw, my - mh, mx + mw, my + mh), mist);
        }
    }

    /// <summary>
    /// Covers the top of <paramref name="shape"/> in snow: the shape in white, with its own colour drawn again a
    /// little lower, so a band of snow is left along every upward edge.
    /// </summary>
    private static void SnowCap(SKCanvas c, SKPath shape, SKPaint fill, float depth)
    {
        using var snow = new SKPaint { IsAntialias = true, Color = new SKColor(0xF4, 0xF8, 0xFF) };
        c.Save();
        c.ClipPath(shape, antialias: true);
        c.DrawPath(shape, snow);
        c.Translate(0, depth);
        c.DrawPath(shape, fill);
        c.Restore();
    }

    /// <summary>
    /// A position for a decoration of radius <paramref name="r"/> that keeps clear of what's already placed,
    /// or null after enough tries. The caller adds it to <paramref name="placed"/> once drawn.
    /// </summary>
    private static SKPoint? FindClear(Random rng, List<(float X, float Y, float R)> placed, float r, float minX, float maxX, float minY, float maxY, float gap = 1.05f)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            float x = R(rng, minX, maxX), y = R(rng, minY, maxY);
            var clear = true;
            foreach (var o in placed)
            {
                if (Math.Sqrt(((x - o.X) * (x - o.X)) + ((y - o.Y) * (y - o.Y))) < (r + o.R) * gap)
                {
                    clear = false;
                    break;
                }
            }

            if (clear)
            {
                return new SKPoint(x, y);
            }
        }

        return null;
    }

    private static SKPath Hills(int w, int h, float top, float amp, int bumps, Random rng)
    {
        var p = new SKPath();
        p.MoveTo(0, h);
        p.LineTo(0, top);
        var step = w / (float)bumps;
        for (var i = 0; i < bumps; i++)
        {
            var x0 = i * step;
            p.QuadTo(x0 + (step / 2), top - (amp * R(rng, 0.6f, 1.4f)), x0 + step, top + R(rng, -amp * 0.3f, amp * 0.3f));
        }

        p.LineTo(w, h);
        p.Close();
        return p;
    }

    private static SKPath Triangle(float cx, float cy, float r, bool pointDown)
    {
        var p = new SKPath();
        var d = pointDown ? 1 : -1;
        p.MoveTo(cx, cy + (d * r));
        p.LineTo(cx - r, cy - (d * r * 0.7f));
        p.LineTo(cx + r, cy - (d * r * 0.7f));
        p.Close();
        return p;
    }

    private static float R(Random rng, float min, float max) => min + ((float)rng.NextDouble() * (max - min));

    internal static SKColor Lighten(SKColor c, float amount)
        => new((byte)(c.Red + ((255 - c.Red) * amount)), (byte)(c.Green + ((255 - c.Green) * amount)), (byte)(c.Blue + ((255 - c.Blue) * amount)));

    private static int StableHash(string s)
    {
        unchecked
        {
            var hash = 17;
            foreach (var ch in s)
            {
                hash = (hash * 31) + ch;
            }

            return hash;
        }
    }
}
