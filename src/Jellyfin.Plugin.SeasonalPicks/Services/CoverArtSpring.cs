using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>The Easter and Valentine's scenes the weeks of the season switch between after the first.</summary>
internal static partial class CoverArt
{
    // ---------- Easter: a basket of eggs and chicks under a blossom branch ----------

    private static void ChickBasket(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var pastels = new[]
        {
            new SKColor(0xF8, 0xB1, 0xC8), new SKColor(0xB5, 0xE3, 0xF5), new SKColor(0xFF, 0xE5, 0x8F),
            new SKColor(0xC9, 0xB6, 0xF2), new SKColor(0xB8, 0xEB, 0xB0), new SKColor(0xFF, 0xC8, 0x9E)
        };

        // Warm spring light from the top right.
        using (var sun = new SKPaint { Shader = SKShader.CreateRadialGradient(new SKPoint(w * 0.9f, 0), s * 0.8f, new[] { new SKColor(0xFF, 0xF3, 0xC4, 110), SKColors.Transparent }, SKShaderTileMode.Clamp) })
        {
            c.DrawRect(0, 0, w, h, sun);
        }

        BlossomBranch(c, 0, s * 0.05f, s, false, rng);
        BlossomBranch(c, w, s * 0.12f, s * 0.75f, true, rng);
        Butterfly(c, w * 0.53f, h * 0.24f, s * 0.035f, 20, new SKColor(0xFF, 0xB3, 0x00));
        Butterfly(c, w * 0.32f, h * 0.27f, s * 0.028f, -15, new SKColor(0x81, 0xD4, 0xFA));

        using var back = new SKPaint { IsAntialias = true, Color = new SKColor(0x4C, 0x9A, 0x3C) };
        using var front = new SKPaint { IsAntialias = true, Color = new SKColor(0x6C, 0xC0, 0x4A) };
        Grass(c, w, h, h * 0.88f, s * 0.07f, back, rng);

        // Tulips along the left, a chick by its broken shell, then the basket.
        var tulips = new[] { new SKColor(0xE5, 0x39, 0x35), new SKColor(0xFF, 0xB3, 0x00), new SKColor(0xEC, 0x40, 0x7A), new SKColor(0xAB, 0x47, 0xBC) };
        var tulipCount = w > h ? 5 : 3;
        for (var i = 0; i < tulipCount; i++)
        {
            Tulip(c, w * (0.04f + (i * 0.055f)) + R(rng, -s * 0.01f, s * 0.01f), h * 0.93f, s * R(rng, 0.14f, 0.19f), tulips[i % tulips.Length], R(rng, -8, 8));
        }

        var basketX = w * (w > h ? 0.68f : 0.64f);
        Basket(c, basketX, h * 0.96f, s * 0.34f, pastels, rng);
        var chickX = w * (w > h ? 0.4f : 0.3f);
        Chick(c, chickX, h * 0.955f, s * 0.075f, false);
        CrackedShell(c, chickX + (s * 0.1f), h * 0.96f, s * 0.05f, pastels[3]);

        Grass(c, w, h, h * 0.965f, s * 0.03f, front, rng);
    }

    /// <summary>A flowering branch reaching in from a top corner.</summary>
    private static void BlossomBranch(SKCanvas c, float x, float y, float size, bool mirror, Random rng)
    {
        c.Save();
        c.Translate(x, y);
        c.Scale(mirror ? -1 : 1, 1);

        using var bark = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, Color = new SKColor(0x5D, 0x40, 0x37) };
        using var branch = new SKPath();
        branch.MoveTo(-size * 0.02f, 0);
        branch.CubicTo(size * 0.15f, -size * 0.03f, size * 0.25f, size * 0.12f, size * 0.42f, size * 0.08f);
        bark.StrokeWidth = size * 0.014f;
        c.DrawPath(branch, bark);

        // Twigs off the main branch.
        bark.StrokeWidth = size * 0.007f;
        var twigs = new[] { (0.12f, 0.0f, 0.2f, -0.05f), (0.22f, 0.07f, 0.26f, 0.16f), (0.32f, 0.1f, 0.4f, 0.0f) };
        foreach (var (x0, y0, x1, y1) in twigs)
        {
            c.DrawLine(size * x0, size * y0, size * x1, size * y1, bark);
        }

        var pinks = new[] { new SKColor(0xF8, 0xBB, 0xD0), new SKColor(0xFF, 0xFF, 0xFF), new SKColor(0xF4, 0x8F, 0xB1) };
        foreach (var (bx, by) in new[] { (0.06f, 0.0f), (0.15f, 0.02f), (0.2f, -0.05f), (0.25f, 0.1f), (0.26f, 0.16f), (0.33f, 0.09f), (0.4f, 0.0f), (0.42f, 0.08f), (0.1f, -0.03f) })
        {
            Blossom(c, size * (bx + R(rng, -0.01f, 0.01f)), size * (by + R(rng, -0.01f, 0.01f)), size * R(rng, 0.022f, 0.032f), pinks[rng.Next(pinks.Length)]);
        }

        using var bud = new SKPaint { IsAntialias = true, Color = new SKColor(0xEC, 0x40, 0x7A) };
        foreach (var (bx, by) in new[] { (0.29f, 0.05f), (0.37f, 0.12f), (0.18f, 0.06f) })
        {
            c.DrawOval(new SKRect(size * (bx - 0.007f), size * (by - 0.011f), size * (bx + 0.007f), size * (by + 0.011f)), bud);
        }

        c.Restore();
    }

    private static void Butterfly(SKCanvas c, float x, float y, float r, float rotation, SKColor colour)
    {
        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(rotation);
        using var wing = new SKPaint { IsAntialias = true, Color = colour.WithAlpha(230) };
        using var spot = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(200) };
        foreach (var side in new[] { -1f, 1f })
        {
            c.DrawOval(new SKRect(side < 0 ? -r * 1.1f : 0, -r * 0.9f, side < 0 ? 0 : r * 1.1f, r * 0.1f), wing);
            c.DrawOval(new SKRect(side < 0 ? -r * 0.75f : 0, -r * 0.05f, side < 0 ? 0 : r * 0.75f, r * 0.7f), wing);
            c.DrawCircle(side * r * 0.55f, -r * 0.45f, r * 0.13f, spot);
        }

        using var body = new SKPaint { IsAntialias = true, Color = new SKColor(0x3E, 0x27, 0x23) };
        c.DrawRoundRect(new SKRect(-r * 0.08f, -r * 0.7f, r * 0.08f, r * 0.6f), r * 0.08f, r * 0.08f, body);
        using var antenna = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = r * 0.04f, Color = body.Color };
        c.DrawLine(0, -r * 0.65f, -r * 0.3f, -r * 1.05f, antenna);
        c.DrawLine(0, -r * 0.65f, r * 0.3f, -r * 1.05f, antenna);
        c.Restore();
    }

    private static void Tulip(SKCanvas c, float x, float ground, float height, SKColor colour, float lean)
    {
        c.Save();
        c.Translate(x, ground);
        c.RotateDegrees(lean);
        using var stem = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = height * 0.035f, Color = new SKColor(0x38, 0x8E, 0x3C) };
        c.DrawLine(0, 0, 0, -height * 0.78f, stem);
        using var leaf = new SKPaint { IsAntialias = true, Color = new SKColor(0x43, 0xA0, 0x47) };
        using (var l = new SKPath())
        {
            l.MoveTo(0, 0);
            l.QuadTo(height * 0.18f, -height * 0.25f, height * 0.08f, -height * 0.5f);
            l.QuadTo(height * 0.02f, -height * 0.25f, 0, 0);
            c.DrawPath(l, leaf);
        }

        var cup = height * 0.13f;
        var top = -height * 0.78f;
        using var petals = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, top - (cup * 1.5f)), new SKPoint(0, top + (cup * 0.3f)), new[] { Lighten(colour, 0.25f), Darken(colour, 0.75f) }, SKShaderTileMode.Clamp) };
        using var head = new SKPath();
        head.MoveTo(-cup, top - (cup * 1.3f));
        head.LineTo(-cup * 0.45f, top - (cup * 0.85f));
        head.LineTo(0, top - (cup * 1.45f));
        head.LineTo(cup * 0.45f, top - (cup * 0.85f));
        head.LineTo(cup, top - (cup * 1.3f));
        head.CubicTo(cup * 1.1f, top - (cup * 0.2f), cup * 0.6f, top + (cup * 0.2f), 0, top + (cup * 0.2f));
        head.CubicTo(-cup * 0.6f, top + (cup * 0.2f), -cup * 1.1f, top - (cup * 0.2f), -cup, top - (cup * 1.3f));
        head.Close();
        c.DrawPath(head, petals);
        c.Restore();
    }

    /// <summary>A fluffy chick sitting at (x, ground), facing left (or right when <paramref name="flip"/>); u is its height.</summary>
    private static void Chick(SKCanvas c, float x, float ground, float u, bool flip)
    {
        c.Save();
        c.Translate(x, ground);
        c.Scale(flip ? -1 : 1, 1);

        using var shadow = SKImageFilter.CreateDropShadow(0, u * 0.03f, u * 0.05f, u * 0.05f, SKColors.Black.WithAlpha(80));
        using var fluff = new SKPaint
        {
            IsAntialias = true,
            ImageFilter = shadow,
            Shader = SKShader.CreateRadialGradient(new SKPoint(-u * 0.2f, -u * 0.8f), u * 1.1f, new[] { new SKColor(0xFF, 0xF1, 0x76), new SKColor(0xFF, 0xD5, 0x4F), new SKColor(0xF9, 0xA8, 0x25) }, new[] { 0f, 0.6f, 1f }, SKShaderTileMode.Clamp)
        };

        using var feet = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = u * 0.05f, Color = new SKColor(0xFF, 0x8F, 0x00) };
        c.DrawLine(-u * 0.12f, -u * 0.05f, -u * 0.2f, 0, feet);
        c.DrawLine(u * 0.08f, -u * 0.05f, 0, 0, feet);

        c.DrawOval(new SKRect(-u * 0.42f, -u * 0.62f, u * 0.4f, -u * 0.02f), fluff);
        c.DrawCircle(-u * 0.12f, -u * 0.72f, u * 0.27f, fluff);

        // Tuft on top.
        using (var tuft = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = u * 0.04f, Color = new SKColor(0xF9, 0xA8, 0x25) })
        {
            c.DrawLine(-u * 0.12f, -u * 0.98f, -u * 0.16f, -u * 1.08f, tuft);
            c.DrawLine(-u * 0.08f, -u * 0.98f, -u * 0.04f, -u * 1.07f, tuft);
        }

        // Wing.
        using (var wing = new SKPaint { IsAntialias = true, Color = new SKColor(0xFB, 0xC0, 0x2D) })
        {
            c.Save();
            c.RotateDegrees(-20, u * 0.12f, -u * 0.32f);
            c.DrawOval(new SKRect(-u * 0.02f, -u * 0.44f, u * 0.3f, -u * 0.2f), wing);
            c.Restore();
        }

        using var beak = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0x8F, 0x00) };
        using (var b = new SKPath())
        {
            b.MoveTo(-u * 0.36f, -u * 0.76f);
            b.LineTo(-u * 0.52f, -u * 0.7f);
            b.LineTo(-u * 0.36f, -u * 0.64f);
            b.Close();
            c.DrawPath(b, beak);
        }

        using var eye = new SKPaint { IsAntialias = true, Color = new SKColor(0x2B, 0x1D, 0x16) };
        using var shine = new SKPaint { IsAntialias = true, Color = SKColors.White };
        c.DrawCircle(-u * 0.24f, -u * 0.8f, u * 0.045f, eye);
        c.DrawCircle(-u * 0.255f, -u * 0.815f, u * 0.015f, shine);
        using var blush = new SKPaint { IsAntialias = true, Color = new SKColor(0xF4, 0x8F, 0xB1, 110) };
        c.DrawCircle(-u * 0.2f, -u * 0.65f, u * 0.05f, blush);
        c.Restore();
    }

    /// <summary>The bottom half of a hatched egg with a zigzag edge.</summary>
    private static void CrackedShell(SKCanvas c, float x, float ground, float width, SKColor colour)
    {
        var height = width * 0.8f;
        using var shell = new SKPath();
        shell.MoveTo(x - width, ground - height);
        for (var k = 1; k <= 6; k++)
        {
            shell.LineTo(x - width + (k * width / 3), ground - height - (k % 2 == 1 ? width * 0.25f : 0));
        }

        shell.ArcTo(new SKRect(x - width, ground - (height * 2), x + width, ground), 0, 180, false);
        shell.Close();
        using var paint = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(x - width, 0), new SKPoint(x + width, 0), new[] { Lighten(colour, 0.3f), colour }, SKShaderTileMode.Clamp) };
        c.DrawPath(shell, paint);
    }

    /// <summary>A wicker basket full of eggs, with a chick sitting on top. (x, ground) is the middle of its base; bw its width.</summary>
    private static void Basket(SKCanvas c, float x, float ground, float bw, SKColor[] eggColours, Random rng)
    {
        var rimY = ground - (bw * 0.38f);
        var wicker = new SKColor(0xC6, 0x8A, 0x4A);
        var dark = new SKColor(0x8D, 0x5A, 0x2B);

        // Handle behind everything.
        using (var handle = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = bw * 0.045f, Color = wicker })
        using (var handleEdge = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = bw * 0.01f, Color = dark })
        {
            var arc = new SKRect(x - (bw * 0.44f), rimY - (bw * 0.42f), x + (bw * 0.44f), rimY + (bw * 0.42f));
            c.DrawArc(arc, 180, 180, false, handle);
            c.DrawArc(arc, 180, 180, false, handleEdge);
        }

        // Eggs in the basket; the basket front covers their lower halves.
        var eggs = new[] { (-0.28f, 8f), (-0.1f, -6f), (0.1f, 10f), (0.28f, -10f) };
        for (var i = 0; i < eggs.Length; i++)
        {
            Egg(c, x + (bw * eggs[i].Item1), rimY + (bw * 0.08f), bw * 0.16f, eggs[i].Item2, eggColours[(i + 1) % eggColours.Length], eggColours[(i + 4) % eggColours.Length]);
        }

        using var body = new SKPath();
        body.MoveTo(x - (bw * 0.5f), rimY);
        body.CubicTo(x - (bw * 0.48f), rimY + (bw * 0.2f), x - (bw * 0.42f), ground, x - (bw * 0.3f), ground);
        body.LineTo(x + (bw * 0.3f), ground);
        body.CubicTo(x + (bw * 0.42f), ground, x + (bw * 0.48f), rimY + (bw * 0.2f), x + (bw * 0.5f), rimY);
        body.Close();
        using (var fill = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateLinearGradient(new SKPoint(x - (bw * 0.5f), 0), new SKPoint(x + (bw * 0.5f), 0), new[] { Lighten(wicker, 0.15f), wicker, Darken(wicker, 0.7f) }, new[] { 0f, 0.4f, 1f }, SKShaderTileMode.Clamp),
            ImageFilter = SKImageFilter.CreateDropShadow(0, bw * 0.01f, bw * 0.02f, bw * 0.02f, SKColors.Black.WithAlpha(110))
        })
        {
            c.DrawPath(body, fill);
        }

        // Weave: rows of short strands, offset row to row, over upright stakes.
        c.Save();
        c.ClipPath(body, antialias: true);
        using (var weave = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = bw * 0.008f, Color = dark })
        {
            var row = 0;
            for (var y = rimY + (bw * 0.03f); y < ground; y += bw * 0.045f, row++)
            {
                for (var sx = x - (bw * 0.55f) + (row % 2 == 0 ? 0 : bw * 0.04f); sx < x + (bw * 0.55f); sx += bw * 0.08f)
                {
                    c.DrawRoundRect(new SKRect(sx, y, sx + (bw * 0.07f), y + (bw * 0.035f)), bw * 0.015f, bw * 0.015f, weave);
                }
            }
        }

        c.Restore();

        using (var rim = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = bw * 0.04f, Color = wicker })
        using (var rimEdge = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = bw * 0.008f, Color = dark })
        {
            var rect = new SKRect(x - (bw * 0.5f), rimY - (bw * 0.035f), x + (bw * 0.5f), rimY + (bw * 0.035f));
            c.DrawArc(rect, 0, 180, false, rim);
            c.DrawArc(rect, 0, 180, false, rimEdge);
        }

        // A bow on the handle and a chick sat on the eggs.
        using (var bow = new SKPaint { IsAntialias = true, Color = new SKColor(0xEC, 0x40, 0x7A) })
        {
            var bx = x - (bw * 0.42f);
            var by = rimY - (bw * 0.15f);
            c.DrawOval(new SKRect(bx - (bw * 0.07f), by - (bw * 0.035f), bx, by + (bw * 0.035f)), bow);
            c.DrawOval(new SKRect(bx, by - (bw * 0.035f), bx + (bw * 0.07f), by + (bw * 0.035f)), bow);
            c.DrawCircle(bx, by, bw * 0.02f, bow);
        }

        Chick(c, x + (bw * 0.05f), rimY + (bw * 0.02f), bw * 0.24f, true);
    }

    // ---------- Valentine's: heart bunting and roses ----------

    private static void HeartsAndRoses(SKCanvas c, int w, int h, SKColor tint, Random rng)
    {
        float s = Math.Min(w, h);
        var colours = new[] { new SKColor(0xE5, 0x39, 0x35), new SKColor(0xFF, 0x80, 0xAB), SKColors.White, Lighten(tint, 0.3f) };

        // Two strings of bunting draped across the top.
        for (var k = 0; k < 2; k++)
        {
            var y0 = h * (0.03f + (k * 0.07f));
            var sag = h * (0.07f + (k * 0.03f));
            SKPoint At(float t) => new((-w * 0.05f) + (t * w * 1.1f), y0 + (4 * sag * t * (1 - t)));

            using (var cord = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.003f, Color = new SKColor(0xFF, 0xE0, 0xE8, 200) })
            using (var line = new SKPath())
            {
                line.MoveTo(At(0));
                for (var t = 0.02f; t <= 1.001f; t += 0.02f)
                {
                    line.LineTo(At(t));
                }

                c.DrawPath(line, cord);
            }

            var count = w > h ? 11 : 8;
            for (var i = 0; i < count; i++)
            {
                var p = At((i + 0.5f + (k * 0.5f)) / (count + 0.5f));
                var size = s * (k == 0 ? 0.05f : 0.042f);
                GlossyHeart(c, p.X, p.Y + (size * 0.45f), size, colours[(i + k) % colours.Length], R(rng, -8, 8));
            }
        }

        // Roses gathered in the bottom corners.
        var roseColours = new[] { new SKColor(0xC6, 0x28, 0x28), new SKColor(0xE5, 0x39, 0x35), new SKColor(0xEC, 0x40, 0x7A), new SKColor(0xAD, 0x14, 0x57) };
        foreach (var mirror in new[] { false, true })
        {
            c.Save();
            if (mirror)
            {
                c.Translate(w, 0);
                c.Scale(-1, 1);
            }

            // Leaves fan out from under the bunch, so only their tips show between the roses.
            var heart = new SKPoint(s * 0.11f, h - (s * 0.1f));
            foreach (var angle in new[] { -105f, -70f, -35f, 0f })
            {
                RoseLeaf(c, heart.X, heart.Y, s * 0.2f, angle);
            }

            var bunch = new[] { (0.05f, 0.17f, 0.06f), (0.18f, 0.12f, 0.06f), (0.08f, 0.05f, 0.07f), (0.21f, 0.03f, 0.055f), (0.0f, 0.04f, 0.055f) };
            for (var i = 0; i < bunch.Length; i++)
            {
                var (rx, up, rr) = bunch[i];
                Rose(c, s * rx, h - (s * up), s * rr, roseColours[(i + (mirror ? 2 : 0)) % roseColours.Length], rng);
            }

            c.Restore();
        }

        // Petals drifting down, kept off the title.
        using var petal = new SKPaint { IsAntialias = true };
        for (var i = 0; i < 18; i++)
        {
            float px = R(rng, 0, w), py = R(rng, h * 0.2f, h * 0.8f);
            if (Math.Abs(px - (w / 2f)) < w * 0.4f && Math.Abs(py - (h / 2f)) < h * 0.17f)
            {
                continue;
            }

            petal.Color = roseColours[rng.Next(roseColours.Length)].WithAlpha(200);
            c.Save();
            c.Translate(px, py);
            c.RotateDegrees(R(rng, 0, 360));
            c.DrawOval(new SKRect(-s * 0.012f, -s * 0.007f, s * 0.012f, s * 0.007f), petal);
            c.Restore();
        }
    }

    /// <summary>A heart with a soft gradient and a shine on its upper left.</summary>
    private static void GlossyHeart(SKCanvas c, float x, float y, float size, SKColor colour, float rotation)
    {
        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(rotation);
        using var heart = Heart(size);
        using var fill = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(new SKPoint(-size * 0.25f, -size * 0.35f), size * 1.1f, new[] { Lighten(colour, 0.4f), colour, Darken(colour, 0.7f) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp),
            ImageFilter = SKImageFilter.CreateDropShadow(0, size * 0.05f, size * 0.08f, size * 0.08f, SKColors.Black.WithAlpha(100))
        };
        c.DrawPath(heart, fill);
        using var shine = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(150) };
        c.RotateDegrees(-35, -size * 0.3f, -size * 0.38f);
        c.DrawOval(new SKRect(-size * 0.42f, -size * 0.45f, -size * 0.18f, -size * 0.32f), shine);
        c.Restore();
    }

    /// <summary>
    /// A rose seen from above: three rings of rounded petals, each ring smaller, lighter and turned against the one
    /// below, every petal shaded darker towards the middle so the layers read; a tight bud in the centre.
    /// </summary>
    private static void Rose(SKCanvas c, float x, float y, float r, SKColor colour, Random rng)
    {
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.08f))
        using (var shadow = new SKPaint { IsAntialias = true, Color = SKColors.Black.WithAlpha(90), MaskFilter = blur })
        {
            c.DrawCircle(x, y + (r * 0.08f), r * 0.98f, shadow);
        }

        var turn = R(rng, 0, (float)Math.PI);
        var rings = new[] { (Count: 5, Dist: 0.5f, Size: 0.5f, Light: 0f), (Count: 5, Dist: 0.3f, Size: 0.4f, Light: 0.12f), (Count: 4, Dist: 0.14f, Size: 0.28f, Light: 0.24f) };
        for (var ring = 0; ring < rings.Length; ring++)
        {
            var (count, dist, size, light) = rings[ring];
            var tone = Lighten(colour, light);
            for (var i = 0; i < count; i++)
            {
                var a = turn + (ring * 0.6f) + (i * Math.PI * 2 / count);
                var px = x + (float)(Math.Cos(a) * r * dist);
                var py = y + (float)(Math.Sin(a) * r * dist);
                using var petal = new SKPaint
                {
                    IsAntialias = true,
                    Shader = SKShader.CreateRadialGradient(new SKPoint(px + (float)(Math.Cos(a) * r * size * 0.4f), py + (float)(Math.Sin(a) * r * size * 0.4f)), r * size * 1.3f, new[] { Lighten(tone, 0.2f), tone, Darken(tone, 0.6f) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp)
                };
                c.DrawCircle(px, py, r * size, petal);
            }
        }

        // Bud: a dark centre with one lighter curl around it.
        using var core = new SKPaint { IsAntialias = true, Color = Darken(colour, 0.5f) };
        c.DrawCircle(x, y, r * 0.12f, core);
        using var curl = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = r * 0.06f, Color = Lighten(colour, 0.3f) };
        c.DrawArc(new SKRect(x - (r * 0.12f), y - (r * 0.12f), x + (r * 0.12f), y + (r * 0.12f)), turn * 57.3f, 200, false, curl);
    }

    private static void RoseLeaf(SKCanvas c, float x, float y, float len, float angle)
    {
        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(angle);
        using var leaf = new SKPath();
        leaf.MoveTo(0, 0);
        leaf.QuadTo(len * 0.5f, -len * 0.32f, len, 0);
        leaf.QuadTo(len * 0.5f, len * 0.32f, 0, 0);
        leaf.Close();
        using var fill = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, -len * 0.3f), new SKPoint(0, len * 0.3f), new[] { new SKColor(0x43, 0xA0, 0x47), new SKColor(0x1B, 0x5E, 0x20) }, SKShaderTileMode.Clamp) };
        c.DrawPath(leaf, fill);
        using var vein = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = len * 0.025f, Color = new SKColor(0xA5, 0xD6, 0xA7, 180) };
        c.DrawLine(0, 0, len * 0.9f, 0, vein);
        c.Restore();
    }

    // ---------- Valentine's: bunches of heart balloons ----------

    private static void HeartBalloons(SKCanvas c, int w, int h, SKColor tint, Random rng)
    {
        float s = Math.Min(w, h);
        var colours = new[] { new SKColor(0xE5, 0x39, 0x35), new SKColor(0xFF, 0x80, 0xAB), new SKColor(0xEC, 0x40, 0x7A), new SKColor(0xF5, 0xF5, 0xF5), Lighten(tint, 0.2f) };

        // A bunch in each top corner. Strings are straight lines to one knot, drawn before the balloons, so they meet
        // only at the knot and pass behind any balloon below them. The knot and its ribbon stay above the title.
        foreach (var right in new[] { false, true })
        {
            var balloons = new[] { (0.1f, 0.075f, 0.075f), (0.24f, 0.1f, 0.07f), (0.06f, 0.21f, 0.065f) };
            SKPoint Place(float fx, float fy) => new(right ? w - (s * fx) : s * fx, s * fy);
            var knot = Place(0.2f, 0.33f);
            var spots = balloons.Select(b => (At: Place(b.Item1, b.Item2), Size: s * b.Item3, Tilt: R(rng, -10, 10))).ToArray();

            using (var str = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.0025f, Color = SKColors.White.WithAlpha(190) })
            {
                foreach (var spot in spots)
                {
                    c.DrawLine(BalloonKnot(spot.At, spot.Size, spot.Tilt), knot, str);
                }
            }

            for (var i = 0; i < spots.Length; i++)
            {
                HeartBalloon(c, spots[i].At.X, spots[i].At.Y, spots[i].Size, colours[(i + (right ? 2 : 0)) % colours.Length], spots[i].Tilt);
            }

            // Bow and a short curl of ribbon.
            using var ribbon = new SKPaint { IsAntialias = true, Color = new SKColor(0xE5, 0x39, 0x35) };
            using (var tail = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.007f, StrokeCap = SKStrokeCap.Round, Color = ribbon.Color })
            using (var curl = new SKPath())
            {
                curl.MoveTo(knot);
                curl.CubicTo(knot.X - (s * 0.025f), knot.Y + (s * 0.025f), knot.X + (s * 0.025f), knot.Y + (s * 0.045f), knot.X, knot.Y + (s * 0.065f));
                c.DrawPath(curl, tail);
            }

            c.DrawOval(new SKRect(knot.X - (s * 0.04f), knot.Y - (s * 0.018f), knot.X, knot.Y + (s * 0.018f)), ribbon);
            c.DrawOval(new SKRect(knot.X, knot.Y - (s * 0.018f), knot.X + (s * 0.04f), knot.Y + (s * 0.018f)), ribbon);
            c.DrawCircle(knot.X, knot.Y, s * 0.012f, ribbon);
        }

        // A sprinkle of small hearts and sparkles, off the title and the balloons.
        using var dot = new SKPaint { IsAntialias = true };
        var hearts = new List<(float X, float Y, float R)>();
        for (var i = 0; i < 90; i++)
        {
            float px = R(rng, 0, w), py = R(rng, 0, h);
            var onTitle = Math.Abs(px - (w / 2f)) < w * 0.44f && Math.Abs(py - (h / 2f)) < h * 0.2f;
            var onBalloons = py < s * 0.42f && (px < s * 0.38f || px > w - (s * 0.38f));
            if (onTitle || onBalloons || hearts.Exists(o => Math.Abs(o.X - px) < s * 0.08f && Math.Abs(o.Y - py) < s * 0.08f))
            {
                continue;
            }

            hearts.Add((px, py, 0));

            if (i % 2 == 0)
            {
                GlossyHeart(c, px, py, s * R(rng, 0.02f, 0.04f), colours[rng.Next(colours.Length)], R(rng, -25, 25));
            }
            else
            {
                dot.Color = SKColors.White.WithAlpha((byte)rng.Next(120, 220));
                c.DrawCircle(px, py, s * R(rng, 0.002f, 0.005f), dot);
            }
        }
    }

    /// <summary>Where a balloon's string starts: under the knot at its point, turned with the balloon.</summary>
    private static SKPoint BalloonKnot(SKPoint at, float size, float rotation)
    {
        var below = (size * 1.6f * 0.35f) + (size * 0.1f);
        var a = rotation * Math.PI / 180;
        return new SKPoint(at.X - (float)(Math.Sin(a) * below), at.Y + (float)(Math.Cos(a) * below));
    }

    /// <summary>A shiny heart-shaped foil balloon centred on (x, y), its point down.</summary>
    private static void HeartBalloon(SKCanvas c, float x, float y, float size, SKColor colour, float rotation)
    {
        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(rotation);
        using var heart = Heart(size * 1.6f);
        using var fill = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(new SKPoint(-size * 0.35f, -size * 0.45f), size * 1.6f, new[] { Lighten(colour, 0.55f), colour, Darken(colour, 0.6f) }, new[] { 0f, 0.45f, 1f }, SKShaderTileMode.Clamp),
            ImageFilter = SKImageFilter.CreateDropShadow(size * 0.04f, size * 0.08f, size * 0.1f, size * 0.1f, SKColors.Black.WithAlpha(110))
        };
        c.DrawPath(heart, fill);

        // Foil highlights: a long streak on the left lobe and a small glint on the right.
        using var shine = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(170) };
        c.Save();
        c.RotateDegrees(-40, -size * 0.5f, -size * 0.5f);
        c.DrawOval(new SKRect(-size * 0.75f, -size * 0.62f, -size * 0.25f, -size * 0.44f), shine);
        c.Restore();
        c.DrawCircle(size * 0.42f, -size * 0.55f, size * 0.06f, shine);

        // Knot at the point.
        var tip = size * 1.6f * 0.35f;
        using var knot = new SKPath();
        knot.MoveTo(0, tip);
        knot.LineTo(-size * 0.07f, tip + (size * 0.1f));
        knot.LineTo(size * 0.07f, tip + (size * 0.1f));
        knot.Close();
        using var knotPaint = new SKPaint { IsAntialias = true, Color = Darken(colour, 0.7f) };
        c.DrawPath(knot, knotPaint);
        c.Restore();
    }
}
