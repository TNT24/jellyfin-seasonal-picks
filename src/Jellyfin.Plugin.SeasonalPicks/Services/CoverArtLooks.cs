using System;
using SkiaSharp;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>
/// More art styles, so a season's covers differ in look as well as subject: a horror-film Halloween, neon signs on a
/// brick wall (New Year, Valentine's) and a layered paper cut-out Christmas. The title band across the middle is
/// left clear.
/// </summary>
internal static partial class CoverArt
{
    // ====================================================================================================
    // Horror: a hooded figure under a blood moon, a clawing dead tree, crows, film scratches
    // ====================================================================================================

    private static void HorrorHalloween(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var wide = w > h;

        // Black at the top bleeding to deep red at the horizon.
        using (var sky = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, h), new[] { new SKColor(0x06, 0x02, 0x04, 245), new SKColor(0x2A, 0x04, 0x08, 230), new SKColor(0x7A, 0x0C, 0x10, 235), new SKColor(0x1A, 0x02, 0x04, 250) }, new[] { 0f, 0.45f, 0.78f, 1f }, SKShaderTileMode.Clamp)
        })
        {
            c.DrawRect(0, 0, w, h, sky);
        }

        // A huge blood moon low behind the figure, hazed.
        var moon = new SKPoint(w * (wide ? 0.66f : 0.64f), h * 0.84f);
        var mr = s * 0.18f;
        using (var haze = new SKPaint { IsAntialias = true, Color = new SKColor(0xD0, 0x20, 0x18, 90), MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.06f) })
        {
            c.DrawCircle(moon, mr * 1.3f, haze);
        }

        using (var disc = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(moon.X - (mr * 0.2f), moon.Y - (mr * 0.3f)), mr, new[] { new SKColor(0xF2, 0x6A, 0x4A), new SKColor(0xB8, 0x1E, 0x16), new SKColor(0x7A, 0x0C, 0x0C) }, new[] { 0f, 0.6f, 1f }, SKShaderTileMode.Clamp) })
        {
            c.DrawCircle(moon, mr, disc);
        }

        // The ground: a black rise the figure stands on.
        var ground = h * 0.9f;
        using (var hill = new SKPath())
        {
            hill.MoveTo(-2, h + 2);
            hill.LineTo(-2, ground + (s * 0.02f));
            hill.QuadTo(moon.X, ground - (s * 0.06f), w + 2, ground + (s * 0.03f));
            hill.LineTo(w + 2, h + 2);
            hill.Close();
            using var black = new SKPaint { IsAntialias = true, Color = new SKColor(0x04, 0x01, 0x02) };
            c.DrawPath(hill, black);
        }

        // The figure: a hunched, hooded shape in a ragged robe, the hood peaked and drooping forward, long bony
        // fingers hanging from one sleeve, and two red eyes in the dark of the hood.
        var fx = moon.X;
        var feet = ground - (s * 0.03f);
        var fh = s * 0.24f;
        var ink = new SKColor(0x03, 0x01, 0x01);
        using (var robe = new SKPaint { IsAntialias = true, Color = ink })
        using (var body = new SKPath())
        {
            body.MoveTo(fx - (fh * 0.3f), feet);
            body.CubicTo(fx - (fh * 0.24f), feet - (fh * 0.45f), fx - (fh * 0.22f), feet - (fh * 0.7f), fx - (fh * 0.12f), feet - (fh * 0.86f));
            body.QuadTo(fx - (fh * 0.06f), feet - (fh * 1.02f), fx + (fh * 0.06f), feet - (fh * 1.0f));
            body.QuadTo(fx + (fh * 0.14f), feet - (fh * 0.98f), fx + (fh * 0.15f), feet - (fh * 0.84f));
            body.CubicTo(fx + (fh * 0.22f), feet - (fh * 0.66f), fx + (fh * 0.26f), feet - (fh * 0.4f), fx + (fh * 0.34f), feet);

            // A torn hem: jagged points along the ground.
            var hem = 9;
            for (var i = 1; i <= hem; i++)
            {
                var t = i / (float)hem;
                var x = fx + (fh * 0.34f) - (t * fh * 0.64f);
                body.LineTo(x + (fh * 0.035f), feet - (fh * (i % 2 == 0 ? 0.0f : 0.06f)));
                body.LineTo(x, feet + (fh * (i % 2 == 0 ? 0.03f : 0.0f)));
            }

            body.Close();
            c.DrawPath(body, robe);

            using var arm = new SKPaint { IsAntialias = true, StrokeWidth = fh * 0.07f, StrokeCap = SKStrokeCap.Round, Color = ink };
            c.DrawLine(fx + (fh * 0.12f), feet - (fh * 0.7f), fx + (fh * 0.27f), feet - (fh * 0.38f), arm);
            using var fingers = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = fh * 0.014f, StrokeCap = SKStrokeCap.Round, Color = ink };
            for (var i = -1; i <= 2; i++)
            {
                using var finger = new SKPath();
                finger.MoveTo(fx + (fh * 0.27f), feet - (fh * 0.38f));
                finger.QuadTo(fx + (fh * (0.3f + (i * 0.018f))), feet - (fh * 0.3f), fx + (fh * (0.29f + (i * 0.025f))), feet - (fh * 0.2f));
                c.DrawPath(finger, fingers);
            }
        }

        using (var glow = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0x20, 0x10, 140), MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.006f) })
        using (var eye = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0x50, 0x30) })
        {
            var ey = feet - (fh * 0.82f);
            foreach (var ex in new[] { fx - (fh * 0.05f), fx + (fh * 0.04f) })
            {
                c.DrawCircle(ex, ey, fh * 0.03f, glow);
                c.DrawOval(new SKRect(ex - (fh * 0.018f), ey - (fh * 0.007f), ex + (fh * 0.018f), ey + (fh * 0.007f)), eye);
            }
        }

        // Blood running down from the top edge in uneven drips.
        using (var blood = new SKPaint { IsAntialias = true, Color = new SKColor(0x8A, 0x04, 0x08) })
        using (var drips = new SKPath())
        {
            drips.MoveTo(-2, -2);
            drips.LineTo(w + 2, -2);
            drips.LineTo(w + 2, s * 0.02f);
            var x = w + 2f;
            while (x > -2)
            {
                var gap = s * R(rng, 0.03f, 0.08f);
                var len = rng.Next(3) == 0 ? s * R(rng, 0.06f, 0.16f) : s * R(rng, 0.01f, 0.035f);
                var dw = s * R(rng, 0.008f, 0.014f);
                drips.LineTo(x - (gap * 0.4f), s * 0.02f);
                drips.LineTo(x - (gap * 0.4f), len);
                drips.ArcTo(new SKRect(x - (gap * 0.4f) - (dw * 2), len - dw, x - (gap * 0.4f), len + dw), 0, 180, false);
                drips.LineTo(x - (gap * 0.4f) - (dw * 2), s * 0.02f);
                x -= gap;
            }

            drips.LineTo(-2, s * 0.02f);
            drips.Close();
            c.DrawPath(drips, blood);
        }

        // A dead tree clawing in from the left, its branches forking into thin twigs.
        using (var bark = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, Color = new SKColor(0x04, 0x01, 0x02) })
        {
            void Branch(SKPoint from, float angle, float len, float width, int depth)
            {
                var to = new SKPoint(from.X + (MathF.Cos(angle) * len), from.Y + (MathF.Sin(angle) * len));
                bark.StrokeWidth = width;
                using (var bend = new SKPath())
                {
                    bend.MoveTo(from);
                    bend.QuadTo(((from.X + to.X) / 2) + (R(rng, -0.15f, 0.15f) * len), ((from.Y + to.Y) / 2) + (R(rng, -0.15f, 0.15f) * len), to.X, to.Y);
                    c.DrawPath(bend, bark);
                }

                if (depth == 0)
                {
                    return;
                }

                Branch(to, angle - R(rng, 0.25f, 0.6f), len * R(rng, 0.6f, 0.75f), width * 0.62f, depth - 1);
                Branch(to, angle + R(rng, 0.2f, 0.55f), len * R(rng, 0.55f, 0.72f), width * 0.62f, depth - 1);
            }

            var root = new SKPoint(w * (wide ? 0.1f : 0.12f), h + (s * 0.01f));
            Branch(root, -MathF.PI / 2 + 0.12f, s * 0.3f, s * 0.045f, 0);
            var fork = new SKPoint(root.X + (s * 0.035f), root.Y - (s * 0.3f));
            Branch(fork, -MathF.PI / 2 - 0.55f, s * 0.16f, s * 0.026f, 4);
            Branch(fork, -MathF.PI / 2 + 0.35f, s * 0.2f, s * 0.03f, 4);
        }

        // Crows wheeling high up.
        using (var crow = new SKPaint { IsAntialias = true, Color = new SKColor(0x04, 0x01, 0x02) })
        {
            foreach (var (cx, cy, size) in new[] { (0.62f, 0.1f, 0.03f), (0.72f, 0.16f, 0.022f), (0.8f, 0.08f, 0.026f), (0.55f, 0.2f, 0.016f) })
            {
                float x = w * cx, y = h * cy, r = s * size;
                using var bird = new SKPath();
                bird.MoveTo(x - r, y - (r * 0.2f));
                bird.QuadTo(x - (r * 0.5f), y - (r * 0.5f), x, y);
                bird.QuadTo(x + (r * 0.5f), y - (r * 0.5f), x + r, y - (r * 0.2f));
                bird.QuadTo(x + (r * 0.45f), y - (r * 0.1f), x, y + (r * 0.18f));
                bird.QuadTo(x - (r * 0.45f), y - (r * 0.1f), x - r, y - (r * 0.2f));
                c.DrawPath(bird, crow);
            }
        }

        // Old film: vertical scratches, dust and a heavy vignette.
        using (var scratch = new SKPaint { IsAntialias = true, StrokeWidth = Math.Max(1, s * 0.0012f) })
        {
            for (var i = 0; i < 9; i++)
            {
                var x = R(rng, 0, w);
                var y0 = R(rng, 0, h * 0.5f);
                scratch.Color = new SKColor(0xF0, 0xE0, 0xD0, (byte)rng.Next(25, 60));
                c.DrawLine(x, y0, x + R(rng, -s * 0.01f, s * 0.01f), y0 + R(rng, h * 0.2f, h * 0.6f), scratch);
            }

            for (var i = 0; i < 160; i++)
            {
                scratch.Color = new SKColor(0xF0, 0xE0, 0xD0, (byte)rng.Next(20, 70));
                c.DrawCircle(R(rng, 0, w), R(rng, 0, h), s * R(rng, 0.0008f, 0.0022f), scratch);
            }
        }

        using (var vignette = new SKPaint { Shader = SKShader.CreateRadialGradient(new SKPoint(w / 2f, h / 2f), MathF.Max(w, h) * 0.72f, new[] { SKColors.Transparent, SKColors.Transparent, new SKColor(0, 0, 0, 200) }, new[] { 0f, 0.6f, 1f }, SKShaderTileMode.Clamp) })
        {
            c.DrawRect(0, 0, w, h, vignette);
        }
    }

    // ====================================================================================================
    // Neon signs on a dark brick wall
    // ====================================================================================================

    /// <summary>A dark brick wall: rows of bricks with darker mortar, lit unevenly.</summary>
    private static void NeonWall(SKCanvas c, int w, int h, float s, Random rng, SKColor glow)
    {
        using (var wall = new SKPaint { Color = new SKColor(0x14, 0x0E, 0x16, 240) })
        {
            c.DrawRect(0, 0, w, h, wall);
        }

        var bh = s * 0.045f;
        var bw = s * 0.11f;
        using var brick = new SKPaint { IsAntialias = true };
        for (var row = 0; row * bh < h; row++)
        {
            var offset = row % 2 == 0 ? 0 : bw / 2;
            for (var x = -offset; x < w; x += bw)
            {
                var shade = (byte)rng.Next(26, 44);
                brick.Color = new SKColor((byte)(shade + 8), (byte)(shade * 0.6f), (byte)(shade * 0.7f), 255);
                c.DrawRoundRect(new SKRect(x + (s * 0.003f), (row * bh) + (s * 0.003f), x + bw - (s * 0.003f), ((row + 1) * bh) - (s * 0.003f)), s * 0.003f, s * 0.003f, brick);
            }
        }

        // The signs' coloured light washing over the wall, darker at the edges.
        using (var wash = new SKPaint { Shader = SKShader.CreateRadialGradient(new SKPoint(w / 2f, h * 0.75f), s * 0.8f, new[] { glow.WithAlpha(70), glow.WithAlpha(0) }, null, SKShaderTileMode.Clamp) })
        {
            c.DrawRect(0, 0, w, h, wash);
        }

        using var edge = new SKPaint { Shader = SKShader.CreateRadialGradient(new SKPoint(w / 2f, h / 2f), MathF.Max(w, h) * 0.7f, new[] { SKColors.Transparent, new SKColor(0, 0, 0, 170) }, new[] { 0.55f, 1f }, SKShaderTileMode.Clamp) };
        c.DrawRect(0, 0, w, h, edge);
    }

    /// <summary>A neon tube along a path: a wide soft glow, a tighter glow and a pale hot core.</summary>
    private static void NeonTube(SKCanvas c, SKPath path, SKColor colour, float s, float width = 0.008f)
    {
        using (var outer = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round, StrokeWidth = s * width * 3.5f, Color = colour.WithAlpha(90), MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.02f) })
        {
            c.DrawPath(path, outer);
        }

        using (var mid = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round, StrokeWidth = s * width * 1.6f, Color = colour, MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.004f) })
        {
            c.DrawPath(path, mid);
        }

        using var core = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round, StrokeWidth = s * width * 0.6f, Color = Lighten(colour, 0.7f) };
        c.DrawPath(path, core);
    }

    /// <summary>A small neon sparkle: a four-pointed star outline.</summary>
    private static void NeonSparkle(SKCanvas c, float x, float y, float r, SKColor colour, float s)
    {
        using var star = new SKPath();
        star.MoveTo(x, y - r);
        star.QuadTo(x, y, x + r, y);
        star.QuadTo(x, y, x, y + r);
        star.QuadTo(x, y, x - r, y);
        star.QuadTo(x, y, x, y - r);
        star.Close();
        NeonTube(c, star, colour, s, 0.004f);
    }

    /// <summary>A heart outline centred on (x, y), r across half its width.</summary>
    private static SKPath NeonHeartPath(float x, float y, float r)
    {
        var p = new SKPath();
        p.MoveTo(x, y + (r * 0.95f));
        p.CubicTo(x - (r * 1.3f), y + (r * 0.1f), x - (r * 1.05f), y - (r * 0.95f), x, y - (r * 0.35f));
        p.CubicTo(x + (r * 1.05f), y - (r * 0.95f), x + (r * 1.3f), y + (r * 0.1f), x, y + (r * 0.95f));
        p.Close();
        return p;
    }

    private static void NeonNewYear(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var wide = w > h;
        var gold = new SKColor(0xFF, 0xC8, 0x4A);
        var pink = new SKColor(0xFF, 0x4A, 0xB4);
        var teal = new SKColor(0x3A, 0xF0, 0xE0);
        NeonWall(c, w, h, s, rng, pink);

        // Two champagne flutes clinking at the bottom.
        var cx = w / 2f;
        var baseY = h * 0.96f;
        foreach (var side in new[] { -1, 1 })
        {
            c.Save();
            c.RotateDegrees(side * 14, cx + (side * s * 0.09f), baseY);
            var gx = cx + (side * s * 0.09f);
            using var glass = new SKPath();
            var rimY = baseY - (s * 0.3f);
            glass.MoveTo(gx - (s * 0.045f), rimY);
            glass.CubicTo(gx - (s * 0.048f), rimY + (s * 0.1f), gx - (s * 0.02f), rimY + (s * 0.17f), gx, rimY + (s * 0.18f));
            glass.CubicTo(gx + (s * 0.02f), rimY + (s * 0.17f), gx + (s * 0.048f), rimY + (s * 0.1f), gx + (s * 0.045f), rimY);
            glass.Close();
            glass.MoveTo(gx, rimY + (s * 0.18f));
            glass.LineTo(gx, baseY - (s * 0.012f));
            glass.MoveTo(gx - (s * 0.04f), baseY - (s * 0.006f));
            glass.LineTo(gx + (s * 0.04f), baseY - (s * 0.006f));
            NeonTube(c, glass, gold, s);
            using var fizz = new SKPath();
            for (var i = 0; i < 4; i++)
            {
                fizz.AddCircle(gx + R(rng, -s * 0.015f, s * 0.015f), rimY + (s * (0.04f + (i * 0.03f))), s * 0.006f);
            }

            NeonTube(c, fizz, gold, s, 0.003f);
            c.Restore();
        }

        // Sparks where the rims meet, and neon bursts and sparkles up top.
        NeonSparkle(c, cx, baseY - (s * 0.34f), s * 0.035f, teal, s);
        foreach (var (x, y, r, col) in new[] { (0.2f, 0.17f, 0.1f, pink), (0.8f, 0.15f, 0.09f, teal), (0.5f, 0.09f, 0.05f, gold) })
        {
            using var burst = new SKPath();
            for (var i = 0; i < 10; i++)
            {
                var a = i * MathF.PI / 5;
                burst.MoveTo((w * x) + (MathF.Cos(a) * s * r * 0.35f), (h * y) + (MathF.Sin(a) * s * r * 0.35f));
                burst.LineTo((w * x) + (MathF.Cos(a) * s * r), (h * y) + (MathF.Sin(a) * s * r));
            }

            NeonTube(c, burst, col, s, 0.005f);
        }

        NeonSparkle(c, w * (wide ? 0.12f : 0.14f), h * 0.78f, s * 0.03f, pink, s);
        NeonSparkle(c, w * (wide ? 0.88f : 0.86f), h * 0.8f, s * 0.025f, gold, s);
    }

    private static void NeonValentine(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var wide = w > h;
        var pink = new SKColor(0xFF, 0x3A, 0x8A);
        var red = new SKColor(0xFF, 0x30, 0x40);
        var white = new SKColor(0xFF, 0xE0, 0xF0);
        NeonWall(c, w, h, s, rng, pink);

        // A big heart low in the middle with an arrow through it.
        var hx = w / 2f;
        var hy = h * 0.83f;
        var hr = s * 0.13f;
        using (var heart = NeonHeartPath(hx, hy, hr))
        {
            NeonTube(c, heart, pink, s, 0.01f);
        }

        using (var arrow = new SKPath())
        {
            var from = new SKPoint(hx - (hr * 2f), hy + (hr * 0.5f));
            var to = new SKPoint(hx + (hr * 2f), hy - (hr * 0.7f));
            arrow.MoveTo(from);
            arrow.LineTo(to);
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var len = MathF.Sqrt((dx * dx) + (dy * dy));
            float ux = dx / len, uy = dy / len;
            arrow.MoveTo(to.X - (ux * s * 0.035f) - (uy * s * 0.02f), to.Y - (uy * s * 0.035f) + (ux * s * 0.02f));
            arrow.LineTo(to);
            arrow.LineTo(to.X - (ux * s * 0.035f) + (uy * s * 0.02f), to.Y - (uy * s * 0.035f) - (ux * s * 0.02f));
            for (var i = 0; i < 3; i++)
            {
                var fx = from.X + (ux * s * 0.012f * i);
                var fy = from.Y + (uy * s * 0.012f * i);
                arrow.MoveTo(fx, fy);
                arrow.LineTo(fx - (ux * s * 0.02f) - (uy * s * 0.02f), fy - (uy * s * 0.02f) + (ux * s * 0.02f));
            }

            NeonTube(c, arrow, white, s, 0.006f);
        }

        // Little hearts and sparkles up top.
        foreach (var (x, y, r, col) in new[] { (0.16f, 0.16f, 0.05f, red), (0.3f, 0.08f, 0.03f, pink), (0.82f, 0.14f, 0.06f, pink), (0.68f, 0.07f, 0.028f, red) })
        {
            using var small = NeonHeartPath(w * x, h * y, s * r);
            NeonTube(c, small, col, s, 0.006f);
        }

        NeonSparkle(c, w * 0.5f, h * 0.1f, s * 0.025f, white, s);
        NeonSparkle(c, w * (wide ? 0.15f : 0.14f), h * 0.82f, s * 0.03f, white, s);
        NeonSparkle(c, w * (wide ? 0.85f : 0.86f), h * 0.78f, s * 0.022f, pink, s);
    }

    // ====================================================================================================
    // Paper cut-out Christmas: layered paper with soft shadows
    // ====================================================================================================

    private static void PaperChristmas(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var wide = w > h;

        using (var card = new SKPaint { Shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, h), new[] { new SKColor(0x8E, 0x1E, 0x28, 240), new SKColor(0xB4, 0x2C, 0x30, 235) }, null, SKShaderTileMode.Clamp) })
        {
            c.DrawRect(0, 0, w, h, card);
        }

        // Paper fibres.
        using (var fibre = new SKPaint { IsAntialias = true, StrokeWidth = Math.Max(1, s * 0.001f) })
        {
            for (var i = 0; i < 260; i++)
            {
                fibre.Color = SKColors.White.WithAlpha((byte)rng.Next(6, 18));
                var x = R(rng, 0, w);
                var y = R(rng, 0, h);
                c.DrawLine(x, y, x + R(rng, -s * 0.02f, s * 0.02f), y + R(rng, -s * 0.01f, s * 0.01f), fibre);
            }
        }

        SKPaint Paper(SKColor colour) => new()
        {
            IsAntialias = true,
            Color = colour,
            ImageFilter = SKImageFilter.CreateDropShadow(0, s * 0.008f, s * 0.008f, s * 0.008f, SKColors.Black.WithAlpha(110))
        };

        // Stars hanging on threads from the top.
        using (var thread = new SKPaint { IsAntialias = true, StrokeWidth = Math.Max(1, s * 0.0015f), Color = new SKColor(0xF4, 0xE6, 0xC8, 160) })
        {
            foreach (var (x, len, r) in wide
                ? new[] { (0.1f, 0.14f, 0.04f), (0.24f, 0.08f, 0.03f), (0.38f, 0.17f, 0.035f), (0.62f, 0.12f, 0.03f), (0.76f, 0.18f, 0.04f), (0.9f, 0.1f, 0.03f) }
                : new[] { (0.14f, 0.15f, 0.045f), (0.32f, 0.08f, 0.035f), (0.5f, 0.16f, 0.04f), (0.68f, 0.09f, 0.035f), (0.86f, 0.17f, 0.045f) })
            {
                var sx = w * x;
                var sy = h * len;
                c.DrawLine(sx, 0, sx, sy - (s * r), thread);
                using var star = new SKPath();
                for (var i = 0; i < 10; i++)
                {
                    var a = (i * MathF.PI / 5) - (MathF.PI / 2);
                    var rr = s * r * (i % 2 == 0 ? 1f : 0.45f);
                    var pt = new SKPoint(sx + (MathF.Cos(a) * rr), sy + (MathF.Sin(a) * rr));
                    if (i == 0)
                    {
                        star.MoveTo(pt);
                    }
                    else
                    {
                        star.LineTo(pt);
                    }
                }

                star.Close();
                using var paint = Paper(rng.Next(2) == 0 ? new SKColor(0xF4, 0xD0, 0x6A) : new SKColor(0xF4, 0xEC, 0xDC));
                c.DrawPath(star, paint);
            }
        }

        // Layers from the back: far snowy hill, trees, a house, nearer hills, more trees.
        void Hill(float top, float amp, float phase, SKColor colour)
        {
            using var hill = new SKPath();
            hill.MoveTo(-2, h + 2);
            for (var x = -2f; x <= w + 2; x += 8)
            {
                hill.LineTo(x, top - (MathF.Sin(((x / w) * 1.3f * MathF.PI * 2) + phase) * amp));
            }

            hill.LineTo(w + 2, h + 2);
            hill.Close();
            using var paint = Paper(colour);
            c.DrawPath(hill, paint);
        }

        void Tree(float x, float ground, float size, SKColor colour)
        {
            using var tree = new SKPath();
            for (var tier = 0; tier < 3; tier++)
            {
                var bottom = ground - (size * tier * 0.28f);
                var half = size * (0.32f - (tier * 0.07f));
                tree.MoveTo(x - half, bottom - (size * 0.08f));
                tree.LineTo(x, bottom - (size * 0.5f));
                tree.LineTo(x + half, bottom - (size * 0.08f));
                tree.Close();
            }

            tree.AddRect(new SKRect(x - (size * 0.04f), ground - (size * 0.1f), x + (size * 0.04f), ground));
            using var paint = Paper(colour);
            c.DrawPath(tree, paint);
        }

        Hill(h * 0.78f, s * 0.025f, 0.6f, new SKColor(0xE8, 0xE2, 0xD8));
        Tree(w * 0.12f, h * 0.81f, s * 0.19f, new SKColor(0x2E, 0x6A, 0x4A));
        Tree(w * 0.84f, h * 0.81f, s * 0.19f, new SKColor(0x26, 0x5E, 0x42));
        if (wide)
        {
            Tree(w * 0.26f, h * 0.81f, s * 0.16f, new SKColor(0x3A, 0x7A, 0x52));
            Tree(w * 0.72f, h * 0.81f, s * 0.15f, new SKColor(0x3A, 0x7A, 0x52));
        }

        // A little house with a lit window.
        var hx = w * 0.58f;
        var hy = h * 0.86f;
        using (var house = new SKPath())
        {
            house.AddRect(new SKRect(hx - (s * 0.07f), hy - (s * 0.08f), hx + (s * 0.07f), hy));
            using var wall = Paper(new SKColor(0xF4, 0xEC, 0xDC));
            c.DrawPath(house, wall);
        }

        using (var roof = new SKPath())
        {
            roof.MoveTo(hx - (s * 0.085f), hy - (s * 0.075f));
            roof.LineTo(hx, hy - (s * 0.14f));
            roof.LineTo(hx + (s * 0.085f), hy - (s * 0.075f));
            roof.Close();
            using var red = Paper(new SKColor(0x6E, 0x14, 0x1C));
            c.DrawPath(roof, red);
        }

        using (var window = new SKPaint { IsAntialias = true, Color = new SKColor(0xF4, 0xC8, 0x5A) })
        {
            c.DrawRect(hx - (s * 0.045f), hy - (s * 0.06f), s * 0.03f, s * 0.03f, window);
            c.DrawRect(hx + (s * 0.015f), hy - (s * 0.06f), s * 0.03f, s * 0.03f, window);
        }

        Hill(h * 0.9f, s * 0.02f, 2.4f, new SKColor(0xF6, 0xF2, 0xEC));
        Tree(w * 0.3f, h * 0.93f, s * 0.15f, new SKColor(0x1E, 0x52, 0x38));
        Tree(w * 0.92f, h * 0.95f, s * 0.13f, new SKColor(0x1E, 0x52, 0x38));

        // Paper snowflakes: small white dots scattered over the sky, off the title.
        using (var snow = Paper(new SKColor(0xFA, 0xF6, 0xEE)))
        {
            for (var i = 0; i < 30; i++)
            {
                var x = R(rng, 0, w);
                var y = R(rng, h * 0.2f, h * 0.75f);
                if (FestInTitle(x, y, w, h, s * 0.02f))
                {
                    continue;
                }

                c.DrawCircle(x, y, s * R(rng, 0.004f, 0.009f), snow);
            }
        }
    }
}
