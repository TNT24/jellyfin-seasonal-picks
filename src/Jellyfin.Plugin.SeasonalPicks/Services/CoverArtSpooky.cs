using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>
/// The illustrated night scenes: Halloween's four (pumpkin patch, graveyard, haunted house, witch's cauldron),
/// the haunted city for Ghostbusters Day and the castle over the lake for Back to Hogwarts.
/// Each is built like a poster: a translucent sky so the picks still show through, layers of scenery that get darker
/// and sharper towards the viewer, glows for the light sources, and the weight along the bottom and the corners so
/// the title in the middle stays clear.
/// </summary>
internal static partial class CoverArt
{
    private static readonly SKColor SpookyInk = new(0x0B, 0x06, 0x10);

    // ---------- Pumpkin patch ----------

    private static void SpookyPumpkinPatch(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var wide = w > h;

        // Dusk sky: deep violet overhead, a smouldering orange haze along the horizon.
        SpookyBands(c, new SKRect(0, 0, w, h), new[]
        {
            new SKColor(0x0A, 0x05, 0x1C, 230), new SKColor(0x1E, 0x0C, 0x34, 170), new SKColor(0x5A, 0x1C, 0x2A, 120),
            new SKColor(0xB0, 0x48, 0x18, 150), new SKColor(0x18, 0x08, 0x10, 240)
        }, new[] { 0f, 0.3f, 0.55f, 0.7f, 0.85f });
        Stars(c, w, h * 0.33f, wide ? 80 : 60, s, rng);

        var moon = new SKPoint(w * (wide ? 0.83f : 0.78f), h * 0.17f);
        var mr = s * 0.095f;
        SpookyGlow(c, moon.X, moon.Y, mr * 5f, new SKColor(0xFF, 0xC0, 0x70, 85));
        Moon(c, moon.X, moon.Y, mr, rng);
        SpookyCloudWisps(c, w, moon.Y + (mr * 0.3f), s, new SKColor(0x1A, 0x0C, 0x26, 150), rng, 3);

        // Bats streaming away from the moon, smaller and paler the further off they are.
        for (var i = 0; i < (wide ? 6 : 5); i++)
        {
            var t = i / 5f;
            var bx = moon.X - (w * (0.08f + (t * 0.42f))) + R(rng, -s * 0.03f, s * 0.03f);
            var by = moon.Y + (h * (0.06f - (t * 0.04f))) + R(rng, -s * 0.04f, s * 0.04f);
            var size = s * (0.05f - (t * 0.028f)) * R(rng, 0.85f, 1.1f);
            SpookyBat(c, bx, by, size, R(rng, -15, 15), R(rng, -0.6f, 1f), SpookyMix(new SKColor(0x10, 0x06, 0x14), new SKColor(0x4A, 0x2A, 0x50), t * 0.7f));
        }

        // Far hills with a farmhouse and a line of trees, softened by the haze.
        var farTop = h * 0.71f;
        using (var far = SpookyRidge(w, h, farTop, h * 0.035f, 5, true, rng))
        {
            SpookyFillV(c, far, new SKColor(0x3A, 0x1A, 0x34, 240), new SKColor(0x22, 0x0E, 0x20, 250));
        }

        var farm = new SKColor(0x2E, 0x14, 0x2C);
        SpookyFarmhouse(c, w * (wide ? 0.66f : 0.62f), farTop + (h * 0.005f), s * 0.07f, farm);
        for (var i = 0; i < 7; i++)
        {
            var tx = w * (wide ? 0.28f : 0.2f) + (i * s * 0.035f) + R(rng, -s * 0.01f, s * 0.01f);
            SpookyRoundTree(c, tx, farTop + (h * 0.012f), s * R(rng, 0.03f, 0.05f), farm);
        }

        SpookyFog(c, w, farTop + (h * 0.03f), s * 0.03f, new SKColor(0xE8, 0x9A, 0x6A, 55), s, rng);

        // Near field: a crooked fence on the rise, then the dark patch itself.
        var near = h * 0.82f;
        using (var field = SpookyRidge(w, h, near, h * 0.025f, 3, true, rng))
        {
            SpookyFillV(c, field, new SKColor(0x1C, 0x0C, 0x18), new SKColor(0x08, 0x03, 0x08));
        }

        SpookyFence(c, w * (wide ? 0.5f : 0.36f), w * (wide ? 0.95f : 1.02f), near + (h * 0.005f), s * 0.07f, new SKColor(0x14, 0x08, 0x12), rng);

        // A gnarled tree leaning in from the left edge, its crown hooking over the top-left corner.
        SpookyTree(c, w * 0.04f, h * 0.92f, s * (wide ? 0.88f : 0.9f), 12f, SpookyInk, SpookyTitleZone(w, h), 0.48f, rng);

        // Pumpkins: a few dim ones further back, the carved lanterns at the front.
        var back = new (float X, float R, float Squash)[] { (0.43f, 0.045f, 0.8f), (0.6f, 0.04f, 0.75f), (0.88f, 0.05f, 0.85f) };
        foreach (var (px, pr, sq) in back)
        {
            SpookyPumpkin(c, w * px, near + (h * 0.04f), s * pr, sq, -1, 0.55f, rng);
        }

        SpookyVine(c, w * 0.15f, h * 0.95f, w * 0.85f, h * 0.93f, s, rng);

        var front = wide
            ? new (float X, float Bottom, float R, float Squash, int Face)[]
            {
                (0.27f, 0.97f, 0.135f, 0.82f, 0), (0.5f, 0.92f, 0.07f, 0.95f, 2), (0.74f, 0.96f, 0.11f, 0.78f, 1),
                (0.92f, 0.99f, 0.075f, 0.7f, -1), (0.07f, 1.0f, 0.08f, 0.85f, -1)
            }
            : new (float X, float Bottom, float R, float Squash, int Face)[]
            {
                (0.3f, 0.97f, 0.15f, 0.82f, 0), (0.56f, 0.92f, 0.07f, 0.95f, 2), (0.8f, 0.96f, 0.12f, 0.78f, 1),
                (0.06f, 1.0f, 0.08f, 0.85f, -1)
            };
        foreach (var (px, pb, pr, sq, face) in front)
        {
            SpookyPumpkin(c, w * px, h * pb, s * pr, sq, face, 1f, rng);
        }

        SpookyGrass(c, w, h * 1.0f, s * 0.035f, SpookyInk, rng);
    }

    // ---------- Graveyard ----------

    private static void SpookyGraveyard(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var wide = w > h;

        // Cold moonlit sky.
        SpookyBands(c, new SKRect(0, 0, w, h), new[]
        {
            new SKColor(0x05, 0x08, 0x1A, 235), new SKColor(0x10, 0x1A, 0x34, 180), new SKColor(0x2C, 0x44, 0x58, 140),
            new SKColor(0x50, 0x6A, 0x74, 160), new SKColor(0x0A, 0x0C, 0x14, 245)
        }, new[] { 0f, 0.3f, 0.55f, 0.7f, 0.86f });
        Stars(c, w, h * 0.35f, wide ? 90 : 70, s, rng);

        var moon = new SKPoint(w * (wide ? 0.2f : 0.24f), h * 0.18f);
        var mr = s * 0.1f;
        SpookyGlow(c, moon.X, moon.Y, mr * 5f, new SKColor(0xC8, 0xDC, 0xFF, 80));
        Moon(c, moon.X, moon.Y, mr, rng);
        SpookyCloudWisps(c, w, moon.Y + (mr * 0.5f), s, new SKColor(0x0A, 0x10, 0x22, 160), rng, 4);

        for (var i = 0; i < 3; i++)
        {
            var t = i / 2f;
            SpookyBat(c, moon.X + (w * (0.12f + (t * 0.18f))), moon.Y - (h * 0.04f) + (h * t * 0.05f), s * (0.04f - (t * 0.012f)), R(rng, -15, 15), R(rng, -0.6f, 1f), SpookyMix(new SKColor(0x08, 0x08, 0x14), new SKColor(0x30, 0x38, 0x50), t * 0.6f));
        }

        // A chapel on the far hill, its window lit.
        var farTop = h * 0.72f;
        using (var far = SpookyRidge(w, h, farTop, h * 0.025f, 4, true, rng))
        {
            SpookyFillV(c, far, new SKColor(0x2A, 0x34, 0x48, 245), new SKColor(0x18, 0x1E, 0x2C, 250));
        }

        SpookyChapel(c, w * (wide ? 0.66f : 0.64f), farTop + (h * 0.01f), s * 0.11f, new SKColor(0x22, 0x2A, 0x3C));
        SpookyFog(c, w, farTop + (h * 0.02f), s * 0.03f, new SKColor(0xB8, 0xC8, 0xD8, 70), s, rng);

        // A row of small stones and an iron railing further back.
        var mid = h * 0.8f;
        using (var ground = SpookyRidge(w, h, mid, h * 0.015f, 3, true, rng))
        {
            SpookyFillV(c, ground, new SKColor(0x1A, 0x20, 0x2A), new SKColor(0x06, 0x07, 0x0C));
        }

        SpookyRailing(c, w * (wide ? 0.3f : 0.36f), w * (wide ? 0.82f : 0.86f), mid + (h * 0.005f), s * 0.07f, new SKColor(0x10, 0x14, 0x1C));
        for (var i = 0; i < (wide ? 7 : 5); i++)
        {
            var gx = w * (0.08f + (i * (wide ? 0.13f : 0.19f))) + R(rng, -s * 0.02f, s * 0.02f);
            var gh = s * R(rng, 0.05f, 0.07f);
            SpookyGravestone(c, gx, mid + (h * 0.02f), gh * R(rng, 0.55f, 0.75f), gh, R(rng, -8, 8), rng.Next(5), 0.45f, rng);
        }

        SpookyFog(c, w, mid + (h * 0.03f), s * 0.025f, new SKColor(0xB8, 0xC8, 0xD8, 55), s, rng);

        // The old tree on the right, reaching over the top corner.
        SpookyTree(c, w * 0.95f, h * 0.93f, s * (wide ? 0.88f : 0.9f), -14f, new SKColor(0x06, 0x06, 0x0C), SpookyTitleZone(w, h), 0.48f, rng);

        // Big headstones in front, lit from the moon's side, with a lantern left burning at one.
        var front = wide
            ? new (float X, float Ground, float H, float Ratio, int Shape)[] { (0.1f, 0.97f, 0.2f, 0.62f, 2), (0.27f, 0.95f, 0.15f, 0.7f, 0), (0.62f, 0.96f, 0.13f, 0.75f, 3), (0.77f, 0.98f, 0.24f, 0.5f, 1), (0.44f, 0.93f, 0.1f, 0.7f, 4) }
            : new (float X, float Ground, float H, float Ratio, int Shape)[] { (0.13f, 0.97f, 0.22f, 0.62f, 2), (0.38f, 0.95f, 0.15f, 0.7f, 0), (0.62f, 0.94f, 0.11f, 0.75f, 3), (0.8f, 0.98f, 0.24f, 0.5f, 1) };
        foreach (var (gx, gg, gh, ratio, shape) in front)
        {
            SpookyGravestone(c, w * gx, h * gg, s * gh * ratio, s * gh, R(rng, -7, 7), shape, 1f, rng);
        }

        SpookyCandleLantern(c, w * (wide ? 0.34f : 0.5f), h * 0.965f, s * 0.05f);
        SpookyFog(c, w, h * 0.96f, s * 0.035f, new SKColor(0xC0, 0xCC, 0xDC, 60), s, rng);
        SpookyGrass(c, w, h, s * 0.03f, new SKColor(0x05, 0x06, 0x0A), rng);
    }

    /// <summary>
    /// A headstone on <paramref name="ground"/>: 0 rounded, 1 cross, 2 ringed cross, 3 pointed arch, 4 obelisk.
    /// Moonlight catches its upper-left edges; <paramref name="light"/> below 1 fades it into the distance.
    /// </summary>
    private static void SpookyGravestone(SKCanvas c, float x, float ground, float sw, float sh, float tilt, int shape, float light, Random rng)
    {
        c.Save();
        c.Translate(x, ground);
        c.RotateDegrees(tilt);
        float hw = sw / 2;
        using var path = new SKPath();
        switch (shape)
        {
            case 1:
            {
                var bar = sw * 0.3f;
                path.AddRect(new SKRect(-bar / 2, -sh, bar / 2, 0));
                path.AddRect(new SKRect(-hw, -sh * 0.78f, hw, (-sh * 0.78f) + bar));
                path.AddRect(new SKRect(-hw * 0.7f, -sh * 0.12f, hw * 0.7f, 0));
                break;
            }

            case 2:
            {
                var bar = sw * 0.26f;
                var cy = -sh * 0.72f;
                using var cross = new SKPath();
                cross.AddRect(new SKRect(-bar / 2, -sh, bar / 2, 0));
                cross.AddRect(new SKRect(-hw, cy - (bar / 2), hw, cy + (bar / 2)));
                cross.AddRect(new SKRect(-hw * 0.75f, -sh * 0.12f, hw * 0.75f, 0));
                using var ring = new SKPath();
                ring.AddCircle(0, cy, hw * 0.62f);
                using var hole = new SKPath();
                hole.AddCircle(0, cy, hw * 0.45f);
                using var band = ring.Op(hole, SKPathOp.Difference);
                using var all = cross.Op(band, SKPathOp.Union);
                path.AddPath(all);
                break;
            }

            case 3:
                path.MoveTo(-hw, 0);
                path.LineTo(-hw, -sh * 0.6f);
                path.QuadTo(-hw, -sh * 0.9f, 0, -sh);
                path.QuadTo(hw, -sh * 0.9f, hw, -sh * 0.6f);
                path.LineTo(hw, 0);
                path.Close();
                break;
            case 4:
                path.MoveTo(-hw, 0);
                path.LineTo(-hw, -sh * 0.12f);
                path.LineTo(-hw * 0.6f, -sh * 0.15f);
                path.LineTo(-hw * 0.42f, -sh * 0.86f);
                path.LineTo(0, -sh);
                path.LineTo(hw * 0.42f, -sh * 0.86f);
                path.LineTo(hw * 0.6f, -sh * 0.15f);
                path.LineTo(hw, -sh * 0.12f);
                path.LineTo(hw, 0);
                path.Close();
                break;
            default:
                path.MoveTo(-hw, 0);
                path.LineTo(-hw, -sh + hw);
                path.ArcTo(new SKRect(-hw, -sh, hw, -sh + sw), 180, 180, false);
                path.LineTo(hw, 0);
                path.Close();
                break;
        }

        var night = new SKColor(0x10, 0x14, 0x20);
        var rimColour = SpookyMix(night, new SKColor(0xD8, 0xE4, 0xF4), light * 0.9f);
        var top = SpookyMix(night, new SKColor(0x7A, 0x80, 0x8C), light);
        var bottom = SpookyMix(night, new SKColor(0x2A, 0x2C, 0x34), light);

        c.Save();
        c.ClipPath(path, antialias: true);
        using (var rim = new SKPaint { IsAntialias = true, Color = rimColour })
        {
            c.DrawPath(path, rim);
        }

        c.Translate(sw * 0.05f, sw * 0.035f);
        using (var body = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(-hw, -sh), new SKPoint(hw * 0.5f, 0), new[] { top, bottom }, SKShaderTileMode.Clamp) })
        {
            c.DrawPath(path, body);
        }

        c.Translate(-sw * 0.05f, -sw * 0.035f);

        // Weathering: lichen near the ground, a crack, and a worn inscription on the slabs.
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, sw * 0.04f))
        using (var moss = new SKPaint { IsAntialias = true, MaskFilter = blur, Color = SpookyMix(night, new SKColor(0x3A, 0x4A, 0x2A), light).WithAlpha(170) })
        {
            for (var i = 0; i < 3; i++)
            {
                var mx = R(rng, -hw, hw);
                c.DrawOval(new SKRect(mx - (sw * 0.2f), -sh * R(rng, 0.05f, 0.2f), mx + (sw * 0.2f), sh * 0.02f), moss);
            }
        }

        if (light > 0.8f)
        {
            using var crack = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = sw * 0.015f, Color = new SKColor(0x10, 0x10, 0x18, 160) };
            using var line = new SKPath();
            var cx0 = R(rng, -hw * 0.5f, hw * 0.5f);
            line.MoveTo(cx0, -sh * 0.9f);
            line.LineTo(cx0 + (sw * 0.06f), -sh * 0.8f);
            line.LineTo(cx0 - (sw * 0.03f), -sh * 0.7f);
            line.LineTo(cx0 + (sw * 0.04f), -sh * 0.6f);
            c.DrawPath(line, crack);

            if (shape is 0 or 3)
            {
                using var font = new SKFont(TitleTypeface.Value, sw * 0.24f);
                using var cut = new SKPaint { IsAntialias = true, Color = new SKColor(0x1A, 0x1C, 0x24, 200) };
                using var edge = new SKPaint { IsAntialias = true, Color = new SKColor(0xB0, 0xB8, 0xC8, 90) };
                c.DrawText("R.I.P", sw * 0.012f, (-sh * 0.55f) + (sw * 0.012f), SKTextAlign.Center, font, edge);
                c.DrawText("R.I.P", 0, -sh * 0.55f, SKTextAlign.Center, font, cut);
                using var groove = new SKPaint { IsAntialias = true, StrokeWidth = sw * 0.02f, Color = new SKColor(0x1A, 0x1C, 0x24, 150) };
                c.DrawLine(-hw * 0.5f, -sh * 0.42f, hw * 0.5f, -sh * 0.42f, groove);
                c.DrawLine(-hw * 0.35f, -sh * 0.34f, hw * 0.35f, -sh * 0.34f, groove);
            }
        }

        c.Restore();

        // A low mound of earth in front.
        using (var mound = new SKPaint { IsAntialias = true, Color = SpookyMix(night, new SKColor(0x08, 0x08, 0x0C), 1f) })
        {
            c.DrawOval(new SKRect(-sw * 0.75f, -sw * 0.06f, sw * 0.75f, sw * 0.08f), mound);
        }

        c.Restore();
    }

    /// <summary>A small stone chapel with a spire, far off.</summary>
    private static void SpookyChapel(SKCanvas c, float x, float ground, float u, SKColor colour)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = colour };
        using var chapel = new SKPath();
        chapel.MoveTo(x - (u * 0.2f), ground);
        chapel.LineTo(x - (u * 0.2f), ground - (u * 0.7f));
        chapel.LineTo(x - (u * 0.1f), ground - (u * 0.82f));
        chapel.LineTo(x, ground - (u * 1.45f));
        chapel.LineTo(x + (u * 0.1f), ground - (u * 0.82f));
        chapel.LineTo(x + (u * 0.2f), ground - (u * 0.7f));
        chapel.LineTo(x + (u * 0.2f), ground - (u * 0.42f));
        chapel.LineTo(x + (u * 0.55f), ground - (u * 0.68f));
        chapel.LineTo(x + (u * 0.9f), ground - (u * 0.42f));
        chapel.LineTo(x + (u * 0.9f), ground);
        chapel.Close();
        c.DrawPath(chapel, paint);
        using var cross = new SKPaint { IsAntialias = true, StrokeWidth = u * 0.03f, Color = colour };
        c.DrawLine(x, ground - (u * 1.45f), x, ground - (u * 1.6f), cross);
        c.DrawLine(x - (u * 0.05f), ground - (u * 1.54f), x + (u * 0.05f), ground - (u * 1.54f), cross);
        SpookyGlow(c, x, ground - (u * 0.45f), u * 0.4f, new SKColor(0xFF, 0xC0, 0x60, 110));
        using var lit = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xCC, 0x70) };
        using var arch = new SKPath();
        arch.AddRoundRect(new SKRect(x - (u * 0.06f), ground - (u * 0.58f), x + (u * 0.06f), ground - (u * 0.36f)), u * 0.06f, u * 0.06f);
        c.DrawPath(arch, lit);
        c.DrawRect(x + (u * 0.45f), ground - (u * 0.36f), u * 0.08f, u * 0.12f, lit);
    }

    /// <summary>Wrought-iron railings with spear-point tops.</summary>
    private static void SpookyRailing(SKCanvas c, float x0, float x1, float ground, float height, SKColor colour)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = colour };
        using var bar = new SKPaint { IsAntialias = true, StrokeWidth = height * 0.06f, Color = colour };
        c.DrawLine(x0, ground - (height * 0.82f), x1, ground - (height * 0.82f), bar);
        c.DrawLine(x0, ground - (height * 0.2f), x1, ground - (height * 0.2f), bar);
        var step = height * 0.22f;
        for (var x = x0; x <= x1; x += step)
        {
            c.DrawLine(x, ground, x, ground - height, bar);
            using var tip = new SKPath();
            tip.MoveTo(x - (height * 0.05f), ground - height);
            tip.LineTo(x, ground - (height * 1.14f));
            tip.LineTo(x + (height * 0.05f), ground - height);
            tip.Close();
            c.DrawPath(tip, paint);
        }

        foreach (var px in new[] { x0, x1 })
        {
            c.DrawRect(px - (height * 0.08f), ground - (height * 1.25f), height * 0.16f, height * 1.25f, paint);
            c.DrawCircle(px, ground - (height * 1.3f), height * 0.09f, paint);
        }
    }

    /// <summary>An iron lantern with a candle burning in it, standing on the ground.</summary>
    private static void SpookyCandleLantern(SKCanvas c, float x, float ground, float size)
    {
        SpookyGlow(c, x, ground - (size * 0.6f), size * 4f, new SKColor(0xFF, 0xA8, 0x40, 110));
        SpookyGlow(c, x, ground, size * 2.5f, new SKColor(0xFF, 0x90, 0x30, 90), 0.3f);
        var frame = new SKColor(0x10, 0x0C, 0x0C);
        using var iron = new SKPaint { IsAntialias = true, Color = frame };
        using var glass = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(new SKPoint(x, ground - (size * 0.55f)), size * 0.5f, new[] { new SKColor(0xFF, 0xF4, 0xC0), new SKColor(0xFF, 0xB0, 0x40), new SKColor(0xC0, 0x60, 0x10) }, new[] { 0f, 0.4f, 1f }, SKShaderTileMode.Clamp)
        };
        c.DrawRect(x - (size * 0.3f), ground - (size * 0.95f), size * 0.6f, size * 0.8f, glass);
        using var stroke = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = size * 0.07f, Color = frame };
        c.DrawRect(x - (size * 0.3f), ground - (size * 0.95f), size * 0.6f, size * 0.8f, stroke);
        c.DrawLine(x, ground - (size * 0.95f), x, ground - (size * 0.15f), stroke);
        c.DrawRect(x - (size * 0.38f), ground - (size * 0.18f), size * 0.76f, size * 0.18f, iron);
        using var roof = new SKPath();
        roof.MoveTo(x - (size * 0.42f), ground - (size * 0.92f));
        roof.LineTo(x, ground - (size * 1.25f));
        roof.LineTo(x + (size * 0.42f), ground - (size * 0.92f));
        roof.Close();
        c.DrawPath(roof, iron);
        stroke.Style = SKPaintStyle.Stroke;
        c.DrawCircle(x, ground - (size * 1.35f), size * 0.1f, stroke);
    }

    // ---------- Haunted house ----------

    private static void SpookyHauntedHouse(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var wide = w > h;

        // Purple night deepening to a blood-red glow on the horizon.
        SpookyBands(c, new SKRect(0, 0, w, h), new[]
        {
            new SKColor(0x08, 0x04, 0x18, 235), new SKColor(0x1E, 0x0C, 0x3A, 175), new SKColor(0x48, 0x14, 0x40, 130),
            new SKColor(0x8A, 0x1E, 0x2A, 150), new SKColor(0x10, 0x04, 0x0C, 245)
        }, new[] { 0f, 0.3f, 0.55f, 0.72f, 0.88f });
        Stars(c, w, h * 0.35f, wide ? 90 : 70, s, rng);

        var moon = new SKPoint(w * (wide ? 0.24f : 0.26f), h * 0.2f);
        var mr = s * 0.13f;
        SpookyGlow(c, moon.X, moon.Y, mr * 4.5f, new SKColor(0xFF, 0xD8, 0xC0, 80));
        Moon(c, moon.X, moon.Y, mr, rng);
        SpookyCloudWisps(c, w, moon.Y + (mr * 0.2f), s, new SKColor(0x14, 0x06, 0x22, 170), rng, 4);

        // A loose swarm of bats wheeling over the house's side of the sky, one crossing the moon.
        var house = new SKPoint(w * (wide ? 0.73f : 0.66f), h * 0.92f);
        var u = s * (wide ? 0.36f : 0.3f);
        SpookyBat(c, moon.X + (mr * 0.2f), moon.Y + (mr * 0.15f), s * 0.05f, -8, 0.8f, new SKColor(0x0C, 0x04, 0x10));
        var swarm = new List<(float X, float Y, float R)> { (moon.X, moon.Y, mr * 1.3f) };
        for (var i = 0; i < (wide ? 8 : 6); i++)
        {
            var br = s * R(rng, 0.025f, 0.05f);
            if (FindClear(rng, swarm, br, w * 0.5f, w * 0.95f, h * 0.07f, h * 0.28f, 1.5f) is { } at)
            {
                SpookyBat(c, at.X, at.Y, br, R(rng, -25, 25), R(rng, -1, 1), SpookyMix(new SKColor(0x0C, 0x04, 0x10), new SKColor(0x3A, 0x1C, 0x40), 1 - (br / (s * 0.05f))));
                swarm.Add((at.X, at.Y, br));
            }
        }

        // Far hills, then the hill the house stands on rising to the right.
        using (var far = SpookyRidge(w, h, h * 0.76f, h * 0.025f, 4, true, rng))
        {
            SpookyFillV(c, far, new SKColor(0x3A, 0x14, 0x30, 240), new SKColor(0x1C, 0x08, 0x18, 250));
        }

        SpookyFog(c, w, h * 0.79f, s * 0.03f, new SKColor(0xE0, 0x9A, 0xB0, 50), s, rng);
        using (var hill = new SKPath())
        {
            hill.MoveTo(-2, h + 2);
            hill.LineTo(-2, h * 0.9f);
            hill.CubicTo(w * 0.3f, h * 0.92f, house.X - (u * 0.9f), house.Y + (h * 0.01f), house.X, house.Y - (h * 0.005f));
            hill.CubicTo(house.X + (u * 0.8f), house.Y, w * 0.95f, house.Y + (h * 0.02f), w + 2, house.Y + (h * 0.03f));
            hill.LineTo(w + 2, h + 2);
            hill.Close();
            SpookyFillV(c, hill, new SKColor(0x14, 0x06, 0x12), new SKColor(0x04, 0x02, 0x06));
        }

        SpookyHouse(c, house.X, house.Y + (u * 0.02f), u, true, rng);
        SpookyTree(c, w * 0.04f, h * 0.94f, s * (wide ? 0.84f : 0.85f), 10f, SpookyInk, SpookyTitleZone(w, h), 0.48f, rng);
        SpookyRailing(c, -s * 0.02f, w * (wide ? 0.42f : 0.38f), h * 0.985f, s * 0.12f, new SKColor(0x05, 0x02, 0x06));
        SpookyFog(c, w, h * 0.93f, s * 0.035f, new SKColor(0xD0, 0xA8, 0xC8, 55), s, rng);
        SpookyGrass(c, w, h, s * 0.028f, new SKColor(0x04, 0x02, 0x06), rng);
    }

    /// <summary>
    /// A crooked Victorian house: a tower with a witch's-hat roof, a mansard roof with dormers, a gabled wing and
    /// chimneys, moonlight on its left edges and candlelight in some windows. (x, ground) is its centre at the
    /// base; <paramref name="u"/> its scale.
    /// </summary>
    private static void SpookyHouse(SKCanvas c, float x, float ground, float u, bool mirror, Random rng)
    {
        c.Save();
        c.Translate(x, ground);
        c.RotateDegrees(-1.5f);
        var flip = mirror ? -1f : 1f;
        c.Scale(flip, 1);

        static SKPath Poly(params float[] xy)
        {
            var p = new SKPath();
            p.MoveTo(xy[0], xy[1]);
            for (var i = 2; i < xy.Length; i += 2)
            {
                p.LineTo(xy[i], xy[i + 1]);
            }

            p.Close();
            return p;
        }

        var shell = new SKPath();
        void Add(SKPath part)
        {
            var merged = shell.Op(part, SKPathOp.Union) ?? shell;
            if (!ReferenceEquals(merged, shell))
            {
                shell.Dispose();
                shell = merged;
            }

            part.Dispose();
        }

        // Main block with a mansard roof and two dormers.
        Add(Poly(-0.38f * u, 0, -0.38f * u, -0.42f * u, 0.3f * u, -0.42f * u, 0.3f * u, 0));
        Add(Poly(-0.42f * u, -0.42f * u, -0.34f * u, -0.62f * u, 0.27f * u, -0.62f * u, 0.34f * u, -0.42f * u));
        Add(Poly(-0.3f * u, -0.64f * u, -0.26f * u, -0.67f * u, 0.22f * u, -0.67f * u, 0.25f * u, -0.64f * u, 0.25f * u, -0.6f * u, -0.3f * u, -0.6f * u));
        foreach (var dx in new[] { -0.16f, 0.1f })
        {
            Add(Poly((dx - 0.05f) * u, -0.5f * u, (dx - 0.05f) * u, -0.62f * u, dx * u, -0.71f * u, (dx + 0.05f) * u, -0.62f * u, (dx + 0.05f) * u, -0.5f * u));
        }

        // Tower on the left with a tall pointed roof and a finial.
        Add(Poly(-0.5f * u, 0, -0.5f * u, -0.78f * u, -0.26f * u, -0.78f * u, -0.26f * u, 0));
        Add(Poly(-0.53f * u, -0.78f * u, -0.53f * u, -0.82f * u, -0.23f * u, -0.82f * u, -0.23f * u, -0.78f * u));
        using (var roof = new SKPath())
        {
            roof.MoveTo(-0.55f * u, -0.82f * u);
            roof.QuadTo(-0.42f * u, -0.95f * u, -0.37f * u, -1.22f * u);
            roof.QuadTo(-0.33f * u, -0.95f * u, -0.21f * u, -0.82f * u);
            roof.Close();
            Add(new SKPath(roof));
        }

        Add(Poly(-0.375f * u, -1.2f * u, -0.37f * u, -1.3f * u, -0.365f * u, -1.2f * u));

        // Gabled wing on the right, with a porch.
        Add(Poly(0.28f * u, 0, 0.28f * u, -0.3f * u, 0.6f * u, -0.3f * u, 0.6f * u, 0));
        Add(Poly(0.25f * u, -0.3f * u, 0.44f * u, -0.55f * u, 0.63f * u, -0.3f * u));
        Add(Poly(-0.2f * u, -0.17f * u, 0.12f * u, -0.17f * u, 0.16f * u, -0.13f * u, -0.24f * u, -0.13f * u));

        // Chimneys, one leaning.
        Add(Poly(0.15f * u, -0.62f * u, 0.15f * u, -0.8f * u, 0.21f * u, -0.8f * u, 0.21f * u, -0.62f * u));
        Add(Poly(-0.1f * u, -0.66f * u, -0.12f * u, -0.78f * u, -0.07f * u, -0.79f * u, -0.05f * u, -0.66f * u));
        Add(Poly(0.5f * u, -0.38f * u, 0.5f * u, -0.5f * u, 0.55f * u, -0.5f * u, 0.55f * u, -0.38f * u));

        // Moonlight on the upper-left edges, then the dark body over it.
        c.Save();
        c.ClipPath(shell, antialias: true);
        using (var rim = new SKPaint { IsAntialias = true, Color = new SKColor(0x9A, 0x84, 0xB8) })
        {
            c.DrawPath(shell, rim);
        }

        c.Translate(u * 0.008f * flip, u * 0.006f);
        using (var body = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, -1.3f * u), new SKPoint(0, 0), new[] { new SKColor(0x22, 0x14, 0x30), new SKColor(0x0A, 0x04, 0x0E) }, SKShaderTileMode.Clamp) })
        {
            c.DrawPath(shell, body);
        }

        c.Restore();

        // Clapboard lines across the walls.
        c.Save();
        c.ClipPath(shell, antialias: true);
        using (var board = new SKPaint { IsAntialias = true, StrokeWidth = u * 0.003f, Color = new SKColor(0x30, 0x20, 0x40, 90) })
        {
            for (var y = -0.04f; y > -0.42f; y -= 0.035f)
            {
                c.DrawLine(-0.5f * u, y * u, 0.6f * u, y * u, board);
            }
        }

        c.Restore();

        // Windows: candlelit, dark or boarded.
        var windows = new (float X, float Y, float W, float H, bool Arched)[]
        {
            (-0.38f, -0.66f, 0.06f, 0.09f, true), (-0.38f, -0.44f, 0.06f, 0.1f, true), (-0.38f, -0.22f, 0.06f, 0.1f, false),
            (-0.21f, -0.36f, 0.07f, 0.12f, false), (-0.03f, -0.36f, 0.07f, 0.12f, false), (0.15f, -0.36f, 0.07f, 0.12f, false),
            (-0.16f, -0.59f, 0.045f, 0.06f, true), (0.1f, -0.59f, 0.045f, 0.06f, true),
            (0.44f, -0.42f, 0.06f, 0.07f, true), (0.44f, -0.2f, 0.12f, 0.1f, false), (0.15f, -0.1f, 0.07f, 0.09f, false)
        };
        var litPattern = new[] { true, false, true, true, false, true, false, true, true, false, true };
        for (var i = 0; i < windows.Length; i++)
        {
            var (wx, wy, ww, wh, arched) = windows[i];
            var rect = new SKRect((wx - (ww / 2)) * u, (wy - (wh / 2)) * u, (wx + (ww / 2)) * u, (wy + (wh / 2)) * u);
            using var shape = new SKPath();
            if (arched)
            {
                shape.MoveTo(rect.Left, rect.Bottom);
                shape.LineTo(rect.Left, rect.Top + (rect.Width / 2));
                shape.ArcTo(new SKRect(rect.Left, rect.Top, rect.Right, rect.Top + rect.Width), 180, 180, false);
                shape.LineTo(rect.Right, rect.Bottom);
                shape.Close();
            }
            else
            {
                shape.AddRect(rect);
            }

            var lit = litPattern[i] ^ (rng.NextDouble() < 0.15);
            if (lit)
            {
                SpookyGlow(c, rect.MidX, rect.MidY, rect.Height * 1.6f, new SKColor(0xFF, 0xA8, 0x30, 120));
                using var glass = new SKPaint
                {
                    IsAntialias = true,
                    Shader = SKShader.CreateRadialGradient(new SKPoint(rect.MidX, rect.Bottom), rect.Height, new[] { new SKColor(0xFF, 0xF0, 0xB0), new SKColor(0xFF, 0xB0, 0x40), new SKColor(0xD0, 0x60, 0x10) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp)
                };
                c.DrawPath(shape, glass);
            }
            else
            {
                using var dark = new SKPaint { IsAntialias = true, Color = new SKColor(0x2A, 0x1E, 0x3A) };
                c.DrawPath(shape, dark);
            }

            using var frame = new SKPaint { IsAntialias = true, StrokeWidth = u * 0.006f, Color = new SKColor(0x0A, 0x04, 0x0E) };
            c.DrawLine(rect.MidX, rect.Top, rect.MidX, rect.Bottom, frame);
            c.DrawLine(rect.Left, rect.MidY + (rect.Height * 0.1f), rect.Right, rect.MidY + (rect.Height * 0.1f), frame);
            if (!lit && i % 3 == 0)
            {
                using var plank = new SKPaint { IsAntialias = true, StrokeWidth = u * 0.012f, Color = new SKColor(0x3A, 0x2A, 0x30) };
                c.DrawLine(rect.Left - (u * 0.01f), rect.Top + (rect.Height * 0.2f), rect.Right + (u * 0.01f), rect.Bottom - (rect.Height * 0.2f), plank);
            }
        }

        // The front door, ajar with light spilling out.
        var door = new SKRect(-0.07f * u, -0.13f * u, -0.0f * u, 0);
        using (var spill = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xB8, 0x50) })
        {
            c.DrawRect(door.Left, door.Top, door.Width * 0.3f, door.Height, spill);
        }

        shell.Dispose();
        c.Restore();
    }

    // ---------- Witch's cauldron ----------

    private static void SpookyCauldronScene(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var wide = w > h;
        var cx = w / 2f;
        var ground = h * 0.95f;
        var r = s * (wide ? 0.135f : 0.14f);

        // A clearing in a dark wood, lit green from the brew.
        SpookyBands(c, new SKRect(0, 0, w, h), new[]
        {
            new SKColor(0x03, 0x06, 0x0A, 235), new SKColor(0x08, 0x16, 0x18, 185), new SKColor(0x0E, 0x2A, 0x20, 140),
            new SKColor(0x0A, 0x1C, 0x12, 215), new SKColor(0x03, 0x05, 0x04, 250)
        }, new[] { 0f, 0.3f, 0.6f, 0.8f, 1f });
        Stars(c, w, h * 0.25f, 35, s, rng);

        // Pale trunks deeper in the wood, away from the title's middle.
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.006f))
        using (var trunk = new SKPaint { IsAntialias = true, MaskFilter = blur })
        {
            for (var i = 0; i < (wide ? 12 : 8); i++)
            {
                var side = i % 2 == 0 ? -1 : 1;
                var tx = cx + (side * w * R(rng, 0.27f, 0.47f));
                var tw = s * R(rng, 0.012f, 0.03f);
                trunk.Color = new SKColor(0x1E, 0x3A, 0x30, (byte)rng.Next(90, 160));
                using var p = new SKPath();
                p.MoveTo(tx - (tw * 0.6f), h * 0.84f);
                p.LineTo(tx - (tw * 0.4f), 0);
                p.LineTo(tx + (tw * 0.4f), 0);
                p.LineTo(tx + (tw * 0.6f), h * 0.84f);
                p.Close();
                c.DrawPath(p, trunk);
            }
        }

        SpookyFog(c, w, h * 0.8f, s * 0.05f, new SKColor(0x90, 0xD8, 0xA0, 50), s, rng);
        SpookyGlow(c, cx, ground - (r * 1.6f), s * 0.8f, new SKColor(0x6C, 0xFF, 0x5A, 70));

        using (var floor = SpookyRidge(w, h, h * 0.86f, h * 0.012f, 3, true, rng))
        {
            SpookyFillV(c, floor, new SKColor(0x10, 0x1C, 0x14), new SKColor(0x03, 0x05, 0x04));
        }

        SpookyGlow(c, cx, ground, r * 2.6f, new SKColor(0x7C, 0xFF, 0x60, 70), 0.3f);

        // Old trees framing the clearing.
        SpookyTree(c, w * 0.03f, h * 0.9f, s * (wide ? 0.88f : 0.9f), 10f, new SKColor(0x03, 0x06, 0x05), SpookyTitleZone(w, h), 0.48f, rng);
        SpookyTree(c, w * 0.97f, h * 0.9f, s * (wide ? 0.84f : 0.85f), -10f, new SKColor(0x03, 0x06, 0x05), SpookyTitleZone(w, h), 0.48f, rng);

        // Steam rising off the brew, thinning out before it reaches the title.
        SpookyVapour(c, cx, ground - (r * 1.8f), h * 0.45f, r * 1.7f, new SKColor(0xC0, 0xFF, 0xB0, 190), rng);
        SpookyCauldron(c, cx, ground, r, rng);

        // Toadstools, a tall candle, spell books and a potion on one side; bottles and the witch's cat on the other.
        float X(float square, float wideX) => w * (wide ? wideX : square);
        SpookyMushroom(c, X(0.06f, 0.1f), h * 0.975f, s * 0.05f, new SKColor(0x8A, 0x3A, 0xC8));
        SpookyMushroom(c, X(0.11f, 0.135f), h * 0.985f, s * 0.035f, new SKColor(0x8A, 0x3A, 0xC8));
        SpookyCandle(c, X(0.17f, 0.2f), ground + (s * 0.01f), s * 0.15f, s * 0.028f, rng);
        var bookTop = SpookyBooks(c, X(0.27f, 0.29f), ground, s * 0.11f, rng);
        SpookyCandle(c, X(0.26f, 0.28f), bookTop, s * 0.06f, s * 0.02f, rng);
        SpookyPotion(c, X(0.36f, 0.37f), ground + (s * 0.005f), s * 0.085f, 0, new SKColor(0xC0, 0x50, 0xFF));
        SpookyPotion(c, X(0.65f, 0.625f), ground, s * 0.11f, 1, new SKColor(0x40, 0xC8, 0xFF));
        SpookyPotion(c, X(0.71f, 0.67f), ground + (s * 0.005f), s * 0.06f, 0, new SKColor(0xFF, 0x50, 0x70));
        SpookyCat(c, X(0.85f, 0.79f), ground, s * 0.16f, new SKColor(0x04, 0x05, 0x06), new SKColor(0xC8, 0xFF, 0x60));

        // Sparks drifting up from the fire.
        using (var sparkBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.003f))
        using (var spark = new SKPaint { IsAntialias = true, MaskFilter = sparkBlur })
        {
            for (var i = 0; i < 26; i++)
            {
                var sy = R(rng, h * 0.68f, h * 0.95f);
                var sx = cx + (R(rng, -1f, 1f) * r * 1.6f * (1.3f - ((sy - (h * 0.66f)) / (h * 0.3f))));
                spark.Color = (i % 3 == 0 ? new SKColor(0xFF, 0xC0, 0x50) : new SKColor(0xB0, 0xFF, 0x80)).WithAlpha((byte)rng.Next(120, 230));
                c.DrawCircle(sx, sy, s * R(rng, 0.002f, 0.005f), spark);
            }
        }

        SpookyGrass(c, w, h, s * 0.025f, new SKColor(0x02, 0x04, 0x03), rng);
    }

    /// <summary>A witch's cauldron on a log fire, bubbling over with a glowing green brew.</summary>
    private static void SpookyCauldron(SKCanvas c, float cx, float ground, float r, Random rng)
    {
        var bodyCy = ground - (r * 1.2f);
        var rx = r;
        var ry = r * 0.8f;
        var rimY = bodyCy - (ry * 0.78f);

        SpookyFire(c, cx, ground, r * 1.2f, rng);

        // Stubby legs.
        using (var iron = new SKPaint { IsAntialias = true, Color = new SKColor(0x08, 0x08, 0x0A) })
        {
            foreach (var dx in new[] { -0.55f, 0.55f })
            {
                using var leg = new SKPath();
                leg.MoveTo(cx + (dx * r) - (r * 0.1f), bodyCy + (ry * 0.6f));
                leg.LineTo(cx + (dx * r * 1.12f) - (r * 0.06f), ground);
                leg.LineTo(cx + (dx * r * 1.12f) + (r * 0.08f), ground);
                leg.LineTo(cx + (dx * r) + (r * 0.12f), bodyCy + (ry * 0.6f));
                leg.Close();
                c.DrawPath(leg, iron);
            }
        }

        using var oval = new SKPath();
        oval.AddOval(new SKRect(cx - rx, bodyCy - ry, cx + rx, bodyCy + ry));
        using var cut = new SKPath();
        cut.AddRect(new SKRect(cx - rx - 2, rimY, cx + rx + 2, bodyCy + ry + 2));
        using var body = oval.Op(cut, SKPathOp.Intersect);
        using (var metal = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(new SKPoint(cx - (rx * 0.3f), bodyCy - (ry * 0.3f)), r * 1.25f, new[] { new SKColor(0x30, 0x38, 0x36), new SKColor(0x12, 0x16, 0x16), new SKColor(0x03, 0x04, 0x04) }, new[] { 0f, 0.45f, 1f }, SKShaderTileMode.Clamp)
        })
        {
            c.DrawPath(body, metal);
        }

        c.Save();
        c.ClipPath(body, antialias: true);
        SpookyBands(c, new SKRect(cx - rx, rimY, cx + rx, rimY + (ry * 0.6f)), new[] { new SKColor(0x7C, 0xFF, 0x50, 120), new SKColor(0x7C, 0xFF, 0x50, 0) }, new[] { 0f, 1f });
        SpookyBands(c, new SKRect(cx - rx, bodyCy + (ry * 0.45f), cx + rx, bodyCy + ry), new[] { new SKColor(0xFF, 0x60, 0x10, 0), new SKColor(0xFF, 0x60, 0x10, 120) }, new[] { 0f, 1f });

        // Green light from the brew catching the iron's curve: a soft sheen on the shoulder and a rim down each side.
        using (var sheenBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.06f))
        using (var sheen = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = r * 0.07f, MaskFilter = sheenBlur, Color = new SKColor(0xA0, 0xFF, 0x80, 120) })
        using (var arc = new SKPath())
        {
            arc.AddArc(new SKRect(cx - (rx * 0.8f), bodyCy - (ry * 0.8f), cx + (rx * 0.8f), bodyCy + (ry * 0.8f)), 200, 50);
            c.DrawPath(arc, sheen);
        }

        using (var edgeBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.025f))
        using (var edge = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = r * 0.05f, MaskFilter = edgeBlur, Color = new SKColor(0x70, 0xE0, 0x60, 110) })
        {
            c.DrawOval(new SKRect(cx - rx, bodyCy - ry, cx + rx, bodyCy + ry), edge);
        }

        c.Restore();

        // The rim, then the brew inside it.
        var rimRect = new SKRect(cx - (rx * 0.86f), rimY - (r * 0.17f), cx + (rx * 0.86f), rimY + (r * 0.17f));
        using (var rim = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, rimRect.Top), new SKPoint(0, rimRect.Bottom), new[] { new SKColor(0x30, 0x36, 0x30), new SKColor(0x0A, 0x0A, 0x0C) }, SKShaderTileMode.Clamp) })
        {
            c.DrawOval(rimRect, rim);
        }

        var brewRect = new SKRect(cx - (rx * 0.72f), rimY - (r * 0.11f), cx + (rx * 0.72f), rimY + (r * 0.1f));
        SpookyGlow(c, cx, rimY, r * 1.2f, new SKColor(0xA0, 0xFF, 0x70, 120), 0.5f);
        using (var brew = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(new SKPoint(cx, rimY), rx * 0.75f, new[] { new SKColor(0xF0, 0xFF, 0xC0), new SKColor(0x90, 0xFF, 0x50), new SKColor(0x30, 0xA0, 0x20) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp)
        })
        {
            c.DrawOval(brewRect, brew);
        }

        using (var lip = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = r * 0.025f, Color = new SKColor(0xB0, 0xFF, 0x90, 150) })
        using (var lipPath = new SKPath())
        {
            lipPath.AddArc(rimRect, 200, 140);
            c.DrawPath(lipPath, lip);
        }

        // Brew slopping over the front, and bubbles on and above the surface.
        using (var ooze = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, rimY), new SKPoint(0, rimY + (r * 0.5f)), new[] { new SKColor(0xB0, 0xFF, 0x70), new SKColor(0x40, 0xC0, 0x30) }, SKShaderTileMode.Clamp) })
        {
            foreach (var (dx, len) in new[] { (-0.5f, 0.16f), (-0.15f, 0.08f), (0.32f, 0.22f) })
            {
                var x = cx + (dx * rx);
                var top = rimY + (r * 0.12f);
                using var drip = new SKPath();
                drip.MoveTo(x - (r * 0.06f), top - (r * 0.04f));
                drip.QuadTo(x - (r * 0.025f), top, x - (r * 0.025f), top + (r * len));
                drip.ArcTo(new SKRect(x - (r * 0.035f), top + (r * len) - (r * 0.035f), x + (r * 0.035f), top + (r * len) + (r * 0.035f)), 180, -180, false);
                drip.QuadTo(x + (r * 0.025f), top, x + (r * 0.06f), top - (r * 0.04f));
                drip.Close();
                c.DrawPath(drip, ooze);
            }
        }

        using var bubble = new SKPaint { IsAntialias = true, Color = new SKColor(0xC8, 0xFF, 0x98, 230) };
        using var bubbleRim = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = r * 0.012f, Color = new SKColor(0x30, 0x90, 0x20, 200) };
        using var shine = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(220) };
        foreach (var (dx, dy, br) in new[] { (-0.4f, -0.01f, 0.07f), (-0.15f, 0.03f, 0.045f), (0.22f, -0.02f, 0.08f), (0.48f, 0.02f, 0.04f), (0.05f, -0.22f, 0.035f), (-0.22f, -0.4f, 0.025f), (0.15f, -0.55f, 0.02f) })
        {
            var bx = cx + (dx * rx);
            var by = rimY + (dy * r);
            c.DrawCircle(bx, by, br * r, bubble);
            c.DrawCircle(bx, by, br * r, bubbleRim);
            c.DrawCircle(bx - (br * r * 0.35f), by - (br * r * 0.35f), br * r * 0.25f, shine);
        }
    }

    /// <summary>A log fire: crossed logs and licking flames, with a warm glow on the ground.</summary>
    private static void SpookyFire(SKCanvas c, float cx, float ground, float size, Random rng)
    {
        SpookyGlow(c, cx, ground - (size * 0.15f), size * 1.8f, new SKColor(0xFF, 0x80, 0x20, 150), 0.45f);
        using (var log = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, ground - (size * 0.14f)), new SKPoint(0, ground), new[] { new SKColor(0x4A, 0x28, 0x16), new SKColor(0x14, 0x0A, 0x06) }, SKShaderTileMode.Clamp) })
        {
            foreach (var angle in new[] { -12f, 10f })
            {
                c.Save();
                c.Translate(cx, ground - (size * 0.06f));
                c.RotateDegrees(angle);
                c.DrawRoundRect(new SKRect(-size * 0.75f, -size * 0.07f, size * 0.75f, size * 0.07f), size * 0.06f, size * 0.06f, log);
                c.Restore();
            }
        }

        using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, size * 0.02f);
        for (var layer = 0; layer < 2; layer++)
        {
            for (var i = -3; i <= 3; i++)
            {
                var fx = cx + (i * size * 0.2f) + R(rng, -size * 0.03f, size * 0.03f);
                var fh = size * (0.62f - (Math.Abs(i) * 0.1f)) * R(rng, 0.8f, 1.15f) * (layer == 0 ? 1f : 0.6f);
                var fw = size * (layer == 0 ? 0.13f : 0.08f);
                var lean = R(rng, -fw, fw);
                var bottom = ground - (size * 0.08f);
                using var flame = new SKPath();
                flame.MoveTo(fx - fw, bottom);
                flame.CubicTo(fx - fw, bottom - (fh * 0.5f), fx + lean - (fw * 0.3f), bottom - (fh * 0.7f), fx + lean, bottom - fh);
                flame.CubicTo(fx + lean + (fw * 0.4f), bottom - (fh * 0.6f), fx + fw, bottom - (fh * 0.4f), fx + fw, bottom);
                flame.Close();
                var colours = layer == 0
                    ? new[] { new SKColor(0xFF, 0x50, 0x10, 0), new SKColor(0xFF, 0x70, 0x10, 230), new SKColor(0xFF, 0xA0, 0x30) }
                    : new[] { new SKColor(0xFF, 0xD0, 0x40, 0), new SKColor(0xFF, 0xE0, 0x70, 240), new SKColor(0xFF, 0xFA, 0xD0) };
                using var paint = new SKPaint { IsAntialias = true, MaskFilter = blur, Shader = SKShader.CreateLinearGradient(new SKPoint(0, bottom - fh), new SKPoint(0, bottom), colours, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp) };
                c.DrawPath(flame, paint);
            }
        }
    }

    /// <summary>Curling wisps of steam from <paramref name="bottom"/> up to <paramref name="top"/>, fading as they rise.</summary>
    private static void SpookyVapour(SKCanvas c, float x, float bottom, float top, float width, SKColor colour, Random rng)
    {
        using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, width * 0.06f);
        for (var k = 0; k < 5; k++)
        {
            var x0 = x + R(rng, -width * 0.4f, width * 0.4f);
            var sway = R(rng, width * 0.2f, width * 0.45f) * (k % 2 == 0 ? 1 : -1);
            using var path = new SKPath();
            path.MoveTo(x0, bottom);
            path.CubicTo(x0 + sway, bottom - ((bottom - top) * 0.3f), x0 - sway, bottom - ((bottom - top) * 0.65f), x0 + (sway * 0.6f), top);
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeCap = SKStrokeCap.Round,
                StrokeWidth = width * R(rng, 0.12f, 0.25f),
                MaskFilter = blur,
                Shader = SKShader.CreateLinearGradient(new SKPoint(0, bottom), new SKPoint(0, top), new[] { colour, colour.WithAlpha((byte)(colour.Alpha * 0.4f)), colour.WithAlpha(0) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp)
            };
            c.DrawPath(path, paint);
        }
    }

    /// <summary>A stack of old spell books; returns the top of the stack.</summary>
    private static float SpookyBooks(SKCanvas c, float x, float ground, float size, Random rng)
    {
        var colours = new[] { new SKColor(0x5A, 0x14, 0x18), new SKColor(0x1A, 0x40, 0x2A), new SKColor(0x3A, 0x1C, 0x4A) };
        var y = ground;
        using var pages = new SKPaint { IsAntialias = true, Color = new SKColor(0xC8, 0xB8, 0x90) };
        using var band = new SKPaint { IsAntialias = true, Color = new SKColor(0xC8, 0x9A, 0x40, 200) };
        for (var i = 0; i < 3; i++)
        {
            var bw = size * (1f - (i * 0.12f));
            var bh = size * R(rng, 0.17f, 0.24f);
            c.Save();
            c.Translate(x + R(rng, -size * 0.05f, size * 0.05f), y - (bh / 2));
            c.RotateDegrees(R(rng, -4, 4));
            c.DrawRoundRect(new SKRect(-bw / 2, -bh / 2, bw / 2, bh / 2), bh * 0.15f, bh * 0.15f, pages);
            using var cover = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, -bh / 2), new SKPoint(0, bh / 2), new[] { Lighten(colours[i], 0.15f), colours[i] }, SKShaderTileMode.Clamp) };
            c.DrawRoundRect(new SKRect(-bw / 2, -bh / 2, (bw / 2) - (bw * 0.06f), bh / 2), bh * 0.15f, bh * 0.15f, cover);
            c.DrawRect(-bw * 0.32f, -bh / 2, bw * 0.05f, bh, band);
            c.Restore();
            y -= bh * 0.95f;
        }

        return y;
    }

    /// <summary>A dripping wax candle with a flickering flame, standing on <paramref name="bottom"/>.</summary>
    private static void SpookyCandle(SKCanvas c, float x, float bottom, float height, float width, Random rng)
    {
        var top = bottom - height;
        SpookyGlow(c, x, top - (width * 0.8f), height * 1.4f, new SKColor(0xFF, 0xB0, 0x40, 110));
        using (var wax = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(x - (width / 2), 0), new SKPoint(x + (width / 2), 0), new[] { new SKColor(0xF4, 0xE8, 0xC8), new SKColor(0xC8, 0xB0, 0x88), new SKColor(0x6A, 0x58, 0x44) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp) })
        {
            c.DrawRect(x - (width / 2), top, width, height, wax);
            c.DrawOval(new SKRect(x - (width * 0.55f), top - (width * 0.12f), x + (width * 0.55f), top + (width * 0.12f)), wax);
            for (var i = 0; i < 2; i++)
            {
                var dx = x + R(rng, -width * 0.4f, width * 0.3f);
                var len = height * R(rng, 0.12f, 0.3f);
                c.DrawRoundRect(new SKRect(dx, top, dx + (width * 0.18f), top + len), width * 0.09f, width * 0.09f, wax);
            }
        }

        using (var wick = new SKPaint { IsAntialias = true, StrokeWidth = width * 0.06f, Color = new SKColor(0x20, 0x18, 0x10) })
        {
            c.DrawLine(x, top, x, top - (width * 0.25f), wick);
        }

        using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, width * 0.08f);
        using var flame = new SKPath();
        var fh = width * 1.3f;
        flame.MoveTo(x, top - (width * 0.1f));
        flame.CubicTo(x - (width * 0.45f), top - (width * 0.2f), x - (width * 0.1f), top - (fh * 0.7f), x + (width * 0.05f), top - fh);
        flame.CubicTo(x + (width * 0.25f), top - (fh * 0.6f), x + (width * 0.45f), top - (width * 0.2f), x, top - (width * 0.1f));
        using var outer = new SKPaint { IsAntialias = true, MaskFilter = blur, Color = new SKColor(0xFF, 0x9A, 0x20) };
        c.DrawPath(flame, outer);
        c.Save();
        c.Translate(x, top);
        c.Scale(0.55f, 0.6f);
        c.Translate(-x, -top);
        using var inner = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xF8, 0xD8) };
        c.DrawPath(flame, inner);
        c.Restore();
    }

    /// <summary>A glass potion bottle with glowing liquid: shape 0 a round flask, 1 a tall bottle.</summary>
    private static void SpookyPotion(SKCanvas c, float x, float ground, float size, int shape, SKColor liquid)
    {
        using var glass = new SKPath();
        if (shape == 0)
        {
            glass.AddCircle(x, ground - (size * 0.38f), size * 0.38f);
            glass.AddRect(new SKRect(x - (size * 0.09f), ground - (size * 0.95f), x + (size * 0.09f), ground - (size * 0.6f)));
        }
        else
        {
            glass.AddRoundRect(new SKRect(x - (size * 0.22f), ground - (size * 0.62f), x + (size * 0.22f), ground), size * 0.06f, size * 0.06f);
            glass.MoveTo(x - (size * 0.22f), ground - (size * 0.58f));
            glass.QuadTo(x - (size * 0.2f), ground - (size * 0.75f), x - (size * 0.07f), ground - (size * 0.78f));
            glass.LineTo(x - (size * 0.07f), ground - (size * 0.95f));
            glass.LineTo(x + (size * 0.07f), ground - (size * 0.95f));
            glass.LineTo(x + (size * 0.07f), ground - (size * 0.78f));
            glass.QuadTo(x + (size * 0.2f), ground - (size * 0.75f), x + (size * 0.22f), ground - (size * 0.58f));
            glass.Close();
        }

        using var merged = glass.Simplify() ?? new SKPath(glass);
        SpookyGlow(c, x, ground - (size * 0.35f), size * 1.1f, liquid.WithAlpha(90));
        using (var bottle = new SKPaint { IsAntialias = true, Color = new SKColor(0x10, 0x18, 0x20, 210) })
        {
            c.DrawPath(merged, bottle);
        }

        c.Save();
        c.ClipPath(merged, antialias: true);
        using (var fill = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(new SKPoint(x, ground - (size * 0.3f)), size * 0.45f, new[] { Lighten(liquid, 0.6f), liquid, Darken(liquid, 0.45f) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp)
        })
        {
            c.DrawRect(x - size, ground - (size * 0.5f), size * 2, size * 0.5f, fill);
        }

        c.Restore();
        using (var edge = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = size * 0.02f, Color = new SKColor(0xD0, 0xF0, 0xFF, 120) })
        {
            c.DrawPath(merged, edge);
        }

        using (var shine = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = size * 0.035f, Color = SKColors.White.WithAlpha(150) })
        {
            c.DrawLine(x - (size * 0.15f), ground - (size * 0.5f), x - (size * 0.16f), ground - (size * 0.3f), shine);
        }

        using var cork = new SKPaint { IsAntialias = true, Color = new SKColor(0x8A, 0x60, 0x3A) };
        c.DrawRoundRect(new SKRect(x - (size * 0.08f), ground - (size * 1.05f), x + (size * 0.08f), ground - (size * 0.92f)), size * 0.02f, size * 0.02f, cork);
    }

    /// <summary>A toadstool with a glowing, spotted cap.</summary>
    private static void SpookyMushroom(SKCanvas c, float x, float ground, float size, SKColor cap)
    {
        SpookyGlow(c, x, ground - (size * 0.6f), size * 1.4f, cap.WithAlpha(90));
        using (var stem = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(x - (size * 0.15f), 0), new SKPoint(x + (size * 0.15f), 0), new[] { new SKColor(0xE8, 0xE0, 0xD0), new SKColor(0x90, 0x88, 0x80) }, SKShaderTileMode.Clamp) })
        using (var p = new SKPath())
        {
            p.MoveTo(x - (size * 0.16f), ground);
            p.QuadTo(x - (size * 0.08f), ground - (size * 0.4f), x - (size * 0.1f), ground - (size * 0.7f));
            p.LineTo(x + (size * 0.1f), ground - (size * 0.7f));
            p.QuadTo(x + (size * 0.08f), ground - (size * 0.4f), x + (size * 0.16f), ground);
            p.Close();
            c.DrawPath(p, stem);
        }

        using (var top = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(x - (size * 0.15f), ground - (size * 0.95f)), size * 0.7f, new[] { Lighten(cap, 0.4f), cap, Darken(cap, 0.5f) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp) })
        using (var dome = new SKPath())
        {
            dome.MoveTo(x - (size * 0.55f), ground - (size * 0.62f));
            dome.CubicTo(x - (size * 0.5f), ground - (size * 1.15f), x + (size * 0.5f), ground - (size * 1.15f), x + (size * 0.55f), ground - (size * 0.62f));
            dome.QuadTo(x, ground - (size * 0.72f), x - (size * 0.55f), ground - (size * 0.62f));
            dome.Close();
            c.DrawPath(dome, top);
        }

        using var spot = new SKPaint { IsAntialias = true, Color = new SKColor(0xF0, 0xE8, 0xFF, 220) };
        c.DrawCircle(x - (size * 0.22f), ground - (size * 0.85f), size * 0.07f, spot);
        c.DrawCircle(x + (size * 0.12f), ground - (size * 0.92f), size * 0.06f, spot);
        c.DrawCircle(x + (size * 0.32f), ground - (size * 0.75f), size * 0.045f, spot);
    }

    /// <summary>A black cat sitting with its tail curled round, eyes catching the light.</summary>
    private static void SpookyCat(SKCanvas c, float x, float ground, float size, SKColor colour, SKColor eye)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = colour };
        using var cat = new SKPath();

        // Haunches and chest in one shape, then the head and ears.
        cat.MoveTo(x - (size * 0.3f), ground);
        cat.CubicTo(x - (size * 0.42f), ground - (size * 0.35f), x - (size * 0.25f), ground - (size * 0.62f), x - (size * 0.08f), ground - (size * 0.7f));
        cat.LineTo(x + (size * 0.1f), ground - (size * 0.72f));
        cat.CubicTo(x + (size * 0.12f), ground - (size * 0.45f), x + (size * 0.25f), ground - (size * 0.2f), x + (size * 0.22f), ground);
        cat.Close();
        cat.AddOval(new SKRect(x - (size * 0.15f), ground - (size * 0.98f), x + (size * 0.17f), ground - (size * 0.68f)));
        cat.MoveTo(x - (size * 0.14f), ground - (size * 0.86f));
        cat.LineTo(x - (size * 0.13f), ground - (size * 1.08f));
        cat.LineTo(x - (size * 0.02f), ground - (size * 0.96f));
        cat.Close();
        cat.MoveTo(x + (size * 0.16f), ground - (size * 0.86f));
        cat.LineTo(x + (size * 0.15f), ground - (size * 1.08f));
        cat.LineTo(x + (size * 0.04f), ground - (size * 0.96f));
        cat.Close();
        c.DrawPath(cat, paint);

        using (var tail = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = size * 0.07f, Color = colour })
        using (var t = new SKPath())
        {
            t.MoveTo(x + (size * 0.15f), ground - (size * 0.03f));
            t.CubicTo(x + (size * 0.5f), ground, x + (size * 0.55f), ground - (size * 0.3f), x + (size * 0.4f), ground - (size * 0.4f));
            c.DrawPath(t, tail);
        }

        // Rim light from the brew on the side facing it.
        using (var rim = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = size * 0.012f, Color = eye.WithAlpha(90) })
        {
            c.Save();
            c.ClipRect(new SKRect(x - size, ground - (size * 1.2f), x - (size * 0.05f), ground));
            c.DrawPath(cat, rim);
            c.Restore();
        }

        using var glow = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, size * 0.015f);
        using var eyes = new SKPaint { IsAntialias = true, Color = eye, MaskFilter = glow };
        foreach (var ex in new[] { -0.06f, 0.08f })
        {
            c.DrawOval(new SKRect(x + ((ex - 0.035f) * size), ground - (size * 0.86f), x + ((ex + 0.035f) * size), ground - (size * 0.82f)), eyes);
        }
    }

    // ---------- Ghostbusters Day: a haunted city under a spectral storm ----------

    private static void SpookyHauntedCity(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var wide = w > h;

        SpookyBands(c, new SKRect(0, 0, w, h), new[]
        {
            new SKColor(0x02, 0x07, 0x0A, 235), new SKColor(0x05, 0x18, 0x18, 190), new SKColor(0x0E, 0x30, 0x22, 140),
            new SKColor(0x1A, 0x48, 0x2A, 150), new SKColor(0x03, 0x08, 0x06, 245)
        }, new[] { 0f, 0.3f, 0.58f, 0.74f, 0.9f });
        Stars(c, w, h * 0.3f, 50, s, rng);

        // The storm: a swirl of green light over the tallest tower, with dark cloud banked round it.
        var eye = new SKPoint(w * (wide ? 0.8f : 0.76f), h * 0.14f);
        var storm = s * (wide ? 0.21f : 0.19f);
        SpookyGlow(c, eye.X, eye.Y, storm * 2f, new SKColor(0x50, 0xE0, 0x60, 80), 0.55f);
        using (var cloudBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.025f))
        using (var cloud = new SKPaint { IsAntialias = true, MaskFilter = cloudBlur })
        {
            for (var i = 0; i < 18; i++)
            {
                var a = i / 18f * MathF.PI * 2;
                var d = storm * R(rng, 0.75f, 1.3f);
                var px = eye.X + (MathF.Cos(a) * d * 1.3f);
                var py = eye.Y + (MathF.Sin(a) * d * 0.45f);
                var pr = s * R(rng, 0.04f, 0.075f);
                cloud.Color = new SKColor(0x06, 0x16, 0x10, (byte)rng.Next(170, 230));
                c.DrawOval(new SKRect(px - (pr * 1.7f), py - pr, px + (pr * 1.7f), py + pr), cloud);
            }
        }

        // Spiralling filaments of light: a wide soft glow under each, a fine bright core on top.
        using (var wideBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.012f))
        using (var fineBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.002f))
        {
            for (var arm = 0; arm < 6; arm++)
            {
                using var spiral = new SKPath();
                var start = (arm * MathF.PI / 3) + R(rng, -0.3f, 0.3f);
                var reach = R(rng, 0.75f, 1.1f);
                for (var t = 0f; t <= 1.001f; t += 0.04f)
                {
                    var a = start + (t * MathF.PI * 1.3f);
                    var d = storm * reach * (1f - (t * 0.9f));
                    var p = new SKPoint(eye.X + (MathF.Cos(a) * d * 1.3f), eye.Y + (MathF.Sin(a) * d * 0.45f));
                    if (t == 0)
                    {
                        spiral.MoveTo(p);
                    }
                    else
                    {
                        spiral.LineTo(p);
                    }
                }

                using var soft = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = s * 0.02f, MaskFilter = wideBlur, Color = new SKColor(0x60, 0xFF, 0x70, 70) };
                c.DrawPath(spiral, soft);
                using var core = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = s * 0.0035f, MaskFilter = fineBlur, Color = new SKColor(0xC8, 0xFF, 0xC0, (byte)rng.Next(110, 190)) };
                c.DrawPath(spiral, core);
            }
        }

        SpookyGlow(c, eye.X, eye.Y, storm * 0.45f, new SKColor(0xE0, 0xFF, 0xD0, 220), 0.55f);

        // Far skyline in the haze, the haunted tower, a green fog, then the nearer blocks.
        var far = h * 0.79f;
        SpookySkyline(c, -s * 0.02f, w + (s * 0.02f), far, s * 0.06f, s * 0.13f, s, new SKColor(0x1A, 0x36, 0x30, 230), new SKColor(0xA0, 0xE0, 0xB0, 90), 0.2f, rng);
        var towerX = w * (wide ? 0.8f : 0.76f);
        SpookyDecoTower(c, towerX, far + (h * 0.02f), eye.Y + (storm * 0.35f), s * 0.09f, eye);
        SpookyFog(c, w, far + (h * 0.01f), s * 0.035f, new SKColor(0x80, 0xFF, 0x90, 50), s, rng);
        var near = h * 0.87f;
        SpookySkyline(c, -s * 0.02f, w + (s * 0.02f), near, s * 0.05f, s * 0.13f, s, new SKColor(0x08, 0x12, 0x10, 245), new SKColor(0xFF, 0xD0, 0x80, 200), 0.3f, rng);

        // Ghosts: one big one swooping in from the top left, others circling the storm, one rising off the roofs.
        SpookyGhost(c, w * (wide ? 0.18f : 0.2f), h * 0.17f, s * 0.075f, -18, 1, 1f);
        SpookyGhost(c, eye.X - (storm * 1.1f), eye.Y + (storm * 0.3f), s * 0.04f, 30, -1, 0.75f);
        SpookyGhost(c, eye.X + (storm * 0.75f), eye.Y + (storm * 0.5f), s * 0.03f, -35, 1, 0.6f);
        SpookyGhost(c, w * (wide ? 0.42f : 0.46f), h * 0.1f, s * 0.028f, 10, -1, 0.55f);
        SpookyGhost(c, w * (wide ? 0.6f : 0.62f), h * 0.79f, s * 0.05f, 12, -1, 0.85f);

        // Rooftop in the foreground: a parapet, a water tower and chimney pots.
        var roof = h * 0.93f;
        using (var top = new SKPath())
        {
            top.MoveTo(-2, h + 2);
            top.LineTo(-2, roof);
            top.LineTo(w * 0.45f, roof);
            top.LineTo(w * 0.45f, roof + (h * 0.015f));
            top.LineTo(w + 2, roof + (h * 0.015f));
            top.LineTo(w + 2, h + 2);
            top.Close();
            SpookyFillV(c, top, new SKColor(0x0A, 0x12, 0x10), new SKColor(0x02, 0x04, 0x03));
        }

        using (var ledge = new SKPaint { IsAntialias = true, StrokeWidth = s * 0.004f, Color = new SKColor(0x60, 0xC0, 0x80, 120) })
        {
            c.DrawLine(0, roof, w * 0.45f, roof, ledge);
        }

        SpookyWaterTower(c, w * (wide ? 0.12f : 0.15f), roof, s * 0.24f, new SKColor(0x03, 0x06, 0x05));
        using (var pot = new SKPaint { IsAntialias = true, Color = new SKColor(0x03, 0x06, 0x05) })
        {
            // A chimney stack with three pots.
            var stack = new SKRect(w - (s * 0.17f), roof - (s * 0.045f), w - (s * 0.05f), roof + (h * 0.02f));
            c.DrawRect(stack, pot);
            c.DrawRect(stack.Left - (s * 0.006f), stack.Top - (s * 0.008f), stack.Width + (s * 0.012f), s * 0.012f, pot);
            for (var i = 0; i < 3; i++)
            {
                var px = stack.Left + (stack.Width * (0.2f + (i * 0.3f)));
                c.DrawRect(px - (s * 0.009f), stack.Top - (s * 0.035f), s * 0.018f, s * 0.03f, pot);
            }
            using var antenna = new SKPaint { IsAntialias = true, StrokeWidth = s * 0.004f, Color = new SKColor(0x03, 0x06, 0x05) };
            c.DrawLine(w * 0.7f, roof + (h * 0.015f), w * 0.7f, roof - (s * 0.1f), antenna);
            c.DrawLine(w * 0.68f, roof - (s * 0.08f), w * 0.72f, roof - (s * 0.08f), antenna);
            c.DrawLine(w * 0.685f, roof - (s * 0.06f), w * 0.715f, roof - (s * 0.06f), antenna);
        }
    }

    /// <summary>
    /// A friendly sheet ghost: a soft, translucent body that trails off into a wisp, with a gentle glow.
    /// <paramref name="facing"/> is 1 or -1 (which way the tail streams); <paramref name="strength"/> fades far ones.
    /// </summary>
    private static void SpookyGhost(SKCanvas c, float x, float y, float r, float rotation, int facing, float strength)
    {
        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(rotation);
        c.Scale(facing, 1);
        using var body = new SKPath();
        body.MoveTo(-r, 0);
        body.ArcTo(new SKRect(-r, -r, r, r), 180, 180, false);
        body.CubicTo(r * 1.05f, r * 0.7f, r * 1.25f, r * 1.1f, r * 1.55f, r * 1.2f);   // right arm flaring out
        body.CubicTo(r * 1.1f, r * 1.35f, r * 0.85f, r * 1.4f, r * 0.7f, r * 1.7f);
        body.CubicTo(r * 0.5f, r * 2.2f, r * 0.0f, r * 2.6f, -r * 0.7f, r * 3.1f);     // the tail
        body.CubicTo(-r * 0.35f, r * 2.5f, -r * 0.5f, r * 2.1f, -r * 0.75f, r * 1.75f);
        body.CubicTo(-r * 1.0f, r * 1.5f, -r * 1.3f, r * 1.4f, -r * 1.5f, r * 1.2f);    // left arm
        body.CubicTo(-r * 1.2f, r * 1.0f, -r * 1.05f, r * 0.6f, -r, 0);
        body.Close();

        using (var glowBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.4f))
        using (var glow = new SKPaint { IsAntialias = true, MaskFilter = glowBlur, Color = new SKColor(0x70, 0xFF, 0x90, (byte)(110 * strength)) })
        {
            c.DrawPath(body, glow);
        }

        using (var softBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.05f))
        using (var sheet = new SKPaint
        {
            IsAntialias = true,
            MaskFilter = softBlur,
            Shader = SKShader.CreateLinearGradient(new SKPoint(0, -r), new SKPoint(0, r * 3.1f), new[] { new SKColor(0xF4, 0xFF, 0xF4, (byte)(235 * strength)), new SKColor(0xC8, 0xFF, 0xD8, (byte)(190 * strength)), new SKColor(0x90, 0xFF, 0xB0, 0) }, new[] { 0f, 0.45f, 1f }, SKShaderTileMode.Clamp)
        })
        {
            c.DrawPath(body, sheet);
        }

        // A face: dark hollow eyes and an open mouth, unflipped so it stays the same way round.
        c.Scale(facing, 1);
        using var faceBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.03f);
        using var hollow = new SKPaint { IsAntialias = true, MaskFilter = faceBlur, Color = new SKColor(0x0A, 0x28, 0x18, (byte)(220 * strength)) };
        c.DrawOval(new SKRect(-r * 0.42f, -r * 0.35f, -r * 0.14f, r * 0.08f), hollow);
        c.DrawOval(new SKRect(r * 0.14f, -r * 0.35f, r * 0.42f, r * 0.08f), hollow);
        c.DrawOval(new SKRect(-r * 0.14f, r * 0.25f, r * 0.14f, r * 0.6f), hollow);
        c.Restore();
    }

    /// <summary>A row of city blocks from <paramref name="x0"/> to <paramref name="x1"/> standing on <paramref name="ground"/>, some windows lit.</summary>
    private static void SpookySkyline(SKCanvas c, float x0, float x1, float ground, float minH, float maxH, float s, SKColor body, SKColor window, float lit, Random rng)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = body };
        using var glass = new SKPaint { IsAntialias = true };
        using var thin = new SKPaint { IsAntialias = true, StrokeWidth = s * 0.003f, Color = body };
        var x = x0;
        while (x < x1)
        {
            var bw = s * R(rng, 0.05f, 0.1f);
            var bh = R(rng, minH, maxH);
            var top = ground - bh;
            c.DrawRect(x, top, bw, bh + (s * 0.2f), paint);
            switch (rng.Next(4))
            {
                case 0:
                    c.DrawRect(x + (bw * 0.2f), top - (bh * 0.12f), bw * 0.6f, bh * 0.12f + 1, paint);
                    c.DrawRect(x + (bw * 0.35f), top - (bh * 0.2f), bw * 0.3f, bh * 0.08f + 1, paint);
                    break;
                case 1:
                    using (var spire = new SKPath())
                    {
                        spire.MoveTo(x + (bw * 0.3f), top);
                        spire.LineTo(x + (bw * 0.5f), top - (bh * 0.3f));
                        spire.LineTo(x + (bw * 0.7f), top);
                        spire.Close();
                        c.DrawPath(spire, paint);
                    }

                    break;
                case 2:
                    c.DrawLine(x + (bw * 0.7f), top, x + (bw * 0.7f), top - (bh * 0.25f), thin);
                    break;
            }

            var cell = s * 0.016f;
            for (var wy = top + (cell * 0.8f); wy < ground - cell; wy += cell * 1.4f)
            {
                for (var wx = x + (cell * 0.6f); wx < x + bw - cell; wx += cell * 1.2f)
                {
                    if (rng.NextDouble() < lit)
                    {
                        glass.Color = window.WithAlpha((byte)(window.Alpha * R(rng, 0.5f, 1f)));
                        c.DrawRect(wx, wy, cell * 0.55f, cell * 0.7f, glass);
                    }
                }
            }

            x += bw + (s * R(rng, 0f, 0.012f));
        }
    }

    /// <summary>A stepped Art Deco skyscraper whose crown glows from the storm above it.</summary>
    private static void SpookyDecoTower(SKCanvas c, float x, float ground, float top, float width, SKPoint storm)
    {
        var height = ground - top;
        using var tower = new SKPath();
        tower.AddRect(new SKRect(x - (width / 2), ground - (height * 0.72f), x + (width / 2), ground));
        tower.AddRect(new SKRect(x - (width * 0.38f), ground - (height * 0.84f), x + (width * 0.38f), ground - (height * 0.7f)));
        tower.AddRect(new SKRect(x - (width * 0.26f), ground - (height * 0.93f), x + (width * 0.26f), ground - (height * 0.82f)));
        tower.MoveTo(x - (width * 0.14f), ground - (height * 0.92f));
        tower.LineTo(x, top);
        tower.LineTo(x + (width * 0.14f), ground - (height * 0.92f));
        tower.Close();
        using var merged = tower.Simplify() ?? new SKPath(tower);

        SpookyGlow(c, x, ground - (height * 0.85f), width * 1.6f, new SKColor(0x80, 0xFF, 0x90, 110));
        c.Save();
        c.ClipPath(merged, antialias: true);
        using (var rim = new SKPaint { IsAntialias = true, Color = new SKColor(0x70, 0xE0, 0x90) })
        {
            c.DrawPath(merged, rim);
        }

        c.Translate(width * 0.04f, width * 0.03f);
        using (var body = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, top), new SKPoint(0, ground), new[] { new SKColor(0x1A, 0x3A, 0x2C), new SKColor(0x0A, 0x16, 0x12) }, SKShaderTileMode.Clamp) })
        {
            c.DrawPath(merged, body);
        }

        c.Restore();

        // Lit bands in the crown; dark vertical piers down the shaft.
        using var band = new SKPaint { IsAntialias = true, Color = new SKColor(0xB0, 0xFF, 0xB0, 200) };
        c.DrawRect(x - (width * 0.2f), ground - (height * 0.89f), width * 0.4f, height * 0.012f, band);
        c.DrawRect(x - (width * 0.32f), ground - (height * 0.79f), width * 0.64f, height * 0.012f, band);
        using var pier = new SKPaint { IsAntialias = true, StrokeWidth = width * 0.03f, Color = new SKColor(0x04, 0x0A, 0x08, 160) };
        for (var i = -2; i <= 2; i++)
        {
            c.DrawLine(x + (i * width * 0.18f), ground - (height * 0.7f), x + (i * width * 0.18f), ground, pier);
        }

        // A crackle of lightning from the storm down to the spire.
        using var boltBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, width * 0.03f);
        using var bolt = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = width * 0.03f, StrokeJoin = SKStrokeJoin.Miter, Color = new SKColor(0xE0, 0xFF, 0xE0, 230), MaskFilter = boltBlur };
        using var path = new SKPath();
        path.MoveTo(storm.X, storm.Y);
        var steps = 5;
        for (var i = 1; i <= steps; i++)
        {
            var t = i / (float)steps;
            var jitter = i == steps ? 0 : (i % 2 == 0 ? -1 : 1) * width * 0.25f;
            path.LineTo(storm.X + ((x - storm.X) * t) + jitter, storm.Y + ((top - storm.Y) * t));
        }

        c.DrawPath(path, bolt);
    }

    /// <summary>A rooftop water tower on stilts, in silhouette with a faint green rim.</summary>
    private static void SpookyWaterTower(SKCanvas c, float x, float roof, float size, SKColor colour)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = colour };
        using var leg = new SKPaint { IsAntialias = true, StrokeWidth = size * 0.025f, Color = colour };
        var tankBottom = roof - (size * 0.4f);
        foreach (var dx in new[] { -0.26f, -0.09f, 0.09f, 0.26f })
        {
            c.DrawLine(x + (dx * size), roof, x + (dx * size * 0.9f), tankBottom, leg);
        }

        leg.StrokeWidth = size * 0.012f;
        c.DrawLine(x - (size * 0.25f), roof - (size * 0.05f), x + (size * 0.24f), tankBottom + (size * 0.02f), leg);
        c.DrawLine(x + (size * 0.25f), roof - (size * 0.05f), x - (size * 0.24f), tankBottom + (size * 0.02f), leg);
        c.DrawLine(x - (size * 0.26f), roof - (size * 0.2f), x + (size * 0.26f), roof - (size * 0.2f), leg);

        using var tank = new SKPath();
        tank.AddRect(new SKRect(x - (size * 0.28f), roof - (size * 0.85f), x + (size * 0.28f), tankBottom));
        tank.MoveTo(x - (size * 0.31f), roof - (size * 0.85f));
        tank.LineTo(x, roof - (size * 1.08f));
        tank.LineTo(x + (size * 0.31f), roof - (size * 0.85f));
        tank.Close();
        c.DrawPath(tank, paint);
        c.DrawCircle(x, roof - (size * 1.1f), size * 0.02f, paint);

        using var rim = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = size * 0.008f, Color = new SKColor(0x70, 0xE0, 0x90, 110) };
        foreach (var fy in new[] { 0.5f, 0.65f, 0.8f })
        {
            c.DrawLine(x - (size * 0.28f), roof - (size * fy), x + (size * 0.28f), roof - (size * fy), rim);
        }

        using var edge = new SKPath();
        edge.MoveTo(x + (size * 0.31f), roof - (size * 0.85f));
        edge.LineTo(x, roof - (size * 1.08f));
        c.DrawPath(edge, rim);
    }

    // ---------- Back to Hogwarts: a castle on a cliff over a moonlit lake ----------

    private static void SpookyCastleLake(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var wide = w > h;

        SpookyBands(c, new SKRect(0, 0, w, h), new[]
        {
            new SKColor(0x03, 0x05, 0x18, 235), new SKColor(0x0A, 0x10, 0x34, 190), new SKColor(0x1E, 0x1E, 0x54, 145),
            new SKColor(0x4A, 0x36, 0x70, 150), new SKColor(0x06, 0x08, 0x1A, 245)
        }, new[] { 0f, 0.3f, 0.55f, 0.72f, 0.82f });

        // The Milky Way: a soft diagonal band dusted with fine stars.
        c.Save();
        c.Translate(w * 0.55f, h * 0.18f);
        c.RotateDegrees(-18);
        using (var bandBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.04f))
        using (var band = new SKPaint { IsAntialias = true, MaskFilter = bandBlur, Color = new SKColor(0xB8, 0xC0, 0xFF, 45) })
        {
            c.DrawOval(new SKRect(-w * 0.6f, -s * 0.05f, w * 0.6f, s * 0.05f), band);
        }

        using (var dust = new SKPaint { IsAntialias = true })
        {
            for (var i = 0; i < 160; i++)
            {
                dust.Color = SKColors.White.WithAlpha((byte)rng.Next(50, 170));
                var dx = R(rng, -w * 0.6f, w * 0.6f);
                var dy = (float)(rng.NextDouble() + rng.NextDouble() - 1) * s * 0.06f;
                c.DrawCircle(dx, dy, s * R(rng, 0.0008f, 0.002f), dust);
            }
        }

        c.Restore();
        Stars(c, w, h * 0.55f, wide ? 110 : 90, s, rng);
        for (var i = 0; i < 6; i++)
        {
            SpookyTwinkle(c, R(rng, w * 0.05f, w * 0.95f), R(rng, h * 0.03f, h * 0.3f), s * R(rng, 0.012f, 0.022f));
        }

        var moon = new SKPoint(w * (wide ? 0.17f : 0.2f), h * 0.17f);
        var mr = s * 0.07f;
        SpookyGlow(c, moon.X, moon.Y, mr * 5.5f, new SKColor(0xC8, 0xD4, 0xFF, 85));
        Moon(c, moon.X, moon.Y, mr, rng);
        SpookyOwl(c, moon.X + (mr * 0.25f), moon.Y + (mr * 0.1f), s * 0.06f, 0.6f, new SKColor(0x05, 0x06, 0x14));
        SpookyOwl(c, moon.X + (mr * 2.1f), moon.Y - (mr * 0.9f), s * 0.032f, -0.4f, new SKColor(0x08, 0x09, 0x1C));

        // Mountains across the lake, fading into mist.
        var shore = h * 0.81f;
        using (var peaks = SpookyRidge(w, h, h * 0.71f, h * 0.045f, wide ? 11 : 8, false, rng))
        {
            SpookyFillV(c, peaks, new SKColor(0x2E, 0x2C, 0x62, 235), new SKColor(0x1A, 0x1A, 0x44, 245));
        }

        SpookyFog(c, w, h * 0.755f, s * 0.025f, new SKColor(0xB0, 0xA8, 0xE0, 55), s, rng);
        using (var hills = SpookyRidge(w, h, h * 0.78f, h * 0.012f, 4, true, rng))
        {
            SpookyFillV(c, hills, new SKColor(0x16, 0x16, 0x38), new SKColor(0x0C, 0x0C, 0x24));
        }

        // The lake.
        SpookyBands(c, new SKRect(0, shore, w, h), new[] { new SKColor(0x14, 0x16, 0x40, 250), new SKColor(0x05, 0x06, 0x16, 252) }, new[] { 0f, 1f });

        // Castle on its cliff, then its reflection in the water below.
        var castleX = w * (wide ? 0.72f : 0.7f);
        var unit = s * (wide ? 0.0066f : 0.0063f);
        var seed = rng.Next();
        SpookyCastle(c, castleX, shore, unit, new Random(seed), false);
        c.Save();
        c.ClipRect(new SKRect(0, shore, w, h));
        using (var fade = new SKPaint { Color = SKColors.White.WithAlpha(95), ImageFilter = SKImageFilter.CreateBlur(unit * 0.3f, unit * 0.9f) })
        {
            c.SaveLayer(fade);
            c.Scale(1, -1, 0, shore);
            SpookyCastle(c, castleX, shore, unit, new Random(seed), true);
            c.Restore();
        }

        c.Restore();

        // Moonlight glittering in a path across the water, and ripples breaking the reflection.
        using (var glint = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = s * 0.003f })
        {
            for (var y = shore + (s * 0.008f); y < h; y += s * 0.012f)
            {
                var t = (y - shore) / (h - shore);
                for (var k = 0; k < 3; k++)
                {
                    var gx = moon.X + (R(rng, -1, 1) * s * (0.03f + (t * 0.08f)));
                    glint.Color = new SKColor(0xE8, 0xEE, 0xFF, (byte)(200 - (t * 120)));
                    c.DrawLine(gx, y, gx + (s * R(rng, 0.01f, 0.04f) * (0.5f + t)), y, glint);
                }

                glint.Color = new SKColor(0x04, 0x05, 0x14, 120);
                var rx = R(rng, 0, w);
                c.DrawLine(rx, y, rx + (s * R(rng, 0.05f, 0.15f)), y, glint);
            }
        }

        // A line of little boats with lanterns, crossing towards the castle.
        for (var i = 0; i < (wide ? 6 : 5); i++)
        {
            var t = i / (wide ? 5f : 4f);
            var bx = (w * (wide ? 0.2f : 0.22f)) + ((castleX - (s * 0.24f) - (w * (wide ? 0.2f : 0.22f))) * t) + R(rng, -s * 0.01f, s * 0.01f);
            var by = (h * 0.95f) - ((h * 0.95f - (shore + (s * 0.02f))) * t);
            SpookyBoat(c, bx, by, s * (0.058f - (t * 0.036f)));
        }

        // Pines and rocks on the near shore, bottom left.
        using (var bank = new SKPath())
        {
            bank.MoveTo(-2, h + 2);
            bank.LineTo(-2, h * 0.86f);
            bank.QuadTo(w * 0.12f, h * 0.87f, w * (wide ? 0.22f : 0.3f), h * 0.97f);
            bank.LineTo(w * (wide ? 0.26f : 0.34f), h + 2);
            bank.Close();
            SpookyFillV(c, bank, new SKColor(0x06, 0x08, 0x16), new SKColor(0x02, 0x03, 0x08));
        }

        var pine = new SKColor(0x03, 0x04, 0x0E);
        SpookyPine(c, w * 0.035f, h * 0.9f, s * 0.42f, pine, rng);
        SpookyPine(c, w * (wide ? 0.085f : 0.11f), h * 0.9f, s * 0.3f, pine, rng);
        SpookyPine(c, w * (wide ? 0.13f : 0.18f), h * 0.93f, s * 0.2f, pine, rng);
        SpookyPine(c, w * 0.97f, h * 1.01f, s * 0.3f, pine, rng);
    }

    /// <summary>
    /// A castle of many towers on a rocky cliff, the base of the cliff on <paramref name="water"/> at <paramref name="cx"/>.
    /// Drawn twice with the same seed for its reflection, which leaves out the moonlit rim.
    /// </summary>
    private static void SpookyCastle(SKCanvas c, float cx, float water, float u, Random rng, bool reflection)
    {
        // Cliff: a rough plateau with crags falling to the water.
        var plateau = water - (u * 14);
        using var cliff = new SKPath();
        cliff.MoveTo(cx - (u * 40), water + 1);
        cliff.LineTo(cx - (u * 36), water - (u * 4));
        cliff.LineTo(cx - (u * 33), water - (u * 5));
        cliff.LineTo(cx - (u * 30), plateau + (u * 3));
        cliff.LineTo(cx - (u * 26), plateau);
        for (var x = -24f; x < 30; x += 4)
        {
            cliff.LineTo(cx + (x * u), plateau + R(rng, -u * 0.6f, u * 0.6f));
        }

        cliff.LineTo(cx + (u * 32), plateau + (u * 2));
        cliff.LineTo(cx + (u * 35), water - (u * 6));
        cliff.LineTo(cx + (u * 40), water - (u * 2));
        cliff.LineTo(cx + (u * 44), water + 1);
        cliff.Close();

        // Towers: (centre, width, wall height, roof: 0 cone, 1 battlements, 2 tall spire).
        var towers = new (float X, float W, float H, int Roof)[]
        {
            (-23, 4, 12, 0), (-17, 5, 19, 0), (-11, 3.2f, 23, 2), (8, 5, 27, 2), (14, 4, 17, 0), (20, 6, 11, 1), (26, 3.4f, 14, 0)
        };
        using var shell = new SKPath();
        shell.AddPath(cliff);

        // The great hall between the towers, with a steep roof.
        shell.AddRect(new SKRect(cx - (u * 9), plateau - (u * 11), cx + (u * 6), plateau + 1));
        using (var hallRoof = new SKPath())
        {
            hallRoof.MoveTo(cx - (u * 9.5f), plateau - (u * 11));
            hallRoof.LineTo(cx - (u * 1.5f), plateau - (u * 17));
            hallRoof.LineTo(cx + (u * 6.5f), plateau - (u * 11));
            hallRoof.Close();
            shell.AddPath(hallRoof);
        }

        // Curtain walls with battlements linking everything.
        shell.AddRect(new SKRect(cx - (u * 25), plateau - (u * 6), cx + (u * 28), plateau + 1));
        for (var x = -25f; x < 28; x += 1.6f)
        {
            shell.AddRect(new SKRect(cx + (x * u), plateau - (u * 7), cx + ((x + 0.8f) * u), plateau - (u * 6)));
        }

        foreach (var (tx, tw, th, roof) in towers)
        {
            var left = cx + ((tx - (tw / 2)) * u);
            var right = cx + ((tx + (tw / 2)) * u);
            var top = plateau - (th * u);
            shell.AddRect(new SKRect(left, top, right, plateau + 1));
            switch (roof)
            {
                case 1:
                    shell.AddRect(new SKRect(left - (u * 0.4f), top - (u * 0.6f), right + (u * 0.4f), top + (u * 0.4f)));
                    for (var x = left - (u * 0.4f); x < right; x += u * 1.4f)
                    {
                        shell.AddRect(new SKRect(x, top - (u * 1.5f), x + (u * 0.75f), top));
                    }

                    break;
                default:
                {
                    var cone = roof == 2 ? tw * 2.6f : tw * 1.7f;
                    using var p = new SKPath();
                    p.MoveTo(left - (u * 0.5f), top);
                    p.QuadTo(cx + (tx * u) - (tw * u * 0.15f), top - (cone * u * 0.4f), cx + (tx * u), top - (cone * u));
                    p.QuadTo(cx + (tx * u) + (tw * u * 0.15f), top - (cone * u * 0.4f), right + (u * 0.5f), top);
                    p.Close();
                    shell.AddPath(p);
                    shell.AddRect(new SKRect(cx + (tx * u) - (u * 0.08f), top - (cone * u) - (u * 2.2f), cx + (tx * u) + (u * 0.08f), top - (cone * u)));
                    break;
                }
            }
        }

        // A bridge of arches out to a gatehouse on a rock to the left.
        using (var bridge = new SKPath())
        {
            bridge.AddRect(new SKRect(cx - (u * 48), plateau + (u * 3), cx - (u * 29), plateau + (u * 5)));
            for (var x = -47f; x < -30; x += 4)
            {
                bridge.AddRect(new SKRect(cx + (x * u), plateau + (u * 5), cx + ((x + 1) * u), water + 1));
            }

            shell.AddPath(bridge);
        }

        shell.AddRect(new SKRect(cx - (u * 52), plateau - (u * 2), cx - (u * 47), water + 1));
        using (var gateRoof = new SKPath())
        {
            gateRoof.MoveTo(cx - (u * 52.6f), plateau - (u * 2));
            gateRoof.LineTo(cx - (u * 49.5f), plateau - (u * 8));
            gateRoof.LineTo(cx - (u * 46.4f), plateau - (u * 2));
            gateRoof.Close();
            shell.AddPath(gateRoof);
        }

        using var solid = shell.Simplify() ?? new SKPath(shell);

        c.Save();
        c.ClipPath(solid, antialias: true);
        if (!reflection)
        {
            using var rim = new SKPaint { IsAntialias = true, Color = new SKColor(0x8C, 0x96, 0xD8) };
            c.DrawPath(solid, rim);
            c.Translate(u * 0.35f, u * 0.25f);
        }

        using (var body = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, plateau - (u * 30)), new SKPoint(0, water), new[] { new SKColor(0x22, 0x26, 0x50), new SKColor(0x10, 0x12, 0x2C), new SKColor(0x06, 0x07, 0x16) }, new[] { 0f, 0.6f, 1f }, SKShaderTileMode.Clamp) })
        {
            c.DrawPath(solid, body);
        }

        c.Restore();

        // Lit windows: rows up each tower, tall arched windows along the great hall.
        using var warm = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xD0, 0x70) };
        using var glowBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, u * 0.8f);
        using var glow = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xA0, 0x30, 150), MaskFilter = glowBlur };
        void Window(float wx, float wy, float ww, float wh)
        {
            var r = new SKRect(wx - (ww / 2), wy - (wh / 2), wx + (ww / 2), wy + (wh / 2));
            c.DrawRect(r, glow);
            c.DrawRoundRect(r, ww / 2, ww / 2, warm);
        }

        for (var i = 0; i < 4; i++)
        {
            Window(cx + ((-7 + (i * 3.6f)) * u), plateau - (u * 6.5f), u * 1.3f, u * 4.2f);
        }

        foreach (var (tx, tw, th, _) in towers)
        {
            for (var y = plateau - (u * 3); y > plateau - (th * u) + (u * 2); y -= u * 3.2f)
            {
                if (rng.NextDouble() < 0.55)
                {
                    Window(cx + (tx * u) + (tw > 4.5f ? R(rng, -1, 1) * u : 0), y, u * 0.8f, u * 1.5f);
                }
            }
        }

        Window(cx - (u * 49.5f), plateau + (u * 1), u * 0.9f, u * 1.6f);
        for (var x = -22f; x < 26; x += 3)
        {
            if (rng.NextDouble() < 0.35)
            {
                Window(cx + (x * u), plateau - (u * 2.5f), u * 0.6f, u * 1f);
            }
        }
    }

    /// <summary>A small rowing boat with a lantern on a pole, and its light on the water.</summary>
    private static void SpookyBoat(SKCanvas c, float x, float y, float size)
    {
        SpookyGlow(c, x, y, size * 1.4f, new SKColor(0xFF, 0xB0, 0x50, 80), 0.35f);
        using (var streak = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, y), new SKPoint(0, y + (size * 0.9f)), new[] { new SKColor(0xFF, 0xC0, 0x60, 140), new SKColor(0xFF, 0xC0, 0x60, 0) }, SKShaderTileMode.Clamp) })
        {
            c.DrawRect(x + (size * 0.25f), y, size * 0.08f, size * 0.9f, streak);
        }

        using var hull = new SKPaint { IsAntialias = true, Color = new SKColor(0x04, 0x04, 0x0C) };
        using var p = new SKPath();
        p.MoveTo(x - (size * 0.55f), y - (size * 0.12f));
        p.LineTo(x + (size * 0.55f), y - (size * 0.12f));
        p.QuadTo(x + (size * 0.45f), y + (size * 0.08f), x + (size * 0.3f), y + (size * 0.08f));
        p.LineTo(x - (size * 0.35f), y + (size * 0.08f));
        p.QuadTo(x - (size * 0.5f), y + (size * 0.05f), x - (size * 0.55f), y - (size * 0.12f));
        p.Close();
        c.DrawPath(p, hull);
        c.DrawOval(new SKRect(x - (size * 0.18f), y - (size * 0.4f), x + (size * 0.02f), y - (size * 0.1f)), hull);
        using var pole = new SKPaint { IsAntialias = true, StrokeWidth = size * 0.03f, Color = hull.Color };
        c.DrawLine(x + (size * 0.3f), y - (size * 0.12f), x + (size * 0.3f), y - (size * 0.6f), pole);
        SpookyGlow(c, x + (size * 0.3f), y - (size * 0.6f), size * 0.45f, new SKColor(0xFF, 0xC0, 0x50, 200));
        using var flame = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xF0, 0xC0) };
        c.DrawCircle(x + (size * 0.3f), y - (size * 0.6f), size * 0.07f, flame);
    }

    /// <summary>An owl in flight, wings spread; <paramref name="flap"/> lifts (+) or lowers (-) the wingtips.</summary>
    private static void SpookyOwl(SKCanvas c, float x, float y, float r, float flap, SKColor colour)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = colour };
        using var owl = new SKPath();

        // Stout body and a big round head with ear tufts.
        owl.AddOval(new SKRect(x - (r * 0.17f), y - (r * 0.28f), x + (r * 0.17f), y + (r * 0.36f)));
        owl.AddCircle(x, y - (r * 0.33f), r * 0.16f);
        foreach (var side in new[] { -1f, 1f })
        {
            owl.MoveTo(x + (side * r * 0.05f), y - (r * 0.44f));
            owl.LineTo(x + (side * r * 0.15f), y - (r * 0.56f));
            owl.LineTo(x + (side * r * 0.15f), y - (r * 0.38f));
            owl.Close();
        }

        // Broad rounded wings with the long flight feathers fanned out only at the tips.
        var lift = flap * r * 0.35f;
        foreach (var side in new[] { -1f, 1f })
        {
            owl.MoveTo(x + (side * r * 0.1f), y - (r * 0.22f));
            owl.CubicTo(x + (side * r * 0.35f), y - (r * 0.45f) - (lift * 0.5f), x + (side * r * 0.7f), y - (r * 0.42f) - lift, x + (side * r * 0.92f), y - (r * 0.3f) - lift);
            for (var k = 0; k < 5; k++)
            {
                var tipX = x + (side * r * (1.05f - (k * 0.07f)));
                var tipY = y - (r * 0.26f) - (lift * (1 - (k * 0.12f))) + (k * r * 0.085f);
                owl.LineTo(tipX, tipY);
                owl.LineTo(tipX - (side * r * 0.1f), tipY + (r * 0.035f));
            }

            owl.QuadTo(x + (side * r * 0.45f), y + (r * 0.14f) - (lift * 0.2f), x + (side * r * 0.12f), y + (r * 0.1f));
            owl.Close();
        }

        owl.MoveTo(x - (r * 0.1f), y + (r * 0.28f));
        owl.LineTo(x - (r * 0.17f), y + (r * 0.52f));
        owl.QuadTo(x, y + (r * 0.58f), x + (r * 0.17f), y + (r * 0.52f));
        owl.LineTo(x + (r * 0.1f), y + (r * 0.28f));
        owl.Close();
        c.DrawPath(owl, paint);
    }

    /// <summary>A dark pine tree: jagged tiers of branches down a thin trunk.</summary>
    private static void SpookyPine(SKCanvas c, float x, float ground, float height, SKColor colour, Random rng)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = colour };
        c.DrawRect(x - (height * 0.02f), ground - (height * 0.2f), height * 0.04f, height * 0.2f + 2, paint);
        using var tree = new SKPath();
        tree.MoveTo(x, ground - height);
        const int Tiers = 8;
        var right = new List<SKPoint>();
        var left = new List<SKPoint>();
        for (var i = 1; i <= Tiers; i++)
        {
            var t = i / (float)Tiers;
            var y = ground - height + (height * 0.85f * t);
            var reach = height * 0.24f * t * R(rng, 0.8f, 1.15f);
            right.Add(new SKPoint(x + reach, y));
            right.Add(new SKPoint(x + (reach * 0.4f), y - (height * 0.02f)));
            reach = height * 0.24f * t * R(rng, 0.8f, 1.15f);
            left.Add(new SKPoint(x - reach, y));
            left.Add(new SKPoint(x - (reach * 0.4f), y - (height * 0.02f)));
        }

        foreach (var p in right)
        {
            tree.LineTo(p);
        }

        tree.LineTo(x, ground - (height * 0.12f));
        for (var i = left.Count - 1; i >= 0; i--)
        {
            tree.LineTo(left[i]);
        }

        tree.Close();
        c.DrawPath(tree, paint);
    }

    /// <summary>A bright star with a soft four-point glint.</summary>
    private static void SpookyTwinkle(SKCanvas c, float x, float y, float r)
    {
        SpookyGlow(c, x, y, r * 0.8f, new SKColor(0xD0, 0xE0, 0xFF, 160));
        using var ray = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = r * 0.08f, Color = new SKColor(0xF0, 0xF4, 0xFF, 200) };
        c.DrawLine(x - r, y, x + r, y, ray);
        c.DrawLine(x, y - r, x, y + r, ray);
        using var core = new SKPaint { IsAntialias = true, Color = SKColors.White };
        c.DrawCircle(x, y, r * 0.12f, core);
    }

    // ---------- shared pieces ----------

    /// <summary>A vertical gradient over <paramref name="r"/>.</summary>
    private static void SpookyBands(SKCanvas c, SKRect r, SKColor[] colours, float[] stops)
    {
        using var paint = new SKPaint { Shader = SKShader.CreateLinearGradient(new SKPoint(0, r.Top), new SKPoint(0, r.Bottom), colours, stops, SKShaderTileMode.Clamp) };
        c.DrawRect(r, paint);
    }

    /// <summary>A path filled top to bottom with a gradient across its own bounds.</summary>
    private static void SpookyFillV(SKCanvas c, SKPath p, SKColor top, SKColor bottom)
    {
        var b = p.Bounds;
        using var paint = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, b.Top), new SKPoint(0, b.Bottom), new[] { top, bottom }, SKShaderTileMode.Clamp) };
        c.DrawPath(p, paint);
    }

    /// <summary>Soft round light fading to nothing, squashed vertically by <paramref name="squash"/>.</summary>
    private static void SpookyGlow(SKCanvas c, float x, float y, float r, SKColor colour, float squash = 1f)
    {
        c.Save();
        c.Translate(x, y);
        c.Scale(1, squash);
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(SKPoint.Empty, r, new[] { colour, colour.WithAlpha((byte)(colour.Alpha * 0.35f)), colour.WithAlpha(0) }, new[] { 0f, 0.35f, 1f }, SKShaderTileMode.Clamp)
        };
        c.DrawCircle(0, 0, r, paint);
        c.Restore();
    }

    private static SKColor SpookyMix(SKColor a, SKColor b, float t)
    {
        t = Math.Clamp(t, 0, 1);
        return new SKColor(
            (byte)(a.Red + ((b.Red - a.Red) * t)),
            (byte)(a.Green + ((b.Green - a.Green) * t)),
            (byte)(a.Blue + ((b.Blue - a.Blue) * t)),
            (byte)(a.Alpha + ((b.Alpha - a.Alpha) * t)));
    }

    /// <summary>A line of hills or rooftops from <paramref name="top"/> (give or take <paramref name="amp"/>) down to the bottom edge.</summary>
    private static SKPath SpookyRidge(float w, float h, float top, float amp, int bumps, bool smooth, Random rng)
    {
        var p = new SKPath();
        p.MoveTo(-2, h + 2);
        var y = top + R(rng, -amp, amp);
        p.LineTo(-2, y);
        var step = (w + 4) / bumps;
        for (var i = 1; i <= bumps; i++)
        {
            var x = -2 + (i * step);
            var ny = top + R(rng, -amp, amp);
            if (smooth)
            {
                p.QuadTo(x - (step / 2), Math.Min(y, ny) - (amp * R(rng, 0.3f, 1.1f)), x, ny);
            }
            else
            {
                p.LineTo(x, ny);
            }

            y = ny;
        }

        p.LineTo(w + 2, h + 2);
        p.Close();
        return p;
    }

    /// <summary>A band of drifting fog centred on <paramref name="y"/>.</summary>
    private static void SpookyFog(SKCanvas c, int w, float y, float thick, SKColor colour, float s, Random rng)
    {
        SpookyBands(c, new SKRect(0, y - (thick * 1.6f), w, y + (thick * 1.6f)), new[] { colour.WithAlpha(0), colour.WithAlpha((byte)(colour.Alpha * 0.7f)), colour.WithAlpha(0) }, new[] { 0f, 0.5f, 1f });
        using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, thick * 0.6f);
        using var puff = new SKPaint { IsAntialias = true, MaskFilter = blur };
        for (var x = -s * 0.1f; x < w + (s * 0.1f); x += s * R(rng, 0.12f, 0.2f))
        {
            puff.Color = colour.WithAlpha((byte)(colour.Alpha * R(rng, 0.5f, 1f)));
            var pw = s * R(rng, 0.12f, 0.24f);
            var ph = thick * R(rng, 0.5f, 0.9f);
            var py = y + R(rng, -thick * 0.5f, thick * 0.5f);
            c.DrawOval(new SKRect(x - pw, py - ph, x + pw, py + ph), puff);
        }
    }

    /// <summary>Thin dark clouds drifting across the sky at about <paramref name="y"/>.</summary>
    private static void SpookyCloudWisps(SKCanvas c, int w, float y, float s, SKColor colour, Random rng, int count)
    {
        using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.012f);
        using var paint = new SKPaint { IsAntialias = true, MaskFilter = blur };
        for (var i = 0; i < count; i++)
        {
            var cx = w * R(rng, 0.45f, 1f);
            var cy = y + R(rng, -s * 0.06f, s * 0.06f);
            var cw = s * R(rng, 0.15f, 0.3f);
            var ch = s * R(rng, 0.008f, 0.016f);
            paint.Color = colour.WithAlpha((byte)(colour.Alpha * R(rng, 0.6f, 1f)));
            c.DrawOval(new SKRect(cx - cw, cy - ch, cx + cw, cy + ch), paint);
        }
    }

    /// <summary>
    /// A gnarled bare tree: a flared root, then a trunk that twists and forks into ever thinner branches.
    /// <paramref name="bias"/> leans the crown (degrees, positive to the right). Branches bend away from
    /// <paramref name="clear"/> (the title) and stop short rather than grow into it.
    /// </summary>
    private static void SpookyTree(SKCanvas c, float x, float ground, float height, float bias, SKColor colour, SKRect clear, float trunk, Random rng)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = colour };
        var baseW = height * 0.075f;
        using (var roots = new SKPath())
        {
            roots.MoveTo(x - (baseW * 2f), ground + 2);
            roots.QuadTo(x - (baseW * 0.55f), ground - (baseW * 0.2f), x - (baseW * 0.5f), ground - (baseW * 2f));
            roots.LineTo(x + (baseW * 0.5f), ground - (baseW * 2f));
            roots.QuadTo(x + (baseW * 0.6f), ground - (baseW * 0.2f), x + (baseW * 2.2f), ground + 2);
            roots.Close();
            c.DrawPath(roots, paint);
        }

        SpookyBranch(c, paint, x, ground, -90 + (bias * 0.4f), height * trunk, baseW, 6, bias, clear, rng);
    }

    private static void SpookyBranch(SKCanvas c, SKPaint paint, float x, float y, float angle, float len, float width, int depth, float bias, SKRect clear, Random rng)
    {
        const int Segments = 3;
        var endW = width * 0.66f;
        for (var i = 0; i < Segments; i++)
        {
            // The trunk only sways a little, so the tree stands where it's put; the branches twist more.
            var sway = depth >= 6 ? 7f : 20f;
            angle = Math.Clamp(angle + R(rng, -sway, sway) + (bias * 0.06f), -168f, -12f);
            var sl = len / Segments;
            float nx = 0, ny = 0;
            var blocked = true;
            for (var turn = 0; turn < 4 && blocked; turn++)
            {
                var a = angle * MathF.PI / 180f;
                nx = x + (MathF.Cos(a) * sl);
                ny = y + (MathF.Sin(a) * sl);
                blocked = clear.Contains(nx, ny);
                if (blocked)
                {
                    // Bend upwards to climb past it; once above it the branch is free to spread again.
                    angle += angle > -90 ? -22 : 22;
                }
            }

            if (blocked)
            {
                return;
            }

            var rad = angle * MathF.PI / 180f;
            var w0 = width + ((endW - width) * i / Segments);
            var w1 = width + ((endW - width) * (i + 1) / Segments);
            float px = -MathF.Sin(rad), py = MathF.Cos(rad);
            using (var seg = new SKPath())
            {
                seg.MoveTo(x + (px * w0 / 2), y + (py * w0 / 2));
                seg.LineTo(nx + (px * w1 / 2), ny + (py * w1 / 2));
                seg.LineTo(nx - (px * w1 / 2), ny - (py * w1 / 2));
                seg.LineTo(x - (px * w0 / 2), y - (py * w0 / 2));
                seg.Close();
                c.DrawPath(seg, paint);
            }

            c.DrawCircle(nx, ny, w1 / 2, paint);
            if (depth is >= 2 and <= 4 && i > 0 && rng.NextDouble() < 0.45)
            {
                SpookyBranch(c, paint, nx, ny, angle + ((rng.Next(2) == 0 ? -1 : 1) * R(rng, 35, 60)), len * 0.4f, w1 * 0.5f, 1, bias, clear, rng);
            }

            x = nx;
            y = ny;
        }

        if (depth <= 0 || endW < 1.2f)
        {
            // Taper off to a sharp twig.
            var rad = angle * MathF.PI / 180f;
            float px = -MathF.Sin(rad), py = MathF.Cos(rad);
            var reach = clear.Contains(x + (MathF.Cos(rad) * len * 0.35f), y + (MathF.Sin(rad) * len * 0.35f)) ? len * 0.12f : len * 0.35f;
            using var tip = new SKPath();
            tip.MoveTo(x + (px * endW / 2), y + (py * endW / 2));
            tip.LineTo(x + (MathF.Cos(rad) * reach), y + (MathF.Sin(rad) * reach));
            tip.LineTo(x - (px * endW / 2), y - (py * endW / 2));
            tip.Close();
            c.DrawPath(tip, paint);
            return;
        }

        var kids = rng.NextDouble() < 0.65 ? 2 : 3;
        var spread = R(rng, 45, 75);
        for (var k = 0; k < kids; k++)
        {
            var a = angle - (spread / 2) + (spread * k / (kids - 1)) + R(rng, -8, 8);
            SpookyBranch(c, paint, x, y, a, len * R(rng, 0.58f, 0.72f), endW * R(rng, 0.62f, 0.78f), depth - 1, bias, clear, rng);
        }
    }

    /// <summary>Where the label, title and accent line sit, which scenery keeps out of.</summary>
    private static SKRect SpookyTitleZone(int w, int h) => new(w * 0.12f, h * 0.31f, w * 0.88f, h * 0.67f);

    /// <summary>A bat in silhouette. <paramref name="flap"/> runs from wings down (-1) to wings raised (1).</summary>
    private static void SpookyBat(SKCanvas c, float x, float y, float r, float rotation, float flap, SKColor colour)
    {
        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(rotation);
        using var paint = new SKPaint { IsAntialias = true, Color = colour };
        using var bat = new SKPath();
        var lift = flap * r * 0.45f;
        foreach (var side in new[] { -1f, 1f })
        {
            bat.MoveTo(side * r * 0.08f, -r * 0.05f);
            bat.QuadTo(side * r * 0.35f, (-r * 0.2f) - lift, side * r * 0.55f, (-r * 0.25f) - lift);
            bat.QuadTo(side * r * 0.8f, (-r * 0.22f) - (lift * 1.2f), side * r, (-r * 0.05f) - (lift * 1.1f));
            bat.QuadTo(side * r * 0.82f, (-r * 0.02f) - (lift * 0.6f), side * r * 0.72f, (r * 0.1f) - (lift * 0.7f));
            bat.QuadTo(side * r * 0.56f, (r * 0.0f) - (lift * 0.4f), side * r * 0.42f, (r * 0.12f) - (lift * 0.4f));
            bat.QuadTo(side * r * 0.28f, (r * 0.02f) - (lift * 0.2f), side * r * 0.08f, r * 0.12f);
            bat.Close();
        }

        c.DrawPath(bat, paint);
        c.DrawOval(new SKRect(-r * 0.09f, -r * 0.12f, r * 0.09f, r * 0.18f), paint);
        using var ears = new SKPath();
        ears.MoveTo(-r * 0.08f, -r * 0.08f);
        ears.LineTo(-r * 0.07f, -r * 0.2f);
        ears.LineTo(0, -r * 0.11f);
        ears.LineTo(r * 0.07f, -r * 0.2f);
        ears.LineTo(r * 0.08f, -r * 0.08f);
        ears.Close();
        c.DrawPath(ears, paint);
        c.Restore();
    }

    /// <summary>
    /// A pumpkin sitting on <paramref name="bottom"/>: ribbed lobes shaded from the upper left, a curled stem, and
    /// (for <paramref name="face"/> 0 to 3) a carved face lit from inside. <paramref name="light"/> below 1 sinks it
    /// into the dusk for pumpkins further back.
    /// </summary>
    private static void SpookyPumpkin(SKCanvas c, float cx, float bottom, float r, float squash, int face, float light, Random rng)
    {
        var ph = r * 2 * squash;
        var top = bottom - ph;
        var cy = bottom - (ph / 2);
        var dusk = new SKColor(0x30, 0x12, 0x20);

        if (face >= 0)
        {
            SpookyGlow(c, cx, bottom, r * 2.6f, new SKColor(0xFF, 0x8A, 0x1E, 120), 0.35f);
        }

        // Shadow on the ground.
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.08f))
        using (var shadow = new SKPaint { IsAntialias = true, Color = SKColors.Black.WithAlpha(150), MaskFilter = blur })
        {
            c.DrawOval(new SKRect(cx - (r * 1.05f), bottom - (r * 0.1f), cx + (r * 1.05f), bottom + (r * 0.1f)), shadow);
        }

        // Lobes from the outside in, each a little lighter, so the ribs read as soft grooves.
        using var outline = new SKPath();
        using var grooveBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.025f);
        foreach (var lobe in new[] { -2, 2, -1, 1, 0 })
        {
            var a = Math.Abs(lobe);
            var lx = cx + (lobe * r * 0.3f);
            var hw = r * (0.56f - (a * 0.05f));
            var lh = ph * (1f - (a * 0.06f));
            var rect = new SKRect(lx - hw, cy - (lh / 2), lx + hw, cy + (lh / 2));
            outline.AddOval(rect);
            var bright = (face >= 0 ? 1f : 0.8f) * light * (1f - (a * 0.1f));
            var hi = SpookyMix(dusk, new SKColor(0xFF, 0xA0, 0x40), bright);
            var mid = SpookyMix(dusk, new SKColor(0xE0, 0x62, 0x10), bright);
            var lo = SpookyMix(dusk, new SKColor(0x8A, 0x2C, 0x08), bright);
            using var body = new SKPaint
            {
                IsAntialias = true,
                Shader = SKShader.CreateRadialGradient(new SKPoint(lx - (hw * 0.25f), cy - (lh * 0.25f)), hw * 1.5f, new[] { hi, mid, lo }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp)
            };
            c.DrawOval(rect, body);
            if (a < 2)
            {
                using var groove = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = r * 0.04f, MaskFilter = grooveBlur, Color = SpookyMix(dusk, new SKColor(0x5A, 0x18, 0x04), light).WithAlpha(150) };
                c.DrawOval(rect, groove);
            }
        }

        // Round the whole thing off: darker towards the base and the far side, a soft sheen up top.
        c.Save();
        c.ClipPath(outline, antialias: true);
        using (var shade = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(new SKPoint(cx - (r * 0.2f), cy - (ph * 0.25f)), r * 1.35f, new[] { SKColors.Transparent, SKColors.Transparent, new SKColor(0x20, 0x04, 0x10, 170) }, new[] { 0f, 0.55f, 1f }, SKShaderTileMode.Clamp)
        })
        {
            c.DrawRect(cx - (r * 1.2f), top - 2, r * 2.4f, ph + 4, shade);
        }

        using (var sheenBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.06f))
        using (var sheen = new SKPaint { IsAntialias = true, MaskFilter = sheenBlur, Color = new SKColor(0xFF, 0xE0, 0xB0, (byte)(70 * light)) })
        {
            c.DrawOval(new SKRect(cx - (r * 0.55f), top + (ph * 0.06f), cx - (r * 0.1f), top + (ph * 0.2f)), sheen);
        }

        c.Restore();

        // Stem with a curling tendril.
        var stemTop = top - (r * 0.32f);
        var lean = R(rng, -0.15f, 0.15f) * r;
        using (var stemPath = new SKPath())
        {
            stemPath.MoveTo(cx - (r * 0.11f), top + (r * 0.08f));
            stemPath.QuadTo(cx - (r * 0.08f), top - (r * 0.15f), cx + lean - (r * 0.06f), stemTop);
            stemPath.LineTo(cx + lean + (r * 0.08f), stemTop - (r * 0.02f));
            stemPath.QuadTo(cx + (r * 0.06f), top - (r * 0.12f), cx + (r * 0.12f), top + (r * 0.08f));
            stemPath.Close();
            using var stem = new SKPaint
            {
                IsAntialias = true,
                Shader = SKShader.CreateLinearGradient(new SKPoint(cx - (r * 0.1f), 0), new SKPoint(cx + (r * 0.1f), 0), new[] { SpookyMix(dusk, new SKColor(0x7A, 0x8A, 0x3A), light), SpookyMix(dusk, new SKColor(0x34, 0x40, 0x14), light) }, SKShaderTileMode.Clamp)
            };
            c.DrawPath(stemPath, stem);
        }

        using (var curl = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = r * 0.03f, Color = SpookyMix(dusk, new SKColor(0x4A, 0x5A, 0x20), light) })
        using (var tendril = new SKPath())
        {
            var side = rng.Next(2) == 0 ? -1 : 1;
            tendril.MoveTo(cx, top - (r * 0.1f));
            tendril.CubicTo(cx + (side * r * 0.3f), top - (r * 0.35f), cx + (side * r * 0.55f), top - (r * 0.05f), cx + (side * r * 0.4f), top - (r * 0.2f));
            tendril.CubicTo(cx + (side * r * 0.3f), top - (r * 0.3f), cx + (side * r * 0.5f), top - (r * 0.38f), cx + (side * r * 0.56f), top - (r * 0.26f));
            c.DrawPath(tendril, curl);
        }

        if (face >= 0)
        {
            SpookyFace(c, cx, cy, r, ph, face);
        }
    }

    /// <summary>A carved face glowing with candlelight.</summary>
    private static void SpookyFace(SKCanvas c, float cx, float cy, float r, float ph, int style)
    {
        var fy = ph / 2;
        using var face = new SKPath();
        switch (style)
        {
            case 1:
                // Scowling slanted eyes and a wide many-toothed grin.
                foreach (var side in new[] { -1f, 1f })
                {
                    face.MoveTo(cx + (side * r * 0.12f), cy - (fy * 0.12f));
                    face.LineTo(cx + (side * r * 0.55f), cy - (fy * 0.38f));
                    face.LineTo(cx + (side * r * 0.45f), cy - (fy * 0.02f));
                    face.Close();
                }

                face.MoveTo(cx - (r * 0.62f), cy + (fy * 0.15f));
                for (var i = 1; i <= 8; i++)
                {
                    var t = i / 8f;
                    var x = cx - (r * 0.62f) + (t * r * 1.24f);
                    var up = (i % 2 == 0) ? 0.15f : 0.32f;
                    face.LineTo(x, cy + (fy * up));
                }

                face.QuadTo(cx, cy + (fy * 0.95f), cx - (r * 0.62f), cy + (fy * 0.15f));
                face.Close();
                break;
            case 2:
                // Happy crescent eyes, a little round nose, a crooked smile with one tooth.
                foreach (var side in new[] { -1f, 1f })
                {
                    var ex = cx + (side * r * 0.32f);
                    face.MoveTo(ex - (r * 0.2f), cy - (fy * 0.05f));
                    face.QuadTo(ex, cy - (fy * 0.6f), ex + (r * 0.2f), cy - (fy * 0.05f));
                    face.QuadTo(ex, cy - (fy * 0.3f), ex - (r * 0.2f), cy - (fy * 0.05f));
                    face.Close();
                }

                face.AddCircle(cx, cy + (fy * 0.08f), r * 0.07f);
                face.MoveTo(cx - (r * 0.5f), cy + (fy * 0.25f));
                face.QuadTo(cx, cy + (fy * 0.55f), cx + (r * 0.5f), cy + (fy * 0.22f));
                face.QuadTo(cx + (r * 0.3f), cy + (fy * 0.75f), cx + (r * 0.05f), cy + (fy * 0.72f));
                face.LineTo(cx + (r * 0.02f), cy + (fy * 0.52f));
                face.LineTo(cx - (r * 0.12f), cy + (fy * 0.52f));
                face.LineTo(cx - (r * 0.12f), cy + (fy * 0.7f));
                face.QuadTo(cx - (r * 0.38f), cy + (fy * 0.68f), cx - (r * 0.5f), cy + (fy * 0.25f));
                face.Close();
                break;
            case 3:
                // Startled round eyes and an open mouth.
                face.AddOval(new SKRect(cx - (r * 0.48f), cy - (fy * 0.45f), cx - (r * 0.18f), cy - (fy * 0.02f)));
                face.AddOval(new SKRect(cx + (r * 0.18f), cy - (fy * 0.45f), cx + (r * 0.48f), cy - (fy * 0.02f)));
                face.AddOval(new SKRect(cx - (r * 0.15f), cy + (fy * 0.2f), cx + (r * 0.15f), cy + (fy * 0.62f)));
                break;
            default:
                // The classic: triangle eyes and nose, a jagged grin.
                foreach (var side in new[] { -1f, 1f })
                {
                    var ex = cx + (side * r * 0.33f);
                    face.MoveTo(ex, cy - (fy * 0.42f));
                    face.LineTo(ex + (r * 0.17f), cy - (fy * 0.04f));
                    face.LineTo(ex - (r * 0.17f), cy - (fy * 0.04f));
                    face.Close();
                }

                face.MoveTo(cx, cy - (fy * 0.02f));
                face.LineTo(cx + (r * 0.08f), cy + (fy * 0.16f));
                face.LineTo(cx - (r * 0.08f), cy + (fy * 0.16f));
                face.Close();
                face.MoveTo(cx - (r * 0.6f), cy + (fy * 0.2f));
                face.LineTo(cx - (r * 0.35f), cy + (fy * 0.32f));
                face.LineTo(cx - (r * 0.3f), cy + (fy * 0.22f));
                face.LineTo(cx - (r * 0.2f), cy + (fy * 0.34f));
                face.LineTo(cx + (r * 0.15f), cy + (fy * 0.34f));
                face.LineTo(cx + (r * 0.22f), cy + (fy * 0.22f));
                face.LineTo(cx + (r * 0.3f), cy + (fy * 0.33f));
                face.LineTo(cx + (r * 0.6f), cy + (fy * 0.2f));
                face.QuadTo(cx + (r * 0.4f), cy + (fy * 0.8f), cx + (r * 0.05f), cy + (fy * 0.78f));
                face.LineTo(cx + (r * 0.05f), cy + (fy * 0.62f));
                face.LineTo(cx - (r * 0.08f), cy + (fy * 0.62f));
                face.LineTo(cx - (r * 0.1f), cy + (fy * 0.76f));
                face.QuadTo(cx - (r * 0.45f), cy + (fy * 0.75f), cx - (r * 0.6f), cy + (fy * 0.2f));
                face.Close();
                break;
        }

        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.09f))
        using (var glow = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0x9A, 0x20, 220), MaskFilter = blur })
        {
            c.DrawPath(face, glow);
        }

        // The cut wall: the hole shows the lit inside, deeper orange at the edges.
        using (var wall = new SKPaint { IsAntialias = true, Color = new SKColor(0xC8, 0x4A, 0x08) })
        {
            c.DrawPath(face, wall);
        }

        c.Save();
        c.Translate(r * 0.02f, r * 0.03f);
        using (var lit = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(new SKPoint(cx, cy + (fy * 0.1f)), r * 0.8f, new[] { new SKColor(0xFF, 0xFA, 0xD0), new SKColor(0xFF, 0xC8, 0x3A), new SKColor(0xFF, 0x8A, 0x10) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp)
        })
        {
            c.ClipPath(face, antialias: true);
            c.Translate(-r * 0.04f, -r * 0.05f);
            c.DrawPath(face, lit);
        }

        c.Restore();
    }

    /// <summary>A pumpkin vine trailing along the ground with leaves and curling tendrils.</summary>
    private static void SpookyVine(SKCanvas c, float x0, float y0, float x1, float y1, float s, Random rng)
    {
        var leaf = new SKColor(0x16, 0x1C, 0x0C);
        using var stroke = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = s * 0.006f, Color = leaf };
        using var vine = new SKPath();
        vine.MoveTo(x0, y0);
        var steps = 6;
        var pts = new List<SKPoint>();
        for (var i = 1; i <= steps; i++)
        {
            var t = i / (float)steps;
            var x = x0 + ((x1 - x0) * t);
            var y = y0 + ((y1 - y0) * t) + R(rng, -s * 0.02f, s * 0.02f);
            vine.QuadTo(x - ((x1 - x0) / steps / 2), y + (i % 2 == 0 ? s * 0.03f : -s * 0.03f), x, y);
            pts.Add(new SKPoint(x, y));
        }

        c.DrawPath(vine, stroke);
        using var fill = new SKPaint { IsAntialias = true, Color = leaf };
        foreach (var p in pts)
        {
            var lr = s * R(rng, 0.022f, 0.035f);
            c.Save();
            c.Translate(p.X, p.Y);
            c.RotateDegrees(R(rng, -150, -30));
            using var shape = new SKPath();
            shape.MoveTo(0, 0);
            shape.CubicTo(lr * 0.5f, -lr * 0.9f, lr * 1.6f, -lr * 0.7f, lr * 2f, 0);
            shape.CubicTo(lr * 1.6f, lr * 0.7f, lr * 0.5f, lr * 0.9f, 0, 0);
            c.DrawPath(shape, fill);
            c.Restore();

            using var curl = new SKPath();
            var dir = rng.Next(2) == 0 ? -1 : 1;
            curl.MoveTo(p.X, p.Y);
            curl.CubicTo(p.X + (dir * s * 0.02f), p.Y - (s * 0.03f), p.X + (dir * s * 0.045f), p.Y - (s * 0.005f), p.X + (dir * s * 0.03f), p.Y - (s * 0.02f));
            stroke.StrokeWidth = s * 0.003f;
            c.DrawPath(curl, stroke);
            stroke.StrokeWidth = s * 0.006f;
        }
    }

    /// <summary>Tufts of grass in silhouette along <paramref name="ground"/>.</summary>
    private static void SpookyGrass(SKCanvas c, int w, float ground, float blade, SKColor colour, Random rng)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = colour };
        using var grass = new SKPath();
        for (var x = 0f; x < w; x += blade * R(rng, 0.15f, 0.35f))
        {
            var bh = blade * R(rng, 0.4f, 1.3f);
            var bend = R(rng, -bh * 0.4f, bh * 0.4f);
            grass.MoveTo(x - (blade * 0.06f), ground + 2);
            grass.QuadTo(x, ground - (bh * 0.6f), x + bend, ground - bh);
            grass.QuadTo(x + (blade * 0.02f), ground - (bh * 0.5f), x + (blade * 0.08f), ground + 2);
            grass.Close();
        }

        c.DrawPath(grass, paint);
    }

    /// <summary>A rough round-topped tree, for distant tree lines.</summary>
    private static void SpookyRoundTree(SKCanvas c, float x, float ground, float size, SKColor colour)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = colour };
        c.DrawRect(x - (size * 0.06f), ground - (size * 0.6f), size * 0.12f, size * 0.6f, paint);
        c.DrawOval(new SKRect(x - (size * 0.4f), ground - (size * 1.25f), x + (size * 0.4f), ground - (size * 0.45f)), paint);
        c.DrawOval(new SKRect(x - (size * 0.5f), ground - (size * 0.95f), x + (size * 0.2f), ground - (size * 0.4f)), paint);
    }

    /// <summary>A small farmhouse with one lit window, far off on the hill.</summary>
    private static void SpookyFarmhouse(SKCanvas c, float x, float ground, float u, SKColor colour)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = colour };
        using var house = new SKPath();
        house.MoveTo(x - u, ground);
        house.LineTo(x - u, ground - (u * 0.55f));
        house.LineTo(x - (u * 0.45f), ground - (u * 0.95f));
        house.LineTo(x + (u * 0.1f), ground - (u * 0.55f));
        house.LineTo(x + (u * 0.9f), ground - (u * 0.55f));
        house.LineTo(x + (u * 0.9f), ground);
        house.Close();
        c.DrawPath(house, paint);
        c.DrawRect(x + (u * 0.5f), ground - (u * 0.85f), u * 0.12f, u * 0.3f, paint);
        SpookyGlow(c, x - (u * 0.45f), ground - (u * 0.35f), u * 0.4f, new SKColor(0xFF, 0xB0, 0x40, 120));
        using var lit = new SKPaint { Color = new SKColor(0xFF, 0xC8, 0x60) };
        c.DrawRect(x - (u * 0.53f), ground - (u * 0.45f), u * 0.16f, u * 0.18f, lit);
    }

    /// <summary>A crooked wooden fence from <paramref name="x0"/> to <paramref name="x1"/>.</summary>
    private static void SpookyFence(SKCanvas c, float x0, float x1, float ground, float height, SKColor colour, Random rng)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = colour };
        var posts = new List<SKPoint>();
        for (var x = x0; x <= x1; x += height * R(rng, 0.9f, 1.2f))
        {
            var ph = height * R(rng, 0.8f, 1.1f);
            var tilt = R(rng, -8, 8);
            c.Save();
            c.Translate(x, ground);
            c.RotateDegrees(tilt);
            using var post = new SKPath();
            post.MoveTo(-height * 0.05f, 0);
            post.LineTo(-height * 0.05f, -ph);
            post.LineTo(0, -ph - (height * 0.08f));
            post.LineTo(height * 0.05f, -ph);
            post.LineTo(height * 0.05f, 0);
            post.Close();
            c.DrawPath(post, paint);
            c.Restore();
            posts.Add(new SKPoint(x + (MathF.Sin(tilt * MathF.PI / 180) * ph * 0.6f), ground - (ph * 0.6f)));
        }

        using var rail = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = height * 0.06f, Color = colour };
        for (var i = 1; i < posts.Count; i++)
        {
            if (rng.NextDouble() < 0.2)
            {
                continue; // a broken rail
            }

            c.DrawLine(posts[i - 1].X, posts[i - 1].Y + R(rng, -height * 0.05f, height * 0.05f), posts[i].X, posts[i].Y, rail);
            c.DrawLine(posts[i - 1].X, posts[i - 1].Y + (height * 0.3f), posts[i].X, posts[i].Y + (height * 0.3f) + R(rng, -height * 0.08f, height * 0.08f), rail);
        }
    }
}
