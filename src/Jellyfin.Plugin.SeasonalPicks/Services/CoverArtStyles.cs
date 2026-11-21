using System;
using SkiaSharp;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>
/// Scenes drawn in other art styles than the painted ones, so a season's covers can vary in look as well as subject.
/// Retro poster: flat bands of colour with hard edges, a striped sun, sun rays and a paper grain, like an old
/// railway travel poster.
/// </summary>
internal static partial class CoverArt
{
    // ---------- Summer, retro poster: a striped sun setting over flat waves, one palm, paper grain ----------

    private static void RetroSummer(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var wide = w > h;
        var horizon = h * 0.78f;
        var sun = new SKPoint(w * (wide ? 0.62f : 0.6f), horizon);
        var sunR = s * 0.17f;

        // Sky in hard-edged bands, deep teal down through magenta and orange to gold at the horizon.
        var bands = new[]
        {
            new SKColor(0x22, 0x4E, 0x62), new SKColor(0x4A, 0x34, 0x6E), new SKColor(0x8E, 0x34, 0x6E),
            new SKColor(0xC2, 0x3B, 0x5A), new SKColor(0xEE, 0x6A, 0x3F), new SKColor(0xF7, 0x9E, 0x3B)
        };
        var stops = new[] { 0f, 0.3f, 0.45f, 0.56f, 0.65f, 0.72f, 0.78f };
        using (var band = new SKPaint { IsAntialias = false })
        {
            for (var i = 0; i < bands.Length; i++)
            {
                band.Color = bands[i].WithAlpha(i < 2 ? (byte)235 : (byte)248);
                c.DrawRect(0, h * stops[i], w, (h * stops[i + 1]) - (h * stops[i]) + 1, band);
            }
        }

        // Sun rays: alternate wedges a shade lighter, fanning out from the sun across the lower sky.
        using (var ray = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xE8, 0xB0, 34) })
        {
            c.Save();
            c.ClipRect(new SKRect(0, h * 0.3f, w, horizon));
            for (var i = 0; i < 16; i++)
            {
                var a0 = MathF.PI + (i * MathF.PI / 16f);
                if (i % 2 == 1)
                {
                    continue;
                }

                var a1 = a0 + (MathF.PI / 16f);
                var far = s * 2f;
                using var wedge = new SKPath();
                wedge.MoveTo(sun);
                wedge.LineTo(sun.X + (MathF.Cos(a0) * far), sun.Y + (MathF.Sin(a0) * far));
                wedge.LineTo(sun.X + (MathF.Cos(a1) * far), sun.Y + (MathF.Sin(a1) * far));
                wedge.Close();
                c.DrawPath(wedge, ray);
            }

            c.Restore();
        }

        // The sun: two flat tones, its lower half cut into ever-thicker gaps the sky shows through.
        c.Save();
        c.ClipRect(new SKRect(0, 0, w, horizon));
        c.SaveLayer();
        using (var disc = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xE0, 0x50) })
        using (var round = new SKPath())
        {
            round.AddCircle(sun.X, sun.Y, sunR);
            c.DrawPath(round, disc);
            c.Save();
            c.ClipPath(round, antialias: true);
            disc.Color = new SKColor(0xFF, 0x9A, 0x3C);
            c.DrawRect(sun.X - sunR, sun.Y - (sunR * 0.45f), sunR * 2, sunR * 0.45f, disc);
            c.Restore();
        }

        using (var cut = new SKPaint { IsAntialias = false, BlendMode = SKBlendMode.Clear })
        {
            var gap = sunR * 0.035f;
            for (var y = sun.Y - (sunR * 0.55f); y < sun.Y; y += sunR * 0.16f)
            {
                c.DrawRect(sun.X - sunR - 1, y, (sunR * 2) + 2, gap, cut);
                gap *= 1.45f;
            }
        }

        c.Restore();
        c.Restore();

        // Flat sea: three rows of scalloped waves, darker towards the viewer.
        var seaColours = new[] { new SKColor(0x2E, 0x6F, 0x7E), new SKColor(0x22, 0x55, 0x66), new SKColor(0x17, 0x3D, 0x4C), new SKColor(0x10, 0x2C, 0x38) };
        using (var sea = new SKPaint { IsAntialias = true, Color = seaColours[0] })
        {
            c.DrawRect(0, horizon, w, h - horizon, sea);
        }

        for (var row = 1; row < seaColours.Length; row++)
        {
            var top = horizon + ((h - horizon) * row / (float)seaColours.Length);
            var bump = s * (0.05f + (row * 0.02f));
            using var wave = new SKPath();
            wave.MoveTo(-bump, h + 2);
            wave.LineTo(-bump, top);
            var phase = row % 2 == 0 ? 0 : bump * 0.5f;
            for (var x = -bump + phase; x < w + bump; x += bump)
            {
                wave.QuadTo(x + (bump * 0.5f), top - (bump * 0.28f), x + bump, top);
            }

            wave.LineTo(w + bump, h + 2);
            wave.Close();
            using var paint = new SKPaint { IsAntialias = true, Color = seaColours[row] };
            c.DrawPath(wave, paint);
        }

        // The sun's reflection: a few flat bars, shortening as they near the viewer.
        using (var glint = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xD2, 0x4A, 220) })
        {
            for (var i = 0; i < 5; i++)
            {
                var y = horizon + (s * 0.012f) + (i * s * 0.022f);
                var half = sunR * (0.85f - (i * 0.16f));
                c.DrawRoundRect(new SKRect(sun.X - half, y, sun.X + half, y + (s * 0.008f)), s * 0.004f, s * 0.004f, glint);
            }
        }

        // One palm leaning in from the left, flat and dark like a print.
        RetroPalm(c, new SKPoint(w * (wide ? 0.09f : 0.1f), h + (s * 0.01f)), new SKPoint(w * (wide ? 0.17f : 0.21f), h * 0.2f), s, new SKColor(0x0E, 0x22, 0x2A));

        // A few gulls, simple ticks.
        using (var gull = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.005f, StrokeCap = SKStrokeCap.Round, Color = new SKColor(0x10, 0x24, 0x30, 220) })
        {
            foreach (var (gx, gy, gs) in new[] { (0.72f, 0.12f, 0.024f), (0.79f, 0.17f, 0.018f), (0.66f, 0.2f, 0.014f) })
            {
                float px = w * gx, py = h * gy, size = s * gs;
                using var wings = new SKPath();
                wings.MoveTo(px - size, py - (size * 0.4f));
                wings.LineTo(px, py);
                wings.LineTo(px + size, py - (size * 0.4f));
                c.DrawPath(wings, gull);
            }
        }

        // Paper grain and a cream border, as on a printed poster.
        using (var grain = new SKPaint { IsAntialias = false })
        {
            for (var i = 0; i < (wide ? 5200 : 4500); i++)
            {
                grain.Color = (i % 2 == 0 ? SKColors.White : SKColors.Black).WithAlpha((byte)rng.Next(10, 26));
                c.DrawRect(R(rng, 0, w), R(rng, 0, h), s * 0.0025f, s * 0.0025f, grain);
            }
        }

        using (var border = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.018f, Color = new SKColor(0xF4, 0xE6, 0xC8, 230) })
        {
            var inset = s * 0.03f;
            c.DrawRect(new SKRect(inset, inset, w - inset, h - inset), border);
        }
    }

    /// <summary>A flat, print-style palm: a curved tapering trunk and a crown of pointed fronds.</summary>
    private static void RetroPalm(SKCanvas c, SKPoint root, SKPoint crown, float s, SKColor colour)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = colour };
        var bendX = (root.X + crown.X) / 2 + (s * 0.08f);
        var bendY = (root.Y + crown.Y) / 2;
        using (var trunk = new SKPath())
        {
            var wBase = s * 0.028f;
            var wTop = s * 0.012f;
            trunk.MoveTo(root.X - wBase, root.Y);
            trunk.QuadTo(bendX - wBase, bendY, crown.X - wTop, crown.Y);
            trunk.LineTo(crown.X + wTop, crown.Y);
            trunk.QuadTo(bendX + wBase, bendY, root.X + wBase, root.Y);
            trunk.Close();
            c.DrawPath(trunk, paint);
        }

        // Fronds: long leaf shapes arching out and drooping at the tips.
        var angles = new[] { -170f, -140f, -110f, -75f, -40f, -10f, 20f };
        foreach (var deg in angles)
        {
            var a = deg * MathF.PI / 180f;
            var len = s * (0.17f + (0.05f * MathF.Cos(a + (MathF.PI / 2))));
            var tip = new SKPoint(crown.X + (MathF.Cos(a) * len), crown.Y + (MathF.Sin(a) * len * 0.6f) + (len * 0.35f));
            var ctrl = new SKPoint(crown.X + (MathF.Cos(a) * len * 0.55f), crown.Y + (MathF.Sin(a) * len * 0.75f) - (len * 0.12f));
            var nx = -(tip.Y - crown.Y);
            var ny = tip.X - crown.X;
            var nl = MathF.Sqrt((nx * nx) + (ny * ny));
            nx = nx / nl * s * 0.022f;
            ny = ny / nl * s * 0.022f;
            using var leaf = new SKPath();
            leaf.MoveTo(crown);
            leaf.QuadTo(ctrl.X + nx, ctrl.Y + ny, tip.X, tip.Y);
            leaf.QuadTo(ctrl.X - nx, ctrl.Y - ny, crown.X, crown.Y);
            leaf.Close();
            c.DrawPath(leaf, paint);
        }

        c.DrawCircle(crown.X, crown.Y + (s * 0.006f), s * 0.016f, paint);
    }
}
