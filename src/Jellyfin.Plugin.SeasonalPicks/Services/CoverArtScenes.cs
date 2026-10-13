using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>Scene-style decorations for particular seasons (tree, lightsabers, ghosts, castle, bonfire).</summary>
internal static partial class CoverArt
{
    // ---------- Christmas: a decorated tree with presents, over falling snow ----------

    private static void ChristmasTree(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var u = s * 0.36f;                      // tree height (top stays below the title)
        DecoratedTree(c, w - (u * 0.36f) - (w * 0.04f), h * 0.93f, u, rng);
    }

    /// <summary>A decorated tree with lights, baubles, a star and presents; (cx, ground) is the foot of its trunk, u its height.</summary>
    private static void DecoratedTree(SKCanvas c, float cx, float ground, float u, Random rng)
    {

        // Soft warm glow behind the tree.
        using (var glow = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(new SKPoint(cx, ground - (u * 0.45f)), u * 0.7f, new[] { new SKColor(0xFF, 0xD5, 0x80, 70), SKColors.Transparent }, SKShaderTileMode.Clamp)
        })
        {
            c.DrawCircle(cx, ground - (u * 0.45f), u * 0.7f, glow);
        }

        using (var trunk = new SKPaint { IsAntialias = true, Color = new SKColor(0x5D, 0x40, 0x37) })
        {
            c.DrawRect(cx - (u * 0.05f), ground - (u * 0.1f), u * 0.1f, u * 0.1f, trunk);
        }

        // Three tiers, darker at the bottom edge of each.
        var tiers = new[] { (Bottom: 0.08f, Half: 0.34f, Height: 0.42f), (Bottom: 0.34f, Half: 0.27f, Height: 0.36f), (Bottom: 0.57f, Half: 0.2f, Height: 0.34f) };
        foreach (var (bottom, half, height) in tiers)
        {
            var by = ground - (u * bottom);
            var ty = by - (u * height);
            using var tier = new SKPath();
            tier.MoveTo(cx, ty);
            tier.LineTo(cx + (u * half), by);
            tier.QuadTo(cx, by + (u * 0.05f), cx - (u * half), by);
            tier.Close();
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Shader = SKShader.CreateLinearGradient(new SKPoint(cx, ty), new SKPoint(cx, by), new[] { new SKColor(0x2E, 0x8B, 0x57), new SKColor(0x14, 0x5A, 0x32) }, SKShaderTileMode.Clamp)
            };
            c.DrawPath(tier, paint);
        }

        var top = ground - (u * 0.91f);

        // Strings of lights draped across the tree.
        var bulbColours = new[] { new SKColor(0xFF, 0xD5, 0x4F), new SKColor(0xFF, 0x52, 0x52), new SKColor(0x40, 0xC4, 0xFF), new SKColor(0x69, 0xF0, 0xAE) };
        using var wire = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = u * 0.006f, Color = new SKColor(0x0B, 0x33, 0x1C) };
        var bulbIndex = 0;
        foreach (var t in new[] { 0.3f, 0.55f, 0.8f })
        {
            var y0 = top + ((ground - top) * (t - 0.14f));
            var y1 = top + ((ground - top) * t);
            var halfW = u * 0.36f * t;
            using var garland = new SKPath();
            garland.MoveTo(cx - (halfW * 0.8f), y0);
            garland.QuadTo(cx, y1 + (u * 0.04f), cx + halfW, y1);
            c.DrawPath(garland, wire);
            for (var k = 0; k <= 6; k++)
            {
                var f = k / 6f;
                var bx = ((1 - f) * (1 - f) * (cx - (halfW * 0.8f))) + (2 * (1 - f) * f * cx) + (f * f * (cx + halfW));
                var by = ((1 - f) * (1 - f) * y0) + (2 * (1 - f) * f * (y1 + (u * 0.04f))) + (f * f * y1);
                var colour = bulbColours[bulbIndex++ % bulbColours.Length];
                using var bulb = new SKPaint { IsAntialias = true, Color = colour, ImageFilter = SKImageFilter.CreateDropShadow(0, 0, u * 0.012f, u * 0.012f, colour) };
                c.DrawCircle(bx, by, u * 0.012f, bulb);
            }
        }

        // Baubles with a highlight, placed inside the tree's outline.
        var baubleColours = new[] { new SKColor(0xD3, 0x2F, 0x2F), new SKColor(0xFF, 0xC1, 0x07), new SKColor(0x19, 0x76, 0xD2), new SKColor(0xE0, 0xE0, 0xE0), new SKColor(0x8E, 0x24, 0xAA) };
        using var shine = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(200) };
        var hung = new List<(float X, float Y, float R)>();
        for (var i = 0; i < 12; i++)
        {
            var r = u * R(rng, 0.022f, 0.032f);
            for (var attempt = 0; attempt < 30; attempt++)
            {
                var t = R(rng, 0.25f, 0.92f);
                var y = top + ((ground - (u * 0.1f) - top) * t);
                var x = cx + (R(rng, -1, 1) * u * 0.3f * t);
                if (hung.Exists(o => Math.Sqrt(((o.X - x) * (o.X - x)) + ((o.Y - y) * (o.Y - y))) < (o.R + r) * 1.3))
                {
                    continue;
                }

                using var bauble = new SKPaint { IsAntialias = true, Color = baubleColours[rng.Next(baubleColours.Length)] };
                c.DrawCircle(x, y, r, bauble);
                c.DrawCircle(x - (r * 0.35f), y - (r * 0.35f), r * 0.28f, shine);
                hung.Add((x, y, r));
                break;
            }
        }

        // Star on top.
        using (var star = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xD5, 0x4F), ImageFilter = SKImageFilter.CreateDropShadow(0, 0, u * 0.03f, u * 0.03f, new SKColor(0xFF, 0xE0, 0x82)) })
        using (var starPath = Star(cx, top - (u * 0.01f), u * 0.075f, u * 0.032f))
        {
            c.DrawPath(starPath, star);
        }

        // Presents at the foot of the tree.
        var gifts = new[]
        {
            (X: cx - (u * 0.5f), W: u * 0.2f, H: u * 0.15f, Box: new SKColor(0xC6, 0x28, 0x28), Ribbon: new SKColor(0xFF, 0xD5, 0x4F)),
            (X: cx - (u * 0.3f), W: u * 0.15f, H: u * 0.11f, Box: new SKColor(0x15, 0x65, 0xC0), Ribbon: SKColors.White),
            (X: cx + (u * 0.18f), W: u * 0.17f, H: u * 0.13f, Box: new SKColor(0x2E, 0x7D, 0x32), Ribbon: new SKColor(0xE5, 0x39, 0x35))
        };
        foreach (var g in gifts)
        {
            var rect = new SKRect(g.X, ground - g.H, g.X + g.W, ground);
            using var box = new SKPaint { IsAntialias = true, Color = g.Box, ImageFilter = SKImageFilter.CreateDropShadow(0, u * 0.01f, u * 0.01f, u * 0.01f, SKColors.Black.WithAlpha(120)) };
            c.DrawRect(rect, box);
            using var ribbon = new SKPaint { IsAntialias = true, Color = g.Ribbon };
            c.DrawRect(rect.MidX - (g.W * 0.08f), rect.Top, g.W * 0.16f, g.H, ribbon);
            c.DrawRect(rect.Left, rect.MidY - (g.H * 0.08f), g.W, g.H * 0.16f, ribbon);
            c.DrawOval(new SKRect(rect.MidX - (g.W * 0.25f), rect.Top - (g.H * 0.2f), rect.MidX, rect.Top + (g.H * 0.02f)), ribbon);
            c.DrawOval(new SKRect(rect.MidX, rect.Top - (g.H * 0.2f), rect.MidX + (g.W * 0.25f), rect.Top + (g.H * 0.02f)), ribbon);
        }
    }

    private static SKPath Star(float cx, float cy, float outer, float inner)
    {
        var p = new SKPath();
        for (var i = 0; i < 10; i++)
        {
            var a = (-Math.PI / 2) + (i * Math.PI / 5);
            var r = i % 2 == 0 ? outer : inner;
            var x = cx + (float)(Math.Cos(a) * r);
            var y = cy + (float)(Math.Sin(a) * r);
            if (i == 0)
            {
                p.MoveTo(x, y);
            }
            else
            {
                p.LineTo(x, y);
            }
        }

        p.Close();
        return p;
    }

    // ---------- Star Wars Day: a desert planet and its moon, two lightsabers clashing below the title ----------

    private static void Lightsabers(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        Sphere(c, w * 0.15f, h * 0.17f, s * 0.085f, new SKColor(0xF0, 0xC0, 0x7A), new SKColor(0xB0, 0x6A, 0x2E), rng, bands: true);
        Sphere(c, w * 0.27f, h * 0.08f, s * 0.025f, new SKColor(0xD8, 0xD8, 0xE0), new SKColor(0x70, 0x70, 0x80), rng, bands: false);

        // The blades meet low in the middle, well under the title, with a flash where they clash.
        Lightsaber(c, new SKPoint(w * 0.12f, h * 1.02f), new SKPoint(w * 0.66f, h * 0.72f), new SKColor(0x29, 0x8D, 0xFF), s);
        Lightsaber(c, new SKPoint(w * 0.88f, h * 1.02f), new SKPoint(w * 0.34f, h * 0.72f), new SKColor(0xFF, 0x1F, 0x1F), s);
        var clash = new SKPoint(w * 0.5f, h * (1.02f - (0.3f * (0.38f / 0.54f))));
        using (var flash = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(clash, s * 0.07f, new[] { SKColors.White, new SKColor(0xFF, 0xF3, 0xC4, 160), SKColors.Transparent }, new[] { 0f, 0.3f, 1f }, SKShaderTileMode.Clamp) })
        {
            c.DrawCircle(clash, s * 0.07f, flash);
        }

        using var spark = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = s * 0.003f, Color = new SKColor(0xFF, 0xF8, 0xE1, 230) };
        for (var i = 0; i < 10; i++)
        {
            var a = (i * Math.PI * 2 / 10) + R(rng, -0.15f, 0.15f);
            var inner = s * 0.02f;
            var outer = s * R(rng, 0.04f, 0.065f);
            c.DrawLine(clash.X + (float)(Math.Cos(a) * inner), clash.Y + (float)(Math.Sin(a) * inner), clash.X + (float)(Math.Cos(a) * outer), clash.Y + (float)(Math.Sin(a) * outer), spark);
        }
    }

    /// <summary>A lit planet or moon: shaded from the upper left, optional soft cloud bands, a dark night side.</summary>
    private static void Sphere(SKCanvas c, float x, float y, float r, SKColor light, SKColor dark, Random rng, bool bands)
    {
        using (var glow = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(x, y), r * 1.6f, new[] { light.WithAlpha(70), SKColors.Transparent }, SKShaderTileMode.Clamp) })
        {
            c.DrawCircle(x, y, r * 1.6f, glow);
        }

        using var disc = new SKPath();
        disc.AddCircle(x, y, r);
        using (var body = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(x - (r * 0.4f), y - (r * 0.4f)), r * 1.7f, new[] { Lighten(light, 0.3f), light, dark, Darken(dark, 0.3f) }, new[] { 0f, 0.35f, 0.75f, 1f }, SKShaderTileMode.Clamp) })
        {
            c.DrawPath(disc, body);
        }

        if (bands)
        {
            c.Save();
            c.ClipPath(disc, antialias: true);
            using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.05f);
            using var band = new SKPaint { IsAntialias = true, MaskFilter = blur };
            for (var i = 0; i < 4; i++)
            {
                band.Color = (i % 2 == 0 ? Lighten(light, 0.4f) : dark).WithAlpha(60);
                var by = y - (r * 0.6f) + (i * r * 0.38f) + R(rng, -r * 0.05f, r * 0.05f);
                c.DrawOval(new SKRect(x - (r * 1.2f), by - (r * 0.07f), x + (r * 1.2f), by + (r * 0.07f)), band);
            }

            c.Restore();
        }
    }

    /// <summary>A lightsaber from the pommel (<paramref name="from"/>) to the blade tip, with a glowing blade and a metal hilt.</summary>
    private static void Lightsaber(SKCanvas c, SKPoint from, SKPoint tip, SKColor colour, float s)
    {
        var dx = tip.X - from.X;
        var dy = tip.Y - from.Y;
        var len = (float)Math.Sqrt((dx * dx) + (dy * dy));
        var ux = dx / len;
        var uy = dy / len;
        var hiltLen = s * 0.2f;
        var emitter = new SKPoint(from.X + (ux * hiltLen), from.Y + (uy * hiltLen));

        // Blade: a wide haze, a strong coloured glow, a solid coloured blade, then a thin white-hot core.
        foreach (var (width, sigma, alpha) in new[] { (s * 0.11f, s * 0.05f, (byte)170), (s * 0.05f, s * 0.018f, (byte)255), (s * 0.03f, s * 0.006f, (byte)255) })
        {
            using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, sigma);
            using var glow = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = width, Color = colour.WithAlpha(alpha), MaskFilter = blur };
            c.DrawLine(emitter, tip, glow);
        }

        using (var core = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = s * 0.009f, Color = Lighten(colour, 0.85f) })
        {
            c.DrawLine(emitter, tip, core);
        }

        // Hilt: brushed metal with a dark grip, an emitter shroud and an activation button.
        c.Save();
        c.Translate(from.X, from.Y);
        c.RotateRadians((float)Math.Atan2(uy, ux));
        var hw = s * 0.018f;
        using (var metal = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateLinearGradient(new SKPoint(0, -hw), new SKPoint(0, hw), new[] { new SKColor(0xF5, 0xF5, 0xF5), new SKColor(0x9E, 0x9E, 0x9E), new SKColor(0x42, 0x42, 0x42) }, SKShaderTileMode.Clamp)
        })
        {
            c.DrawRoundRect(new SKRect(0, -hw, hiltLen, hw), hw * 0.3f, hw * 0.3f, metal);
            c.DrawRoundRect(new SKRect(hiltLen * 0.8f, -hw * 1.3f, hiltLen, hw * 1.3f), hw * 0.3f, hw * 0.3f, metal);
        }

        using (var grip = new SKPaint { IsAntialias = true, Color = new SKColor(0x21, 0x21, 0x21) })
        {
            for (var x = hiltLen * 0.08f; x < hiltLen * 0.55f; x += hw * 0.7f)
            {
                c.DrawRect(x, -hw, hw * 0.35f, hw * 2, grip);
            }
        }

        using (var button = new SKPaint { IsAntialias = true, Color = new SKColor(0xE5, 0x39, 0x35) })
        {
            c.DrawCircle(hiltLen * 0.66f, -hw * 0.45f, hw * 0.3f, button);
        }

        c.Restore();
    }

    // ---------- Ghostbusters Day: friendly sheet ghosts and green slime ----------

    private static void Ghosts(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);

        using (var night = new SKPaint { Color = new SKColor(0x06, 0x14, 0x10, 110) })
        {
            c.DrawRect(0, 0, w, h, night);
        }

        // Slime oozing from the top edge.
        var slime = new SKColor(0x7C, 0xE0, 0x3A);
        using (var ooze = new SKPaint { IsAntialias = true, Color = slime.WithAlpha(235), ImageFilter = SKImageFilter.CreateDropShadow(0, s * 0.004f, s * 0.006f, s * 0.006f, new SKColor(0x2E, 0x7D, 0x32, 200)) })
        using (var gloop = new SKPath())
        {
            gloop.MoveTo(0, 0);
            gloop.LineTo(w, 0);
            gloop.LineTo(w, h * 0.03f);
            for (var x = (float)w; x > 0;)
            {
                var step = s * R(rng, 0.04f, 0.09f);
                var drip = rng.NextDouble() < 0.45 ? h * R(rng, 0.05f, 0.14f) : h * R(rng, 0.01f, 0.03f);
                var mid = x - (step / 2);
                gloop.QuadTo(x - (step * 0.1f), h * 0.03f, mid + (step * 0.18f), (h * 0.03f) + drip);
                gloop.ArcTo(new SKRect(mid - (step * 0.18f), (h * 0.03f) + drip - (step * 0.18f), mid + (step * 0.18f), (h * 0.03f) + drip + (step * 0.18f)), 0, 180, false);
                gloop.QuadTo(x - (step * 0.9f), h * 0.03f, x - step, h * 0.03f);
                x -= step;
            }

            gloop.LineTo(0, 0);
            gloop.Close();
            c.DrawPath(gloop, ooze);
        }

        // A dark city along the bottom, its windows lit green.
        Skyline(c, w, h, s, rng, h * 0.06f, h * 0.11f, new SKColor(0xB9, 0xF6, 0xCA, 170));

        // Ghosts float above the city, kept clear of the title, the slime and each other.
        var placed = new List<(float X, float Y, float R)>();
        for (var i = 0; i < 6; i++)
        {
            var size = s * (i < 3 ? R(rng, 0.06f, 0.075f) : R(rng, 0.042f, 0.052f));
            for (var attempt = 0; attempt < 60; attempt++)
            {
                if (FindClear(rng, placed, size, size, w - size, (h * 0.18f) + size, (h * 0.88f) - size, 1.15f) is not { } at)
                {
                    break;
                }

                // The title runs from its label (about a third of the way down) to the line under it.
                if (Math.Abs(at.X - (w / 2f)) < (w * 0.46f) + size && at.Y > (h * 0.33f) - size && at.Y < (h * 0.64f) + size)
                {
                    continue;
                }

                Ghost(c, at.X, at.Y, size, R(rng, -14, 14), (byte)(i < 3 ? 240 : rng.Next(190, 230)));
                placed.Add((at.X, at.Y, size));
                break;
            }
        }
    }

    /// <summary>City blocks along the bottom edge, between the given heights, with some windows lit.</summary>
    private static void Skyline(SKCanvas c, int w, int h, float s, Random rng, float minHeight, float maxHeight, SKColor window)
    {
        using var city = new SKPaint { IsAntialias = true, Color = new SKColor(0x0B, 0x0B, 0x16, 245) };
        using var lit = new SKPaint { Color = window };
        var x = 0f;
        while (x < w)
        {
            var bw = s * R(rng, 0.06f, 0.12f);
            var bh = R(rng, minHeight, maxHeight);
            c.DrawRect(x, h - bh, bw + 1, bh, city);
            for (var wy = h - bh + (s * 0.02f); wy < h - (s * 0.02f); wy += s * 0.03f)
            {
                for (var wx = x + (s * 0.012f); wx < x + bw - (s * 0.015f); wx += s * 0.022f)
                {
                    if (rng.NextDouble() < 0.3)
                    {
                        c.DrawRect(wx, wy, s * 0.008f, s * 0.012f, lit);
                    }
                }
            }

            x += bw;
        }
    }

    private static void Ghost(SKCanvas c, float x, float y, float r, float rotation, byte alpha)
    {
        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(rotation);

        using var body = new SKPath();
        body.MoveTo(-r * 0.7f, 0);
        body.ArcTo(new SKRect(-r * 0.7f, -r, r * 0.7f, r * 0.4f), 180, 180, false);
        body.LineTo(r * 0.75f, r * 0.75f);
        var scallops = 4;
        for (var i = 0; i < scallops; i++)
        {
            var x0 = r * 0.75f - (i * (r * 1.5f / scallops));
            var x1 = x0 - (r * 1.5f / scallops);
            body.QuadTo((x0 + x1) / 2, r * (i % 2 == 0 ? 0.5f : 1f), x1, r * 0.75f);
        }

        body.Close();

        // Little arms, one waving, merged into the sheet so there's no seam.
        body.AddOval(new SKRect(-r * 0.98f, -r * 0.12f, -r * 0.55f, r * 0.12f));
        body.AddOval(new SKRect(r * 0.55f, -r * 0.42f, r * 0.9f, -r * 0.16f));
        using var merged = new SKPath();
        body.Simplify(merged);
        body.Reset();
        body.AddPath(merged);

        using (var sheet = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateLinearGradient(new SKPoint(0, -r), new SKPoint(0, r), new[] { new SKColor(0xFF, 0xFF, 0xFF, alpha), new SKColor(0xD6, 0xF5, 0xE3, alpha) }, SKShaderTileMode.Clamp),
            ImageFilter = SKImageFilter.CreateDropShadow(0, 0, r * 0.12f, r * 0.12f, new SKColor(0xB9, 0xF6, 0xCA, 160))
        })
        {
            c.DrawPath(body, sheet);
        }

        using var face = new SKPaint { IsAntialias = true, Color = new SKColor(0x1A, 0x1A, 0x22) };
        c.DrawOval(new SKRect(-r * 0.32f, -r * 0.42f, -r * 0.1f, -r * 0.1f), face);
        c.DrawOval(new SKRect(r * 0.1f, -r * 0.42f, r * 0.32f, -r * 0.1f), face);
        c.DrawOval(new SKRect(-r * 0.12f, r * 0.02f, r * 0.12f, r * 0.3f), face);
        c.Restore();
    }

    // ---------- Back to Hogwarts: a castle on a hill at night, with floating candles ----------

    private static void Castle(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);

        using (var night = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, h), new[] { new SKColor(0x05, 0x08, 0x1C, 170), new SKColor(0x10, 0x16, 0x38, 120) }, SKShaderTileMode.Clamp)
        })
        {
            c.DrawRect(0, 0, w, h, night);
        }

        using (var star = new SKPaint { IsAntialias = true })
        {
            for (var i = 0; i < 120; i++)
            {
                star.Color = SKColors.White.WithAlpha((byte)rng.Next(80, 220));
                c.DrawCircle(R(rng, 0, w), R(rng, 0, h * 0.7f), R(rng, s * 0.001f, s * 0.0035f), star);
            }
        }

        // Crescent moon.
        using (var moon = new SKPath())
        using (var bite = new SKPath())
        using (var crescent = new SKPath())
        using (var moonPaint = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xF4, 0xC7), ImageFilter = SKImageFilter.CreateDropShadow(0, 0, s * 0.03f, s * 0.03f, new SKColor(0xFF, 0xF4, 0xC7, 150)) })
        {
            var mc = new SKPoint(w * 0.14f, h * 0.16f);
            moon.AddCircle(mc.X, mc.Y, s * 0.07f);
            bite.AddCircle(mc.X + (s * 0.035f), mc.Y - (s * 0.02f), s * 0.062f);
            moon.Op(bite, SKPathOp.Difference, crescent);
            c.DrawPath(crescent, moonPaint);
        }

        // Floating candles in the sky above the title, clear of the moon and of each other.
        var placed = new List<(float X, float Y, float R)> { (w * 0.14f, h * 0.16f, s * 0.09f) };
        var candles = w > h ? 14 : 10;
        for (var i = 0; i < candles; i++)
        {
            var height = s * R(rng, 0.045f, 0.065f);
            if (FindClear(rng, placed, height * 0.6f, w * 0.04f, w * 0.96f, h * 0.07f, h * 0.27f, 1.1f) is { } at)
            {
                Candle(c, at.X, at.Y - (height * 0.4f), height);
                placed.Add((at.X, at.Y, height * 0.6f));
            }
        }

        // The castle on the far shore of a lake, mirrored in the water. Drawn twice from the same seed so the
        // reflection matches, the second time flipped about the shoreline and faded.
        var shore = h * 0.88f;
        var unit = s * 0.036f;
        var seed = rng.Next();
        using (var hill = new SKPaint { IsAntialias = true, Color = new SKColor(0x14, 0x18, 0x30) })
        {
            using var land = new SKPath();
            land.MoveTo(0, shore);
            land.QuadTo(w * 0.25f, shore - (h * 0.05f), w * 0.5f, shore - (h * 0.04f));
            land.QuadTo(w * 0.75f, shore - (h * 0.03f), w, shore - (h * 0.06f));
            land.LineTo(w, shore);
            land.Close();
            c.DrawPath(land, hill);
        }

        CastleSilhouette(c, w / 2f, shore, unit, new Random(seed), reflection: false);

        using (var water = new SKPaint { Shader = SKShader.CreateLinearGradient(new SKPoint(0, shore), new SKPoint(0, h), new[] { new SKColor(0x0C, 0x14, 0x34, 235), new SKColor(0x05, 0x08, 0x1C, 245) }, SKShaderTileMode.Clamp) })
        {
            c.DrawRect(0, shore, w, h - shore, water);
        }

        c.Save();
        c.ClipRect(new SKRect(0, shore, w, h));
        using (var fade = new SKPaint { Color = SKColors.White.WithAlpha(80) })
        {
            c.SaveLayer(fade);
            c.Scale(1, -1, 0, shore);
            CastleSilhouette(c, w / 2f, shore, unit, new Random(seed), reflection: true);
            c.Restore();
        }

        // Ripples: short level strokes of moonlight on the water.
        using (var ripple = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = s * 0.003f, Color = new SKColor(0xC8, 0xD4, 0xFF, 110) })
        {
            for (var y = shore + (s * 0.015f); y < h; y += s * 0.018f)
            {
                for (var k = 0; k < 3; k++)
                {
                    var x = R(rng, 0, w);
                    c.DrawLine(x, y, x + (s * R(rng, 0.02f, 0.06f)), y, ripple);
                }
            }
        }

        c.Restore();
    }

    /// <summary>A many-towered castle in silhouette with lit windows; its base sits on <paramref name="ground"/> at <paramref name="cx"/>.</summary>
    private static void CastleSilhouette(SKCanvas c, float cx, float ground, float unit, Random rng, bool reflection)
    {
        using var dark = new SKPaint { IsAntialias = true, Color = new SKColor(0x2A, 0x31, 0x5A) };
        if (!reflection)
        {
            dark.ImageFilter = SKImageFilter.CreateDropShadow(0, 0, unit * 0.15f, unit * 0.15f, new SKColor(0x8C, 0x9E, 0xFF, 110));
        }

        // Tallest towers at the ends and in the middle, lower halls between; slim spires on the tall ones.
        var towers = new[]
        {
            (X: -6.2f, Wd: 0.8f, Ht: 4.4f), (X: -5.0f, Wd: 1.6f, Ht: 2.6f), (X: -3.2f, Wd: 1.0f, Ht: 3.6f), (X: -2.0f, Wd: 1.8f, Ht: 2.2f),
            (X: -0.5f, Wd: 1.0f, Ht: 4.8f), (X: 0.7f, Wd: 1.8f, Ht: 2.6f), (X: 2.7f, Wd: 1.0f, Ht: 3.8f), (X: 3.9f, Wd: 1.5f, Ht: 2.4f), (X: 5.6f, Wd: 0.8f, Ht: 4.2f)
        };
        using var window = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xD5, 0x80) };
        if (!reflection)
        {
            window.ImageFilter = SKImageFilter.CreateDropShadow(0, 0, unit * 0.15f, unit * 0.15f, new SKColor(0xFF, 0xB3, 0x00, 180));
        }

        using var castle = new SKPath();
        castle.AddRect(new SKRect(cx - (unit * 6.2f), ground - (unit * 1.4f), cx + (unit * 6.4f), ground + 2));
        foreach (var t in towers)
        {
            var left = cx + (t.X * unit);
            var right = left + (t.Wd * unit);
            var top = ground - (t.Ht * unit);
            castle.AddRect(new SKRect(left, top, right, ground + 2));
            var slim = t.Wd < 1.2f;
            castle.MoveTo(left - (unit * 0.12f), top);
            castle.LineTo((left + right) / 2, top - (unit * (slim ? 1.8f : 0.9f)));
            castle.LineTo(right + (unit * 0.12f), top);
            castle.Close();
        }

        // Battlements along the curtain wall.
        for (var x = cx - (unit * 6.1f); x < cx + (unit * 6.3f); x += unit * 0.5f)
        {
            castle.AddRect(new SKRect(x, ground - (unit * 1.62f), x + (unit * 0.26f), ground - (unit * 1.38f)));
        }

        using (var merged = new SKPath())
        {
            castle.Simplify(merged);
            c.DrawPath(merged, dark);
        }

        foreach (var t in towers)
        {
            var left = cx + (t.X * unit);
            var right = left + (t.Wd * unit);
            var top = ground - (t.Ht * unit);
            for (var wy = top + (unit * 0.45f); wy < ground - (unit * 0.6f); wy += unit * 0.8f)
            {
                if (rng.NextDouble() < 0.7)
                {
                    var wx = ((left + right) / 2) - (unit * 0.1f);
                    c.DrawRoundRect(new SKRect(wx, wy, wx + (unit * 0.2f), wy + (unit * 0.32f)), unit * 0.1f, unit * 0.1f, window);
                }
            }
        }
    }

    private static void Candle(SKCanvas c, float x, float top, float height)
    {
        var width = height * 0.26f;
        using (var halo = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(new SKPoint(x, top - (height * 0.15f)), height * 0.7f, new[] { new SKColor(0xFF, 0xD5, 0x80, 90), SKColors.Transparent }, SKShaderTileMode.Clamp)
        })
        {
            c.DrawCircle(x, top - (height * 0.15f), height * 0.7f, halo);
        }

        using (var wax = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateLinearGradient(new SKPoint(x - (width / 2), 0), new SKPoint(x + (width / 2), 0), new[] { new SKColor(0xFF, 0xF8, 0xE7), new SKColor(0xD7, 0xCC, 0xB8) }, SKShaderTileMode.Clamp)
        })
        {
            c.DrawRoundRect(new SKRect(x - (width / 2), top, x + (width / 2), top + height), width * 0.15f, width * 0.15f, wax);
        }

        using var flame = new SKPath();
        flame.MoveTo(x, top - (height * 0.32f));
        flame.CubicTo(x + (width * 0.45f), top - (height * 0.12f), x + (width * 0.3f), top - (height * 0.02f), x, top - (height * 0.02f));
        flame.CubicTo(x - (width * 0.3f), top - (height * 0.02f), x - (width * 0.45f), top - (height * 0.12f), x, top - (height * 0.32f));
        flame.Close();
        using var fire = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xC1, 0x07), ImageFilter = SKImageFilter.CreateDropShadow(0, 0, width * 0.3f, width * 0.3f, new SKColor(0xFF, 0x8F, 0x00)) };
        c.DrawPath(flame, fire);
    }

    // ---------- Bonfire Night: a bonfire with rising sparks under fireworks ----------

    private static void BonfireScene(SKCanvas c, int w, int h, SKColor tint, Random rng)
    {
        float s = Math.Min(w, h);

        using (var night = new SKPaint { Color = new SKColor(0x07, 0x05, 0x10, 120) })
        {
            c.DrawRect(0, 0, w, h, night);
        }

        var colours = new[] { new SKColor(0xFF, 0xD7, 0x40), new SKColor(0xFF, 0x52, 0x7A), new SKColor(0x64, 0xD8, 0xFF), new SKColor(0xB3, 0x88, 0xFF), new SKColor(0x69, 0xF0, 0xAE) };
        // Fireworks in the sky above the title.
        var bursts = w > h ? 4 : 3;
        for (var i = 0; i < bursts; i++)
        {
            var bx = (w / (float)bursts * (i + 0.5f)) + R(rng, -w * 0.03f, w * 0.03f);
            Burst(c, bx, h * (i % 2 == 0 ? 0.13f : 0.17f), s * R(rng, 0.08f, 0.1f), colours[(i + rng.Next(2)) % colours.Length], rng);
        }

        // Treeline on the horizon, then the dark field.
        var ground = h * 0.94f;
        using (var trees = new SKPaint { IsAntialias = true, Color = new SKColor(0x10, 0x0A, 0x08, 235) })
        using (var line = new SKPath())
        {
            var horizon = h * 0.86f;
            line.MoveTo(0, h);
            line.LineTo(0, horizon);
            for (var x = 0f; x <= w; x += s * 0.02f)
            {
                line.LineTo(x + (s * 0.01f), horizon - (s * R(rng, 0.02f, 0.05f)));
                line.LineTo(x + (s * 0.02f), horizon - (s * R(rng, 0.0f, 0.015f)));
            }

            line.LineTo(w, h);
            line.Close();
            c.DrawPath(line, trees);
        }

        using (var field = new SKPaint { IsAntialias = true, Color = new SKColor(0x0C, 0x08, 0x06, 245) })
        {
            c.DrawOval(new SKRect(-w * 0.2f, ground - (h * 0.04f), w * 1.2f, h * 1.3f), field);
        }

        var fx = w * 0.5f;
        var fireH = s * 0.26f;

        // Warm glow over the ground and sky around the fire.
        using (var glow = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(new SKPoint(fx, ground - (fireH * 0.3f)), fireH * 1.6f, new[] { new SKColor(0xFF, 0x8F, 0x00, 120), SKColors.Transparent }, SKShaderTileMode.Clamp)
        })
        {
            c.DrawCircle(fx, ground - (fireH * 0.3f), fireH * 1.6f, glow);
        }

        // People standing round the fire, warming up, in silhouette against the glow.
        foreach (var (px, ph) in new[] { (-0.62f, 0.2f), (-0.45f, 0.17f), (0.5f, 0.19f), (0.7f, 0.15f) })
        {
            Person(c, fx + (px * s * (w > h ? 0.75f : 0.62f)), ground + (s * 0.01f), s * ph);
        }

        // Logs stacked flat under the flames: two side by side, one across the top, cut ends showing.
        using (var log = new SKPaint { IsAntialias = true, Color = new SKColor(0x4E, 0x34, 0x2E) })
        using (var cut = new SKPaint { IsAntialias = true, Color = new SKColor(0xC8, 0x9A, 0x6A) })
        {
            var lh = s * 0.03f;
            foreach (var (x0, x1, y) in new[] { (-0.42f, -0.02f, 0f), (0.02f, 0.42f, 0f), (-0.25f, 0.25f, -1f) })
            {
                var rect = new SKRect(fx + (x0 * fireH), ground + (y * lh) - lh, fx + (x1 * fireH), ground + (y * lh));
                c.DrawRoundRect(rect, lh / 2, lh / 2, log);
                c.DrawCircle(x0 < 0 && y == 0 ? rect.Left + (lh / 2) : rect.Right - (lh / 2), rect.MidY, lh * 0.4f, cut);
            }
        }

        // Flames: layered tongues from deep red to yellow.
        var layers = new[]
        {
            (Colour: new SKColor(0xD8, 0x43, 0x15, 230), Height: 1f, Width: 0.34f),
            (Colour: new SKColor(0xFF, 0x8F, 0x00, 235), Height: 0.78f, Width: 0.26f),
            (Colour: new SKColor(0xFF, 0xD5, 0x4F, 240), Height: 0.5f, Width: 0.17f)
        };
        foreach (var (colour, height, width) in layers)
        {
            using var paint = new SKPaint { IsAntialias = true, Color = colour, ImageFilter = SKImageFilter.CreateDropShadow(0, 0, s * 0.01f, s * 0.01f, colour) };
            for (var t = 0; t < 5; t++)
            {
                var offset = (t - 2) * fireH * width * 0.35f;
                var th = fireH * height * R(rng, 0.65f, 1f) * (t == 2 ? 1f : 0.8f);
                var lean = R(rng, -fireH * 0.08f, fireH * 0.08f);
                using var tongue = new SKPath();
                tongue.MoveTo(fx + offset - (fireH * width * 0.35f), ground - (fireH * 0.08f));
                tongue.CubicTo(fx + offset - (fireH * width * 0.4f), ground - (th * 0.5f), fx + offset + lean, ground - (th * 0.75f), fx + offset + lean, ground - th);
                tongue.CubicTo(fx + offset + lean + (fireH * width * 0.1f), ground - (th * 0.6f), fx + offset + (fireH * width * 0.4f), ground - (th * 0.4f), fx + offset + (fireH * width * 0.35f), ground - (fireH * 0.08f));
                tongue.Close();
                c.DrawPath(tongue, paint);
            }
        }

        // Sparks drifting up round the flames (not up into the title).
        using var spark = new SKPaint { IsAntialias = true };
        for (var i = 0; i < 50; i++)
        {
            spark.Color = (rng.NextDouble() < 0.5 ? new SKColor(0xFF, 0xB3, 0x00) : new SKColor(0xFF, 0xE0, 0x82)).WithAlpha((byte)rng.Next(120, 255));
            c.DrawCircle(fx + R(rng, -fireH * 0.75f, fireH * 0.75f), ground - (fireH * R(rng, 0.45f, 1.05f)), R(rng, s * 0.002f, s * 0.005f), spark);
        }
    }

    /// <summary>A person standing in silhouette, seen from behind: head, shoulders, coat and legs. h is their height.</summary>
    private static void Person(SKCanvas c, float x, float ground, float height)
    {
        using var p = new SKPaint { IsAntialias = true, Color = new SKColor(0x08, 0x05, 0x04) };
        using var body = new SKPath();
        body.AddCircle(x, ground - (height * 0.9f), height * 0.085f);
        body.MoveTo(x - (height * 0.15f), ground - (height * 0.42f));
        body.CubicTo(x - (height * 0.16f), ground - (height * 0.7f), x - (height * 0.12f), ground - (height * 0.79f), x, ground - (height * 0.8f));
        body.CubicTo(x + (height * 0.12f), ground - (height * 0.79f), x + (height * 0.16f), ground - (height * 0.7f), x + (height * 0.15f), ground - (height * 0.42f));
        body.Close();
        body.AddRect(new SKRect(x - (height * 0.1f), ground - (height * 0.44f), x - (height * 0.015f), ground));
        body.AddRect(new SKRect(x + (height * 0.015f), ground - (height * 0.44f), x + (height * 0.1f), ground));
        using var merged = new SKPath();
        body.Simplify(merged);
        c.DrawPath(merged, p);
    }
}
