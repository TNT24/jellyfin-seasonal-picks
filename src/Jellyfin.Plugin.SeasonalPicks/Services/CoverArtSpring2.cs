using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>
/// Painted scenery shared by the spring and summer covers (skies, rolling hills, haze, bokeh, grass), and the
/// St Patrick's Day, Pancake Day and Summer scenes.
/// </summary>
internal static partial class CoverArt
{
    // ---------- shared painterly helpers ----------

    /// <summary>True when a shape of radius r at (x, y) keeps out of the band the title sits in.</summary>
    private static bool SpringOffTitle(float x, float y, float r, int w, int h)
        => Math.Abs(y - (h / 2f)) > (h * 0.17f) + r || Math.Abs(x - (w / 2f)) > (w * 0.44f) + r;

    private static SKColor SpringMix(SKColor a, SKColor b, float t)
        => new(
            (byte)(a.Red + ((b.Red - a.Red) * t)),
            (byte)(a.Green + ((b.Green - a.Green) * t)),
            (byte)(a.Blue + ((b.Blue - a.Blue) * t)),
            (byte)(a.Alpha + ((b.Alpha - a.Alpha) * t)));

    /// <summary>A see-through vertical gradient over the whole cover: the sky, laid over the posters.</summary>
    private static void SpringSky(SKCanvas c, int w, int h, SKColor[] colours, float[] stops)
    {
        using var p = new SKPaint { Shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, h), colours, stops, SKShaderTileMode.Clamp) };
        c.DrawRect(0, 0, w, h, p);
    }

    /// <summary>A soft round glow fading out to nothing.</summary>
    private static void SpringGlow(SKCanvas c, float x, float y, float r, SKColor colour)
    {
        using var p = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(new SKPoint(x, y), r, new[] { colour, colour.WithAlpha((byte)(colour.Alpha * 0.35f)), colour.WithAlpha(0) }, new[] { 0f, 0.35f, 1f }, SKShaderTileMode.Clamp)
        };
        c.DrawCircle(x, y, r, p);
    }

    /// <summary>
    /// A gently rolling ridge around <paramref name="baseY"/>, closed down past the bottom edge, and its height at
    /// any x. Its swells are sized from the cover's short side, so wide covers get more of them rather than wider ones.
    /// </summary>
    private static (SKPath Path, Func<float, float> Y) SpringRidge(int w, int h, float s, float baseY, float amp, Random rng, float scale = 1f)
    {
        float p1 = R(rng, 0, 6.3f), p2 = R(rng, 0, 6.3f), p3 = R(rng, 0, 6.3f);
        float Y(float x)
        {
            var t = x / (s * scale);
            return baseY - (amp * ((0.62f * MathF.Sin((t * 2.2f) + p1)) + (0.28f * MathF.Sin((t * 5.1f) + p2)) + (0.1f * MathF.Sin((t * 11.3f) + p3))));
        }

        var path = new SKPath();
        path.MoveTo(-40, h + 40);
        for (var x = -40f; x <= w + 40; x += 4)
        {
            path.LineTo(x, Y(x));
        }

        path.LineTo(w + 40, h + 40);
        path.Close();
        return (path, Y);
    }

    /// <summary>Fills a hill top-to-bottom with a gradient, with soft light catching its crest.</summary>
    private static void SpringFillHill(SKCanvas c, SKPath hill, float top, float bottom, SKColor upper, SKColor lower, SKColor rim, float rimWidth)
    {
        using (var fill = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, top), new SKPoint(0, bottom), new[] { upper, lower }, null, SKShaderTileMode.Clamp) })
        {
            c.DrawPath(hill, fill);
        }

        if (rim.Alpha == 0 || rimWidth <= 0)
        {
            return;
        }

        c.Save();
        c.ClipPath(hill, antialias: true);
        using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, rimWidth * 0.45f);
        using var edge = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = rimWidth, Color = rim, MaskFilter = blur };
        c.DrawPath(hill, edge);
        c.Restore();
    }

    /// <summary>A soft horizontal band of haze centred on y.</summary>
    private static void SpringHaze(SKCanvas c, int w, float y, float half, SKColor colour)
    {
        using var p = new SKPaint { Shader = SKShader.CreateLinearGradient(new SKPoint(0, y - half), new SKPoint(0, y + half), new[] { colour.WithAlpha(0), colour, colour.WithAlpha(0) }, null, SKShaderTileMode.Clamp) };
        c.DrawRect(0, y - half, w, half * 2, p);
    }

    /// <summary>Out-of-focus discs of light, each with a slightly brighter rim, kept off the title.</summary>
    private static void SpringBokeh(SKCanvas c, int w, int h, float s, Random rng, int count, SKColor[] colours, float minY, float maxY, float minR, float maxR)
    {
        using var disc = new SKPaint { IsAntialias = true };
        using var ring = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke };
        for (var i = 0; i < count; i++)
        {
            var r = s * R(rng, minR, maxR);
            float x = R(rng, 0, w), y = R(rng, minY, maxY);
            var colour = colours[rng.Next(colours.Length)];
            var alpha = (byte)rng.Next(22, 60);
            if (!SpringOffTitle(x, y, r, w, h))
            {
                continue;
            }

            using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.1f);
            disc.MaskFilter = blur;
            disc.Color = colour.WithAlpha(alpha);
            c.DrawCircle(x, y, r, disc);
            ring.MaskFilter = blur;
            ring.StrokeWidth = r * 0.07f;
            ring.Color = colour.WithAlpha((byte)Math.Min(255, alpha * 1.6f));
            c.DrawCircle(x, y, r * 0.95f, ring);
        }
    }

    /// <summary>A four-pointed glint with a small halo.</summary>
    private static void SpringGlint(SKCanvas c, float x, float y, float r, SKColor colour)
    {
        SpringGlow(c, x, y, r * 1.1f, colour.WithAlpha(110));
        using var star = new SKPath();
        star.MoveTo(x, y - r);
        star.QuadTo(x + (r * 0.1f), y - (r * 0.1f), x + (r * 0.7f), y);
        star.QuadTo(x + (r * 0.1f), y + (r * 0.1f), x, y + r);
        star.QuadTo(x - (r * 0.1f), y + (r * 0.1f), x - (r * 0.7f), y);
        star.QuadTo(x - (r * 0.1f), y - (r * 0.1f), x, y - r);
        star.Close();
        using var p = new SKPaint { IsAntialias = true, Color = Lighten(colour, 0.7f) };
        c.DrawPath(star, p);
    }

    /// <summary>Blades of grass along a ground line, each shaded from its root to a lighter tip.</summary>
    private static void SpringBlades(SKCanvas c, int w, float s, Random rng, Func<float, float> ground, float spacing, float minH, float maxH, SKColor root, SKColor tip, float sink = 0)
    {
        using var p = new SKPaint { IsAntialias = true };
        for (var x = -spacing; x < w + spacing; x += spacing * R(rng, 0.5f, 1.3f))
        {
            var g = ground(x) + sink;
            var bh = s * R(rng, minH, maxH);
            var bw = s * 0.0055f * R(rng, 0.7f, 1.3f);
            var lean = bh * R(rng, -0.4f, 0.4f);
            using var blade = new SKPath();
            blade.MoveTo(x - bw, g);
            blade.QuadTo(x - (bw * 0.2f) + (lean * 0.25f), g - (bh * 0.6f), x + lean, g - bh);
            blade.QuadTo(x + (bw * 0.4f) + (lean * 0.25f), g - (bh * 0.5f), x + bw, g);
            blade.Close();
            using var shade = SKShader.CreateLinearGradient(new SKPoint(0, g), new SKPoint(0, g - bh), new[] { root, SpringMix(root, tip, R(rng, 0.6f, 1f)) }, null, SKShaderTileMode.Clamp);
            p.Shader = shade;
            c.DrawPath(blade, p);
        }
    }

    /// <summary>A single drifting petal: rounded, notched at the top, shaded along its length.</summary>
    private static void SpringPetal(SKCanvas c, float x, float y, float size, float rotation, SKColor colour, byte alpha = 230)
    {
        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(rotation);
        using var path = new SKPath();
        path.MoveTo(0, size * 0.55f);
        path.CubicTo(-size * 0.6f, size * 0.25f, -size * 0.5f, -size * 0.45f, -size * 0.1f, -size * 0.5f);
        path.QuadTo(0, -size * 0.36f, size * 0.1f, -size * 0.5f);
        path.CubicTo(size * 0.5f, -size * 0.45f, size * 0.6f, size * 0.25f, 0, size * 0.55f);
        path.Close();
        using var p = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.White.WithAlpha(alpha),
            Shader = SKShader.CreateLinearGradient(new SKPoint(0, -size * 0.5f), new SKPoint(0, size * 0.55f), new[] { Lighten(colour, 0.4f), colour, Darken(colour, 0.75f) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp)
        };
        c.DrawPath(path, p);
        c.Restore();
    }

    /// <summary>A full, rounded heart centred near the origin, its point down. About 1.2 × size wide and 1.05 × size tall.</summary>
    private static SKPath SpringHeartPath(float size)
    {
        var p = new SKPath();
        p.MoveTo(0, size * 0.5f);
        p.CubicTo(-size * 0.16f, size * 0.33f, -size * 0.62f, size * 0.06f, -size * 0.6f, -size * 0.2f);
        p.CubicTo(-size * 0.58f, -size * 0.5f, -size * 0.18f, -size * 0.62f, 0, -size * 0.32f);
        p.CubicTo(size * 0.18f, -size * 0.62f, size * 0.58f, -size * 0.5f, size * 0.6f, -size * 0.2f);
        p.CubicTo(size * 0.62f, size * 0.06f, size * 0.16f, size * 0.33f, 0, size * 0.5f);
        p.Close();
        return p;
    }

    /// <summary>A soft cloud of overlapping puffs, lit from above, its edges slightly blurred.</summary>
    private static void SpringSoftCloud(SKCanvas c, float x, float y, float r, SKColor lit, SKColor shade)
    {
        using var puffs = new SKPath();
        foreach (var (dx, dy, pr) in new[] { (-1.1f, 0.2f, 0.45f), (-0.55f, -0.1f, 0.62f), (0.1f, -0.3f, 0.75f), (0.75f, -0.05f, 0.58f), (1.25f, 0.22f, 0.4f), (0f, 0.25f, 0.55f) })
        {
            puffs.AddCircle(x + (dx * r), y + (dy * r), pr * r);
        }

        using var merged = new SKPath();
        puffs.Simplify(merged);
        using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.04f);
        using (var paint = new SKPaint
        {
            IsAntialias = true,
            MaskFilter = blur,
            Shader = SKShader.CreateLinearGradient(new SKPoint(0, y - r), new SKPoint(0, y + (r * 0.8f)), new[] { lit, lit, shade }, new[] { 0f, 0.4f, 1f }, SKShaderTileMode.Clamp)
        })
        {
            c.DrawPath(merged, paint);
        }

        // A bright rim where the sun catches the tops of the puffs.
        c.Save();
        c.ClipPath(merged, antialias: true);
        using var rimBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.06f);
        using var rim = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = r * 0.12f, Color = SKColors.White.WithAlpha(170), MaskFilter = rimBlur };
        c.Translate(0, r * 0.05f);
        c.DrawPath(merged, rim);
        c.Restore();
    }

    /// <summary>A small rounded tree on a hillside: a trunk and a shaded canopy.</summary>
    private static void SpringTree(SKCanvas c, float x, float ground, float r, SKColor leaf, SKColor trunk)
    {
        using (var t = new SKPaint { IsAntialias = true, Color = trunk })
        {
            c.DrawRect(x - (r * 0.08f), ground - (r * 1.1f), r * 0.16f, r * 1.1f, t);
        }

        using var canopy = new SKPath();
        canopy.AddCircle(x, ground - (r * 1.55f), r * 0.75f);
        canopy.AddCircle(x - (r * 0.45f), ground - (r * 1.15f), r * 0.55f);
        canopy.AddCircle(x + (r * 0.45f), ground - (r * 1.2f), r * 0.58f);
        using var merged = new SKPath();
        canopy.Simplify(merged);
        using var fill = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(new SKPoint(x + (r * 0.3f), ground - (r * 1.9f)), r * 1.8f, new[] { Lighten(leaf, 0.35f), leaf, Darken(leaf, 0.7f) }, new[] { 0f, 0.45f, 1f }, SKShaderTileMode.Clamp)
        };
        c.DrawPath(merged, fill);
    }

    /// <summary>A soft dark ellipse under something resting on the ground.</summary>
    private static void SpringContactShadow(SKCanvas c, float x, float y, float rx, float ry, byte alpha = 110)
    {
        using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, ry * 0.6f);
        using var p = new SKPaint { IsAntialias = true, Color = SKColors.Black.WithAlpha(alpha), MaskFilter = blur };
        c.DrawOval(new SKRect(x - rx, y - ry, x + rx, y + ry), p);
    }

    // ---------- St Patrick's Day: rolling Irish hills under a soft rainbow, shamrocks in the foreground ----------

    private static void Shamrocks(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);

        // A clearing sky after rain: cool teal overhead, warm gold along the horizon.
        SpringSky(c, w, h, new[] { new SKColor(0x1C, 0x4A, 0x5C, 200), new SKColor(0x4E, 0x8C, 0x94, 120), new SKColor(0xC8, 0xD8, 0xA8, 110), new SKColor(0xFF, 0xEA, 0xB0, 200) }, new[] { 0f, 0.32f, 0.58f, 0.72f });
        SpringGlow(c, w * 0.1f, h * 0.08f, s * 0.6f, new SKColor(0xFF, 0xF0, 0xC0, 120));

        // The rainbow: soft-edged bands, a faint brighter sky inside the bow, its feet fading into the haze.
        var centre = new SKPoint(w * 0.56f, h * 0.8f);
        var radius = h * 0.7f;
        using (var fade = new SKPaint())
        {
            c.SaveLayer(fade);
            using (var inside = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(centre, radius, new[] { SKColors.White.WithAlpha(0), SKColors.White.WithAlpha(0), SKColors.White.WithAlpha(40) }, new[] { 0f, 0.7f, 1f }, SKShaderTileMode.Clamp) })
            {
                c.DrawCircle(centre, radius - (s * 0.06f), inside);
            }

            var bands = new[] { new SKColor(0xE8, 0x4A, 0x3C), new SKColor(0xF5, 0x9A, 0x30), new SKColor(0xF8, 0xDE, 0x4C), new SKColor(0x5C, 0xC0, 0x5A), new SKColor(0x3C, 0x8C, 0xE0), new SKColor(0x7A, 0x4C, 0xC8) };
            var band = s * 0.017f;
            using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, band * 0.45f);
            using var stroke = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = band * 1.15f, MaskFilter = blur };
            for (var i = 0; i < bands.Length; i++)
            {
                stroke.Color = bands[i].WithAlpha(150);
                c.DrawCircle(centre, radius - (i * band), stroke);
            }

            using var mask = new SKPaint
            {
                BlendMode = SKBlendMode.DstIn,
                Shader = SKShader.CreateLinearGradient(new SKPoint(0, h * 0.04f), new SKPoint(0, h * 0.37f), new[] { SKColors.White.WithAlpha(150), SKColors.White, SKColors.White.WithAlpha(0) }, new[] { 0f, 0.45f, 1f }, SKShaderTileMode.Clamp)
            };
            c.DrawRect(0, 0, w, h, mask);
            c.Restore();
        }

        SpringSoftCloud(c, w * 0.12f, h * 0.2f, s * 0.07f, SKColors.White.WithAlpha(240), new SKColor(0xD0, 0xDC, 0xE4, 220));
        SpringSoftCloud(c, w * 0.9f, h * 0.12f, s * 0.06f, SKColors.White.WithAlpha(235), new SKColor(0xD0, 0xDC, 0xE4, 210));

        // Distant mountains, blue with distance, then nearer hills, then a band of haze.
        var mountains = SpringRidge(w, h, s, h * 0.68f, h * 0.07f, rng, 0.9f);
        SpringFillHill(c, mountains.Path, h * 0.6f, h * 0.76f, new SKColor(0x6A, 0x8C, 0xA0, 230), new SKColor(0x9C, 0xB8, 0xB0, 230), new SKColor(0xFF, 0xF0, 0xD0, 90), s * 0.006f);
        var far = SpringRidge(w, h, s, h * 0.73f, h * 0.04f, rng, 1.4f);
        SpringFillHill(c, far.Path, h * 0.67f, h * 0.8f, new SKColor(0x6C, 0xA0, 0x70), new SKColor(0x8C, 0xB4, 0x84), new SKColor(0xFF, 0xF4, 0xC0, 120), s * 0.008f);
        SpringHaze(c, w, h * 0.765f, h * 0.03f, new SKColor(0xF8, 0xF0, 0xD0, 110));

        // Middle hills: patchwork fields divided by hedgerows, a white cottage or two, sheep grazing.
        var mid = SpringRidge(w, h, s, h * 0.8f, h * 0.035f, rng, 1.1f);
        SpringFillHill(c, mid.Path, h * 0.74f, h * 0.9f, new SKColor(0x7C, 0xC0, 0x58), new SKColor(0x3C, 0x84, 0x3A), new SKColor(0xFF, 0xF4, 0xB0, 170), s * 0.01f);
        c.Save();
        c.ClipPath(mid.Path, antialias: true);
        using (var patch = new SKPaint { IsAntialias = true })
        {
            // Alternate fields a little lighter or yellower, then the hedges between them.
            var cuts = new List<float>();
            for (var x = R(rng, -s * 0.05f, s * 0.1f); x < w + (s * 0.2f); x += s * R(rng, 0.18f, 0.28f))
            {
                cuts.Add(x);
            }

            for (var k = 0; k < cuts.Count - 1; k += 2)
            {
                using var field = new SKPath();
                field.MoveTo(cuts[k], mid.Y(cuts[k]) - (s * 0.02f));
                field.LineTo(cuts[k + 1], mid.Y(cuts[k + 1]) - (s * 0.02f));
                field.LineTo(cuts[k + 1] - (s * 0.06f), h);
                field.LineTo(cuts[k] - (s * 0.06f), h);
                field.Close();
                patch.Color = (k % 4 == 0 ? new SKColor(0xE0, 0xE8, 0x80) : new SKColor(0xB0, 0xE8, 0x90)).WithAlpha(45);
                c.DrawPath(field, patch);
            }

            foreach (var x in cuts)
            {
                var top = mid.Y(x);
                SpringHedgerow(c, s, rng, t => new SKPoint(x - (s * 0.06f * t), top + ((h - top) * t)), (h - top) / (s * 0.008f));
            }

            SpringHedgerow(c, s, rng, t => new SKPoint(-10 + ((w + 20) * t), mid.Y(-10 + ((w + 20) * t)) + (s * 0.045f) + (MathF.Sin(t * 9) * s * 0.004f)), w / (s * 0.008f));
        }

        c.Restore();

        foreach (var fx in w > h ? new[] { 0.2f, 0.74f } : new[] { 0.24f })
        {
            var x = w * fx;
            SpringCottage(c, x, mid.Y(x) + (s * 0.012f), s * 0.05f);
        }

        for (var i = 0; i < (w > h ? 6 : 4); i++)
        {
            var x = R(rng, w * 0.35f, w * 0.95f);
            SpringSheep(c, x, mid.Y(x) + (s * R(rng, 0.015f, 0.032f)), s * 0.016f, rng.Next(2) == 0);
        }

        // Near hill: deep emerald, a low dry-stone wall winding along it.
        var near = SpringRidge(w, h, s, h * 0.88f, h * 0.022f, rng, 1.6f);
        SpringFillHill(c, near.Path, h * 0.84f, h, new SKColor(0x4C, 0xA8, 0x4A), new SKColor(0x14, 0x52, 0x22), new SKColor(0xF8, 0xF0, 0xA0, 180), s * 0.012f);
        SpringStoneWall(c, w, s, rng, x => near.Y(x) + (s * 0.02f));
        SpringBlades(c, w, s, rng, near.Y, s * 0.006f, 0.012f, 0.03f, new SKColor(0x1E, 0x60, 0x26), new SKColor(0x8C, 0xD0, 0x60), s * 0.03f);

        // Clover in the foreground: big shamrocks gathered in the bottom corners, smaller ones between, the back ones
        // darker and the front ones lit.
        var greens = new[] { new SKColor(0x2E, 0x9A, 0x48), new SKColor(0x3C, 0xB0, 0x56), new SKColor(0x24, 0x84, 0x3C) };
        var clusters = new List<(float X, float Y, float Size, float Rot, float Light)>
        {
            (s * 0.08f, h - (s * 0.1f), s * 0.11f, -20, 0.7f),
            (s * 0.22f, h - (s * 0.05f), s * 0.085f, 15, 0.9f),
            (s * 0.02f, h - (s * 0.02f), s * 0.09f, 30, 1f),
            (s * 0.33f, h - (s * 0.015f), s * 0.06f, -10, 1f),
            (w - (s * 0.09f), h - (s * 0.12f), s * 0.12f, 25, 0.7f),
            (w - (s * 0.24f), h - (s * 0.04f), s * 0.085f, -15, 0.9f),
            (w - (s * 0.02f), h - (s * 0.025f), s * 0.085f, -35, 1f),
            (w - (s * 0.36f), h - (s * 0.01f), s * 0.055f, 10, 1f)
        };
        if (w > h)
        {
            clusters.Add((w * 0.5f + (s * 0.05f), h - (s * 0.005f), s * 0.05f, 12, 1f));
            clusters.Add((w * 0.5f - (s * 0.12f), h + (s * 0.005f), s * 0.04f, -25, 1f));
        }

        clusters.Sort((a, b) => a.Light.CompareTo(b.Light));
        foreach (var (x, y, size, rot, light) in clusters)
        {
            SpringShamrock(c, x, y, size, rot + R(rng, -6, 6), Darken(greens[rng.Next(greens.Length)], light), light);
        }

        // A few golden motes catching the light under the rainbow.
        for (var i = 0; i < 5; i++)
        {
            float gx = R(rng, w * 0.15f, w * 0.85f), gy = R(rng, h * 0.06f, h * 0.3f);
            if (SpringOffTitle(gx, gy, s * 0.02f, w, h))
            {
                SpringGlint(c, gx, gy, s * R(rng, 0.006f, 0.012f), new SKColor(0xFF, 0xE0, 0x80));
            }
        }
    }

    /// <summary>
    /// A shamrock seen from above: three heart-shaped leaves meeting in the middle, each shaded from its rim, with the
    /// pale chevron clover leaves carry, a midrib, and a curved stem. <paramref name="light"/> 0-1 dims the back ones.
    /// </summary>
    private static void SpringShamrock(SKCanvas c, float x, float y, float size, float rotation, SKColor colour, float light)
    {
        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(rotation);

        SpringContactShadow(c, size * 0.06f, size * 0.1f, size * 0.95f, size * 0.85f, (byte)(70 * light));

        using (var stem = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = size * 0.08f, Color = Darken(colour, 0.75f) })
        using (var stemPath = new SKPath())
        {
            stemPath.MoveTo(0, 0);
            stemPath.QuadTo(size * 0.2f, size * 0.6f, size * 0.1f, size * 1.4f);
            c.DrawPath(stemPath, stem);
        }

        using var leafPath = SpringHeartPath(size);
        using var vein = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = size * 0.025f, Color = Lighten(colour, 0.45f).WithAlpha(150) };
        using var chevronBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, size * 0.025f);
        using var chevron = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round, StrokeWidth = size * 0.07f, Color = Lighten(colour, 0.55f).WithAlpha(120), MaskFilter = chevronBlur };
        using var edge = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = size * 0.02f, Color = Darken(colour, 0.6f).WithAlpha(160) };
        foreach (var angle in new[] { 0f, 120f, 240f })
        {
            c.Save();
            c.RotateDegrees(angle);
            c.Translate(0, -size * 0.5f);
            using var fill = new SKPaint
            {
                IsAntialias = true,
                Shader = SKShader.CreateRadialGradient(new SKPoint(-size * 0.15f, -size * 0.2f), size * 0.85f, new[] { Lighten(colour, 0.3f), colour, Darken(colour, 0.62f) }, new[] { 0f, 0.55f, 1f }, SKShaderTileMode.Clamp)
            };
            c.DrawPath(leafPath, fill);
            c.Save();
            c.ClipPath(leafPath, antialias: true);
            using (var v = new SKPath())
            {
                v.MoveTo(-size * 0.32f, -size * 0.1f);
                v.LineTo(0, size * 0.14f);
                v.LineTo(size * 0.32f, -size * 0.1f);
                c.DrawPath(v, chevron);
            }

            c.Restore();
            c.DrawLine(0, size * 0.42f, 0, -size * 0.25f, vein);
            c.DrawPath(leafPath, edge);
            c.Restore();
        }

        c.Restore();
    }

    /// <summary>A whitewashed cottage with a dark slate roof, a chimney and a lit window. (x, ground) is under its middle.</summary>
    private static void SpringCottage(SKCanvas c, float x, float ground, float size)
    {
        var wall = new SKRect(x - (size * 0.6f), ground - (size * 0.45f), x + (size * 0.6f), ground);
        using (var white = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(wall.Left, 0), new SKPoint(wall.Right, 0), new[] { new SKColor(0xFF, 0xFA, 0xEC), new SKColor(0xD8, 0xD4, 0xC8) }, null, SKShaderTileMode.Clamp) })
        {
            c.DrawRect(wall, white);
        }

        using (var roof = new SKPaint { IsAntialias = true, Color = new SKColor(0x3A, 0x3C, 0x48) })
        using (var path = new SKPath())
        {
            c.DrawRect(x + (size * 0.25f), wall.Top - (size * 0.42f), size * 0.12f, size * 0.3f, roof);
            path.MoveTo(wall.Left - (size * 0.08f), wall.Top + (size * 0.02f));
            path.LineTo(wall.Left + (size * 0.18f), wall.Top - (size * 0.3f));
            path.LineTo(wall.Right - (size * 0.18f), wall.Top - (size * 0.3f));
            path.LineTo(wall.Right + (size * 0.08f), wall.Top + (size * 0.02f));
            path.Close();
            c.DrawPath(path, roof);
        }

        using var window = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xD8, 0x80) };
        using var door = new SKPaint { IsAntialias = true, Color = new SKColor(0xB0, 0x28, 0x28) };
        c.DrawRect(x - (size * 0.42f), wall.Top + (size * 0.12f), size * 0.18f, size * 0.15f, window);
        c.DrawRect(x + (size * 0.24f), wall.Top + (size * 0.12f), size * 0.18f, size * 0.15f, window);
        c.DrawRect(x - (size * 0.07f), wall.Top + (size * 0.18f), size * 0.14f, size * 0.27f, door);
    }

    /// <summary>A tiny grazing sheep, seen side on.</summary>
    private static void SpringSheep(SKCanvas c, float x, float ground, float size, bool faceLeft)
    {
        var dir = faceLeft ? -1 : 1;
        using var legs = new SKPaint { IsAntialias = true, StrokeWidth = size * 0.14f, StrokeCap = SKStrokeCap.Round, Color = new SKColor(0x2A, 0x26, 0x24) };
        c.DrawLine(x - (size * 0.4f), ground - (size * 0.3f), x - (size * 0.4f), ground, legs);
        c.DrawLine(x + (size * 0.4f), ground - (size * 0.3f), x + (size * 0.4f), ground, legs);
        using var wool = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, ground - (size * 0.9f)), new SKPoint(0, ground - (size * 0.2f)), new[] { SKColors.White, new SKColor(0xD6, 0xD2, 0xC6) }, null, SKShaderTileMode.Clamp) };
        c.DrawOval(new SKRect(x - (size * 0.65f), ground - (size * 0.85f), x + (size * 0.65f), ground - (size * 0.22f)), wool);
        using var head = new SKPaint { IsAntialias = true, Color = new SKColor(0x2A, 0x26, 0x24) };
        c.DrawOval(new SKRect(x + (dir * size * 0.55f) - (size * 0.2f), ground - (size * 0.62f), x + (dir * size * 0.55f) + (size * 0.2f), ground - (size * 0.32f)), head);
    }

    /// <summary>A hedgerow: a chain of small dark bushes along a line (t from 0 to 1), lit a little on top.</summary>
    private static void SpringHedgerow(SKCanvas c, float s, Random rng, Func<float, SKPoint> along, float steps)
    {
        // A soft dark band with a lumpy, sunlit top: low bushes, not a string of beads.
        var n = Math.Max(2, (int)steps);
        using var line = new SKPath();
        using var top = new SKPath();
        for (var i = 0; i <= n; i++)
        {
            var p = along(i / (float)n);
            var lump = s * 0.004f * R(rng, 0.2f, 1f);
            if (i == 0)
            {
                line.MoveTo(p);
                top.MoveTo(p.X, p.Y - (s * 0.004f) - lump);
            }
            else
            {
                line.LineTo(p);
                top.LineTo(p.X, p.Y - (s * 0.004f) - lump);
            }
        }

        using var soft = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.0015f);
        using var body = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round, StrokeWidth = s * 0.01f, Color = new SKColor(0x22, 0x50, 0x24, 220), MaskFilter = soft };
        c.DrawPath(line, body);
        using var lit = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round, StrokeWidth = s * 0.004f, Color = new SKColor(0x7C, 0xB4, 0x50, 200), MaskFilter = soft };
        c.DrawPath(top, lit);
    }

    /// <summary>A dry-stone wall following a line across the cover: rows of rounded grey stones.</summary>
    private static void SpringStoneWall(SKCanvas c, int w, float s, Random rng, Func<float, float> line)
    {
        var stoneH = s * 0.0095f;
        using var stone = new SKPaint { IsAntialias = true };
        using var under = new SKPaint { IsAntialias = true, Color = new SKColor(0x22, 0x2A, 0x22, 200) };
        using (var body = new SKPath())
        {
            body.MoveTo(-10, line(-10) - (stoneH * 2.6f));
            for (var x = -10f; x <= w + 10; x += 8)
            {
                body.LineTo(x, line(x) - (stoneH * 2.6f));
            }

            for (var x = w + 10f; x >= -10; x -= 8)
            {
                body.LineTo(x, line(x) + (stoneH * 0.2f));
            }

            body.Close();
            c.DrawPath(body, under);
        }

        for (var row = 0; row < 3; row++)
        {
            var x = -R(rng, 0, s * 0.03f);
            while (x < w + 10)
            {
                var sw = s * R(rng, 0.014f, 0.028f) * (row == 2 ? 0.7f : 1f);
                var y = line(x + (sw / 2)) - (row * stoneH * 0.95f) - (stoneH * 0.5f);
                var grey = (byte)rng.Next(0x78, 0xA8);
                var tone = new SKColor(grey, grey, (byte)(grey - 6));
                var rect = new SKRect(x, y - (stoneH * 0.48f), x + sw, y + (stoneH * 0.48f));
                using var shade = SKShader.CreateLinearGradient(new SKPoint(0, rect.Top), new SKPoint(0, rect.Bottom), new[] { Lighten(tone, 0.25f), Darken(tone, 0.7f) }, null, SKShaderTileMode.Clamp);
                stone.Shader = shade;
                c.DrawRoundRect(rect, stoneH * 0.4f, stoneH * 0.4f, stone);
                x += sw + (s * 0.002f);
            }
        }
    }

    // ---------- Pancake Day: a tall, glossy stack on a plate, on a wooden table in a warm café ----------

    private static void PancakeStack(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var cx = w / 2f;

        // A warm, dim café: deeper at the top, a glow of lamplight low down.
        SpringSky(c, w, h, new[] { new SKColor(0x1A, 0x0C, 0x04, 170), new SKColor(0x30, 0x18, 0x08, 80), new SKColor(0x60, 0x34, 0x12, 120) }, new[] { 0f, 0.5f, 0.8f });
        SpringGlow(c, cx, h * 0.8f, s * 0.75f, new SKColor(0xFF, 0xB8, 0x60, 110));

        var warm = new[] { new SKColor(0xFF, 0xC8, 0x70), new SKColor(0xFF, 0xA8, 0x50), new SKColor(0xFF, 0xE0, 0xA0) };
        SpringBokeh(c, w, h, s, rng, w > h ? 22 : 16, warm, h * 0.02f, h * 0.3f, 0.025f, 0.06f);
        SpringFairyLights(c, w, h, s, rng);

        // The table: planks running across, nearer ones taller, the grain catching the light.
        var tableTop = h * 0.8f;
        using (var wood = new SKPaint { Shader = SKShader.CreateLinearGradient(new SKPoint(0, tableTop), new SKPoint(0, h), new[] { new SKColor(0xA8, 0x6C, 0x3C), new SKColor(0x7A, 0x48, 0x24), new SKColor(0x4A, 0x28, 0x12) }, new[] { 0f, 0.4f, 1f }, SKShaderTileMode.Clamp) })
        {
            c.DrawRect(0, tableTop, w, h - tableTop, wood);
        }

        using (var grain = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.0018f })
        using (var gap = new SKPaint { IsAntialias = true, StrokeWidth = s * 0.004f, Color = new SKColor(0x2E, 0x18, 0x08, 200) })
        {
            var planks = 4;
            for (var k = 0; k <= planks; k++)
            {
                var y0 = tableTop + ((h - tableTop) * MathF.Pow(k / (float)planks, 1.3f));
                var y1 = tableTop + ((h - tableTop) * MathF.Pow((k + 1) / (float)planks, 1.3f));
                if (k > 0)
                {
                    c.DrawLine(0, y0, w, y0, gap);
                }

                for (var g = 0; g < 4; g++)
                {
                    var gy = y0 + ((y1 - y0) * (0.2f + (g * 0.2f)));
                    grain.Color = (g % 2 == 0 ? new SKColor(0x3A, 0x1E, 0x0A) : new SKColor(0xD0, 0x96, 0x5C)).WithAlpha(60);
                    using var line = new SKPath();
                    var phase = R(rng, 0, 6.3f);
                    line.MoveTo(0, gy);
                    for (var x = 0f; x <= w; x += s * 0.02f)
                    {
                        line.LineTo(x, gy + (MathF.Sin((x / s * 6) + phase) * s * 0.003f));
                    }

                    c.DrawPath(line, grain);
                }
            }
        }

        using (var edge = new SKPaint { IsAntialias = true, StrokeWidth = s * 0.004f, Color = new SKColor(0xFF, 0xD0, 0x98, 150) })
        {
            c.DrawLine(0, tableTop, w, tableTop, edge);
        }

        // A pool of lamplight on the table.
        SpringGlow(c, cx, h * 0.9f, s * 0.5f, new SKColor(0xFF, 0xD0, 0x8A, 70));

        // Props either side: a glass jug of syrup on the left, a bowl of berries on the right.
        var side = w > h ? w * 0.2f : s * 0.1f;
        SpringSyrupJug(c, side, h * 0.97f, s * 0.19f);
        SpringBerryBowl(c, w - side, h * 0.975f, s * (w > h ? 0.2f : 0.15f), rng);

        // The plate.
        var plateY = h * 0.925f;
        var plateRx = s * 0.3f;
        var plateRy = s * 0.06f;
        SpringContactShadow(c, cx, plateY + (plateRy * 0.4f), plateRx * 1.02f, plateRy * 1.1f, 140);
        using (var rim = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, plateY - plateRy), new SKPoint(0, plateY + plateRy), new[] { new SKColor(0xE8, 0xE2, 0xD8), new SKColor(0xFF, 0xFC, 0xF6), new SKColor(0xC8, 0xC0, 0xB4) }, new[] { 0f, 0.6f, 1f }, SKShaderTileMode.Clamp) })
        {
            c.DrawOval(new SKRect(cx - plateRx, plateY - plateRy, cx + plateRx, plateY + plateRy), rim);
        }

        using (var well = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, plateY - (plateRy * 0.65f)), new SKPoint(0, plateY + (plateRy * 0.65f)), new[] { new SKColor(0xD4, 0xCC, 0xC0), new SKColor(0xF6, 0xF2, 0xEA) }, null, SKShaderTileMode.Clamp) })
        {
            c.DrawOval(new SKRect(cx - (plateRx * 0.72f), plateY - (plateRy * 0.66f), cx + (plateRx * 0.72f), plateY + (plateRy * 0.66f)), well);
        }

        // Syrup pooled on the plate around the base of the stack.
        using (var puddle = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, plateY - (plateRy * 0.4f)), new SKPoint(0, plateY + (plateRy * 0.5f)), new[] { new SKColor(0x7A, 0x30, 0x06), new SKColor(0xC0, 0x66, 0x14) }, null, SKShaderTileMode.Clamp) })
        {
            c.DrawOval(new SKRect(cx - (plateRx * 0.66f), plateY - (plateRy * 0.4f), cx + (plateRx * 0.6f), plateY + (plateRy * 0.5f)), puddle);
        }

        // The stack, bottom pancake first.
        var layers = 7;
        var thick = s * 0.027f;
        var rx = s * 0.2f;
        var ry = s * 0.048f;
        var baseY = plateY - (plateRy * 0.1f);
        SKPoint topCentre = default;
        float topRx = rx;
        for (var i = 0; i < layers; i++)
        {
            var px = cx + R(rng, -s * 0.007f, s * 0.007f);
            var prx = rx * R(rng, 0.96f, 1.03f) * (1 - (i * 0.006f));
            var bottomY = baseY - (i * thick);
            var topY = bottomY - (thick * 0.88f);
            var wobble = R(rng, 0, 6.3f);
            SpringPancake(c, px, bottomY, topY, prx, ry, wobble, rng, i == layers - 1);
            topCentre = new SKPoint(px, topY);
            topRx = prx;
        }

        // Syrup: a glossy pool on top, drips running down the front, the longest reaching the plate.
        SpringSyrup(c, topCentre, topRx, ry, baseY, s, rng);

        // Butter, berries and a sprig of mint on top.
        SpringBlueberry(c, topCentre.X - (topRx * 0.48f), topCentre.Y - (ry * 0.15f), s * 0.018f);
        SpringBlueberry(c, topCentre.X - (topRx * 0.3f), topCentre.Y + (ry * 0.35f), s * 0.019f);
        SpringStrawberryHalf(c, topCentre.X + (topRx * 0.42f), topCentre.Y - (ry * 0.1f), s * 0.03f, 20);
        SpringButter(c, topCentre.X - (s * 0.01f), topCentre.Y - (ry * 0.05f), s * 0.07f);
        SpringMint(c, topCentre.X + (topRx * 0.12f), topCentre.Y - (ry * 0.55f), s * 0.03f);

        // Berries on the plate and the table.
        SpringBlueberry(c, cx + (plateRx * 0.62f), plateY + (plateRy * 0.2f), s * 0.018f);
        SpringBlueberry(c, cx + (plateRx * 0.72f), plateY - (plateRy * 0.1f), s * 0.016f);
        SpringStrawberryHalf(c, cx - (plateRx * 0.68f), plateY + (plateRy * 0.1f), s * 0.032f, -25);
        SpringBlueberry(c, cx - (plateRx * 1.15f), h * 0.985f, s * 0.018f);
        SpringBlueberry(c, cx + (plateRx * 1.12f), h * 0.99f, s * 0.017f);

        // Steam curling up from the hot stack.
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.01f))
        using (var steam = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = s * 0.014f, MaskFilter = blur })
        {
            foreach (var (dx, phase) in new[] { (-0.09f, 0f), (0.02f, 2f), (0.11f, 4f) })
            {
                var x0 = topCentre.X + (s * dx);
                var y0 = topCentre.Y - (s * 0.05f);
                var y1 = h * 0.63f;
                using var wisp = new SKPath();
                wisp.MoveTo(x0, y0);
                for (var y = y0; y >= y1; y -= s * 0.01f)
                {
                    var t = (y0 - y) / (y0 - y1);
                    wisp.LineTo(x0 + (MathF.Sin((t * 6) + phase) * s * 0.02f * (0.5f + t)), y);
                }

                steam.Shader = SKShader.CreateLinearGradient(new SKPoint(0, y0), new SKPoint(0, y1), new[] { SKColors.White.WithAlpha(90), SKColors.White.WithAlpha(0) }, null, SKShaderTileMode.Clamp);
                c.DrawPath(wisp, steam);
                steam.Shader.Dispose();
                steam.Shader = null;
            }
        }
    }

    /// <summary>One pancake: a browned side band, a crumb line, and a golden top face speckled where it cooked.</summary>
    private static void SpringPancake(SKCanvas c, float x, float bottomY, float topY, float rx, float ry, float wobble, Random rng, bool isTop)
    {
        SKPoint Edge(float cy, float a)
        {
            var k = 1 + (0.018f * MathF.Sin((a * 5) + wobble)) + (0.01f * MathF.Sin((a * 11) + (wobble * 2)));
            return new SKPoint(x + (MathF.Cos(a) * rx * k), cy + (MathF.Sin(a) * ry * k));
        }

        // The side: the bottom edge's front half, up the side, back along the top edge's front half.
        using var side = new SKPath();
        side.MoveTo(Edge(bottomY, 0));
        for (var a = 0f; a <= MathF.PI + 0.001f; a += MathF.PI / 48)
        {
            side.LineTo(Edge(bottomY, a));
        }

        for (var a = MathF.PI; a >= -0.001f; a -= MathF.PI / 48)
        {
            side.LineTo(Edge(topY, a));
        }

        side.Close();
        using (var band = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateLinearGradient(new SKPoint(x - rx, 0), new SKPoint(x + rx, 0), new[] { new SKColor(0x8E, 0x4A, 0x14), new SKColor(0xE0, 0x98, 0x40), new SKColor(0xF4, 0xB8, 0x5C), new SKColor(0xC4, 0x74, 0x28), new SKColor(0x6E, 0x34, 0x0C) }, new[] { 0f, 0.25f, 0.45f, 0.8f, 1f }, SKShaderTileMode.Clamp)
        })
        {
            c.DrawPath(side, band);
        }

        // The pale crumb showing between the browned faces, and a shadow line under it.
        using (var crumb = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = (bottomY - topY) * 0.3f, Color = new SKColor(0xFF, 0xE4, 0xA8, 150) })
        using (var line = new SKPath())
        {
            var midY = (bottomY + topY) / 2;
            line.MoveTo(Edge(midY, 0.1f));
            for (var a = 0.1f; a <= MathF.PI - 0.1f; a += MathF.PI / 48)
            {
                line.LineTo(Edge(midY, a));
            }

            c.DrawPath(line, crumb);
        }

        using (var shadow = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = (bottomY - topY) * 0.12f, Color = new SKColor(0x3A, 0x18, 0x04, 140) })
        using (var line = new SKPath())
        {
            line.MoveTo(Edge(bottomY, 0.05f));
            for (var a = 0.05f; a <= MathF.PI - 0.05f; a += MathF.PI / 48)
            {
                line.LineTo(Edge(bottomY, a));
            }

            c.DrawPath(line, shadow);
        }

        // The top face.
        using var face = new SKPath();
        face.MoveTo(Edge(topY, 0));
        for (var a = 0f; a <= (MathF.PI * 2) + 0.001f; a += MathF.PI / 48)
        {
            face.LineTo(Edge(topY, a));
        }

        face.Close();
        using (var top = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(new SKPoint(x - (rx * 0.15f), topY - (ry * 0.2f)), rx, new[] { new SKColor(0xF0, 0xB4, 0x58), new SKColor(0xD8, 0x8E, 0x36), new SKColor(0xA8, 0x5E, 0x1A) }, new[] { 0f, 0.65f, 1f }, SKShaderTileMode.Clamp)
        })
        {
            c.DrawPath(face, top);
        }

        if (!isTop)
        {
            return;
        }

        c.Save();
        c.ClipPath(face, antialias: true);
        using var speck = new SKPaint { IsAntialias = true };
        for (var i = 0; i < 40; i++)
        {
            var a = R(rng, 0, MathF.PI * 2);
            var d = MathF.Sqrt(R(rng, 0, 1));
            speck.Color = new SKColor(0x8A, 0x46, 0x10, (byte)rng.Next(50, 120));
            var sx = x + (MathF.Cos(a) * rx * d);
            var sy = topY + (MathF.Sin(a) * ry * d);
            c.DrawOval(new SKRect(sx - (rx * 0.03f), sy - (ry * 0.05f), sx + (rx * 0.03f), sy + (ry * 0.05f)), speck);
        }

        c.Restore();
    }

    /// <summary>Glossy syrup pooled on the top pancake and running down the front of the stack.</summary>
    private static void SpringSyrup(SKCanvas c, SKPoint top, float rx, float ry, float baseY, float s, Random rng)
    {
        var dark = new SKColor(0x8A, 0x3A, 0x06, 235);
        var amber = new SKColor(0xE8, 0x96, 0x28, 235);
        using var syrup = new SKPaint { IsAntialias = true };

        // Drips first so the pool sits over their tops.
        foreach (var (a, len) in new[] { (0.22f, 0.5f), (0.42f, 0.95f), (0.62f, 0.3f), (0.78f, 0.75f), (0.92f, 0.4f), (0.3f, 0.2f) })
        {
            var ang = MathF.PI * a;
            var dx = top.X + (MathF.Cos(ang) * rx * 0.86f);
            var dy = top.Y + (MathF.Sin(ang) * ry * 0.86f);
            var bottom = dy + ((baseY - top.Y + (ry * 0.6f)) * len * R(rng, 0.9f, 1.05f));
            var dw = s * 0.017f * R(rng, 0.85f, 1.2f);
            using var drip = new SKPath();
            drip.MoveTo(dx - (dw * 0.9f), dy - (ry * 0.2f));
            drip.CubicTo(dx - (dw * 0.6f), dy + ((bottom - dy) * 0.3f), dx - (dw * 0.45f), bottom - (dw * 1.2f), dx - (dw * 0.62f), bottom - (dw * 0.4f));
            drip.ArcTo(new SKRect(dx - (dw * 0.62f), bottom - (dw * 1.04f), dx + (dw * 0.62f), bottom + (dw * 0.2f)), 180, -180, false);
            drip.CubicTo(dx + (dw * 0.45f), bottom - (dw * 1.2f), dx + (dw * 0.6f), dy + ((bottom - dy) * 0.3f), dx + (dw * 0.9f), dy - (ry * 0.2f));
            drip.Close();
            using var shade = SKShader.CreateLinearGradient(new SKPoint(dx - dw, 0), new SKPoint(dx + dw, 0), new[] { amber, dark, dark }, new[] { 0f, 0.55f, 1f }, SKShaderTileMode.Clamp);
            syrup.Shader = shade;
            c.DrawPath(drip, syrup);
            using var shine = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = dw * 0.22f, Color = SKColors.White.WithAlpha(150) };
            c.DrawLine(dx - (dw * 0.35f), dy + ((bottom - dy) * 0.2f), dx - (dw * 0.3f), bottom - (dw * 0.8f), shine);
        }

        using var pool = new SKPath();
        for (var k = 0; k <= 64; k++)
        {
            var a = k / 64f * MathF.PI * 2;
            var r = 0.84f + (0.05f * MathF.Sin(a * 6)) + (0.03f * MathF.Sin((a * 13) + 1));
            var p = new SKPoint(top.X + (MathF.Cos(a) * rx * r), top.Y + (MathF.Sin(a) * ry * r));
            if (k == 0)
            {
                pool.MoveTo(p);
            }
            else
            {
                pool.LineTo(p);
            }
        }

        pool.Close();
        using var poolShade = SKShader.CreateRadialGradient(new SKPoint(top.X - (rx * 0.3f), top.Y - (ry * 0.3f)), rx * 1.1f, new[] { amber, new SKColor(0xB8, 0x5C, 0x10, 235), dark }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp);
        syrup.Shader = poolShade;
        c.DrawPath(pool, syrup);

        using var gloss = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = ry * 0.12f, Color = SKColors.White.WithAlpha(170) };
        c.DrawArc(new SKRect(top.X - (rx * 0.7f), top.Y - (ry * 0.65f), top.X + (rx * 0.5f), top.Y + (ry * 0.6f)), 195, 60, false, gloss);
        using var dot = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(200) };
        c.DrawCircle(top.X + (rx * 0.45f), top.Y + (ry * 0.25f), ry * 0.07f, dot);
    }

    /// <summary>A pat of butter, a rounded cube seen from above, softening at the base.</summary>
    private static void SpringButter(SKCanvas c, float x, float y, float size)
    {
        var hw = size * 0.55f;
        var depth = size * 0.22f;
        var tall = size * 0.38f;

        using (var melt = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xE8, 0x90, 200) })
        {
            c.DrawOval(new SKRect(x - (hw * 1.35f), y - (depth * 0.4f), x + (hw * 1.4f), y + (depth * 1.7f)), melt);
        }

        using var corner = SKPathEffect.CreateCorner(size * 0.06f);
        SKPoint P(float px, float py) => new(x + px, y + py);
        var top = new[] { P(-hw, -tall), P(0, -tall - depth), P(hw, -tall), P(0, -tall + depth) };
        var left = new[] { P(-hw, -tall), P(0, -tall + depth), P(0, depth), P(-hw, 0) };
        var right = new[] { P(hw, -tall), P(0, -tall + depth), P(0, depth), P(hw, 0) };
        foreach (var (pts, colour) in new[] { (left, new SKColor(0xFF, 0xDE, 0x78)), (right, new SKColor(0xF0, 0xC0, 0x4A)), (top, new SKColor(0xFF, 0xF4, 0xC0)) })
        {
            using var face = new SKPath();
            face.AddPoly(pts, true);
            using var p = new SKPaint { IsAntialias = true, Color = colour, PathEffect = corner };
            c.DrawPath(face, p);
        }

        using var shine = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = size * 0.035f, Color = SKColors.White.WithAlpha(200) };
        c.DrawLine(P(-hw * 0.7f, -tall - (depth * 0.1f)), P(-hw * 0.15f, -tall - (depth * 0.75f)), shine);
    }

    private static void SpringBlueberry(SKCanvas c, float x, float y, float r)
    {
        SpringContactShadow(c, x + (r * 0.2f), y + (r * 0.8f), r * 1.1f, r * 0.35f, 100);
        using (var berry = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(x - (r * 0.35f), y - (r * 0.4f)), r * 1.5f, new[] { new SKColor(0x8A, 0x96, 0xD8), new SKColor(0x3A, 0x40, 0x8C), new SKColor(0x16, 0x16, 0x40) }, new[] { 0f, 0.45f, 1f }, SKShaderTileMode.Clamp) })
        {
            c.DrawCircle(x, y, r, berry);
        }

        using var crown = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = r * 0.12f, StrokeCap = SKStrokeCap.Round, Color = new SKColor(0x10, 0x10, 0x28) };
        for (var i = 0; i < 5; i++)
        {
            var a = i * MathF.PI * 2 / 5;
            c.DrawLine(x + (r * 0.1f), y - (r * 0.45f), x + (r * 0.1f) + (MathF.Cos(a) * r * 0.22f), y - (r * 0.45f) + (MathF.Sin(a) * r * 0.16f), crown);
        }

        using var shine = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(150) };
        c.DrawCircle(x - (r * 0.4f), y - (r * 0.3f), r * 0.16f, shine);
    }

    /// <summary>Half a strawberry, cut face up: red skin, pale flesh with its paler core, a green calyx.</summary>
    private static void SpringStrawberryHalf(SKCanvas c, float x, float y, float r, float rotation)
    {
        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(rotation);
        SpringContactShadow(c, r * 0.1f, r * 0.7f, r * 0.9f, r * 0.3f, 90);
        using var berry = new SKPath();
        berry.MoveTo(0, r);
        berry.CubicTo(-r * 1.05f, r * 0.25f, -r * 0.9f, -r * 0.8f, 0, -r * 0.72f);
        berry.CubicTo(r * 0.9f, -r * 0.8f, r * 1.05f, r * 0.25f, 0, r);
        berry.Close();
        using (var red = new SKPaint { IsAntialias = true, Color = new SKColor(0xC8, 0x18, 0x28) })
        {
            c.DrawPath(berry, red);
        }

        c.Save();
        c.Scale(0.8f, 0.8f);
        using (var flesh = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(0, -r * 0.1f), r * 0.9f, new[] { new SKColor(0xFF, 0xD8, 0xD0), new SKColor(0xF8, 0x60, 0x60), new SKColor(0xE0, 0x28, 0x34) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp) })
        {
            c.DrawPath(berry, flesh);
        }

        c.Restore();
        using (var leaf = new SKPaint { IsAntialias = true, Color = new SKColor(0x3C, 0x8C, 0x3A) })
        {
            foreach (var a in new[] { -40f, 0f, 40f })
            {
                c.Save();
                c.RotateDegrees(a, 0, -r * 0.72f);
                c.DrawOval(new SKRect(-r * 0.12f, -r * 1.15f, r * 0.12f, -r * 0.68f), leaf);
                c.Restore();
            }
        }

        c.Restore();
    }

    /// <summary>A sprig of two mint leaves.</summary>
    private static void SpringMint(SKCanvas c, float x, float y, float size)
    {
        foreach (var (a, sc) in new[] { (-35f, 1f), (30f, 0.85f) })
        {
            c.Save();
            c.Translate(x, y);
            c.RotateDegrees(a);
            c.Scale(sc);
            using var leaf = new SKPath();
            leaf.MoveTo(0, 0);
            leaf.QuadTo(size * 0.55f, -size * 0.45f, 0, -size * 1.1f);
            leaf.QuadTo(-size * 0.55f, -size * 0.45f, 0, 0);
            leaf.Close();
            using var fill = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(-size * 0.4f, 0), new SKPoint(size * 0.4f, 0), new[] { new SKColor(0x7C, 0xD0, 0x6A), new SKColor(0x2E, 0x84, 0x3A) }, null, SKShaderTileMode.Clamp) };
            c.DrawPath(leaf, fill);
            using var vein = new SKPaint { IsAntialias = true, StrokeWidth = size * 0.04f, Color = new SKColor(0xC8, 0xF0, 0xB8, 160) };
            c.DrawLine(0, -size * 0.05f, 0, -size * 0.95f, vein);
            c.Restore();
        }
    }

    /// <summary>Strings of warm fairy lights draped across the top of the cover.</summary>
    private static void SpringFairyLights(SKCanvas c, int w, int h, float s, Random rng)
    {
        foreach (var (y0, y1, sag, scale) in new[] { (h * 0.02f, h * 0.06f, h * 0.15f, 0.75f), (-h * 0.01f, h * 0.0f, h * 0.11f, 1f) })
        {
            SKPoint At(float t) => new((-w * 0.05f) + (t * w * 1.1f), y0 + ((y1 - y0) * t) + (4 * sag * t * (1 - t)));
            using (var wire = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.0025f * scale, Color = new SKColor(0x20, 0x14, 0x0C, 220) })
            using (var line = new SKPath())
            {
                line.MoveTo(At(0));
                for (var t = 0.02f; t <= 1.001f; t += 0.02f)
                {
                    line.LineTo(At(t));
                }

                c.DrawPath(line, wire);
            }

            var count = (int)(w / (s * 0.075f * scale));
            for (var i = 1; i < count; i++)
            {
                var p = At(i / (float)count);
                var r = s * 0.011f * scale;
                SpringGlow(c, p.X, p.Y + (r * 1.2f), r * 5, new SKColor(0xFF, 0xC8, 0x70, (byte)(130 * scale)));
                using var bulb = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(p.X, p.Y + (r * 1.1f)), r * 1.2f, new[] { new SKColor(0xFF, 0xFF, 0xE8), new SKColor(0xFF, 0xD0, 0x70) }, null, SKShaderTileMode.Clamp) };
                c.DrawOval(new SKRect(p.X - (r * 0.7f), p.Y + (r * 0.1f), p.X + (r * 0.7f), p.Y + (r * 2.1f)), bulb);
            }
        }
    }

    /// <summary>A little glass jug of syrup standing on the table: clear glass, amber inside, a handle and a spout.</summary>
    private static void SpringSyrupJug(SKCanvas c, float x, float ground, float height)
    {
        var bw = height * 0.62f;
        SpringContactShadow(c, x, ground, bw * 0.6f, height * 0.05f, 120);
        using var body = new SKPath();
        body.MoveTo(x - (bw * 0.28f), ground - height);
        body.LineTo(x + (bw * 0.22f), ground - height);
        body.QuadTo(x + (bw * 0.36f), ground - (height * 1.03f), x + (bw * 0.42f), ground - (height * 1.06f));
        body.QuadTo(x + (bw * 0.36f), ground - (height * 0.92f), x + (bw * 0.3f), ground - (height * 0.8f));
        body.CubicTo(x + (bw * 0.6f), ground - (height * 0.55f), x + (bw * 0.55f), ground, x + (bw * 0.25f), ground);
        body.LineTo(x - (bw * 0.25f), ground);
        body.CubicTo(x - (bw * 0.55f), ground, x - (bw * 0.6f), ground - (height * 0.55f), x - (bw * 0.3f), ground - (height * 0.8f));
        body.Close();

        // Handle, behind the glass on the left.
        using (var handle = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = bw * 0.08f, StrokeCap = SKStrokeCap.Round, Color = SKColors.White.WithAlpha(110) })
        {
            c.DrawArc(new SKRect(x - (bw * 0.7f), ground - (height * 0.85f), x - (bw * 0.2f), ground - (height * 0.3f)), 90, 180, false, handle);
        }

        using (var glass = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(40) })
        {
            c.DrawPath(body, glass);
        }

        c.Save();
        c.ClipPath(body, antialias: true);
        var level = ground - (height * 0.6f);
        using (var liquid = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(x - bw * 0.5f, 0), new SKPoint(x + bw * 0.5f, 0), new[] { new SKColor(0xE0, 0x8A, 0x20, 235), new SKColor(0x8A, 0x36, 0x06, 245), new SKColor(0x5A, 0x20, 0x04, 245) }, new[] { 0f, 0.55f, 1f }, SKShaderTileMode.Clamp) })
        {
            c.DrawRect(x - bw, level, bw * 2, height, liquid);
        }

        using (var meniscus = new SKPaint { IsAntialias = true, Color = new SKColor(0xF0, 0xA8, 0x40, 220) })
        {
            c.DrawOval(new SKRect(x - (bw * 0.52f), level - (height * 0.03f), x + (bw * 0.52f), level + (height * 0.03f)), meniscus);
        }

        c.Restore();
        using (var outline = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = bw * 0.025f, Color = SKColors.White.WithAlpha(150) })
        {
            c.DrawPath(body, outline);
        }

        using var shine = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = bw * 0.06f, Color = SKColors.White.WithAlpha(170) };
        c.DrawLine(x - (bw * 0.3f), ground - (height * 0.62f), x - (bw * 0.34f), ground - (height * 0.2f), shine);
    }

    /// <summary>A white bowl heaped with blueberries and strawberries.</summary>
    private static void SpringBerryBowl(SKCanvas c, float x, float ground, float width, Random rng)
    {
        var rimY = ground - (width * 0.38f);
        SpringContactShadow(c, x, ground, width * 0.5f, width * 0.06f, 130);
        using (var inside = new SKPaint { IsAntialias = true, Color = new SKColor(0x9A, 0x92, 0x88) })
        {
            c.DrawOval(new SKRect(x - (width * 0.5f), rimY - (width * 0.09f), x + (width * 0.5f), rimY + (width * 0.09f)), inside);
        }

        // The heap of fruit, back to front.
        for (var row = 0; row < 3; row++)
        {
            var count = 5 - row;
            for (var i = 0; i < count; i++)
            {
                var bx = x + ((i - ((count - 1) / 2f)) * width * 0.17f) + R(rng, -width * 0.02f, width * 0.02f);
                var by = rimY - (width * (0.1f - (row * 0.05f))) - (width * 0.04f * (1 - Math.Abs(i - ((count - 1) / 2f)) / count));
                if ((i + row) % 3 == 1)
                {
                    SpringStrawberryHalf(c, bx, by, width * 0.085f, R(rng, -30, 30));
                }
                else
                {
                    SpringBlueberry(c, bx, by, width * 0.055f);
                }
            }
        }

        using var bowl = new SKPath();
        bowl.MoveTo(x - (width * 0.5f), rimY);
        bowl.CubicTo(x - (width * 0.48f), ground - (width * 0.05f), x - (width * 0.3f), ground, x, ground);
        bowl.CubicTo(x + (width * 0.3f), ground, x + (width * 0.48f), ground - (width * 0.05f), x + (width * 0.5f), rimY);
        bowl.ArcTo(new SKRect(x - (width * 0.5f), rimY - (width * 0.09f), x + (width * 0.5f), rimY + (width * 0.09f)), 0, 180, false);
        bowl.Close();
        using (var china = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(x - (width * 0.5f), 0), new SKPoint(x + (width * 0.5f), 0), new[] { new SKColor(0xC8, 0xC4, 0xBC), SKColors.White, new SKColor(0xE8, 0xE4, 0xDC), new SKColor(0x9C, 0x96, 0x8C) }, new[] { 0f, 0.35f, 0.6f, 1f }, SKShaderTileMode.Clamp) })
        {
            c.DrawPath(bowl, china);
        }

        using var band = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = width * 0.02f, Color = new SKColor(0x3A, 0x6E, 0xB0, 200) };
        c.DrawArc(new SKRect(x - (width * 0.49f), rimY - (width * 0.05f), x + (width * 0.49f), rimY + (width * 0.17f)), 10, 160, false, band);
    }

    // ---------- Summer: sunset over the sea, framed by palm trees ----------

    private static void Summer(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var horizon = h * 0.73f;

        // Sunset sky: deep blue overhead through violet and rose to gold at the horizon.
        SpringSky(c, w, h, new[] { new SKColor(0x14, 0x1E, 0x58, 215), new SKColor(0x4A, 0x2A, 0x78, 150), new SKColor(0xC0, 0x48, 0x70, 140), new SKColor(0xFF, 0x8C, 0x50, 200), new SKColor(0xFF, 0xC8, 0x78, 240) }, new[] { 0f, 0.25f, 0.5f, 0.64f, 0.73f });

        // The sun, half set, with a broad glow.
        // Saturated, since the title's shadow dims everything near the middle.
        var sun = new SKPoint(w * 0.7f, horizon + (s * 0.015f));
        var sr = s * 0.1f;
        SpringGlow(c, sun.X, sun.Y, s * 0.9f, new SKColor(0xFF, 0x80, 0x30, 160));
        SpringGlow(c, sun.X, sun.Y, s * 0.32f, new SKColor(0xFF, 0xB0, 0x40, 230));
        c.Save();
        c.ClipRect(new SKRect(0, 0, w, horizon));
        // A soft-edged disc: a hard ring round it read as a cut-out.
        using (var discBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.003f))
        using (var disc = new SKPaint { IsAntialias = true, MaskFilter = discBlur, Shader = SKShader.CreateLinearGradient(new SKPoint(0, sun.Y - sr), new SKPoint(0, sun.Y), new[] { new SKColor(0xFF, 0xF0, 0x90), new SKColor(0xFF, 0x98, 0x20) }, null, SKShaderTileMode.Clamp) })
        {
            c.DrawCircle(sun, sr, disc);
        }

        c.Restore();

        // Thin clouds lit from below, high up and low along the horizon.
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.008f))
        using (var cloud = new SKPaint { IsAntialias = true, MaskFilter = blur })
        {
            for (var i = 0; i < (w > h ? 9 : 7); i++)
            {
                var low = i < 3;
                var cy = low ? R(rng, h * 0.66f, h * 0.7f) : R(rng, h * 0.07f, h * 0.3f);
                var cxp = R(rng, 0, w);
                var cw = s * R(rng, 0.12f, 0.3f);
                var ch = s * R(rng, 0.008f, 0.016f);
                if (!low && !SpringOffTitle(cxp, cy, ch, w, h))
                {
                    continue;
                }

                cloud.Shader = SKShader.CreateLinearGradient(new SKPoint(0, cy - ch), new SKPoint(0, cy + ch), new[] { new SKColor(0x8A, 0x4A, 0x8A, 120), new SKColor(0xFF, 0xA8, 0x88, low ? (byte)150 : (byte)170) }, null, SKShaderTileMode.Clamp);
                c.DrawOval(new SKRect(cxp - cw, cy - ch, cxp + cw, cy + ch), cloud);
                cloud.Shader.Dispose();
                cloud.Shader = null;
            }
        }

        // Gulls.
        using (var gull = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round, StrokeWidth = s * 0.0045f, Color = new SKColor(0x20, 0x14, 0x30, 220) })
        {
            foreach (var (gx, gy, gs) in new[] { (0.46f, 0.16f, 0.028f), (0.53f, 0.12f, 0.022f), (0.58f, 0.2f, 0.018f) })
            {
                float px = w * gx, py = h * gy, size = s * gs;
                using var wings = new SKPath();
                wings.MoveTo(px - size, py - (size * 0.15f));
                wings.QuadTo(px - (size * 0.5f), py - (size * 0.55f), px, py);
                wings.QuadTo(px + (size * 0.5f), py - (size * 0.55f), px + size, py - (size * 0.15f));
                c.DrawPath(wings, gull);
            }
        }

        // The sea: lit near the horizon, deepening towards the shore, a path of glitter under the sun.
        var shore = h * 0.85f;
        using (var sea = new SKPaint { Shader = SKShader.CreateLinearGradient(new SKPoint(0, horizon), new SKPoint(0, shore), new[] { new SKColor(0xD8, 0x7A, 0x7A, 240), new SKColor(0x5A, 0x4A, 0x8A, 245), new SKColor(0x1E, 0x3A, 0x6A, 250) }, new[] { 0f, 0.35f, 1f }, SKShaderTileMode.Clamp) })
        {
            c.DrawRect(0, horizon, w, h - horizon, sea);
        }

        // The sun's reflection: a soft glow on the water, broken into short tapered glints that fan out towards the
        // shore (solid bars read as a stack of planks).
        c.Save();
        c.ClipRect(new SKRect(0, horizon, w, shore));
        c.Scale(1, 4.5f, sun.X, horizon);
        SpringGlow(c, sun.X, horizon, s * 0.09f, new SKColor(0xFF, 0xC0, 0x60, 120));
        c.Restore();
        using (var glintBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.0015f))
        using (var glitter = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, MaskFilter = glintBlur })
        {
            var rows = 30;
            for (var k = 0; k < rows; k++)
            {
                var t = k / (float)rows;
                var y = horizon + ((shore - horizon) * t * t) + (s * 0.004f);
                var spread = s * (0.03f + (0.14f * t));
                glitter.StrokeWidth = s * 0.0025f * (1 + t);
                for (var g = 0; g < 2 + (int)(t * 4); g++)
                {
                    var gx = sun.X + (R(rng, -1, 1) * spread);
                    var len = s * R(rng, 0.006f, 0.02f) * (0.6f + t);
                    var fade = 1 - (Math.Abs(gx - sun.X) / (spread * 1.2f));
                    glitter.Color = new SKColor(0xFF, 0xE0, 0x90, (byte)(230 * fade * (1 - (t * 0.5f))));
                    c.DrawLine(gx - len, y, gx + len, y, glitter);
                }
            }

            for (var i = 0; i < 60; i++)
            {
                var t = R(rng, 0, 1);
                var y = horizon + ((shore - horizon) * t);
                var x = R(rng, 0, w);
                var len = s * R(rng, 0.01f, 0.04f) * (0.5f + t);
                glitter.Color = new SKColor(0xFF, 0xC8, 0xC8, (byte)rng.Next(40, 100));
                c.DrawRect(x - len, y, len * 2, s * 0.0025f, glitter);
            }
        }

        SpringHaze(c, w, horizon, h * 0.012f, new SKColor(0xFF, 0xD8, 0xA0, 150));

        // The beach: wet sand along the water's edge with a lace of foam, then warm dry sand.
        var wet = SpringRidge(w, h, s, shore, s * 0.01f, rng, 2f);
        SpringFillHill(c, wet.Path, shore - (s * 0.02f), h, new SKColor(0x9A, 0x6A, 0x78), new SKColor(0x6A, 0x46, 0x54), SKColors.Transparent, 0);
        using (var foam = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = s * 0.006f, Color = SKColors.White.WithAlpha(200) })
        using (var foamBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.002f))
        using (var lace = new SKPath())
        {
            foam.MaskFilter = foamBlur;
            for (var x = -10f; x <= w + 10; x += 6)
            {
                var y = wet.Y(x) + (MathF.Sin(x / s * 40) * s * 0.002f);
                if (x < 0)
                {
                    lace.MoveTo(x, y);
                }
                else
                {
                    lace.LineTo(x, y);
                }
            }

            c.DrawPath(lace, foam);
        }

        var sand = SpringRidge(w, h, s, h * 0.9f, s * 0.018f, rng, 1.2f);
        SpringFillHill(c, sand.Path, h * 0.86f, h, new SKColor(0xF2, 0xB8, 0x88), new SKColor(0xA8, 0x6A, 0x4A), new SKColor(0xFF, 0xE0, 0xB0, 200), s * 0.01f);

        // A starfish and a shell on the sand.
        SpringStarfish(c, w - (s * 0.28f), h * 0.955f, s * 0.045f, 18);
        SpringShell(c, s * 0.34f, h * 0.965f, s * 0.035f, -15);

        // Palm trees leaning in from both sides, as dark silhouettes against the sunset.
        var palm = new SKColor(0x1A, 0x0E, 0x22);
        SpringPalm(c, new SKPoint(-s * 0.02f, h + (s * 0.02f)), new SKPoint(s * 0.15f, h * 0.09f), s * 0.16f, s * 0.04f, s * 0.28f, palm, rng);
        SpringPalm(c, new SKPoint(w + (s * 0.03f), h + (s * 0.02f)), new SKPoint(w - (s * 0.1f), h * 0.17f), -s * 0.08f, s * 0.032f, s * 0.22f, palm, rng);

        // Dune grass at the foot of the palms.
        foreach (var right in new[] { false, true })
        {
            using var tuft = new SKPaint { IsAntialias = true, Color = palm };
            var bx = right ? w - (s * 0.07f) : s * 0.09f;
            for (var i = 0; i < 16; i++)
            {
                var x0 = bx + R(rng, -s * 0.09f, s * 0.09f);
                var len = s * R(rng, 0.05f, 0.13f);
                var lean = R(rng, -0.7f, 0.7f);
                var bw = s * 0.007f;
                using var blade = new SKPath();
                blade.MoveTo(x0 - bw, h + 2);
                blade.QuadTo(x0 + (lean * len * 0.3f), h - (len * 0.6f), x0 + (lean * len), h - len);
                blade.QuadTo(x0 + (lean * len * 0.3f) + bw, h - (len * 0.5f), x0 + bw, h + 2);
                blade.Close();
                c.DrawPath(blade, tuft);
            }
        }
    }

    /// <summary>
    /// A palm tree from <paramref name="root"/> up to <paramref name="crown"/>: a tapering ringed trunk bowed by
    /// <paramref name="bend"/>, and drooping fronds fringed with leaflets.
    /// </summary>
    private static void SpringPalm(SKCanvas c, SKPoint root, SKPoint crown, float bend, float width, float frondLen, SKColor colour, Random rng)
    {
        var c1 = new SKPoint(root.X + bend, root.Y - ((root.Y - crown.Y) * 0.4f));
        var c2 = new SKPoint(crown.X + (bend * 0.4f), crown.Y + ((root.Y - crown.Y) * 0.3f));
        SKPoint At(float t)
        {
            var u = 1 - t;
            return new SKPoint(
                (u * u * u * root.X) + (3 * u * u * t * c1.X) + (3 * u * t * t * c2.X) + (t * t * t * crown.X),
                (u * u * u * root.Y) + (3 * u * u * t * c1.Y) + (3 * u * t * t * c2.Y) + (t * t * t * crown.Y));
        }

        var left = new List<SKPoint>();
        var right = new List<SKPoint>();
        var steps = 40;
        for (var i = 0; i <= steps; i++)
        {
            var t = i / (float)steps;
            var p = At(t);
            var q = At(Math.Min(1, t + 0.01f));
            var o = At(Math.Max(0, t - 0.01f));
            var dx = q.X - o.X;
            var dy = q.Y - o.Y;
            var len = MathF.Sqrt((dx * dx) + (dy * dy));
            var half = width * (1 - (t * 0.45f)) / 2;
            var nx = -dy / len * half;
            var ny = dx / len * half;
            left.Add(new SKPoint(p.X + nx, p.Y + ny));
            right.Add(new SKPoint(p.X - nx, p.Y - ny));
        }

        using var trunk = new SKPath();
        trunk.MoveTo(left[0]);
        foreach (var p in left)
        {
            trunk.LineTo(p);
        }

        for (var i = right.Count - 1; i >= 0; i--)
        {
            trunk.LineTo(right[i]);
        }

        trunk.Close();
        using (var bark = new SKPaint { IsAntialias = true, Color = colour })
        {
            c.DrawPath(trunk, bark);
        }

        // Rings up the trunk, lit faintly by the sunset.
        using (var ring = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = width * 0.08f, Color = new SKColor(0xC0, 0x60, 0x50, 70) })
        {
            for (var i = 2; i < steps - 1; i += 2)
            {
                c.DrawLine(left[i], right[i + 1], ring);
            }
        }

        // Fronds: each a long arching leaf, widest in the middle, its edges cut into a feathered fringe of leaflets
        // that hang down from the spine.
        using var leaf = new SKPaint { IsAntialias = true, Color = colour };
        foreach (var deg in new[] { -168f, -138f, -105f, -72f, -40f, -10f, 22f, 150f })
        {
            var a = (deg + R(rng, -6, 6)) * MathF.PI / 180;
            var len = frondLen * R(rng, 0.85f, 1.08f);
            var dir = new SKPoint(MathF.Cos(a), MathF.Sin(a));
            var droop = len * (0.3f + (0.3f * Math.Abs(dir.X)));
            var ctrl = new SKPoint(crown.X + (dir.X * len * 0.6f), crown.Y + (dir.Y * len * 0.6f) - (len * 0.12f));
            var tip = new SKPoint(crown.X + (dir.X * len * 0.95f), crown.Y + (dir.Y * len * 0.65f) + droop);
            SKPoint F(float t)
            {
                var u = 1 - t;
                return new SKPoint((u * u * crown.X) + (2 * u * t * ctrl.X) + (t * t * tip.X), (u * u * crown.Y) + (2 * u * t * ctrl.Y) + (t * t * tip.Y));
            }

            var upper = new List<SKPoint>();
            var lower = new List<SKPoint>();
            var n = 26;
            for (var i = 0; i <= n; i++)
            {
                var t = i / (float)n;
                var p = F(t);
                var q = F(Math.Min(1, t + 0.01f));
                var o = F(Math.Max(0, t - 0.01f));
                var tx = q.X - o.X;
                var ty = q.Y - o.Y;
                var tl = MathF.Max(0.001f, MathF.Sqrt((tx * tx) + (ty * ty)));
                tx /= tl;
                ty /= tl;

                // Leaflets: long at the points of the fringe, short in the notches between, swept towards the tip and
                // hanging down.
                var spread = len * 0.2f * MathF.Pow(MathF.Sin(MathF.PI * Math.Min(1, (t * 0.95f) + 0.04f)), 0.8f);
                var reach = i % 2 == 0 ? spread : spread * 0.25f;
                var sweep = i % 2 == 0 ? 0.5f : 0f;
                foreach (var side in new[] { -1f, 1f })
                {
                    var nx = -ty * side;
                    var ny = tx * side;
                    var hang = ny >= 0 ? 0.7f : 0.45f;
                    var dx = nx + (tx * sweep);
                    var dy = ny + (ty * sweep) + hang;
                    var dl = MathF.Sqrt((dx * dx) + (dy * dy));
                    var pt = new SKPoint(p.X + (dx / dl * reach), p.Y + (dy / dl * reach));
                    (side > 0 ? lower : upper).Add(pt);
                }
            }

            using var frond = new SKPath();
            frond.MoveTo(crown);
            foreach (var p in upper)
            {
                frond.LineTo(p);
            }

            frond.LineTo(tip);
            for (var i = lower.Count - 1; i >= 0; i--)
            {
                frond.LineTo(lower[i]);
            }

            frond.Close();
            c.DrawPath(frond, leaf);
        }

        using var nut = new SKPaint { IsAntialias = true, Color = colour };
        c.DrawCircle(crown.X - (width * 0.3f), crown.Y + (width * 0.45f), width * 0.32f, nut);
        c.DrawCircle(crown.X + (width * 0.3f), crown.Y + (width * 0.5f), width * 0.3f, nut);
    }

    private static void SpringStarfish(SKCanvas c, float x, float y, float r, float rotation)
    {
        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(rotation);
        c.Scale(1, 0.7f);
        using var star = new SKPath();
        for (var i = 0; i < 10; i++)
        {
            var a = (i * MathF.PI / 5) - (MathF.PI / 2);
            var rr = i % 2 == 0 ? r : r * 0.42f;
            var p = new SKPoint(MathF.Cos(a) * rr, MathF.Sin(a) * rr);
            if (i == 0)
            {
                star.MoveTo(p);
            }
            else
            {
                star.LineTo(p);
            }
        }

        star.Close();
        SpringContactShadow(c, r * 0.1f, r * 0.2f, r * 1.05f, r * 0.9f, 90);
        using var corner = SKPathEffect.CreateCorner(r * 0.25f);
        using var fill = new SKPaint { IsAntialias = true, PathEffect = corner, Shader = SKShader.CreateRadialGradient(new SKPoint(-r * 0.2f, -r * 0.3f), r * 1.2f, new[] { new SKColor(0xFF, 0xA0, 0x60), new SKColor(0xE8, 0x60, 0x30), new SKColor(0xA0, 0x38, 0x18) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp) };
        c.DrawPath(star, fill);
        using var dot = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xE0, 0xB0, 200) };
        for (var i = 0; i < 5; i++)
        {
            var a = (i * MathF.PI * 2 / 5) - (MathF.PI / 2);
            foreach (var d in new[] { 0.3f, 0.5f, 0.7f })
            {
                c.DrawCircle(MathF.Cos(a) * r * d, MathF.Sin(a) * r * d, r * 0.05f, dot);
            }
        }

        c.Restore();
    }

    private static void SpringShell(SKCanvas c, float x, float y, float r, float rotation)
    {
        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(rotation);
        SpringContactShadow(c, 0, r * 0.4f, r * 1.1f, r * 0.3f, 90);
        using var fan = new SKPath();
        fan.MoveTo(0, r * 0.45f);
        fan.LineTo(-r, -r * 0.2f);
        fan.ArcTo(new SKRect(-r, -r * 0.9f, r, r * 0.5f), 200, 140, false);
        fan.Close();
        using var fill = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(0, r * 0.4f), r * 1.3f, new[] { new SKColor(0xFF, 0xE8, 0xD8), new SKColor(0xF4, 0xB8, 0xA0), new SKColor(0xC8, 0x80, 0x70) }, new[] { 0f, 0.6f, 1f }, SKShaderTileMode.Clamp) };
        c.DrawPath(fan, fill);
        using var rib = new SKPaint { IsAntialias = true, StrokeWidth = r * 0.05f, Color = new SKColor(0xB0, 0x68, 0x58, 160) };
        for (var i = 1; i < 6; i++)
        {
            var a = (200 + (i * 140 / 6f)) * MathF.PI / 180;
            c.DrawLine(0, r * 0.45f, MathF.Cos(a) * r * 0.95f, (-r * 0.2f) + (MathF.Sin(a) * r * 0.68f), rib);
        }

        c.Restore();
    }
}
