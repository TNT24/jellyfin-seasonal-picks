using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>Scene-style decorations for particular seasons (tree, lightsabers, ghosts, castle, bonfire).</summary>
internal static partial class CoverArt
{
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

    // ---------- Ghostbusters Day: a haunted city under a spectral storm (drawn in CoverArtSpooky.cs) ----------

    private static void Ghosts(SKCanvas c, int w, int h, Random rng) => SpookyHauntedCity(c, w, h, rng);

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

    // ---------- Back to Hogwarts: a castle on a cliff over a moonlit lake (drawn in CoverArtSpooky.cs) ----------

    private static void Castle(SKCanvas c, int w, int h, Random rng) => SpookyCastleLake(c, w, h, rng);

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
        FestBonfire(c, w, h, tint, rng);
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
