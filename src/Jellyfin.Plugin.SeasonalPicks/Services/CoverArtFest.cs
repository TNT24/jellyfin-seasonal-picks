using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>
/// Illustrated scenes for the default cover (a night at the pictures), Space, Star Wars Day, Bonfire Night and
/// Lunar New Year, plus the lighting helpers they share. Everything here is prefixed Fest so it can't collide with
/// the other themes' helpers.
/// </summary>
internal static partial class CoverArt
{
    // ====================================================================================================
    // Shared lighting and sky helpers
    // ====================================================================================================

    /// <summary>A soft radial glow, brightest in the middle, screened over what's below by default.</summary>
    private static void FestGlow(SKCanvas c, float x, float y, float r, SKColor col, byte alpha, SKBlendMode mode = SKBlendMode.Screen)
    {
        if (r <= 0)
        {
            return;
        }

        using var shader = SKShader.CreateRadialGradient(
            new SKPoint(x, y),
            r,
            new[] { col.WithAlpha(alpha), col.WithAlpha((byte)(alpha * 0.45f)), col.WithAlpha((byte)(alpha * 0.14f)), col.WithAlpha(0) },
            new[] { 0f, 0.22f, 0.55f, 1f },
            SKShaderTileMode.Clamp);
        using var p = new SKPaint { IsAntialias = true, Shader = shader, BlendMode = mode };
        c.DrawCircle(x, y, r, p);
    }

    /// <summary>A vertical gradient band from <paramref name="y0"/> to <paramref name="y1"/>.</summary>
    private static void FestBand(SKCanvas c, int w, float y0, float y1, SKColor col, byte a0, byte a1)
    {
        using var shader = SKShader.CreateLinearGradient(new SKPoint(0, y0), new SKPoint(0, y1), new[] { col.WithAlpha(a0), col.WithAlpha(a1) }, SKShaderTileMode.Clamp);
        using var p = new SKPaint { Shader = shader };
        c.DrawRect(0, Math.Min(y0, y1), w, Math.Abs(y1 - y0), p);
    }

    private static SKColor FestMix(SKColor a, SKColor b, float t)
        => new(
            (byte)(a.Red + ((b.Red - a.Red) * t)),
            (byte)(a.Green + ((b.Green - a.Green) * t)),
            (byte)(a.Blue + ((b.Blue - a.Blue) * t)),
            (byte)(a.Alpha + ((b.Alpha - a.Alpha) * t)));

    /// <summary>Whether a point sits in the band the title is drawn over (with some padding).</summary>
    private static bool FestInTitle(float x, float y, int w, int h, float pad)
        => Math.Abs(x - (w / 2f)) < (w * 0.44f) + pad && Math.Abs(y - (h / 2f)) < (h * 0.16f) + pad;

    /// <summary>A four-pointed glint with a halo.</summary>
    private static void FestTwinkle(SKCanvas c, float x, float y, float r, SKColor col, byte glow = 120)
    {
        FestGlow(c, x, y, r * 1.3f, col, glow);
        using var p = new SKPaint { IsAntialias = true, Color = Lighten(col, 0.65f), BlendMode = SKBlendMode.Screen };
        using var path = new SKPath();
        var t = r * 0.08f;
        path.MoveTo(x, y - r);
        path.QuadTo(x + t, y - t, x + r, y);
        path.QuadTo(x + t, y + t, x, y + r);
        path.QuadTo(x - t, y + t, x - r, y);
        path.QuadTo(x - t, y - t, x, y - r);
        path.Close();
        c.DrawPath(path, p);

        // A smaller diagonal cross for sparkle.
        var d = r * 0.4f;
        using var diag = new SKPath();
        diag.MoveTo(x - d, y - d);
        diag.QuadTo(x, y - (t * 0.6f), x + d, y - d);
        diag.QuadTo(x + (t * 0.6f), y, x + d, y + d);
        diag.QuadTo(x, y + (t * 0.6f), x - d, y + d);
        diag.QuadTo(x - (t * 0.6f), y, x - d, y - d);
        diag.Close();
        p.Color = Lighten(col, 0.5f).WithAlpha(150);
        c.DrawPath(diag, p);
        p.Color = SKColors.White;
        c.DrawCircle(x, y, r * 0.1f, p);
    }

    /// <summary>A starfield with depth: faint dust, nearer coloured stars and a few bright glinting ones off the title.</summary>
    private static void FestStarfield(SKCanvas c, int w, int h, float minY, float maxY, float s, Random rng, int count, int bright = 6)
    {
        using var p = new SKPaint { IsAntialias = true };
        for (var i = 0; i < count; i++)
        {
            p.Color = SKColors.White.WithAlpha((byte)rng.Next(35, 150));
            c.DrawCircle(R(rng, 0, w), R(rng, minY, maxY), s * R(rng, 0.0007f, 0.0016f), p);
        }

        var tints = new[] { SKColors.White, new SKColor(0xCA, 0xDC, 0xFF), new SKColor(0xFF, 0xE9, 0xC4), new SKColor(0xD8, 0xC8, 0xFF) };
        for (var i = 0; i < count / 5; i++)
        {
            float x = R(rng, 0, w), y = R(rng, minY, maxY), r = s * R(rng, 0.0018f, 0.0032f);
            var col = tints[rng.Next(tints.Length)];
            FestGlow(c, x, y, r * 4, col, 60);
            p.Color = col.WithAlpha((byte)rng.Next(170, 255));
            c.DrawCircle(x, y, r, p);
        }

        for (var i = 0; i < bright * 3 && bright > 0; i++)
        {
            float x = R(rng, w * 0.03f, w * 0.97f), y = R(rng, minY, maxY), r = s * R(rng, 0.014f, 0.026f);
            if (FestInTitle(x, y, w, h, r))
            {
                continue;
            }

            FestTwinkle(c, x, y, r, tints[rng.Next(tints.Length)], 90);
            if (--bright <= 0)
            {
                break;
            }
        }
    }

    /// <summary>A soft cloud of glowing gas made of layered, blurred lobes in two colours.</summary>
    private static void FestNebula(SKCanvas c, float cx, float cy, float rx, float ry, SKColor a, SKColor b, byte alpha, Random rng)
    {
        var m = Math.Max(rx, ry);
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, m * 0.22f))
        using (var p = new SKPaint { IsAntialias = true, MaskFilter = blur, BlendMode = SKBlendMode.Screen })
        {
            for (var i = 0; i < 10; i++)
            {
                float px = cx + (R(rng, -1, 1) * rx * 0.75f), py = cy + (R(rng, -1, 1) * ry * 0.75f);
                var pr = m * R(rng, 0.22f, 0.5f);
                p.Color = FestMix(a, b, (float)rng.NextDouble()).WithAlpha((byte)(alpha * R(rng, 0.45f, 1f)));
                c.DrawOval(new SKRect(px - (pr * 1.5f), py - (pr * 0.75f), px + (pr * 1.5f), py + (pr * 0.75f)), p);
            }
        }

        // Brighter wisps inside the cloud.
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, m * 0.07f))
        using (var p = new SKPaint { IsAntialias = true, MaskFilter = blur, BlendMode = SKBlendMode.Screen })
        {
            for (var i = 0; i < 6; i++)
            {
                float px = cx + (R(rng, -1, 1) * rx * 0.5f), py = cy + (R(rng, -1, 1) * ry * 0.5f);
                var pr = m * R(rng, 0.08f, 0.18f);
                p.Color = Lighten(FestMix(a, b, (float)rng.NextDouble()), 0.35f).WithAlpha((byte)(alpha * 0.6f));
                c.DrawOval(new SKRect(px - (pr * 1.8f), py - (pr * 0.5f), px + (pr * 1.8f), py + (pr * 0.5f)), p);
            }
        }
    }

    /// <summary>
    /// A planet lit from the upper left: an atmosphere halo, shaded body with optional cloud bands, a soft night
    /// side, a bright rim on the lit edge and, optionally, banded rings tilted by <paramref name="ringTilt"/> degrees.
    /// </summary>
    private static void FestPlanet(SKCanvas c, float x, float y, float r, SKColor light, SKColor dark, SKColor atmo, Random rng, bool bands, float? ringTilt = null, SKColor? ringColour = null, bool litFromRight = false)
    {
        c.Save();
        if (litFromRight)
        {
            c.Scale(-1, 1, x, 0);
        }

        var ringCol = ringColour ?? new SKColor(0xE8, 0xD8, 0xB8);
        if (ringTilt is { } tilt)
        {
            FestRings(c, x, y, r, tilt, ringCol, front: false);
        }

        // Atmosphere halo just outside the disc.
        using (var halo = SKShader.CreateRadialGradient(new SKPoint(x, y), r * 1.32f, new[] { atmo.WithAlpha(0), atmo.WithAlpha(150), atmo.WithAlpha(40), atmo.WithAlpha(0) }, new[] { 0.7f, 0.76f, 0.86f, 1f }, SKShaderTileMode.Clamp))
        using (var p = new SKPaint { IsAntialias = true, Shader = halo, BlendMode = SKBlendMode.Screen })
        {
            c.DrawCircle(x, y, r * 1.32f, p);
        }

        using var disc = new SKPath();
        disc.AddCircle(x, y, r);
        using (var body = SKShader.CreateRadialGradient(new SKPoint(x - (r * 0.45f), y - (r * 0.45f)), r * 2f, new[] { Lighten(light, 0.3f), light, dark, Darken(dark, 0.4f) }, new[] { 0f, 0.3f, 0.65f, 1f }, SKShaderTileMode.Clamp))
        using (var p = new SKPaint { IsAntialias = true, Shader = body })
        {
            c.DrawPath(disc, p);
        }

        c.Save();
        c.ClipPath(disc, antialias: true);
        if (bands)
        {
            using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.04f);
            using var band = new SKPaint { IsAntialias = true, MaskFilter = blur };
            c.Save();
            c.RotateDegrees(ringTilt ?? -8, x, y);
            for (var i = 0; i < 7; i++)
            {
                band.Color = (i % 2 == 0 ? Lighten(light, 0.35f) : Darken(dark, 0.8f)).WithAlpha((byte)rng.Next(35, 80));
                var by = y - (r * 0.85f) + (i * r * 0.27f) + R(rng, -r * 0.04f, r * 0.04f);
                var bh = r * R(rng, 0.04f, 0.1f);
                c.DrawOval(new SKRect(x - (r * 1.3f), by - bh, x + (r * 1.3f), by + bh), band);
            }

            c.Restore();
        }

        // Night side: a soft dark disc pushed down and right.
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.22f))
        using (var night = new SKPaint { IsAntialias = true, Color = new SKColor(0x02, 0x02, 0x08, 200), MaskFilter = blur })
        {
            c.DrawCircle(x + (r * 0.62f), y + (r * 0.62f), r * 1.15f, night);
        }

        // Bright rim along the lit edge.
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.04f))
        using (var rim = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = r * 0.07f, Color = Lighten(atmo, 0.4f).WithAlpha(200), MaskFilter = blur, BlendMode = SKBlendMode.Screen })
        {
            c.DrawArc(new SKRect(x - r, y - r, x + r, y + r), 140, 170, false, rim);
        }

        c.Restore();

        if (ringTilt is { } tilt2)
        {
            FestRings(c, x, y, r, tilt2, ringCol, front: true);
        }

        c.Restore();
    }

    /// <summary>Half of a planet's ring system: the back half (behind the planet) or the front half.</summary>
    private static void FestRings(SKCanvas c, float x, float y, float r, float tilt, SKColor col, bool front)
    {
        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(tilt);
        var outer = r * 2.25f;
        c.ClipRect(front ? new SKRect(-outer * 1.1f, 0, outer * 1.1f, outer) : new SKRect(-outer * 1.1f, -outer, outer * 1.1f, 0));
        using var p = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke };
        var bandsSpec = new[] { (1.4f, 0.1f, 90), (1.55f, 0.16f, 200), (1.72f, 0.08f, 120), (1.86f, 0.14f, 210), (2.04f, 0.1f, 110), (2.18f, 0.05f, 70) };
        foreach (var (k, width, alpha) in bandsSpec)
        {
            p.StrokeWidth = r * width;
            p.Color = (front ? col : Darken(col, 0.7f)).WithAlpha((byte)alpha);
            c.DrawOval(new SKRect(-r * k, -r * k * 0.24f, r * k, r * k * 0.24f), p);
        }

        c.Restore();
    }

    /// <summary>Glowing dust drifting in a region (bokeh-style specks), kept off the title.</summary>
    private static void FestDust(SKCanvas c, int w, int h, float minY, float maxY, float s, Random rng, int count, SKColor col)
    {
        using var p = new SKPaint { IsAntialias = true, BlendMode = SKBlendMode.Screen };
        for (var i = 0; i < count; i++)
        {
            float x = R(rng, 0, w), y = R(rng, minY, maxY), r = s * R(rng, 0.0015f, 0.004f);
            if (FestInTitle(x, y, w, h, 0))
            {
                continue;
            }

            p.Color = col.WithAlpha((byte)rng.Next(90, 220));
            c.DrawCircle(x, y, r, p);
        }
    }

    /// <summary>Out-of-focus discs of light, the further-off lights of a scene.</summary>
    private static void FestBokeh(SKCanvas c, int w, int h, float minY, float maxY, float s, Random rng, int count, SKColor[] colours, float minR, float maxR, bool avoidTitle = true)
    {
        using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.004f);
        using var p = new SKPaint { IsAntialias = true, MaskFilter = blur, BlendMode = SKBlendMode.Screen };
        using var ring = new SKPaint { IsAntialias = true, MaskFilter = blur, BlendMode = SKBlendMode.Screen, Style = SKPaintStyle.Stroke };
        for (var i = 0; i < count; i++)
        {
            float x = R(rng, 0, w), y = R(rng, minY, maxY), r = s * R(rng, minR, maxR);
            if (avoidTitle && FestInTitle(x, y, w, h, r))
            {
                continue;
            }

            var col = colours[rng.Next(colours.Length)];
            p.Color = col.WithAlpha((byte)rng.Next(30, 75));
            c.DrawCircle(x, y, r, p);
            ring.StrokeWidth = r * 0.08f;
            ring.Color = col.WithAlpha((byte)rng.Next(30, 70));
            c.DrawCircle(x, y, r * 0.96f, ring);
        }
    }

    // ====================================================================================================
    // Out of season (the default cover): a night at the pictures
    // ====================================================================================================

    /// <summary>
    /// Marquee bulbs framing the cover, searchlights sweeping up behind the title, gold glints, and a film strip
    /// curling along the bottom between a popcorn bucket and a clapperboard. Uses the season colour for the film.
    /// </summary>
    private static void FestCinema(SKCanvas c, int w, int h, SKColor tint, Random rng)
    {
        float s = Math.Min(w, h);
        var gold = new SKColor(0xFF, 0xD2, 0x7A);
        var warm = new SKColor(0xFF, 0xE8, 0xB8);

        var wide = w > h;

        // A dark auditorium: deeper at the top and over the floor.
        FestBand(c, w, 0, h * 0.35f, new SKColor(0x04, 0x03, 0x08), 190, 40);
        FestBand(c, w, h * 0.6f, h, new SKColor(0x04, 0x03, 0x08), 40, 225);

        // Two spotlights from up in the wings, meeting in a pool of light on the floor.
        var beamCol = FestMix(warm, Lighten(tint, 0.6f), 0.25f);
        FestBeam(c, new SKPoint(w * 0.22f, h * 0.06f), new SKPoint(w * 0.56f, h * 1.02f), s * 0.03f, s * (wide ? 0.42f : 0.36f), beamCol, 70);
        FestBeam(c, new SKPoint(w * 0.78f, h * 0.06f), new SKPoint(w * 0.44f, h * 1.02f), s * 0.03f, s * (wide ? 0.42f : 0.36f), beamCol, 70);
        using (var pool = SKShader.CreateRadialGradient(new SKPoint(w * 0.5f, h * 0.97f), w * 0.36f, new[] { beamCol.WithAlpha(110), beamCol.WithAlpha(40), beamCol.WithAlpha(0) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp))
        using (var p = new SKPaint { IsAntialias = true, Shader = pool, BlendMode = SKBlendMode.Screen })
        {
            c.Save();
            c.Scale(1, 0.3f, w * 0.5f, h * 0.97f);
            c.DrawCircle(w * 0.5f, h * 0.97f, w * 0.36f, p);
            c.Restore();
        }

        // Dust glinting in the beams, and gold sparkles up in the dark.
        FestDust(c, w, h, h * 0.12f, h * 0.95f, s, rng, 50, warm);
        FestBokeh(c, w, h, h * 0.12f, h * 0.3f, s, rng, 10, new[] { gold, warm, Lighten(tint, 0.5f) }, 0.012f, 0.03f);
        var placed = new List<(float X, float Y, float R)>();
        for (var i = 0; i < 5; i++)
        {
            var r = s * R(rng, 0.016f, 0.028f);
            if (FindClear(rng, placed, r, w * 0.24f, w * 0.76f, h * 0.17f, h * 0.29f, 3f) is { } at)
            {
                FestTwinkle(c, at.X, at.Y, r, gold, 110);
                placed.Add((at.X, at.Y, r));
            }
        }

        // Velvet curtains in the season's colour, tied back at the sides, under a swagged valance strung with lights.
        FestCurtain(c, w, h, s, tint, left: true);
        FestCurtain(c, w, h, s, tint, left: false);
        FestValance(c, w, h, s, tint);

        // The film strip curling over the floor, then the snacks in front of it.
        FestFilmRibbon(c, w, h, s, tint, rng);
        var popX = wide ? w * 0.13f : w * 0.17f;
        var clapX = wide ? w * 0.87f : w * 0.83f;
        FestGlow(c, popX, h * 0.88f, s * 0.24f, gold, 70);
        FestShadow(c, popX, h * 0.995f, s * 0.15f, s * 0.025f);
        Popcorn(c, popX, h * 0.995f, s * 0.22f, rng);
        FestShadow(c, clapX, h * 0.99f, s * 0.15f, s * 0.025f);
        Clapperboard(c, clapX, h * 0.985f, s * 0.21f);

        FestTwinkle(c, popX + (s * 0.08f), h * 0.74f, s * 0.022f, warm, 110);
        FestTwinkle(c, clapX + (s * 0.09f), h * 0.75f, s * 0.018f, warm, 110);
    }

    /// <summary>A gathered velvet curtain down one side, pinched by a gold tie-back below the middle.</summary>
    private static void FestCurtain(SKCanvas c, int w, int h, float s, SKColor tint, bool left)
    {
        c.Save();
        if (!left)
        {
            c.Scale(-1, 1, w / 2f, 0);
        }

        var wide = w > h;
        var topW = w * (wide ? 0.17f : 0.21f);
        var tieX = w * (wide ? 0.035f : 0.045f);
        var tieY = h * 0.6f;
        var footW = w * (wide ? 0.1f : 0.13f);

        // The curtain's inner edge through the points fraction f of the way across.
        SKPath Edge(float f, bool closeToWall)
        {
            var p = new SKPath();
            p.MoveTo(topW * f, 0);
            p.CubicTo(topW * f * 0.92f, h * 0.28f, tieX * f * 1.5f, tieY * 0.82f, tieX * f, tieY);
            p.CubicTo(tieX * f * 1.6f, tieY + (h * 0.12f), footW * f * 0.85f, h * 0.9f, footW * f, h);
            if (closeToWall)
            {
                p.LineTo(0, h);
                p.LineTo(0, 0);
                p.Close();
            }

            return p;
        }

        var dark = Darken(FestMix(tint, new SKColor(0x50, 0x08, 0x18), 0.2f), 0.22f);
        var light = Darken(FestMix(tint, new SKColor(0xA0, 0x20, 0x30), 0.15f), 0.85f);
        using var shape = Edge(1, true);
        using (var fill = new SKPaint { IsAntialias = true, Color = dark })
        {
            c.DrawPath(shape, fill);
        }

        c.Save();
        c.ClipPath(shape, antialias: true);
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.008f))
        using (var fold = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, MaskFilter = blur })
        {
            for (var f = 0.06f; f < 1.0f; f += 0.13f)
            {
                using var path = Edge(f, false);
                fold.StrokeWidth = s * 0.016f * (0.6f + f);
                fold.Color = light.WithAlpha((byte)(90 + (f * 120)));
                c.DrawPath(path, fold);
            }
        }

        // Spotlight catching the inner edge, darker in the shadow under the valance.
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.01f))
        using (var rim = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.014f, Color = Lighten(light, 0.35f).WithAlpha(150), MaskFilter = blur })
        using (var edge = Edge(0.97f, false))
        {
            c.DrawPath(edge, rim);
        }

        FestBand(c, w, 0, h * 0.25f, SKColors.Black, 150, 0);
        FestBand(c, w, h * 0.8f, h, SKColors.Black, 0, 130);
        c.Restore();

        // Gold rope tie-back with a tassel.
        var gold = new SKColor(0xE8, 0xB0, 0x48);
        using (var rope = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.01f, StrokeCap = SKStrokeCap.Round, Color = gold })
        using (var band = new SKPath())
        {
            band.MoveTo(0, tieY - (s * 0.012f));
            band.QuadTo(tieX * 0.6f, tieY + (s * 0.01f), tieX + (s * 0.004f), tieY - (s * 0.004f));
            c.DrawPath(band, rope);
        }

        using (var tassel = new SKPaint { IsAntialias = true, Color = gold })
        using (var path = new SKPath())
        {
            var tx = tieX * 0.8f;
            c.DrawCircle(tx, tieY + (s * 0.02f), s * 0.009f, tassel);
            path.MoveTo(tx - (s * 0.006f), tieY + (s * 0.026f));
            path.LineTo(tx + (s * 0.006f), tieY + (s * 0.026f));
            path.LineTo(tx + (s * 0.014f), tieY + (s * 0.075f));
            path.LineTo(tx - (s * 0.014f), tieY + (s * 0.075f));
            path.Close();
            c.DrawPath(path, tassel);
        }

        c.Restore();
    }

    /// <summary>A valance of velvet swags across the top, trimmed with gold braid and strung with lit bulbs.</summary>
    private static void FestValance(SKCanvas c, int w, int h, float s, SKColor tint)
    {
        var swags = w > h ? 5 : 4;
        var top = h * 0.045f;
        var sag = h * 0.05f;
        var span = w / (float)swags;
        var dark = Darken(FestMix(tint, new SKColor(0x50, 0x08, 0x18), 0.2f), 0.3f);
        var light = FestMix(tint, new SKColor(0xA0, 0x20, 0x30), 0.15f);

        using var shape = new SKPath();
        shape.MoveTo(0, 0);
        shape.LineTo(w, 0);
        shape.LineTo(w, top);
        for (var i = swags - 1; i >= 0; i--)
        {
            shape.QuadTo((i + 0.5f) * span, top + (sag * 2), i * span, top);
        }

        shape.Close();
        using (var shadowBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.015f))
        using (var shadow = new SKPaint { IsAntialias = true, Color = SKColors.Black.WithAlpha(160), MaskFilter = shadowBlur })
        {
            c.Save();
            c.Translate(0, s * 0.012f);
            c.DrawPath(shape, shadow);
            c.Restore();
        }

        using (var fill = new SKPaint { IsAntialias = true, Color = dark })
        {
            c.DrawPath(shape, fill);
        }

        // Folds following each swag's curve.
        c.Save();
        c.ClipPath(shape, antialias: true);
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.005f))
        using (var fold = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.01f, MaskFilter = blur })
        {
            for (var i = 0; i < swags; i++)
            {
                foreach (var k in new[] { 0.35f, 0.65f, 0.9f })
                {
                    using var arc = new SKPath();
                    arc.MoveTo(i * span, top * k);
                    arc.QuadTo((i + 0.5f) * span, (top + (sag * 2)) * k, (i + 1) * span, top * k);
                    fold.Color = light.WithAlpha((byte)(90 + (k * 120)));
                    c.DrawPath(arc, fold);
                }
            }
        }

        c.Restore();

        // Gold braid along the hem, and a bulb string along each swag.
        var gold = new SKColor(0xE8, 0xB0, 0x48);
        using (var braid = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.006f, Color = gold })
        {
            for (var i = 0; i < swags; i++)
            {
                using var arc = new SKPath();
                arc.MoveTo(i * span, top);
                arc.QuadTo((i + 0.5f) * span, top + (sag * 2), (i + 1) * span, top);
                c.DrawPath(arc, braid);
            }
        }

        for (var i = 0; i < swags; i++)
        {
            const int Bulbs = 6;
            for (var b = 1; b < Bulbs; b++)
            {
                var t = b / (float)Bulbs;
                var x = (i + t) * span;
                var y = top + (2 * t * (1 - t) * sag * 2) + (s * 0.012f);
                FestBulb(c, x, y, s * 0.009f);
            }

            // A tassel where two swags meet.
            using var tassel = new SKPaint { IsAntialias = true, Color = gold };
            using var path = new SKPath();
            var tx = i * span;
            if (i > 0)
            {
                c.DrawCircle(tx, top + (s * 0.006f), s * 0.009f, tassel);
                path.MoveTo(tx - (s * 0.005f), top + (s * 0.012f));
                path.LineTo(tx + (s * 0.005f), top + (s * 0.012f));
                path.LineTo(tx + (s * 0.012f), top + (s * 0.06f));
                path.LineTo(tx - (s * 0.012f), top + (s * 0.06f));
                path.Close();
                c.DrawPath(path, tassel);
            }
        }
    }

    /// <summary>A soft searchlight beam from <paramref name="from"/> towards <paramref name="to"/>, widening and fading as it goes.</summary>
    private static void FestBeam(SKCanvas c, SKPoint from, SKPoint to, float startWidth, float endWidth, SKColor col, byte alpha)
    {
        float dx = to.X - from.X, dy = to.Y - from.Y;
        var len = (float)Math.Sqrt((dx * dx) + (dy * dy));
        float nx = -dy / len, ny = dx / len;
        using var path = new SKPath();
        path.MoveTo(from.X + (nx * startWidth / 2), from.Y + (ny * startWidth / 2));
        path.LineTo(to.X + (nx * endWidth / 2), to.Y + (ny * endWidth / 2));
        path.LineTo(to.X - (nx * endWidth / 2), to.Y - (ny * endWidth / 2));
        path.LineTo(from.X - (nx * startWidth / 2), from.Y - (ny * startWidth / 2));
        path.Close();
        using var shader = SKShader.CreateLinearGradient(from, to, new[] { col.WithAlpha(alpha), col.WithAlpha((byte)(alpha * 0.55f)), col.WithAlpha(0) }, new[] { 0f, 0.45f, 1f }, SKShaderTileMode.Clamp);
        using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, startWidth * 0.5f);
        using var p = new SKPaint { IsAntialias = true, Shader = shader, MaskFilter = blur, BlendMode = SKBlendMode.Screen };
        c.DrawPath(path, p);
    }

    /// <summary>A soft oval shadow on the floor under an object.</summary>
    private static void FestShadow(SKCanvas c, float x, float y, float rx, float ry)
    {
        using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, ry * 0.6f);
        using var p = new SKPaint { IsAntialias = true, Color = SKColors.Black.WithAlpha(150), MaskFilter = blur };
        c.DrawOval(new SKRect(x - rx, y - ry, x + rx, y + ry), p);
    }

    /// <summary>One lit marquee bulb with its halo.</summary>
    private static void FestBulb(SKCanvas c, float x, float y, float r)
    {
        FestGlow(c, x, y, r * 4.5f, new SKColor(0xFF, 0xC8, 0x60), 120);
        using (var socket = new SKPaint { IsAntialias = true, Color = new SKColor(0x5A, 0x42, 0x20) })
        {
            c.DrawCircle(x, y, r * 1.25f, socket);
        }

        using var shader = SKShader.CreateRadialGradient(new SKPoint(x - (r * 0.25f), y - (r * 0.25f)), r * 1.2f, new[] { SKColors.White, new SKColor(0xFF, 0xF0, 0xC0), new SKColor(0xFF, 0xB8, 0x40) }, new[] { 0f, 0.45f, 1f }, SKShaderTileMode.Clamp);
        using var p = new SKPaint { IsAntialias = true, Shader = shader };
        c.DrawCircle(x, y, r, p);
    }

    /// <summary>A film strip following a gentle S-curve across the bottom, its frames lit in shades of the season colour.</summary>
    private static void FestFilmRibbon(SKCanvas c, int w, int h, float s, SKColor tint, Random rng)
    {
        var p0 = new SKPoint(-w * 0.08f, h * 0.96f);
        var p1 = new SKPoint(w * 0.3f, h * 0.74f);
        var p2 = new SKPoint(w * 0.68f, h * 1.0f);
        var p3 = new SKPoint(w * 1.08f, h * 0.8f);
        var width = s * 0.115f;

        SKPoint At(float t)
        {
            var u = 1 - t;
            return new SKPoint(
                (u * u * u * p0.X) + (3 * u * u * t * p1.X) + (3 * u * t * t * p2.X) + (t * t * t * p3.X),
                (u * u * u * p0.Y) + (3 * u * u * t * p1.Y) + (3 * u * t * t * p2.Y) + (t * t * t * p3.Y));
        }

        using var curve = new SKPath();
        curve.MoveTo(p0);
        curve.CubicTo(p1, p2, p3);

        // Shadow, then the film itself.
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.012f))
        using (var shadow = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = width, Color = SKColors.Black.WithAlpha(150), MaskFilter = blur })
        {
            c.Save();
            c.Translate(0, s * 0.012f);
            c.DrawPath(curve, shadow);
            c.Restore();
        }

        using (var film = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = width, Color = new SKColor(0x14, 0x12, 0x16) })
        {
            c.DrawPath(curve, film);
        }

        // Walk the curve at even spacing, placing sprocket holes and frames.
        var samples = new List<(SKPoint P, float Len)>();
        var prev = At(0);
        float total = 0;
        samples.Add((prev, 0));
        for (var i = 1; i <= 400; i++)
        {
            var pt = At(i / 400f);
            total += SKPoint.Distance(prev, pt);
            samples.Add((pt, total));
            prev = pt;
        }

        var k = 0;
        (SKPoint P, float Angle) Place(float d)
        {
            while (k < samples.Count - 2 && samples[k + 1].Len < d)
            {
                k++;
            }

            var a = samples[k].P;
            var b = samples[Math.Min(k + 1, samples.Count - 1)].P;
            return (a, (float)(Math.Atan2(b.Y - a.Y, b.X - a.X) * 180 / Math.PI));
        }

        using var hole = new SKPaint { IsAntialias = true, Color = new SKColor(0xE8, 0xE0, 0xD0, 230) };
        var holeStep = width * 0.2f;
        k = 0;
        for (var d = holeStep / 2; d < total; d += holeStep)
        {
            var (pt, angle) = Place(d);
            c.Save();
            c.Translate(pt.X, pt.Y);
            c.RotateDegrees(angle);
            foreach (var side in new[] { -1f, 1f })
            {
                var cy = side * width * 0.4f;
                c.DrawRoundRect(new SKRect(-width * 0.045f, cy - (width * 0.04f), width * 0.045f, cy + (width * 0.04f)), width * 0.015f, width * 0.015f, hole);
            }

            c.Restore();
        }

        var frameW = width * 0.88f;
        var frameStep = frameW + (width * 0.09f);
        var shades = new[]
        {
            (Lighten(tint, 0.55f), Darken(tint, 0.45f)),
            (new SKColor(0xFF, 0xC8, 0x7A), Darken(tint, 0.35f)),
            (Lighten(tint, 0.3f), new SKColor(0x1A, 0x14, 0x30)),
            (new SKColor(0xFF, 0xE6, 0xB0), Darken(tint, 0.6f))
        };
        var n = rng.Next(shades.Length);
        k = 0;
        for (var d = frameStep / 2; d < total; d += frameStep)
        {
            var (pt, angle) = Place(d);
            c.Save();
            c.Translate(pt.X, pt.Y);
            c.RotateDegrees(angle);
            var frame = new SKRect(-frameW / 2, -width * 0.3f, frameW / 2, width * 0.3f);
            var (top, bottom) = shades[n++ % shades.Length];
            using (var shader = SKShader.CreateLinearGradient(new SKPoint(0, frame.Top), new SKPoint(0, frame.Bottom), new[] { top, bottom }, SKShaderTileMode.Clamp))
            using (var paint = new SKPaint { IsAntialias = true, Shader = shader })
            {
                c.DrawRoundRect(frame, width * 0.03f, width * 0.03f, paint);
            }

            // A little horizon and a light in each frame, like a still from a film.
            using (var land = new SKPaint { IsAntialias = true, Color = SKColors.Black.WithAlpha(90) })
            {
                c.Save();
                c.ClipRect(frame);
                c.DrawOval(new SKRect(frame.Left - (frameW * 0.2f), frame.Bottom - (width * 0.16f), frame.Right * 0.6f, frame.Bottom + (width * 0.2f)), land);
                c.DrawOval(new SKRect(frame.Left + (frameW * 0.3f), frame.Bottom - (width * 0.12f), frame.Right + (frameW * 0.3f), frame.Bottom + (width * 0.2f)), land);
                c.Restore();
            }

            using (var sheen = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(40) })
            {
                c.DrawRect(frame.Left, frame.Top, frameW, width * 0.08f, sheen);
            }

            c.Restore();
        }
    }

    // ====================================================================================================
    // Space: a planet's horizon at sunrise under nebulae, a ringed world and a moon
    // ====================================================================================================

    private static void FestSpace(SKCanvas c, int w, int h, SKColor tint, Random rng)
    {
        float s = Math.Min(w, h);
        using (var dark = new SKPaint { Color = new SKColor(0x02, 0x03, 0x10, 165) })
        {
            c.DrawRect(0, 0, w, h, dark);
        }

        FestBand(c, w, 0, h * 0.35f, new SKColor(0x02, 0x02, 0x0A), 120, 0);

        var violet = new SKColor(0x8E, 0x3B, 0xC8);
        var magenta = new SKColor(0xD8, 0x3E, 0x8C);
        var teal = new SKColor(0x1E, 0xA8, 0xC8);
        var blue = new SKColor(0x2A, 0x4C, 0xD8);
        FestNebula(c, w * 0.78f, h * 0.2f, s * 0.42f, s * 0.24f, FestMix(violet, tint, 0.15f), magenta, 120, rng);
        FestNebula(c, w * 0.12f, h * 0.55f, s * 0.3f, s * 0.35f, teal, blue, 90, rng);

        FestStarfield(c, w, h, 0, h * 0.86f, s, rng, w > h ? 320 : 260, 6);

        // A ringed world high on the left and a small moon on the right.
        FestPlanet(c, w * 0.17f, h * 0.17f, s * 0.085f, new SKColor(0xF2, 0xC8, 0x8E), new SKColor(0x8A, 0x4A, 0x2A), new SKColor(0xFF, 0xC8, 0x8E), rng, bands: true, ringTilt: -18);
        FestPlanet(c, w * 0.86f, h * 0.13f, s * 0.035f, new SKColor(0xC8, 0xD4, 0xE8), new SKColor(0x40, 0x48, 0x60), new SKColor(0xA8, 0xC8, 0xFF), rng, bands: false);

        // A shooting star.
        FestStreak(c, w * 0.62f, h * 0.08f, w * 0.5f, h * 0.15f, s);

        FestPlanetHorizon(c, w, h, s, FestMix(new SKColor(0x40, 0xC8, 0xFF), tint, 0.25f), new SKPoint(w * 0.3f, 0), rng);
    }

    /// <summary>A meteor streak, bright at its head.</summary>
    private static void FestStreak(SKCanvas c, float x0, float y0, float x1, float y1, float s)
    {
        using var shader = SKShader.CreateLinearGradient(new SKPoint(x0, y0), new SKPoint(x1, y1), new[] { SKColors.White.WithAlpha(0), SKColors.White.WithAlpha(220) }, SKShaderTileMode.Clamp);
        using var p = new SKPaint { IsAntialias = true, Shader = shader, StrokeWidth = s * 0.003f, StrokeCap = SKStrokeCap.Round };
        c.DrawLine(x0, y0, x1, y1, p);
        FestGlow(c, x1, y1, s * 0.015f, SKColors.White, 200);
    }

    /// <summary>
    /// The curved limb of a huge planet across the bottom, a thin bright atmosphere and a sun rising over its edge
    /// at <paramref name="sunAt"/>.X, with a lens streak.
    /// </summary>
    private static void FestPlanetHorizon(SKCanvas c, int w, int h, float s, SKColor atmo, SKPoint sunAt, Random rng)
    {
        var big = Math.Max(w, h) * 1.25f;
        var top = h * 0.8f;
        var centre = new SKPoint(w * 0.55f, top + big);

        // Haze above the limb.
        using (var haze = SKShader.CreateRadialGradient(centre, big * 1.12f, new[] { atmo.WithAlpha(0), atmo.WithAlpha(130), atmo.WithAlpha(0) }, new[] { 0.86f, 0.893f, 1f }, SKShaderTileMode.Clamp))
        using (var p = new SKPaint { IsAntialias = true, Shader = haze, BlendMode = SKBlendMode.Screen })
        {
            c.DrawRect(0, 0, w, h, p);
        }

        // The sun, just rising: drawn before the planet so the limb cuts it off.
        var sunX = sunAt.X;
        var sunY = centre.Y - (float)Math.Sqrt(Math.Max(0, (big * big) - ((sunX - centre.X) * (sunX - centre.X))));
        FestGlow(c, sunX, sunY, s * 0.6f, FestMix(new SKColor(0xFF, 0xC0, 0x80), atmo, 0.3f), 140);
        FestGlow(c, sunX, sunY, s * 0.16f, new SKColor(0xFF, 0xF4, 0xE0), 255);
        using (var core = new SKPaint { IsAntialias = true, Color = SKColors.White })
        {
            c.DrawCircle(sunX, sunY + (s * 0.01f), s * 0.035f, core);
        }

        using var disc = new SKPath();
        disc.AddCircle(centre.X, centre.Y, big);
        using (var body = SKShader.CreateLinearGradient(new SKPoint(0, top), new SKPoint(0, h), new[] { new SKColor(0x14, 0x2A, 0x4A), new SKColor(0x08, 0x10, 0x22), new SKColor(0x03, 0x05, 0x0E) }, new[] { 0f, 0.35f, 1f }, SKShaderTileMode.Clamp))
        using (var p = new SKPaint { IsAntialias = true, Shader = body })
        {
            c.DrawPath(disc, p);
        }

        // Cloud swirls on the day side of the planet, near the limb.
        c.Save();
        c.ClipPath(disc, antialias: true);
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.012f))
        using (var cloud = new SKPaint { IsAntialias = true, MaskFilter = blur })
        {
            for (var i = 0; i < 14; i++)
            {
                var x = R(rng, 0, w);
                var limbY = centre.Y - (float)Math.Sqrt(Math.Max(0, (big * big) - ((x - centre.X) * (x - centre.X))));
                var y = limbY + (s * R(rng, 0.01f, 0.09f));
                var near = 1 - Math.Min(1, Math.Abs(x - sunAt.X) / w);
                cloud.Color = Lighten(atmo, 0.6f).WithAlpha((byte)(20 + (near * 60)));
                c.DrawOval(new SKRect(x - (s * R(rng, 0.06f, 0.16f)), y - (s * 0.006f), x + (s * R(rng, 0.06f, 0.16f)), y + (s * 0.006f)), cloud);
            }
        }

        // Light from the sunrise washing across the limb.
        FestGlow(c, sunX, sunY, s * 0.5f, Lighten(atmo, 0.3f), 110);
        c.Restore();

        // The thin bright atmosphere along the limb.
        foreach (var (width, sigma, alpha) in new[] { (s * 0.03f, s * 0.02f, (byte)150), (s * 0.008f, s * 0.004f, (byte)230) })
        {
            using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, sigma);
            using var shader = SKShader.CreateRadialGradient(new SKPoint(sunX, sunY), w * 0.8f, new[] { Lighten(atmo, 0.6f), atmo, atmo.WithAlpha(60) }, new[] { 0f, 0.4f, 1f }, SKShaderTileMode.Clamp);
            using var p = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = width, Shader = shader, MaskFilter = blur, BlendMode = SKBlendMode.Screen, Color = SKColors.White.WithAlpha(alpha) };
            c.DrawCircle(centre.X, centre.Y, big, p);
        }

        // Light spilling over the edge: a halo and a horizontal lens streak.
        FestGlow(c, sunX, sunY, s * 0.3f, new SKColor(0xFF, 0xE8, 0xC8), 150);
        FestGlow(c, sunX, sunY, s * 0.07f, SKColors.White, 230);
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.006f))
        using (var shader = SKShader.CreateLinearGradient(new SKPoint(sunX - (w * 0.5f), 0), new SKPoint(sunX + (w * 0.5f), 0), new[] { SKColors.Transparent, new SKColor(0xD8, 0xF0, 0xFF, 200), SKColors.Transparent }, SKShaderTileMode.Clamp))
        using (var p = new SKPaint { IsAntialias = true, Shader = shader, MaskFilter = blur, BlendMode = SKBlendMode.Screen })
        {
            c.DrawOval(new SKRect(sunX - (w * 0.5f), sunY - (s * 0.005f), sunX + (w * 0.5f), sunY + (s * 0.005f)), p);
        }
    }

    // ====================================================================================================
    // Star Wars Day: two energy blades clashing on a rocky ridge under a ringed world
    // ====================================================================================================

    private static void FestSaberDuel(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var blue = new SKColor(0x2E, 0x9C, 0xFF);
        var red = new SKColor(0xFF, 0x26, 0x2A);

        using (var dark = new SKPaint { Color = new SKColor(0x02, 0x02, 0x0A, 175) })
        {
            c.DrawRect(0, 0, w, h, dark);
        }

        FestNebula(c, w * 0.85f, h * 0.22f, s * 0.36f, s * 0.22f, new SKColor(0xC0, 0x30, 0x40), new SKColor(0x70, 0x20, 0x80), 90, rng);
        FestNebula(c, w * 0.1f, h * 0.35f, s * 0.3f, s * 0.25f, new SKColor(0x20, 0x50, 0xC0), new SKColor(0x30, 0x90, 0xD0), 70, rng);
        FestStarfield(c, w, h, 0, h * 0.85f, s, rng, w > h ? 300 : 240, 5);

        // A big desert world filling the top-left corner, with a pale moon beside it.
        FestPlanet(c, w * 0.08f, h * 0.06f, s * 0.2f, new SKColor(0xF2, 0xC2, 0x84), new SKColor(0xA0, 0x58, 0x28), new SKColor(0xFF, 0xC8, 0x90), rng, bands: true, litFromRight: true);
        FestPlanet(c, w * (w > h ? 0.27f : 0.34f), h * 0.2f, s * 0.03f, new SKColor(0xD8, 0xDC, 0xE8), new SKColor(0x50, 0x54, 0x68), new SKColor(0xC0, 0xD0, 0xFF), rng, bands: false);

        // Coloured light from the blades filling the lower half.
        FestGlow(c, w * 0.18f, h * 0.9f, s * 0.75f, blue, 110);
        FestGlow(c, w * 0.82f, h * 0.9f, s * 0.75f, red, 110);

        var bluePommel = new SKPoint(w * (w > h ? 0.15f : 0.1f), h * 0.99f);
        var redPommel = new SKPoint(w - bluePommel.X, bluePommel.Y);
        var tipY = h * (w > h ? 0.74f : 0.68f); // under the title's accent line
        var blueTip = new SKPoint(w * (w > h ? 0.68f : 0.7f), tipY);
        var redTip = new SKPoint(w - blueTip.X, tipY);

        // A rocky ridge in the foreground, rim-lit by the blades.
        using var ridge = new SKPath();
        ridge.MoveTo(0, h);
        ridge.LineTo(0, h * 0.86f);
        var x = 0f;
        while (x < w)
        {
            var step = s * R(rng, 0.04f, 0.09f);
            x += step;
            var y = h * (0.86f + (0.05f * (float)Math.Pow(Math.Abs((x / w) - 0.5f) * 2, 2) * -1)) + (s * R(rng, -0.02f, 0.02f));
            ridge.LineTo(x - (step * 0.5f), y - (s * R(rng, 0.0f, 0.025f)));
            ridge.LineTo(x, y);
        }

        ridge.LineTo(w, h);
        ridge.Close();
        using (var rock = new SKPaint { IsAntialias = true, Color = new SKColor(0x05, 0x05, 0x0B) })
        {
            c.DrawPath(ridge, rock);
        }

        c.Save();
        c.ClipPath(ridge, antialias: true);
        FestGlow(c, w * 0.22f, h * 0.88f, s * 0.35f, blue, 120);
        FestGlow(c, w * 0.78f, h * 0.88f, s * 0.35f, red, 120);
        FestGlow(c, w * 0.5f, h * 0.8f, s * 0.25f, new SKColor(0xFF, 0xF0, 0xE0), 90);
        c.Restore();

        // Rim light along the ridge's edge.
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.003f))
        using (var shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(w, 0), new[] { blue.WithAlpha(200), new SKColor(0xFF, 0xF0, 0xF0, 160), red.WithAlpha(200) }, SKShaderTileMode.Clamp))
        using (var rim = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.004f, Shader = shader, MaskFilter = blur, BlendMode = SKBlendMode.Screen })
        {
            c.Save();
            c.ClipRect(new SKRect(0, 0, w, h * 0.995f));
            c.DrawPath(ridge, rim);
            c.Restore();
        }

        FestSaber(c, bluePommel, blueTip, blue, s);
        FestSaber(c, redPommel, redTip, red, s);

        // The clash where the blades cross: a white-hot flare, a cross streak and sparks.
        var clash = FestCross(bluePommel, blueTip, redPommel, redTip);
        FestGlow(c, clash.X, clash.Y, s * 0.22f, new SKColor(0xFF, 0xE8, 0xD0), 150);
        FestGlow(c, clash.X, clash.Y, s * 0.05f, SKColors.White, 255);
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.004f))
        using (var streak = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(200), MaskFilter = blur, BlendMode = SKBlendMode.Screen })
        {
            c.DrawOval(new SKRect(clash.X - (s * 0.11f), clash.Y - (s * 0.003f), clash.X + (s * 0.11f), clash.Y + (s * 0.003f)), streak);
        }

        using var spark = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, BlendMode = SKBlendMode.Screen };
        for (var i = 0; i < 26; i++)
        {
            var a = R(rng, (float)Math.PI * 0.1f, (float)Math.PI * 1.9f) + (float)Math.PI * 0.5f;
            var inner = s * R(rng, 0.02f, 0.05f);
            var outer = inner + (s * R(rng, 0.02f, 0.07f));
            spark.StrokeWidth = s * R(rng, 0.002f, 0.004f);
            spark.Color = (rng.NextDouble() < 0.5 ? new SKColor(0xFF, 0xF4, 0xD0) : new SKColor(0xFF, 0xC8, 0x80)).WithAlpha((byte)rng.Next(150, 255));
            float ex = clash.X + (float)(Math.Cos(a) * outer), ey = clash.Y + (float)(Math.Sin(a) * outer);
            if (ey < h * 0.66f)
            {
                continue; // nothing flying up into the title
            }

            c.DrawLine(clash.X + (float)(Math.Cos(a) * inner), clash.Y + (float)(Math.Sin(a) * inner), ex, ey, spark);
        }
    }

    /// <summary>Where two segments' lines cross.</summary>
    private static SKPoint FestCross(SKPoint a0, SKPoint a1, SKPoint b0, SKPoint b1)
    {
        float d1x = a1.X - a0.X, d1y = a1.Y - a0.Y, d2x = b1.X - b0.X, d2y = b1.Y - b0.Y;
        var den = (d1x * d2y) - (d1y * d2x);
        if (Math.Abs(den) < 1e-3f)
        {
            return new SKPoint((a1.X + b1.X) / 2, (a1.Y + b1.Y) / 2);
        }

        var t = (((b0.X - a0.X) * d2y) - ((b0.Y - a0.Y) * d2x)) / den;
        return new SKPoint(a0.X + (t * d1x), a0.Y + (t * d1y));
    }

    /// <summary>An energy blade from the pommel to the tip: a wide bloom, a coloured glow, a white-hot core and a metal hilt.</summary>
    private static void FestSaber(SKCanvas c, SKPoint pommel, SKPoint tip, SKColor col, float s)
    {
        float dx = tip.X - pommel.X, dy = tip.Y - pommel.Y;
        var len = (float)Math.Sqrt((dx * dx) + (dy * dy));
        float ux = dx / len, uy = dy / len;
        var hiltLen = s * 0.17f;
        var emitter = new SKPoint(pommel.X + (ux * hiltLen), pommel.Y + (uy * hiltLen));
        var bladeStart = new SKPoint(emitter.X + (ux * s * 0.01f), emitter.Y + (uy * s * 0.01f));

        // Wide bloom (screened, so it lights what's behind), then saturated glows, then the hot core.
        foreach (var (width, sigma, alpha, mode) in new[]
        {
            (s * 0.2f, s * 0.08f, (byte)130, SKBlendMode.Screen),
            (s * 0.07f, s * 0.022f, (byte)255, SKBlendMode.Screen),
            (s * 0.045f, s * 0.012f, (byte)255, SKBlendMode.SrcOver),
            (s * 0.028f, s * 0.005f, (byte)255, SKBlendMode.SrcOver)
        })
        {
            using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, sigma);
            using var glow = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = width, Color = col.WithAlpha(alpha), MaskFilter = blur, BlendMode = mode };
            c.DrawLine(bladeStart, tip, glow);
        }

        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.003f))
        using (var blade = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = s * 0.024f, Color = Lighten(col, 0.1f), MaskFilter = blur })
        {
            c.DrawLine(bladeStart, tip, blade);
        }

        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.002f))
        using (var core = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = s * 0.008f, Color = Lighten(col, 0.92f), MaskFilter = blur })
        {
            c.DrawLine(bladeStart, tip, core);
        }

        // Hilt, drawn along its own axis.
        c.Save();
        c.Translate(pommel.X, pommel.Y);
        c.RotateRadians((float)Math.Atan2(uy, ux));
        var hw = s * 0.017f;
        using (var metal = SKShader.CreateLinearGradient(new SKPoint(0, -hw * 1.3f), new SKPoint(0, hw * 1.3f), new[] { new SKColor(0xFF, 0xFF, 0xFF), new SKColor(0xB8, 0xBC, 0xC4), new SKColor(0x5A, 0x5E, 0x66), new SKColor(0x22, 0x24, 0x28) }, new[] { 0f, 0.3f, 0.65f, 1f }, SKShaderTileMode.Clamp))
        using (var p = new SKPaint { IsAntialias = true, Shader = metal })
        {
            c.DrawRoundRect(new SKRect(0, -hw * 1.1f, hiltLen * 0.1f, hw * 1.1f), hw * 0.3f, hw * 0.3f, p); // pommel cap
            c.DrawRect(hiltLen * 0.08f, -hw, hiltLen * 0.72f, hw * 2, p); // body
            c.DrawRoundRect(new SKRect(hiltLen * 0.76f, -hw * 1.35f, hiltLen, hw * 1.35f), hw * 0.25f, hw * 0.25f, p); // emitter shroud
        }

        using (var grip = new SKPaint { IsAntialias = true, Color = new SKColor(0x18, 0x18, 0x1C) })
        {
            for (var gx = hiltLen * 0.14f; gx < hiltLen * 0.5f; gx += hw * 0.75f)
            {
                c.DrawRoundRect(new SKRect(gx, -hw * 1.05f, gx + (hw * 0.4f), hw * 1.05f), hw * 0.1f, hw * 0.1f, grip);
            }

            c.DrawRect(hiltLen * 0.8f, -hw * 0.25f, hiltLen * 0.16f, hw * 0.12f, grip); // vent slot
        }

        using (var button = new SKPaint { IsAntialias = true, Color = Lighten(col, 0.2f) })
        {
            c.DrawRoundRect(new SKRect(hiltLen * 0.58f, -hw * 0.75f, hiltLen * 0.68f, -hw * 0.3f), hw * 0.1f, hw * 0.1f, button);
        }

        // The blade's colour reflected along the top of the hilt.
        using (var sheen = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(hiltLen, 0), new[] { col.WithAlpha(0), col.WithAlpha(180) }, SKShaderTileMode.Clamp))
        using (var p = new SKPaint { IsAntialias = true, Shader = sheen, BlendMode = SKBlendMode.Screen })
        {
            c.DrawRect(0, -hw * 1.35f, hiltLen, hw * 0.6f, p);
        }

        c.Restore();
    }

    // ====================================================================================================
    // Bonfire Night: a roaring bonfire, onlookers in silhouette, embers rising and fireworks overhead
    // ====================================================================================================

    private static void FestBonfire(SKCanvas c, int w, int h, SKColor tint, Random rng)
    {
        float s = Math.Min(w, h);
        var fireCol = new SKColor(0xFF, 0x7A, 0x1A);

        // Night sky: deep blue at the top warming towards the fire.
        FestBand(c, w, 0, h * 0.6f, new SKColor(0x04, 0x05, 0x16), 200, 120);
        FestBand(c, w, h * 0.6f, h, new SKColor(0x10, 0x06, 0x06), 120, 220);
        FestStarfield(c, w, h, 0, h * 0.4f, s, rng, 90, 0);

        // Drifting smoke lit from below.
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.05f))
        using (var smoke = new SKPaint { IsAntialias = true, MaskFilter = blur })
        {
            for (var i = 0; i < 9; i++)
            {
                float sx = R(rng, 0, w), sy = h * R(rng, 0.12f, 0.3f), sw = s * R(rng, 0.2f, 0.4f);
                smoke.Color = new SKColor(0x8A, 0x70, 0x90, (byte)rng.Next(14, 30));
                c.DrawOval(new SKRect(sx - sw, sy - (sw * 0.3f), sx + sw, sy + (sw * 0.3f)), smoke);
            }
        }

        // Fireworks across the top.
        var colours = new[] { new SKColor(0xFF, 0xD7, 0x40), new SKColor(0xFF, 0x4F, 0x7A), new SKColor(0x5C, 0xD0, 0xFF), new SKColor(0xB8, 0x88, 0xFF), new SKColor(0x70, 0xF0, 0xA0) };
        var first = rng.Next(colours.Length);
        var spots = w > h
            ? new[] { (0.12f, 0.15f, 0.12f), (0.37f, 0.1f, 0.09f), (0.66f, 0.12f, 0.11f), (0.89f, 0.17f, 0.085f) }
            : new[] { (0.17f, 0.14f, 0.12f), (0.52f, 0.1f, 0.09f), (0.84f, 0.17f, 0.11f) };
        for (var i = 0; i < spots.Length; i++)
        {
            var (fx0, fy0, fr) = spots[i];
            FestFirework(c, w * fx0, h * fy0, s * fr, colours[(first + (i * 2)) % colours.Length], rng, s, willow: i == 1);
        }

        // Treeline of rounded crowns on the horizon, then the dark field.
        var horizon = h * 0.85f;
        using (var trees = new SKPaint { IsAntialias = true, Color = new SKColor(0x0A, 0x06, 0x08) })
        {
            c.DrawRect(0, horizon, w, h - horizon, trees);
            for (var x = -s * 0.02f; x <= w + (s * 0.05f);)
            {
                var r = s * R(rng, 0.025f, 0.05f);
                c.DrawCircle(x, horizon - (r * R(rng, 0.2f, 0.7f)), r, trees);
                x += r * R(rng, 0.9f, 1.4f);
            }
        }

        var fx = w * 0.5f;
        var ground = h * 0.965f;
        var fireH = s * 0.3f;

        // Firelight across the ground, trees and smoke.
        FestGlow(c, fx, ground - (fireH * 0.25f), s * 0.85f, fireCol, 120);
        FestGlow(c, fx, ground - (fireH * 0.3f), s * 0.38f, new SKColor(0xFF, 0xA0, 0x30), 160);


        FestFire(c, fx, ground, fireH, s, rng);
        FestEmbers(c, w, h, fx, ground - (fireH * 0.5f), s, rng);

        // Onlookers in the foreground, overlapping, rim-lit on the side facing the fire.
        var people = w > h
            ? new[] { (0.02f, 0.87f, 1.15f, 4), (0.1f, 0.83f, 1.0f, 1), (0.18f, 0.88f, 1.2f, 0), (0.27f, 0.88f, 0.9f, 3), (0.73f, 0.89f, 0.95f, 5), (0.81f, 0.84f, 1.05f, 0), (0.9f, 0.86f, 1.2f, 1), (0.99f, 0.84f, 1.0f, 4) }
            : new[] { (0.03f, 0.84f, 1.1f, 1), (0.13f, 0.87f, 1.2f, 4), (0.26f, 0.88f, 0.9f, 3), (0.75f, 0.89f, 0.95f, 5), (0.86f, 0.84f, 1.1f, 0), (0.97f, 0.87f, 1.2f, 1) };
        foreach (var (px, py, scale, kind) in people)
        {
            var x = w * px;
            FestOnlooker(c, x, h * py, s * 0.034f * scale, h, kind, x < fx ? 1 : -1, fireCol, rng);
        }
    }

    /// <summary>A firework burst: drooping streaks fading in from the centre, glowing tips and a central flash.</summary>
    private static void FestFirework(SKCanvas c, float x, float y, float r, SKColor col, Random rng, float s, bool willow = false)
    {
        FestGlow(c, x, y, r * 1.5f, col, 60);
        var n = willow ? 34 : 44;
        using var streak = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, Style = SKPaintStyle.Stroke, BlendMode = SKBlendMode.Screen };
        using var tipPaint = new SKPaint { IsAntialias = true, BlendMode = SKBlendMode.Screen };
        var light = Lighten(col, 0.55f);
        for (var i = 0; i < n; i++)
        {
            var a = (i * Math.PI * 2 / n) + R(rng, -0.06f, 0.06f);
            var len = r * R(rng, 0.75f, 1f);
            float cx = (float)Math.Cos(a), cy = (float)Math.Sin(a);
            var start = new SKPoint(x + (cx * len * 0.18f), y + (cy * len * 0.18f));
            var droop = r * (willow ? 0.55f : 0.16f);
            var end = new SKPoint(x + (cx * len), y + (cy * len) + droop);
            var mid = new SKPoint(x + (cx * len * 0.65f), y + (cy * len * 0.65f));
            using var path = new SKPath();
            path.MoveTo(start);
            path.QuadTo(mid, end);
            using var shader = SKShader.CreateLinearGradient(start, end, new[] { col.WithAlpha(0), col.WithAlpha(170), light.WithAlpha(240) }, new[] { 0f, 0.6f, 1f }, SKShaderTileMode.Clamp);
            streak.Shader = shader;
            streak.StrokeWidth = s * (willow ? 0.0028f : 0.0035f);
            c.DrawPath(path, streak);
            streak.Shader = null;

            tipPaint.Color = light;
            c.DrawCircle(end, s * 0.0035f, tipPaint);
            if (rng.NextDouble() < 0.35)
            {
                FestGlow(c, end.X, end.Y, s * 0.012f, col, 160);
            }

            // Some streaks leave a few glittering drops below their tips.
            if (willow || rng.NextDouble() < 0.25)
            {
                tipPaint.Color = light.WithAlpha(150);
                for (var k = 1; k <= 2; k++)
                {
                    c.DrawCircle(end.X, end.Y + (k * r * 0.07f), s * 0.0022f, tipPaint);
                }
            }
        }

        FestGlow(c, x, y, r * 0.3f, Lighten(col, 0.8f), 220);
    }

    /// <summary>Layered flames over a log pile: deep red outer tongues to a white-yellow core, added up so they glow.</summary>
    private static void FestFire(SKCanvas c, float x, float ground, float height, float s, Random rng)
    {

        var layers = new[]
        {
            (Colour: new SKColor(0xC8, 0x28, 0x08), Height: 1.0f, Width: 0.62f, Blur: 0.035f, Count: 9),
            (Colour: new SKColor(0xFF, 0x6A, 0x10), Height: 0.82f, Width: 0.48f, Blur: 0.025f, Count: 8),
            (Colour: new SKColor(0xFF, 0xB0, 0x30), Height: 0.6f, Width: 0.34f, Blur: 0.018f, Count: 7),
            (Colour: new SKColor(0xFF, 0xF0, 0xA0), Height: 0.36f, Width: 0.2f, Blur: 0.014f, Count: 5)
        };
        foreach (var (colour, lh, lw, lb, count) in layers)
        {
            using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, height * lb);
            using var paint = new SKPaint { IsAntialias = true, Color = colour.WithAlpha(210), MaskFilter = blur, BlendMode = SKBlendMode.Plus };
            for (var t = 0; t < count; t++)
            {
                var f = (t / (float)(count - 1)) - 0.5f; // -0.5 .. 0.5 across the fire
                var baseX = x + (f * height * lw * 1.3f);
                var th = height * lh * R(rng, 0.55f, 1f) * (1 - (Math.Abs(f) * 0.9f));
                var bw = height * lw * 0.35f;
                var lean = R(rng, -height * 0.1f, height * 0.1f) + (f * height * 0.2f);
                using var tongue = new SKPath();
                tongue.MoveTo(baseX - bw, ground);
                tongue.CubicTo(baseX - (bw * 1.1f), ground - (th * 0.45f), baseX + lean - (bw * 0.5f), ground - (th * 0.7f), baseX + lean, ground - th);
                tongue.CubicTo(baseX + lean + (bw * 0.2f), ground - (th * 0.6f), baseX + (bw * 1.1f), ground - (th * 0.4f), baseX + bw, ground);
                tongue.Close();
                c.DrawPath(tongue, paint);
            }
        }

        // Loose licks of flame breaking off above the fire.
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, height * 0.012f))
        using (var wisp = new SKPaint { IsAntialias = true, MaskFilter = blur, BlendMode = SKBlendMode.Plus })
        {
            for (var i = 0; i < 6; i++)
            {
                var wx = x + R(rng, -height * 0.25f, height * 0.25f);
                var wy = ground - (height * R(rng, 0.85f, 1.15f));
                var wr = height * R(rng, 0.03f, 0.06f);
                wisp.Color = (i % 2 == 0 ? new SKColor(0xFF, 0x80, 0x20) : new SKColor(0xFF, 0xB8, 0x40)).WithAlpha(200);
                using var drop = new SKPath();
                drop.MoveTo(wx, wy - (wr * 2.2f));
                drop.CubicTo(wx + (wr * 0.9f), wy - (wr * 0.6f), wx + (wr * 0.8f), wy + wr, wx, wy + wr);
                drop.CubicTo(wx - (wr * 0.8f), wy + wr, wx - (wr * 0.9f), wy - (wr * 0.6f), wx, wy - (wr * 2.2f));
                drop.Close();
                c.DrawPath(drop, wisp);
            }
        }

        // White-hot heart of the fire, and glowing embers in the logs at the base.
        FestGlow(c, x, ground - (height * 0.12f), height * 0.35f, new SKColor(0xFF, 0xF4, 0xC8), 200);
        using var ember = new SKPaint { IsAntialias = true, BlendMode = SKBlendMode.Plus };
        for (var i = 0; i < 30; i++)
        {
            ember.Color = new SKColor(0xFF, (byte)rng.Next(80, 180), 0x20, (byte)rng.Next(120, 220));
            c.DrawCircle(x + R(rng, -height * 0.45f, height * 0.45f), ground - R(rng, 0, height * 0.06f), s * R(rng, 0.002f, 0.006f), ember);
        }

        // Logs in front, their ends glowing.
        using (var log = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, Color = new SKColor(0x16, 0x0C, 0x08), StrokeWidth = height * 0.075f })
        {
            c.DrawLine(x - (height * 0.62f), ground + (height * 0.03f), x + (height * 0.12f), ground - (height * 0.05f), log);
            c.DrawLine(x + (height * 0.6f), ground + (height * 0.04f), x - (height * 0.1f), ground - (height * 0.03f), log);
        }

        using (var ring = new SKPaint { IsAntialias = true, Color = new SKColor(0x6A, 0x40, 0x24) })
        {
            c.DrawCircle(x - (height * 0.62f), ground + (height * 0.03f), height * 0.03f, ring);
            c.DrawCircle(x + (height * 0.6f), ground + (height * 0.04f), height * 0.03f, ring);
        }

        FestGlow(c, x + (height * 0.1f), ground - (height * 0.05f), height * 0.1f, new SKColor(0xFF, 0x90, 0x20), 220);
        FestGlow(c, x - (height * 0.08f), ground - (height * 0.03f), height * 0.1f, new SKColor(0xFF, 0x90, 0x20), 220);
    }

    /// <summary>Sparks and embers rising from the fire in a widening plume, short streaks with a few glowing.</summary>
    private static void FestEmbers(SKCanvas c, int w, int h, float x, float y, float s, Random rng)
    {
        using var streak = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, BlendMode = SKBlendMode.Plus };
        for (var i = 0; i < 90; i++)
        {
            var rise = (float)Math.Pow(rng.NextDouble(), 0.8) * h * 0.38f; // how far up it's got
            var spread = (s * 0.05f) + (rise * 0.75f);
            var ex = x + (R(rng, -1, 1) * spread * (float)Math.Sqrt(rng.NextDouble()));
            var ey = y - rise;
            if (FestInTitle(ex, ey, w, h, 0) && rng.NextDouble() < 0.75)
            {
                continue;
            }

            var heat = 1 - (rise / (h * 0.4f));
            var col = FestMix(new SKColor(0xFF, 0x50, 0x10), new SKColor(0xFF, 0xE8, 0x90), heat * (float)rng.NextDouble());
            streak.Color = col.WithAlpha((byte)(80 + (heat * 170)));
            streak.StrokeWidth = s * R(rng, 0.002f, 0.0045f);
            var drift = R(rng, -s * 0.012f, s * 0.012f);
            c.DrawLine(ex, ey, ex - drift, ey + (s * R(rng, 0.006f, 0.02f)), streak);
            if (rng.NextDouble() < 0.15)
            {
                FestGlow(c, ex, ey, s * 0.012f, col, 160);
            }
        }
    }

    /// <summary>
    /// A person seen from behind, head and shoulders, standing close to the viewer and cut off by the bottom edge.
    /// kind: 0 bare head, 1 bobble hat, 3 holding a sparkler up, 4 hood, 5 flat cap.
    /// <paramref name="towardsFire"/> is +1 if the fire is to their right; that edge gets a warm rim light.
    /// </summary>
    private static void FestOnlooker(SKCanvas c, float x, float headY, float hr, int h, int kind, int towardsFire, SKColor fire, Random rng)
    {
        // Built up from parts unioned together, so overlapping parts never leave holes.
        var merged = new SKPath();
        void Add(SKPath part)
        {
            var joined = merged.Op(part, SKPathOp.Union) ?? merged;
            if (!ReferenceEquals(joined, merged))
            {
                merged.Dispose();
                merged = joined;
            }

            part.Dispose();
        }

        SKPath Circle(float cx, float cy, float r)
        {
            var p = new SKPath();
            p.AddCircle(cx, cy, r);
            return p;
        }

        SKPath Oval(SKRect r)
        {
            var p = new SKPath();
            p.AddOval(r);
            return p;
        }

        Add(Circle(x, headY, hr));
        if (kind == 4)
        {
            Add(Oval(new SKRect(x - (hr * 1.25f), headY - (hr * 1.2f), x + (hr * 1.25f), headY + (hr * 1.3f))));
        }

        using (var body = new SKPath())
        {
            body.MoveTo(x - (hr * 0.45f), headY + (hr * 0.6f));
            body.LineTo(x - (hr * 0.5f), headY + (hr * 1.25f));
            body.CubicTo(x - (hr * 1.3f), headY + (hr * 1.35f), x - (hr * 1.9f), headY + (hr * 1.6f), x - (hr * 2.0f), headY + (hr * 2.6f));
            body.LineTo(x - (hr * 2.15f), h + hr);
            body.LineTo(x + (hr * 2.15f), h + hr);
            body.LineTo(x + (hr * 2.0f), headY + (hr * 2.6f));
            body.CubicTo(x + (hr * 1.9f), headY + (hr * 1.6f), x + (hr * 1.3f), headY + (hr * 1.35f), x + (hr * 0.5f), headY + (hr * 1.25f));
            body.LineTo(x + (hr * 0.45f), headY + (hr * 0.6f));
            body.Close();
            Add(new SKPath(body));
        }

        if (kind == 1)
        {
            Add(Oval(new SKRect(x - (hr * 1.05f), headY - (hr * 1.2f), x + (hr * 1.05f), headY + (hr * 0.1f))));
            Add(Circle(x, headY - (hr * 1.35f), hr * 0.38f));
        }

        if (kind == 5)
        {
            // A flat cap with its peak.
            Add(Oval(new SKRect(x - (hr * 1.1f), headY - (hr * 1.1f), x + (hr * 1.1f), headY - (hr * 0.1f))));
            Add(Oval(new SKRect(x - (hr * 1.35f), headY - (hr * 0.45f), x + (hr * 1.35f), headY - (hr * 0.05f))));
        }

        SKPoint? hand = null;
        if (kind == 3)
        {
            // An arm raised towards the sky, holding a sparkler.
            var sx = x + (towardsFire * hr * 1.5f);
            var sy = headY + (hr * 1.8f);
            var hx = x + (towardsFire * hr * 2.6f);
            var hy = headY - (hr * 2.4f);
            // Upper arm out to the elbow, forearm up to the hand: thick, rounded strokes turned into shapes.
            var ex = x + (towardsFire * hr * 2.9f);
            var ey = headY + (hr * 0.2f);
            using (var limb = new SKPath())
            using (var pen = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round, StrokeWidth = hr * 0.75f })
            {
                limb.MoveTo(sx, sy);
                limb.LineTo(ex, ey);
                limb.LineTo(hx, hy);
                var outline = new SKPath();
                pen.GetFillPath(limb, outline);
                Add(outline);
            }

            Add(Circle(hx, hy, hr * 0.32f));
            hand = new SKPoint(hx, hy);
        }


        // Rim light: a blurred warm copy, then the dark figure nudged away from the fire.
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, hr * 0.06f))
        using (var rim = new SKPaint { IsAntialias = true, Color = Lighten(fire, 0.35f).WithAlpha(240), MaskFilter = blur })
        {
            c.Save();
            c.Translate(towardsFire * hr * 0.08f, 0);
            c.DrawPath(merged, rim);
            c.Restore();
        }

        using (var dark = new SKPaint { IsAntialias = true, Color = new SKColor(0x07, 0x04, 0x04) })
        {
            c.Save();
            c.Translate(-towardsFire * hr * 0.06f, hr * 0.03f);
            c.DrawPath(merged, dark);
            c.Restore();
        }

        if (hand is { } at)
        {
            FestSparkler(c, at.X + (towardsFire * hr * 0.6f), at.Y - (hr * 1.6f), hr, rng);
            using var stick = new SKPaint { IsAntialias = true, StrokeWidth = hr * 0.1f, Color = new SKColor(0x80, 0x70, 0x60) };
            c.DrawLine(at.X, at.Y, at.X + (towardsFire * hr * 0.6f), at.Y - (hr * 1.6f), stick);
        }

        merged.Dispose();
    }

    /// <summary>A lit sparkler: a white-hot point throwing out fine, kinked sparks.</summary>
    private static void FestSparkler(SKCanvas c, float x, float y, float size, Random rng)
    {
        FestGlow(c, x, y, size * 3.5f, new SKColor(0xFF, 0xD8, 0x90), 200);
        using var p = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = size * 0.06f, BlendMode = SKBlendMode.Screen };
        for (var i = 0; i < 26; i++)
        {
            var a = rng.NextDouble() * Math.PI * 2;
            var len = size * R(rng, 0.8f, 2.4f);
            float ex = x + (float)(Math.Cos(a) * len), ey = y + (float)(Math.Sin(a) * len);
            p.Color = new SKColor(0xFF, 0xF0, 0xC0, (byte)rng.Next(150, 255));
            c.DrawLine(x, y, ex, ey, p);
            c.DrawLine(ex, ey, ex + (float)(Math.Cos(a + 0.7) * len * 0.3f), ey + (float)(Math.Sin(a + 0.7) * len * 0.3f), p);
        }

        FestGlow(c, x, y, size * 0.8f, SKColors.White, 255);
    }

    // ====================================================================================================
    // Lunar New Year, week 1: red lanterns over temple rooftops, plum blossom and gold
    // ====================================================================================================

    private static void FestRedLanterns(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var gold = new SKColor(0xFF, 0xC8, 0x50);

        // Rich red, darkest at the edges.
        // One smooth gradient: two bands met in a visible seam across the title.
        using (var sky = SKShader.CreateLinearGradient(
            new SKPoint(0, 0),
            new SKPoint(0, h),
            new[] { new SKColor(0x4A, 0x02, 0x08, 200), new SKColor(0x3A, 0x02, 0x06, 120), new SKColor(0x2A, 0x01, 0x05, 220) },
            new[] { 0f, 0.5f, 1f },
            SKShaderTileMode.Clamp))
        using (var skyPaint = new SKPaint { Shader = sky })
        {
            c.DrawRect(0, 0, w, h, skyPaint);
        }
        FestGlow(c, w * 0.5f, h * 0.15f, s * 0.8f, new SKColor(0xFF, 0x50, 0x20), 60);

        // Far lights out of focus.
        FestBokeh(c, w, h, 0, h * 0.85f, s, rng, 26, new[] { gold, new SKColor(0xFF, 0x60, 0x30), new SKColor(0xFF, 0x90, 0x40) }, 0.02f, 0.05f);

        // Distant lanterns: small, soft and dim, on strings across the top.
        var far = w > h ? 9 : 7;
        for (var i = 0; i < far; i++)
        {
            var lx = (w / (float)far * (i + 0.5f)) + R(rng, -s * 0.03f, s * 0.03f);
            var ly = h * R(rng, 0.11f, 0.22f);
            FestLantern(c, lx, ly, s * R(rng, 0.03f, 0.04f), 0, s, blur: s * 0.004f, alpha: 170);
        }

        // Near lanterns, big and sharp, hung from the top edge at the sides.
        var near = w > h
            ? new[] { (0.06f, 0.16f, 0.11f), (0.2f, 0.1f, 0.075f), (0.8f, 0.1f, 0.075f), (0.94f, 0.16f, 0.11f) }
            : new[] { (0.09f, 0.15f, 0.105f), (0.28f, 0.09f, 0.065f), (0.72f, 0.09f, 0.065f), (0.91f, 0.15f, 0.105f) };
        foreach (var (nx, ny, nr) in near)
        {
            FestLantern(c, w * nx, h * ny, s * nr, 0, s);
        }

        // A middle row of mid-sized lanterns between them.
        var mid = w > h ? new[] { 0.36f, 0.5f, 0.64f } : new[] { 0.5f };
        foreach (var mx in mid)
        {
            FestLantern(c, w * mx, h * 0.12f, s * 0.05f, 0, s);
        }

        // Temple rooftops along the bottom with lanterns under their eaves.
        FestRooftops(c, w, h, h * 0.8f, s, rng, new SKColor(0x2E, 0x06, 0x0C), 0.6f, glowWindows: true, gold: gold, lanterns: false);
        FestRooftops(c, w, h, h * 0.87f, s, rng, new SKColor(0x10, 0x02, 0x05), 1f, glowWindows: true, gold: gold, lanterns: true);

        // Plum blossom reaching in from the bottom-left corner.
        FestPlumBranch(c, -s * 0.02f, h * 1.01f, -38, s * 0.24f, s * 0.03f, s, rng, h * 0.7f);
        if (w > h)
        {
            FestPlumBranch(c, w + (s * 0.02f), h * 0.98f, -145, s * 0.18f, s * 0.024f, s, rng, h * 0.72f);
        }

        FestDust(c, w, h, 0, h, s, rng, 60, gold);
        for (var i = 0; i < 4; i++)
        {
            var x = R(rng, w * 0.25f, w * 0.75f);
            var y = h * R(rng, 0.24f, 0.31f);
            FestTwinkle(c, x, y, s * R(rng, 0.012f, 0.02f), gold, 100);
        }
    }

    /// <summary>
    /// A glowing red paper lantern on a cord from the top edge: bloom, lit paper with ribs, gold caps and a tassel.
    /// (x, cy) is its middle and r its half-width; blur/alpha push it back into the distance.
    /// </summary>
    private static void FestLantern(SKCanvas c, float x, float cy, float r, float top, float s, float blur = 0, byte alpha = 255)
    {
        if (blur > 0 || alpha < 255)
        {
            using var layer = new SKPaint { Color = SKColors.White.WithAlpha(alpha), ImageFilter = blur > 0 ? SKImageFilter.CreateBlur(blur, blur) : null };
            c.SaveLayer(layer);
        }

        var bh = r * 0.82f;
        using (var cord = new SKPaint { IsAntialias = true, StrokeWidth = Math.Max(1.5f, r * 0.04f), Color = new SKColor(0xC0, 0x8A, 0x30) })
        {
            c.DrawLine(x, top, x, cy - bh, cord);
        }

        FestGlow(c, x, cy, r * 3.2f, new SKColor(0xFF, 0x58, 0x20), 130);

        var body = new SKRect(x - r, cy - bh, x + r, cy + bh);
        using var shape = new SKPath();
        shape.AddOval(body);
        using (var paper = SKShader.CreateRadialGradient(new SKPoint(x, cy + (bh * 0.1f)), r * 1.15f, new[] { new SKColor(0xFF, 0xD0, 0x70), new SKColor(0xFF, 0x60, 0x28), new SKColor(0xD8, 0x18, 0x18), new SKColor(0x78, 0x04, 0x08) }, new[] { 0f, 0.3f, 0.7f, 1f }, SKShaderTileMode.Clamp))
        using (var p = new SKPaint { IsAntialias = true, Shader = paper })
        {
            c.DrawPath(shape, p);
        }

        c.Save();
        c.ClipPath(shape, antialias: true);
        using (var rib = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = r * 0.035f, Color = new SKColor(0x7A, 0x06, 0x08, 120) })
        {
            foreach (var f in new[] { 0.3f, 0.62f, 0.88f })
            {
                c.DrawOval(new SKRect(x - (r * f), body.Top, x + (r * f), body.Bottom), rib);
            }

            c.DrawLine(x, body.Top, x, body.Bottom, rib);
        }

        // A soft sheen on the upper left.
        using (var blurSheen = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.12f))
        using (var sheen = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(60), MaskFilter = blurSheen })
        {
            c.DrawOval(new SKRect(x - (r * 0.75f), cy - (bh * 0.8f), x - (r * 0.2f), cy - (bh * 0.1f)), sheen);
        }

        c.Restore();

        using var goldShade = SKShader.CreateLinearGradient(new SKPoint(x - (r * 0.5f), 0), new SKPoint(x + (r * 0.5f), 0), new[] { new SKColor(0xFF, 0xE8, 0x9A), new SKColor(0xE0, 0xA0, 0x30), new SKColor(0x8A, 0x58, 0x10) }, SKShaderTileMode.Clamp);
        using var goldPaint = new SKPaint { IsAntialias = true, Shader = goldShade };
        c.DrawRoundRect(new SKRect(x - (r * 0.5f), body.Top - (r * 0.14f), x + (r * 0.5f), body.Top + (r * 0.08f)), r * 0.05f, r * 0.05f, goldPaint);
        c.DrawRoundRect(new SKRect(x - (r * 0.5f), body.Bottom - (r * 0.08f), x + (r * 0.5f), body.Bottom + (r * 0.14f)), r * 0.05f, r * 0.05f, goldPaint);

        // Tassel: a gold knot and bead, then a fall of red threads with a gold band.
        var knotY = body.Bottom + (r * 0.28f);
        using (var thread = new SKPaint { IsAntialias = true, StrokeWidth = r * 0.04f, Color = new SKColor(0xD8, 0x9A, 0x30) })
        {
            c.DrawLine(x, body.Bottom + (r * 0.12f), x, knotY, thread);
        }

        c.DrawCircle(x, knotY, r * 0.1f, goldPaint);
        using var tassel = new SKPath();
        tassel.MoveTo(x - (r * 0.1f), knotY + (r * 0.08f));
        tassel.LineTo(x + (r * 0.1f), knotY + (r * 0.08f));
        tassel.LineTo(x + (r * 0.2f), knotY + (r * 0.85f));
        tassel.LineTo(x - (r * 0.2f), knotY + (r * 0.85f));
        tassel.Close();
        using (var red = SKShader.CreateLinearGradient(new SKPoint(x - (r * 0.2f), 0), new SKPoint(x + (r * 0.2f), 0), new[] { new SKColor(0xE8, 0x30, 0x28), new SKColor(0x90, 0x08, 0x0C) }, SKShaderTileMode.Clamp))
        using (var p = new SKPaint { IsAntialias = true, Shader = red })
        {
            c.DrawPath(tassel, p);
        }

        c.DrawRect(x - (r * 0.13f), knotY + (r * 0.16f), r * 0.26f, r * 0.08f, goldPaint);

        if (blur > 0 || alpha < 255)
        {
            c.Restore();
        }
    }

    /// <summary>
    /// A row of buildings with sweeping, upturned temple roofs, their tops at around <paramref name="line"/>,
    /// running down off the bottom. Roof edges are picked out in gold; some windows are lit.
    /// </summary>
    private static void FestRooftops(SKCanvas c, int w, int h, float line, float s, Random rng, SKColor colour, float scale, bool glowWindows, SKColor gold, bool lanterns)
    {
        var x = -s * R(rng, 0.02f, 0.12f);
        using var wall = new SKPaint { IsAntialias = true, Color = colour };
        using var edge = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.003f, Color = gold.WithAlpha((byte)(scale > 0.8f ? 170 : 90)) };
        using var window = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xB8, 0x50, (byte)(scale > 0.8f ? 230 : 140)) };
        while (x < w)
        {
            var bw = s * R(rng, 0.2f, 0.3f) * scale * 1.3f;
            var roofY = line + (s * R(rng, -0.03f, 0.03f) * scale);
            var rh = s * 0.05f * scale;
            var ov = bw * 0.14f;
            float x0 = x, x1 = x + bw;

            c.DrawRect(x0, roofY, bw, h - roofY + 2, wall);

            if (glowWindows)
            {
                // A lit lattice window or door in the middle of each building.
                var ww = bw * 0.16f;
                var wy = roofY + (rh * 0.6f);
                var rect = new SKRect(((x0 + x1) / 2) - (ww / 2), wy, ((x0 + x1) / 2) + (ww / 2), wy + (ww * 1.1f));
                FestGlow(c, rect.MidX, rect.MidY, ww * 1.6f, new SKColor(0xFF, 0x90, 0x30), (byte)(scale > 0.8f ? 120 : 60));
                c.DrawRect(rect, window);
                using var lattice = new SKPaint { IsAntialias = true, StrokeWidth = Math.Max(1, ww * 0.05f), Color = colour };
                c.DrawLine(rect.MidX, rect.Top, rect.MidX, rect.Bottom, lattice);
                c.DrawLine(rect.Left, rect.MidY, rect.Right, rect.MidY, lattice);
            }

            using var roof = FestRoofPath(x0, x1, roofY, rh, ov);
            c.DrawPath(roof, wall);
            c.DrawPath(roof, edge);

            if (lanterns)
            {
                foreach (var lx in new[] { x0 + (bw * 0.12f), x1 - (bw * 0.12f) })
                {
                    FestLantern(c, lx, roofY + (rh * 0.9f), s * 0.022f, roofY, s);
                }
            }

            x = x1 + (s * R(rng, 0.0f, 0.04f));
        }
    }

    /// <summary>A hip roof whose eaves sweep up at both ends; (x0..x1, eave) is the wall top it covers.</summary>
    private static SKPath FestRoofPath(float x0, float x1, float eave, float rh, float ov)
    {
        var bw = x1 - x0;
        var p = new SKPath();
        var tipL = new SKPoint(x0 - ov, eave - (rh * 0.55f));
        var tipR = new SKPoint(x1 + ov, eave - (rh * 0.55f));
        p.MoveTo(tipL);
        p.QuadTo(x0 - (ov * 0.2f), eave + (rh * 0.15f), x0 + (ov * 0.6f), eave + (rh * 0.08f));
        p.LineTo(x1 - (ov * 0.6f), eave + (rh * 0.08f));
        p.QuadTo(x1 + (ov * 0.2f), eave + (rh * 0.15f), tipR.X, tipR.Y);
        p.QuadTo(x1 - (bw * 0.05f), eave - (rh * 0.3f), x1 - (bw * 0.2f), eave - rh);
        p.LineTo(x1 - (bw * 0.18f), eave - (rh * 1.25f));
        p.LineTo(x0 + (bw * 0.18f), eave - (rh * 1.25f));
        p.LineTo(x0 + (bw * 0.2f), eave - rh);
        p.QuadTo(x0 + (bw * 0.05f), eave - (rh * 0.3f), tipL.X, tipL.Y);
        p.Close();
        return p;
    }

    /// <summary>A gnarled plum branch growing from (x, y) at <paramref name="angle"/> degrees, with blossoms and buds.</summary>
    private static void FestPlumBranch(SKCanvas c, float x, float y, float angle, float len, float width, float s, Random rng, float minY)
    {
        var blossoms = new List<(SKPoint P, float R)>();
        using var bark = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, Style = SKPaintStyle.Stroke, Color = new SKColor(0x22, 0x10, 0x0C) };

        void Grow(SKPoint from, float a, float l, float wd, int depth)
        {
            var rad = a * Math.PI / 180;
            if (from.Y + (Math.Sin(rad) * l) < minY)
            {
                // Don't grow up into the title: shorten the twig to stop at minY, or end it here with a bud.
                var room = from.Y - minY;
                if (Math.Sin(rad) >= -0.05 || room < s * 0.03f)
                {
                    blossoms.Add((from, -s * 0.016f));
                    return;
                }

                l = (float)(room / -Math.Sin(rad));
            }

            var bend = R(rng, -18, 18) * Math.PI / 180;
            var mid = new SKPoint(from.X + (float)(Math.Cos(rad + bend) * l * 0.5f), from.Y + (float)(Math.Sin(rad + bend) * l * 0.5f));
            var end = new SKPoint(from.X + (float)(Math.Cos(rad) * l), from.Y + (float)(Math.Sin(rad) * l));
            using var path = new SKPath();
            path.MoveTo(from);
            path.QuadTo(mid, end);
            bark.StrokeWidth = wd;
            c.DrawPath(path, bark);

            // Blossoms along the twig and at its end.
            if (depth >= 1)
            {
                for (var i = 0; i < 2; i++)
                {
                    var t = R(rng, 0.3f, 0.9f);
                    var bp = new SKPoint(from.X + ((end.X - from.X) * t) + R(rng, -wd, wd), from.Y + ((end.Y - from.Y) * t) + R(rng, -wd, wd));
                    blossoms.Add((bp, s * R(rng, 0.016f, 0.026f)));
                }
            }

            if (depth >= 3 || l < s * 0.03f)
            {
                blossoms.Add((end, s * R(rng, 0.012f, 0.02f) * -1)); // a bud (negative radius)
                return;
            }

            Grow(end, a + R(rng, -35, -15), l * R(rng, 0.6f, 0.8f), wd * 0.65f, depth + 1);
            Grow(end, a + R(rng, 15, 40), l * R(rng, 0.5f, 0.7f), wd * 0.6f, depth + 1);
        }

        Grow(new SKPoint(x, y), angle, len, width, 0);

        var petals = new[] { new SKColor(0xFF, 0xE6, 0xEE), new SKColor(0xFF, 0xB0, 0xC8), new SKColor(0xF0, 0x6C, 0x96) };
        foreach (var (p, r) in blossoms)
        {
            if (r < 0)
            {
                using var bud = new SKPaint { IsAntialias = true, Color = new SKColor(0xD8, 0x3A, 0x6A) };
                c.DrawCircle(p, -r * 0.4f, bud);
                continue;
            }

            FestPlumBlossom(c, p.X, p.Y, r, petals[rng.Next(petals.Length)], R(rng, 0, 72));
        }
    }

    /// <summary>A five-petalled plum blossom with a dark heart and gold stamens.</summary>
    private static void FestPlumBlossom(SKCanvas c, float x, float y, float r, SKColor col, float rotation)
    {
        FestGlow(c, x, y, r * 2, col, 60);
        using var petal = new SKPaint { IsAntialias = true };
        for (var i = 0; i < 5; i++)
        {
            var a = (rotation + (i * 72)) * Math.PI / 180;
            float px = x + (float)(Math.Cos(a) * r * 0.52f), py = y + (float)(Math.Sin(a) * r * 0.52f);
            using var shade = SKShader.CreateRadialGradient(new SKPoint(x, y), r * 1.1f, new[] { Darken(col, 0.75f), col, Lighten(col, 0.4f) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp);
            petal.Shader = shade;
            c.DrawCircle(px, py, r * 0.5f, petal);
            petal.Shader = null;
        }

        using var heart = new SKPaint { IsAntialias = true, Color = new SKColor(0xA0, 0x18, 0x40) };
        c.DrawCircle(x, y, r * 0.2f, heart);
        using var stamen = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xD8, 0x60) };
        for (var i = 0; i < 6; i++)
        {
            var a = (rotation + (i * 60) + 30) * Math.PI / 180;
            c.DrawCircle(x + (float)(Math.Cos(a) * r * 0.32f), y + (float)(Math.Sin(a) * r * 0.32f), r * 0.06f, stamen);
        }
    }

    // ====================================================================================================
    // Lunar New Year, week 2: sky lanterns rising from a riverside town under the full moon
    // ====================================================================================================

    private static void FestLanternFestival(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var warm = new SKColor(0xFF, 0xA0, 0x40);

        // One smooth sky, night blue warming to plum behind the town (two bands left a hard seam across the title).
        using (var sky = SKShader.CreateLinearGradient(
            new SKPoint(0, 0),
            new SKPoint(0, h),
            new[] { new SKColor(0x08, 0x08, 0x28, 215), new SKColor(0x10, 0x0A, 0x2C, 165), new SKColor(0x2C, 0x0C, 0x26, 160), new SKColor(0x3A, 0x0C, 0x24, 210) },
            new[] { 0f, 0.4f, 0.7f, 1f },
            SKShaderTileMode.Clamp))
        using (var skyPaint = new SKPaint { Shader = sky })
        {
            c.DrawRect(0, 0, w, h, skyPaint);
        }
        FestGlow(c, w * 0.5f, h * 0.95f, s * 0.9f, new SKColor(0xFF, 0x70, 0x30), 90);
        Stars(c, w, h * 0.4f, 70, s, rng);

        var moonX = w > h ? w * 0.84f : w * 0.8f;
        var moonY = h * 0.15f;
        FestGlow(c, moonX, moonY, s * 0.3f, new SKColor(0xFF, 0xE8, 0xC0), 90);
        Moon(c, moonX, moonY, s * 0.075f, rng);

        // Sky lanterns: far ones small and soft, near ones big and bright, drifting up from the town.
        var placed = new List<(float X, float Y, float R)> { (moonX, moonY, s * 0.11f) };
        var layers = new[] { (Count: w > h ? 30 : 22, Min: 0.008f, Max: 0.014f, Alpha: (byte)150), (Count: w > h ? 12 : 9, Min: 0.018f, Max: 0.028f, Alpha: (byte)220), (Count: 5, Min: 0.034f, Max: 0.046f, Alpha: (byte)255) };
        foreach (var (count, min, max, alpha) in layers)
        {
            for (var i = 0; i < count; i++)
            {
                var r = s * R(rng, min, max);
                if (FindClear(rng, placed, r * 1.6f, r * 1.5f, w - (r * 1.5f), r * 2, h * 0.8f, 1.05f) is not { } at)
                {
                    continue;
                }

                // Keep the title band clear, apart from a few small ones at its far edges.
                if (FestInTitle(at.X, at.Y, w, h, r * 1.5f) && (r > s * 0.016f || Math.Abs(at.X - (w / 2f)) < w * 0.38f))
                {
                    continue;
                }

                FestSkyLantern(c, at.X, at.Y, r, alpha);
                placed.Add((at.X, at.Y, r * 1.6f));
            }
        }

        // The far shore: a pagoda among rooftops, then the near rooftops on the river bank.
        var shore = h * 0.86f;
        using (var hazeBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.03f))
        using (var haze = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0x80, 0x50, 60), MaskFilter = hazeBlur })
        {
            c.DrawOval(new SKRect(-w * 0.1f, shore - (s * 0.12f), w * 1.1f, shore + (s * 0.04f)), haze);
        }

        var pagodaX = w > h ? w * 0.2f : w * 0.22f;
        FestPagoda(c, pagodaX, shore, Math.Min(s * 0.3f, shore - (h * (w > h ? 0.69f : 0.65f))), new SKColor(0x1C, 0x0A, 0x22), warm);
        FestRooftops(c, w, h, shore - (s * 0.04f), s, rng, new SKColor(0x1A, 0x08, 0x1E), 0.75f, glowWindows: true, gold: warm, lanterns: true);

        // River with the town's lights and lanterns reflected.
        using (var water = SKShader.CreateLinearGradient(new SKPoint(0, shore), new SKPoint(0, h), new[] { new SKColor(0x18, 0x08, 0x20, 245), new SKColor(0x06, 0x03, 0x0C, 250) }, SKShaderTileMode.Clamp))
        using (var p = new SKPaint { Shader = water })
        {
            c.DrawRect(0, shore, w, h - shore, p);
        }

        // Long soft reflections of the town's lights, broken by ripples.
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.006f))
        using (var column = new SKPaint { IsAntialias = true, MaskFilter = blur, BlendMode = SKBlendMode.Screen })
        {
            for (var i = 0; i < (w > h ? 16 : 12); i++)
            {
                var x = R(rng, 0, w);
                column.Color = FestMix(new SKColor(0xFF, 0xB0, 0x50), new SKColor(0xFF, 0x40, 0x30), (float)rng.NextDouble()).WithAlpha((byte)rng.Next(50, 100));
                var cw = s * R(rng, 0.008f, 0.016f);
                c.DrawOval(new SKRect(x - cw, shore, x + cw, shore + ((h - shore) * R(rng, 0.5f, 0.9f))), column);
            }
        }

        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.0015f))
        using (var shimmer = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, BlendMode = SKBlendMode.Screen, MaskFilter = blur })
        {
            for (var i = 0; i < 26; i++)
            {
                var x = R(rng, 0, w);
                var y = R(rng, shore + (s * 0.008f), h);
                var near = (y - shore) / (h - shore);
                shimmer.StrokeWidth = s * (0.002f + (near * 0.003f));
                shimmer.Color = FestMix(new SKColor(0xFF, 0xC0, 0x60), new SKColor(0xFF, 0x50, 0x30), (float)rng.NextDouble()).WithAlpha((byte)rng.Next(70, 170));
                var len = s * R(rng, 0.01f, 0.04f) * (1 + near);
                c.DrawLine(x, y, x + len, y, shimmer);
            }
        }

        // A few lanterns floating on the water, and a pair rising close to the camera.
        foreach (var fx in w > h ? new[] { 0.42f, 0.55f, 0.66f, 0.9f } : new[] { 0.45f, 0.6f, 0.92f })
        {
            FestWaterLantern(c, w * fx, h * R(rng, 0.92f, 0.97f), s * 0.018f);
        }

        FestSkyLantern(c, w * 0.06f, h * 0.72f, s * 0.055f, 255);
        FestSkyLantern(c, w * 0.94f, h * 0.68f, s * 0.045f, 255);
    }

    /// <summary>A paper sky lantern, wider at the top, glowing from the flame at its open base; (x, y) is its middle.</summary>
    private static void FestSkyLantern(SKCanvas c, float x, float y, float r, byte alpha)
    {
        FestGlow(c, x, y + (r * 0.2f), r * 3.2f, new SKColor(0xFF, 0x90, 0x30), (byte)(alpha * 0.55f));
        using var body = new SKPath();
        body.MoveTo(x - (r * 0.82f), y - (r * 1.1f));
        body.QuadTo(x, y - (r * 1.3f), x + (r * 0.82f), y - (r * 1.1f));
        body.QuadTo(x + (r * 0.78f), y + (r * 0.3f), x + (r * 0.55f), y + (r * 1.0f));
        body.QuadTo(x, y + (r * 1.1f), x - (r * 0.55f), y + (r * 1.0f));
        body.QuadTo(x - (r * 0.78f), y + (r * 0.3f), x - (r * 0.82f), y - (r * 1.1f));
        body.Close();
        using var paper = SKShader.CreateRadialGradient(new SKPoint(x, y + (r * 0.9f)), r * 2.2f, new[] { new SKColor(0xFF, 0xF4, 0xC0, alpha), new SKColor(0xFF, 0xB0, 0x40, alpha), new SKColor(0xE0, 0x50, 0x18, alpha) }, new[] { 0f, 0.45f, 1f }, SKShaderTileMode.Clamp);
        using var p = new SKPaint { IsAntialias = true, Shader = paper };
        c.DrawPath(body, p);

        // Faint vertical seams.
        using var seam = new SKPaint { IsAntialias = true, StrokeWidth = Math.Max(1, r * 0.04f), Color = new SKColor(0xB0, 0x40, 0x10, (byte)(alpha * 0.35f)) };
        c.DrawLine(x - (r * 0.3f), y - (r * 1.12f), x - (r * 0.22f), y + (r * 1.02f), seam);
        c.DrawLine(x + (r * 0.3f), y - (r * 1.12f), x + (r * 0.22f), y + (r * 1.02f), seam);
        FestGlow(c, x, y + (r * 0.95f), r * 0.5f, SKColors.White, alpha);
    }

    /// <summary>A small lotus-style lantern floating on the river with its glow on the water.</summary>
    private static void FestWaterLantern(SKCanvas c, float x, float y, float r)
    {
        FestGlow(c, x, y, r * 4, new SKColor(0xFF, 0x90, 0x30), 140);
        using var reflection = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xB0, 0x50, 90), BlendMode = SKBlendMode.Screen };
        c.DrawOval(new SKRect(x - (r * 0.5f), y + (r * 0.4f), x + (r * 0.5f), y + (r * 2.6f)), reflection);
        using var petals = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0x80, 0x90) };
        using var path = new SKPath();
        path.MoveTo(x - (r * 1.2f), y);
        path.QuadTo(x - (r * 0.9f), y - (r * 1.1f), x - (r * 0.4f), y - (r * 0.3f));
        path.QuadTo(x, y - (r * 1.4f), x + (r * 0.4f), y - (r * 0.3f));
        path.QuadTo(x + (r * 0.9f), y - (r * 1.1f), x + (r * 1.2f), y);
        path.QuadTo(x, y + (r * 0.5f), x - (r * 1.2f), y);
        path.Close();
        c.DrawPath(path, petals);
        FestGlow(c, x, y - (r * 0.4f), r * 0.6f, new SKColor(0xFF, 0xF0, 0xB0), 255);
    }

    /// <summary>A pagoda with upturned eaves and lit windows on every storey; (x, ground) is the middle of its base.</summary>
    private static void FestPagoda(SKCanvas c, float x, float ground, float height, SKColor colour, SKColor light)
    {
        using var wall = new SKPaint { IsAntialias = true, Color = colour };
        using var edge = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = height * 0.006f, Color = light.WithAlpha(110) };
        using var lit = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xC0, 0x60) };
        const int Storeys = 5;
        var storey = height * 0.13f;
        var y = ground;
        for (var i = 0; i < Storeys; i++)
        {
            var half = height * (0.15f - (i * 0.02f));
            c.DrawRect(x - half, y - storey, half * 2, storey, wall);
            var win = new SKRect(x - (half * 0.3f), y - (storey * 0.75f), x + (half * 0.3f), y - (storey * 0.2f));
            FestGlow(c, win.MidX, win.MidY, half * 0.9f, new SKColor(0xFF, 0x90, 0x30), 110);
            c.DrawRect(win, lit);
            using var roof = FestRoofPath(x - half, x + half, y - storey, storey * 0.45f, half * 0.45f);
            c.DrawPath(roof, wall);
            c.DrawPath(roof, edge);
            y -= storey + (storey * 0.45f);
        }

        using var spire = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = height * 0.012f, Color = colour };
        c.DrawLine(x, y + (storey * 0.3f), x, y - (storey * 0.6f), spire);
    }
}
