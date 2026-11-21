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
        CoverTheme.Halloween => new[] { "Pumpkin patch", "Graveyard", "Haunted house", "Witch's cauldron", "Horror" },
        CoverTheme.Christmas => new[] { "Christmas tree", "Fireplace", "Santa's sleigh", "Baubles and holly", "Paper cut-out" },
        CoverTheme.Snow => new[] { "Snowfall", "Snowy forest" },
        CoverTheme.Easter => new[] { "Bunny and eggs", "Basket of chicks" },
        CoverTheme.Hearts => new[] { "Floating hearts", "Hearts and roses", "Heart balloons", "Neon sign" },
        CoverTheme.LunarNewYear => new[] { "Red lanterns", "Lantern festival" },
        CoverTheme.Summer => new[] { "Sunset beach", "Retro poster" },
        CoverTheme.Fireworks => new[] { "Fireworks", "Retro poster", "Neon sign" },
        CoverTheme.Bonfire => new[] { "Bonfire", "Retro poster" },
        CoverTheme.Shamrock => new[] { "Irish hills", "Retro poster" },
        CoverTheme.Lightsabers => new[] { "Crossed blades", "Retro poster" },
        CoverTheme.Ghosts => new[] { "Haunted city", "Retro poster" },
        CoverTheme.Castle => new[] { "Castle on the lake", "Retro poster" },
        CoverTheme.Pancakes => new[] { "Pancake stack", "Retro poster" },
        _ => new[] { "Standard" }
    };

    /// <summary>
    /// The scene to draw: the one the design pins (1-based), else the step (week of the season plus the year) picks it
    /// from a shuffled order. Every scene comes round once per cycle, a scene never shows two steps running, and the
    /// order shifts each cycle so the styles mix rather than always following each other the same way.
    /// </summary>
    public static int SceneFor(CoverTheme theme, int pinned, int week)
    {
        var count = Scenes(theme).Length;
        if (pinned > 0)
        {
            return (pinned - 1) % count;
        }

        var step = Math.Max(0, week);
        if (count < 3)
        {
            return step % count;
        }

        // A fixed shuffle for the decoration, rotated by one each cycle: with three or more scenes the last of one
        // cycle is never the first of the next.
        var order = Enumerable.Range(0, count).ToArray();
        var shuffle = new Random(((int)theme * 7919) + count);
        for (var i = count - 1; i > 0; i--)
        {
            var j = shuffle.Next(i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }

        var cycle = step / count;
        return order[((step % count) + cycle) % count];
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
            case CoverTheme.Halloween when scene == 4: HorrorHalloween(canvas, w, h, rng); break;
            case CoverTheme.Christmas when scene == 4: PaperChristmas(canvas, w, h, rng); break;
            case CoverTheme.Hearts when scene == 3: NeonValentine(canvas, w, h, rng); break;
            case CoverTheme.Fireworks when scene == 2: NeonNewYear(canvas, w, h, rng); break;
            case CoverTheme.Christmas when scene == 1: Fireplace(canvas, w, h, rng); break;
            case CoverTheme.Christmas when scene == 2: SantaSleigh(canvas, w, h, rng); break;
            case CoverTheme.Christmas when scene == 3: BaublesAndHolly(canvas, w, h, rng); break;
            case CoverTheme.Snow when scene == 1: SnowyForest(canvas, w, h, rng); break;
            case CoverTheme.Easter when scene == 1: ChickBasket(canvas, w, h, rng); break;
            case CoverTheme.Hearts when scene == 1: HeartsAndRoses(canvas, w, h, tint, rng); break;
            case CoverTheme.Hearts when scene == 2: HeartBalloons(canvas, w, h, tint, rng); break;
            case CoverTheme.LunarNewYear when scene == 1: LanternFestival(canvas, w, h, rng); break;
            case CoverTheme.Summer when scene == 1: RetroSummer(canvas, w, h, rng); break;
            case CoverTheme.Fireworks when scene == 1: RetroNewYear(canvas, w, h, rng); break;
            case CoverTheme.Bonfire when scene == 1: RetroBonfire(canvas, w, h, rng); break;
            case CoverTheme.Shamrock when scene == 1: RetroShamrockHills(canvas, w, h, rng); break;
            case CoverTheme.Lightsabers when scene == 1: RetroSabers(canvas, w, h, rng); break;
            case CoverTheme.Ghosts when scene == 1: RetroGhostCity(canvas, w, h, rng); break;
            case CoverTheme.Castle when scene == 1: RetroCastle(canvas, w, h, rng); break;
            case CoverTheme.Pancakes when scene == 1: RetroPancakes(canvas, w, h, rng); break;
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
            case CoverTheme.Sparkle: Sparkle(canvas, w, h, tint, rng); break;
            case CoverTheme.Christmas: Snow(canvas, w, h, rng, treeCorner: true); ChristmasTree(canvas, w, h, rng); break;
            case CoverTheme.Lightsabers: FestSaberDuel(canvas, w, h, rng); break;
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

    /// <summary>
    /// Snowfall: a moonlit valley with a lit cottage (see WinterSnowScene). With <paramref name="treeCorner"/>, only the
    /// sky and far hills, for the Christmas tree scene to stand in.
    /// </summary>
    private static void Snow(SKCanvas c, int w, int h, Random rng, bool treeCorner = false) => WinterSnowScene(c, w, h, rng, backdropOnly: treeCorner);

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

    /// <summary>A pumpkin patch at dusk (drawn in CoverArtSpooky.cs).</summary>
    private static void Halloween(SKCanvas c, int w, int h, Random rng) => SpookyPumpkinPatch(c, w, h, rng);

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

    /// <summary>New Year: fireworks over a city skyline and river (see WinterFireworksScene).</summary>
    private static void Fireworks(SKCanvas c, int w, int h, SKColor tint, Random rng) => WinterFireworksScene(c, w, h, rng);

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
        FestSpace(c, w, h, tint, rng);
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

    private static SKColor Darken(SKColor col, float factor)
        => new((byte)(col.Red * factor), (byte)(col.Green * factor), (byte)(col.Blue * factor));

    // ---------- Out of season: a night at the pictures (film strip, popcorn, clapperboard) ----------

    private static void Sparkle(SKCanvas c, int w, int h, SKColor tint, Random rng)
    {
        FestCinema(c, w, h, tint, rng);
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

    /// <summary>
    /// A stone river wall along <paramref name="y"/>, so buildings on the bank stand on something rather than float on
    /// the water: a coping stone along the top caught by the light, block joints, and a dark wet line at its foot.
    /// </summary>
    private static void RiverWall(SKCanvas c, int w, float y, float s, SKColor stone, SKColor light)
    {
        var height = s * 0.03f;
        var face = new SKRect(-2, y - (height * 0.35f), w + 2, y + (height * 0.65f));
        using (var shader = SKShader.CreateLinearGradient(new SKPoint(0, face.Top), new SKPoint(0, face.Bottom), new[] { stone, Darken(stone, 0.45f) }, null, SKShaderTileMode.Clamp))
        using (var wall = new SKPaint { IsAntialias = true, Shader = shader })
        {
            c.DrawRect(face, wall);
        }

        using (var joint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = Math.Max(1, s * 0.0012f), Color = SKColors.Black.WithAlpha(90) })
        {
            var mid = face.Top + (face.Height * 0.5f);
            c.DrawLine(face.Left, mid, face.Right, mid, joint);
            var block = s * 0.05f;
            for (var x = 0f; x < w; x += block)
            {
                c.DrawLine(x, face.Top, x, mid, joint);
                c.DrawLine(x + (block / 2), mid, x + (block / 2), face.Bottom, joint);
            }
        }

        using (var coping = new SKPaint { IsAntialias = true, Color = light })
        {
            c.DrawRect(face.Left, face.Top - (s * 0.003f), face.Width, s * 0.005f, coping);
        }

        using (var wetBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.003f))
        using (var wet = new SKPaint { IsAntialias = true, Color = SKColors.Black.WithAlpha(160), MaskFilter = wetBlur })
        {
            c.DrawRect(face.Left, face.Bottom - (s * 0.002f), face.Width, s * 0.006f, wet);
        }
    }
}
