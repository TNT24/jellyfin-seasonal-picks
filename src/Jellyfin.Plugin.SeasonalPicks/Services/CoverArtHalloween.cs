using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>The Halloween scenes the weeks of October switch between after the pumpkin patch.</summary>
internal static partial class CoverArt
{
    private static readonly SKColor Silhouette = new(0x0E, 0x08, 0x14);

    // ---------- Graveyard: a bare tree, leaning headstones and mist under a full moon ----------

    private static void Graveyard(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        NightSky(c, w, h, new SKColor(0x0B, 0x05, 0x1A, 170), new SKColor(0x2A, 0x10, 0x30, 90));
        Stars(c, w, h * 0.5f, 90, s, rng);
        Moon(c, w * 0.2f, h * 0.19f, s * 0.12f, rng);

        using (var bat = new SKPaint { IsAntialias = true, Color = new SKColor(0x12, 0x08, 0x10, 235) })
        {
            foreach (var (bx, by) in new[] { (0.4f, 0.1f), (0.52f, 0.22f), (0.64f, 0.11f) })
            {
                Bat(c, w * (bx + R(rng, -0.03f, 0.03f)), h * (by + R(rng, -0.02f, 0.02f)), s * R(rng, 0.035f, 0.055f), R(rng, -20, 20), bat);
            }
        }

        using (var tree = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, Color = Silhouette })
        {
            BareTree(c, w * 0.88f, h * 0.9f, s * 0.3f, tree, rng);
        }

        using (var hill = new SKPaint { IsAntialias = true, Color = new SKColor(0x12, 0x0A, 0x18, 245) })
        {
            c.DrawPath(Hills(w, h, h * 0.86f, h * 0.035f, 3, rng), hill);
        }

        // Headstones in a row, with one slot left for a skeleton climbing out of its grave.
        var count = w > h ? 6 : 4;
        var skeletonSlot = w > h ? 2 : 1;
        for (var i = 0; i < count; i++)
        {
            var x = (w * 0.82f / count * (i + 0.5f)) + R(rng, -w * 0.02f, w * 0.02f);
            var stoneH = s * R(rng, 0.11f, 0.16f);
            if (i == skeletonSlot)
            {
                Skeleton(c, x, h * 0.93f, s * 0.19f);
                continue;
            }

            Gravestone(c, x, h * (0.92f + R(rng, 0, 0.02f)), stoneH * R(rng, 0.55f, 0.7f), stoneH, R(rng, -9, 9), i % 3, rng);
        }

        Mist(c, w, h, h * 0.8f, s, rng, 75);
    }

    /// <summary>A bare, twisting tree: each branch splits into thinner ones.</summary>
    private static void BareTree(SKCanvas c, float x, float ground, float height, SKPaint paint, Random rng)
    {
        void Branch(float x0, float y0, double angle, float len, float width, int depth)
        {
            var x1 = x0 + (float)(Math.Cos(angle) * len);
            var y1 = y0 - (float)(Math.Sin(angle) * len);
            var bend = R(rng, -len * 0.22f, len * 0.22f);
            using var path = new SKPath();
            path.MoveTo(x0, y0);
            path.QuadTo(((x0 + x1) / 2) + (bend * (float)Math.Sin(angle)), ((y0 + y1) / 2) + (bend * (float)Math.Cos(angle)), x1, y1);
            paint.StrokeWidth = width;
            c.DrawPath(path, paint);
            if (depth == 0 || width < 1.2f)
            {
                return;
            }

            var kids = depth > 4 ? 2 : rng.Next(2, 4);
            for (var k = 0; k < kids; k++)
            {
                var spread = ((k - ((kids - 1) / 2.0)) * R(rng, 0.45f, 0.8f)) + R(rng, -0.12f, 0.12f);
                var next = Math.Clamp(angle + spread, Math.PI * 0.04, Math.PI * 0.96);
                Branch(x1, y1, next, len * R(rng, 0.62f, 0.8f), width * 0.66f, depth - 1);
            }
        }

        Branch(x, ground, (Math.PI / 2) + R(rng, -0.1f, 0.1f), height * 0.3f, height * 0.065f, 6);
    }

    private static void Gravestone(SKCanvas c, float x, float ground, float sw, float sh, float tilt, int shape, Random rng)
    {
        c.Save();
        c.Translate(x, ground);
        c.RotateDegrees(tilt);

        using var stone = new SKPath();
        switch (shape)
        {
            case 1: // Celtic cross on a plinth: the bars joined to a ring
                using (var cross = new SKPath())
                using (var ring = new SKPath { FillType = SKPathFillType.EvenOdd })
                {
                    cross.AddRect(new SKRect(-sw * 0.16f, -sh, sw * 0.16f, 0));
                    cross.AddRect(new SKRect(-sw * 0.48f, -sh * 0.78f, sw * 0.48f, -sh * 0.62f));
                    cross.AddRect(new SKRect(-sw * 0.4f, -sh * 0.08f, sw * 0.4f, 0));
                    ring.AddCircle(0, -sh * 0.7f, sw * 0.27f);
                    ring.AddCircle(0, -sh * 0.7f, sw * 0.18f);
                    using var joined = cross.Op(ring, SKPathOp.Union);
                    stone.AddPath(joined);
                }

                break;
            case 2: // pointed
                stone.MoveTo(-sw / 2, 0);
                stone.LineTo(-sw / 2, -sh * 0.7f);
                stone.QuadTo(-sw / 2, -sh * 0.92f, 0, -sh);
                stone.QuadTo(sw / 2, -sh * 0.92f, sw / 2, -sh * 0.7f);
                stone.LineTo(sw / 2, 0);
                stone.Close();
                break;
            default: // rounded
                stone.MoveTo(-sw / 2, 0);
                stone.LineTo(-sw / 2, -sh + (sw / 2));
                stone.ArcTo(new SKRect(-sw / 2, -sh, sw / 2, -sh + sw), 180, 180, false);
                stone.LineTo(sw / 2, 0);
                stone.Close();
                break;
        }

        using (var fill = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateLinearGradient(new SKPoint(-sw / 2, -sh), new SKPoint(sw / 2, 0), new[] { new SKColor(0x9A, 0x9F, 0xAE), new SKColor(0x5A, 0x5E, 0x6C), new SKColor(0x36, 0x38, 0x44) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp)
        })
        using (var edge = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = sw * 0.03f, Color = new SKColor(0x22, 0x23, 0x2C) })
        {
            c.DrawPath(stone, fill);
            c.DrawPath(stone, edge);
        }

        using var carve = new SKPaint { IsAntialias = true, Color = new SKColor(0x2B, 0x2D, 0x36, 210), Style = SKPaintStyle.Stroke, StrokeWidth = sw * 0.035f, StrokeCap = SKStrokeCap.Round };
        if (shape != 1)
        {
            using var font = new SKFont(TitleTypeface.Value, sw * 0.27f);
            using var text = new SKPaint { IsAntialias = true, Color = carve.Color };
            c.DrawText("RIP", 0, -sh * 0.48f, SKTextAlign.Center, font, text);
            c.DrawLine(-sw * 0.28f, -sh * 0.36f, sw * 0.28f, -sh * 0.36f, carve);
            c.DrawLine(-sw * 0.2f, -sh * 0.27f, sw * 0.2f, -sh * 0.27f, carve);

            // A crack down one side.
            using var crack = new SKPath();
            var cx = sw * R(rng, -0.35f, 0.35f);
            crack.MoveTo(cx, -sh * 0.98f);
            crack.LineTo(cx + (sw * 0.06f), -sh * 0.85f);
            crack.LineTo(cx - (sw * 0.03f), -sh * 0.78f);
            crack.LineTo(cx + (sw * 0.04f), -sh * 0.68f);
            carve.StrokeWidth = sw * 0.02f;
            c.DrawPath(crack, carve);
        }

        // Moss at the foot.
        using (var moss = new SKPaint { IsAntialias = true, Color = new SKColor(0x2E, 0x42, 0x24, 150) })
        {
            for (var i = 0; i < 6; i++)
            {
                c.DrawCircle(R(rng, -sw * 0.45f, sw * 0.45f), -sh * R(rng, 0, 0.06f), sw * R(rng, 0.04f, 0.08f), moss);
            }
        }

        c.Restore();
    }

    /// <summary>
    /// A skeleton climbing out of its grave, arms raised, seen from the front. (x, ground) is where its spine meets the
    /// earth; u is its height above it. Every bone's dark outline goes down before any bone, so joints stay clean.
    /// </summary>
    private static void Skeleton(SKCanvas c, float x, float ground, float u)
    {
        using var bones = new SKPath();
        void Bone(params (float X, float Y)[] pts)
        {
            bones.MoveTo(x + (pts[0].X * u), ground + (pts[0].Y * u));
            for (var i = 1; i < pts.Length; i++)
            {
                bones.LineTo(x + (pts[i].X * u), ground + (pts[i].Y * u));
            }
        }

        // Spine and neck, collarbones, then each arm: shoulder, elbow, wrist, and four spread fingers.
        Bone((0, 0.05f), (0, -0.66f));
        Bone((-0.17f, -0.57f), (0, -0.6f), (0.17f, -0.57f));
        foreach (var side in new[] { -1f, 1f })
        {
            Bone((side * 0.17f, -0.57f), (side * 0.3f, -0.76f), (side * 0.27f, -0.98f));
            foreach (var (fx, fy) in new[] { (0.2f, -1.07f), (0.25f, -1.1f), (0.3f, -1.09f), (0.34f, -1.05f) })
            {
                Bone((side * 0.27f, -0.99f), (side * fx, fy));
            }
        }

        // Ribs: pairs curving out and down from the spine, shorter towards the bottom.
        for (var r = 0; r < 4; r++)
        {
            var y = -0.52f + (r * 0.08f);
            var reach = 0.15f - (r * 0.015f);
            foreach (var side in new[] { -1f, 1f })
            {
                bones.MoveTo(x, ground + (y * u));
                bones.CubicTo(x + (side * reach * 0.6f * u), ground + ((y - 0.03f) * u), x + (side * reach * u), ground + ((y - 0.01f) * u), x + (side * reach * u), ground + ((y + 0.05f) * u));
            }
        }

        using var outline = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round, StrokeWidth = u * 0.055f, Color = new SKColor(0x2A, 0x24, 0x22) };
        using var ivory = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round, StrokeWidth = u * 0.03f, Color = new SKColor(0xED, 0xE6, 0xD3) };
        c.DrawPath(bones, outline);
        c.DrawPath(bones, ivory);

        // Knobbly joints at the elbows and wrists.
        using var joint = new SKPaint { IsAntialias = true, Color = ivory.Color };
        foreach (var side in new[] { -1f, 1f })
        {
            c.DrawCircle(x + (side * 0.3f * u), ground - (0.76f * u), u * 0.022f, joint);
            c.DrawCircle(x + (side * 0.27f * u), ground - (0.99f * u), u * 0.026f, joint);
        }

        // Skull: cranium and jaw as one shape, with sockets, nose and teeth.
        using var skull = new SKPath();
        skull.AddOval(new SKRect(x - (u * 0.11f), ground - (u * 0.95f), x + (u * 0.11f), ground - (u * 0.72f)));
        skull.AddRoundRect(new SKRect(x - (u * 0.07f), ground - (u * 0.78f), x + (u * 0.07f), ground - (u * 0.66f)), u * 0.03f, u * 0.03f);
        using (var head = new SKPath())
        {
            skull.Simplify(head);
            using var fill = new SKPaint { IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(x - (u * 0.04f), ground - (u * 0.88f)), u * 0.2f, new[] { new SKColor(0xFA, 0xF5, 0xE8), new SKColor(0xD8, 0xCF, 0xB8) }, SKShaderTileMode.Clamp) };
            using var edge = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = u * 0.012f, Color = outline.Color };
            c.DrawPath(head, fill);
            c.DrawPath(head, edge);
        }

        using var dark = new SKPaint { IsAntialias = true, Color = new SKColor(0x1E, 0x18, 0x18) };
        c.DrawOval(new SKRect(x - (u * 0.075f), ground - (u * 0.87f), x - (u * 0.02f), ground - (u * 0.8f)), dark);
        c.DrawOval(new SKRect(x + (u * 0.02f), ground - (u * 0.87f), x + (u * 0.075f), ground - (u * 0.8f)), dark);
        using (var nose = new SKPath())
        {
            nose.MoveTo(x, ground - (u * 0.79f));
            nose.LineTo(x - (u * 0.015f), ground - (u * 0.755f));
            nose.LineTo(x + (u * 0.015f), ground - (u * 0.755f));
            nose.Close();
            c.DrawPath(nose, dark);
        }

        using var teeth = new SKPaint { IsAntialias = true, StrokeWidth = u * 0.008f, Color = dark.Color };
        c.DrawLine(x - (u * 0.05f), ground - (u * 0.705f), x + (u * 0.05f), ground - (u * 0.705f), teeth);
        for (var t = -2; t <= 2; t++)
        {
            c.DrawLine(x + (t * u * 0.02f), ground - (u * 0.725f), x + (t * u * 0.02f), ground - (u * 0.685f), teeth);
        }

        // Fresh earth heaped in front, hiding the hips.
        using var earth = new SKPaint { IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(0, ground - (u * 0.12f)), new SKPoint(0, ground + (u * 0.1f)), new[] { new SKColor(0x5D, 0x40, 0x30), new SKColor(0x2E, 0x1E, 0x16) }, SKShaderTileMode.Clamp) };
        c.DrawOval(new SKRect(x - (u * 0.42f), ground - (u * 0.14f), x + (u * 0.42f), ground + (u * 0.12f)), earth);
        using var clod = new SKPaint { IsAntialias = true, Color = new SKColor(0x4A, 0x32, 0x24) };
        foreach (var (cx, cy, cr) in new[] { (-0.3f, -0.1f, 0.04f), (0.27f, -0.08f, 0.035f), (-0.12f, -0.15f, 0.03f), (0.15f, -0.14f, 0.025f) })
        {
            c.DrawCircle(x + (cx * u), ground + (cy * u), cr * u, clod);
        }
    }

    // ---------- Haunted house: a crooked house on the hill, a witch across the moon ----------

    private static void HauntedHouse(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        NightSky(c, w, h, new SKColor(0x10, 0x06, 0x22, 175), new SKColor(0x30, 0x12, 0x28, 90));
        Stars(c, w, h * 0.55f, 100, s, rng);

        var moon = new SKPoint(w * 0.74f, h * 0.2f);
        var mr = s * 0.15f;
        Moon(c, moon.X, moon.Y, mr, rng);
        Witch(c, moon.X - (mr * 0.1f), moon.Y + (mr * 0.1f), mr * 0.85f);

        using (var bat = new SKPaint { IsAntialias = true, Color = new SKColor(0x12, 0x08, 0x10, 235) })
        {
            var placed = new List<(float X, float Y, float R)> { (moon.X, moon.Y, mr * 1.1f) };
            for (var i = 0; i < 4; i++)
            {
                var br = s * R(rng, 0.03f, 0.05f);
                if (FindClear(rng, placed, br, w * 0.05f, w * 0.6f, h * 0.06f, h * 0.28f, 1.4f) is { } at)
                {
                    Bat(c, at.X, at.Y, br, R(rng, -20, 20), bat);
                    placed.Add((at.X, at.Y, br));
                }
            }
        }

        using (var hill = new SKPaint { IsAntialias = true, Color = new SKColor(0x14, 0x0A, 0x1A, 245) })
        {
            c.DrawOval(new SKRect(-w * 0.3f, h * 0.86f, w * 0.75f, h * 1.3f), hill);
            c.DrawPath(Hills(w, h, h * 0.92f, h * 0.02f, 2, rng), hill);
        }

        House(c, w * (w > h ? 0.16f : 0.2f), h * 0.9f, s * 0.21f, rng);

        using (var tree = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, Color = Silhouette })
        {
            BareTree(c, w * 0.9f, h * 0.95f, s * 0.3f, tree, rng);
        }

        Fence(c, w, h, s, rng);
        Mist(c, w, h, h * 0.85f, s, rng, 60);
    }

    /// <summary>A crooked old house with a tower, lit and broken windows. (x, ground) is its centre at ground level; u its height unit.</summary>
    private static void House(SKCanvas c, float x, float ground, float u, Random rng)
    {
        using var dark = new SKPaint { IsAntialias = true, Color = Silhouette };
        using var rim = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = u * 0.008f, Color = new SKColor(0x6A, 0x4A, 0x7A, 160) };

        SKPath Poly(params (float X, float Y)[] pts)
        {
            var p = new SKPath();
            p.MoveTo(x + (pts[0].X * u), ground + (pts[0].Y * u));
            for (var i = 1; i < pts.Length; i++)
            {
                p.LineTo(x + (pts[i].X * u), ground + (pts[i].Y * u));
            }

            p.Close();
            return p;
        }

        var parts = new[]
        {
            Poly((-0.78f, 0), (-0.78f, -0.32f), (-0.45f, -0.32f), (-0.45f, 0)),                          // left wing
            Poly((-0.84f, -0.31f), (-0.6f, -0.55f), (-0.4f, -0.31f)),                                     // its roof
            Poly((-0.45f, 0), (-0.46f, -0.47f), (0.36f, -0.45f), (0.35f, 0)),                              // main block
            Poly((-0.55f, -0.46f), (-0.06f, -0.86f), (0.45f, -0.44f)),                                    // main roof, a little off true
            Poly((-0.32f, -0.6f), (-0.33f, -0.9f), (-0.22f, -0.92f), (-0.21f, -0.68f)),                    // chimney, leaning
            Poly((0.2f, 0), (0.2f, -0.98f), (0.44f, -0.98f), (0.44f, 0)),                                  // tower
            Poly((0.15f, -0.97f), (0.33f, -1.38f), (0.49f, -0.97f))                                       // tower roof
        };
        foreach (var part in parts)
        {
            c.DrawPath(part, dark);
            c.DrawPath(part, rim);
            part.Dispose();
        }

        // Weathervane.
        using (var vane = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = u * 0.012f, Color = Silhouette })
        {
            c.DrawLine(x + (u * 0.33f), ground - (u * 1.38f), x + (u * 0.33f), ground - (u * 1.52f), vane);
            c.DrawLine(x + (u * 0.25f), ground - (u * 1.47f), x + (u * 0.41f), ground - (u * 1.47f), vane);
        }

        using var lit = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(new SKPoint(x, ground - (u * 0.5f)), u, new[] { new SKColor(0xFF, 0xE0, 0x82), new SKColor(0xFF, 0xA7, 0x26) }, SKShaderTileMode.Clamp),
            ImageFilter = SKImageFilter.CreateDropShadow(0, 0, u * 0.03f, u * 0.03f, new SKColor(0xFF, 0x9F, 0x00, 200))
        };
        using var unlit = new SKPaint { IsAntialias = true, Color = new SKColor(0x24, 0x1A, 0x2C) };
        using var bars = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = u * 0.012f, Color = Silhouette };
        using var boards = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = u * 0.025f, Color = new SKColor(0x4A, 0x36, 0x2C) };

        var windows = new[] { (-0.36f, -0.4f), (-0.16f, -0.4f), (0.04f, -0.4f), (-0.36f, -0.2f), (0.04f, -0.2f), (0.26f, -0.86f), (0.26f, -0.62f), (0.26f, -0.38f), (-0.68f, -0.22f) };
        foreach (var (wx, wy) in windows)
        {
            var rect = new SKRect(x + (wx * u), ground + (wy * u), x + ((wx + 0.12f) * u), ground + ((wy + 0.14f) * u));
            var on = rng.NextDouble() < 0.7;
            c.DrawRoundRect(rect, u * 0.012f, u * 0.012f, on ? lit : unlit);
            c.DrawLine(rect.MidX, rect.Top, rect.MidX, rect.Bottom, bars);
            c.DrawLine(rect.Left, rect.MidY, rect.Right, rect.MidY, bars);
            if (!on)
            {
                c.DrawLine(rect.Left - (u * 0.01f), rect.Top + (u * 0.02f), rect.Right + (u * 0.01f), rect.Bottom - (u * 0.03f), boards);
                c.DrawLine(rect.Left - (u * 0.01f), rect.Bottom - (u * 0.02f), rect.Right + (u * 0.01f), rect.Top + (u * 0.04f), boards);
            }
        }

        // Round attic window and the door.
        c.DrawCircle(x - (u * 0.06f), ground - (u * 0.6f), u * 0.055f, lit);
        c.DrawLine(x - (u * 0.06f), ground - (u * 0.655f), x - (u * 0.06f), ground - (u * 0.545f), bars);
        using var door = new SKPath();
        door.MoveTo(x - (u * 0.16f), ground);
        door.LineTo(x - (u * 0.16f), ground - (u * 0.14f));
        door.ArcTo(new SKRect(x - (u * 0.16f), ground - (u * 0.2f), x - (u * 0.04f), ground - (u * 0.08f)), 180, 180, false);
        door.LineTo(x - (u * 0.04f), ground);
        door.Close();
        using (var doorPaint = new SKPaint { IsAntialias = true, Color = new SKColor(0x5A, 0x3A, 0x1E) })
        {
            c.DrawPath(door, doorPaint);
        }
    }

    /// <summary>A witch on a broomstick in silhouette, flying right. u is about her height.</summary>
    private static void Witch(SKCanvas c, float x, float y, float u)
    {
        using var p = new SKPaint { IsAntialias = true, Color = Silhouette };
        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(-10);

        // Broom handle and bristles.
        using (var stick = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = u * 0.045f, Color = Silhouette })
        {
            c.DrawLine(-u * 0.9f, u * 0.2f, u * 0.95f, u * 0.08f, stick);
        }

        using (var bristles = new SKPath())
        {
            bristles.MoveTo(-u * 0.85f, u * 0.17f);
            bristles.LineTo(-u * 1.35f, u * 0.02f);
            bristles.QuadTo(-u * 1.3f, u * 0.22f, -u * 1.4f, u * 0.42f);
            bristles.LineTo(-u * 0.85f, u * 0.24f);
            bristles.Close();
            c.DrawPath(bristles, p);
        }

        // Cloak sitting on the broom, flying back in the wind.
        using (var cloak = new SKPath())
        {
            cloak.MoveTo(u * 0.3f, u * 0.14f);
            cloak.CubicTo(u * 0.25f, -u * 0.15f, u * 0.15f, -u * 0.35f, u * 0.02f, -u * 0.42f);
            cloak.CubicTo(-u * 0.25f, -u * 0.4f, -u * 0.55f, -u * 0.2f, -u * 0.8f, -u * 0.12f);
            cloak.LineTo(-u * 0.62f, -u * 0.02f);
            cloak.LineTo(-u * 0.78f, u * 0.08f);
            cloak.CubicTo(-u * 0.4f, u * 0.1f, -u * 0.2f, u * 0.2f, -u * 0.15f, u * 0.4f);
            cloak.LineTo(u * 0.05f, u * 0.38f);
            cloak.Close();
            c.DrawPath(cloak, p);
        }

        // Arm out to the broom, head with a long nose, and the hat bent back.
        using (var arm = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = u * 0.06f, Color = Silhouette })
        {
            c.DrawLine(u * 0.1f, -u * 0.3f, u * 0.45f, u * 0.1f, arm);
        }

        c.DrawCircle(u * 0.06f, -u * 0.52f, u * 0.11f, p);
        using (var face = new SKPath())
        {
            face.MoveTo(u * 0.14f, -u * 0.56f);
            face.LineTo(u * 0.3f, -u * 0.5f);
            face.LineTo(u * 0.15f, -u * 0.47f);
            face.Close();
            c.DrawPath(face, p);
        }

        c.DrawOval(new SKRect(-u * 0.14f, -u * 0.66f, u * 0.28f, -u * 0.58f), p);
        using (var hat = new SKPath())
        {
            hat.MoveTo(-u * 0.06f, -u * 0.62f);
            hat.QuadTo(u * 0.0f, -u * 0.9f, -u * 0.28f, -u * 1.02f);
            hat.QuadTo(u * 0.1f, -u * 0.86f, u * 0.2f, -u * 0.62f);
            hat.Close();
            c.DrawPath(hat, p);
        }

        c.Restore();
    }

    /// <summary>A rickety picket fence along the bottom, with a few pickets missing.</summary>
    private static void Fence(SKCanvas c, int w, int h, float s, Random rng)
    {
        using var wood = new SKPaint { IsAntialias = true, Color = new SKColor(0x1C, 0x12, 0x1E) };
        var top = h * 0.9f;
        c.DrawRect(0, top + (s * 0.025f), w, s * 0.012f, wood);
        c.DrawRect(0, top + (s * 0.06f), w, s * 0.012f, wood);
        for (var x = s * 0.01f; x < w; x += s * 0.034f)
        {
            if (rng.NextDouble() < 0.12)
            {
                continue;
            }

            var ph = s * R(rng, 0.075f, 0.1f);
            c.Save();
            c.Translate(x, h);
            c.RotateDegrees(R(rng, -6, 6));
            using var picket = new SKPath();
            picket.MoveTo(-s * 0.009f, 0);
            picket.LineTo(-s * 0.009f, -ph);
            picket.LineTo(0, -ph - (s * 0.014f));
            picket.LineTo(s * 0.009f, -ph);
            picket.LineTo(s * 0.009f, 0);
            picket.Close();
            c.DrawPath(picket, wood);
            c.Restore();
        }
    }

    // ---------- Witch's cauldron: cobwebs, hanging spiders, candles and a bubbling brew ----------

    private static void Cauldron(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        NightSky(c, w, h, new SKColor(0x0C, 0x06, 0x18, 160), new SKColor(0x0E, 0x16, 0x0C, 120));

        // Cobwebs in the top corners; spiders hang in the gap between them so no thread crosses a web.
        var leftWeb = s * 0.34f;
        var rightWeb = s * 0.3f;
        Cobweb(c, 0, 0, leftWeb, false, s);
        Cobweb(c, w, 0, rightWeb, true, s);
        var gapLeft = leftWeb + (s * 0.06f);
        var gapRight = w - rightWeb - (s * 0.06f);
        var spiders = w > h ? 3 : 2;
        for (var i = 0; i < spiders; i++)
        {
            var sx = gapLeft + ((gapRight - gapLeft) * (i + 0.5f) / spiders);
            Spider(c, sx, h * (i % 2 == 0 ? 0.2f : 0.11f), s * R(rng, 0.025f, 0.033f), s);
        }

        // Stone floor.
        using (var floor = new SKPaint { Shader = SKShader.CreateLinearGradient(new SKPoint(0, h * 0.88f), new SKPoint(0, h), new[] { new SKColor(0x1A, 0x16, 0x20, 0), new SKColor(0x1A, 0x16, 0x20, 240) }, SKShaderTileMode.Clamp) })
        {
            c.DrawRect(0, h * 0.88f, w, h * 0.12f, floor);
        }

        var cx = w / 2f;
        var ground = h * 0.97f;
        var r = s * 0.16f;
        CauldronPot(c, cx, ground, r, rng, s);

        var side = w > h ? 0.08f : 0.0f;
        TallCandle(c, w * (0.07f + side), ground, s * 0.2f, rng);
        TallCandle(c, w * (0.15f + side), ground, s * 0.13f, rng);
        TallCandle(c, w * (0.88f - side), ground, s * 0.17f, rng);
        Pumpkin(c, w * (0.28f + (side / 2)), ground, s * 0.075f);
        Pumpkin(c, w * (0.73f - (side / 2)), ground, s * 0.06f);
    }

    private static void Cobweb(SKCanvas c, float cornerX, float cornerY, float size, bool mirror, float s)
    {
        c.Save();
        c.Translate(cornerX, cornerY);
        c.Scale(mirror ? -1 : 1, 1);

        using var silk = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.0022f, Color = SKColors.White.WithAlpha(150) };
        const int Spokes = 7;
        var angles = new double[Spokes];
        for (var i = 0; i < Spokes; i++)
        {
            angles[i] = Math.PI / 2 * i / (Spokes - 1);
            c.DrawLine(0, 0, (float)(Math.Cos(angles[i]) * size), (float)(Math.Sin(angles[i]) * size), silk);
        }

        // Rings sag towards the corner between the spokes.
        for (var ring = 1; ring <= 6; ring++)
        {
            var rr = size * ring / 6.4f;
            using var path = new SKPath();
            for (var i = 0; i < Spokes; i++)
            {
                var px = (float)(Math.Cos(angles[i]) * rr);
                var py = (float)(Math.Sin(angles[i]) * rr);
                if (i == 0)
                {
                    path.MoveTo(px, py);
                    continue;
                }

                var mid = (angles[i] + angles[i - 1]) / 2;
                path.QuadTo((float)(Math.Cos(mid) * rr * 0.86), (float)(Math.Sin(mid) * rr * 0.86), px, py);
            }

            c.DrawPath(path, silk);
        }

        c.Restore();
    }

    /// <summary>A spider hanging on its thread from the top edge, body at (x, y).</summary>
    private static void Spider(SKCanvas c, float x, float y, float r, float s)
    {
        using (var thread = new SKPaint { IsAntialias = true, StrokeWidth = s * 0.0018f, Color = SKColors.White.WithAlpha(140) })
        {
            c.DrawLine(x, 0, x, y - r, thread);
        }

        using var body = new SKPaint { IsAntialias = true, Color = new SKColor(0x0A, 0x08, 0x0E) };
        using var legs = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round, StrokeWidth = r * 0.14f, Color = body.Color };
        foreach (var side in new[] { -1f, 1f })
        {
            for (var i = 0; i < 4; i++)
            {
                var ky = y - (r * 0.3f) + (i * r * 0.32f);
                using var leg = new SKPath();
                leg.MoveTo(x, ky);
                leg.LineTo(x + (side * r * 1.1f), ky - (r * (0.55f - (i * 0.18f))));
                leg.LineTo(x + (side * r * 1.6f), ky + (r * (0.3f + (i * 0.15f))));
                c.DrawPath(leg, legs);
            }
        }

        c.DrawOval(new SKRect(x - (r * 0.6f), y - (r * 0.1f), x + (r * 0.6f), y + (r * 1.1f)), body);
        c.DrawCircle(x, y - (r * 0.3f), r * 0.38f, body);
        using var eye = new SKPaint { IsAntialias = true, Color = new SKColor(0xE5, 0x39, 0x35) };
        c.DrawCircle(x - (r * 0.13f), y - (r * 0.38f), r * 0.07f, eye);
        c.DrawCircle(x + (r * 0.13f), y - (r * 0.38f), r * 0.07f, eye);
    }

    private static void CauldronPot(SKCanvas c, float cx, float ground, float r, Random rng, float s)
    {
        var rimY = ground - (r * 1.5f);

        // Fire underneath.
        foreach (var (colour, scale) in new[] { (new SKColor(0xD8, 0x43, 0x15, 230), 1f), (new SKColor(0xFF, 0x8F, 0x00, 235), 0.75f), (new SKColor(0xFF, 0xD5, 0x4F, 240), 0.45f) })
        {
            using var flame = new SKPaint { IsAntialias = true, Color = colour, ImageFilter = SKImageFilter.CreateDropShadow(0, 0, r * 0.06f, r * 0.06f, colour) };
            for (var t = -2; t <= 2; t++)
            {
                var fx = cx + (t * r * 0.28f * scale);
                var fh = r * 0.45f * scale * R(rng, 0.7f, 1f);
                using var tongue = new SKPath();
                tongue.MoveTo(fx - (r * 0.14f * scale), ground);
                tongue.QuadTo(fx - (r * 0.1f), ground - (fh * 0.6f), fx + R(rng, -r * 0.05f, r * 0.05f), ground - fh);
                tongue.QuadTo(fx + (r * 0.1f), ground - (fh * 0.6f), fx + (r * 0.14f * scale), ground);
                tongue.Close();
                c.DrawPath(tongue, flame);
            }
        }

        // Three stubby legs, then the pot.
        using var iron = new SKPaint { IsAntialias = true, Color = new SKColor(0x16, 0x16, 0x1C) };
        foreach (var lx in new[] { -0.62f, 0f, 0.62f })
        {
            c.DrawRoundRect(new SKRect(cx + (lx * r) - (r * 0.07f), ground - (r * 0.4f), cx + (lx * r) + (r * 0.07f), ground - (r * 0.05f)), r * 0.03f, r * 0.03f, iron);
        }

        using var pot = new SKPath();
        pot.MoveTo(cx - (r * 0.88f), rimY);
        pot.CubicTo(cx - (r * 1.3f), rimY + (r * 0.55f), cx - (r * 0.95f), ground - (r * 0.3f), cx, ground - (r * 0.28f));
        pot.CubicTo(cx + (r * 0.95f), ground - (r * 0.3f), cx + (r * 1.3f), rimY + (r * 0.55f), cx + (r * 0.88f), rimY);
        pot.Close();
        using (var body = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(new SKPoint(cx - (r * 0.45f), rimY + (r * 0.35f)), r * 1.5f, new[] { new SKColor(0x4A, 0x4A, 0x58), new SKColor(0x1C, 0x1C, 0x24), new SKColor(0x08, 0x08, 0x0C) }, new[] { 0f, 0.45f, 1f }, SKShaderTileMode.Clamp)
        })
        {
            c.DrawPath(pot, body);
        }

        // Green glow from the brew on the pot's belly.
        using (var sheen = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = r * 0.03f, Color = new SKColor(0x9C, 0xFF, 0x57, 70) })
        {
            c.DrawArc(new SKRect(cx - (r * 0.95f), rimY - (r * 0.1f), cx + (r * 0.95f), ground - (r * 0.4f)), 200, 60, false, sheen);
        }

        // Brew, rim, bubbles and steam.
        var brewRect = new SKRect(cx - (r * 0.86f), rimY - (r * 0.16f), cx + (r * 0.86f), rimY + (r * 0.16f));
        using (var brew = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(new SKPoint(cx, rimY), r * 0.9f, new[] { new SKColor(0xC6, 0xFF, 0x7A), new SKColor(0x4C, 0xC2, 0x2A) }, SKShaderTileMode.Clamp),
            ImageFilter = SKImageFilter.CreateDropShadow(0, 0, r * 0.12f, r * 0.12f, new SKColor(0x7C, 0xFF, 0x3A, 220))
        })
        {
            c.DrawOval(brewRect, brew);
        }

        // Bubbles on the surface stay inside the brew (under the rim); a few rise clear above it.
        using var bubble = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = r * 0.02f, Color = new SKColor(0xE6, 0xFF, 0xC8, 220) };
        using (var surface = new SKPath())
        {
            surface.AddOval(brewRect);
            c.Save();
            c.ClipPath(surface, antialias: true);
            for (var i = 0; i < 5; i++)
            {
                c.DrawCircle(cx + R(rng, -r * 0.55f, r * 0.55f), rimY + R(rng, -r * 0.03f, r * 0.04f), r * R(rng, 0.035f, 0.07f), bubble);
            }

            c.Restore();
        }

        using (var lip = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = r * 0.09f, Color = new SKColor(0x2E, 0x2E, 0x38) })
        {
            c.DrawOval(brewRect, lip);
        }

        for (var i = 0; i < 4; i++)
        {
            c.DrawCircle(cx + R(rng, -r * 0.45f, r * 0.45f), rimY - R(rng, r * 0.3f, r * 0.46f), r * R(rng, 0.03f, 0.05f), bubble);
        }

        // Steam curling up, kept below the title.
        using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.01f);
        using var steam = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = r * 0.12f, Color = new SKColor(0xB9, 0xFF, 0x8A, 110), MaskFilter = blur };
        for (var i = -1; i <= 1; i++)
        {
            using var wisp = new SKPath();
            var sx = cx + (i * r * 0.4f);
            wisp.MoveTo(sx, rimY - (r * 0.12f));
            wisp.CubicTo(sx + (r * 0.2f), rimY - (r * 0.3f), sx - (r * 0.2f), rimY - (r * 0.45f), sx + (r * 0.08f), rimY - (r * 0.62f));
            c.DrawPath(wisp, steam);
        }
    }

    /// <summary>A tall church candle with wax drips, standing on the ground.</summary>
    private static void TallCandle(SKCanvas c, float x, float ground, float height, Random rng)
    {
        var cw = height * 0.22f;
        var top = ground - height;
        using (var halo = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(new SKPoint(x, top - (cw * 0.5f)), height * 0.6f, new[] { new SKColor(0xFF, 0xD5, 0x80, 90), SKColors.Transparent }, SKShaderTileMode.Clamp)
        })
        {
            c.DrawCircle(x, top - (cw * 0.5f), height * 0.6f, halo);
        }

        using var wax = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateLinearGradient(new SKPoint(x - (cw / 2), 0), new SKPoint(x + (cw / 2), 0), new[] { new SKColor(0xFF, 0xF4, 0xDC), new SKColor(0xE8, 0xD8, 0xB8), new SKColor(0xB8, 0xA4, 0x84) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp)
        };
        c.DrawRect(x - (cw / 2), top, cw, height, wax);

        // Drips running down from the top edge, and a pool at the foot.
        for (var i = 0; i < 3; i++)
        {
            var dx = x + R(rng, -cw * 0.4f, cw * 0.4f);
            var len = height * R(rng, 0.08f, 0.3f);
            var dw = cw * R(rng, 0.12f, 0.2f);
            c.DrawRoundRect(new SKRect(dx - (dw / 2), top, dx + (dw / 2), top + len), dw / 2, dw / 2, wax);
            c.DrawCircle(dx, top + len, dw * 0.6f, wax);
        }

        c.DrawOval(new SKRect(x - (cw * 0.8f), ground - (cw * 0.15f), x + (cw * 0.8f), ground + (cw * 0.1f)), wax);

        using var wick = new SKPaint { IsAntialias = true, StrokeWidth = cw * 0.06f, Color = new SKColor(0x2B, 0x1B, 0x16) };
        c.DrawLine(x, top, x, top - (cw * 0.2f), wick);
        using var flame = new SKPath();
        flame.MoveTo(x, top - (cw * 1.3f));
        flame.CubicTo(x + (cw * 0.4f), top - (cw * 0.6f), x + (cw * 0.28f), top - (cw * 0.15f), x, top - (cw * 0.15f));
        flame.CubicTo(x - (cw * 0.28f), top - (cw * 0.15f), x - (cw * 0.4f), top - (cw * 0.6f), x, top - (cw * 1.3f));
        flame.Close();
        using var fire = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xC1, 0x07), ImageFilter = SKImageFilter.CreateDropShadow(0, 0, cw * 0.3f, cw * 0.3f, new SKColor(0xFF, 0x8F, 0x00)) };
        c.DrawPath(flame, fire);
    }
}
