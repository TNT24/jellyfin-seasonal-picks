using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>The Easter and Valentine's scenes.</summary>
internal static partial class CoverArt
{
    private static readonly SKColor[] SpringPastels =
    {
        new(0xF4, 0x8F, 0xB1), new(0x7C, 0xC8, 0xF0), new(0xFF, 0xD0, 0x50),
        new(0xB3, 0x9D, 0xDB), new(0x86, 0xD4, 0x9C), new(0xFF, 0xA8, 0x7A)
    };

    // ---------- Easter, week 1: a bunny and painted eggs in a spring meadow ----------

    private static void Easter(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var ground = SpringMeadow(c, w, h, s, rng, golden: false);

        SpringBlossomBranch(c, -s * 0.02f, s * 0.03f, s * 0.62f, false, rng);

        // Tulips behind the eggs.
        var tulipColours = new[] { new SKColor(0xE8, 0x3A, 0x5A), new SKColor(0xFF, 0xB0, 0x30), new SKColor(0xF0, 0x6A, 0xA8), new SKColor(0xA8, 0x5A, 0xD0) };
        var tulipXs = w > h ? new[] { 0.05f, 0.11f, 0.4f, 0.45f, 0.5f } : new[] { 0.04f, 0.1f, 0.47f, 0.53f };
        for (var i = 0; i < tulipXs.Length; i++)
        {
            var x = (w * tulipXs[i]) + R(rng, -s * 0.01f, s * 0.01f);
            SpringTulip(c, x, ground(x) + (s * 0.05f), s * R(rng, 0.15f, 0.2f), tulipColours[i % tulipColours.Length], R(rng, -8, 8));
        }

        SpringBlades(c, w, s, rng, ground, s * 0.007f, 0.02f, 0.05f, new SKColor(0x2E, 0x70, 0x28), new SKColor(0x9C, 0xD8, 0x6A), s * 0.03f);

        // A cluster of painted eggs nestled in the grass on the left, a couple more in the middle on wide covers.
        var ex = w > h ? w * 0.22f : s * 0.25f;
        var eggs = new List<(float X, float Y, float W, float Rot, int Pattern)>
        {
            (ex - (s * 0.1f), s * 0.01f, s * 0.1f, -14, 0),
            (ex + (s * 0.1f), s * 0.015f, s * 0.095f, 12, 1),
            (ex, 0, s * 0.125f, 3, 2),
            (ex - (s * 0.045f), s * 0.05f, s * 0.08f, -22, 3),
            (ex + (s * 0.055f), s * 0.055f, s * 0.075f, 24, 4)
        };
        if (w > h)
        {
            eggs.Add((w * 0.5f, s * 0.0f, s * 0.085f, -8, 1));
            eggs.Add((w * 0.5f + (s * 0.08f), s * 0.02f, s * 0.07f, 16, 3));
        }

        var k = 0;
        foreach (var (x, dy, ew, rot, pattern) in eggs.OrderBy(e => e.Y))
        {
            SpringEgg(c, x, ground(x) + (s * 0.05f) + dy, ew, rot, SpringPastels[(k + 1) % SpringPastels.Length], SpringPastels[(k + 4) % SpringPastels.Length], pattern);
            k++;
        }

        // The bunny sitting up in the grass on the right.
        var bx = w - (s * 0.21f);
        SpringBunny(c, bx, ground(bx) + (s * 0.085f), s * 0.36f);

        // Short grass in front, daisies dotted through it.
        SpringBlades(c, w, s, rng, ground, s * 0.009f, 0.012f, 0.03f, new SKColor(0x2A, 0x6A, 0x26), new SKColor(0x8A, 0xD0, 0x5C), s * 0.095f);
        for (var i = 0; i < (w > h ? 9 : 6); i++)
        {
            var x = R(rng, 0, w);
            SpringDaisy(c, x, ground(x) + (s * R(rng, 0.075f, 0.1f)), s * R(rng, 0.014f, 0.022f));
        }

        SpringDriftingPetals(c, w, h, s, rng, 10);
    }

    /// <summary>
    /// A spring landscape: a soft sky with the sun's glow, a few clouds and bokeh, then far hills hazy with distance,
    /// a nearer hill with little trees, and the foreground meadow. Morning light, or warm golden hour. Returns the
    /// meadow's ground line.
    /// </summary>
    private static Func<float, float> SpringMeadow(SKCanvas c, int w, int h, float s, Random rng, bool golden)
    {
        var sun = golden ? new SKPoint(w * 0.13f, h * 0.68f) : new SKPoint(w * 0.86f, h * 0.09f);
        if (golden)
        {
            SpringSky(c, w, h, new[] { new SKColor(0x4C, 0x5C, 0xB0, 220), new SKColor(0xB0, 0x88, 0xC0, 150), new SKColor(0xFF, 0xC0, 0x90, 150), new SKColor(0xFF, 0xC8, 0x80, 230) }, new[] { 0f, 0.35f, 0.6f, 0.74f });
            SpringGlow(c, sun.X, sun.Y, s * 0.85f, new SKColor(0xFF, 0xC0, 0x60, 150));
            SpringGlow(c, sun.X, sun.Y, s * 0.16f, new SKColor(0xFF, 0xF8, 0xD8, 240));
        }
        else
        {
            SpringSky(c, w, h, new[] { new SKColor(0x3C, 0x90, 0xE0, 225), new SKColor(0x88, 0xC0, 0xEC, 150), new SKColor(0xD8, 0xEC, 0xF0, 130), new SKColor(0xFF, 0xF4, 0xDC, 220) }, new[] { 0f, 0.36f, 0.6f, 0.76f });
            SpringGlow(c, sun.X, sun.Y, s * 0.75f, new SKColor(0xFF, 0xF4, 0xC0, 170));
            SpringGlow(c, sun.X, sun.Y, s * 0.13f, new SKColor(0xFF, 0xFF, 0xF0, 245));
        }

        var cloudLit = SKColors.White.WithAlpha(245);
        var cloudShade = golden ? new SKColor(0xF0, 0xC0, 0xC0, 230) : new SKColor(0xD8, 0xE4, 0xF4, 230);
        SpringSoftCloud(c, w * (golden ? 0.7f : 0.36f), h * 0.12f, s * 0.06f, cloudLit, cloudShade);
        if (w > h)
        {
            SpringSoftCloud(c, w * (golden ? 0.42f : 0.62f), h * 0.22f, s * 0.045f, cloudLit, cloudShade);
        }

        SpringBokeh(c, w, h, s, rng, 6, new[] { new SKColor(0xFF, 0xF4, 0xD0), new SKColor(0xFF, 0xE0, 0xF0) }, 0, h * 0.3f, 0.015f, 0.035f);

        var far = SpringRidge(w, h, s, h * 0.72f, h * 0.06f, rng, 1.2f);
        SpringFillHill(c, far.Path, h * 0.64f, h * 0.8f, golden ? new SKColor(0x9A, 0x90, 0xA8) : new SKColor(0x7C, 0xAC, 0xB4), golden ? new SKColor(0xC8, 0xB0, 0xA0) : new SKColor(0xA8, 0xCC, 0xB8), golden ? new SKColor(0xFF, 0xD8, 0xA8, 140) : new SKColor(0xF0, 0xF8, 0xFF, 110), s * 0.008f);
        SpringHaze(c, w, h * 0.77f, h * 0.03f, golden ? new SKColor(0xFF, 0xE0, 0xB0, 110) : new SKColor(0xFF, 0xF6, 0xE0, 100));

        var mid = SpringRidge(w, h, s, h * 0.81f, h * 0.03f, rng, 1.1f);
        SpringFillHill(c, mid.Path, h * 0.76f, h * 0.9f, golden ? new SKColor(0xB8, 0xC8, 0x58) : new SKColor(0xA4, 0xD4, 0x64), golden ? new SKColor(0x6A, 0x90, 0x38) : new SKColor(0x5C, 0xA0, 0x44), golden ? new SKColor(0xFF, 0xE8, 0xA0, 190) : new SKColor(0xF8, 0xFF, 0xD0, 180), s * 0.01f);
        var blossomTree = new SKColor(0xF4, 0xB8, 0xCC);
        var greenTree = golden ? new SKColor(0x6A, 0x94, 0x3A) : new SKColor(0x4E, 0x96, 0x44);
        for (var i = 0; i < (w > h ? 7 : 5); i++)
        {
            var x = R(rng, 0, w);
            SpringTree(c, x, mid.Y(x) + (s * 0.008f), s * R(rng, 0.018f, 0.03f), i % 2 == 0 ? blossomTree : greenTree, new SKColor(0x5A, 0x40, 0x30));
        }

        var near = SpringRidge(w, h, s, h * 0.88f, h * 0.02f, rng, 1.6f);
        SpringFillHill(c, near.Path, h * 0.85f, h, golden ? new SKColor(0x7C, 0xB4, 0x3A) : new SKColor(0x5C, 0xB0, 0x40), new SKColor(0x1E, 0x5A, 0x22), golden ? new SKColor(0xFF, 0xE8, 0x9A, 190) : new SKColor(0xEC, 0xFF, 0xB8, 170), s * 0.012f);
        SpringBlades(c, w, s, rng, near.Y, s * 0.006f, 0.012f, 0.03f, new SKColor(0x3A, 0x84, 0x30), golden ? new SKColor(0xC8, 0xE0, 0x70) : new SKColor(0xA8, 0xDC, 0x70), s * 0.004f);
        return near.Y;
    }

    /// <summary>Blossom petals drifting through the top and bottom of the cover, off the title.</summary>
    private static void SpringDriftingPetals(SKCanvas c, int w, int h, float s, Random rng, int count)
    {
        var pinks = new[] { new SKColor(0xFF, 0xC8, 0xD8), new SKColor(0xF8, 0xA8, 0xC0), new SKColor(0xFF, 0xEE, 0xF2) };
        for (var i = 0; i < count; i++)
        {
            float x = R(rng, 0, w), y = R(rng, h * 0.04f, h * 0.8f), size = s * R(rng, 0.014f, 0.024f);
            if (SpringOffTitle(x, y, size, w, h))
            {
                SpringPetal(c, x, y, size, R(rng, 0, 360), pinks[rng.Next(pinks.Length)], 220);
            }
        }
    }

    /// <summary>
    /// A white rabbit sitting up, facing us: tall ears with pink insides, a round fluffy body with its haunches and
    /// feet, a glossy-eyed face. (x, ground) is under its middle; u is its height to the ear tips.
    /// </summary>
    private static void SpringBunny(SKCanvas c, float x, float ground, float u)
    {
        var fur = new SKColor(0xFF, 0xFC, 0xF8);
        var mid = new SKColor(0xEA, 0xE0, 0xD6);
        var shade = new SKColor(0xB4, 0xA4, 0x9A);
        var pink = new SKColor(0xF6, 0xA8, 0xBC);

        SpringContactShadow(c, x + (u * 0.03f), ground, u * 0.34f, u * 0.045f, 120);

        // Ears behind the head: one upright, one tipped out.
        foreach (var (side, rot) in new[] { (-1f, -10f), (1f, 18f) })
        {
            c.Save();
            c.Translate(x + (side * u * 0.065f), ground - (u * 0.6f));
            c.RotateDegrees(rot);
            var ear = new SKRect(-u * 0.058f, -u * 0.4f, u * 0.058f, u * 0.02f);
            using (var outer = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(-u * 0.06f, 0), new SKPoint(u * 0.06f, 0), new[] { fur, mid, shade }, new[] { 0f, 0.6f, 1f }, SKShaderTileMode.Clamp) })
            {
                c.DrawOval(ear, outer);
            }

            using (var inner = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, -u * 0.36f), new SKPoint(0, 0), new[] { Lighten(pink, 0.3f), pink, Darken(pink, 0.85f) }, null, SKShaderTileMode.Clamp) })
            {
                c.DrawOval(new SKRect(-u * 0.03f, -u * 0.35f, u * 0.03f, -u * 0.03f), inner);
            }

            c.Restore();
        }

        // Body, haunches, head and cheeks as one soft silhouette.
        using var shape = new SKPath();
        shape.AddOval(new SKRect(x - (u * 0.21f), ground - (u * 0.47f), x + (u * 0.21f), ground - (u * 0.02f)));
        shape.AddOval(new SKRect(x - (u * 0.27f), ground - (u * 0.24f), x - (u * 0.03f), ground));
        shape.AddOval(new SKRect(x + (u * 0.03f), ground - (u * 0.24f), x + (u * 0.27f), ground));
        shape.AddOval(new SKRect(x - (u * 0.16f), ground - (u * 0.67f), x + (u * 0.16f), ground - (u * 0.39f)));
        shape.AddOval(new SKRect(x - (u * 0.19f), ground - (u * 0.56f), x - (u * 0.01f), ground - (u * 0.4f)));
        shape.AddOval(new SKRect(x + (u * 0.01f), ground - (u * 0.56f), x + (u * 0.19f), ground - (u * 0.4f)));
        using var body = new SKPath();
        shape.Simplify(body);
        using (var fill = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(x + (u * 0.05f), ground - (u * 0.5f)), u * 0.62f, new[] { fur, fur, mid, shade }, new[] { 0f, 0.35f, 0.72f, 1f }, SKShaderTileMode.Clamp) })
        {
            c.DrawPath(body, fill);
        }

        SpringFluff(c, body, new SKPoint(x, ground - (u * 0.3f)), u * 0.025f, fur.WithAlpha(230));

        // Soft creases for the haunches and the chest.
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, u * 0.008f))
        using (var crease = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = u * 0.012f, Color = shade.WithAlpha(150), MaskFilter = blur })
        {
            c.DrawArc(new SKRect(x - (u * 0.27f), ground - (u * 0.24f), x - (u * 0.03f), ground), 200, 90, false, crease);
            c.DrawArc(new SKRect(x + (u * 0.03f), ground - (u * 0.24f), x + (u * 0.27f), ground), 250, 90, false, crease);
        }

        using (var bellyBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, u * 0.03f))
        using (var belly = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(170), MaskFilter = bellyBlur })
        {
            c.DrawOval(new SKRect(x - (u * 0.1f), ground - (u * 0.36f), x + (u * 0.1f), ground - (u * 0.08f)), belly);
        }

        // Feet and front paws.
        using (var paw = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, ground - (u * 0.12f)), new SKPoint(0, ground), new[] { fur, mid }, null, SKShaderTileMode.Clamp) })
        using (var toe = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = u * 0.006f, Color = shade.WithAlpha(170) })
        {
            foreach (var side in new[] { -1f, 1f })
            {
                c.DrawOval(new SKRect(x + (side * u * 0.17f) - (u * 0.085f), ground - (u * 0.05f), x + (side * u * 0.17f) + (u * 0.085f), ground + (u * 0.015f)), paw);
                c.DrawOval(new SKRect(x + (side * u * 0.055f) - (u * 0.045f), ground - (u * 0.14f), x + (side * u * 0.055f) + (u * 0.045f), ground - (u * 0.03f)), paw);
                c.DrawLine(x + (side * u * 0.055f), ground - (u * 0.05f), x + (side * u * 0.055f), ground - (u * 0.035f), toe);
            }
        }

        // Face: blush, glossy eyes, pink nose, mouth and whiskers.
        using (var blushBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, u * 0.012f))
        using (var blush = new SKPaint { IsAntialias = true, Color = pink.WithAlpha(130), MaskFilter = blushBlur })
        {
            c.DrawOval(new SKRect(x - (u * 0.14f), ground - (u * 0.49f), x - (u * 0.07f), ground - (u * 0.45f)), blush);
            c.DrawOval(new SKRect(x + (u * 0.07f), ground - (u * 0.49f), x + (u * 0.14f), ground - (u * 0.45f)), blush);
        }

        using (var eye = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(x, ground - (u * 0.55f)), u * 0.1f, new[] { new SKColor(0x5A, 0x38, 0x28), new SKColor(0x1A, 0x10, 0x0C) }, null, SKShaderTileMode.Clamp) })
        using (var shine = new SKPaint { IsAntialias = true, Color = SKColors.White })
        {
            foreach (var side in new[] { -1f, 1f })
            {
                var ex = x + (side * u * 0.068f);
                var ey = ground - (u * 0.545f);
                c.DrawOval(new SKRect(ex - (u * 0.026f), ey - (u * 0.032f), ex + (u * 0.026f), ey + (u * 0.032f)), eye);
                c.DrawCircle(ex + (u * 0.009f), ey - (u * 0.013f), u * 0.0095f, shine);
                c.DrawCircle(ex - (u * 0.008f), ey + (u * 0.012f), u * 0.004f, shine);
            }
        }

        using (var nose = new SKPath())
        using (var nosePaint = new SKPaint { IsAntialias = true, Color = new SKColor(0xF0, 0x80, 0x9C) })
        {
            nose.MoveTo(x - (u * 0.022f), ground - (u * 0.5f));
            nose.QuadTo(x, ground - (u * 0.51f), x + (u * 0.022f), ground - (u * 0.5f));
            nose.QuadTo(x + (u * 0.01f), ground - (u * 0.48f), x, ground - (u * 0.478f));
            nose.QuadTo(x - (u * 0.01f), ground - (u * 0.48f), x - (u * 0.022f), ground - (u * 0.5f));
            c.DrawPath(nose, nosePaint);
        }

        using (var ink = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = u * 0.006f, Color = new SKColor(0x9A, 0x70, 0x70) })
        using (var mouth = new SKPath())
        {
            mouth.MoveTo(x, ground - (u * 0.478f));
            mouth.LineTo(x, ground - (u * 0.465f));
            mouth.MoveTo(x - (u * 0.02f), ground - (u * 0.458f));
            mouth.QuadTo(x - (u * 0.008f), ground - (u * 0.452f), x, ground - (u * 0.465f));
            mouth.QuadTo(x + (u * 0.008f), ground - (u * 0.452f), x + (u * 0.02f), ground - (u * 0.458f));
            c.DrawPath(mouth, ink);
            ink.StrokeWidth = u * 0.003f;
            ink.Color = new SKColor(0xA0, 0x90, 0x88, 200);
            foreach (var side in new[] { -1f, 1f })
            {
                foreach (var dy in new[] { -0.015f, 0f, 0.015f })
                {
                    c.DrawLine(x + (side * u * 0.05f), ground - (u * 0.47f), x + (side * u * 0.17f), ground - (u * (0.47f - (dy * 1.6f))), ink);
                }
            }
        }
    }

    /// <summary>Short soft strokes standing out all round a silhouette, so it looks fluffy rather than cut out.</summary>
    private static void SpringFluff(SKCanvas c, SKPath outline, SKPoint centre, float len, SKColor colour)
    {
        // A soft halo first, then fine wisps of uneven length so the edge reads as fur, not a saw blade.
        using (var haloBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, len * 0.35f))
        using (var halo = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = len * 0.5f, Color = colour.WithAlpha((byte)(colour.Alpha * 0.3f)), MaskFilter = haloBlur })
        {
            c.DrawPath(outline, halo);
        }

        using var measure = new SKPathMeasure(outline, true);
        using var wispBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, len * 0.06f);
        using var tuft = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = len * 0.2f, Color = colour.WithAlpha((byte)(colour.Alpha * 0.4f)), MaskFilter = wispBlur };
        var k = 0;
        do
        {
            for (var d = 0f; d < measure.Length; d += len * 0.7f, k++)
            {
                if (!measure.GetPositionAndTangent(d, out var p, out var t))
                {
                    continue;
                }

                var nx = t.Y;
                var ny = -t.X;
                if ((nx * (p.X - centre.X)) + (ny * (p.Y - centre.Y)) < 0)
                {
                    nx = -nx;
                    ny = -ny;
                }

                var reach = len * (0.1f + (0.12f * (((k * 7) % 5) / 4f)));
                var sweep = ((k % 3) - 1) * 0.35f;
                c.DrawLine(p.X - (nx * len * 0.3f), p.Y - (ny * len * 0.3f), p.X + (nx * reach) + (t.X * reach * sweep), p.Y + (ny * reach) + (t.Y * reach * sweep), tuft);
            }
        }
        while (measure.NextContour());
    }

    /// <summary>
    /// A painted egg standing on (cx, bottom): a soft contact shadow, a pattern (0 zigzag bands, 1 dots, 2 wavy
    /// stripes, 3 flowers on a band, 4 two-tone with a scalloped edge), shading so it looks round, and a shine.
    /// </summary>
    private static void SpringEgg(SKCanvas c, float cx, float bottom, float width, float rotation, SKColor colour, SKColor accent, int pattern)
    {
        var height = width * 1.32f;
        float hw = width / 2, hh = height / 2;
        SpringContactShadow(c, cx + (width * 0.06f), bottom, width * 0.55f, width * 0.12f, 120);

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
        using (var fill = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, -hh), new SKPoint(0, hh), new[] { Lighten(colour, 0.25f), colour }, null, SKShaderTileMode.Clamp) })
        {
            c.DrawPath(egg, fill);
        }

        c.Save();
        c.ClipPath(egg, antialias: true);
        var deep = Darken(accent, 0.85f);
        var white = SKColors.White.WithAlpha(235);
        switch (pattern % 5)
        {
            case 1:
                using (var dot = new SKPaint { IsAntialias = true })
                {
                    var row = 0;
                    for (var y = -hh * 0.85f; y < hh; y += height * 0.15f, row++)
                    {
                        for (var x = -hw + (row % 2 == 0 ? 0 : width * 0.12f); x < hw; x += width * 0.24f)
                        {
                            dot.Color = row % 2 == 0 ? deep : white;
                            c.DrawCircle(x, y, width * (row % 2 == 0 ? 0.06f : 0.04f), dot);
                        }
                    }
                }

                break;
            case 2:
                using (var stripe = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = height * 0.08f, Color = deep })
                using (var line = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = height * 0.022f, Color = white })
                {
                    foreach (var y in new[] { -hh * 0.55f, -hh * 0.05f, hh * 0.45f })
                    {
                        using var wave = new SKPath();
                        wave.MoveTo(-width, y);
                        for (var x = -width; x <= width; x += width * 0.05f)
                        {
                            wave.LineTo(x, y + (MathF.Sin(x / width * 18) * height * 0.025f));
                        }

                        c.DrawPath(wave, stripe);
                        c.Save();
                        c.Translate(0, height * 0.075f);
                        c.DrawPath(wave, line);
                        c.Restore();
                    }
                }

                break;
            case 3:
                using (var belt = new SKPaint { IsAntialias = true, Color = deep })
                using (var edge = new SKPaint { IsAntialias = true, Color = white })
                {
                    c.DrawRect(-hw, -hh * 0.2f, width, height * 0.26f, belt);
                    c.DrawRect(-hw, -hh * 0.2f - (height * 0.02f), width, height * 0.02f, edge);
                    c.DrawRect(-hw, (-hh * 0.2f) + (height * 0.26f), width, height * 0.02f, edge);
                }

                foreach (var fx in new[] { -0.3f, 0f, 0.3f })
                {
                    Blossom(c, width * fx, (-hh * 0.2f) + (height * 0.13f), width * 0.11f, white);
                }

                using (var dots = new SKPaint { IsAntialias = true, Color = deep })
                {
                    foreach (var (fx, fy) in new[] { (-0.15f, -0.65f), (0.18f, -0.6f), (-0.2f, 0.55f), (0.2f, 0.6f), (0f, 0.75f) })
                    {
                        c.DrawCircle(width * fx, hh * fy, width * 0.035f, dots);
                    }
                }

                break;
            case 4:
                using (var lower = new SKPaint { IsAntialias = true, Color = deep })
                using (var scallop = new SKPaint { IsAntialias = true, Color = white })
                {
                    var y0 = hh * 0.05f;
                    c.DrawRect(-hw, y0, width, hh, lower);
                    for (var x = -hw; x < hw + (width * 0.1f); x += width * 0.16f)
                    {
                        c.DrawCircle(x, y0, width * 0.08f, scallop);
                        c.DrawCircle(x, y0 + (height * 0.02f), width * 0.06f, lower);
                    }

                    foreach (var (fx, fy) in new[] { (-0.2f, 0.5f), (0.15f, 0.45f), (0f, 0.75f), (-0.12f, -0.5f), (0.16f, -0.35f) })
                    {
                        c.DrawCircle(width * fx, hh * fy, width * 0.04f, scallop);
                    }
                }

                break;
            default:
                using (var zig = new SKPaint { IsAntialias = true, Color = deep, Style = SKPaintStyle.Stroke, StrokeWidth = width * 0.09f, StrokeJoin = SKStrokeJoin.Round })
                using (var thin = new SKPaint { IsAntialias = true, Color = white, Style = SKPaintStyle.Stroke, StrokeWidth = width * 0.025f })
                {
                    foreach (var y in new[] { -height * 0.17f, height * 0.17f })
                    {
                        using var z = new SKPath();
                        z.MoveTo(-width, y);
                        for (var x = -width; x < width; x += width * 0.18f)
                        {
                            z.LineTo(x + (width * 0.09f), y - (width * 0.09f));
                            z.LineTo(x + (width * 0.18f), y);
                        }

                        c.DrawPath(z, zig);
                        c.DrawLine(-hw, y - (width * 0.15f), hw, y - (width * 0.15f), thin);
                        c.DrawLine(-hw, y + (width * 0.07f), hw, y + (width * 0.07f), thin);
                    }
                }

                using (var dots = new SKPaint { IsAntialias = true, Color = white })
                {
                    c.DrawCircle(-width * 0.16f, -hh * 0.65f, width * 0.045f, dots);
                    c.DrawCircle(width * 0.14f, -hh * 0.68f, width * 0.035f, dots);
                    c.DrawCircle(0, 0, width * 0.05f, dots);
                    c.DrawCircle(width * 0.18f, hh * 0.7f, width * 0.04f, dots);
                    c.DrawCircle(-width * 0.18f, hh * 0.68f, width * 0.035f, dots);
                }

                break;
        }

        // Round shading: light from the upper left, a darker rim, a little reflected light along the bottom.
        using (var shade = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(-hw * 0.35f, -hh * 0.35f), height * 0.95f, new[] { SKColors.White.WithAlpha(80), SKColors.Transparent, SKColors.Black.WithAlpha(120) }, new[] { 0f, 0.45f, 1f }, SKShaderTileMode.Clamp) })
        {
            c.DrawPath(egg, shade);
        }

        c.Restore();
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, width * 0.03f))
        using (var shine = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(190), MaskFilter = blur })
        {
            c.Save();
            c.RotateDegrees(-28, -hw * 0.4f, -hh * 0.5f);
            c.DrawOval(new SKRect(-hw * 0.6f, -hh * 0.66f, -hw * 0.22f, -hh * 0.34f), shine);
            c.Restore();
        }

        c.Restore();
    }

    private static void SpringDaisy(SKCanvas c, float x, float y, float r)
    {
        c.Save();
        c.Translate(x, y);
        c.Scale(1, 0.75f);
        using var petal = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(0, 0), r, new[] { new SKColor(0xE0, 0xE0, 0xE8), SKColors.White }, null, SKShaderTileMode.Clamp) };
        for (var i = 0; i < 12; i++)
        {
            c.Save();
            c.RotateDegrees(i * 30);
            c.DrawOval(new SKRect(-r * 0.17f, -r, r * 0.17f, -r * 0.1f), petal);
            c.Restore();
        }

        using var middle = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(-r * 0.1f, -r * 0.1f), r * 0.35f, new[] { new SKColor(0xFF, 0xE0, 0x50), new SKColor(0xE0, 0x9A, 0x10) }, null, SKShaderTileMode.Clamp) };
        c.DrawCircle(0, 0, r * 0.3f, middle);
        c.Restore();
    }

    /// <summary>A tulip on a slender stem with two long leaves; its cup is three overlapping shaded petals.</summary>
    private static void SpringTulip(SKCanvas c, float x, float ground, float height, SKColor colour, float lean)
    {
        c.Save();
        c.Translate(x, ground);
        c.RotateDegrees(lean);
        using (var stem = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = height * 0.035f, Shader = SKShader.CreateLinearGradient(new SKPoint(-height * 0.02f, 0), new SKPoint(height * 0.02f, 0), new[] { new SKColor(0x6C, 0xB0, 0x4C), new SKColor(0x2E, 0x6C, 0x2A) }, null, SKShaderTileMode.Clamp) })
        {
            c.DrawLine(0, 0, 0, -height * 0.74f, stem);
        }

        foreach (var side in new[] { -1f, 1f })
        {
            using var leaf = new SKPath();
            leaf.MoveTo(0, 0);
            leaf.QuadTo(side * height * 0.2f, -height * 0.18f, side * height * 0.14f, -height * 0.5f);
            leaf.QuadTo(side * height * 0.04f, -height * 0.2f, 0, -height * 0.02f);
            leaf.Close();
            using var fill = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(side * height * 0.15f, -height * 0.4f), new[] { new SKColor(0x2E, 0x70, 0x2C), new SKColor(0x7C, 0xC0, 0x5A) }, null, SKShaderTileMode.Clamp) };
            c.DrawPath(leaf, fill);
        }

        var top = -height;
        var cupW = height * 0.15f;
        foreach (var (dx, rot, tone) in new[] { (0f, 0f, 0.8f), (-0.45f, -12f, 1f), (0.45f, 12f, 0.92f) })
        {
            c.Save();
            c.Translate(dx * cupW, top + (height * 0.2f));
            c.RotateDegrees(rot);
            using var petal = new SKPath();
            petal.MoveTo(0, height * 0.06f);
            petal.CubicTo(-cupW * 0.95f, height * 0.05f, -cupW * 0.85f, -height * 0.12f, 0, -height * 0.21f);
            petal.CubicTo(cupW * 0.85f, -height * 0.12f, cupW * 0.95f, height * 0.05f, 0, height * 0.06f);
            petal.Close();
            var col = Darken(colour, tone);
            using var fill = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, -height * 0.2f), new SKPoint(0, height * 0.06f), new[] { Lighten(col, 0.35f), col, Darken(col, 0.65f) }, new[] { 0f, 0.45f, 1f }, SKShaderTileMode.Clamp) };
            c.DrawPath(petal, fill);
            c.Restore();
        }

        using var shine = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = height * 0.012f, Color = SKColors.White.WithAlpha(110) };
        c.DrawLine(-cupW * 0.55f, top + (height * 0.1f), -cupW * 0.45f, top + (height * 0.2f), shine);
        c.Restore();
    }

    /// <summary>A flowering branch reaching in from a corner: a tapering bough, twigs, blossoms, buds and new leaves.</summary>
    private static void SpringBlossomBranch(SKCanvas c, float x, float y, float size, bool mirror, Random rng)
    {
        c.Save();
        c.Translate(x, y);
        c.Scale(mirror ? -1 : 1, 1);

        var bark = new SKColor(0x4A, 0x32, 0x2A);
        void Bough(SKPoint a, SKPoint b, SKPoint ctrl, float w0, float w1)
        {
            using var p = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, Color = bark };
            var prev = a;
            for (var i = 1; i <= 16; i++)
            {
                var t = i / 16f;
                var u = 1 - t;
                var pt = new SKPoint((u * u * a.X) + (2 * u * t * ctrl.X) + (t * t * b.X), (u * u * a.Y) + (2 * u * t * ctrl.Y) + (t * t * b.Y));
                p.StrokeWidth = w0 + ((w1 - w0) * t);
                c.DrawLine(prev, pt, p);
                prev = pt;
            }
        }

        SKPoint P(float px, float py) => new(size * px, size * py);
        Bough(P(-0.05f, -0.02f), P(0.62f, 0.12f), P(0.3f, -0.04f), size * 0.03f, size * 0.008f);
        Bough(P(0.18f, 0.0f), P(0.36f, 0.24f), P(0.24f, 0.14f), size * 0.014f, size * 0.004f);
        Bough(P(0.38f, 0.05f), P(0.5f, -0.04f), P(0.44f, 0.04f), size * 0.01f, size * 0.004f);
        Bough(P(0.05f, 0.0f), P(0.12f, 0.18f), P(0.1f, 0.06f), size * 0.012f, size * 0.004f);

        // New leaves, then blossoms all along, then a few deep-pink buds.
        foreach (var (lx, ly, rot) in new[] { (0.3f, 0.02f, 30f), (0.46f, 0.11f, 140f), (0.22f, 0.2f, 200f), (0.58f, 0.07f, 10f) })
        {
            c.Save();
            c.Translate(size * lx, size * ly);
            c.RotateDegrees(rot);
            using var leaf = new SKPath();
            leaf.MoveTo(0, 0);
            leaf.QuadTo(size * 0.025f, -size * 0.02f, size * 0.06f, 0);
            leaf.QuadTo(size * 0.025f, size * 0.02f, 0, 0);
            using var fill = new SKPaint { IsAntialias = true, Color = new SKColor(0x7C, 0xC0, 0x50) };
            c.DrawPath(leaf, fill);
            c.Restore();
        }

        var tones = new[] { new SKColor(0xFF, 0xE4, 0xEC), new SKColor(0xF8, 0xB8, 0xCC), new SKColor(0xFF, 0xF6, 0xF8) };
        var spots = new[] { (0.04f, 0.0f), (0.1f, 0.06f), (0.13f, 0.17f), (0.2f, -0.02f), (0.27f, 0.08f), (0.33f, 0.2f), (0.36f, 0.25f), (0.34f, -0.02f), (0.42f, 0.06f), (0.48f, -0.03f), (0.52f, 0.1f), (0.6f, 0.12f), (0.25f, 0.15f), (0.15f, 0.08f) };
        foreach (var (bx, by) in spots)
        {
            SpringBlossom(c, size * (bx + R(rng, -0.012f, 0.012f)), size * (by + R(rng, -0.012f, 0.012f)), size * R(rng, 0.03f, 0.045f), tones[rng.Next(tones.Length)], R(rng, 0, 72));
        }

        using var bud = new SKPaint { IsAntialias = true, Color = new SKColor(0xE8, 0x5A, 0x8A) };
        foreach (var (bx, by) in new[] { (0.56f, 0.04f), (0.4f, 0.13f), (0.08f, 0.13f), (0.64f, 0.13f) })
        {
            c.DrawOval(new SKRect(size * (bx - 0.009f), size * (by - 0.013f), size * (bx + 0.009f), size * (by + 0.013f)), bud);
        }

        c.Restore();
    }

    /// <summary>A five-petalled blossom: cupped petals paler at the rim, a pink heart and a ring of stamens.</summary>
    private static void SpringBlossom(SKCanvas c, float x, float y, float r, SKColor colour, float rotation)
    {
        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(rotation);
        using var petal = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(0, 0), r, new[] { new SKColor(0xE8, 0x70, 0x98), colour, Lighten(colour, 0.5f) }, new[] { 0f, 0.45f, 1f }, SKShaderTileMode.Clamp) };
        using var shape = new SKPath();
        for (var i = 0; i < 5; i++)
        {
            c.Save();
            c.RotateDegrees(i * 72);
            shape.Reset();
            shape.MoveTo(0, 0);
            shape.CubicTo(-r * 0.6f, -r * 0.3f, -r * 0.55f, -r * 0.95f, -r * 0.08f, -r);
            shape.QuadTo(0, -r * 0.9f, r * 0.08f, -r);
            shape.CubicTo(r * 0.55f, -r * 0.95f, r * 0.6f, -r * 0.3f, 0, 0);
            c.DrawPath(shape, petal);
            c.Restore();
        }

        using var stamen = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xD8, 0x60) };
        for (var i = 0; i < 7; i++)
        {
            var a = i * MathF.PI * 2 / 7;
            c.DrawCircle(MathF.Cos(a) * r * 0.25f, MathF.Sin(a) * r * 0.25f, r * 0.06f, stamen);
        }

        c.Restore();
    }

    // ---------- Easter, week 2: a basket of eggs and chicks in a golden-hour meadow ----------

    private static void ChickBasket(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var ground = SpringMeadow(c, w, h, s, rng, golden: true);

        SpringBlossomBranch(c, w + (s * 0.02f), s * 0.03f, s * 0.6f, true, rng);
        SpringButterfly(c, w * 0.14f, h * 0.17f, s * 0.04f, -18, new SKColor(0xFF, 0xA8, 0x30));
        SpringButterfly(c, w * 0.3f, h * 0.27f, s * 0.03f, 12, new SKColor(0x6C, 0xB8, 0xF0));

        // Tulips gathered on both sides.
        var tulipColours = new[] { new SKColor(0xE8, 0x3A, 0x5A), new SKColor(0xFF, 0xB0, 0x30), new SKColor(0xF0, 0x6A, 0xA8), new SKColor(0xA8, 0x5A, 0xD0), new SKColor(0xFF, 0x70, 0x40) };
        var tulipXs = w > h ? new[] { 0.03f, 0.08f, 0.13f, 0.88f, 0.94f } : new[] { 0.03f, 0.09f, 0.94f };
        for (var i = 0; i < tulipXs.Length; i++)
        {
            var x = (w * tulipXs[i]) + R(rng, -s * 0.01f, s * 0.01f);
            SpringTulip(c, x, ground(x) + (s * 0.06f), s * R(rng, 0.17f, 0.22f), tulipColours[i % tulipColours.Length], R(rng, -9, 9));
        }

        SpringBlades(c, w, s, rng, ground, s * 0.007f, 0.02f, 0.05f, new SKColor(0x3A, 0x70, 0x24), new SKColor(0xC8, 0xE0, 0x70), s * 0.03f);

        // The basket, a chick beside it, and one just hatched further over.
        var basketX = w * 0.62f;
        SpringBasket(c, basketX, ground(basketX) + (s * 0.1f), s * 0.33f, rng);
        var chickX = w > h ? w * 0.36f : w * 0.27f;
        SpringChick(c, chickX, ground(chickX) + (s * 0.085f), s * 0.11f, false);
        var shellX = chickX - (s * 0.13f);
        SpringHatchling(c, shellX, ground(shellX) + (s * 0.09f), s * 0.075f, SpringPastels[3]);
        SpringEgg(c, chickX + (s * 0.1f), ground(chickX) + (s * 0.095f), s * 0.07f, 18, SpringPastels[0], SpringPastels[2], 1);

        SpringBlades(c, w, s, rng, ground, s * 0.009f, 0.012f, 0.03f, new SKColor(0x34, 0x66, 0x22), new SKColor(0xB8, 0xD8, 0x60), s * 0.1f);
        for (var i = 0; i < (w > h ? 8 : 5); i++)
        {
            var x = R(rng, 0, w);
            SpringDaisy(c, x, ground(x) + (s * R(rng, 0.08f, 0.105f)), s * R(rng, 0.014f, 0.02f));
        }

        SpringDriftingPetals(c, w, h, s, rng, 8);
    }

    /// <summary>
    /// A fluffy chick standing on (x, ground), u tall: round body and head with a fuzzy edge, a wing, a tuft, a glossy
    /// eye, a little orange beak and feet.
    /// </summary>
    private static void SpringChick(SKCanvas c, float x, float ground, float u, bool faceLeft, bool feet = true)
    {
        var dir = faceLeft ? -1 : 1;
        SpringContactShadow(c, x, ground, u * 0.4f, u * 0.06f, 110);
        var orange = new SKColor(0xF5, 0x8A, 0x1C);
        if (feet)
        {
            using var foot = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = u * 0.035f, Color = orange };
            foreach (var fx in new[] { -0.12f, 0.12f })
            {
                var bx = x + (fx * u);
                c.DrawLine(bx, ground - (u * 0.1f), bx, ground, foot);
                c.DrawLine(bx, ground, bx - (u * 0.07f), ground + (u * 0.01f), foot);
                c.DrawLine(bx, ground, bx + (u * 0.07f), ground + (u * 0.01f), foot);
            }
        }

        using var shape = new SKPath();
        shape.AddOval(new SKRect(x - (u * 0.36f), ground - (u * 0.68f), x + (u * 0.36f), ground - (u * 0.06f)));
        shape.AddCircle(x + (dir * u * 0.1f), ground - (u * 0.78f), u * 0.24f);
        using var body = new SKPath();
        shape.Simplify(body);
        using (var fluff = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(x - (u * 0.1f), ground - (u * 0.8f)), u * 0.95f, new[] { new SKColor(0xFF, 0xF6, 0xB8), new SKColor(0xFF, 0xD8, 0x40), new SKColor(0xE6, 0xA0, 0x10) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp) })
        {
            c.DrawPath(body, fluff);
        }

        SpringFluff(c, body, new SKPoint(x, ground - (u * 0.45f)), u * 0.05f, new SKColor(0xFF, 0xE0, 0x60, 230));

        // Tuft on the head.
        using (var tuft = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = u * 0.03f, Color = new SKColor(0xFF, 0xD0, 0x30) })
        {
            var hx = x + (dir * u * 0.1f);
            var hy = ground - (u * 1.0f);
            foreach (var (dx, ex) in new[] { (-0.04f, -0.08f), (0f, 0.02f), (0.04f, 0.1f) })
            {
                using var curl = new SKPath();
                curl.MoveTo(hx + (dx * u), hy + (u * 0.02f));
                curl.QuadTo(hx + (dx * u), hy - (u * 0.08f), hx + (ex * u), hy - (u * 0.08f));
                c.DrawPath(curl, tuft);
            }
        }

        // Wing.
        using (var wing = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, ground - (u * 0.5f)), new SKPoint(0, ground - (u * 0.2f)), new[] { new SKColor(0xFF, 0xD0, 0x38), new SKColor(0xE0, 0x98, 0x10) }, null, SKShaderTileMode.Clamp) })
        {
            c.Save();
            c.RotateDegrees(dir * -25, x - (dir * u * 0.08f), ground - (u * 0.36f));
            c.DrawOval(new SKRect(x - (dir * u * 0.08f) - (u * 0.16f), ground - (u * 0.48f), x - (dir * u * 0.08f) + (u * 0.16f), ground - (u * 0.24f)), wing);
            c.Restore();
        }

        // Face.
        var fx0 = x + (dir * u * 0.2f);
        var fy0 = ground - (u * 0.82f);
        using (var eye = new SKPaint { IsAntialias = true, Color = new SKColor(0x22, 0x16, 0x10) })
        using (var shine = new SKPaint { IsAntialias = true, Color = SKColors.White })
        {
            c.DrawOval(new SKRect(fx0 - (u * 0.035f), fy0 - (u * 0.045f), fx0 + (u * 0.035f), fy0 + (u * 0.045f)), eye);
            c.DrawCircle(fx0 + (u * 0.012f), fy0 - (u * 0.018f), u * 0.014f, shine);
        }

        using (var blushBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, u * 0.02f))
        using (var blush = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0x90, 0x70, 120), MaskFilter = blushBlur })
        {
            c.DrawCircle(fx0 - (dir * u * 0.02f), fy0 + (u * 0.09f), u * 0.045f, blush);
        }

        using (var beak = new SKPaint { IsAntialias = true, Color = orange })
        using (var lower = new SKPaint { IsAntialias = true, Color = Darken(orange, 0.8f) })
        using (var top = new SKPath())
        using (var bottom = new SKPath())
        {
            var bx = x + (dir * u * 0.3f);
            var by = ground - (u * 0.76f);
            top.MoveTo(bx, by - (u * 0.04f));
            top.LineTo(bx + (dir * u * 0.12f), by);
            top.LineTo(bx, by + (u * 0.01f));
            top.Close();
            bottom.MoveTo(bx, by + (u * 0.01f));
            bottom.LineTo(bx + (dir * u * 0.09f), by + (u * 0.015f));
            bottom.LineTo(bx, by + (u * 0.045f));
            bottom.Close();
            c.DrawPath(bottom, lower);
            c.DrawPath(top, beak);
        }
    }

    /// <summary>A chick just hatched: its head and shoulders up out of the lower half of a cracked egg.</summary>
    private static void SpringHatchling(SKCanvas c, float x, float ground, float width, SKColor colour)
    {
        SpringChick(c, x, ground - (width * 0.3f), width * 1.1f, true, feet: false);
        var height = width * 0.75f;
        using var shell = new SKPath();
        shell.MoveTo(x - (width / 2), ground - (height * 0.7f));
        var teeth = 6;
        for (var i = 1; i <= teeth; i++)
        {
            shell.LineTo(x - (width / 2) + (width * (i - 0.5f) / teeth), ground - (height * (i % 2 == 0 ? 0.7f : 0.98f)));
            shell.LineTo(x - (width / 2) + (width * i / teeth), ground - (height * 0.7f));
        }

        shell.CubicTo(x + (width * 0.52f), ground - (height * 0.2f), x + (width * 0.3f), ground, x, ground);
        shell.CubicTo(x - (width * 0.3f), ground, x - (width * 0.52f), ground - (height * 0.2f), x - (width / 2), ground - (height * 0.7f));
        shell.Close();
        SpringContactShadow(c, x, ground, width * 0.55f, width * 0.1f, 110);
        using var fill = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(x - (width * 0.2f), ground - (height * 0.6f)), width, new[] { Lighten(colour, 0.5f), colour, Darken(colour, 0.7f) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp) };
        c.DrawPath(shell, fill);
        using var dots = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(220) };
        foreach (var (fx, fy) in new[] { (-0.2f, 0.4f), (0.15f, 0.3f), (0f, 0.15f), (0.25f, 0.55f), (-0.25f, 0.6f) })
        {
            c.DrawCircle(x + (width * fx), ground - (height * fy), width * 0.035f, dots);
        }
    }

    /// <summary>
    /// A woven wicker basket on (x, ground), bw wide: a braided handle, Easter grass, eggs and a chick peeking over a
    /// basket-weave body with a braided rim and a ribbon bow.
    /// </summary>
    private static void SpringBasket(SKCanvas c, float x, float ground, float bw, Random rng)
    {
        var rimY = ground - (bw * 0.4f);
        var rimRy = bw * 0.1f;
        var wicker = new SKColor(0xC8, 0x8E, 0x4C);
        SpringContactShadow(c, x + (bw * 0.04f), ground, bw * 0.55f, bw * 0.06f, 140);

        // Inside and the handle behind it.
        using (var inside = new SKPaint { IsAntialias = true, Color = new SKColor(0x4A, 0x2C, 0x14) })
        {
            c.DrawOval(new SKRect(x - (bw / 2), rimY - rimRy, x + (bw / 2), rimY + rimRy), inside);
        }

        using (var handle = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = bw * 0.05f, Shader = SKShader.CreateLinearGradient(new SKPoint(x - (bw / 2), 0), new SKPoint(x + (bw / 2), 0), new[] { new SKColor(0x8A, 0x56, 0x28), wicker, new SKColor(0x7A, 0x4A, 0x22) }, null, SKShaderTileMode.Clamp) })
        using (var twist = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = bw * 0.008f, Color = new SKColor(0x5A, 0x36, 0x18, 200) })
        {
            var arc = new SKRect(x - (bw * 0.44f), rimY - (bw * 0.46f), x + (bw * 0.44f), rimY + (bw * 0.3f));
            c.DrawArc(arc, 180, 180, false, handle);
            for (var a = 185f; a < 360; a += 7)
            {
                var rad = a * MathF.PI / 180;
                var px = arc.MidX + (MathF.Cos(rad) * arc.Width / 2);
                var py = arc.MidY + (MathF.Sin(rad) * arc.Height / 2);
                c.DrawLine(px - (bw * 0.012f), py - (bw * 0.018f), px + (bw * 0.012f), py + (bw * 0.018f), twist);
            }
        }

        // Easter grass, then the eggs and a chick sitting in it.
        using (var shred = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = bw * 0.006f })
        {
            for (var i = 0; i < 70; i++)
            {
                var sx = x + R(rng, -bw * 0.48f, bw * 0.48f);
                var sy = rimY + R(rng, -rimRy * 0.4f, rimRy * 0.6f);
                shred.Color = (i % 3 == 0 ? new SKColor(0xB8, 0xE8, 0x80) : new SKColor(0x6C, 0xC0, 0x50)).WithAlpha(230);
                using var curl = new SKPath();
                curl.MoveTo(sx, sy);
                curl.QuadTo(sx + R(rng, -bw * 0.05f, bw * 0.05f), sy - (bw * R(rng, 0.03f, 0.08f)), sx + R(rng, -bw * 0.06f, bw * 0.06f), sy - (bw * R(rng, 0.02f, 0.06f)));
                c.DrawPath(curl, shred);
            }
        }

        var inBasket = new[] { (-0.28f, 0.02f, 0.2f, -22f, 3), (0.27f, 0.03f, 0.19f, 20f, 0), (0.1f, 0.0f, 0.18f, 10f, 4), (-0.12f, 0.04f, 0.17f, -8f, 2) };
        for (var i = 0; i < inBasket.Length; i++)
        {
            var (fx, dy, ew, rot, pattern) = inBasket[i];
            SpringEgg(c, x + (bw * fx), rimY + (bw * (0.07f + dy)), bw * ew, rot, SpringPastels[(i + 2) % SpringPastels.Length], SpringPastels[(i + 5) % SpringPastels.Length], pattern);
        }

        SpringChick(c, x - (bw * 0.03f), rimY + (bw * 0.14f), bw * 0.34f, true, feet: false);

        // The basket body: rows of woven strands, shaded round, a braided rim and a bow.
        using var bodyPath = new SKPath();
        bodyPath.MoveTo(x - (bw / 2), rimY);
        bodyPath.CubicTo(x - (bw * 0.5f), ground - (bw * 0.12f), x - (bw * 0.4f), ground, x - (bw * 0.25f), ground);
        bodyPath.LineTo(x + (bw * 0.25f), ground);
        bodyPath.CubicTo(x + (bw * 0.4f), ground, x + (bw * 0.5f), ground - (bw * 0.12f), x + (bw / 2), rimY);
        bodyPath.ArcTo(new SKRect(x - (bw / 2), rimY - rimRy, x + (bw / 2), rimY + rimRy), 0, 180, false);
        bodyPath.Close();
        using (var fill = new SKPaint { IsAntialias = true, Color = new SKColor(0x8A, 0x56, 0x28) })
        {
            c.DrawPath(bodyPath, fill);
        }

        c.Save();
        c.ClipPath(bodyPath, antialias: true);
        using (var strand = new SKPaint { IsAntialias = true })
        {
            var rowH = bw * 0.055f;
            var row = 0;
            for (var y = rimY - rimRy; y < ground + rowH; y += rowH, row++)
            {
                for (var sx = x - (bw * 0.55f) + (row % 2 == 0 ? 0 : bw * 0.045f); sx < x + (bw * 0.55f); sx += bw * 0.09f)
                {
                    var cell = new SKRect(sx, y + (rowH * 0.08f), sx + (bw * 0.085f), y + (rowH * 0.92f));
                    using var shade = SKShader.CreateLinearGradient(new SKPoint(0, cell.Top), new SKPoint(0, cell.Bottom), new[] { Lighten(wicker, 0.3f), wicker, Darken(wicker, 0.7f) }, new[] { 0f, 0.4f, 1f }, SKShaderTileMode.Clamp);
                    strand.Shader = shade;
                    c.DrawRoundRect(cell, rowH * 0.45f, rowH * 0.45f, strand);
                }
            }
        }

        using (var round = new SKPaint { Shader = SKShader.CreateLinearGradient(new SKPoint(x - (bw / 2), 0), new SKPoint(x + (bw / 2), 0), new[] { SKColors.Black.WithAlpha(120), SKColors.Transparent, SKColors.Transparent, SKColors.Black.WithAlpha(150) }, new[] { 0f, 0.3f, 0.55f, 1f }, SKShaderTileMode.Clamp) })
        {
            c.DrawRect(x - bw, rimY - rimRy, bw * 2, bw, round);
        }

        using (var under = new SKPaint { Shader = SKShader.CreateLinearGradient(new SKPoint(0, ground - (bw * 0.15f)), new SKPoint(0, ground), new[] { SKColors.Transparent, SKColors.Black.WithAlpha(110) }, null, SKShaderTileMode.Clamp) })
        {
            c.DrawRect(x - bw, ground - (bw * 0.15f), bw * 2, bw * 0.15f, under);
        }

        c.Restore();

        // Braided rim along the front.
        using (var rim = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = bw * 0.05f, StrokeCap = SKStrokeCap.Round, Color = Darken(wicker, 0.9f) })
        using (var plait = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = bw * 0.018f, Color = Lighten(wicker, 0.25f) })
        {
            var rect = new SKRect(x - (bw / 2), rimY - rimRy, x + (bw / 2), rimY + rimRy);
            c.DrawArc(rect, 0, 180, false, rim);
            for (var a = 4f; a < 178; a += 9)
            {
                var rad = a * MathF.PI / 180;
                var px = x + (MathF.Cos(rad) * bw / 2);
                var py = rimY + (MathF.Sin(rad) * rimRy);
                c.DrawLine(px - (bw * 0.012f), py - (bw * 0.016f), px + (bw * 0.012f), py + (bw * 0.016f), plait);
            }
        }

        SpringBow(c, x + (bw * 0.3f), rimY + (rimRy * 0.85f), bw * 0.09f, new SKColor(0xF0, 0x6A, 0x9A));
    }

    /// <summary>A satin ribbon bow with two tails.</summary>
    private static void SpringBow(SKCanvas c, float x, float y, float size, SKColor colour)
    {
        using var satin = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(x - size, y - size), new SKPoint(x + size, y + size), new[] { Lighten(colour, 0.35f), colour, Darken(colour, 0.7f) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp) };
        foreach (var side in new[] { -1f, 1f })
        {
            using var tail = new SKPath();
            tail.MoveTo(x, y);
            tail.LineTo(x + (side * size * 0.5f), y + (size * 1.1f));
            tail.LineTo(x + (side * size * 0.25f), y + (size * 1.0f));
            tail.LineTo(x + (side * size * 0.1f), y + (size * 1.15f));
            tail.Close();
            c.DrawPath(tail, satin);
            using var loop = new SKPath();
            loop.MoveTo(x, y);
            loop.CubicTo(x + (side * size * 0.6f), y - (size * 0.8f), x + (side * size * 1.2f), y - (size * 0.3f), x + (side * size * 1.0f), y + (size * 0.15f));
            loop.CubicTo(x + (side * size * 0.8f), y + (size * 0.5f), x + (side * size * 0.3f), y + (size * 0.2f), x, y);
            loop.Close();
            c.DrawPath(loop, satin);
        }

        using var knot = new SKPaint { IsAntialias = true, Color = Darken(colour, 0.8f) };
        c.DrawOval(new SKRect(x - (size * 0.2f), y - (size * 0.22f), x + (size * 0.2f), y + (size * 0.22f)), knot);
    }

    private static void SpringButterfly(SKCanvas c, float x, float y, float r, float rotation, SKColor colour)
    {
        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(rotation);
        var edge = Darken(colour, 0.35f);
        foreach (var side in new[] { -1f, 1f })
        {
            using var upper = new SKPath();
            upper.MoveTo(0, -r * 0.1f);
            upper.CubicTo(side * r * 0.4f, -r * 1.1f, side * r * 1.3f, -r * 1.0f, side * r * 1.1f, -r * 0.3f);
            upper.CubicTo(side * r * 1.0f, -r * 0.05f, side * r * 0.4f, 0, 0, r * 0.05f);
            upper.Close();
            using var lower = new SKPath();
            lower.MoveTo(0, 0);
            lower.CubicTo(side * r * 0.7f, r * 0.05f, side * r * 0.9f, r * 0.6f, side * r * 0.5f, r * 0.75f);
            lower.CubicTo(side * r * 0.25f, r * 0.85f, side * r * 0.1f, r * 0.4f, 0, r * 0.2f);
            lower.Close();
            foreach (var wing in new[] { upper, lower })
            {
                using var fill = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(0, 0), r * 1.2f, new[] { Lighten(colour, 0.3f), colour, edge }, new[] { 0f, 0.65f, 1f }, SKShaderTileMode.Clamp) };
                c.DrawPath(wing, fill);
            }

            using var spot = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(220) };
            c.DrawCircle(side * r * 0.95f, -r * 0.55f, r * 0.08f, spot);
            c.DrawCircle(side * r * 0.75f, -r * 0.75f, r * 0.06f, spot);
        }

        using var body = new SKPaint { IsAntialias = true, Color = new SKColor(0x2E, 0x22, 0x1E) };
        c.DrawRoundRect(new SKRect(-r * 0.07f, -r * 0.6f, r * 0.07f, r * 0.55f), r * 0.07f, r * 0.07f, body);
        using var antenna = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = r * 0.04f, Color = body.Color };
        c.DrawLine(0, -r * 0.55f, -r * 0.3f, -r * 1.0f, antenna);
        c.DrawLine(0, -r * 0.55f, r * 0.3f, -r * 1.0f, antenna);
        c.Restore();
    }

    // ---------- Valentine's ----------

    private static readonly SKColor[] SpringLoveColours =
    {
        new(0xE8, 0x1E, 0x4A), new(0xFF, 0x6E, 0x9A), new(0xC2, 0x10, 0x3C), new(0xFF, 0xA8, 0xC4), new(0xF4, 0xB4, 0x98), new(0xFF, 0xE4, 0xEC)
    };

    /// <summary>A romantic evening backdrop: deep plum above, a rosy glow rising from the bottom.</summary>
    private static void SpringValentineSky(SKCanvas c, int w, int h, float s)
    {
        SpringSky(c, w, h, new[] { new SKColor(0x28, 0x04, 0x20, 190), new SKColor(0x60, 0x10, 0x38, 100), new SKColor(0x80, 0x18, 0x44, 100), new SKColor(0xC8, 0x40, 0x68, 170) }, new[] { 0f, 0.35f, 0.65f, 1f });
        SpringGlow(c, w / 2f, h * 1.05f, s * 0.8f, new SKColor(0xFF, 0x9A, 0xB8, 140));
    }

    /// <summary>
    /// A glossy heart: rich gradient, soft drop shadow, a rim of reflected light and a bright shine. With
    /// <paramref name="blur"/> it becomes an out-of-focus heart for the background.
    /// </summary>
    private static void SpringGlossyHeart(SKCanvas c, float x, float y, float size, SKColor colour, float rotation, float blur = 0, byte alpha = 255)
    {
        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(rotation);
        using var heart = SpringHeartPath(size);
        if (blur > 0)
        {
            using var soft = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, blur);
            using var p = new SKPaint { IsAntialias = true, MaskFilter = soft, Color = SKColors.White.WithAlpha(alpha), Shader = SKShader.CreateRadialGradient(new SKPoint(-size * 0.2f, -size * 0.25f), size, new[] { Lighten(colour, 0.35f), colour }, null, SKShaderTileMode.Clamp) };
            c.DrawPath(heart, p);
            c.Restore();
            return;
        }

        using (var shadowBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, size * 0.06f))
        using (var shadow = new SKPaint { IsAntialias = true, Color = SKColors.Black.WithAlpha(110), MaskFilter = shadowBlur })
        {
            c.Save();
            c.Translate(size * 0.03f, size * 0.07f);
            c.DrawPath(heart, shadow);
            c.Restore();
        }

        using (var fill = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(alpha), Shader = SKShader.CreateRadialGradient(new SKPoint(-size * 0.22f, -size * 0.28f), size * 1.05f, new[] { Lighten(colour, 0.4f), colour, Darken(colour, 0.5f) }, new[] { 0f, 0.45f, 1f }, SKShaderTileMode.Clamp) })
        {
            c.DrawPath(heart, fill);
        }

        c.Save();
        c.ClipPath(heart, antialias: true);
        using (var rimBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, size * 0.03f))
        using (var rim = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = size * 0.06f, Color = Lighten(colour, 0.55f).WithAlpha(150), MaskFilter = rimBlur })
        {
            c.Translate(-size * 0.04f, -size * 0.05f);
            c.DrawPath(heart, rim);
        }

        c.Restore();

        using (var glossBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, size * 0.025f))
        using (var gloss = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(200), MaskFilter = glossBlur })
        {
            c.Save();
            c.RotateDegrees(-38, -size * 0.32f, -size * 0.3f);
            c.DrawOval(new SKRect(-size * 0.46f, -size * 0.38f, -size * 0.2f, -size * 0.25f), gloss);
            c.Restore();
        }

        using (var dot = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(220) })
        {
            c.DrawCircle(size * 0.32f, -size * 0.3f, size * 0.035f, dot);
        }

        c.Restore();
    }

    /// <summary>Out-of-focus hearts scattered off the title, for depth behind the main ones.</summary>
    private static void SpringHeartBokeh(SKCanvas c, int w, int h, float s, Random rng, int count, float minSize, float maxSize, float blurFactor, byte minAlpha, byte maxAlpha)
    {
        for (var i = 0; i < count; i++)
        {
            var size = s * R(rng, minSize, maxSize);
            float x = R(rng, 0, w), y = R(rng, 0, h);
            var colour = SpringLoveColours[rng.Next(SpringLoveColours.Length)];
            var rot = R(rng, -25, 25);
            var alpha = (byte)rng.Next(minAlpha, maxAlpha);
            if (SpringOffTitle(x, y, size * 0.6f, w, h))
            {
                SpringGlossyHeart(c, x, y, size, colour, rot, size * blurFactor, alpha);
            }
        }
    }

    // ---------- Valentine's, week 1: glossy hearts floating with depth of field ----------

    private static void Hearts(SKCanvas c, int w, int h, SKColor tint, Random rng)
    {
        float s = Math.Min(w, h);
        SpringValentineSky(c, w, h, s);
        SpringHeartBokeh(c, w, h, s, rng, w > h ? 30 : 24, 0.03f, 0.07f, 0.22f, 60, 120);

        // The big, sharp hearts frame the corners, cut by the edges; the middle layer drifts between them.
        var near = new[]
        {
            (X: w * 0.06f, Y: h * 0.9f, Size: s * 0.24f, Rot: -18f, Colour: SpringLoveColours[0]),
            (X: w * 0.94f, Y: h * 0.87f, Size: s * 0.22f, Rot: 16f, Colour: SpringLoveColours[1]),
            (X: w * 0.22f, Y: h * 0.98f, Size: s * 0.13f, Rot: 12f, Colour: SpringLoveColours[3]),
            (X: w * 0.77f, Y: h * 0.99f, Size: s * 0.12f, Rot: -10f, Colour: SpringLoveColours[2]),
            (X: w * 0.93f, Y: h * 0.09f, Size: s * 0.14f, Rot: 20f, Colour: SpringLoveColours[2]),
            (X: w * 0.07f, Y: h * 0.1f, Size: s * 0.11f, Rot: -14f, Colour: SpringLoveColours[4])
        };
        var placed = near.Select(n => (n.X, n.Y, n.Size * 0.62f)).ToList();
        var midColours = new[] { SpringLoveColours[0], SpringLoveColours[1], SpringLoveColours[3], SpringLoveColours[5], Lighten(tint, 0.25f) };
        for (var i = 0; i < (w > h ? 12 : 9); i++)
        {
            var size = s * R(rng, 0.05f, 0.085f);
            for (var attempt = 0; attempt < 20; attempt++)
            {
                if (FindClear(rng, placed, size * 0.62f, 0, w, 0, h, 1.15f) is not { } at)
                {
                    break;
                }

                if (!SpringOffTitle(at.X, at.Y, size * 0.6f, w, h))
                {
                    continue;
                }

                SpringGlossyHeart(c, at.X, at.Y, size, midColours[rng.Next(midColours.Length)], R(rng, -25, 25), size * 0.035f, 235);
                placed.Add((at.X, at.Y, size * 0.62f));
                break;
            }
        }

        foreach (var n in near)
        {
            SpringGlossyHeart(c, n.X, n.Y, n.Size, n.Colour, n.Rot);
        }

        for (var i = 0; i < 14; i++)
        {
            float x = R(rng, 0, w), y = R(rng, 0, h);
            if (SpringOffTitle(x, y, s * 0.02f, w, h))
            {
                SpringGlint(c, x, y, s * R(rng, 0.008f, 0.018f), new SKColor(0xFF, 0xD8, 0xE4));
            }
        }
    }

    // ---------- Valentine's, week 2: a garland of hearts above, a bed of roses below ----------

    private static void HeartsAndRoses(SKCanvas c, int w, int h, SKColor tint, Random rng)
    {
        float s = Math.Min(w, h);
        SpringValentineSky(c, w, h, s);
        SpringHeartBokeh(c, w, h, s, rng, w > h ? 18 : 14, 0.03f, 0.06f, 0.22f, 50, 100);

        // Hearts hanging on fine gold threads from the top edge, at alternating lengths.
        var count = w > h ? 9 : 6;
        using (var thread = new SKPaint { IsAntialias = true, StrokeWidth = s * 0.0025f, Color = new SKColor(0xF8, 0xD8, 0x98, 200) })
        {
            for (var i = 0; i < count; i++)
            {
                var x = (w * (i + 0.5f) / count) + R(rng, -s * 0.015f, s * 0.015f);
                var size = s * R(rng, 0.05f, 0.065f);
                var len = h * (i % 2 == 0 ? R(rng, 0.07f, 0.12f) : R(rng, 0.15f, 0.2f));
                c.DrawLine(x, 0, x, len, thread);
                var colour = i % 3 == 2 ? Lighten(tint, 0.3f) : SpringLoveColours[(i * 2) % 4];
                SpringGlossyHeart(c, x, len + (size * 0.4f), size, colour, R(rng, -8, 8));
            }
        }

        // Rose petals scattered on the ground between the bouquets, and a few drifting down.
        var roseColours = new[] { new SKColor(0xC0, 0x10, 0x2C), new SKColor(0xE0, 0x24, 0x48), new SKColor(0xD8, 0x1A, 0x3A), new SKColor(0xF0, 0x6C, 0x94) };
        for (var i = 0; i < (w > h ? 22 : 14); i++)
        {
            SpringPetal(c, R(rng, w * 0.2f, w * 0.8f), h - (s * R(rng, 0.0f, 0.05f)), s * R(rng, 0.022f, 0.032f), R(rng, 0, 360), roseColours[rng.Next(roseColours.Length)], 240);
        }

        for (var i = 0; i < 12; i++)
        {
            float x = R(rng, 0, w), y = R(rng, h * 0.3f, h * 0.85f), size = s * R(rng, 0.018f, 0.028f);
            if (SpringOffTitle(x, y, size, w, h))
            {
                SpringPetal(c, x, y, size, R(rng, 0, 360), roseColours[rng.Next(roseColours.Length)], 220);
            }
        }

        // A bouquet in each bottom corner: leaves fanned out behind, roses back to front, baby's breath between.
        foreach (var mirror in new[] { false, true })
        {
            c.Save();
            if (mirror)
            {
                c.Translate(w, 0);
                c.Scale(-1, 1);
            }

            foreach (var (lx, ly, angle) in new[] { (0.1f, 0.22f, -80f), (0.2f, 0.16f, -40f), (0.28f, 0.1f, -15f), (0.04f, 0.24f, -110f), (0.3f, 0.03f, 5f) })
            {
                SpringRoseLeaf(c, s * lx, h - (s * ly), s * 0.15f, angle);
            }

            var bunch = new[] { (0.07f, 0.22f, 0.075f), (0.2f, 0.15f, 0.08f), (0.32f, 0.06f, 0.065f), (0.04f, 0.07f, 0.1f), (0.17f, 0.02f, 0.085f) };
            for (var i = 0; i < bunch.Length; i++)
            {
                var (rx, up, rr) = bunch[i];
                SpringRose(c, s * rx, h - (s * up), s * rr, roseColours[(i + (mirror ? 2 : 0)) % roseColours.Length], rng);
            }

            using (var breath = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(230) })
            {
                foreach (var (bx, by) in new[] { (0.14f, 0.27f), (0.27f, 0.2f), (0.36f, 0.13f), (0.02f, 0.3f) })
                {
                    for (var k = 0; k < 6; k++)
                    {
                        c.DrawCircle(s * (bx + R(rng, -0.02f, 0.02f)), h - (s * (by + R(rng, -0.02f, 0.02f))), s * R(rng, 0.004f, 0.007f), breath);
                    }
                }
            }

            c.Restore();
        }
    }

    /// <summary>
    /// A rose seen from above: rings of cupped petals, each paler at its curled rim and shadowed where it tucks under
    /// the ring above, turning in to a tight spiral at the heart.
    /// </summary>
    private static void SpringRose(SKCanvas c, float x, float y, float r, SKColor colour, Random rng)
    {
        SpringContactShadow(c, x + (r * 0.06f), y + (r * 0.12f), r * 1.05f, r * 1.0f, 120);
        var turn = R(rng, 0, 360);
        var rings = new[] { (Count: 5, Dist: 0.5f, Size: 0.55f, Light: 0f), (Count: 5, Dist: 0.33f, Size: 0.45f, Light: 0.08f), (Count: 4, Dist: 0.18f, Size: 0.34f, Light: 0.15f) };
        using var shadowBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.06f);
        using var under = new SKPaint { IsAntialias = true, Color = Darken(colour, 0.25f).WithAlpha(200), MaskFilter = shadowBlur };
        using var lip = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round };
        for (var ring = 0; ring < rings.Length; ring++)
        {
            var (count, dist, size, light) = rings[ring];
            var tone = Lighten(colour, light);
            if (ring > 0)
            {
                c.DrawCircle(x, y + (r * 0.03f), r * (dist + (size * 0.75f)), under);
            }

            for (var i = 0; i < count; i++)
            {
                var a = (turn + (ring * 36f) + (i * 360f / count)) * MathF.PI / 180;
                var px = x + (MathF.Cos(a) * r * dist);
                var py = y + (MathF.Sin(a) * r * dist);
                var pr = r * size;
                var outer = new SKPoint(px + (MathF.Cos(a) * pr * 0.7f), py + (MathF.Sin(a) * pr * 0.7f));
                using var petal = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(outer, pr * 1.6f, new[] { Lighten(tone, 0.35f), tone, Darken(tone, 0.4f) }, new[] { 0f, 0.3f, 1f }, SKShaderTileMode.Clamp) };
                c.DrawCircle(px, py, pr, petal);
                lip.StrokeWidth = pr * 0.1f;
                lip.Color = Lighten(tone, 0.4f).WithAlpha(170);
                var deg = (a * 180 / MathF.PI) - 50;
                c.DrawArc(new SKRect(px - (pr * 0.92f), py - (pr * 0.92f), px + (pr * 0.92f), py + (pr * 0.92f)), deg, 100, false, lip);
            }
        }

        // The spiral heart.
        using var core = new SKPaint { IsAntialias = true, Color = Darken(colour, 0.45f) };
        c.DrawCircle(x, y, r * 0.17f, core);
        using var curl = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = r * 0.05f, Color = Lighten(colour, 0.25f) };
        foreach (var (rad, start) in new[] { (0.14f, 0f), (0.095f, 140f), (0.05f, 260f) })
        {
            c.DrawArc(new SKRect(x - (r * rad), y - (r * rad), x + (r * rad), y + (r * rad)), turn + start, 230, false, curl);
        }
    }

    private static void SpringRoseLeaf(SKCanvas c, float x, float y, float len, float angle)
    {
        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(angle);
        using var leaf = new SKPath();
        leaf.MoveTo(0, 0);
        leaf.CubicTo(len * 0.3f, -len * 0.3f, len * 0.75f, -len * 0.25f, len, 0);
        leaf.CubicTo(len * 0.75f, len * 0.25f, len * 0.3f, len * 0.3f, 0, 0);
        leaf.Close();
        using var fill = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, -len * 0.25f), new SKPoint(0, len * 0.25f), new[] { new SKColor(0x5A, 0xA8, 0x50), new SKColor(0x1A, 0x50, 0x22) }, null, SKShaderTileMode.Clamp) };
        c.DrawPath(leaf, fill);
        using var vein = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = len * 0.02f, Color = new SKColor(0xB8, 0xE0, 0xA8, 150) };
        c.DrawLine(len * 0.02f, 0, len * 0.92f, 0, vein);
        for (var t = 0.25f; t < 0.85f; t += 0.18f)
        {
            c.DrawLine(len * t, 0, len * (t + 0.1f), -len * 0.12f, vein);
            c.DrawLine(len * t, 0, len * (t + 0.1f), len * 0.12f, vein);
        }

        c.Restore();
    }

    // ---------- Valentine's, week 3: heart balloons over a sunset sea of clouds ----------

    private static void HeartBalloons(SKCanvas c, int w, int h, SKColor tint, Random rng)
    {
        float s = Math.Min(w, h);
        SpringSky(c, w, h, new[] { new SKColor(0x30, 0x22, 0x60, 195), new SKColor(0x88, 0x40, 0x88, 115), new SKColor(0xE0, 0x78, 0x98, 125), new SKColor(0xFF, 0xB8, 0x98, 200) }, new[] { 0f, 0.38f, 0.65f, 0.85f });
        SpringGlow(c, w * 0.5f, h * 0.84f, s * 0.7f, new SKColor(0xFF, 0xD0, 0xA8, 130));

        // Far-off balloons drifting up through the middle of the sky.
        // Each trails a short, gently waving string from its knot (straight sticks looked like lollipops).
        using (var str = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.0015f, Color = SKColors.White.WithAlpha(80) })
        {
            for (var i = 0; i < (w > h ? 9 : 5); i++)
            {
                var size = s * R(rng, 0.018f, 0.032f);
                float x = R(rng, w * 0.3f, w * 0.7f), y = R(rng, h * 0.05f, h * 0.27f);
                var tilt = R(rng, -12, 12);
                var from = SpringBalloonKnot(new SKPoint(x, y), size, tilt);
                var sway = size * R(rng, 0.3f, 0.6f) * (rng.Next(2) == 0 ? -1 : 1);
                using var tail = new SKPath();
                tail.MoveTo(from);
                tail.CubicTo(from.X + sway, from.Y + (size * 0.6f), from.X - sway, from.Y + (size * 1.2f), from.X + (sway * 0.5f), from.Y + (size * 1.9f));
                c.DrawPath(tail, str);
                SpringHeartBalloon(c, x, y, size, SpringLoveColours[rng.Next(SpringLoveColours.Length)], tilt, (byte)rng.Next(110, 170), size * 0.08f);
            }
        }

        // A sea of clouds below, lit pink and gold from the sunset.
        SpringCloudBank(c, w, h, s, h * 0.8f, rng, new SKColor(0xF8, 0xC0, 0xC8, 210), new SKColor(0x9A, 0x70, 0xA8, 220), new SKColor(0xFF, 0xE8, 0xC8, 200));
        SpringCloudBank(c, w, h, s, h * 0.89f, rng, new SKColor(0xFF, 0xD8, 0xD8, 240), new SKColor(0xB0, 0x88, 0xB8, 240), new SKColor(0xFF, 0xF4, 0xE0, 220));

        // A bunch in each top corner, strings gathered to a bow above the title, ribbons curling below it.
        var colours = new[] { SpringLoveColours[0], SpringLoveColours[1], SpringLoveColours[2], SpringLoveColours[5], Lighten(tint, 0.2f), SpringLoveColours[4] };
        foreach (var right in new[] { false, true })
        {
            SKPoint Place(float fx, float fy) => new(right ? w - (s * fx) : s * fx, s * fy);
            // A tight bunch well above the bow, each balloon leaning out from it, so the strings fall steeply and
            // gather at the bow instead of fanning out sideways.
            var knot = Place(0.19f, 0.33f);
            var spec = new[] { (0.11f, 0.07f, 0.062f), (0.25f, 0.055f, 0.058f), (0.28f, 0.16f, 0.048f), (0.185f, 0.14f, 0.064f), (0.09f, 0.18f, 0.048f) };
            var spots = spec.Select((b, i) =>
            {
                var at = Place(b.Item1, b.Item2);
                var lean = Math.Clamp((at.X - knot.X) / (s * 0.1f), -1f, 1f) * 14f;
                return (At: at, Size: s * b.Item3, Tilt: lean + R(rng, -4, 4), Colour: colours[(i + (right ? 3 : 0)) % colours.Length]);
            }).ToArray();

            using (var str = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.0018f, Color = SKColors.White.WithAlpha(170) })
            {
                foreach (var spot in spots)
                {
                    // Hangs down from the balloon first, then curves in to the bow.
                    var from = SpringBalloonKnot(spot.At, spot.Size, spot.Tilt);
                    using var line = new SKPath();
                    line.MoveTo(from);
                    line.CubicTo(from.X, from.Y + ((knot.Y - from.Y) * 0.45f), knot.X + ((from.X - knot.X) * 0.25f), knot.Y - ((knot.Y - from.Y) * 0.3f), knot.X, knot.Y);
                    c.DrawPath(line, str);
                }
            }

            foreach (var spot in spots.OrderBy(p => p.At.Y))
            {
                SpringHeartBalloon(c, spot.At.X, spot.At.Y, spot.Size, spot.Colour, spot.Tilt);
            }

            using (var ribbon = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.006f, StrokeCap = SKStrokeCap.Round, Color = new SKColor(0xF8, 0xD8, 0x98) })
            {
                foreach (var side in new[] { -1f, 1f })
                {
                    using var curl = new SKPath();
                    curl.MoveTo(knot);
                    curl.CubicTo(knot.X + (side * s * 0.03f), knot.Y + (s * 0.015f), knot.X - (side * s * 0.015f), knot.Y + (s * 0.035f), knot.X + (side * s * 0.02f), knot.Y + (s * 0.06f));
                    c.DrawPath(curl, ribbon);
                }
            }

            SpringBow(c, knot.X, knot.Y, s * 0.022f, new SKColor(0xF0, 0xC8, 0x80));
        }

        for (var i = 0; i < 12; i++)
        {
            float x = R(rng, 0, w), y = R(rng, 0, h * 0.8f);
            if (SpringOffTitle(x, y, s * 0.02f, w, h))
            {
                SpringGlint(c, x, y, s * R(rng, 0.007f, 0.016f), new SKColor(0xFF, 0xE8, 0xD0));
            }
        }
    }

    /// <summary>A bank of soft cumulus clouds along the bottom, lit from above, with bright edges.</summary>
    private static void SpringCloudBank(SKCanvas c, int w, int h, float s, float top, Random rng, SKColor lit, SKColor shade, SKColor rim)
    {
        // Big billows with smaller ones on their shoulders, so the top edge isn't a row of equal scallops.
        var circles = new List<(float X, float Y, float R)>();
        for (var x = -s * 0.1f; x < w + (s * 0.15f); x += s * R(rng, 0.1f, 0.18f))
        {
            var r = s * R(rng, 0.07f, 0.13f);
            var y = top + (r * R(rng, 0.25f, 0.6f));
            circles.Add((x, y, r));
            circles.Add((x + (r * R(rng, 0.6f, 0.9f)), y + (r * R(rng, 0.15f, 0.4f)), r * R(rng, 0.45f, 0.65f)));
        }

        using var puffs = new SKPath();
        foreach (var (x, y, r) in circles)
        {
            puffs.AddCircle(x, y, r);
        }

        puffs.AddRect(new SKRect(-20, top + (s * 0.08f), w + 20, h + 20));
        using var bank = new SKPath();
        puffs.Simplify(bank);
        using var soft = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.004f);
        using (var fill = new SKPaint { IsAntialias = true, MaskFilter = soft, Shader = SKShader.CreateLinearGradient(new SKPoint(0, top - (s * 0.1f)), new SKPoint(0, h), new[] { lit, shade }, null, SKShaderTileMode.Clamp) })
        {
            c.DrawPath(bank, fill);
        }

        // Each billow lit on its upper side and shaded underneath, then a bright rim along the top.
        c.Save();
        c.ClipPath(bank, antialias: true);
        using (var billowShade = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.02f))
        using (var under = new SKPaint { IsAntialias = true, Color = Darken(shade, 0.8f).WithAlpha(90), MaskFilter = billowShade })
        using (var glow = new SKPaint { IsAntialias = true, MaskFilter = billowShade })
        {
            foreach (var (x, y, r) in circles)
            {
                c.DrawCircle(x + (r * 0.1f), y + (r * 0.75f), r * 0.6f, under);
            }

            foreach (var (x, y, r) in circles)
            {
                glow.Color = rim.WithAlpha(110);
                c.DrawCircle(x - (r * 0.15f), y - (r * 0.35f), r * 0.55f, glow);
            }
        }

        using var rimBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.006f);
        using var edge = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.012f, Color = rim, MaskFilter = rimBlur };
        c.DrawPath(bank, edge);
        c.Restore();
    }

    /// <summary>Where a heart balloon's string starts: under the knot at its point, turned with the balloon.</summary>
    private static SKPoint SpringBalloonKnot(SKPoint at, float size, float rotation)
    {
        var below = size * 0.78f;
        var a = rotation * MathF.PI / 180;
        return new SKPoint(at.X - (MathF.Sin(a) * below), at.Y + (MathF.Cos(a) * below));
    }

    /// <summary>A shiny heart-shaped foil balloon centred on (x, y), its point down with a knot.</summary>
    private static void SpringHeartBalloon(SKCanvas c, float x, float y, float size, SKColor colour, float rotation, byte alpha = 255, float blur = 0)
    {
        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(rotation);
        var hs = size * 1.3f;
        using var heart = SpringHeartPath(hs);
        using var soft = blur > 0 ? SKMaskFilter.CreateBlur(SKBlurStyle.Normal, blur) : null;
        if (blur <= 0)
        {
            using var shadowBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, size * 0.08f);
            using var shadow = new SKPaint { IsAntialias = true, Color = SKColors.Black.WithAlpha(90), MaskFilter = shadowBlur };
            c.Save();
            c.Translate(size * 0.05f, size * 0.1f);
            c.DrawPath(heart, shadow);
            c.Restore();
        }

        using (var fill = new SKPaint { IsAntialias = true, MaskFilter = soft, Color = SKColors.White.WithAlpha(alpha), Shader = SKShader.CreateRadialGradient(new SKPoint(-hs * 0.25f, -hs * 0.3f), hs * 1.1f, new[] { Lighten(colour, 0.6f), colour, Darken(colour, 0.55f) }, new[] { 0f, 0.42f, 1f }, SKShaderTileMode.Clamp) })
        {
            c.DrawPath(heart, fill);
        }

        using var knot = new SKPath();
        knot.MoveTo(0, hs * 0.48f);
        knot.LineTo(-size * 0.07f, (hs * 0.5f) + (size * 0.13f));
        knot.LineTo(size * 0.07f, (hs * 0.5f) + (size * 0.13f));
        knot.Close();
        using (var knotPaint = new SKPaint { IsAntialias = true, Color = Darken(colour, 0.7f).WithAlpha(alpha), MaskFilter = soft })
        {
            c.DrawPath(knot, knotPaint);
        }

        if (blur <= 0)
        {
            // Foil: a crease of reflected light down the right lobe, a long streak on the left and a bright glint.
            c.Save();
            c.ClipPath(heart, antialias: true);
            using (var rimBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, size * 0.05f))
            using (var rim = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = size * 0.1f, Color = Lighten(colour, 0.5f).WithAlpha(140), MaskFilter = rimBlur })
            {
                c.Translate(-size * 0.06f, -size * 0.06f);
                c.DrawPath(heart, rim);
            }

            c.Restore();
            using (var streakBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, size * 0.03f))
            using (var streak = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(200), MaskFilter = streakBlur })
            {
                c.Save();
                c.RotateDegrees(-40, -hs * 0.3f, -hs * 0.3f);
                c.DrawOval(new SKRect(-hs * 0.5f, -hs * 0.38f, -hs * 0.14f, -hs * 0.24f), streak);
                c.Restore();
            }

            using var dot = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(230) };
            c.DrawCircle(hs * 0.3f, -hs * 0.3f, size * 0.05f, dot);
        }

        c.Restore();
    }
}
