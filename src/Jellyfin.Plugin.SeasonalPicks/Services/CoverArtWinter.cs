using System;
using SkiaSharp;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>The Christmas and winter scenes the weeks of the season switch between after the first.</summary>
internal static partial class CoverArt
{
    // ---------- Fireplace: a living room with the fire and stockings on one side, the tree and presents on the other ----------

    private static void Fireplace(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var width = s * 0.62f;
        var fx = (w / 2f) - (s * 0.2f);
        var left = fx - (width / 2);
        var floor = h * 0.97f;
        var mantelY = floor - (s * 0.23f);

        // Warm light from the fire across the room.
        using (var glow = new SKPaint { Shader = SKShader.CreateRadialGradient(new SKPoint(fx, floor - (s * 0.08f)), s * 0.75f, new[] { new SKColor(0xFF, 0x9F, 0x40, 110), SKColors.Transparent }, SKShaderTileMode.Clamp) })
        {
            c.DrawRect(0, 0, w, h, glow);
        }

        // Floorboards, the brick chimney breast, then the firebox opening with the fire inside.
        using (var boards = new SKPaint { Shader = SKShader.CreateLinearGradient(new SKPoint(0, floor), new SKPoint(0, h), new[] { new SKColor(0x6D, 0x45, 0x2A), new SKColor(0x4A, 0x2E, 0x1A) }, SKShaderTileMode.Clamp) })
        {
            c.DrawRect(0, floor, w, h - floor, boards);
        }

        var body = new SKRect(left + (width * 0.04f), mantelY, left + (width * 0.96f), floor);
        using (var brick = new SKPaint { Shader = SKShader.CreateLinearGradient(new SKPoint(0, body.Top), new SKPoint(0, body.Bottom), new[] { new SKColor(0x9C, 0x45, 0x30), new SKColor(0x6E, 0x2C, 0x1E) }, SKShaderTileMode.Clamp) })
        {
            c.DrawRect(body, brick);
        }

        c.Save();
        c.ClipRect(body);
        using (var mortar = new SKPaint { IsAntialias = true, StrokeWidth = s * 0.003f, Color = new SKColor(0xD8, 0xC0, 0xA8, 150) })
        {
            var rowH = s * 0.028f;
            var brickW = s * 0.07f;
            var row = 0;
            for (var y = body.Top + rowH; y < body.Bottom + rowH; y += rowH, row++)
            {
                c.DrawLine(body.Left, y, body.Right, y, mortar);
                for (var x = body.Left + (row % 2 == 0 ? 0 : brickW / 2); x < body.Right; x += brickW)
                {
                    c.DrawLine(x, y - rowH, x, y, mortar);
                }
            }
        }

        c.Restore();

        var opening = new SKRect(fx - (s * 0.105f), floor - (s * 0.16f), fx + (s * 0.105f), floor);
        using var arch = new SKPath();
        arch.MoveTo(opening.Left, opening.Bottom);
        arch.LineTo(opening.Left, opening.Top + (opening.Width * 0.18f));
        arch.QuadTo(opening.MidX, opening.Top - (opening.Width * 0.08f), opening.Right, opening.Top + (opening.Width * 0.18f));
        arch.LineTo(opening.Right, opening.Bottom);
        arch.Close();
        using (var stone = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.016f, StrokeJoin = SKStrokeJoin.Round, Color = new SKColor(0xB8, 0xA8, 0x98) })
        using (var soot = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, opening.Top), new SKPoint(0, opening.Bottom), new[] { new SKColor(0x0A, 0x08, 0x08), new SKColor(0x2A, 0x16, 0x0C) }, SKShaderTileMode.Clamp) })
        {
            c.DrawPath(arch, stone);
            c.DrawPath(arch, soot);
        }

        c.Save();
        c.ClipPath(arch, antialias: true);
        LogFire(c, opening, s, rng);
        c.Restore();

        // Hearth along the floor.
        using (var hearth = new SKPaint { IsAntialias = true, Color = new SKColor(0x8A, 0x80, 0x78) })
        {
            c.DrawRect(left, floor - (s * 0.018f), width, s * 0.018f, hearth);
        }

        // Mantel shelf with a garland of lights, and a candle at each end.
        var shelf = new SKRect(left, mantelY - (s * 0.03f), left + width, mantelY);
        using (var wood = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, shelf.Top), new SKPoint(0, shelf.Bottom + (s * 0.012f)), new[] { new SKColor(0x8D, 0x5A, 0x34), new SKColor(0x4E, 0x2E, 0x18) }, SKShaderTileMode.Clamp), ImageFilter = SKImageFilter.CreateDropShadow(0, s * 0.008f, s * 0.008f, s * 0.008f, SKColors.Black.WithAlpha(140)) })
        {
            c.DrawRoundRect(shelf, s * 0.006f, s * 0.006f, wood);
            c.DrawRect(shelf.Left + (width * 0.03f), shelf.Bottom, width * 0.94f, s * 0.012f, wood);
        }

        TallCandle(c, left + (width * 0.05f), shelf.Top, s * 0.05f, rng);
        TallCandle(c, left + (width * 0.95f), shelf.Top, s * 0.05f, rng);
        MantelGarland(c, shelf.Left + (width * 0.1f), shelf.Right - (width * 0.1f), shelf.Top, s, rng);

        // Stockings hang from hooks under the shelf, two each side of the opening, toes pointing outwards. The spacing
        // (in units of s from the middle) leaves a gap between each toe and the next stocking's heel.
        var stockings = new[]
        {
            (X: -0.225f, Body: new SKColor(0xC6, 0x28, 0x28), Trim: new SKColor(0x2E, 0x7D, 0x32), Pattern: 0),
            (X: -0.14f, Body: new SKColor(0x2E, 0x7D, 0x32), Trim: new SKColor(0xC6, 0x28, 0x28), Pattern: 1),
            (X: 0.14f, Body: new SKColor(0x15, 0x65, 0xC0), Trim: new SKColor(0xFF, 0xD5, 0x4F), Pattern: 2),
            (X: 0.225f, Body: new SKColor(0xC6, 0x28, 0x28), Trim: SKColors.White, Pattern: 1)
        };
        foreach (var st in stockings)
        {
            Stocking(c, fx + (s * st.X), shelf.Bottom + (s * 0.012f), s * 0.12f, st.X > 0, st.Body, st.Trim, st.Pattern, s);
        }

        // A wreath on the chimney breast above the mantel.
        Wreath(c, fx, shelf.Top - (s * 0.045f), s * 0.032f, s, rng);

        // The tree with its presents across the room.
        DecoratedTree(c, fx + (s * 0.53f), floor, s * 0.34f, rng);

        // A round rug in front of the fire with the cat asleep on it.
        var rug = new SKRect(fx - (s * 0.24f), floor - (s * 0.015f), fx + (s * 0.24f), h + (s * 0.05f));
        using (var wool = new SKPaint { IsAntialias = true, Color = new SKColor(0x8E, 0x24, 0x2A) })
        using (var band = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.006f, Color = new SKColor(0xE8, 0xC8, 0x80, 200) })
        {
            c.DrawOval(rug, wool);
            foreach (var inset in new[] { 0.018f, 0.04f })
            {
                var ring = rug;
                ring.Inflate(-s * inset, -s * inset * 0.35f);
                c.DrawOval(ring, band);
            }
        }

        SleepingCat(c, fx + (s * 0.07f), floor + (s * 0.012f), s * 0.11f);
    }

    /// <summary>An evergreen wreath with red berries and a bow at the bottom; r is the radius of its ring.</summary>
    private static void Wreath(SKCanvas c, float x, float y, float r, float s, Random rng)
    {
        var greens = new[] { new SKColor(0x1B, 0x5E, 0x20), new SKColor(0x2E, 0x7D, 0x32), new SKColor(0x38, 0x8E, 0x3C) };
        using (var ring = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = r * 0.5f, Color = greens[0], ImageFilter = SKImageFilter.CreateDropShadow(0, r * 0.06f, r * 0.08f, r * 0.08f, SKColors.Black.WithAlpha(120)) })
        {
            c.DrawCircle(x, y, r, ring);
        }

        using var needle = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = s * 0.003f };
        for (var i = 0; i < 160; i++)
        {
            var a = R(rng, 0, (float)(Math.PI * 2));
            var d = r + R(rng, -r * 0.22f, r * 0.22f);
            var px = x + (float)(Math.Cos(a) * d);
            var py = y + (float)(Math.Sin(a) * d);
            var dir = a + (Math.PI / 2) + R(rng, -0.6f, 0.6f);
            var len = r * R(rng, 0.12f, 0.22f);
            needle.Color = greens[rng.Next(greens.Length)];
            c.DrawLine(px, py, px + (float)(Math.Cos(dir) * len), py + (float)(Math.Sin(dir) * len), needle);
        }

        using var berry = new SKPaint { IsAntialias = true, Color = new SKColor(0xD3, 0x2F, 0x2F) };
        foreach (var a in new[] { 20f, 60f, 140f, 200f, 250f, 300f, 340f })
        {
            var rad = a * Math.PI / 180;
            c.DrawCircle(x + (float)(Math.Cos(rad) * r), y + (float)(Math.Sin(rad) * r * 0.98f), r * 0.09f, berry);
        }

        // Bow at the bottom.
        var by = y + r;
        c.DrawOval(new SKRect(x - (r * 0.5f), by - (r * 0.2f), x, by + (r * 0.2f)), berry);
        c.DrawOval(new SKRect(x, by - (r * 0.2f), x + (r * 0.5f), by + (r * 0.2f)), berry);
        using var tails = new SKPath();
        tails.MoveTo(x - (r * 0.05f), by);
        tails.LineTo(x - (r * 0.3f), by + (r * 0.55f));
        tails.LineTo(x - (r * 0.14f), by + (r * 0.5f));
        tails.Close();
        tails.MoveTo(x + (r * 0.05f), by);
        tails.LineTo(x + (r * 0.3f), by + (r * 0.55f));
        tails.LineTo(x + (r * 0.14f), by + (r * 0.5f));
        tails.Close();
        c.DrawPath(tails, berry);
        using var knot = new SKPaint { IsAntialias = true, Color = new SKColor(0xB7, 0x1C, 0x1C) };
        c.DrawCircle(x, by, r * 0.1f, knot);
    }

    /// <summary>A ginger cat curled up asleep, tail wrapped round; (x, ground) is the middle of where it lies, size its length.</summary>
    private static void SleepingCat(SKCanvas c, float x, float ground, float size)
    {
        var fur = new SKColor(0xE0, 0x8A, 0x3C);
        using var coat = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateLinearGradient(new SKPoint(x, ground - (size * 0.4f)), new SKPoint(x, ground), new[] { Lighten(fur, 0.15f), Darken(fur, 0.75f) }, SKShaderTileMode.Clamp),
            ImageFilter = SKImageFilter.CreateDropShadow(0, size * 0.03f, size * 0.05f, size * 0.05f, SKColors.Black.WithAlpha(110))
        };

        // Body, head and ears as one shape.
        using var shape = new SKPath();
        shape.AddOval(new SKRect(x - (size * 0.5f), ground - (size * 0.38f), x + (size * 0.38f), ground));
        shape.AddCircle(x - (size * 0.38f), ground - (size * 0.16f), size * 0.17f);
        foreach (var ex in new[] { -0.5f, -0.3f })
        {
            shape.MoveTo(x + (size * (ex - 0.07f)), ground - (size * 0.26f));
            shape.LineTo(x + (size * ex), ground - (size * 0.4f));
            shape.LineTo(x + (size * (ex + 0.07f)), ground - (size * 0.27f));
            shape.Close();
        }

        using (var merged = new SKPath())
        {
            shape.Simplify(merged);
            c.DrawPath(merged, coat);
        }

        // Tail curling round the front, in the same coat so it joins without an outline.
        using (var tail = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = size * 0.09f, Color = Darken(fur, 0.85f) })
        using (var curl = new SKPath())
        {
            curl.MoveTo(x + (size * 0.36f), ground - (size * 0.08f));
            curl.QuadTo(x + (size * 0.2f), ground + (size * 0.02f), x - (size * 0.2f), ground - (size * 0.01f));
            c.DrawPath(curl, tail);
        }

        // Stripes on the back, closed eyes and a pink nose.
        using (var stripe = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = size * 0.025f, Color = Darken(fur, 0.65f) })
        {
            foreach (var sx in new[] { -0.05f, 0.08f, 0.2f })
            {
                c.DrawArc(new SKRect(x + (size * (sx - 0.05f)), ground - (size * 0.37f), x + (size * (sx + 0.05f)), ground - (size * 0.25f)), 200, 140, false, stripe);
            }
        }

        using var ink = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = size * 0.015f, Color = new SKColor(0x3E, 0x27, 0x23) };
        c.DrawArc(new SKRect(x - (size * 0.49f), ground - (size * 0.21f), x - (size * 0.42f), ground - (size * 0.15f)), 20, 140, false, ink);
        c.DrawArc(new SKRect(x - (size * 0.36f), ground - (size * 0.21f), x - (size * 0.29f), ground - (size * 0.15f)), 20, 140, false, ink);
        using var nose = new SKPaint { IsAntialias = true, Color = new SKColor(0xF4, 0x8F, 0xB1) };
        c.DrawCircle(x - (size * 0.39f), ground - (size * 0.11f), size * 0.018f, nose);
    }

    /// <summary>Logs in the firebox with layered flames and a glowing bed of embers.</summary>
    private static void LogFire(SKCanvas c, SKRect box, float s, Random rng)
    {
        var cx = box.MidX;
        var floor = box.Bottom - (s * 0.018f);
        using (var embers = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(cx, floor), box.Width * 0.45f, new[] { new SKColor(0xFF, 0x8F, 0x00, 200), SKColors.Transparent }, SKShaderTileMode.Clamp) })
        {
            c.DrawRect(box, embers);
        }

        var fireH = box.Height * 0.8f;
        var layers = new[] { (Colour: new SKColor(0xD8, 0x43, 0x15, 230), Height: 1f, Width: 0.3f), (Colour: new SKColor(0xFF, 0x8F, 0x00, 235), Height: 0.75f, Width: 0.22f), (Colour: new SKColor(0xFF, 0xD5, 0x4F, 240), Height: 0.45f, Width: 0.14f) };
        foreach (var (colour, height, wdt) in layers)
        {
            using var paint = new SKPaint { IsAntialias = true, Color = colour, ImageFilter = SKImageFilter.CreateDropShadow(0, 0, s * 0.008f, s * 0.008f, colour) };
            for (var t = -2; t <= 2; t++)
            {
                var offset = t * box.Width * wdt * 0.32f;
                var th = fireH * height * R(rng, 0.7f, 1f) * (t == 0 ? 1f : 0.78f);
                var lean = R(rng, -fireH * 0.06f, fireH * 0.06f);
                var half = box.Width * wdt * 0.3f;
                using var tongue = new SKPath();
                tongue.MoveTo(cx + offset - half, floor - (s * 0.02f));
                tongue.CubicTo(cx + offset - (half * 1.1f), floor - (th * 0.5f), cx + offset + lean, floor - (th * 0.75f), cx + offset + lean, floor - th);
                tongue.CubicTo(cx + offset + lean + (half * 0.3f), floor - (th * 0.6f), cx + offset + (half * 1.1f), floor - (th * 0.4f), cx + offset + half, floor - (s * 0.02f));
                tongue.Close();
                c.DrawPath(tongue, paint);
            }
        }

        // Two logs side by side in front of the flames, cut ends showing their rings.
        using var bark = new SKPaint { IsAntialias = true, Color = new SKColor(0x4E, 0x34, 0x2E) };
        using var cut = new SKPaint { IsAntialias = true, Color = new SKColor(0xC8, 0x9A, 0x6A) };
        using var ring = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.002f, Color = new SKColor(0x8D, 0x5A, 0x34) };
        var logH = s * 0.032f;
        foreach (var side in new[] { -1f, 1f })
        {
            var log = new SKRect(cx + (side < 0 ? -box.Width * 0.4f : box.Width * 0.02f), floor - logH, cx + (side < 0 ? -box.Width * 0.02f : box.Width * 0.4f), floor);
            c.DrawRoundRect(log, logH / 2, logH / 2, bark);
            var endX = side < 0 ? log.Left + (logH * 0.5f) : log.Right - (logH * 0.5f);
            c.DrawCircle(endX, log.MidY, logH * 0.45f, cut);
            c.DrawCircle(endX, log.MidY, logH * 0.25f, ring);
        }
    }

    /// <summary>A garland draped along the front of the mantel between two points, with coloured lights.</summary>
    private static void MantelGarland(SKCanvas c, float left, float right, float top, float s, Random rng)
    {
        const int Swags = 3;
        var span = (right - left) / Swags;
        float Y(float x)
        {
            var t = (x - left) / span % 1;
            return top + (s * 0.004f) + (4 * s * 0.035f * t * (1 - t));
        }

        var greens = new[] { new SKColor(0x1B, 0x5E, 0x20), new SKColor(0x2E, 0x7D, 0x32), new SKColor(0x38, 0x8E, 0x3C) };
        using var needle = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = s * 0.0035f };
        for (var x = left; x <= right; x += s * 0.004f)
        {
            var y = Y(x);
            for (var k = 0; k < 3; k++)
            {
                var a = R(rng, 0, (float)(Math.PI * 2));
                var len = s * R(rng, 0.012f, 0.026f);
                needle.Color = greens[rng.Next(greens.Length)];
                c.DrawLine(x, y, x + (float)(Math.Cos(a) * len), y + (float)(Math.Sin(a) * len * 0.7f), needle);
            }
        }

        var bulbs = new[] { new SKColor(0xFF, 0xD5, 0x4F), new SKColor(0xFF, 0x52, 0x52), new SKColor(0x40, 0xC4, 0xFF), new SKColor(0x69, 0xF0, 0xAE) };
        var i = 0;
        for (var x = left + (span * 0.12f); x < right; x += span / 5)
        {
            var colour = bulbs[i++ % bulbs.Length];
            using var bulb = new SKPaint { IsAntialias = true, Color = colour, ImageFilter = SKImageFilter.CreateDropShadow(0, 0, s * 0.006f, s * 0.006f, colour) };
            c.DrawCircle(x, Y(x) + (s * 0.006f), s * 0.006f, bulb);
        }
    }

    /// <summary>
    /// A Christmas stocking hanging from a hook at (x, top): fluffy cuff, then the leg and foot, with a heel and toe
    /// patch and a pattern (0 stripes, 1 snow dots, 2 a star). The toe points left, or right when <paramref name="toeRight"/>.
    /// </summary>
    private static void Stocking(SKCanvas c, float x, float top, float length, bool toeRight, SKColor body, SKColor trim, int pattern, float s)
    {
        var sw = length * 0.36f;
        c.Save();
        c.Translate(x, top);
        c.Scale(toeRight ? 1 : -1, 1);
        c.Translate(-sw / 2, 0);

        using (var hook = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.004f, Color = new SKColor(0xD4, 0xAF, 0x37) })
        {
            c.DrawLine(sw / 2, 0, sw / 2, length * 0.06f, hook);
        }

        var cuffH = length * 0.2f;
        using var leg = new SKPath();
        leg.MoveTo(0, cuffH * 0.9f);
        leg.LineTo(sw, cuffH * 0.9f);
        leg.LineTo(sw, length * 0.62f);
        leg.CubicTo(sw, length * 0.7f, sw * 1.45f, length * 0.68f, sw * 1.65f, length * 0.8f);
        leg.CubicTo(sw * 1.85f, length * 0.94f, sw * 1.5f, length, sw * 1.1f, length);
        leg.LineTo(sw * 0.45f, length);
        leg.CubicTo(-sw * 0.08f, length, -sw * 0.08f, length * 0.84f, 0, length * 0.7f);
        leg.Close();
        using (var fill = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(sw * 1.6f, 0), new[] { Lighten(body, 0.15f), body, Darken(body, 0.7f) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp),
            ImageFilter = SKImageFilter.CreateDropShadow(s * 0.003f, s * 0.006f, s * 0.006f, s * 0.006f, SKColors.Black.WithAlpha(120))
        })
        {
            c.DrawPath(leg, fill);
        }

        c.Save();
        c.ClipPath(leg, antialias: true);
        using (var patch = new SKPaint { IsAntialias = true, Color = trim })
        {
            c.DrawCircle(sw * 1.55f, length * 0.92f, sw * 0.42f, patch);  // toe
            c.DrawCircle(sw * 0.05f, length * 0.9f, sw * 0.35f, patch);   // heel
        }

        using (var deco = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(220) })
        {
            switch (pattern)
            {
                case 0:
                    for (var y = cuffH + (length * 0.06f); y < length * 0.6f; y += length * 0.1f)
                    {
                        c.DrawRect(-sw * 0.1f, y, sw * 1.3f, length * 0.035f, deco);
                    }

                    break;
                case 1:
                    for (var row = 0; row < 5; row++)
                    {
                        for (var col = 0; col < 3; col++)
                        {
                            c.DrawCircle((sw * 0.2f) + (col * sw * 0.3f) + (row % 2 == 0 ? 0 : sw * 0.15f), cuffH + (length * 0.08f) + (row * length * 0.09f), sw * 0.05f, deco);
                        }
                    }

                    break;
                default:
                    deco.Color = new SKColor(0xFF, 0xD5, 0x4F);
                    using (var star = Star(sw * 0.5f, length * 0.42f, sw * 0.3f, sw * 0.13f))
                    {
                        c.DrawPath(star, deco);
                    }

                    break;
            }
        }

        c.Restore();

        // Fluffy cuff over the top of the leg.
        using var fur = new SKPaint { IsAntialias = true, Color = new SKColor(0xF7, 0xF4, 0xEE), ImageFilter = SKImageFilter.CreateDropShadow(0, s * 0.003f, s * 0.004f, s * 0.004f, SKColors.Black.WithAlpha(90)) };
        using var cuff = new SKPath();
        cuff.AddRoundRect(new SKRect(-sw * 0.12f, length * 0.04f, sw * 1.12f, cuffH), sw * 0.12f, sw * 0.12f);
        for (var k = 0; k <= 4; k++)
        {
            cuff.AddCircle((-sw * 0.06f) + (k * sw * 0.31f), cuffH - (sw * 0.02f), sw * 0.11f);
        }

        using (var merged = new SKPath())
        {
            cuff.Simplify(merged);
            c.DrawPath(merged, fur);
        }

        c.Restore();
    }


    /// <summary>A snow-dusted pine: tiers with ragged lower edges. (x, ground) is the foot of its trunk.</summary>
    private static void Pine(SKCanvas c, float x, float ground, float height, SKColor colour, Random rng)
        => Pine(c, x, ground, height, colour, rng, PineShape.Classic, 0.05f);

    private enum PineShape
    {
        /// <summary>Four broad tiers with ragged edges.</summary>
        Classic,

        /// <summary>A tall, narrow spruce with many short tiers.</summary>
        Spruce,

        /// <summary>A fuller fir with rounded, drooping tiers.</summary>
        Fir
    }

    /// <summary>
    /// A snowy evergreen. <paramref name="snow"/> is how deep the snow lies on each tier, as a share of the tree's
    /// height (about 0.02 for a dusting, 0.09 for a heavy load, which also adds clumps on the branch tips).
    /// </summary>
    private static void Pine(SKCanvas c, float x, float ground, float height, SKColor colour, Random rng, PineShape shape, float snow)
    {
        var (tiers, width, step, rise) = shape switch
        {
            PineShape.Spruce => (6, 0.22f, 0.135f, 0.26f),
            PineShape.Fir => (4, 0.4f, 0.2f, 0.34f),
            _ => (4, 0.34f, 0.2f, 0.36f)
        };

        using (var trunk = new SKPaint { IsAntialias = true, Color = new SKColor(0x3E, 0x2A, 0x22) })
        {
            c.DrawRect(x - (height * 0.035f), ground - (height * 0.12f), height * 0.07f, height * 0.12f, trunk);
        }

        using var needles = new SKPaint { IsAntialias = true, Color = colour };
        using var clump = new SKPaint { IsAntialias = true, Color = new SKColor(0xF4, 0xF8, 0xFF) };
        for (var i = 0; i < tiers; i++)
        {
            var bottom = ground - (height * 0.08f) - (i * height * step);
            var half = height * width * (1 - (i * (0.8f / tiers)));
            var tip = bottom - (height * (i == tiers - 1 ? rise * 0.95f : rise));
            using var tier = new SKPath();
            tier.MoveTo(x, tip);
            if (shape == PineShape.Fir)
            {
                // Rounded tiers that bulge out and droop at the ends.
                tier.QuadTo(x + (half * 0.75f), tip + ((bottom - tip) * 0.45f), x + half, bottom);
                tier.QuadTo(x, bottom - (height * 0.06f), x - half, bottom);
                tier.QuadTo(x - (half * 0.75f), tip + ((bottom - tip) * 0.45f), x, tip);
            }
            else
            {
                tier.LineTo(x + half, bottom);
                var teeth = shape == PineShape.Spruce ? 3 : 4;
                for (var t = 1; t <= teeth; t++)
                {
                    var tx = x + half - (half * 2 * t / teeth);
                    tier.LineTo(tx + (half / teeth), bottom - (height * R(rng, 0.03f, 0.05f)));
                    tier.LineTo(tx, bottom);
                }
            }

            tier.Close();
            c.DrawPath(tier, needles);
            SnowCap(c, tier, needles, height * snow);

            // Heavy snow sits in clumps on the branch tips.
            if (snow >= 0.07f)
            {
                c.DrawOval(new SKRect(x + half - (height * 0.05f), bottom - (height * 0.035f), x + half + (height * 0.01f), bottom + (height * 0.005f)), clump);
                c.DrawOval(new SKRect(x - half - (height * 0.01f), bottom - (height * 0.035f), x - half + (height * 0.05f), bottom + (height * 0.005f)), clump);
            }
        }
    }

    /// <summary>A silver birch: a white trunk with dark marks and bare branches, snow lying along their tops.</summary>
    private static void Birch(SKCanvas c, float x, float ground, float height, Random rng)
    {
        var tw = height * 0.04f;
        using var bark = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(x - tw, 0), new SKPoint(x + tw, 0), new[] { new SKColor(0xF5, 0xF2, 0xEA), new SKColor(0xC8, 0xC2, 0xB4) }, SKShaderTileMode.Clamp) };
        using var trunk = new SKPath();
        trunk.MoveTo(x - tw, ground);
        trunk.LineTo(x - (tw * 0.35f), ground - height);
        trunk.LineTo(x + (tw * 0.35f), ground - height);
        trunk.LineTo(x + tw, ground);
        trunk.Close();
        c.DrawPath(trunk, bark);

        using var mark = new SKPaint { IsAntialias = true, Color = new SKColor(0x2B, 0x26, 0x24, 220) };
        for (var y = ground - (height * 0.08f); y > ground - (height * 0.9f); y -= height * R(rng, 0.07f, 0.12f))
        {
            var t = (ground - y) / height;
            var half = tw * (1 - (t * 0.65f));
            var side = rng.NextDouble() < 0.5 ? -1 : 1;
            c.DrawRoundRect(new SKRect(side < 0 ? x - half : x + (half * 0.1f), y, side < 0 ? x - (half * 0.1f) : x + half, y + (height * 0.012f)), height * 0.004f, height * 0.004f, mark);
        }

        // Branches angled upward from alternate sides, each with snow along its top edge.
        using var twig = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, Color = new SKColor(0x5D, 0x4A, 0x40) };
        using var snowLine = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, Color = new SKColor(0xF4, 0xF8, 0xFF) };
        var sideOf = 1;
        for (var t = 0.45f; t < 0.95f; t += 0.1f)
        {
            var y = ground - (height * t);
            var len = height * (0.28f - (t * 0.18f));
            var x0 = x + (sideOf * tw * (1 - (t * 0.65f)) * 0.8f);
            var x1 = x0 + (sideOf * len);
            var y1 = y - (len * 0.45f);
            twig.StrokeWidth = height * 0.008f;
            c.DrawLine(x0, y, x1, y1, twig);
            snowLine.StrokeWidth = height * 0.006f;
            c.DrawLine(x0, y - (height * 0.006f), x1 - (sideOf * len * 0.1f), y1 - (height * 0.005f), snowLine);
            sideOf = -sideOf;
        }
    }

    /// <summary>Snowflakes falling across the picture.</summary>
    private static void LightSnow(SKCanvas c, int w, int h, float s, Random rng, int count)
    {
        using var dot = new SKPaint { IsAntialias = true };
        for (var i = 0; i < count; i++)
        {
            dot.Color = SKColors.White.WithAlpha((byte)rng.Next(110, 225));
            c.DrawCircle(R(rng, 0, w), R(rng, 0, h), R(rng, s * 0.002f, s * 0.006f), dot);
        }
    }

    // ---------- Santa's sleigh: the team crossing the moon over snowy hills ----------

    private static void SantaSleigh(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        NightSky(c, w, h, new SKColor(0x05, 0x0B, 0x26, 180), new SKColor(0x12, 0x20, 0x44, 90));
        Stars(c, w, h * 0.6f, 150, s, rng);

        var moon = new SKPoint(w * (w > h ? 0.62f : 0.6f), h * 0.19f);
        var mr = s * 0.17f;
        Moon(c, moon.X, moon.Y, mr, rng);

        // The team crosses the middle of the moon, with a sparkle trail behind (left of) the sleigh.
        var u = s * 0.045f;
        var origin = new SKPoint(moon.X - (u * 3.4f), moon.Y + (u * 1.1f));
        using (var gold = new SKPaint { IsAntialias = true })
        {
            for (var i = 0; i < 40; i++)
            {
                var t = R(rng, 0, 1);
                var tx = origin.X - (u * 1.8f) - (t * s * 0.32f);
                var ty = origin.Y + (u * 0.5f) + (t * t * s * 0.08f) + R(rng, -u * 0.4f, u * 0.4f);
                gold.Color = new SKColor(0xFF, 0xE0, 0x82, (byte)(230 * (1 - (t * 0.7f))));
                c.DrawCircle(tx, ty, u * R(rng, 0.04f, 0.12f), gold);
            }
        }

        c.Save();
        c.Translate(origin.X, origin.Y);
        c.RotateDegrees(-6);
        SleighTeam(c, u);
        c.Restore();

        // Snowy hills with pines.
        using (var back = new SKPaint { IsAntialias = true, Color = new SKColor(0xB8, 0xC8, 0xE2, 230) })
        {
            c.DrawPath(Hills(w, h, h * 0.82f, h * 0.05f, 2, rng), back);
        }

        var count = w > h ? 9 : 6;
        for (var i = 0; i < count; i++)
        {
            var px = (w / (float)count * (i + 0.5f)) + R(rng, -w * 0.03f, w * 0.03f);
            Pine(c, px, h * R(rng, 0.86f, 0.9f), s * R(rng, 0.12f, 0.2f), new SKColor(0x15, 0x33, 0x2A), rng);
        }

        using (var front = new SKPaint { IsAntialias = true, Color = new SKColor(0xF4, 0xF8, 0xFF) })
        {
            c.DrawPath(Hills(w, h, h * 0.92f, h * 0.025f, 3, rng), front);
        }

        LightSnow(c, w, h, s, rng, 70);
    }

    /// <summary>Santa's sleigh with four reindeer in silhouette, flying right; u is about a reindeer's body height.</summary>
    private static void SleighTeam(SKCanvas c, float u)
    {
        using var p = new SKPaint { IsAntialias = true, Color = new SKColor(0x0B, 0x10, 0x26) };
        using var stroke = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round, StrokeWidth = u * 0.1f, Color = p.Color };
        using var rim = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = u * 0.04f, Color = new SKColor(0xFF, 0xF3, 0xC4, 110) };

        // Three reindeer in single file, each a little higher; the reins run from Santa's hands to the first one's
        // rump, then from each deer's chest to the next one's rump, clear of every leg and antler.
        var deer = new[] { new SKPoint(u * 2.8f, -u * 0.1f), new SKPoint(u * 5.3f, -u * 0.4f), new SKPoint(u * 7.8f, -u * 0.7f) };
        using (var reins = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = u * 0.04f, Color = p.Color })
        {
            var from = new SKPoint(u * 0.42f, -u * 0.6f);
            foreach (var d in deer)
            {
                using var line = new SKPath();
                line.MoveTo(from);
                line.QuadTo((from.X + d.X - (u * 0.6f)) / 2, Math.Max(from.Y, d.Y - (u * 0.1f)) + (u * 0.12f), d.X - (u * 0.6f), d.Y - (u * 0.1f));
                c.DrawPath(line, reins);
                from = new SKPoint(d.X + (u * 0.5f), d.Y - (u * 0.18f));
            }
        }

        for (var i = 0; i < deer.Length; i++)
        {
            Reindeer(c, deer[i].X, deer[i].Y, u, i % 2 == 0, p, stroke, rim);
        }

        // Sleigh: a curled runner, the body with a scrolled front, Santa and his sack.
        using (var runner = new SKPath())
        {
            runner.MoveTo(-u * 1.5f, u * 0.55f);
            runner.LineTo(u * 0.9f, u * 0.55f);
            runner.CubicTo(u * 1.4f, u * 0.55f, u * 1.5f, u * 0.05f, u * 1.15f, u * 0.05f);
            runner.CubicTo(u * 0.95f, u * 0.05f, u * 0.95f, u * 0.3f, u * 1.15f, u * 0.3f);
            c.DrawPath(runner, stroke);
            c.DrawPath(runner, rim);
        }

        using (var body = new SKPath())
        {
            body.MoveTo(-u * 1.35f, -u * 0.5f);
            body.LineTo(-u * 1.2f, u * 0.35f);
            body.LineTo(u * 0.65f, u * 0.35f);
            body.CubicTo(u * 1f, u * 0.3f, u * 1.05f, -u * 0.1f, u * 0.8f, -u * 0.2f);
            body.LineTo(u * 0.2f, -u * 0.2f);
            body.LineTo(-u * 0.6f, -u * 0.3f);
            body.Close();
            c.DrawPath(body, p);
            c.DrawPath(body, rim);
        }

        c.DrawLine(-u * 0.9f, u * 0.35f, -u * 0.9f, u * 0.55f, stroke);
        c.DrawLine(u * 0.4f, u * 0.35f, u * 0.4f, u * 0.55f, stroke);

        // Santa: sack behind, round body, head with a beard pointing forward, a bent hat with a bobble, arm out to the reins.
        using (var santa = new SKPath())
        {
            santa.AddOval(new SKRect(-u * 1.45f, -u * 1.15f, -u * 0.6f, -u * 0.2f));
            santa.MoveTo(-u * 1.12f, -u * 1.12f);
            santa.LineTo(-u * 1.0f, -u * 1.38f);
            santa.LineTo(-u * 0.9f, -u * 1.12f);
            santa.Close();
            santa.AddOval(new SKRect(-u * 0.6f, -u * 1.0f, u * 0.25f, -u * 0.12f));
            santa.AddCircle(-u * 0.12f, -u * 1.15f, u * 0.23f);
            santa.MoveTo(u * 0.02f, -u * 1.12f);
            santa.QuadTo(u * 0.3f, -u * 1.0f, u * 0.2f, -u * 0.8f);
            santa.LineTo(-u * 0.05f, -u * 0.95f);
            santa.Close();
            santa.MoveTo(-u * 0.34f, -u * 1.24f);
            santa.QuadTo(-u * 0.26f, -u * 1.7f, -u * 0.78f, -u * 1.58f);
            santa.LineTo(u * 0.1f, -u * 1.26f);
            santa.Close();
            santa.AddCircle(-u * 0.82f, -u * 1.55f, u * 0.1f);
            using (var union = new SKPath())
            {
                santa.Simplify(union);
                c.DrawPath(union, p);
                c.DrawPath(union, rim);
            }
        }

        using (var arm = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = u * 0.16f, Color = p.Color })
        {
            c.DrawLine(-u * 0.05f, -u * 0.72f, u * 0.4f, -u * 0.6f, arm);
        }
    }

    /// <summary>A flying reindeer, legs stretched front and back. (x, y) is the middle of its body.</summary>
    private static void Reindeer(SKCanvas c, float x, float y, float u, bool stride, SKPaint fill, SKPaint stroke, SKPaint rim)
    {
        c.Save();
        c.Translate(x, y);

        using var shape = new SKPath();
        shape.AddOval(new SKRect(-u * 0.62f, -u * 0.26f, u * 0.62f, u * 0.24f));
        shape.MoveTo(u * 0.35f, -u * 0.18f);
        shape.QuadTo(u * 0.6f, -u * 0.4f, u * 0.72f, -u * 0.72f);
        shape.LineTo(u * 0.92f, -u * 0.62f);
        shape.QuadTo(u * 0.7f, -u * 0.25f, u * 0.55f, u * 0.05f);
        shape.Close();
        shape.AddOval(new SKRect(u * 0.66f, -u * 0.86f, u * 1.12f, -u * 0.62f)); // head, muzzle forward
        c.DrawPath(shape, fill);
        c.DrawPath(shape, rim);

        // Legs: front pair reaching forward, back pair kicked out behind; alternate deer swap the stride a little.
        var reach = stride ? 1f : 0.85f;
        foreach (var (hx, hy, kx, ky, fx, fy) in new[]
        {
            (0.4f, 0.15f, 0.75f * reach, 0.32f, 1.0f * reach, 0.18f),
            (0.3f, 0.18f, 0.62f * reach, 0.42f, 0.88f * reach, 0.35f),
            (-0.45f, 0.12f, -0.82f * reach, 0.32f, -1.12f * reach, 0.3f),
            (-0.35f, 0.15f, -0.7f * reach, 0.42f, -1.0f * reach, 0.45f)
        })
        {
            using var leg = new SKPath();
            leg.MoveTo(u * hx, u * hy);
            leg.LineTo(u * kx, u * ky);
            leg.LineTo(u * fx, u * fy);
            c.DrawPath(leg, stroke);
        }

        // Tail, ear and branching antlers.
        c.DrawLine(-u * 0.6f, -u * 0.12f, -u * 0.75f, -u * 0.28f, stroke);
        c.DrawLine(u * 0.75f, -u * 0.82f, u * 0.62f, -u * 0.95f, stroke);
        using var antlers = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = u * 0.06f, Color = fill.Color };
        using var antler = new SKPath();
        antler.MoveTo(u * 0.8f, -u * 0.86f);
        antler.QuadTo(u * 0.7f, -u * 1.25f, u * 0.45f, -u * 1.45f);
        antler.MoveTo(u * 0.72f, -u * 1.1f);
        antler.LineTo(u * 0.9f, -u * 1.3f);
        antler.MoveTo(u * 0.6f, -u * 1.32f);
        antler.LineTo(u * 0.72f, -u * 1.55f);
        antler.MoveTo(u * 0.66f, -u * 1.2f);
        antler.LineTo(u * 0.42f, -u * 1.22f);
        c.DrawPath(antler, antlers);

        c.Restore();
    }

    // ---------- Baubles and holly: a pine garland with hanging baubles, holly and candy canes ----------

    private static void BaublesAndHolly(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        using (var shade = new SKPaint { Shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, h * 0.4f), new[] { new SKColor(0x08, 0x10, 0x0C, 150), SKColors.Transparent }, SKShaderTileMode.Clamp) })
        {
            c.DrawRect(0, 0, w, h * 0.4f, shade);
        }

        // Baubles on ribbons hang from the garland, so they go first.
        var colours = new[] { new SKColor(0xC6, 0x28, 0x28), new SKColor(0xFF, 0xB3, 0x00), new SKColor(0x15, 0x65, 0xC0), new SKColor(0x2E, 0x7D, 0x32), new SKColor(0x8E, 0x24, 0xAA), new SKColor(0xE0, 0xE0, 0xE0) };
        var count = w > h ? 8 : 6;
        for (var i = 0; i < count; i++)
        {
            var bx = (w / (float)count * (i + 0.5f)) + R(rng, -w * 0.02f, w * 0.02f);
            var by = h * (i % 2 == 0 ? R(rng, 0.17f, 0.24f) : R(rng, 0.1f, 0.15f));
            Bauble(c, bx, s * 0.05f, by, s * R(rng, 0.042f, 0.058f), colours[(i + rng.Next(2)) % colours.Length], i % 4, rng);
        }

        Garland(c, w, s, rng);

        // Bottom: holly in both corners, crossed candy canes in the middle.
        Holly(c, w * 0.02f, h * 0.98f, s * 0.2f, -20, rng);
        Holly(c, w * 0.98f, h * 0.98f, s * 0.2f, 200, rng);
        CandyCane(c, (w / 2f) - (s * 0.045f), h * 1.01f, s * 0.22f, false, s);
        CandyCane(c, (w / 2f) + (s * 0.045f), h * 1.01f, s * 0.22f, true, s);
        using (var sparkle = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(200) })
        {
            for (var i = 0; i < 40; i++)
            {
                c.DrawCircle(R(rng, 0, w), R(rng, h * 0.3f, h), R(rng, s * 0.002f, s * 0.004f), sparkle);
            }
        }
    }

    /// <summary>A bushy pine garland draped along the top edge, with red berries.</summary>
    private static void Garland(SKCanvas c, int w, float s, Random rng)
    {
        float Y(float x) => (s * 0.04f) + (float)(Math.Sin(x / w * Math.PI * (w > s ? 3 : 2)) * s * 0.015f);

        var greens = new[] { new SKColor(0x1B, 0x5E, 0x20), new SKColor(0x2E, 0x7D, 0x32), new SKColor(0x38, 0x8E, 0x3C), new SKColor(0x14, 0x48, 0x1A) };
        using var needle = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = s * 0.004f };
        for (var x = -s * 0.02f; x < w + (s * 0.02f); x += s * 0.004f)
        {
            var y = Y(x);
            for (var k = 0; k < 3; k++)
            {
                var a = R(rng, 0, (float)(Math.PI * 2));
                var len = s * R(rng, 0.02f, 0.045f);
                needle.Color = greens[rng.Next(greens.Length)];
                c.DrawLine(x, y, x + (float)(Math.Cos(a) * len), y + (float)(Math.Sin(a) * len * 0.7f), needle);
            }
        }

        using var berry = new SKPaint { IsAntialias = true, Color = new SKColor(0xD3, 0x2F, 0x2F) };
        using var shine = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(180) };
        for (var x = s * 0.05f; x < w; x += s * R(rng, 0.08f, 0.14f))
        {
            for (var k = 0; k < 3; k++)
            {
                float bx = x + R(rng, -s * 0.012f, s * 0.012f), by = Y(x) + R(rng, -s * 0.01f, s * 0.015f), br = s * 0.008f;
                c.DrawCircle(bx, by, br, berry);
                c.DrawCircle(bx - (br * 0.3f), by - (br * 0.3f), br * 0.3f, shine);
            }
        }
    }

    /// <summary>A glass bauble on a ribbon from <paramref name="hangY"/>: plain, striped, glittered or a teardrop.</summary>
    private static void Bauble(SKCanvas c, float x, float hangY, float y, float r, SKColor colour, int style, Random rng)
    {
        using (var ribbon = new SKPaint { IsAntialias = true, StrokeWidth = r * 0.06f, Color = new SKColor(0xFF, 0xD5, 0x4F, 220) })
        {
            c.DrawLine(x, hangY, x, y - r, ribbon);
        }

        using var shape = new SKPath();
        if (style == 3)
        {
            shape.MoveTo(x, y - r);
            shape.CubicTo(x + (r * 1.1f), y - (r * 0.2f), x + (r * 0.6f), y + (r * 1.4f), x, y + (r * 2f));
            shape.CubicTo(x - (r * 0.6f), y + (r * 1.4f), x - (r * 1.1f), y - (r * 0.2f), x, y - r);
            shape.Close();
        }
        else
        {
            shape.AddCircle(x, y, r);
        }

        using (var glass = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(new SKPoint(x - (r * 0.35f), y - (r * 0.4f)), r * 1.6f, new[] { Lighten(colour, 0.45f), colour, Darken(colour, 0.45f) }, new[] { 0f, 0.45f, 1f }, SKShaderTileMode.Clamp),
            ImageFilter = SKImageFilter.CreateDropShadow(0, r * 0.08f, r * 0.12f, r * 0.12f, SKColors.Black.WithAlpha(120))
        })
        {
            c.DrawPath(shape, glass);
        }

        c.Save();
        c.ClipPath(shape, antialias: true);
        if (style == 1)
        {
            using var band = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(200) };
            using var zig = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = r * 0.08f, Color = new SKColor(0xFF, 0xD5, 0x4F) };
            c.DrawRect(x - r, y - (r * 0.12f), r * 2, r * 0.24f, band);
            using var wave = new SKPath();
            wave.MoveTo(x - r, y - (r * 0.45f));
            for (var k = 0; k < 8; k++)
            {
                wave.LineTo(x - r + ((k + 0.5f) * r / 4), y - (r * (k % 2 == 0 ? 0.6f : 0.45f)));
            }

            c.DrawPath(wave, zig);
            c.Translate(0, r * 0.9f);
            c.DrawPath(wave, zig);
        }
        else if (style == 2)
        {
            using var glitter = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(210) };
            for (var k = 0; k < 18; k++)
            {
                c.DrawCircle(x + R(rng, -r, r), y + R(rng, -r, r), r * R(rng, 0.03f, 0.07f), glitter);
            }
        }

        c.Restore();

        using (var highlight = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(150) })
        {
            c.Save();
            c.RotateDegrees(-35, x - (r * 0.4f), y - (r * 0.45f));
            c.DrawOval(new SKRect(x - (r * 0.6f), y - (r * 0.58f), x - (r * 0.2f), y - (r * 0.34f)), highlight);
            c.Restore();
        }

        // Metal cap and loop.
        using var metal = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(x - (r * 0.25f), 0), new SKPoint(x + (r * 0.25f), 0), new[] { new SKColor(0xFF, 0xF1, 0xB8), new SKColor(0xB8, 0x8A, 0x1E) }, SKShaderTileMode.Clamp) };
        c.DrawRoundRect(new SKRect(x - (r * 0.25f), y - r - (r * 0.2f), x + (r * 0.25f), y - r + (r * 0.08f)), r * 0.05f, r * 0.05f, metal);
        using var loop = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = r * 0.05f, Color = new SKColor(0xB8, 0x8A, 0x1E) };
        c.DrawCircle(x, y - r - (r * 0.28f), r * 0.1f, loop);
    }

    /// <summary>A sprig of three spiky holly leaves and berries, growing from (x, y) at <paramref name="rotation"/>.</summary>
    private static void Holly(SKCanvas c, float x, float y, float size, float rotation, Random rng)
    {
        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(rotation);

        using var leaf = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, -size * 0.2f), new SKPoint(0, size * 0.2f), new[] { new SKColor(0x38, 0x8E, 0x3C), new SKColor(0x1B, 0x5E, 0x20) }, SKShaderTileMode.Clamp) };
        using var vein = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = size * 0.012f, Color = new SKColor(0x9C, 0xCC, 0x65, 200) };
        foreach (var angle in new[] { -30f, 5f, 40f })
        {
            c.Save();
            c.RotateDegrees(angle);
            using var path = new SKPath();
            path.MoveTo(0, 0);
            const int Points = 4;
            for (var k = 1; k <= Points; k++)
            {
                var px = size * k / Points;
                var bulge = (float)Math.Sin(Math.PI * k / Points) * size * 0.2f;
                path.QuadTo(px - (size * 0.06f), -bulge * 0.6f, px, k == Points ? 0 : -bulge);
            }

            for (var k = Points - 1; k >= 0; k--)
            {
                var px = size * k / Points;
                var bulge = (float)Math.Sin(Math.PI * k / Points) * size * 0.2f;
                path.QuadTo(px + (size * 0.06f), bulge * 0.6f, px, bulge);
            }

            path.Close();
            c.DrawPath(path, leaf);
            c.DrawLine(0, 0, size * 0.95f, 0, vein);
            c.Restore();
        }

        using var berry = new SKPaint { IsAntialias = true, Color = new SKColor(0xD3, 0x2F, 0x2F), ImageFilter = SKImageFilter.CreateDropShadow(0, size * 0.01f, size * 0.015f, size * 0.015f, SKColors.Black.WithAlpha(120)) };
        using var shine = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(190) };
        foreach (var (bx, by) in new[] { (0.05f, -0.05f), (0.12f, 0.04f), (0.02f, 0.08f) })
        {
            var br = size * R(rng, 0.055f, 0.065f);
            c.DrawCircle(size * bx, size * by, br, berry);
            c.DrawCircle((size * bx) - (br * 0.3f), (size * by) - (br * 0.3f), br * 0.3f, shine);
        }

        c.Restore();
    }

    /// <summary>A striped candy cane standing upright on its foot at (x, ground), the hook curling outwards (left, or right when <paramref name="mirror"/>).</summary>
    private static void CandyCane(SKCanvas c, float x, float ground, float height, bool mirror, float s)
    {
        c.Save();
        c.Translate(x, ground);
        c.Scale(mirror ? 1 : -1, 1);
        var hook = height * 0.16f;
        using var cane = new SKPath();
        cane.MoveTo(0, 0);
        cane.LineTo(0, -height + hook);
        cane.ArcTo(new SKRect(0, -height, hook * 2, -height + (hook * 2)), 180, 180, false);
        cane.LineTo(hook * 2, -height + (hook * 1.6f));

        var width = height * 0.09f;
        using var white = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = width, Color = new SKColor(0xFA, 0xFA, 0xFA), ImageFilter = SKImageFilter.CreateDropShadow(0, s * 0.004f, s * 0.006f, s * 0.006f, SKColors.Black.WithAlpha(120)) };
        c.DrawPath(cane, white);
        using var dash = SKPathEffect.CreateDash(new[] { width * 0.7f, width * 0.7f }, 0);
        using var red = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = width, Color = new SKColor(0xD3, 0x2F, 0x2F), PathEffect = dash };
        c.DrawPath(cane, red);
        using var gloss = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = width * 0.18f, Color = SKColors.White.WithAlpha(130) };
        c.DrawLine(-width * 0.22f, -height * 0.05f, -width * 0.22f, -height + hook, gloss);
        c.Restore();
    }

    // ---------- Snowy forest: layered pines under the northern lights ----------

    private static void SnowyForest(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        NightSky(c, w, h, new SKColor(0x04, 0x10, 0x24, 170), new SKColor(0x10, 0x2A, 0x40, 70));
        Stars(c, w, h * 0.55f, 120, s, rng);
        Aurora(c, w, h, s, rng);

        using (var far = new SKPaint { IsAntialias = true, Color = new SKColor(0xC9, 0xD6, 0xEA, 230) })
        {
            c.DrawPath(Hills(w, h, h * 0.8f, h * 0.05f, 3, rng), far);
        }

        // A misty back row in cool blue-greens, then a front row of mixed shapes and snow, with the odd birch.
        var shapes = new[] { PineShape.Classic, PineShape.Spruce, PineShape.Fir };
        var backGreens = new[] { new SKColor(0x2E, 0x4A, 0x5E), new SKColor(0x35, 0x55, 0x66), new SKColor(0x2A, 0x45, 0x52) };
        for (var x = R(rng, 0, s * 0.05f); x < w; x += s * R(rng, 0.07f, 0.11f))
        {
            Pine(c, x, h * R(rng, 0.84f, 0.87f), s * R(rng, 0.1f, 0.17f), backGreens[rng.Next(backGreens.Length)], rng, shapes[rng.Next(shapes.Length)], R(rng, 0.03f, 0.08f));
        }

        var frontGreens = new[] { new SKColor(0x16, 0x33, 0x26), new SKColor(0x1E, 0x40, 0x2E), new SKColor(0x12, 0x2C, 0x28), new SKColor(0x23, 0x3F, 0x24) };
        var lastWasBirch = true;
        for (var x = R(rng, 0, s * 0.08f); x < w; x += s * R(rng, 0.15f, 0.22f))
        {
            var ground = h * R(rng, 0.95f, 0.98f);
            if (!lastWasBirch && rng.NextDouble() < 0.22)
            {
                Birch(c, x, ground, s * R(rng, 0.24f, 0.3f), rng);
                lastWasBirch = true;
                continue;
            }

            lastWasBirch = false;
            var shape = shapes[rng.Next(shapes.Length)];
            var height = s * (shape == PineShape.Spruce ? R(rng, 0.24f, 0.3f) : R(rng, 0.18f, 0.25f));
            Pine(c, x, ground, height, frontGreens[rng.Next(frontGreens.Length)], rng, shape, R(rng, 0.02f, 0.1f));
        }

        using (var drift = new SKPaint { IsAntialias = true, Color = new SKColor(0xF4, 0xF8, 0xFF) })
        {
            c.DrawPath(Hills(w, h, h * 0.96f, h * 0.012f, 4, rng), drift);
        }

        LightSnow(c, w, h, s, rng, 110);
    }

    /// <summary>Northern lights: soft curtains of green, teal and violet across the top of the sky.</summary>
    private static void Aurora(SKCanvas c, int w, int h, float s, Random rng)
    {
        var bands = new[] { new SKColor(0x4C, 0xFF, 0xA0), new SKColor(0x26, 0xC6, 0xDA), new SKColor(0xB3, 0x88, 0xFF) };
        using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.008f);
        for (var b = 0; b < bands.Length; b++)
        {
            var baseY = h * (0.15f + (b * 0.04f));
            var amp = h * R(rng, 0.03f, 0.06f);
            var len = w * R(rng, 0.35f, 0.6f);
            var phase = R(rng, 0, len);
            var curtain = s * R(rng, 0.12f, 0.2f);
            using var band = new SKPath();
            band.MoveTo(0, baseY);
            for (float x = 0; x <= w; x += 6)
            {
                band.LineTo(x, baseY + (float)(Math.Sin((x + phase) / len * Math.PI * 2) * amp));
            }

            for (float x = w; x >= 0; x -= 6)
            {
                band.LineTo(x, baseY - curtain + (float)(Math.Sin((x + phase) / len * Math.PI * 2) * amp * 1.3));
            }

            band.Close();
            using var light = new SKPaint
            {
                IsAntialias = true,
                MaskFilter = blur,
                Shader = SKShader.CreateLinearGradient(new SKPoint(0, baseY - curtain - amp), new SKPoint(0, baseY + amp), new[] { SKColors.Transparent, bands[b].WithAlpha(90), bands[b].WithAlpha(160) }, new[] { 0f, 0.7f, 1f }, SKShaderTileMode.Clamp)
            };
            c.DrawPath(band, light);
        }
    }
}
