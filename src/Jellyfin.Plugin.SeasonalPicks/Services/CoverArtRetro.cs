using System;
using SkiaSharp;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>
/// Retro travel-poster scenes: flat colour, hard edges, sun rays, paper grain and a cream border. A second look for
/// the seasons that otherwise have one painted scene. The title band across the middle is left clear.
/// </summary>
internal static partial class CoverArt
{
    // ---------- shared poster pieces ----------

    /// <summary>Hard-edged horizontal bands of colour from the top; stops are fractions of the height, one more than colours.</summary>
    private static void RetroBands(SKCanvas c, int w, int h, SKColor[] colours, float[] stops, byte alpha = 240)
    {
        using var band = new SKPaint { IsAntialias = false };
        for (var i = 0; i < colours.Length; i++)
        {
            band.Color = colours[i].WithAlpha(alpha);
            c.DrawRect(0, h * stops[i], w, (h * stops[i + 1]) - (h * stops[i]) + 1, band);
        }
    }

    /// <summary>Alternate pale wedges fanning out from a point, inside <paramref name="clip"/>.</summary>
    private static void RetroRays(SKCanvas c, SKPoint from, SKRect clip, float s, SKColor colour, int count = 16, float start = MathF.PI, float sweep = MathF.PI)
    {
        using var ray = new SKPaint { IsAntialias = true, Color = colour };
        c.Save();
        c.ClipRect(clip);
        var step = sweep / count;
        for (var i = 0; i < count; i += 2)
        {
            var a0 = start + (i * step);
            using var wedge = new SKPath();
            wedge.MoveTo(from);
            wedge.LineTo(from.X + (MathF.Cos(a0) * s * 2), from.Y + (MathF.Sin(a0) * s * 2));
            wedge.LineTo(from.X + (MathF.Cos(a0 + step) * s * 2), from.Y + (MathF.Sin(a0 + step) * s * 2));
            wedge.Close();
            c.DrawPath(wedge, ray);
        }

        c.Restore();
    }

    /// <summary>Paper grain over everything and a cream border, as on a printed poster.</summary>
    private static void RetroFinish(SKCanvas c, int w, int h, float s, Random rng)
    {
        using (var grain = new SKPaint { IsAntialias = false })
        {
            for (var i = 0; i < (w > h ? 5200 : 4500); i++)
            {
                grain.Color = (i % 2 == 0 ? SKColors.White : SKColors.Black).WithAlpha((byte)rng.Next(10, 26));
                c.DrawRect(R(rng, 0, w), R(rng, 0, h), s * 0.0025f, s * 0.0025f, grain);
            }
        }

        using var border = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.018f, Color = new SKColor(0xF4, 0xE6, 0xC8, 230) };
        var inset = s * 0.03f;
        c.DrawRect(new SKRect(inset, inset, w - inset, h - inset), border);
    }

    /// <summary>A flat firework: straight spokes with a dot at each tip, and a few shorter inner spokes.</summary>
    private static void RetroBurst(SKCanvas c, float x, float y, float r, SKColor colour, float s, int spokes = 16)
    {
        using var line = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = s * 0.006f, Color = colour };
        using var dot = new SKPaint { IsAntialias = true, Color = colour };
        for (var i = 0; i < spokes; i++)
        {
            var a = i * MathF.PI * 2 / spokes;
            float cx = MathF.Cos(a), cy = MathF.Sin(a);
            c.DrawLine(x + (cx * r * 0.3f), y + (cy * r * 0.3f), x + (cx * r * 0.85f), y + (cy * r * 0.85f), line);
            c.DrawCircle(x + (cx * r), y + (cy * r), s * 0.007f, dot);
            var b = a + (MathF.PI / spokes);
            c.DrawCircle(x + (MathF.Cos(b) * r * 0.55f), y + (MathF.Sin(b) * r * 0.55f), s * 0.0045f, dot);
        }
    }

    /// <summary>Flat blocks of a city from x0 to x1 standing on <paramref name="ground"/>, with a scatter of lit windows.</summary>
    private static void RetroSkyline(SKCanvas c, float x0, float x1, float ground, float minH, float maxH, float s, Random rng, SKColor colour, SKColor window)
    {
        using var block = new SKPaint { IsAntialias = true, Color = colour };
        using var lit = new SKPaint { IsAntialias = false, Color = window };
        for (var x = x0; x < x1;)
        {
            var bw = s * R(rng, 0.05f, 0.1f);
            var bh = R(rng, minH, maxH);
            c.DrawRect(x, ground - bh, bw + 1, bh + 1, block);
            if (rng.Next(3) == 0)
            {
                c.DrawRect(x + (bw * 0.35f), ground - bh - (s * 0.02f), bw * 0.3f, s * 0.021f, block);
            }

            var size = s * 0.011f;
            for (var wy = ground - bh + (s * 0.02f); wy < ground - (s * 0.02f); wy += s * 0.03f)
            {
                for (var wx = x + (s * 0.012f); wx < x + bw - size; wx += s * 0.022f)
                {
                    if (rng.Next(4) == 0)
                    {
                        c.DrawRect(wx, wy, size, size * 1.3f, lit);
                    }
                }
            }

            x += bw;
        }
    }

    /// <summary>A smooth flat hill line across the cover from <paramref name="top"/>, filled to the bottom.</summary>
    private static void RetroHill(SKCanvas c, int w, int h, float top, float amp, float waves, float phase, SKColor colour)
    {
        using var hill = new SKPath();
        hill.MoveTo(-2, h + 2);
        for (var x = -2f; x <= w + 2; x += 8)
        {
            hill.LineTo(x, top - (MathF.Sin(((x / w) * waves * MathF.PI * 2) + phase) * amp));
        }

        hill.LineTo(w + 2, h + 2);
        hill.Close();
        using var paint = new SKPaint { IsAntialias = true, Color = colour };
        c.DrawPath(hill, paint);
    }

    /// <summary>Short flat bars on water under a light, shortening towards the viewer.</summary>
    private static void RetroReflection(SKCanvas c, float x, float top, float halfWidth, float s, SKColor colour, int rows = 5)
    {
        using var bar = new SKPaint { IsAntialias = true, Color = colour };
        for (var i = 0; i < rows; i++)
        {
            var y = top + (s * 0.014f) + (i * s * 0.022f);
            var half = halfWidth * (1f - (i * 0.17f));
            c.DrawRoundRect(new SKRect(x - half, y, x + half, y + (s * 0.008f)), s * 0.004f, s * 0.004f, bar);
        }
    }

    // ---------- New Year: flat fireworks over a city on the river ----------

    private static void RetroNewYear(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var water = h * 0.84f;
        RetroBands(c, w, h, new[] { new SKColor(0x12, 0x1A, 0x38), new SKColor(0x24, 0x1E, 0x4E), new SKColor(0x46, 0x22, 0x5E), new SKColor(0x78, 0x2C, 0x5E), new SKColor(0xB4, 0x44, 0x5C) }, new[] { 0f, 0.36f, 0.56f, 0.68f, 0.77f, 0.84f });
        RetroRays(c, new SKPoint(w / 2f, water), new SKRect(0, h * 0.62f, w, water), s, new SKColor(0xFF, 0xE0, 0xC0, 24), 14);

        var gold = new SKColor(0xF6, 0xC8, 0x4C);
        var pink = new SKColor(0xF2, 0x6B, 0x8A);
        var teal = new SKColor(0x5C, 0xD0, 0xC8);
        RetroBurst(c, w * 0.2f, h * 0.17f, s * 0.11f, gold, s);
        RetroBurst(c, w * 0.5f, h * 0.11f, s * 0.08f, pink, s, 12);
        RetroBurst(c, w * 0.8f, h * 0.19f, s * 0.12f, teal, s, 18);
        if (w > h)
        {
            RetroBurst(c, w * 0.08f, h * 0.45f, s * 0.06f, pink, s, 12);
            RetroBurst(c, w * 0.93f, h * 0.48f, s * 0.06f, gold, s, 12);
        }

        var city = new SKColor(0x10, 0x10, 0x26);
        RetroSkyline(c, -s * 0.02f, w, water, s * 0.06f, s * 0.15f, s, rng, city, new SKColor(0xF6, 0xC8, 0x4C, 220));

        // A clock tower at five to midnight.
        var tx = w * 0.64f;
        using (var tower = new SKPaint { IsAntialias = true, Color = city })
        using (var face = new SKPaint { IsAntialias = true, Color = new SKColor(0xF4, 0xE6, 0xC8) })
        using (var hand = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.005f, StrokeCap = SKStrokeCap.Round, Color = city })
        {
            c.DrawRect(tx - (s * 0.035f), water - (s * 0.17f), s * 0.07f, s * 0.17f, tower);
            using var roof = new SKPath();
            roof.MoveTo(tx - (s * 0.045f), water - (s * 0.17f));
            roof.LineTo(tx, water - (s * 0.22f));
            roof.LineTo(tx + (s * 0.045f), water - (s * 0.17f));
            roof.Close();
            c.DrawPath(roof, tower);
            var cy = water - (s * 0.13f);
            c.DrawCircle(tx, cy, s * 0.027f, face);
            c.DrawLine(tx, cy, tx, cy - (s * 0.02f), hand);
            c.DrawLine(tx, cy, tx - (s * 0.008f), cy - (s * 0.014f), hand);
        }

        using (var river = new SKPaint { IsAntialias = false, Color = new SKColor(0x16, 0x1C, 0x3A) })
        {
            c.DrawRect(0, water, w, h - water, river);
        }

        RetroReflection(c, w * 0.2f, water, s * 0.06f, s, gold.WithAlpha(200), 4);
        RetroReflection(c, w * 0.8f, water, s * 0.07f, s, teal.WithAlpha(200), 4);
        RetroReflection(c, tx, water, s * 0.03f, s, new SKColor(0xF4, 0xE6, 0xC8, 160), 4);
        RetroFinish(c, w, h, s, rng);
    }

    // ---------- Bonfire Night: a flat bonfire, rockets and the crowd in silhouette ----------

    private static void RetroBonfire(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var ground = h * 0.86f;
        RetroBands(c, w, h, new[] { new SKColor(0x14, 0x10, 0x1E), new SKColor(0x2A, 0x16, 0x2C), new SKColor(0x4E, 0x1E, 0x2E), new SKColor(0x8A, 0x2E, 0x26), new SKColor(0xC8, 0x52, 0x22) }, new[] { 0f, 0.38f, 0.58f, 0.7f, 0.79f, 0.86f });
        RetroRays(c, new SKPoint(w / 2f, ground), new SKRect(0, h * 0.64f, w, ground), s, new SKColor(0xFF, 0xD0, 0x90, 26), 18);

        RetroBurst(c, w * 0.22f, h * 0.16f, s * 0.1f, new SKColor(0xF2, 0x6B, 0x8A), s);
        RetroBurst(c, w * 0.78f, h * 0.14f, s * 0.11f, new SKColor(0xF6, 0xC8, 0x4C), s, 18);
        using (var trail = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.004f, StrokeCap = SKStrokeCap.Round, PathEffect = SKPathEffect.CreateDash(new[] { s * 0.012f, s * 0.01f }, 0), Color = new SKColor(0xF4, 0xE6, 0xC8, 180) })
        {
            c.DrawLine(w * 0.22f, h * 0.3f, w * 0.17f, h * 0.34f, trail);
            c.DrawLine(w * 0.5f, h * 0.06f, w * 0.47f, h * 0.24f, trail);
        }

        RetroHill(c, w, h, ground, s * 0.008f, 2, 0.5f, new SKColor(0x18, 0x10, 0x14));

        // Flames: nested flat tongues, red outside to pale yellow at the heart.
        var fx = w / 2f;
        var flames = new[] { (new SKColor(0xC8, 0x2E, 0x1E), 1f), (new SKColor(0xEE, 0x6A, 0x22), 0.74f), (new SKColor(0xF6, 0xB4, 0x3A), 0.5f), (new SKColor(0xFA, 0xE6, 0x9A), 0.28f) };
        foreach (var (colour, k) in flames)
        {
            using var fire = new SKPath();
            var fw = s * 0.2f * k;
            var fh = s * 0.2f * (0.4f + (k * 0.6f));
            var baseY = ground - (s * 0.015f);
            fire.MoveTo(fx - fw, baseY);
            fire.CubicTo(fx - fw, baseY - (fh * 0.5f), fx - (fw * 0.55f), baseY - (fh * 0.55f), fx - (fw * 0.45f), baseY - (fh * 0.95f));
            fire.QuadTo(fx - (fw * 0.2f), baseY - (fh * 0.6f), fx - (fw * 0.05f), baseY - fh * 1.1f);
            fire.QuadTo(fx + (fw * 0.15f), baseY - (fh * 0.65f), fx + (fw * 0.4f), baseY - (fh * 0.9f));
            fire.CubicTo(fx + (fw * 0.55f), baseY - (fh * 0.55f), fx + fw, baseY - (fh * 0.5f), fx + fw, baseY);
            fire.Close();
            using var paint = new SKPaint { IsAntialias = true, Color = colour };
            c.DrawPath(fire, paint);
        }

        using (var log = new SKPaint { IsAntialias = true, StrokeWidth = s * 0.022f, StrokeCap = SKStrokeCap.Round, Color = new SKColor(0x3A, 0x22, 0x18) })
        {
            c.DrawLine(fx - (s * 0.2f), ground + (s * 0.005f), fx + (s * 0.16f), ground - (s * 0.035f), log);
            c.DrawLine(fx + (s * 0.2f), ground + (s * 0.005f), fx - (s * 0.16f), ground - (s * 0.035f), log);
        }

        using (var spark = new SKPaint { IsAntialias = true, Color = new SKColor(0xF6, 0xC8, 0x4C) })
        {
            for (var i = 0; i < 24; i++)
            {
                var y = R(rng, h * 0.64f, ground - (s * 0.1f));
                c.DrawCircle(fx + R(rng, -s * 0.22f, s * 0.22f), y, s * R(rng, 0.003f, 0.006f), spark);
            }
        }

        // The crowd: rounded heads and shoulders at both edges.
        using (var crowd = new SKPaint { IsAntialias = true, Color = new SKColor(0x0C, 0x08, 0x0C) })
        {
            foreach (var (x, size) in new[] { (0.06f, 0.11f), (0.17f, 0.09f), (0.27f, 0.075f), (0.73f, 0.08f), (0.84f, 0.1f), (0.95f, 0.11f) })
            {
                var px = w * x;
                var r = s * size;
                c.DrawCircle(px, h - (r * 1.25f), r * 0.42f, crowd);
                c.DrawRoundRect(new SKRect(px - (r * 0.75f), h - (r * 0.85f), px + (r * 0.75f), h + r), r * 0.5f, r * 0.5f, crowd);
            }
        }

        RetroFinish(c, w, h, s, rng);
    }

    // ---------- St Patrick's Day: green hills under a flat rainbow ----------

    private static void RetroShamrockHills(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var horizon = h * 0.74f;
        RetroBands(c, w, h, new[] { new SKColor(0x2E, 0x5E, 0x6A), new SKColor(0x4A, 0x82, 0x7E), new SKColor(0x7E, 0xA8, 0x86), new SKColor(0xC8, 0xC4, 0x84), new SKColor(0xEE, 0xD6, 0x8A) }, new[] { 0f, 0.34f, 0.54f, 0.64f, 0.7f, 0.74f });
        var sun = new SKPoint(w * 0.5f, horizon);
        RetroRays(c, sun, new SKRect(0, h * 0.62f, w, horizon), s, new SKColor(0xFF, 0xF4, 0xD0, 34), 18);

        // A rainbow arching over the whole scene, its ends behind the hills, kept above the title band.
        var arcs = new[] { new SKColor(0xD8, 0x4A, 0x3E), new SKColor(0xEE, 0x8E, 0x3A), new SKColor(0xF2, 0xCC, 0x4A), new SKColor(0x5C, 0xA8, 0x5A), new SKColor(0x3E, 0x7E, 0xB8), new SKColor(0x7A, 0x5A, 0xA8) };
        var rx = w * 0.62f;
        var ry = horizon - (h * 0.06f);
        for (var i = 0; i < arcs.Length; i++)
        {
            var inset = i * s * 0.022f;
            using var arc = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.023f, Color = arcs[i].WithAlpha(230) };
            c.DrawArc(new SKRect((w / 2f) - rx + inset, ry - (horizon * 0.8f) + inset, (w / 2f) + rx - inset, ry + (horizon * 0.8f) - inset), 180, 180, false, arc);
        }

        RetroHill(c, w, h, h * 0.76f, s * 0.025f, 1.3f, 0.4f, new SKColor(0x6E, 0xA6, 0x52));
        RetroHill(c, w, h, h * 0.83f, s * 0.03f, 1.1f, 2.2f, new SKColor(0x3E, 0x86, 0x46));
        RetroHill(c, w, h, h * 0.92f, s * 0.025f, 0.9f, 4.1f, new SKColor(0x22, 0x5E, 0x34));

        // A white cottage with a dark roof, and a few sheep.
        var cx = w * 0.3f;
        var cy = h * 0.79f;
        using (var wall = new SKPaint { IsAntialias = true, Color = new SKColor(0xF4, 0xEC, 0xD8) })
        using (var roof = new SKPaint { IsAntialias = true, Color = new SKColor(0x2A, 0x2A, 0x34) })
        using (var door = new SKPaint { IsAntialias = true, Color = new SKColor(0xC8, 0x3A, 0x2E) })
        {
            c.DrawRect(cx - (s * 0.05f), cy - (s * 0.035f), s * 0.1f, s * 0.035f, wall);
            using var top = new SKPath();
            top.MoveTo(cx - (s * 0.058f), cy - (s * 0.035f));
            top.LineTo(cx - (s * 0.035f), cy - (s * 0.065f));
            top.LineTo(cx + (s * 0.035f), cy - (s * 0.065f));
            top.LineTo(cx + (s * 0.058f), cy - (s * 0.035f));
            top.Close();
            c.DrawPath(top, roof);
            c.DrawRect(cx - (s * 0.007f), cy - (s * 0.022f), s * 0.014f, s * 0.022f, door);
        }

        using (var wool = new SKPaint { IsAntialias = true, Color = new SKColor(0xF4, 0xEC, 0xD8) })
        using (var face = new SKPaint { IsAntialias = true, Color = new SKColor(0x2A, 0x2A, 0x34) })
        {
            foreach (var (x, y) in new[] { (0.62f, 0.86f), (0.7f, 0.875f), (0.8f, 0.86f) })
            {
                c.DrawOval(new SKRect((w * x) - (s * 0.018f), (h * y) - (s * 0.011f), (w * x) + (s * 0.018f), (h * y) + (s * 0.011f)), wool);
                c.DrawCircle((w * x) + (s * 0.02f), (h * y) - (s * 0.004f), s * 0.006f, face);
            }
        }

        RetroFinish(c, w, h, s, rng);
    }

    // ---------- Star Wars Day: a flat ringed planet and two blades crossing ----------

    private static void RetroSabers(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var wide = w > h;
        RetroBands(c, w, h, new[] { new SKColor(0x0C, 0x0E, 0x22), new SKColor(0x18, 0x14, 0x34), new SKColor(0x2C, 0x18, 0x40), new SKColor(0x46, 0x1C, 0x46) }, new[] { 0f, 0.4f, 0.62f, 0.8f, 1f });
        using (var star = new SKPaint { IsAntialias = true, Color = new SKColor(0xF4, 0xE6, 0xC8) })
        {
            for (var i = 0; i < (wide ? 90 : 70); i++)
            {
                var x = R(rng, 0, w);
                var y = R(rng, 0, h);
                if (FestInTitle(x, y, w, h, 0))
                {
                    continue;
                }

                c.DrawCircle(x, y, s * R(rng, 0.0015f, 0.004f), star);
            }
        }

        // A ringed planet in two flat tones with stripes, top left.
        var p = new SKPoint(w * (wide ? 0.14f : 0.2f), h * 0.17f);
        var pr = s * 0.11f;
        using (var ringBack = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.012f, Color = new SKColor(0xE8, 0xB8, 0x6A) })
        {
            c.Save();
            c.RotateDegrees(-16, p.X, p.Y);
            c.DrawArc(new SKRect(p.X - (pr * 1.8f), p.Y - (pr * 0.45f), p.X + (pr * 1.8f), p.Y + (pr * 0.45f)), 180, 180, false, ringBack);
            using (var body = new SKPaint { IsAntialias = true, Color = new SKColor(0xEE, 0x8E, 0x4A) })
            using (var stripe = new SKPaint { IsAntialias = true, Color = new SKColor(0xC8, 0x5E, 0x3A) })
            using (var round = new SKPath())
            {
                round.AddCircle(p.X, p.Y, pr);
                c.DrawPath(round, body);
                c.Save();
                c.ClipPath(round, antialias: true);
                c.DrawRect(p.X - pr, p.Y - (pr * 0.35f), pr * 2, pr * 0.18f, stripe);
                c.DrawRect(p.X - pr, p.Y + (pr * 0.1f), pr * 2, pr * 0.28f, stripe);
                c.DrawRect(p.X, p.Y - pr, pr, pr * 2, new SKPaint { Color = SKColors.Black.WithAlpha(50) });
                c.Restore();
            }

            c.DrawArc(new SKRect(p.X - (pr * 1.8f), p.Y - (pr * 0.45f), p.X + (pr * 1.8f), p.Y + (pr * 0.45f)), 0, 180, false, ringBack);
            c.Restore();
        }

        // Two blades crossing low down: a flat coloured stroke with a white core, plain grey hilts.
        var blue = new SKColor(0x3E, 0xA6, 0xF2);
        var red = new SKColor(0xEE, 0x3A, 0x3A);
        var bluePommel = new SKPoint(w * (wide ? 0.2f : 0.12f), h * 0.97f);
        var redPommel = new SKPoint(w - bluePommel.X, bluePommel.Y);
        var tipY = h * (wide ? 0.72f : 0.68f);
        var blueTip = new SKPoint(w * (wide ? 0.66f : 0.72f), tipY);
        var redTip = new SKPoint(w - blueTip.X, tipY);
        foreach (var (pommel, tip, colour) in new[] { (bluePommel, blueTip, blue), (redPommel, redTip, red) })
        {
            var dx = tip.X - pommel.X;
            var dy = tip.Y - pommel.Y;
            var len = MathF.Sqrt((dx * dx) + (dy * dy));
            var hiltEnd = new SKPoint(pommel.X + (dx / len * s * 0.1f), pommel.Y + (dy / len * s * 0.1f));
            using (var outer = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = s * 0.03f, Color = colour.WithAlpha(110) })
            using (var mid = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = s * 0.018f, Color = colour })
            using (var core = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = s * 0.007f, Color = new SKColor(0xFA, 0xF4, 0xEA) })
            {
                c.DrawLine(hiltEnd, tip, outer);
                c.DrawLine(hiltEnd, tip, mid);
                c.DrawLine(hiltEnd, tip, core);
            }

            using var hilt = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Butt, StrokeWidth = s * 0.026f, Color = new SKColor(0x9A, 0x9C, 0xA8) };
            c.DrawLine(pommel, hiltEnd, hilt);
            hilt.Color = new SKColor(0x3A, 0x3A, 0x46);
            hilt.StrokeWidth = s * 0.028f;
            hilt.PathEffect = SKPathEffect.CreateDash(new[] { s * 0.006f, s * 0.008f }, 0);
            c.DrawLine(pommel, new SKPoint(pommel.X + ((hiltEnd.X - pommel.X) * 0.55f), pommel.Y + ((hiltEnd.Y - pommel.Y) * 0.55f)), hilt);
        }

        // Where they meet: a flat eight-pointed flash.
        var clash = FestCross(bluePommel, blueTip, redPommel, redTip);
        using (var flash = new SKPaint { IsAntialias = true, Color = new SKColor(0xFA, 0xF4, 0xEA) })
        using (var star8 = new SKPath())
        {
            for (var i = 0; i < 16; i++)
            {
                var a = i * MathF.PI / 8;
                var r = s * (i % 2 == 0 ? (i % 4 == 0 ? 0.06f : 0.035f) : 0.012f);
                var pt = new SKPoint(clash.X + (MathF.Cos(a) * r), clash.Y + (MathF.Sin(a) * r));
                if (i == 0)
                {
                    star8.MoveTo(pt);
                }
                else
                {
                    star8.LineTo(pt);
                }
            }

            star8.Close();
            c.DrawPath(star8, flash);
        }
        RetroFinish(c, w, h, s, rng);
    }

    // ---------- Ghostbusters Day: flat ghosts round a tall tower over the city ----------

    private static void RetroGhostCity(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var wide = w > h;
        var ground = h * 0.9f;
        RetroBands(c, w, h, new[] { new SKColor(0x0E, 0x1C, 0x1A), new SKColor(0x16, 0x30, 0x28), new SKColor(0x22, 0x4A, 0x34), new SKColor(0x4A, 0x7A, 0x3A), new SKColor(0x9A, 0xC0, 0x4E) }, new[] { 0f, 0.38f, 0.58f, 0.72f, 0.82f, 0.9f });

        var tx = w * (wide ? 0.9f : 0.93f);
        RetroRays(c, new SKPoint(tx, h * 0.12f), new SKRect(0, 0, w, ground), s, new SKColor(0xB8, 0xF0, 0x8A, 22), 22, 0, MathF.PI * 2);

        var city = new SKColor(0x0C, 0x16, 0x14);
        RetroSkyline(c, -s * 0.02f, w, ground, s * 0.07f, s * 0.2f, s, rng, city, new SKColor(0xF6, 0xD8, 0x6A, 220));
        using (var street = new SKPaint { IsAntialias = false, Color = new SKColor(0x08, 0x10, 0x0E) })
        {
            c.DrawRect(0, ground, w, h - ground, street);
        }

        // The tower: stepped, with a spire into the swirl.
        using (var tower = new SKPaint { IsAntialias = true, Color = city })
        {
            var tw = s * 0.055f;
            c.DrawRect(tx - (tw / 2), h * 0.36f, tw, ground - (h * 0.36f) + 1, tower);
            c.DrawRect(tx - (tw * 0.36f), h * 0.27f, tw * 0.72f, h * 0.1f, tower);
            c.DrawRect(tx - (tw * 0.22f), h * 0.2f, tw * 0.44f, h * 0.08f, tower);
            using var spire = new SKPath();
            spire.MoveTo(tx - (tw * 0.1f), h * 0.2f);
            spire.LineTo(tx, h * 0.12f);
            spire.LineTo(tx + (tw * 0.1f), h * 0.2f);
            spire.Close();
            c.DrawPath(spire, tower);
        }

        using (var swirl = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.006f, StrokeCap = SKStrokeCap.Round, Color = new SKColor(0xB8, 0xF0, 0x8A) })
        {
            for (var i = 0; i < 3; i++)
            {
                var r = s * (0.05f + (i * 0.035f));
                c.DrawArc(new SKRect(tx - (r * 1.6f), (h * 0.1f) - (r * 0.5f), tx + (r * 1.6f), (h * 0.1f) + (r * 0.5f)), 200 + (i * 40), 220, false, swirl);
            }
        }

        // Flat ghosts: a rounded hood, a wavy hem and two hollow eyes.
        void Ghost(float gx, float gy, float r, float tilt)
        {
            c.Save();
            c.RotateDegrees(tilt, gx, gy);
            using var body = new SKPath();
            body.MoveTo(gx - r, gy + (r * 1.1f));
            body.LineTo(gx - r, gy);
            body.ArcTo(new SKRect(gx - r, gy - r, gx + r, gy + r), 180, 180, false);
            body.LineTo(gx + r, gy + (r * 1.1f));
            for (var k = 0; k < 4; k++)
            {
                var x0 = gx + r - (k * r * 0.5f);
                body.QuadTo(x0 - (r * 0.25f), gy + (r * (k % 2 == 0 ? 0.8f : 1.35f)), x0 - (r * 0.5f), gy + (r * 1.1f));
            }

            body.Close();
            using var sheet = new SKPaint { IsAntialias = true, Color = new SKColor(0xEE, 0xF6, 0xE4, 235) };
            c.DrawPath(body, sheet);
            using var eye = new SKPaint { IsAntialias = true, Color = new SKColor(0x16, 0x30, 0x28) };
            c.DrawOval(new SKRect(gx - (r * 0.45f), gy - (r * 0.2f), gx - (r * 0.15f), gy + (r * 0.25f)), eye);
            c.DrawOval(new SKRect(gx + (r * 0.1f), gy - (r * 0.2f), gx + (r * 0.4f), gy + (r * 0.25f)), eye);
            c.Restore();
        }

        Ghost(w * 0.17f, h * 0.15f, s * 0.07f, -12);
        Ghost(w * 0.42f, h * 0.1f, s * 0.04f, 10);
        Ghost(w * (wide ? 0.72f : 0.66f), h * 0.2f, s * 0.045f, 14);
        Ghost(w * 0.48f, h * 0.8f, s * 0.05f, -8);
        RetroFinish(c, w, h, s, rng);
    }

    // ---------- Back to Hogwarts: a flat castle on its crag under a big moon ----------

    private static void RetroCastle(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var wide = w > h;
        var water = h * 0.86f;
        RetroBands(c, w, h, new[] { new SKColor(0x10, 0x12, 0x30), new SKColor(0x1E, 0x1C, 0x46), new SKColor(0x34, 0x24, 0x56), new SKColor(0x58, 0x30, 0x62), new SKColor(0x8A, 0x46, 0x6A) }, new[] { 0f, 0.38f, 0.58f, 0.7f, 0.79f, 0.86f });

        var moon = new SKPoint(w * (wide ? 0.16f : 0.22f), h * 0.18f);
        var mr = s * 0.1f;
        RetroRays(c, moon, new SKRect(0, 0, w, h * 0.34f), s, new SKColor(0xF4, 0xE6, 0xC8, 18), 24, 0, MathF.PI * 2);
        using (var disc = new SKPaint { IsAntialias = true, Color = new SKColor(0xF4, 0xE6, 0xC8) })
        using (var shade = new SKPaint { IsAntialias = true, Color = new SKColor(0xDC, 0xC8, 0xA4) })
        {
            c.DrawCircle(moon, mr, disc);
            c.DrawCircle(moon.X + (mr * 0.3f), moon.Y - (mr * 0.2f), mr * 0.18f, shade);
            c.DrawCircle(moon.X - (mr * 0.35f), moon.Y + (mr * 0.3f), mr * 0.12f, shade);
        }

        using (var star = new SKPaint { IsAntialias = true, Color = new SKColor(0xF4, 0xE6, 0xC8) })
        {
            for (var i = 0; i < 40; i++)
            {
                var x = R(rng, 0, w);
                var y = R(rng, 0, h * 0.32f);
                c.DrawCircle(x, y, s * R(rng, 0.0015f, 0.0035f), star);
            }
        }

        RetroHill(c, w, h, h * 0.8f, s * 0.04f, 1.4f, 1.1f, new SKColor(0x2A, 0x22, 0x4A));

        // The crag and castle, right of centre: towers with cone roofs, lit windows.
        var cx = w * (wide ? 0.74f : 0.68f);
        var u = s * 0.008f;
        var dark = new SKColor(0x12, 0x10, 0x24);
        using (var rock = new SKPaint { IsAntialias = true, Color = dark })
        using (var lit = new SKPaint { IsAntialias = false, Color = new SKColor(0xF6, 0xC8, 0x4C) })
        {
            using (var crag = new SKPath())
            {
                crag.MoveTo(cx - (u * 22), water);
                crag.LineTo(cx - (u * 14), water - (u * 7));
                crag.LineTo(cx + (u * 16), water - (u * 8));
                crag.LineTo(cx + (u * 24), water);
                crag.Close();
                c.DrawPath(crag, rock);
            }

            var top = water - (u * 7.5f);
            foreach (var (dx, tw, th) in new[] { (-12f, 5f, 10f), (-5f, 4f, 14f), (2f, 9f, 8f), (9f, 4f, 16f), (15f, 5f, 11f) })
            {
                var x0 = cx + (dx * u) - (tw * u / 2);
                c.DrawRect(x0, top - (th * u), tw * u, (th * u) + 1, rock);
                using var cone = new SKPath();
                cone.MoveTo(x0 - (u * 0.6f), top - (th * u));
                cone.LineTo(x0 + (tw * u / 2), top - (th * u) - (tw * u * 1.4f));
                cone.LineTo(x0 + (tw * u) + (u * 0.6f), top - (th * u));
                cone.Close();
                c.DrawPath(cone, rock);
                for (var wy = top - (th * u) + (u * 2); wy < top - (u * 2); wy += u * 3.5f)
                {
                    if (rng.Next(3) != 0)
                    {
                        c.DrawRect(x0 + (tw * u / 2) - (u * 0.6f), wy, u * 1.2f, u * 1.8f, lit);
                    }
                }
            }
        }

        using (var lake = new SKPaint { IsAntialias = false, Color = new SKColor(0x18, 0x18, 0x3A) })
        {
            c.DrawRect(0, water, w, h - water, lake);
        }

        RetroReflection(c, cx, water, s * 0.08f, s, new SKColor(0xF6, 0xC8, 0x4C, 170), 4);
        RetroReflection(c, moon.X, water, s * 0.05f, s, new SKColor(0xF4, 0xE6, 0xC8, 120), 4);
        RetroFinish(c, w, h, s, rng);
    }

    // ---------- Pancake Day: a flat stack with syrup under sunburst rays ----------

    private static void RetroPancakes(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var table = h * 0.9f;
        RetroBands(c, w, h, new[] { new SKColor(0x4A, 0x24, 0x1E), new SKColor(0x7A, 0x36, 0x22), new SKColor(0xB4, 0x5A, 0x28), new SKColor(0xE0, 0x8C, 0x36), new SKColor(0xF2, 0xB8, 0x52) }, new[] { 0f, 0.38f, 0.58f, 0.7f, 0.8f, 0.9f });
        var cx = w / 2f;
        RetroRays(c, new SKPoint(cx, h * 0.82f), new SKRect(0, h * 0.64f, w, table), s, new SKColor(0xFF, 0xF0, 0xD0, 30), 18);

        using (var wood = new SKPaint { IsAntialias = false, Color = new SKColor(0x5A, 0x34, 0x22) })
        {
            c.DrawRect(0, table, w, h - table, wood);
            wood.Color = new SKColor(0x4A, 0x2A, 0x1A);
            c.DrawRect(0, table + (s * 0.03f), w, s * 0.006f, wood);
        }

        // Plate, then pancakes stacked from the bottom: a darker edge and a golden top for each.
        var pw = s * 0.24f;
        using (var plate = new SKPaint { IsAntialias = true, Color = new SKColor(0xF4, 0xEC, 0xD8) })
        {
            c.DrawOval(new SKRect(cx - (pw * 1.25f), table - (s * 0.03f), cx + (pw * 1.25f), table + (s * 0.025f)), plate);
        }

        var layer = s * 0.028f;
        var y = table - (s * 0.01f);
        using var edge = new SKPaint { IsAntialias = true, Color = new SKColor(0xC4, 0x7A, 0x34) };
        using var face = new SKPaint { IsAntialias = true, Color = new SKColor(0xEE, 0xB4, 0x5A) };
        for (var i = 0; i < 6; i++)
        {
            var jitter = R(rng, -s * 0.006f, s * 0.006f);
            c.DrawRoundRect(new SKRect(cx - pw + jitter, y - layer, cx + pw + jitter, y), layer * 0.5f, layer * 0.5f, edge);
            c.DrawOval(new SKRect(cx - pw + jitter, y - layer - (s * 0.018f), cx + pw + jitter, y - layer + (s * 0.018f)), face);
            y -= layer;
        }

        // Syrup over the top and running down the sides in a few drips.
        using (var syrup = new SKPaint { IsAntialias = true, Color = new SKColor(0x8A, 0x3E, 0x16) })
        {
            c.DrawOval(new SKRect(cx - (pw * 0.75f), y - (s * 0.03f), cx + (pw * 0.75f), y + (s * 0.006f)), syrup);
            foreach (var (dx, len) in new[] { (-0.55f, 0.07f), (-0.15f, 0.11f), (0.3f, 0.06f), (0.62f, 0.09f) })
            {
                var x = cx + (pw * dx);
                c.DrawRoundRect(new SKRect(x - (s * 0.009f), y - (s * 0.01f), x + (s * 0.009f), y + (s * len)), s * 0.009f, s * 0.009f, syrup);
            }
        }

        using (var butter = new SKPaint { IsAntialias = true, Color = new SKColor(0xF8, 0xE4, 0x8A) })
        {
            c.DrawRect(cx - (s * 0.03f), y - (s * 0.045f), s * 0.06f, s * 0.03f, butter);
            butter.Color = new SKColor(0xFC, 0xF0, 0xB4);
            c.DrawRect(cx - (s * 0.03f), y - (s * 0.045f), s * 0.06f, s * 0.008f, butter);
        }

        // Up top: a frying pan tossing a pancake, with a dashed arc for its flight.
        var wide = w > h;
        var pan = new SKPoint(w * (wide ? 0.2f : 0.24f), h * 0.2f);
        using (var iron = new SKPaint { IsAntialias = true, Color = new SKColor(0x22, 0x1A, 0x1A) })
        using (var handle = new SKPaint { IsAntialias = true, StrokeWidth = s * 0.022f, StrokeCap = SKStrokeCap.Round, Color = new SKColor(0x22, 0x1A, 0x1A) })
        {
            c.Save();
            c.RotateDegrees(-14, pan.X, pan.Y);
            c.DrawLine(pan.X - (s * 0.1f), pan.Y, pan.X - (s * 0.22f), pan.Y + (s * 0.02f), handle);
            c.DrawOval(new SKRect(pan.X - (s * 0.11f), pan.Y - (s * 0.02f), pan.X + (s * 0.11f), pan.Y + (s * 0.035f)), iron);
            c.Restore();
        }

        var flying = new SKPoint(w * (wide ? 0.42f : 0.56f), h * 0.1f);
        using (var arc = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.004f, StrokeCap = SKStrokeCap.Round, PathEffect = SKPathEffect.CreateDash(new[] { s * 0.014f, s * 0.012f }, 0), Color = new SKColor(0xF4, 0xE6, 0xC8, 170) })
        using (var flight = new SKPath())
        {
            flight.MoveTo(pan.X + (s * 0.04f), pan.Y - (s * 0.04f));
            flight.QuadTo((pan.X + flying.X) / 2, h * 0.02f, flying.X - (s * 0.08f), flying.Y);
            c.DrawPath(flight, arc);
        }

        c.Save();
        c.RotateDegrees(22, flying.X, flying.Y);
        c.DrawOval(new SKRect(flying.X - (s * 0.075f), flying.Y - (s * 0.014f), flying.X + (s * 0.075f), flying.Y + (s * 0.024f)), edge);
        c.DrawOval(new SKRect(flying.X - (s * 0.075f), flying.Y - (s * 0.024f), flying.X + (s * 0.075f), flying.Y + (s * 0.014f)), face);
        c.Restore();

        // A lemon half on the plate's edge.
        using (var lemon = new SKPaint { IsAntialias = true, Color = new SKColor(0xF2, 0xD0, 0x3A) })
        {
            c.DrawCircle(cx + (pw * 1.3f), table - (s * 0.02f), s * 0.03f, lemon);
            lemon.Color = new SKColor(0xFA, 0xEA, 0x9A);
            c.DrawCircle(cx + (pw * 1.3f), table - (s * 0.02f), s * 0.022f, lemon);
        }

        RetroFinish(c, w, h, s, rng);
    }
}
