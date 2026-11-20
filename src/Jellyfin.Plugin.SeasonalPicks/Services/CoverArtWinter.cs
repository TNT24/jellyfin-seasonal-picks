using System;
using SkiaSharp;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>The Christmas and winter scenes: tree, fireplace, sleigh, baubles and holly, snowy forest.</summary>
internal static partial class CoverArt
{
    // ---------- Christmas tree: a lit tree and presents in the snow beside a cottage, lights strung overhead ----------

    /// <summary>Drawn over <see cref="Snow"/>'s backdrop (the sky and far hills).</summary>
    private static void ChristmasTree(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var wide = w > h;

        // Fairy lights strung across the top, framing the title from above.
        var swags = wide ? 3 : 2;
        for (var i = 0; i < swags; i++)
        {
            var from = new SKPoint((w * i / (float)swags) - (s * 0.02f), h * (i % 2 == 0 ? 0.015f : 0.03f));
            var to = new SKPoint((w * (i + 1) / (float)swags) + (s * 0.02f), h * (i % 2 == 0 ? 0.03f : 0.015f));
            WinterLightString(c, from, to, h * 0.075f, wide ? 9 : 10, s, rng);
        }

        // A snowy rise with a cottage and pines on the left.
        var mid = WinterRidgeLine(h * 0.84f, h * 0.03f, s, 0.6f, rng);
        using (WinterRidge(c, w, h, mid, new SKColor(0xB4, 0xC8, 0xE6), new SKColor(0x86, 0x9E, 0xC8), new SKColor(0xFF, 0xFF, 0xFF, 160), s))
        {
        }

        var cottageX = w * (wide ? 0.2f : 0.24f);
        var cottageSize = s * 0.25f;
        var behind = cottageX - (cottageSize * 0.72f);
        WinterPine(c, behind, mid(behind) + (s * 0.01f), s * 0.22f, new SKColor(0x12, 0x30, 0x26), new SKColor(0x2C, 0x5A, 0x44), WinterSnowWhite, 0.06f, rng, 6);
        if (wide)
        {
            var far = cottageX + (cottageSize * 0.95f);
            WinterPine(c, far, mid(far) + (s * 0.01f), s * 0.15f, new SKColor(0x1E, 0x3A, 0x48), new SKColor(0x3C, 0x60, 0x6E), new SKColor(0xDE, 0xEA, 0xFA), 0.05f, rng, 5);
        }

        WinterCottage(c, cottageX, mid(cottageX) + (s * 0.014f), cottageSize, rng, s, chimneyRight: false);

        // Fresh snow in front, then the tree with its presents.
        var near = WinterRidgeLine(h * 0.935f, h * 0.018f, s, 0.5f, rng);
        using (WinterRidge(c, w, h, near, WinterSnowWhite, new SKColor(0xC4, 0xD4, 0xEE), SKColors.Transparent, s))
        {
        }

        var snowmanX = w * (wide ? 0.5f : 0.49f);
        WinterSnowman(c, snowmanX, near(snowmanX) + (s * 0.02f), s * (wide ? 0.17f : 0.15f));

        var treeX = w * (wide ? 0.8f : 0.74f);
        WinterChristmasTree(c, treeX, h * 0.965f, h * (wide ? 0.39f : 0.36f), s, rng, 0.02f, snowyPresents: true);
        WinterGlints(c, w, h, near, s, rng, 40);
        WinterSnowfall(c, w, h, s, rng, wide ? 120 : 90);
    }

    /// <summary>
    /// A lush Christmas tree: lights swagged round each tier, gold bead garlands, glossy baubles, a glowing star and
    /// presents round the foot. (x, ground) is the foot of the trunk.
    /// </summary>
    private static void WinterChristmasTree(SKCanvas c, float x, float ground, float height, float s, Random rng, float snow, bool snowyPresents)
    {
        WinterGlow(c, x, ground - (height * 0.45f), height * 0.9f, new SKColor(0xFF, 0xC0, 0x60, 75), add: true);
        var tiers = WinterPine(c, x, ground, height, new SKColor(0x06, 0x30, 0x1A), new SKColor(0x2E, 0x84, 0x4E), WinterSnowWhite, snow, rng, 7, 0.42f);

        var baubleColours = new[] { new SKColor(0xD3, 0x22, 0x2A), new SKColor(0xF2, 0xB2, 0x2E), new SKColor(0x2A, 0x6C, 0xD8), new SKColor(0xC8, 0xD2, 0xDC), new SKColor(0xA0, 0x1C, 0x48) };
        var lights = new System.Collections.Generic.List<(float X, float Y, SKColor Colour)>();
        using var bead = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xD7, 0x6A) };
        var colourIndex = rng.Next(baubleColours.Length);
        for (var i = 0; i < tiers.Count; i++)
        {
            var (bottom, half, tip) = tiers[i];
            var bandTop = i + 1 < tiers.Count ? tiers[i + 1].Bottom : tip + ((bottom - tip) * 0.45f);
            float WidthAt(float y) => half * Math.Clamp((y - tip) / (bottom - tip), 0, 1) * 0.9f;

            // A swag across the visible part of the tier, sloping alternately.
            var yA = bandTop + ((bottom - bandTop) * (i % 2 == 0 ? 0.15f : 0.55f));
            var yB = bandTop + ((bottom - bandTop) * (i % 2 == 0 ? 0.55f : 0.15f));
            var from = new SKPoint(x - WidthAt(yA), yA);
            var to = new SKPoint(x + WidthAt(yB), yB);
            var ctrl = new SKPoint(x, ((yA + yB) / 2) + ((bottom - bandTop) * 0.45f));
            SKPoint Along(float t) => new(
                ((1 - t) * (1 - t) * from.X) + (2 * (1 - t) * t * ctrl.X) + (t * t * to.X),
                ((1 - t) * (1 - t) * from.Y) + (2 * (1 - t) * t * ctrl.Y) + (t * t * to.Y));

            if (i % 2 == 1 && i < tiers.Count - 1)
            {
                var beads = (int)((to.X - from.X) / (height * 0.012f));
                for (var k = 0; k <= beads; k++)
                {
                    var p = Along(k / (float)Math.Max(1, beads));
                    c.DrawCircle(p.X, p.Y + (height * 0.012f), height * 0.0055f, bead);
                }
            }

            var bulbs = Math.Max(2, (int)((to.X - from.X) / (height * 0.06f)));
            for (var k = 0; k < bulbs; k++)
            {
                var p = Along((k + 0.5f) / bulbs);
                lights.Add((p.X + R(rng, -height * 0.01f, height * 0.01f), p.Y + R(rng, -height * 0.01f, height * 0.01f), WinterBulbColours[rng.Next(WinterBulbColours.Length)]));
            }

            // Baubles hanging along the lower edge.
            if (i < tiers.Count - 1)
            {
                var count = Math.Max(1, (int)(half / (height * 0.1f)));
                for (var k = 0; k < count; k++)
                {
                    var bx = x - (half * 0.72f) + (half * 1.44f * (k + 0.5f) / count) + R(rng, -height * 0.02f, height * 0.02f);
                    var by = bottom - (height * R(rng, 0.0f, 0.02f));
                    WinterBauble(c, bx, by, height * R(rng, 0.026f, 0.036f), baubleColours[colourIndex++ % baubleColours.Length], rng.Next(3), rng);
                }
            }
        }

        foreach (var (lx, ly, colour) in lights)
        {
            WinterBulb(c, lx, ly, height * 0.0065f, colour, 120);
        }

        WinterTopStar(c, x, ground - height - (height * 0.005f), height * 0.055f);

        // Presents, back row first.
        var u = height;
        WinterPresent(c, x - (u * 0.3f), ground, u * 0.2f, u * 0.15f, new SKColor(0xC6, 0x28, 0x28), new SKColor(0xFF, 0xD0, 0x50), snowyPresents);
        WinterPresent(c, x + (u * 0.24f), ground, u * 0.17f, u * 0.2f, new SKColor(0x1E, 0x5B, 0xB8), new SKColor(0xE8, 0xEE, 0xF8), snowyPresents);
        WinterPresent(c, x + (u * 0.42f), ground + (u * 0.012f), u * 0.12f, u * 0.09f, new SKColor(0x2E, 0x7D, 0x32), new SKColor(0xE5, 0x39, 0x35), snowyPresents);
        WinterPresent(c, x - (u * 0.1f), ground + (u * 0.02f), u * 0.15f, u * 0.1f, new SKColor(0xF2, 0xE6, 0xCE), new SKColor(0xC6, 0x28, 0x28), snowyPresents);
        WinterPresent(c, x + (u * 0.08f), ground + (u * 0.03f), u * 0.13f, u * 0.07f, new SKColor(0x8E, 0x24, 0xAA), new SKColor(0xFF, 0xD0, 0x50), snowyPresents);
    }

    // ---------- Fireplace: a stone fireplace with a roaring fire, stockings and candles on the mantel ----------

    private static void Fireplace(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var wide = w > h;
        var cx = w / 2f;
        var floor = h * 0.975f;
        var fw = wide ? s * 0.92f : w * 0.8f;
        var mantelY = h * 0.69f;

        // A warm, dim room lit by the fire.
        WinterWash(c, w, 0, h, new[] { new SKColor(0x10, 0x06, 0x04, 210), new SKColor(0x1E, 0x0C, 0x06, 150), new SKColor(0x2A, 0x10, 0x06, 170) }, new[] { 0f, 0.55f, 1f });
        WinterGlow(c, cx, floor - (s * 0.12f), s * 1.0f, new SKColor(0xFF, 0x86, 0x2C, 95), add: true);
        WinterWash(c, w, floor, h, new[] { new SKColor(0x4A, 0x2A, 0x16), new SKColor(0x2A, 0x16, 0x0C) });

        // Faint striped wallpaper.
        using (var stripe = new SKPaint { Color = new SKColor(0xFF, 0xE0, 0xB0, 7) })
        {
            for (var x = s * 0.02f; x < w; x += s * 0.07f)
            {
                c.DrawRect(x, 0, s * 0.025f, floor, stripe);
            }
        }

        // A garland of greenery and warm lights swagged along the top, tied with a bow in the middle.
        foreach (var (x0, x1) in new[] { (-0.03f, 0.5f), (0.5f, 1.03f) })
        {
            var swag = new SKPoint[12];
            for (var i = 0; i < swag.Length; i++)
            {
                var t = i / (float)(swag.Length - 1);
                swag[i] = new SKPoint(w * (x0 + ((x1 - x0) * t)), (h * 0.015f) + ((float)Math.Sin(t * Math.PI) * h * 0.075f));
            }

            WinterBough(c, swag, s * 0.07f, rng, 0.9f);
            for (var i = 1; i < swag.Length - 1; i++)
            {
                WinterBulb(c, swag[i].X + R(rng, -s * 0.01f, s * 0.01f), swag[i].Y + (s * 0.012f), s * 0.005f, new SKColor(0xFF, 0xD8, 0x90), 120);
            }
        }

        WinterBow(c, w / 2f, h * 0.03f, s * 0.07f, new SKColor(0xC6, 0x1E, 0x2A));

        if (wide)
        {
            WinterWindowView(c, new SKRect(w * 0.035f, h * 0.1f, w * 0.19f, h * 0.62f), s, rng);
        }

        // Stone surround with the firebox in an arched opening.
        var surround = new SKRect(cx - (fw / 2), mantelY + (s * 0.03f), cx + (fw / 2), floor);
        using (var shader = SKShader.CreateLinearGradient(new SKPoint(0, surround.Top), new SKPoint(0, surround.Bottom), new[] { new SKColor(0x9A, 0x86, 0x6E), new SKColor(0x6A, 0x58, 0x46) }, null, SKShaderTileMode.Clamp))
        using (var stone = new SKPaint { IsAntialias = true, Shader = shader })
        {
            c.DrawRect(surround, stone);
        }

        // Stone blocks.
        c.Save();
        c.ClipRect(surround);
        using (var joint = new SKPaint { IsAntialias = true, StrokeWidth = Math.Max(1f, s * 0.003f), Color = new SKColor(0x3A, 0x2C, 0x22, 150) })
        {
            var row = 0;
            var rowH = s * 0.045f;
            for (var y = surround.Top + rowH; y < surround.Bottom + rowH; y += rowH, row++)
            {
                c.DrawLine(surround.Left, y, surround.Right, y, joint);
                var bw = s * 0.09f;
                for (var x = surround.Left + (row % 2 == 0 ? 0 : bw / 2) + R(rng, 0, bw * 0.2f); x < surround.Right; x += bw * R(rng, 0.8f, 1.2f))
                {
                    c.DrawLine(x, y - rowH, x, y, joint);
                }
            }
        }

        c.Restore();

        var opening = new SKRect(cx - (fw * 0.26f), surround.Top + (s * 0.08f), cx + (fw * 0.26f), floor - (s * 0.015f));
        using var arch = new SKPath();
        arch.MoveTo(opening.Left, opening.Bottom);
        arch.LineTo(opening.Left, opening.Top + (opening.Height * 0.25f));
        arch.QuadTo(opening.MidX, opening.Top - (opening.Height * 0.15f), opening.Right, opening.Top + (opening.Height * 0.25f));
        arch.LineTo(opening.Right, opening.Bottom);
        arch.Close();
        using (var trim = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.022f, StrokeJoin = SKStrokeJoin.Round, Color = new SKColor(0xB8, 0xA6, 0x8E) })
        {
            c.DrawPath(arch, trim);
        }

        using (var shader = SKShader.CreateLinearGradient(new SKPoint(0, opening.Top), new SKPoint(0, opening.Bottom), new[] { new SKColor(0x08, 0x05, 0x04), new SKColor(0x2A, 0x12, 0x08) }, null, SKShaderTileMode.Clamp))
        using (var soot = new SKPaint { IsAntialias = true, Shader = shader })
        {
            c.DrawPath(arch, soot);
        }

        c.Save();
        c.ClipPath(arch, antialias: true);
        WinterFire(c, opening, s, rng);
        c.Restore();

        // Firelight on the stone around the opening.
        c.Save();
        c.ClipRect(surround);
        WinterGlow(c, cx, opening.Bottom - (opening.Height * 0.3f), fw * 0.55f, new SKColor(0xFF, 0x80, 0x30, 70), add: true);
        c.Restore();

        // Hearth slab.
        using (var shader = SKShader.CreateLinearGradient(new SKPoint(0, floor - (s * 0.02f)), new SKPoint(0, h), new[] { new SKColor(0xB0, 0x9C, 0x84), new SKColor(0x6A, 0x5A, 0x4A) }, null, SKShaderTileMode.Clamp))
        using (var slab = new SKPaint { IsAntialias = true, Shader = shader })
        {
            c.DrawRect(surround.Left - (s * 0.03f), floor - (s * 0.018f), fw + (s * 0.06f), h - floor + (s * 0.02f), slab);
        }

        // Mantel shelf, lit from below by the fire.
        var shelf = new SKRect(cx - (fw * 0.56f), mantelY - (s * 0.012f), cx + (fw * 0.56f), mantelY + (s * 0.035f));
        using (var shader = SKShader.CreateLinearGradient(new SKPoint(0, shelf.Top), new SKPoint(0, shelf.Bottom), new[] { new SKColor(0x7A, 0x4A, 0x2A), new SKColor(0x4A, 0x28, 0x14), new SKColor(0x9A, 0x5A, 0x2A) }, new[] { 0f, 0.6f, 1f }, SKShaderTileMode.Clamp))
        using (var wood = new SKPaint { IsAntialias = true, Shader = shader, ImageFilter = SKImageFilter.CreateDropShadow(0, s * 0.01f, s * 0.01f, s * 0.01f, SKColors.Black.WithAlpha(150)) })
        {
            c.DrawRoundRect(shelf, s * 0.005f, s * 0.005f, wood);
        }

        // Candles at both ends of the mantel, a little garland of greenery and lights along its front.
        foreach (var (dx, ch) in new[] { (-0.5f, 0.09f), (-0.44f, 0.06f), (0.44f, 0.075f), (0.5f, 0.1f) })
        {
            WinterCandle(c, cx + (fw * dx), shelf.Top, s * ch, s * 0.028f);
        }

        var spine = new SKPoint[17];
        for (var i = 0; i < spine.Length; i++)
        {
            var t = i / (float)(spine.Length - 1);
            var sx = shelf.Left + (shelf.Width * 0.06f) + (shelf.Width * 0.88f * t);
            var swing = (float)Math.Abs(Math.Sin(t * Math.PI * 4));
            spine[i] = new SKPoint(sx, shelf.Bottom + (s * 0.005f) + (swing * s * 0.035f));
        }

        WinterBough(c, spine, s * 0.06f, rng, 0.8f);
        for (var i = 0; i < 26; i++)
        {
            var t = (i + 0.5f) / 26;
            var at = spine[Math.Min(spine.Length - 1, (int)(t * (spine.Length - 1)))];
            WinterBulb(c, at.X + R(rng, -s * 0.01f, s * 0.01f), at.Y + R(rng, -s * 0.01f, s * 0.01f), s * 0.0045f, new SKColor(0xFF, 0xD8, 0x90), 110);
        }

        // Stockings hung from the shelf over the stone either side of the fire, toes outwards.
        var stockings = new[]
        {
            (X: -0.455f, Body: new SKColor(0xC6, 0x28, 0x28), Trim: new SKColor(0x2E, 0x7D, 0x32), Pattern: 0),
            (X: -0.325f, Body: new SKColor(0x23, 0x6B, 0x3A), Trim: new SKColor(0xC6, 0x28, 0x28), Pattern: 1),
            (X: 0.325f, Body: new SKColor(0x1E, 0x4E, 0x9C), Trim: new SKColor(0xF2, 0xC2, 0x4A), Pattern: 2),
            (X: 0.455f, Body: new SKColor(0xB0, 0x1E, 0x2A), Trim: new SKColor(0xF2, 0xF2, 0xF2), Pattern: 1)
        };
        foreach (var st in stockings)
        {
            WinterStocking(c, cx + (fw * st.X), shelf.Bottom + (s * 0.01f), Math.Min(s * 0.15f, fw * 0.17f), st.Body, st.Trim, st.X > 0, st.Pattern);
        }

        if (wide)
        {
            WinterChristmasTree(c, w * 0.875f, floor + (s * 0.005f), h * 0.52f, s, rng, 0, snowyPresents: false);
            WinterPresent(c, w * 0.09f, floor + (s * 0.01f), s * 0.11f, s * 0.08f, new SKColor(0x2E, 0x7D, 0x32), new SKColor(0xE5, 0x39, 0x35));
            WinterPresent(c, w * 0.16f, floor + (s * 0.015f), s * 0.085f, s * 0.11f, new SKColor(0xC6, 0x28, 0x28), new SKColor(0xFF, 0xD0, 0x50));
        }
        else
        {
            WinterPresent(c, w * 0.045f, floor + (s * 0.012f), s * 0.07f, s * 0.09f, new SKColor(0xC6, 0x28, 0x28), new SKColor(0xFF, 0xD0, 0x50));
            WinterPresent(c, w * 0.955f, floor + (s * 0.012f), s * 0.07f, s * 0.06f, new SKColor(0x2E, 0x7D, 0x32), new SKColor(0xE5, 0x39, 0x35));
        }

        // Dark vignette at the top corners so the room closes in round the title.
        using (var vignette = SKShader.CreateRadialGradient(new SKPoint(cx, h * 0.62f), Math.Max(w, h) * 0.8f, new[] { SKColors.Transparent, SKColors.Transparent, new SKColor(0x06, 0x02, 0x01, 150) }, new[] { 0f, 0.6f, 1f }, SKShaderTileMode.Clamp))
        using (var dark = new SKPaint { Shader = vignette })
        {
            c.DrawRect(0, 0, w, h, dark);
        }
    }

    /// <summary>A crackling log fire filling <paramref name="box"/> (the firebox opening): embers, logs and layered flames.</summary>
    private static void WinterFire(SKCanvas c, SKRect box, float s, Random rng)
    {
        var baseY = box.Bottom - (box.Height * 0.16f);
        WinterGlow(c, box.MidX, baseY - (box.Height * 0.25f), box.Width * 0.7f, new SKColor(0xFF, 0x80, 0x28, 140), add: true);

        // Brick back wall picked out by the glow.
        using (var brick = new SKPaint { IsAntialias = true, StrokeWidth = Math.Max(1f, s * 0.002f), Color = new SKColor(0xFF, 0x90, 0x40, 40) })
        {
            var row = 0;
            for (var y = box.Top; y < baseY; y += s * 0.022f, row++)
            {
                c.DrawLine(box.Left, y, box.Right, y, brick);
                for (var x = box.Left + (row % 2 == 0 ? 0 : s * 0.025f); x < box.Right; x += s * 0.05f)
                {
                    c.DrawLine(x, y, x, y + (s * 0.022f), brick);
                }
            }
        }

        WinterGlowOval(c, box.MidX, box.Bottom - (box.Height * 0.08f), box.Width * 0.5f, box.Height * 0.14f, new SKColor(0xFF, 0x6A, 0x18, 220), add: true);

        void Flames(SKColor colour, float scale, float widthScale, int count, byte alpha, bool add = true)
        {
            for (var k = 0; k < count; k++)
            {
                var off = (k - ((count - 1) / 2f)) / Math.Max(1, (count - 1) / 2f);
                var fx = box.MidX + (off * box.Width * 0.32f) + R(rng, -box.Width * 0.03f, box.Width * 0.03f);
                var fh = box.Height * 0.85f * scale * (1 - (Math.Abs(off) * 0.45f)) * R(rng, 0.65f, 1.05f);
                var fwid = box.Width * 0.11f * widthScale;
                var lean = R(rng, -fwid * 0.5f, fwid * 0.5f);
                using var flame = new SKPath();
                flame.MoveTo(fx - (fwid / 2), baseY);
                flame.CubicTo(fx - (fwid * 0.75f), baseY - (fh * 0.45f), fx + (lean * 0.3f) - (fwid * 0.1f), baseY - (fh * 0.7f), fx + lean, baseY - fh);
                flame.CubicTo(fx + (lean * 0.3f) + (fwid * 0.2f), baseY - (fh * 0.55f), fx + (fwid * 0.8f), baseY - (fh * 0.35f), fx + (fwid / 2), baseY);
                flame.Close();
                using var shader = SKShader.CreateLinearGradient(new SKPoint(0, baseY), new SKPoint(0, baseY - fh), new[] { colour.WithAlpha(alpha), colour.WithAlpha((byte)(alpha * 0.8f)), colour.WithAlpha(0) }, new[] { 0f, 0.55f, 1f }, SKShaderTileMode.Clamp);
                using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, fwid * 0.06f);
                using var paint = new SKPaint { IsAntialias = true, Shader = shader, MaskFilter = blur, BlendMode = add ? SKBlendMode.Plus : SKBlendMode.SrcOver };
                c.DrawPath(flame, paint);
            }
        }

        // Tall red-orange tongues behind, the crossed logs, then brighter flames licking over them; the front log last.
        Flames(new SKColor(0xE8, 0x40, 0x10), 1f, 1.3f, 7, 230, add: false);
        WinterLog(c, new SKPoint(box.MidX - (box.Width * 0.36f), baseY + (box.Height * 0.02f)), new SKPoint(box.MidX + (box.Width * 0.12f), baseY - (box.Height * 0.06f)), box.Height * 0.11f);
        WinterLog(c, new SKPoint(box.MidX + (box.Width * 0.36f), baseY + (box.Height * 0.02f)), new SKPoint(box.MidX - (box.Width * 0.1f), baseY - (box.Height * 0.07f)), box.Height * 0.11f);
        Flames(new SKColor(0xFF, 0x8A, 0x20), 0.75f, 1.1f, 6, 170);
        Flames(new SKColor(0xFF, 0xE0, 0x90), 0.42f, 0.7f, 5, 150);
        WinterLog(c, new SKPoint(box.MidX - (box.Width * 0.4f), box.Bottom - (box.Height * 0.06f)), new SKPoint(box.MidX + (box.Width * 0.4f), box.Bottom - (box.Height * 0.07f)), box.Height * 0.12f);

        // Sparks rising.
        using var spark = new SKPaint { IsAntialias = true, BlendMode = SKBlendMode.Plus };
        for (var i = 0; i < 18; i++)
        {
            spark.Color = new SKColor(0xFF, (byte)rng.Next(150, 220), 0x60, (byte)rng.Next(150, 255));
            c.DrawCircle(box.MidX + R(rng, -box.Width * 0.3f, box.Width * 0.3f), R(rng, box.Top + (box.Height * 0.15f), baseY - (box.Height * 0.3f)), R(rng, s * 0.0015f, s * 0.003f), spark);
        }
    }

    /// <summary>A log from <paramref name="a"/> to <paramref name="b"/>, bark on top, embers glowing underneath, a cut end showing its rings.</summary>
    private static void WinterLog(SKCanvas c, SKPoint a, SKPoint b, float thickness)
    {
        var len = (float)Math.Sqrt(((b.X - a.X) * (b.X - a.X)) + ((b.Y - a.Y) * (b.Y - a.Y)));
        var angle = (float)(Math.Atan2(b.Y - a.Y, b.X - a.X) * 180 / Math.PI);
        c.Save();
        c.Translate((a.X + b.X) / 2, (a.Y + b.Y) / 2);
        c.RotateDegrees(angle);
        var body = new SKRect(-len / 2, -thickness / 2, len / 2, thickness / 2);
        using (var shader = SKShader.CreateLinearGradient(new SKPoint(0, body.Top), new SKPoint(0, body.Bottom), new[] { new SKColor(0x6A, 0x46, 0x2A), new SKColor(0x3A, 0x22, 0x12), new SKColor(0xC8, 0x50, 0x18) }, new[] { 0f, 0.6f, 1f }, SKShaderTileMode.Clamp))
        using (var bark = new SKPaint { IsAntialias = true, Shader = shader })
        {
            c.DrawRoundRect(body, thickness * 0.4f, thickness * 0.4f, bark);
        }

        using (var crack = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = thickness * 0.05f, Color = new SKColor(0x22, 0x12, 0x08, 160) })
        {
            c.DrawLine(-len * 0.35f, -thickness * 0.15f, len * 0.1f, -thickness * 0.2f, crack);
            c.DrawLine(-len * 0.05f, thickness * 0.05f, len * 0.35f, thickness * 0.02f, crack);
        }

        var end = new SKRect((len / 2) - (thickness * 0.35f), -thickness / 2, (len / 2) + (thickness * 0.35f), thickness / 2);
        using (var wood = new SKPaint { IsAntialias = true, Color = new SKColor(0xD8, 0xA8, 0x6A) })
        using (var ring = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = thickness * 0.04f, Color = new SKColor(0x8A, 0x5A, 0x30) })
        {
            c.DrawOval(end, wood);
            var inner = end;
            inner.Inflate(-end.Width * 0.22f, -end.Height * 0.22f);
            c.DrawOval(inner, ring);
        }

        c.Restore();
    }

    /// <summary>A pillar candle standing on (x, baseY) with a lit flame and a soft halo.</summary>
    private static void WinterCandle(SKCanvas c, float x, float baseY, float height, float width)
    {
        var body = new SKRect(x - (width / 2), baseY - height, x + (width / 2), baseY);
        using (var shader = SKShader.CreateLinearGradient(new SKPoint(body.Left, 0), new SKPoint(body.Right, 0), new[] { new SKColor(0xFF, 0xF4, 0xDC), new SKColor(0xF0, 0xDC, 0xB4), new SKColor(0xB8, 0x9C, 0x74) }, new[] { 0f, 0.45f, 1f }, SKShaderTileMode.Clamp))
        using (var wax = new SKPaint { IsAntialias = true, Shader = shader })
        {
            c.DrawRoundRect(body, width * 0.12f, width * 0.12f, wax);
            c.DrawOval(new SKRect(body.Left + (width * 0.1f), body.Top - (width * 0.04f), body.Left + (width * 0.3f), body.Top + (width * 0.45f)), wax);
        }

        using (var wick = new SKPaint { IsAntialias = true, StrokeWidth = Math.Max(1f, width * 0.06f), Color = new SKColor(0x2A, 0x1A, 0x10) })
        {
            c.DrawLine(x, body.Top, x, body.Top - (width * 0.18f), wick);
        }

        var fy = body.Top - (width * 0.2f);
        WinterGlow(c, x, fy - (width * 0.2f), width * 3.5f, new SKColor(0xFF, 0xC0, 0x60, 110), add: true);
        using var flame = new SKPath();
        flame.MoveTo(x, fy - (width * 0.85f));
        flame.CubicTo(x + (width * 0.3f), fy - (width * 0.35f), x + (width * 0.25f), fy + (width * 0.05f), x, fy + (width * 0.05f));
        flame.CubicTo(x - (width * 0.25f), fy + (width * 0.05f), x - (width * 0.3f), fy - (width * 0.35f), x, fy - (width * 0.85f));
        flame.Close();
        using var fshader = SKShader.CreateRadialGradient(new SKPoint(x, fy - (width * 0.15f)), width * 0.6f, new[] { SKColors.White, new SKColor(0xFF, 0xE0, 0x80), new SKColor(0xFF, 0x90, 0x30) }, new[] { 0f, 0.45f, 1f }, SKShaderTileMode.Clamp);
        using var fill = new SKPaint { IsAntialias = true, Shader = fshader };
        c.DrawPath(flame, fill);
    }

    /// <summary>A knitted stocking hanging from (x, top): fluffy cuff, contrasting heel and toe, a pattern, lit warmly from below.</summary>
    private static void WinterStocking(SKCanvas c, float x, float top, float len, SKColor body, SKColor trim, bool toeRight, int pattern)
    {
        c.Save();
        c.Translate(x, top);
        if (!toeRight)
        {
            c.Scale(-1, 1);
        }

        var wd = len * 0.36f;
        using var leg = new SKPath();
        leg.MoveTo(-wd / 2, len * 0.15f);
        leg.LineTo(-wd / 2, len * 0.7f);
        leg.CubicTo(-wd / 2, len * 0.98f, wd * 0.1f, len * 1.02f, wd * 0.6f, len * 0.98f);
        leg.CubicTo(wd * 1.3f, len * 0.95f, wd * 1.35f, len * 0.66f, wd * 0.8f, len * 0.66f);
        leg.LineTo(wd / 2, len * 0.62f);
        leg.LineTo(wd / 2, len * 0.15f);
        leg.Close();

        using (var shader = SKShader.CreateLinearGradient(new SKPoint(-wd / 2, 0), new SKPoint(wd * 1.3f, len), new[] { Lighten(body, 0.15f), body, WinterMix(body, SKColors.Black, 0.45f) }, new[] { 0f, 0.4f, 1f }, SKShaderTileMode.Clamp))
        using (var knit = new SKPaint { IsAntialias = true, Shader = shader, ImageFilter = SKImageFilter.CreateDropShadow(len * 0.02f, len * 0.03f, len * 0.03f, len * 0.03f, SKColors.Black.WithAlpha(130)) })
        {
            c.DrawPath(leg, knit);
        }

        c.Save();
        c.ClipPath(leg, antialias: true);
        using (var patch = new SKPaint { IsAntialias = true, Color = trim })
        {
            c.DrawOval(new SKRect(-wd * 0.8f, len * 0.8f, wd * 0.25f, len * 1.15f), patch);
            c.DrawOval(new SKRect(wd * 0.85f, len * 0.6f, wd * 1.6f, len * 1.1f), patch);
        }

        using (var deco = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(220) })
        {
            switch (pattern)
            {
                case 0:
                    for (var y = len * 0.32f; y < len * 0.66f; y += len * 0.11f)
                    {
                        c.DrawRect(-wd, y, wd * 2, len * 0.045f, deco);
                    }

                    break;
                case 1:
                    for (var y = len * 0.3f; y < len * 0.75f; y += len * 0.09f)
                    {
                        for (var px = -wd * 0.35f + ((int)(y / (len * 0.09f)) % 2 * wd * 0.17f); px < wd * 0.6f; px += wd * 0.34f)
                        {
                            c.DrawCircle(px, y, len * 0.014f, deco);
                        }
                    }

                    break;
                default:
                    using (var zig = new SKPath())
                    {
                        zig.MoveTo(-wd, len * 0.42f);
                        for (var k = 0; k < 8; k++)
                        {
                            zig.LineTo(-wd + ((k + 0.5f) * wd / 3), len * (k % 2 == 0 ? 0.36f : 0.42f));
                        }

                        deco.Style = SKPaintStyle.Stroke;
                        deco.StrokeWidth = len * 0.025f;
                        c.DrawPath(zig, deco);
                        c.Translate(0, len * 0.14f);
                        c.DrawPath(zig, deco);
                    }

                    break;
            }
        }

        c.Restore();

        // Warm firelight on the lower part.
        using (var warm = SKShader.CreateLinearGradient(new SKPoint(0, len * 0.4f), new SKPoint(0, len), new[] { SKColors.Transparent, new SKColor(0xFF, 0x90, 0x40, 60) }, null, SKShaderTileMode.Clamp))
        using (var light = new SKPaint { IsAntialias = true, Shader = warm, BlendMode = SKBlendMode.Plus })
        {
            c.Save();
            c.ClipPath(leg, antialias: true);
            c.DrawRect(-wd, 0, wd * 3, len * 1.1f, light);
            c.Restore();
        }

        // Fluffy cuff and its hanging loop.
        using (var loop = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = len * 0.02f, Color = trim })
        {
            c.DrawArc(new SKRect(-wd * 0.15f, -len * 0.04f, wd * 0.25f, len * 0.06f), 180, 180, false, loop);
        }

        var cuff = new SKRect(-wd * 0.62f, 0, wd * 0.62f, len * 0.22f);
        using (var shader = SKShader.CreateLinearGradient(new SKPoint(0, cuff.Top), new SKPoint(0, cuff.Bottom), new[] { SKColors.White, new SKColor(0xE6, 0xE0, 0xD4), new SKColor(0xB8, 0xAE, 0xA0) }, new[] { 0f, 0.6f, 1f }, SKShaderTileMode.Clamp))
        using (var fur = new SKPaint { IsAntialias = true, Shader = shader })
        {
            c.DrawRoundRect(cuff, len * 0.06f, len * 0.06f, fur);
            for (var k = 0; k < 6; k++)
            {
                c.DrawCircle(cuff.Left + (cuff.Width * (k + 0.5f) / 6), cuff.Bottom - (len * 0.01f), len * 0.035f, fur);
            }
        }

        c.Restore();
    }

    /// <summary>A window onto a snowy night: moon, snow on the ground, pines, frost in the corners and a snowy sill.</summary>
    private static void WinterWindowView(SKCanvas c, SKRect r, float s, Random rng)
    {
        using (var frame = new SKPaint { IsAntialias = true, Color = new SKColor(0x3A, 0x22, 0x14), ImageFilter = SKImageFilter.CreateDropShadow(0, s * 0.008f, s * 0.01f, s * 0.01f, SKColors.Black.WithAlpha(150)) })
        {
            var f = r;
            f.Inflate(s * 0.018f, s * 0.018f);
            c.DrawRect(f, frame);
        }

        c.Save();
        c.ClipRect(r);
        using (var shader = SKShader.CreateLinearGradient(new SKPoint(0, r.Top), new SKPoint(0, r.Bottom), new[] { new SKColor(0x06, 0x10, 0x2C), new SKColor(0x1C, 0x34, 0x64) }, null, SKShaderTileMode.Clamp))
        using (var sky = new SKPaint { Shader = shader })
        {
            c.DrawRect(r, sky);
        }

        WinterGlow(c, r.Left + (r.Width * 0.7f), r.Top + (r.Height * 0.2f), r.Width * 0.6f, new SKColor(0xC8, 0xDC, 0xFF, 90), add: true);
        using (var moon = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xF6, 0xDC) })
        {
            c.DrawCircle(r.Left + (r.Width * 0.7f), r.Top + (r.Height * 0.2f), r.Width * 0.11f, moon);
        }

        var ground = r.Bottom - (r.Height * 0.18f);
        WinterPine(c, r.Left + (r.Width * 0.25f), ground + (r.Height * 0.02f), r.Height * 0.38f, new SKColor(0x0E, 0x22, 0x2A), new SKColor(0x22, 0x3E, 0x4A), new SKColor(0xD8, 0xE6, 0xF8), 0.06f, rng, 5);
        WinterPine(c, r.Left + (r.Width * 0.6f), ground + (r.Height * 0.03f), r.Height * 0.26f, new SKColor(0x12, 0x28, 0x32), new SKColor(0x26, 0x44, 0x50), new SKColor(0xD8, 0xE6, 0xF8), 0.06f, rng, 4);
        using (var snow = new SKPaint { IsAntialias = true, Color = new SKColor(0xD8, 0xE4, 0xF6) })
        {
            c.DrawOval(new SKRect(r.Left - (r.Width * 0.3f), ground, r.Right + (r.Width * 0.3f), r.Bottom + (r.Height * 0.2f)), snow);
        }

        using (var flake = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(200) })
        {
            for (var i = 0; i < 40; i++)
            {
                c.DrawCircle(R(rng, r.Left, r.Right), R(rng, r.Top, r.Bottom), R(rng, s * 0.0015f, s * 0.004f), flake);
            }
        }

        // Frost creeping in from the corners.
        foreach (var corner in new[] { new SKPoint(r.Left, r.Bottom), new SKPoint(r.Right, r.Bottom), new SKPoint(r.Left, r.Top), new SKPoint(r.Right, r.Top) })
        {
            WinterGlow(c, corner.X, corner.Y, r.Width * 0.4f, new SKColor(0xE8, 0xF2, 0xFF, 120));
        }

        c.Restore();

        using (var bar = new SKPaint { IsAntialias = true, Color = new SKColor(0x3A, 0x22, 0x14), StrokeWidth = s * 0.012f })
        {
            c.DrawLine(r.MidX, r.Top, r.MidX, r.Bottom, bar);
            c.DrawLine(r.Left, r.Top + (r.Height * 0.36f), r.Right, r.Top + (r.Height * 0.36f), bar);
            c.DrawLine(r.Left, r.Top + (r.Height * 0.68f), r.Right, r.Top + (r.Height * 0.68f), bar);
        }

        using (var sill = new SKPaint { IsAntialias = true, Color = new SKColor(0x5A, 0x36, 0x20) })
        {
            c.DrawRect(r.Left - (s * 0.03f), r.Bottom + (s * 0.012f), r.Width + (s * 0.06f), s * 0.02f, sill);
        }

        // A candle on the sill.
        WinterCandle(c, r.MidX, r.Bottom + (s * 0.012f), s * 0.06f, s * 0.022f);
    }

    // ---------- Santa's sleigh: the team silhouetted across a huge moon over snowy rooftops ----------

    private static void SantaSleigh(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var wide = w > h;
        WinterWash(c, w, 0, h, new[] { new SKColor(0x03, 0x07, 0x1E, 230), new SKColor(0x0A, 0x16, 0x3E, 180), new SKColor(0x1E, 0x30, 0x5E, 130), new SKColor(0x0A, 0x12, 0x30, 210) }, new[] { 0f, 0.5f, 0.78f, 1f });
        WinterStars(c, w, h * 0.62f, wide ? 200 : 160, s, rng);

        var moon = new SKPoint(w * (wide ? 0.62f : 0.6f), h * 0.2f);
        var mr = s * 0.155f;
        WinterGlow(c, moon.X, moon.Y, s * 0.8f, new SKColor(0xFF, 0xEE, 0xC8, 45), add: true);
        WinterMoon(c, moon.X, moon.Y, mr, rng);

        // The team climbs across the moon, a trail of magic dust curving away behind the sleigh.
        var u = s * 0.044f;
        var origin = new SKPoint(moon.X - (u * 3.8f), moon.Y + (u * 1.1f));
        using (var dust = new SKPaint { IsAntialias = true, BlendMode = SKBlendMode.Plus })
        {
            for (var i = 0; i < 90; i++)
            {
                var t = (float)Math.Pow(rng.NextDouble(), 0.8);
                var tx = origin.X - (u * 1.6f) - (t * s * 0.5f);
                var ty = origin.Y + (u * 0.4f) + (t * t * s * 0.2f) + R(rng, -u * 0.5f, u * 0.5f) * (0.4f + t);
                dust.Color = new SKColor(0xFF, 0xE0, 0x90, (byte)(230 * (1 - (t * 0.75f))));
                c.DrawCircle(tx, ty, u * R(rng, 0.03f, 0.1f) * (1 - (t * 0.5f)), dust);
            }

            for (var i = 0; i < 5; i++)
            {
                var t = R(rng, 0.05f, 0.7f);
                WinterTwinkle(c, origin.X - (u * 1.6f) - (t * s * 0.5f), origin.Y + (u * 0.4f) + (t * t * s * 0.2f), u * R(rng, 0.4f, 0.8f), new SKColor(0xFF, 0xE8, 0xB0));
            }
        }

        c.Save();
        c.Translate(origin.X, origin.Y);
        c.RotateDegrees(-8);
        SleighTeam(c, u);
        c.Restore();

        // Rooftops: a far row with a church spire, then close-up roofs deep in snow, chimneys smoking.
        WinterRooftops(c, w, h, s, rng, h * 0.84f, 0.85f, new SKColor(0x1A, 0x26, 0x4A), new SKColor(0xA4, 0xB6, 0xD8), new SKColor(0x70, 0x84, 0xB0), 0.35f, steeple: true);
        Mist(c, w, h, h * 0.8f, s, rng, 40);
        WinterRooftops(c, w, h, s, rng, h * 0.92f, 1.5f, new SKColor(0x0C, 0x12, 0x2C), WinterSnowWhite, new SKColor(0xAE, 0xC0, 0xE0), 0.55f, steeple: false);
        WinterSnowfall(c, w, h, s, rng, wide ? 150 : 110);
    }

    /// <summary>
    /// A row of houses with their eaves at <paramref name="eaves"/> (scaled by <paramref name="scale"/>): snow-laden roofs,
    /// chimneys, and windows lit at random.
    /// </summary>
    private static void WinterRooftops(SKCanvas c, int w, int h, float s, Random rng, float eaves, float scale, SKColor wall, SKColor snowTop, SKColor snowBottom, float lit, bool steeple)
    {
        using var wallPaint = new SKPaint { IsAntialias = true, Color = wall };
        using var brick = new SKPaint { IsAntialias = true, Color = WinterMix(wall, new SKColor(0x3A, 0x1E, 0x1A), 0.5f) };
        using var dark = new SKPaint { IsAntialias = true, Color = WinterMix(wall, SKColors.Black, 0.4f) };
        var steepleAt = steeple ? R(rng, w * 0.25f, w * 0.4f) : -1;
        var x = R(rng, -s * 0.1f, 0);
        while (x < w)
        {
            var bw = s * R(rng, 0.13f, 0.2f) * scale;
            var top = eaves + (s * R(rng, -0.025f, 0.025f) * scale);
            var roofH = bw * R(rng, 0.32f, 0.48f);
            var gable = rng.NextDouble() < 0.45;
            var ov = bw * 0.06f;

            if (steepleAt > 0 && x + bw > steepleAt)
            {
                // A church tower and spire.
                var tw = s * 0.05f * scale;
                var tx = steepleAt;
                c.DrawRect(tx - (tw / 2), top - (s * 0.1f * scale), tw, h - top + (s * 0.1f * scale), wallPaint);
                using var spire = new SKPath();
                spire.MoveTo(tx - (tw * 0.6f), top - (s * 0.1f * scale));
                spire.LineTo(tx, top - (s * 0.24f * scale));
                spire.LineTo(tx + (tw * 0.6f), top - (s * 0.1f * scale));
                spire.Close();
                c.DrawPath(spire, wallPaint);
                using var snowCap = new SKPaint { IsAntialias = true, Color = snowTop };
                using var litFace = new SKPath();
                litFace.MoveTo(tx - (tw * 0.6f), top - (s * 0.1f * scale));
                litFace.LineTo(tx, top - (s * 0.24f * scale));
                litFace.LineTo(tx - (tw * 0.1f), top - (s * 0.1f * scale));
                litFace.Close();
                c.DrawPath(litFace, snowCap);
                WinterWindow(c, new SKRect(tx - (tw * 0.18f), top - (s * 0.07f * scale), tx + (tw * 0.18f), top - (s * 0.03f * scale)), s, sill: false);
                steepleAt = -1;
            }

            // Chimney behind the roof.
            if (rng.NextDouble() < 0.6)
            {
                var chX = x + (bw * R(rng, 0.25f, 0.75f));
                var chTop = top - (roofH * R(rng, 0.7f, 1.1f));
                c.DrawRect(chX - (bw * 0.05f), chTop, bw * 0.1f, top - chTop, brick);
                if (scale > 1)
                {
                    WinterSmoke(c, chX, chTop - (bw * 0.04f), bw * 0.35f, rng, 50);
                }

                using var cap = new SKPaint { IsAntialias = true, Color = snowTop };
                c.DrawRoundRect(new SKRect(chX - (bw * 0.065f), chTop - (bw * 0.025f), chX + (bw * 0.065f), chTop + (bw * 0.012f)), bw * 0.02f, bw * 0.02f, cap);
            }

            c.DrawRect(x, top, bw, h - top + 2, wallPaint);
            c.DrawRect(x - ov, top - (bw * 0.01f), bw + (2 * ov), bw * 0.06f, dark);

            using var roof = new SKPath();
            if (gable)
            {
                roof.MoveTo(x - ov, top + (bw * 0.02f));
                roof.LineTo(x + (bw / 2), top - (roofH * 1.25f));
                roof.LineTo(x + bw + ov, top + (bw * 0.02f));
            }
            else
            {
                roof.MoveTo(x - ov, top + (bw * 0.02f));
                roof.LineTo(x + (bw * 0.18f), top - roofH);
                roof.QuadTo(x + (bw / 2), top - (roofH * 1.08f), x + (bw * 0.82f), top - roofH);
                roof.LineTo(x + bw + ov, top + (bw * 0.02f));
            }

            const int Lumps = 5;
            for (var k = 1; k <= Lumps; k++)
            {
                var px = x + bw + ov - ((bw + (2 * ov)) * k / Lumps);
                var mx = x + bw + ov - ((bw + (2 * ov)) * (k - 0.5f) / Lumps);
                roof.QuadTo(mx, top + (bw * R(rng, 0.04f, 0.07f)), px, top + (bw * 0.02f));
            }

            roof.Close();
            using (var shader = SKShader.CreateLinearGradient(new SKPoint(0, top - (roofH * 1.25f)), new SKPoint(0, top + (bw * 0.06f)), new[] { snowTop, snowBottom }, null, SKShaderTileMode.Clamp))
            using (var snow = new SKPaint { IsAntialias = true, Shader = shader })
            {
                c.DrawPath(roof, snow);
            }

            if (scale > 1)
            {
                using var ice = new SKPaint { IsAntialias = true, Color = new SKColor(0xDD, 0xEE, 0xFF, 210) };
                for (var ix = x; ix < x + bw; ix += bw * R(rng, 0.04f, 0.08f))
                {
                    var len = bw * R(rng, 0.03f, 0.08f);
                    using var icicle = new SKPath();
                    icicle.MoveTo(ix - (bw * 0.008f), top + (bw * 0.04f));
                    icicle.LineTo(ix + (bw * 0.008f), top + (bw * 0.04f));
                    icicle.LineTo(ix, top + (bw * 0.04f) + len);
                    icicle.Close();
                    c.DrawPath(icicle, ice);
                }
            }

            // Windows: a row or two, some lit.
            var winW = bw * 0.14f;
            for (var wy = top + (bw * 0.12f); wy < h - (winW * 0.5f); wy += winW * 2.1f)
            {
                for (var col = 0; col < 3; col++)
                {
                    var wx = x + (bw * (0.2f + (col * 0.3f)));
                    var rect = new SKRect(wx - (winW / 2), wy, wx + (winW / 2), wy + (winW * 1.3f));
                    if (rng.NextDouble() < lit)
                    {
                        WinterWindow(c, rect, s, sill: scale > 1);
                    }
                    else
                    {
                        c.DrawRect(rect, dark);
                    }
                }
            }

            x += bw + (bw * R(rng, 0.02f, 0.12f));
        }
    }

    // ---------- Baubles and holly: glossy baubles hanging from a pine bough, holly and fairy-light bokeh ----------

    private static void BaublesAndHolly(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var wide = w > h;

        // Deep evergreen backdrop with out-of-focus lights.
        WinterWash(c, w, 0, h, new[] { new SKColor(0x02, 0x12, 0x0A, 225), new SKColor(0x06, 0x22, 0x14, 150), new SKColor(0x06, 0x22, 0x14, 150), new SKColor(0x02, 0x10, 0x08, 230) }, new[] { 0f, 0.4f, 0.62f, 1f });
        var bokeh = new[] { new SKColor(0xFF, 0xB0, 0x40), new SKColor(0xFF, 0xD8, 0x8A), new SKColor(0xFF, 0x50, 0x3C), new SKColor(0xFF, 0xC0, 0x60) };
        WinterGlowOval(c, w / 2f, h * 0.15f, w * 0.6f, h * 0.2f, new SKColor(0xFF, 0xA0, 0x40, 40), add: true);
        WinterGlowOval(c, w / 2f, h * 0.9f, w * 0.6f, h * 0.2f, new SKColor(0xFF, 0xA0, 0x40, 40), add: true);
        for (var i = 0; i < (wide ? 30 : 22); i++)
        {
            var y = rng.NextDouble() < 0.55 ? R(rng, 0, h * 0.36f) : R(rng, h * 0.64f, h);
            WinterBokeh(c, R(rng, 0, w), y, s * R(rng, 0.018f, 0.055f), bokeh[rng.Next(bokeh.Length)], (byte)rng.Next(70, 140));
        }

        // Baubles on gold threads, hanging from the bough along the top.
        var colours = new[] { new SKColor(0xC8, 0x14, 0x24), new SKColor(0xE8, 0xA8, 0x20), new SKColor(0x1E, 0x58, 0xC8), new SKColor(0xB8, 0xC4, 0xD0), new SKColor(0x8A, 0x10, 0x3C), new SKColor(0xD0, 0x30, 0x30) };
        var hanging = wide
            ? new[] { (X: 0.08f, Y: 0.2f, R: 0.085f), (X: 0.23f, Y: 0.13f, R: 0.065f), (X: 0.38f, Y: 0.23f, R: 0.07f), (X: 0.62f, Y: 0.15f, R: 0.07f), (X: 0.77f, Y: 0.24f, R: 0.075f), (X: 0.92f, Y: 0.16f, R: 0.09f) }
            : new[] { (X: 0.13f, Y: 0.21f, R: 0.09f), (X: 0.38f, Y: 0.13f, R: 0.065f), (X: 0.63f, Y: 0.22f, R: 0.075f), (X: 0.87f, Y: 0.15f, R: 0.085f) };
        var offset = rng.Next(colours.Length);
        using (var thread = new SKPaint { IsAntialias = true, StrokeWidth = Math.Max(1f, s * 0.0025f), Color = new SKColor(0xFF, 0xD7, 0x6A, 220) })
        {
            for (var i = 0; i < hanging.Length; i++)
            {
                var (hx, hy, hr) = hanging[i];
                float bx = w * hx, by = h * hy, br = s * hr;
                c.DrawLine(bx, 0, bx, by - br, thread);
                WinterBauble(c, bx, by, br, colours[(i + offset) % colours.Length], i % 3, rng);
            }
        }

        var top = new SKPoint[14];
        for (var i = 0; i < top.Length; i++)
        {
            var t = i / (float)(top.Length - 1);
            top[i] = new SKPoint((-0.05f * w) + (t * 1.1f * w), (s * 0.03f) + ((float)Math.Sin((t * Math.PI * 3) + 0.5) * s * 0.012f));
        }

        WinterBough(c, top, s * 0.11f, rng);
        foreach (var hx in wide ? new[] { 0.3f, 0.7f } : new[] { 0.5f })
        {
            WinterHollySprig(c, w * hx, s * 0.04f, s * 0.09f, 90, rng);
        }

        // Along the bottom: a bough with baubles resting in it, holly in the corners.
        var bottom = new SKPoint[14];
        for (var i = 0; i < bottom.Length; i++)
        {
            var t = i / (float)(bottom.Length - 1);
            bottom[i] = new SKPoint((-0.05f * w) + (t * 1.1f * w), h - (s * 0.035f) + ((float)Math.Sin((t * Math.PI * 2.5) + 1.2) * s * 0.012f));
        }

        WinterBough(c, bottom, s * 0.13f, rng);
        var resting = wide ? new[] { (0.27f, 0.06f), (0.42f, 0.075f), (0.58f, 0.055f), (0.73f, 0.07f) } : new[] { (0.3f, 0.065f), (0.52f, 0.075f), (0.72f, 0.06f) };
        for (var i = 0; i < resting.Length; i++)
        {
            var (rx, rr) = resting[i];
            WinterBauble(c, w * rx, h - (s * 0.045f) - (s * rr * 0.6f), s * rr, colours[(i + offset + 3) % colours.Length], (i + 1) % 3, rng);
        }

        for (var i = 0; i < bottom.Length; i++)
        {
            bottom[i] = new SKPoint(bottom[i].X, bottom[i].Y + (s * 0.035f));
        }

        WinterBough(c, bottom, s * 0.08f, rng, 0.5f);
        WinterHollySprig(c, w * 0.04f, h - (s * 0.06f), s * 0.2f, -50, rng);
        WinterHollySprig(c, w * 0.96f, h - (s * 0.06f), s * 0.2f, -130, rng);

        // Glitter in the air.
        for (var i = 0; i < 40; i++)
        {
            var y = rng.NextDouble() < 0.5 ? R(rng, h * 0.05f, h * 0.35f) : R(rng, h * 0.66f, h * 0.9f);
            using var dot = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xE8, 0xB0, (byte)rng.Next(120, 230)) };
            c.DrawCircle(R(rng, 0, w), y, R(rng, s * 0.0015f, s * 0.0035f), dot);
        }

        for (var i = 0; i < 6; i++)
        {
            WinterTwinkle(c, R(rng, 0, w), rng.NextDouble() < 0.5 ? R(rng, h * 0.05f, h * 0.32f) : R(rng, h * 0.68f, h * 0.88f), s * R(rng, 0.015f, 0.03f), new SKColor(0xFF, 0xF0, 0xD0));
        }
    }

    // ---------- Snowy forest: pines receding into the mist under the northern lights, a cabin's windows glowing ----------

    private static void SnowyForest(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var wide = w > h;

        WinterWash(c, w, 0, h * 0.85f, new[] { new SKColor(0x02, 0x0A, 0x1C, 230), new SKColor(0x05, 0x1C, 0x30, 180), new SKColor(0x1A, 0x40, 0x58, 110) }, new[] { 0f, 0.45f, 1f });
        WinterStars(c, w, h * 0.5f, wide ? 160 : 130, s, rng);
        WinterAurora(c, w, h, s, rng);

        WinterMountains(c, w, h, s, rng, h * 0.76f, h * 0.6f);
        Mist(c, w, h, h * 0.68f, s, rng, 40);

        // Far treeline, soft and blue.
        var far = WinterRidgeLine(h * 0.77f, h * 0.012f, s, 1.2f, rng);
        using (WinterRidge(c, w, h, far, new SKColor(0x58, 0x74, 0x98), new SKColor(0x40, 0x5A, 0x7E), SKColors.Transparent, s))
        {
        }

        WinterTreeLine(c, w, far, s, rng, s * 0.05f, s * 0.085f, s * 0.03f, new SKColor(0x24, 0x3A, 0x5C), new SKColor(0x36, 0x52, 0x7A), new SKColor(0x90, 0xA8, 0xCA), s * 0.0025f);
        Mist(c, w, h, h * 0.75f, s, rng, 45);

        // Middle: snowy clearing with the cabin, more pines.
        var mid = WinterRidgeLine(h * 0.84f, h * 0.02f, s, 0.8f, rng);
        using (WinterRidge(c, w, h, mid, new SKColor(0x9C, 0xB2, 0xD2), new SKColor(0x74, 0x8C, 0xB4), new SKColor(0xE0, 0xEC, 0xFF, 140), s))
        {
        }

        WinterTreeLine(c, w, mid, s, rng, s * 0.1f, s * 0.15f, s * 0.07f, new SKColor(0x13, 0x2C, 0x40), new SKColor(0x28, 0x4A, 0x60), new SKColor(0xC8, 0xD8, 0xEE), s * 0.001f);
        var cabinX = w * (wide ? 0.3f : 0.3f);
        var cabin = s * 0.17f;
        WinterCottage(c, cabinX, mid(cabinX) + (s * 0.012f), cabin, rng, s);

        // Near: fresh snow and big pines, kept off the cabin.
        var near = WinterRidgeLine(h * 0.92f, h * 0.02f, s, 0.6f, rng);
        using (WinterRidge(c, w, h, near, WinterSnowWhite, new SKColor(0xC0, 0xD0, 0xEA), SKColors.Transparent, s))
        {
        }

        for (var x = R(rng, s * 0.15f, s * 0.25f); x < w - (s * 0.12f); x += s * R(rng, 0.2f, 0.32f))
        {
            if (Math.Abs(x - cabinX) < cabin * 0.9f)
            {
                continue;
            }

            WinterPine(c, x, near(x) + (s * 0.015f), s * R(rng, 0.17f, 0.25f), new SKColor(0x0C, 0x26, 0x1C), new SKColor(0x24, 0x4E, 0x3A), new SKColor(0xE6, 0xEE, 0xFA), R(rng, 0.035f, 0.055f), rng, 6);
        }

        // Tall dark pines framing both edges.
        WinterPine(c, w * 0.03f, h * 1.03f, h * 0.5f, new SKColor(0x06, 0x18, 0x12), new SKColor(0x18, 0x3A, 0x2C), new SKColor(0xDC, 0xE6, 0xF6), 0.03f, rng, 8);
        WinterPine(c, w * 0.97f, h * 1.03f, h * 0.48f, new SKColor(0x06, 0x18, 0x12), new SKColor(0x18, 0x3A, 0x2C), new SKColor(0xDC, 0xE6, 0xF6), 0.03f, rng, 8);
        WinterGlints(c, w, h, near, s, rng, 50);
        WinterSnowfall(c, w, h, s, rng, wide ? 200 : 150);
    }

    /// <summary>Northern lights: curtains of light rising from a wavering hem, green fading to violet at the top.</summary>
    private static void WinterAurora(SKCanvas c, int w, int h, float s, Random rng)
    {
        using var blur = SKImageFilter.CreateBlur(s * 0.006f, s * 0.006f);
        using var layer = new SKPaint { ImageFilter = blur };
        c.SaveLayer(layer);
        var bands = new[]
        {
            (Low: new SKColor(0x3C, 0xFF, 0xA0), High: new SKColor(0x8A, 0x6C, 0xFF), Base: 0.27f, Height: 0.17f),
            (Low: new SKColor(0x2E, 0xE8, 0xC8), High: new SKColor(0x4C, 0x8C, 0xFF), Base: 0.2f, Height: 0.13f)
        };
        using var ray = new SKPaint { IsAntialias = true, StrokeWidth = s * 0.009f, BlendMode = SKBlendMode.Plus };
        foreach (var band in bands)
        {
            double p1 = R(rng, 0, 6.3f), p2 = R(rng, 0, 6.3f), p3 = R(rng, 0, 6.3f);
            for (var x = -s * 0.01f; x < w + (s * 0.01f); x += s * 0.006f)
            {
                var u = x / s;
                var hem = (h * band.Base) + (float)(Math.Sin((u * 4.2) + p1) * h * 0.05f) + (float)(Math.Sin((u * 9.7) + p2) * h * 0.012f);
                var strength = (float)Math.Clamp(0.25 + (0.75 * Math.Pow((Math.Sin((u * 3.1) + p3) + 1) / 2, 1.5)), 0, 1);
                var top = hem - (h * band.Height * (0.6f + (0.4f * strength)));
                using var shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, hem), new SKPoint(0, top),
                    new[] { band.Low.WithAlpha(0), band.Low.WithAlpha((byte)(150 * strength)), band.Low.WithAlpha((byte)(70 * strength)), band.High.WithAlpha((byte)(40 * strength)), band.High.WithAlpha(0) },
                    new[] { 0f, 0.06f, 0.35f, 0.7f, 1f },
                    SKShaderTileMode.Clamp);
                ray.Shader = shader;
                c.DrawLine(x, hem, x, top, ray);
            }

            ray.Shader = null;
        }

        c.Restore();
    }

    /// <summary>A distant mountain range: pale snowy faces lit from the left, shadowed on the right.</summary>
    private static void WinterMountains(SKCanvas c, int w, int h, float s, Random rng, float baseY, float peak)
    {
        var points = new System.Collections.Generic.List<SKPoint> { new(-4, baseY) };
        var x = -s * 0.05f;
        var up = true;
        while (x < w + (s * 0.2f))
        {
            x += s * R(rng, 0.09f, 0.18f);
            var y = up ? R(rng, peak, peak + ((baseY - peak) * 0.45f)) : R(rng, baseY - ((baseY - peak) * 0.45f), baseY - ((baseY - peak) * 0.15f));
            points.Add(new SKPoint(x, y));
            up = !up;
        }

        using var range = new SKPath();
        range.MoveTo(-4, h);
        foreach (var p in points)
        {
            range.LineTo(p);
        }

        range.LineTo(w + 4, baseY);
        range.LineTo(w + 4, h);
        range.Close();
        using (var shader = SKShader.CreateLinearGradient(new SKPoint(0, peak), new SKPoint(0, baseY), new[] { new SKColor(0xB4, 0xC6, 0xE2, 200), new SKColor(0x5A, 0x72, 0x9A, 220) }, null, SKShaderTileMode.Clamp))
        using (var fill = new SKPaint { IsAntialias = true, Shader = shader })
        {
            c.DrawPath(range, fill);
        }

        // Shadowed faces: from each peak down its right-hand side.
        c.Save();
        c.ClipPath(range, antialias: true);
        using var shade = new SKPaint { IsAntialias = true, Color = new SKColor(0x1E, 0x2C, 0x4C, 120) };
        for (var i = 1; i < points.Count - 1; i++)
        {
            if (points[i].Y >= points[i - 1].Y || points[i].Y >= points[i + 1].Y)
            {
                continue;
            }

            using var face = new SKPath();
            face.MoveTo(points[i]);
            face.LineTo(points[i + 1]);
            face.LineTo(points[i + 1].X, h);
            face.LineTo(points[i].X + ((points[i + 1].X - points[i].X) * 0.15f), h);
            face.Close();
            c.DrawPath(face, shade);
        }

        c.Restore();
    }

    // ---------- the sleigh silhouette ----------

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
}
