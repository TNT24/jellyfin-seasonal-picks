using System;
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

    public static void DrawTheme(SKCanvas canvas, CoverTheme theme, int w, int h, SKColor tint, string seed)
    {
        var rng = new Random(StableHash(seed));
        switch (theme)
        {
            case CoverTheme.Snow: Snow(canvas, w, h, rng); break;
            case CoverTheme.Halloween: Halloween(canvas, w, h, rng); break;
            case CoverTheme.Easter: Easter(canvas, w, h, rng); break;
            case CoverTheme.Hearts: Hearts(canvas, w, h, tint, rng); break;
            case CoverTheme.Summer: Summer(canvas, w, h, rng); break;
            case CoverTheme.Fireworks: Fireworks(canvas, w, h, tint, rng); break;
            case CoverTheme.Shamrock: Shamrocks(canvas, w, h, rng); break;
            case CoverTheme.Space: Space(canvas, w, h, tint, rng); break;
            case CoverTheme.Sparkle: Sparkle(canvas, w, h, rng); break;
            case CoverTheme.Christmas: Snow(canvas, w, h, rng); ChristmasTree(canvas, w, h, rng); break;
            case CoverTheme.Lightsabers: StarBackdrop(canvas, w, h, rng); Lightsabers(canvas, w, h); break;
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

    private static void Snow(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);

        // Falling snow.
        using var dot = new SKPaint { IsAntialias = true };
        for (var i = 0; i < 170; i++)
        {
            dot.Color = SKColors.White.WithAlpha((byte)rng.Next(120, 235));
            c.DrawCircle(R(rng, 0, w), R(rng, 0, h * 0.9f), R(rng, s * 0.002f, s * 0.008f), dot);
        }

        // Snowflakes.
        using var flake = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, Color = SKColors.White.WithAlpha(215) };
        for (var i = 0; i < 11; i++)
        {
            var r = R(rng, s * 0.022f, s * 0.05f);
            flake.StrokeWidth = r * 0.12f;
            Snowflake(c, R(rng, 0, w), R(rng, 0, h * 0.8f), r, R(rng, 0, 60), flake);
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

        // Moon with a glow.
        var moon = new SKPoint(w * 0.82f, h * 0.2f);
        var mr = s * 0.11f;
        using (var glow = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(moon, mr * 2.4f, new[] { new SKColor(0xFF, 0xF3, 0xC4, 120), SKColors.Transparent }, SKShaderTileMode.Clamp)
        })
        {
            c.DrawCircle(moon, mr * 2.4f, glow);
        }

        using (var moonPaint = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xF1, 0xB8, 240) })
        {
            c.DrawCircle(moon, mr, moonPaint);
        }

        // Bats.
        using var bat = new SKPaint { IsAntialias = true, Color = new SKColor(0x12, 0x08, 0x10, 235) };
        // Spread out: each bat keeps clear of the others and of the moon.
        var placed = new System.Collections.Generic.List<(float X, float Y, float R)> { (moon.X, moon.Y, mr) };
        for (var i = 0; i < 6; i++)
        {
            var br = R(rng, s * 0.045f, s * 0.085f);
            for (var attempt = 0; attempt < 40; attempt++)
            {
                float bx = R(rng, w * 0.06f, w * 0.94f), by = R(rng, h * 0.06f, h * 0.32f);
                var clear = true;
                foreach (var o in placed)
                {
                    if (Math.Sqrt(((bx - o.X) * (bx - o.X)) + ((by - o.Y) * (by - o.Y))) < (br + o.R) * 1.15)
                    {
                        clear = false;
                        break;
                    }
                }

                if (clear)
                {
                    Bat(c, bx, by, br, R(rng, -20, 20), bat);
                    placed.Add((bx, by, br));
                    break;
                }
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
        using var body = new SKPaint { IsAntialias = true, Color = new SKColor(0xF2, 0x7A, 0x1A) };
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

        var eggsRight = bunnyX - (bunnyUnit * 0.5f);
        var count = w > h ? 5 : 3;
        for (var i = 0; i < count; i++)
        {
            var ew = s * R(rng, 0.095f, 0.125f);
            var x = (eggsRight / count * (i + 0.5f)) + R(rng, -eggsRight * 0.03f, eggsRight * 0.03f);
            Egg(c, x, h * 0.915f, ew, R(rng, -18, 18), pastels[i % pastels.Length], pastels[(i + 3) % pastels.Length]);
        }

        Grass(c, w, h, h * 0.955f, s * 0.03f, front, rng);
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

        // Body: a round haunch with the chest rising towards the head.
        c.DrawOval(new SKRect(x - (u * 0.2f), ground - (u * 0.46f), x + (u * 0.2f), ground), furPaint);
        c.DrawOval(new SKRect(x - (u * 0.34f), ground - (u * 0.52f), x - (u * 0.06f), ground - (u * 0.08f)), furPaint);

        // Tail.
        using (var tail = new SKPaint { IsAntialias = true, Color = SKColors.White })
        {
            c.DrawCircle(x + (u * 0.2f), ground - (u * 0.16f), u * 0.065f, tail);
        }

        // Head.
        c.DrawOval(new SKRect(x - (u * 0.42f), ground - (u * 0.66f), x - (u * 0.14f), ground - (u * 0.4f)), furPaint);

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

        // Front paw.
        c.DrawOval(new SKRect(x - (u * 0.31f), ground - (u * 0.07f), x - (u * 0.17f), ground + (u * 0.01f)), furPaint);
    }

    private static void Egg(SKCanvas c, float cx, float bottom, float width, float rotation, SKColor color, SKColor band)
    {
        var height = width * 1.35f;
        c.Save();
        c.Translate(cx, bottom - (height / 2));
        c.RotateDegrees(rotation);
        var rect = new SKRect(-width / 2, -height / 2, width / 2, height / 2);
        using var egg = new SKPath();
        egg.AddOval(rect);

        using (var fill = new SKPaint { IsAntialias = true, Color = color })
        {
            c.DrawPath(egg, fill);
        }

        c.Save();
        c.ClipPath(egg, antialias: true);
        using (var zig = new SKPaint { IsAntialias = true, Color = band, Style = SKPaintStyle.Stroke, StrokeWidth = width * 0.09f, StrokeJoin = SKStrokeJoin.Round })
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

        using (var dots = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(210) })
        {
            c.DrawCircle(-width * 0.2f, -height * 0.33f, width * 0.05f, dots);
            c.DrawCircle(width * 0.15f, -height * 0.36f, width * 0.04f, dots);
            c.DrawCircle(0, height * 0.02f, width * 0.05f, dots);
            c.DrawCircle(width * 0.22f, height * 0.36f, width * 0.04f, dots);
        }

        c.Restore();
        c.Restore();
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
        for (var i = 0; i < 34; i++)
        {
            var big = i < 4;
            var size = big ? R(rng, s * 0.09f, s * 0.14f) : R(rng, s * 0.02f, s * 0.06f);
            var x = R(rng, 0, w);
            var y = R(rng, 0, h);
            if (big && Math.Abs(y - (h / 2f)) < h * 0.2f)
            {
                y = y < h / 2f ? h * 0.15f : h * 0.85f; // keep the big ones off the title
            }

            p.Color = colours[rng.Next(colours.Length)].WithAlpha((byte)(big ? 215 : rng.Next(110, 220)));
            c.Save();
            c.Translate(x, y);
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

        // Bursts spread along the top two-thirds, kept away from each other.
        var count = w > h ? 5 : 4;
        for (var i = 0; i < count; i++)
        {
            var cx = (w / (float)count * (i + 0.5f)) + R(rng, -w * 0.05f, w * 0.05f);
            var cy = i % 2 == 0 ? R(rng, h * 0.12f, h * 0.24f) : R(rng, h * 0.3f, h * 0.42f);
            var radius = s * R(rng, 0.11f, 0.17f);
            var colour = colours[rng.Next(colours.Length)];
            Burst(c, cx, cy, radius, colour, rng);

            // Rising trail below the burst.
            using var trail = new SKPaint
            {
                IsAntialias = true,
                StrokeWidth = s * 0.004f,
                StrokeCap = SKStrokeCap.Round,
                Shader = SKShader.CreateLinearGradient(new SKPoint(cx, h * 0.85f), new SKPoint(cx, cy + (radius * 0.4f)), new[] { SKColors.Transparent, colour.WithAlpha(150) }, SKShaderTileMode.Clamp)
            };
            c.DrawLine(cx + R(rng, -s * 0.02f, s * 0.02f), h * 0.85f, cx, cy + (radius * 0.4f), trail);
        }

        // Sparks drifting down.
        using var spark = new SKPaint { IsAntialias = true };
        for (var i = 0; i < 70; i++)
        {
            spark.Color = colours[rng.Next(colours.Length)].WithAlpha((byte)rng.Next(120, 230));
            c.DrawCircle(R(rng, 0, w), R(rng, 0, h * 0.8f), R(rng, s * 0.002f, s * 0.005f), spark);
        }

        // City skyline along the bottom, with a few lit windows.
        using var city = new SKPaint { IsAntialias = true, Color = new SKColor(0x0B, 0x0B, 0x16, 245) };
        using var window = new SKPaint { Color = new SKColor(0xFF, 0xD5, 0x80, 170) };
        var x = 0f;
        while (x < w)
        {
            var bw = s * R(rng, 0.05f, 0.11f);
            var bh = h * R(rng, 0.08f, 0.2f);
            c.DrawRect(x, h - bh, bw + 1, bh, city);
            for (var wy = h - bh + (s * 0.02f); wy < h - (s * 0.02f); wy += s * 0.03f)
            {
                for (var wx = x + (s * 0.012f); wx < x + bw - (s * 0.015f); wx += s * 0.022f)
                {
                    if (rng.NextDouble() < 0.28)
                    {
                        c.DrawRect(wx, wy, s * 0.008f, s * 0.012f, window);
                    }
                }
            }

            x += bw;
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

    private static void Shamrocks(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var greens = new[] { new SKColor(0x2E, 0x9E, 0x4F), new SKColor(0x4C, 0xC2, 0x6A), new SKColor(0x1B, 0x7A, 0x3C), new SKColor(0x8B, 0xD8, 0x7F) };

        // Gold glints.
        using var gold = new SKPaint { IsAntialias = true };
        for (var i = 0; i < 40; i++)
        {
            gold.Color = new SKColor(0xFF, 0xD5, 0x4F, (byte)rng.Next(110, 220));
            c.DrawCircle(R(rng, 0, w), R(rng, 0, h), R(rng, s * 0.002f, s * 0.006f), gold);
        }

        // Shamrocks scattered around the edges, the big ones kept off the title.
        var placed = new System.Collections.Generic.List<(float X, float Y, float R)> { (w / 2f, h / 2f, s * 0.28f) };
        for (var i = 0; i < 16; i++)
        {
            var size = i < 5 ? s * R(rng, 0.08f, 0.12f) : s * R(rng, 0.035f, 0.065f);
            for (var attempt = 0; attempt < 40; attempt++)
            {
                float x = R(rng, 0, w), y = R(rng, 0, h);
                var clear = true;
                foreach (var o in placed)
                {
                    if (Math.Sqrt(((x - o.X) * (x - o.X)) + ((y - o.Y) * (y - o.Y))) < (size + o.R) * 1.05)
                    {
                        clear = false;
                        break;
                    }
                }

                if (clear)
                {
                    Shamrock(c, x, y, size, R(rng, -40, 40), greens[rng.Next(greens.Length)]);
                    placed.Add((x, y, size));
                    break;
                }
            }
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

    private static void Sparkle(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        using var bokeh = new SKPaint { IsAntialias = true };
        for (var i = 0; i < 22; i++)
        {
            bokeh.Color = SKColors.White.WithAlpha((byte)rng.Next(14, 40));
            c.DrawCircle(R(rng, 0, w), R(rng, 0, h), R(rng, s * 0.03f, s * 0.1f), bokeh);
        }

        using var star = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(210) };
        for (var i = 0; i < 14; i++)
        {
            var x = R(rng, 0, w);
            var y = R(rng, 0, h);
            var r = R(rng, s * 0.008f, s * 0.022f);
            using var p = new SKPath();
            p.MoveTo(x, y - r);
            p.QuadTo(x, y, x + r, y);
            p.QuadTo(x, y, x, y + r);
            p.QuadTo(x, y, x - r, y);
            p.QuadTo(x, y, x, y - r);
            p.Close();
            c.DrawPath(p, star);
        }
    }

    // ---------- helpers ----------

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
