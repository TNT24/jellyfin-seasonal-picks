using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>Chinese New Year scenes.</summary>
internal static partial class CoverArt
{
    private static readonly SKColor Gold = new(0xFF, 0xC1, 0x07);

    // ---------- Chinese New Year, week 1: red lanterns, plum blossom and red envelopes ----------

    private static void RedLanterns(SKCanvas c, int w, int h, Random rng)
    {
        FestRedLanterns(c, w, h, rng);
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
        FestLanternFestival(c, w, h, rng);
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
