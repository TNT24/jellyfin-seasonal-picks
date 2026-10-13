using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>Pancake Day and Chinese New Year scenes.</summary>
internal static partial class CoverArt
{
    private static readonly SKColor Gold = new(0xFF, 0xC1, 0x07);

    // ---------- Pancake Day: a stack on a plate with syrup, butter and berries, one pancake mid-flip ----------

    private static void PancakeStack(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);

        // Warm kitchen light and a wooden table along the bottom.
        using (var warm = new SKPaint { Shader = SKShader.CreateRadialGradient(new SKPoint(w / 2f, h), s * 0.8f, new[] { new SKColor(0xFF, 0xC8, 0x7A, 90), SKColors.Transparent }, SKShaderTileMode.Clamp) })
        {
            c.DrawRect(0, 0, w, h, warm);
        }

        var tableTop = h * 0.88f;
        using (var wood = new SKPaint { Shader = SKShader.CreateLinearGradient(new SKPoint(0, tableTop), new SKPoint(0, h), new[] { new SKColor(0x8D, 0x5A, 0x34), new SKColor(0x5D, 0x3A, 0x22) }, SKShaderTileMode.Clamp) })
        {
            c.DrawRect(0, tableTop, w, h - tableTop, wood);
        }

        using (var grain = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.002f, Color = new SKColor(0x4A, 0x2E, 0x1A, 120) })
        {
            for (var y = tableTop + (s * 0.02f); y < h; y += s * 0.025f)
            {
                using var line = new SKPath();
                line.MoveTo(0, y);
                for (var x = 0f; x <= w; x += s * 0.05f)
                {
                    line.LineTo(x, y + (float)(Math.Sin((x / s * 9) + y) * s * 0.003f));
                }

                c.DrawPath(line, grain);
            }
        }

        // Plate.
        var cx = w / 2f;
        var plateY = h * 0.93f;
        var plateW = s * 0.56f;
        using (var plate = new SKPaint { IsAntialias = true, Color = new SKColor(0xF5, 0xF3, 0xEE), ImageFilter = SKImageFilter.CreateDropShadow(0, s * 0.008f, s * 0.012f, s * 0.012f, SKColors.Black.WithAlpha(120)) })
        using (var well = new SKPaint { IsAntialias = true, Color = new SKColor(0xE4, 0xE0, 0xD8) })
        {
            c.DrawOval(new SKRect(cx - (plateW / 2), plateY - (s * 0.045f), cx + (plateW / 2), plateY + (s * 0.045f)), plate);
            c.DrawOval(new SKRect(cx - (plateW * 0.36f), plateY - (s * 0.03f), cx + (plateW * 0.36f), plateY + (s * 0.03f)), well);
        }

        // Lemon wedge and berries on the plate, beside the stack.
        LemonWedge(c, cx - (plateW * 0.4f), plateY + (s * 0.005f), s * 0.05f);
        using (var berry = new SKPaint { IsAntialias = true })
        {
            foreach (var (bx, by) in new[] { (0.33f, -0.004f), (0.38f, 0.012f), (0.42f, -0.006f) })
            {
                var br = s * 0.014f;
                using var shade = SKShader.CreateRadialGradient(new SKPoint(cx + (plateW * bx) - (br * 0.3f), plateY + (s * by) - (br * 0.3f)), br * 1.3f, new[] { new SKColor(0x7C, 0x8C, 0xE8), new SKColor(0x30, 0x3F, 0x9F), new SKColor(0x1A, 0x23, 0x7E) }, SKShaderTileMode.Clamp);
                berry.Shader = shade;
                c.DrawCircle(cx + (plateW * bx), plateY + (s * by), br, berry);
            }
        }

        Strawberry(c, cx + (plateW * 0.28f), plateY + (s * 0.012f), s * 0.03f);

        // The stack, bottom pancake first: a browned side band under a golden top.
        var layers = 6;
        var thick = s * 0.026f;
        var baseY = plateY - (s * 0.012f);
        var top = SKRect.Empty;
        for (var i = 0; i < layers; i++)
        {
            var pw = s * (0.3f - (i * 0.006f)) + R(rng, -s * 0.008f, s * 0.008f);
            var px = cx + R(rng, -s * 0.008f, s * 0.008f);
            var y = baseY - (i * thick);
            var ellipseH = s * 0.05f;
            var bottomOval = new SKRect(px - (pw / 2), y - (ellipseH / 2), px + (pw / 2), y + (ellipseH / 2));
            top = new SKRect(bottomOval.Left, bottomOval.Top - thick, bottomOval.Right, bottomOval.Bottom - thick);
            using var side = new SKPath();
            side.AddOval(bottomOval);
            side.AddRect(new SKRect(bottomOval.Left, top.MidY, bottomOval.Right, bottomOval.MidY));
            using var sideMerged = new SKPath();
            side.Simplify(sideMerged);
            using (var edge = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(bottomOval.Left, 0), new SKPoint(bottomOval.Right, 0), new[] { new SKColor(0xB8, 0x6A, 0x22), new SKColor(0xD8, 0x8E, 0x3A), new SKColor(0x9C, 0x55, 0x18) }, new[] { 0f, 0.4f, 1f }, SKShaderTileMode.Clamp) })
            {
                c.DrawPath(sideMerged, edge);
            }

            using var face = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(top.MidX - (pw * 0.15f), top.MidY - (ellipseH * 0.2f)), pw * 0.6f, new[] { new SKColor(0xF6, 0xC8, 0x6A), new SKColor(0xE2, 0xA2, 0x48), new SKColor(0xC8, 0x80, 0x30) }, new[] { 0f, 0.7f, 1f }, SKShaderTileMode.Clamp) };
            c.DrawOval(top, face);
        }

        // Syrup pooled on top and running down the sides in drips.
        var syrupColour = new SKColor(0x8A, 0x3C, 0x0E, 235);
        using (var syrup = new SKPaint { IsAntialias = true, Color = syrupColour })
        using (var gloss = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(110) })
        {
            var pool = new SKRect(top.Left + (top.Width * 0.12f), top.Top + (top.Height * 0.12f), top.Right - (top.Width * 0.1f), top.Bottom - (top.Height * 0.08f));
            c.DrawOval(pool, syrup);
            foreach (var (fx, len) in new[] { (0.16f, 0.09f), (0.3f, 0.05f), (0.62f, 0.11f), (0.8f, 0.06f), (0.9f, 0.08f) })
            {
                var dx = pool.Left + (pool.Width * fx);
                var dw = s * 0.016f;
                var dropTop = pool.MidY + (float)(Math.Sqrt(Math.Max(0, 1 - Math.Pow(((dx - pool.MidX) / (pool.Width / 2)), 2))) * pool.Height / 2) - (s * 0.004f);
                c.DrawRoundRect(new SKRect(dx - (dw / 2), dropTop - (s * 0.005f), dx + (dw / 2), dropTop + (s * len)), dw / 2, dw / 2, syrup);
                c.DrawCircle(dx, dropTop + (s * len), dw * 0.62f, syrup);
            }

            c.DrawOval(new SKRect(pool.Left + (pool.Width * 0.2f), pool.Top + (pool.Height * 0.2f), pool.Left + (pool.Width * 0.45f), pool.Top + (pool.Height * 0.38f)), gloss);
        }

        // A pat of butter, just starting to melt.
        var butter = new SKRect(top.MidX - (s * 0.032f), top.MidY - (s * 0.04f), top.MidX + (s * 0.032f), top.MidY - (s * 0.006f));
        using (var melt = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xE0, 0x82, 200) })
        {
            c.DrawOval(new SKRect(butter.Left - (s * 0.012f), butter.Bottom - (s * 0.01f), butter.Right + (s * 0.012f), butter.Bottom + (s * 0.012f)), melt);
        }

        using (var pat = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, butter.Top), new SKPoint(0, butter.Bottom), new[] { new SKColor(0xFF, 0xF3, 0xB0), new SKColor(0xFF, 0xD5, 0x4F) }, SKShaderTileMode.Clamp) })
        {
            c.DrawRoundRect(butter, s * 0.006f, s * 0.006f, pat);
        }

        // One pancake flipping high above the stack, off to the side, with a couple of motion arcs.
        var flip = new SKPoint(w - (s * 0.2f), h * 0.17f);
        c.Save();
        c.Translate(flip.X, flip.Y);
        c.RotateDegrees(-25);
        using (var edge = new SKPaint { IsAntialias = true, Color = new SKColor(0xB8, 0x6A, 0x22) })
        using (var face = new SKPaint { IsAntialias = true, Color = new SKColor(0xEE, 0xB4, 0x58) })
        {
            c.DrawOval(new SKRect(-s * 0.1f, -s * 0.022f, s * 0.1f, s * 0.03f), edge);
            c.DrawOval(new SKRect(-s * 0.1f, -s * 0.03f, s * 0.1f, s * 0.022f), face);
        }

        c.Restore();
        using (var motion = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = s * 0.006f, Color = SKColors.White.WithAlpha(140) })
        {
            c.DrawArc(new SKRect(flip.X - (s * 0.17f), flip.Y - (s * 0.06f), flip.X - (s * 0.05f), flip.Y + (s * 0.14f)), 200, 60, false, motion);
            c.DrawArc(new SKRect(flip.X + (s * 0.05f), flip.Y - (s * 0.12f), flip.X + (s * 0.17f), flip.Y + (s * 0.06f)), -60, 60, false, motion);
        }

        // Sugar sparkles drifting in the top band.
        using var sugar = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(200) };
        for (var i = 0; i < 30; i++)
        {
            c.DrawCircle(R(rng, 0, w * 0.7f), R(rng, h * 0.04f, h * 0.28f), R(rng, s * 0.002f, s * 0.004f), sugar);
        }
    }

    private static void LemonWedge(SKCanvas c, float x, float y, float r)
    {
        var rect = new SKRect(x - r, y - r, x + r, y + r);
        using var rind = new SKPaint { IsAntialias = true, Color = new SKColor(0xFB, 0xC0, 0x2D) };
        using var flesh = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xF5, 0x9D) };
        using var segment = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = r * 0.06f, Color = new SKColor(0xFF, 0xFD, 0xE7) };
        c.DrawArc(rect, 180, 180, true, rind);
        var inner = new SKRect(x - (r * 0.85f), y - (r * 0.85f), x + (r * 0.85f), y + (r * 0.85f));
        c.DrawArc(inner, 180, 180, true, flesh);
        foreach (var a in new[] { 216f, 252f, 288f, 324f })
        {
            var rad = a * Math.PI / 180;
            c.DrawLine(x, y, x + (float)(Math.Cos(rad) * r * 0.8f), y + (float)(Math.Sin(rad) * r * 0.8f), segment);
        }
    }

    private static void Strawberry(SKCanvas c, float x, float y, float r)
    {
        using var berry = new SKPath();
        berry.MoveTo(x, y + r);
        berry.CubicTo(x - (r * 1.1f), y + (r * 0.2f), x - (r * 0.9f), y - (r * 0.8f), x, y - (r * 0.7f));
        berry.CubicTo(x + (r * 0.9f), y - (r * 0.8f), x + (r * 1.1f), y + (r * 0.2f), x, y + r);
        berry.Close();
        using var red = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(x - (r * 0.3f), y - (r * 0.3f)), r * 1.4f, new[] { new SKColor(0xFF, 0x52, 0x52), new SKColor(0xC6, 0x28, 0x28) }, SKShaderTileMode.Clamp) };
        c.DrawPath(berry, red);
        using var seed = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xE0, 0x82) };
        foreach (var (sx, sy) in new[] { (-0.35f, -0.2f), (0.3f, -0.25f), (0f, 0.05f), (-0.3f, 0.3f), (0.3f, 0.3f), (0f, 0.55f) })
        {
            c.DrawOval(new SKRect(x + (r * sx) - (r * 0.05f), y + (r * sy) - (r * 0.08f), x + (r * sx) + (r * 0.05f), y + (r * sy) + (r * 0.08f)), seed);
        }

        using var leaf = new SKPaint { IsAntialias = true, Color = new SKColor(0x43, 0xA0, 0x47) };
        foreach (var a in new[] { -50f, 0f, 50f })
        {
            c.Save();
            c.RotateDegrees(a, x, y - (r * 0.7f));
            c.DrawOval(new SKRect(x - (r * 0.12f), y - (r * 1.15f), x + (r * 0.12f), y - (r * 0.65f)), leaf);
            c.Restore();
        }
    }

    // ---------- Chinese New Year, week 1: red lanterns, plum blossom and red envelopes ----------

    private static void RedLanterns(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        NightSky(c, w, h, new SKColor(0x3A, 0x05, 0x08, 150), new SKColor(0x20, 0x04, 0x06, 90));

        // Two cords draped across the top with lanterns hung along them, alternating long and short strings.
        for (var k = 0; k < 2; k++)
        {
            var y0 = h * (0.02f + (k * 0.05f));
            var sag = h * 0.05f;
            SKPoint At(float t) => new((-w * 0.05f) + (t * w * 1.1f), y0 + (4 * sag * t * (1 - t)));

            using (var cord = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.004f, Color = new SKColor(0xB7, 0x1C, 0x1C) })
            using (var line = new SKPath())
            {
                line.MoveTo(At(0));
                for (var t = 0.02f; t <= 1.001f; t += 0.02f)
                {
                    line.LineTo(At(t));
                }

                c.DrawPath(line, cord);
            }

            var count = w > h ? 5 : 3;
            for (var i = 0; i < count; i++)
            {
                var hook = At((i + 0.5f + (k * 0.5f)) / (count + 0.5f));
                var size = s * (k == 0 ? 0.062f : 0.05f);
                var drop = s * (k == 0 ? 0.05f : 0.035f);
                Lantern(c, hook.X, hook.Y, drop, size, s);
            }
        }

        // Plum blossom from the bottom-left corner, red envelopes and gold coins at the bottom right.
        PlumBranch(c, 0, h, s, rng);
        RedEnvelope(c, w - (s * 0.2f), h * 0.97f, s * 0.13f, -12);
        RedEnvelope(c, w - (s * 0.09f), h * 0.985f, s * 0.12f, 10);
        foreach (var (gx, gy) in new[] { (0.3f, 0.965f), (0.36f, 0.975f) })
        {
            Coin(c, w - (s * gx), h * gy, s * 0.022f);
        }

        using var gold = new SKPaint { IsAntialias = true };
        for (var i = 0; i < 40; i++)
        {
            float gx = R(rng, 0, w), gy = R(rng, 0, h);
            if (Math.Abs(gx - (w / 2f)) < w * 0.46f && gy > h * 0.3f && gy < h * 0.66f)
            {
                continue;
            }

            gold.Color = Gold.WithAlpha((byte)rng.Next(100, 210));
            c.DrawCircle(gx, gy, R(rng, s * 0.002f, s * 0.005f), gold);
        }
    }

    /// <summary>A round red paper lantern hanging on a string from (x, hookY), with gold caps and a tassel.</summary>
    private static void Lantern(SKCanvas c, float x, float hookY, float drop, float r, float s)
    {
        using (var str = new SKPaint { IsAntialias = true, StrokeWidth = s * 0.003f, Color = Gold })
        {
            c.DrawLine(x, hookY, x, hookY + drop, str);
        }

        var cy = hookY + drop + (r * 0.85f);
        using (var glow = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(x, cy), r * 2, new[] { new SKColor(0xFF, 0x70, 0x40, 90), SKColors.Transparent }, SKShaderTileMode.Clamp) })
        {
            c.DrawCircle(x, cy, r * 2, glow);
        }

        var body = new SKRect(x - r, cy - (r * 0.78f), x + r, cy + (r * 0.78f));
        using (var paper = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(x - (r * 0.2f), cy - (r * 0.15f)), r * 1.2f, new[] { new SKColor(0xFF, 0x8A, 0x65), new SKColor(0xE5, 0x39, 0x35), new SKColor(0x9A, 0x10, 0x10) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp) })
        {
            c.DrawOval(body, paper);
        }

        // Ribs: arcs from cap to cap, each inside the outline, none crossing.
        using (var rib = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = r * 0.03f, Color = new SKColor(0x8B, 0x0E, 0x0E, 170) })
        {
            foreach (var f in new[] { 0.35f, 0.7f })
            {
                c.DrawOval(new SKRect(x - (r * f), body.Top, x + (r * f), body.Bottom), rib);
            }

            c.DrawLine(x, body.Top, x, body.Bottom, rib);
        }

        using var gold = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(x - (r * 0.5f), 0), new SKPoint(x + (r * 0.5f), 0), new[] { new SKColor(0xFF, 0xE0, 0x82), new SKColor(0xC8, 0x8A, 0x10) }, SKShaderTileMode.Clamp) };
        c.DrawRoundRect(new SKRect(x - (r * 0.45f), body.Top - (r * 0.1f), x + (r * 0.45f), body.Top + (r * 0.08f)), r * 0.04f, r * 0.04f, gold);
        c.DrawRoundRect(new SKRect(x - (r * 0.45f), body.Bottom - (r * 0.08f), x + (r * 0.45f), body.Bottom + (r * 0.1f)), r * 0.04f, r * 0.04f, gold);

        // Tassel: a knot, then threads fanning slightly out from it.
        var knotY = body.Bottom + (r * 0.22f);
        c.DrawCircle(x, knotY, r * 0.08f, gold);
        using var thread = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = r * 0.035f, Color = new SKColor(0xE5, 0x39, 0x35) };
        c.DrawLine(x, body.Bottom + (r * 0.1f), x, knotY, thread);
        for (var t = -3; t <= 3; t++)
        {
            c.DrawLine(x + (t * r * 0.02f), knotY + (r * 0.06f), x + (t * r * 0.06f), knotY + (r * 0.6f), thread);
        }
    }

    /// <summary>A dark plum branch with pink and white blossoms, reaching in from the bottom-left corner.</summary>
    private static void PlumBranch(SKCanvas c, float x, float y, float s, Random rng)
    {
        using var bark = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, Color = new SKColor(0x3E, 0x27, 0x23) };
        using var main = new SKPath();
        main.MoveTo(x - (s * 0.02f), y - (s * 0.05f));
        main.CubicTo(x + (s * 0.1f), y - (s * 0.1f), x + (s * 0.14f), y - (s * 0.22f), x + (s * 0.28f), y - (s * 0.26f));
        bark.StrokeWidth = s * 0.016f;
        c.DrawPath(main, bark);
        bark.StrokeWidth = s * 0.008f;
        c.DrawLine(x + (s * 0.12f), y - (s * 0.16f), x + (s * 0.24f), y - (s * 0.14f), bark);
        c.DrawLine(x + (s * 0.08f), y - (s * 0.1f), x + (s * 0.06f), y - (s * 0.22f), bark);

        var colours = new[] { new SKColor(0xF4, 0x8F, 0xB1), new SKColor(0xFF, 0xFF, 0xFF), new SKColor(0xEC, 0x40, 0x7A) };
        foreach (var (bx, by) in new[] { (0.05f, 0.08f), (0.12f, 0.15f), (0.17f, 0.21f), (0.23f, 0.25f), (0.28f, 0.27f), (0.2f, 0.14f), (0.25f, 0.14f), (0.06f, 0.2f), (0.1f, 0.11f) })
        {
            Blossom(c, x + (s * bx), y - (s * by), s * R(rng, 0.018f, 0.026f), colours[rng.Next(colours.Length)]);
        }
    }

    /// <summary>A red envelope (hongbao) leaning at an angle, with a gold flap edge and a gold medallion.</summary>
    private static void RedEnvelope(SKCanvas c, float x, float bottom, float height, float tilt)
    {
        var ew = height * 0.62f;
        c.Save();
        c.Translate(x, bottom);
        c.RotateDegrees(tilt);
        var rect = new SKRect(-ew / 2, -height, ew / 2, 0);
        using (var red = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(rect.Left, 0), new SKPoint(rect.Right, 0), new[] { new SKColor(0xE5, 0x39, 0x35), new SKColor(0xB7, 0x1C, 0x1C) }, SKShaderTileMode.Clamp), ImageFilter = SKImageFilter.CreateDropShadow(0, height * 0.03f, height * 0.04f, height * 0.04f, SKColors.Black.WithAlpha(130)) })
        {
            c.DrawRoundRect(rect, height * 0.04f, height * 0.04f, red);
        }

        using var gold = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = height * 0.02f, Color = Gold };
        using var flap = new SKPath();
        flap.MoveTo(rect.Left, rect.Top + (height * 0.22f));
        flap.QuadTo(0, rect.Top + (height * 0.4f), rect.Right, rect.Top + (height * 0.22f));
        c.DrawPath(flap, gold);
        using var medal = new SKPaint { IsAntialias = true, Color = Gold };
        c.DrawCircle(0, rect.Top + (height * 0.36f), ew * 0.16f, medal);
        using var hole = new SKPaint { IsAntialias = true, Color = new SKColor(0xB7, 0x1C, 0x1C) };
        c.DrawRect(-ew * 0.05f, rect.Top + (height * 0.36f) - (ew * 0.05f), ew * 0.1f, ew * 0.1f, hole);
        c.Restore();
    }

    /// <summary>A round gold coin with a square hole, lying on the ground.</summary>
    private static void Coin(SKCanvas c, float x, float y, float r)
    {
        using var gold = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(x - (r * 0.3f), y - (r * 0.3f)), r * 1.3f, new[] { new SKColor(0xFF, 0xF1, 0x76), Gold, new SKColor(0xB8, 0x86, 0x0B) }, new[] { 0f, 0.55f, 1f }, SKShaderTileMode.Clamp) };
        c.DrawOval(new SKRect(x - r, y - (r * 0.6f), x + r, y + (r * 0.6f)), gold);
        using var hole = new SKPaint { IsAntialias = true, Color = new SKColor(0x6D, 0x4C, 0x0B) };
        c.DrawRect(x - (r * 0.22f), y - (r * 0.14f), r * 0.44f, r * 0.28f, hole);
    }

    // ---------- Chinese New Year, week 2: sky lanterns over a pagoda and a moon bridge ----------

    private static void LanternFestival(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        NightSky(c, w, h, new SKColor(0x0A, 0x0E, 0x2E, 180), new SKColor(0x3A, 0x14, 0x30, 100));
        Stars(c, w, h * 0.5f, 80, s, rng);
        Moon(c, w * 0.15f, h * 0.17f, s * 0.08f, rng);

        // Sky lanterns, big near ones and small far ones, all in the sky above the title.
        var placed = new List<(float X, float Y, float R)> { (w * 0.15f, h * 0.17f, s * 0.1f) };
        for (var i = 0; i < (w > h ? 18 : 13); i++)
        {
            var size = i < 5 ? s * R(rng, 0.035f, 0.05f) : s * R(rng, 0.012f, 0.025f);
            if (FindClear(rng, placed, size * 1.4f, size, w - size, size * 2, (h * 0.3f) - (size * 1.5f), 1.1f) is { } at)
            {
                SkyLantern(c, at.X, at.Y, size);
                placed.Add((at.X, at.Y, size * 1.4f));
            }
        }

        // Far shore, the pagoda on the right and the moon bridge on the left, then the water and reflections.
        var shore = h * 0.88f;
        using (var land = new SKPaint { IsAntialias = true, Color = new SKColor(0x10, 0x0C, 0x20) })
        {
            c.DrawPath(Hills(w, h, shore - (h * 0.02f), h * 0.025f, 3, rng), land);
        }

        Pagoda(c, w - (s * 0.2f), shore, s * 0.25f);
        MoonBridge(c, s * 0.26f, shore, s * 0.34f);

        using (var water = new SKPaint { Shader = SKShader.CreateLinearGradient(new SKPoint(0, shore), new SKPoint(0, h), new[] { new SKColor(0x14, 0x12, 0x34, 240), new SKColor(0x08, 0x08, 0x1C, 250) }, SKShaderTileMode.Clamp) })
        {
            c.DrawRect(0, shore, w, h - shore, water);
        }

        // The bridge's reflection completes its circle; lantern glows shimmer in the water.
        c.Save();
        c.ClipRect(new SKRect(0, shore, w, h));
        using (var fade = new SKPaint { Color = SKColors.White.WithAlpha(70) })
        {
            c.SaveLayer(fade);
            c.Scale(1, -1, 0, shore);
            MoonBridge(c, s * 0.26f, shore, s * 0.34f);
            Pagoda(c, w - (s * 0.2f), shore, s * 0.25f);
            c.Restore();
        }

        using (var shimmer = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = s * 0.004f, Color = new SKColor(0xFF, 0xB0, 0x60, 120) })
        {
            for (var i = 0; i < 26; i++)
            {
                var x = R(rng, 0, w);
                var y = R(rng, shore + (s * 0.01f), h);
                c.DrawLine(x, y, x + (s * R(rng, 0.015f, 0.04f)), y, shimmer);
            }
        }

        c.Restore();
    }

    /// <summary>A glowing paper sky lantern, wider at the top, rising; (x, y) is its middle.</summary>
    private static void SkyLantern(SKCanvas c, float x, float y, float r)
    {
        using (var halo = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(x, y), r * 2.4f, new[] { new SKColor(0xFF, 0xA0, 0x40, 110), SKColors.Transparent }, SKShaderTileMode.Clamp) })
        {
            c.DrawCircle(x, y, r * 2.4f, halo);
        }

        using var body = new SKPath();
        body.MoveTo(x - (r * 0.8f), y - (r * 1.1f));
        body.LineTo(x + (r * 0.8f), y - (r * 1.1f));
        body.QuadTo(x + (r * 0.75f), y + (r * 0.3f), x + (r * 0.5f), y + (r * 1.0f));
        body.LineTo(x - (r * 0.5f), y + (r * 1.0f));
        body.QuadTo(x - (r * 0.75f), y + (r * 0.3f), x - (r * 0.8f), y - (r * 1.1f));
        body.Close();
        using var paper = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, y - (r * 1.1f)), new SKPoint(0, y + r), new[] { new SKColor(0xFF, 0xA7, 0x26), new SKColor(0xFF, 0xE0, 0x82) }, SKShaderTileMode.Clamp) };
        c.DrawPath(body, paper);
        using var flame = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(220) };
        c.DrawOval(new SKRect(x - (r * 0.18f), y + (r * 0.55f), x + (r * 0.18f), y + (r * 0.95f)), flame);
    }

    /// <summary>A five-storey pagoda in silhouette with upturned eaves and lit windows; (x, ground) is the middle of its base.</summary>
    private static void Pagoda(SKCanvas c, float x, float ground, float height)
    {
        using var dark = new SKPaint { IsAntialias = true, Color = new SKColor(0x1A, 0x10, 0x2A) };
        using var lit = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xC8, 0x6A) };
        const int Storeys = 5;
        var storey = height * 0.16f;
        var y = ground;
        for (var i = 0; i < Storeys; i++)
        {
            var half = height * (0.2f - (i * 0.028f));
            c.DrawRect(x - (half * 0.62f), y - storey, half * 1.24f, storey, dark);
            c.DrawRoundRect(new SKRect(x - (half * 0.16f), y - (storey * 0.75f), x + (half * 0.16f), y - (storey * 0.2f)), half * 0.05f, half * 0.05f, lit);

            // Roof with its ends sweeping up.
            using var roof = new SKPath();
            var eave = y - storey;
            roof.MoveTo(x - (half * 1.15f), eave - (storey * 0.35f));
            roof.QuadTo(x - (half * 0.8f), eave + (storey * 0.05f), x - (half * 0.4f), eave - (storey * 0.12f));
            roof.LineTo(x, eave - (storey * 0.45f));
            roof.LineTo(x + (half * 0.4f), eave - (storey * 0.12f));
            roof.QuadTo(x + (half * 0.8f), eave + (storey * 0.05f), x + (half * 1.15f), eave - (storey * 0.35f));
            roof.LineTo(x + (half * 0.5f), eave - (storey * 0.3f));
            roof.LineTo(x, eave - (storey * 0.6f));
            roof.LineTo(x - (half * 0.5f), eave - (storey * 0.3f));
            roof.Close();
            c.DrawPath(roof, dark);
            y -= storey + (storey * 0.25f);
        }

        using var spire = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = height * 0.012f, Color = dark.Color };
        c.DrawLine(x, y + (storey * 0.2f), x, y - (storey * 0.5f), spire);
    }

    /// <summary>An arched moon bridge with a railing; (x, ground) is the middle of its span at the water.</summary>
    private static void MoonBridge(SKCanvas c, float x, float ground, float span)
    {
        var r = span / 2;
        using var stone = new SKPaint { IsAntialias = true, Color = new SKColor(0x22, 0x18, 0x34) };
        using var deck = new SKPath();
        deck.MoveTo(x - r - (span * 0.08f), ground);
        deck.QuadTo(x, ground - (r * 2.1f), x + r + (span * 0.08f), ground);
        deck.LineTo(x + r, ground);
        deck.ArcTo(new SKRect(x - r * 0.82f, ground - (r * 0.82f), x + r * 0.82f, ground + (r * 0.82f)), 0, -180, false);
        deck.LineTo(x - r - (span * 0.08f), ground);
        deck.Close();
        c.DrawPath(deck, stone);

        // Railing posts standing on the deck's curve.
        using var post = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = span * 0.012f, Color = stone.Color };
        using var rail = new SKPath();
        for (var i = 1; i < 8; i++)
        {
            var t = i / 8f;
            var px = (1 - t) * (1 - t) * (x - r - (span * 0.08f)) + (2 * (1 - t) * t * x) + (t * t * (x + r + (span * 0.08f)));
            var py = (1 - t) * (1 - t) * ground + (2 * (1 - t) * t * (ground - (r * 2.1f))) + (t * t * ground);
            c.DrawLine(px, py, px, py - (span * 0.06f), post);
            if (i == 1)
            {
                rail.MoveTo(px, py - (span * 0.06f));
            }
            else
            {
                rail.LineTo(px, py - (span * 0.06f));
            }
        }

        post.Style = SKPaintStyle.Stroke;
        c.DrawPath(rail, post);
    }
}
