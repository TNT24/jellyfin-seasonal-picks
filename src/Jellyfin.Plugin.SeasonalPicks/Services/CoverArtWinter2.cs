using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>
/// Painterly building blocks for the winter covers (snow, Christmas, New Year): skies, glows, layered snowy ground,
/// lush pines, cottages, glossy baubles, lights and fireworks. Everything is seeded, so a cover never changes.
/// </summary>
internal static partial class CoverArt
{
    private static readonly SKColor WinterSnowWhite = new(0xF3, 0xF7, 0xFF);

    private static readonly SKColor[] WinterBulbColours =
    {
        new(0xFF, 0xE6, 0xA8), new(0xFF, 0xC4, 0x45), new(0xFF, 0x5A, 0x4E), new(0x6E, 0xC6, 0xFF), new(0x8C, 0xF5, 0x9E), new(0xFF, 0xE6, 0xA8)
    };

    // ---------- colour and light ----------

    private static SKColor WinterMix(SKColor a, SKColor b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new SKColor(
            (byte)(a.Red + ((b.Red - a.Red) * t)),
            (byte)(a.Green + ((b.Green - a.Green) * t)),
            (byte)(a.Blue + ((b.Blue - a.Blue) * t)),
            (byte)(a.Alpha + ((b.Alpha - a.Alpha) * t)));
    }

    /// <summary>A vertical gradient band across the whole width, from <paramref name="y0"/> to <paramref name="y1"/>.</summary>
    private static void WinterWash(SKCanvas c, float w, float y0, float y1, SKColor[] colours, float[]? stops = null)
    {
        using var shader = SKShader.CreateLinearGradient(new SKPoint(0, y0), new SKPoint(0, y1), colours, stops, SKShaderTileMode.Clamp);
        using var p = new SKPaint { Shader = shader };
        c.DrawRect(-2, y0, w + 4, y1 - y0, p);
    }

    /// <summary>A soft round light: <paramref name="colour"/> in the middle fading to nothing at radius r. Added light when <paramref name="add"/>.</summary>
    private static void WinterGlow(SKCanvas c, float x, float y, float r, SKColor colour, bool add = false)
    {
        if (r <= 0)
        {
            return;
        }

        using var shader = SKShader.CreateRadialGradient(
            new SKPoint(x, y), r,
            new[] { colour, colour.WithAlpha((byte)(colour.Alpha * 0.4f)), colour.WithAlpha((byte)(colour.Alpha * 0.12f)), colour.WithAlpha(0) },
            new[] { 0f, 0.3f, 0.65f, 1f },
            SKShaderTileMode.Clamp);
        using var p = new SKPaint { IsAntialias = true, Shader = shader, BlendMode = add ? SKBlendMode.Plus : SKBlendMode.SrcOver };
        c.DrawCircle(x, y, r, p);
    }

    /// <summary>An oval glow (light pooling on the ground, say), rx wide and ry tall.</summary>
    private static void WinterGlowOval(SKCanvas c, float x, float y, float rx, float ry, SKColor colour, bool add = false)
    {
        c.Save();
        c.Translate(x, y);
        c.Scale(1, ry / rx);
        WinterGlow(c, 0, 0, rx, colour, add);
        c.Restore();
    }

    /// <summary>A glowing point of light: wide soft halo, then a hot core.</summary>
    private static void WinterBulb(SKCanvas c, float x, float y, float r, SKColor colour, byte glow = 150)
    {
        WinterGlow(c, x, y, r * 5f, colour.WithAlpha(glow), add: true);
        using var core = new SKPaint { IsAntialias = true, Color = Lighten(colour, 0.45f) };
        c.DrawCircle(x, y, r, core);
        using var hot = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(230) };
        c.DrawCircle(x - (r * 0.15f), y - (r * 0.15f), r * 0.45f, hot);
    }

    /// <summary>An out-of-focus light: a soft disc with a slightly brighter rim.</summary>
    private static void WinterBokeh(SKCanvas c, float x, float y, float r, SKColor colour, byte alpha)
    {
        using var shader = SKShader.CreateRadialGradient(
            new SKPoint(x, y), r,
            new[] { Lighten(colour, 0.3f).WithAlpha((byte)(alpha * 0.6f)), colour.WithAlpha((byte)(alpha * 0.7f)), colour.WithAlpha(alpha), colour.WithAlpha(0) },
            new[] { 0f, 0.7f, 0.9f, 1f },
            SKShaderTileMode.Clamp);
        using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.06f);
        using var p = new SKPaint { IsAntialias = true, Shader = shader, MaskFilter = blur };
        c.DrawCircle(x, y, r, p);
    }

    /// <summary>A four-point twinkle: a glow with thin horizontal and vertical flares.</summary>
    private static void WinterTwinkle(SKCanvas c, float x, float y, float r, SKColor colour)
    {
        WinterGlow(c, x, y, r * 0.6f, colour.WithAlpha(160), add: true);
        using var flare = new SKPaint { IsAntialias = true, StrokeWidth = Math.Max(1f, r * 0.05f), StrokeCap = SKStrokeCap.Round, BlendMode = SKBlendMode.Plus };
        var clear = colour.WithAlpha(0);
        using (var hx = SKShader.CreateLinearGradient(new SKPoint(x - r, y), new SKPoint(x + r, y), new[] { clear, colour, clear }, null, SKShaderTileMode.Clamp))
        {
            flare.Shader = hx;
            c.DrawLine(x - r, y, x + r, y, flare);
        }

        using (var vy = SKShader.CreateLinearGradient(new SKPoint(x, y - r), new SKPoint(x, y + r), new[] { clear, colour, clear }, null, SKShaderTileMode.Clamp))
        {
            flare.Shader = vy;
            c.DrawLine(x, y - r, x, y + r, flare);
        }

        flare.Shader = null;
        using var dot = new SKPaint { IsAntialias = true, Color = SKColors.White };
        c.DrawCircle(x, y, Math.Max(1f, r * 0.06f), dot);
    }

    // ---------- sky ----------

    /// <summary>Stars, thicker towards the top, a few of them twinkling.</summary>
    private static void WinterStars(SKCanvas c, int w, float maxY, int count, float s, Random rng)
    {
        using var star = new SKPaint { IsAntialias = true };
        var tints = new[] { SKColors.White, new SKColor(0xCF, 0xE2, 0xFF), new SKColor(0xFF, 0xF1, 0xD0) };
        for (var i = 0; i < count; i++)
        {
            var y = maxY * (float)Math.Pow(rng.NextDouble(), 1.5);
            var fade = 1 - (y / maxY * 0.7f);
            star.Color = tints[rng.Next(tints.Length)].WithAlpha((byte)(rng.Next(90, 240) * fade));
            c.DrawCircle(R(rng, 0, w), y, R(rng, s * 0.0008f, s * 0.0026f), star);
        }

        for (var i = 0; i < Math.Max(2, count / 30); i++)
        {
            WinterTwinkle(c, R(rng, 0, w), maxY * (float)Math.Pow(rng.NextDouble(), 1.3) * 0.8f, s * R(rng, 0.012f, 0.024f), tints[rng.Next(tints.Length)]);
        }
    }

    /// <summary>
    /// Falling snow with depth: lots of fine far flakes, some nearer ones, and a few big blurred ones close to the
    /// lens. The near ones keep off the title band.
    /// </summary>
    private static void WinterSnowfall(SKCanvas c, int w, int h, float s, Random rng, int count, float maxY = 1f)
    {
        using var dot = new SKPaint { IsAntialias = true };
        for (var i = 0; i < count; i++)
        {
            dot.Color = WinterSnowWhite.WithAlpha((byte)rng.Next(70, 170));
            c.DrawCircle(R(rng, 0, w), R(rng, 0, h * maxY), R(rng, s * 0.0012f, s * 0.0026f), dot);
        }

        using var soft = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.0012f);
        dot.MaskFilter = soft;
        for (var i = 0; i < count / 3; i++)
        {
            float x = R(rng, 0, w), y = R(rng, 0, h * maxY);
            if (Math.Abs(y - (h * 0.5f)) < h * 0.13f && Math.Abs(x - (w * 0.5f)) < w * 0.42f)
            {
                continue;
            }

            dot.Color = WinterSnowWhite.WithAlpha((byte)rng.Next(170, 245));
            c.DrawCircle(x, y, R(rng, s * 0.003f, s * 0.0055f), dot);
        }

        dot.MaskFilter = null;
        for (var i = 0; i < Math.Max(3, count / 14); i++)
        {
            var r = s * R(rng, 0.008f, 0.022f);
            var y = rng.NextDouble() < 0.6 ? R(rng, 0, h * 0.3f) : R(rng, h * 0.7f, h * maxY);
            var x = rng.NextDouble() < 0.5 ? R(rng, 0, w * 0.3f) : R(rng, w * 0.7f, w);
            using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.45f);
            using var flake = new SKPaint { IsAntialias = true, MaskFilter = blur, Color = WinterSnowWhite.WithAlpha((byte)rng.Next(60, 120)) };
            c.DrawCircle(x, y, r, flake);
        }
    }

    /// <summary>A bright winter moon: pearly white with faint blue-grey seas, a crisp halo and a wide glow.</summary>
    private static void WinterMoon(SKCanvas c, float x, float y, float r, Random rng)
    {
        WinterGlow(c, x, y, r * 6f, new SKColor(0xB8, 0xD0, 0xFF, 60), add: true);
        WinterGlow(c, x, y, r * 2.2f, new SKColor(0xF0, 0xF4, 0xFF, 90), add: true);
        using (var shader = SKShader.CreateRadialGradient(new SKPoint(x - (r * 0.3f), y - (r * 0.3f)), r * 1.4f, new[] { SKColors.White, new SKColor(0xF4, 0xF2, 0xE8), new SKColor(0xD4, 0xD4, 0xD8) }, new[] { 0f, 0.6f, 1f }, SKShaderTileMode.Clamp))
        using (var body = new SKPaint { IsAntialias = true, Shader = shader })
        {
            c.DrawCircle(x, y, r, body);
        }

        using var disc = new SKPath();
        disc.AddCircle(x, y, r);
        c.Save();
        c.ClipPath(disc, antialias: true);
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.07f))
        using (var sea = new SKPaint { IsAntialias = true, Color = new SKColor(0x9C, 0xA6, 0xB8, 70), MaskFilter = blur })
        {
            foreach (var (dx, dy, sr) in new[] { (-0.25f, -0.2f, 0.3f), (0.2f, 0.1f, 0.26f), (-0.05f, 0.4f, 0.2f), (0.35f, -0.35f, 0.14f) })
            {
                var jitter = R(rng, 0.85f, 1.15f);
                c.DrawOval(new SKRect(x + (r * (dx - (sr * jitter))), y + (r * (dy - (sr * 0.8f))), x + (r * (dx + (sr * jitter))), y + (r * (dy + (sr * 0.8f)))), sea);
            }
        }

        c.Restore();
    }

    /// <summary>A snowman with a top hat, red scarf, coal buttons and stick arms; height is to the top of the hat.</summary>
    private static void WinterSnowman(SKCanvas c, float x, float ground, float height)
    {
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, height * 0.03f))
        using (var shadow = new SKPaint { IsAntialias = true, Color = new SKColor(0x40, 0x50, 0x80, 90), MaskFilter = blur })
        {
            c.DrawOval(new SKRect(x - (height * 0.2f), ground - (height * 0.03f), x + (height * 0.4f), ground + (height * 0.04f)), shadow);
        }

        using var stick = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = height * 0.02f, Color = new SKColor(0x4A, 0x30, 0x20) };
        foreach (var side in new[] { -1, 1 })
        {
            float sx = x + (side * height * 0.13f), sy = ground - (height * 0.5f);
            float ex = x + (side * height * 0.36f), ey = ground - (height * (side < 0 ? 0.66f : 0.6f));
            c.DrawLine(sx, sy, ex, ey, stick);
            c.DrawLine(x + (side * height * 0.29f), ground - (height * 0.6f), x + (side * height * 0.33f), ground - (height * 0.68f), stick);
        }

        foreach (var (cy, r) in new[] { (0.19f, 0.2f), (0.5f, 0.15f), (0.73f, 0.105f) })
        {
            using var shader = SKShader.CreateRadialGradient(new SKPoint(x - (height * r * 0.35f), ground - (height * cy) - (height * r * 0.4f)), height * r * 1.5f, new[] { SKColors.White, new SKColor(0xDE, 0xE8, 0xF6), new SKColor(0x90, 0xA6, 0xCC) }, new[] { 0f, 0.55f, 1f }, SKShaderTileMode.Clamp);
            using var ball = new SKPaint { IsAntialias = true, Shader = shader };
            c.DrawCircle(x, ground - (height * cy), height * r, ball);
        }

        using var coal = new SKPaint { IsAntialias = true, Color = new SKColor(0x1A, 0x1A, 0x22) };
        foreach (var by in new[] { 0.44f, 0.51f, 0.58f })
        {
            c.DrawCircle(x + (height * 0.01f), ground - (height * by), height * 0.014f, coal);
        }

        var head = ground - (height * 0.73f);
        c.DrawCircle(x - (height * 0.035f), head - (height * 0.02f), height * 0.012f, coal);
        c.DrawCircle(x + (height * 0.035f), head - (height * 0.02f), height * 0.012f, coal);
        using (var carrot = new SKPath())
        using (var orange = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0x7A, 0x1A) })
        {
            carrot.MoveTo(x, head + (height * 0.005f));
            carrot.LineTo(x + (height * 0.11f), head + (height * 0.02f));
            carrot.LineTo(x, head + (height * 0.03f));
            carrot.Close();
            c.DrawPath(carrot, orange);
        }

        // Scarf: a band round the neck with a tail hanging down.
        using (var red = new SKPaint { IsAntialias = true, Color = new SKColor(0xD3, 0x2F, 0x2F) })
        using (var stripe = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xF0, 0xE0, 200) })
        {
            c.DrawRoundRect(new SKRect(x - (height * 0.12f), head + (height * 0.07f), x + (height * 0.12f), head + (height * 0.12f)), height * 0.02f, height * 0.02f, red);
            var tail = new SKRect(x + (height * 0.04f), head + (height * 0.09f), x + (height * 0.1f), head + (height * 0.25f));
            c.DrawRoundRect(tail, height * 0.015f, height * 0.015f, red);
            c.DrawRect(tail.Left, tail.Top + (tail.Height * 0.45f), tail.Width, tail.Height * 0.12f, stripe);
            c.DrawRect(tail.Left, tail.Top + (tail.Height * 0.72f), tail.Width, tail.Height * 0.12f, stripe);
        }

        // Top hat with a red band.
        using (var hat = new SKPaint { IsAntialias = true, Color = new SKColor(0x16, 0x16, 0x1E) })
        using (var band = new SKPaint { IsAntialias = true, Color = new SKColor(0xC6, 0x28, 0x28) })
        {
            var brim = head - (height * 0.08f);
            c.DrawOval(new SKRect(x - (height * 0.13f), brim - (height * 0.02f), x + (height * 0.13f), brim + (height * 0.02f)), hat);
            c.DrawRect(x - (height * 0.08f), brim - (height * 0.15f), height * 0.16f, height * 0.15f, hat);
            c.DrawRect(x - (height * 0.08f), brim - (height * 0.04f), height * 0.16f, height * 0.025f, band);
        }
    }

    // ---------- ground ----------

    /// <summary>A gently rolling skyline for a layer of hills, as a function of x. waves is roughly bumps per s of width.</summary>
    private static Func<float, float> WinterRidgeLine(float baseY, float amp, float s, float waves, Random rng)
    {
        double p1 = R(rng, 0, 6.3f), p2 = R(rng, 0, 6.3f), p3 = R(rng, 0, 6.3f);
        var f = waves * Math.PI * 2 / s;
        return x => baseY - (amp * (float)((0.55 * Math.Sin((x * f) + p1)) + (0.3 * Math.Sin((x * f * 2.3) + p2)) + (0.15 * Math.Sin((x * f * 5.1) + p3))));
    }

    /// <summary>
    /// A layer of snowy ground under <paramref name="line"/>: a vertical gradient, with a soft lit rim along its crest
    /// (moonlight catching the snow) when <paramref name="rim"/> isn't transparent.
    /// </summary>
    private static SKPath WinterRidge(SKCanvas c, int w, int h, Func<float, float> line, SKColor top, SKColor bottom, SKColor rim, float s)
    {
        var path = new SKPath();
        path.MoveTo(-4, h + 4);
        var minY = float.MaxValue;
        for (float x = -4; x <= w + 10; x += 6)
        {
            var y = line(x);
            minY = Math.Min(minY, y);
            path.LineTo(x, y);
        }

        path.LineTo(w + 10, h + 4);
        path.Close();

        using (var shader = SKShader.CreateLinearGradient(new SKPoint(0, minY), new SKPoint(0, Math.Min(h, minY + (h * 0.25f))), new[] { top, bottom }, null, SKShaderTileMode.Clamp))
        using (var fill = new SKPaint { IsAntialias = true, Shader = shader })
        {
            c.DrawPath(path, fill);
        }

        if (rim.Alpha > 0)
        {
            using var edge = new SKPath();
            edge.MoveTo(-4, line(-4));
            for (float x = -4; x <= w + 10; x += 6)
            {
                edge.LineTo(x, line(x));
            }

            using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, s * 0.003f);
            using var pen = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.006f, Color = rim, MaskFilter = blur };
            c.Save();
            c.ClipPath(path, antialias: true);
            c.DrawPath(edge, pen);
            c.Restore();
        }

        return path;
    }

    /// <summary>Glints of light on fresh snow below <paramref name="line"/>.</summary>
    private static void WinterGlints(SKCanvas c, int w, int h, Func<float, float> line, float s, Random rng, int count)
    {
        using var dot = new SKPaint { IsAntialias = true };
        for (var i = 0; i < count; i++)
        {
            var x = R(rng, 0, w);
            var y = R(rng, line(x) + (s * 0.01f), h);
            dot.Color = SKColors.White.WithAlpha((byte)rng.Next(120, 230));
            c.DrawCircle(x, y, R(rng, s * 0.0012f, s * 0.0025f), dot);
            if (rng.NextDouble() < 0.08)
            {
                WinterTwinkle(c, x, y, s * 0.012f, new SKColor(0xE8, 0xF2, 0xFF));
            }
        }
    }

    /// <summary>Covers the upward edges of <paramref name="shape"/> in snow of the given colour, thicker on the left (lit) side.</summary>
    private static void WinterSnowCap(SKCanvas c, SKPath shape, SKPaint fill, float depth, SKColor snow)
    {
        using var white = new SKPaint { IsAntialias = true, Color = snow };
        c.Save();
        c.ClipPath(shape, antialias: true);
        c.DrawPath(shape, white);
        c.Translate(depth * 0.3f, depth);
        c.DrawPath(shape, fill);
        c.Restore();
    }

    /// <summary>
    /// A full, layered evergreen: drooping tiers with ragged lower edges, lit from the left, each casting a soft shadow
    /// on the one below, with snow along the top of every tier. Returns each tier (bottom, half width, tip) from the
    /// bottom up, so decorations can be hung on it.
    /// </summary>
    private static List<(float Bottom, float Half, float Tip)> WinterPine(
        SKCanvas c, float x, float ground, float height, SKColor dark, SKColor light, SKColor snowColour, float snow, Random rng, int tiers = 6, float spread = 0.36f)
    {
        var result = new List<(float Bottom, float Half, float Tip)>();
        using (var trunk = new SKPaint { IsAntialias = true, Color = WinterMix(dark, new SKColor(0x2A, 0x1A, 0x12, dark.Alpha), 0.6f) })
        {
            c.DrawRect(x - (height * 0.03f), ground - (height * 0.14f), height * 0.06f, height * 0.14f, trunk);
        }

        using var shadeBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, height * 0.012f);
        using var shade = new SKPaint { IsAntialias = true, Color = SKColors.Black.WithAlpha((byte)(80 * dark.Alpha / 255)), MaskFilter = shadeBlur };
        using var needles = new SKPaint { IsAntialias = true };
        SKPath? previous = null;
        for (var i = 0; i < tiers; i++)
        {
            var t = i / (float)tiers;
            var bottom = ground - (height * 0.1f) - (t * height * 0.72f);
            var half = height * spread * (1 - (t * 0.86f)) * R(rng, 0.94f, 1.04f);
            var tip = i == tiers - 1 ? ground - height : bottom - (height * (0.3f - (t * 0.1f)));
            var tier = new SKPath();
            tier.MoveTo(x, tip);
            tier.QuadTo(x + (half * 0.42f), tip + ((bottom - tip) * 0.62f), x + half, bottom + (height * 0.02f));
            var n = Math.Max(3, (int)(half / (height * 0.045f)));
            for (var k = 1; k <= n; k++)
            {
                var px = x + half - (2 * half * k / n);
                var mx = x + half - (2 * half * (k - 0.5f) / n);
                var py = k == n ? bottom + (height * 0.02f) : bottom + (height * R(rng, -0.004f, 0.016f));
                tier.QuadTo(mx, bottom - (height * R(rng, 0.025f, 0.045f)), px, py);
            }

            tier.QuadTo(x - (half * 0.42f), tip + ((bottom - tip) * 0.62f), x, tip);
            tier.Close();

            if (previous is not null)
            {
                c.Save();
                c.ClipPath(previous, antialias: true);
                c.Translate(0, height * 0.025f);
                c.DrawPath(tier, shade);
                c.Restore();
                previous.Dispose();
            }

            using var shader = SKShader.CreateLinearGradient(new SKPoint(x - half, tip), new SKPoint(x + half, bottom), new[] { light, dark }, null, SKShaderTileMode.Clamp);
            needles.Shader = shader;
            c.DrawPath(tier, needles);
            if (snow > 0)
            {
                WinterSnowCap(c, tier, needles, height * snow, snowColour);
            }

            needles.Shader = null;
            previous = tier;
            result.Add((bottom, half, tip));
        }

        previous?.Dispose();
        return result;
    }

    /// <summary>A row of pines along a ridge, drawn as one softly blurred layer (out of focus, so it sits back).</summary>
    private static void WinterTreeLine(SKCanvas c, int w, Func<float, float> line, float s, Random rng, float minH, float maxH, float gap, SKColor dark, SKColor light, SKColor snow, float blur, float sink = 0.01f)
    {
        using var filter = blur > 0 ? SKImageFilter.CreateBlur(blur, blur) : null;
        using var layer = new SKPaint { ImageFilter = filter };
        c.SaveLayer(layer);
        for (var x = R(rng, -gap, 0); x < w + gap; x += gap * R(rng, 0.55f, 1.3f))
        {
            var height = R(rng, minH, maxH);
            WinterPine(c, x, line(x) + (s * sink), height, dark, light, snow, R(rng, 0.03f, 0.07f), rng, 4 + rng.Next(2));
        }

        c.Restore();
    }

    // ---------- buildings ----------

    /// <summary>Chimney smoke: soft puffs rising and drifting to one side, fading as they go.</summary>
    private static void WinterSmoke(SKCanvas c, float x, float y, float size, Random rng, byte alpha = 70, float drift = 1f)
    {
        for (var i = 0; i < 10; i++)
        {
            var t = i / 9f;
            var r = size * (0.1f + (0.32f * t));
            var cx = x + (drift * size * 0.9f * t * t) + R(rng, -size * 0.05f, size * 0.05f);
            var cy = y - (size * 1.5f * t);
            using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.6f);
            using var puff = new SKPaint { IsAntialias = true, MaskFilter = blur, Color = new SKColor(0xC8, 0xD0, 0xE0, (byte)(alpha * (1 - (t * 0.85f)))) };
            c.DrawCircle(cx, cy, r, puff);
        }
    }

    /// <summary>A lit window: warm glass, dark frame and cross bars, a snowy sill, a halo and light spilling below.</summary>
    private static void WinterWindow(SKCanvas c, SKRect r, float s, bool sill = true)
    {
        WinterGlow(c, r.MidX, r.MidY, Math.Max(r.Width, r.Height) * 2.2f, new SKColor(0xFF, 0xB0, 0x50, 70), add: true);
        using (var frame = new SKPaint { IsAntialias = true, Color = new SKColor(0x2A, 0x18, 0x10) })
        {
            var f = r;
            f.Inflate(r.Width * 0.12f, r.Width * 0.12f);
            c.DrawRect(f, frame);
        }

        using (var shader = SKShader.CreateRadialGradient(new SKPoint(r.MidX, r.MidY + (r.Height * 0.2f)), Math.Max(r.Width, r.Height) * 0.8f, new[] { new SKColor(0xFF, 0xF0, 0xB8), new SKColor(0xFF, 0xB8, 0x4E), new SKColor(0xE0, 0x78, 0x28) }, new[] { 0f, 0.55f, 1f }, SKShaderTileMode.Clamp))
        using (var glass = new SKPaint { IsAntialias = true, Shader = shader })
        {
            c.DrawRect(r, glass);
        }

        using (var bar = new SKPaint { IsAntialias = true, Color = new SKColor(0x2A, 0x18, 0x10), StrokeWidth = Math.Max(1.5f, r.Width * 0.09f) })
        {
            c.DrawLine(r.MidX, r.Top, r.MidX, r.Bottom, bar);
            c.DrawLine(r.Left, r.MidY, r.Right, r.MidY, bar);
        }

        if (sill)
        {
            using var snow = new SKPaint { IsAntialias = true, Color = WinterSnowWhite };
            c.DrawRoundRect(new SKRect(r.Left - (r.Width * 0.22f), r.Bottom + (r.Width * 0.06f), r.Right + (r.Width * 0.22f), r.Bottom + (r.Width * 0.22f)), r.Width * 0.08f, r.Width * 0.08f, snow);
        }
    }

    /// <summary>
    /// A log cottage seen side-on: thick snow on the roof with icicles under the eaves, a smoking chimney, and warm
    /// windows whose light pools on the snow in front. size is its width; (x, ground) the middle of its base.
    /// </summary>
    private static void WinterCottage(SKCanvas c, float x, float ground, float size, Random rng, float s, bool chimneyRight = true)
    {
        var half = size * 0.5f;
        var wallTop = ground - (size * 0.46f);
        var ridge = wallTop - (size * 0.36f);

        // Light from the windows, pooled on the snow.
        WinterGlowOval(c, x, ground + (size * 0.05f), size * 0.9f, size * 0.13f, new SKColor(0xFF, 0xB0, 0x55, 110), add: true);

        // Chimney, behind the roof.
        var chX = x + (chimneyRight ? half * 0.5f : -half * 0.5f);
        var chTop = ridge - (size * 0.06f);
        using (var brick = new SKPaint { IsAntialias = true, Color = new SKColor(0x4A, 0x26, 0x22) })
        {
            c.DrawRect(chX - (size * 0.055f), chTop, size * 0.11f, wallTop - chTop, brick);
        }

        WinterSmoke(c, chX, chTop - (size * 0.05f), size * 0.4f, rng, 60, chimneyRight ? 1 : -1);
        using (var cap = new SKPaint { IsAntialias = true, Color = WinterSnowWhite })
        {
            c.DrawRoundRect(new SKRect(chX - (size * 0.07f), chTop - (size * 0.03f), chX + (size * 0.07f), chTop + (size * 0.015f)), size * 0.02f, size * 0.02f, cap);
        }

        // Log walls, darker towards the right.
        var wall = new SKRect(x - half, wallTop, x + half, ground);
        using (var shader = SKShader.CreateLinearGradient(new SKPoint(wall.Left, 0), new SKPoint(wall.Right, 0), new[] { new SKColor(0x6E, 0x40, 0x2A), new SKColor(0x3A, 0x20, 0x16) }, null, SKShaderTileMode.Clamp))
        using (var wood = new SKPaint { IsAntialias = true, Shader = shader })
        {
            c.DrawRect(wall, wood);
        }

        using (var seam = new SKPaint { IsAntialias = true, StrokeWidth = Math.Max(1f, size * 0.008f), Color = new SKColor(0x1E, 0x10, 0x0A, 160) })
        {
            for (var y = wallTop + (size * 0.055f); y < ground; y += size * 0.055f)
            {
                c.DrawLine(wall.Left, y, wall.Right, y, seam);
            }
        }

        // Windows either side of the door; the door has a wreath.
        var winW = size * 0.15f;
        foreach (var wx in new[] { x - (half * 0.55f), x + (half * 0.55f) })
        {
            WinterWindow(c, new SKRect(wx - (winW / 2), wallTop + (size * 0.1f), wx + (winW / 2), wallTop + (size * 0.1f) + (winW * 1.1f)), s);
        }

        var door = new SKRect(x - (size * 0.075f), ground - (size * 0.3f), x + (size * 0.075f), ground);
        using (var doorPaint = new SKPaint { IsAntialias = true, Color = new SKColor(0x2C, 0x16, 0x10) })
        {
            c.DrawRoundRect(door, size * 0.02f, size * 0.02f, doorPaint);
        }

        using (var wreath = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = size * 0.018f, Color = new SKColor(0x2E, 0x7D, 0x32) })
        using (var bow = new SKPaint { IsAntialias = true, Color = new SKColor(0xE5, 0x39, 0x35) })
        {
            c.DrawCircle(door.MidX, door.Top + (door.Height * 0.3f), size * 0.035f, wreath);
            c.DrawCircle(door.MidX, door.Top + (door.Height * 0.3f) + (size * 0.035f), size * 0.012f, bow);
        }

        using (var doorLight = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xC0, 0x60, 200) })
        {
            c.DrawCircle(door.Right - (door.Width * 0.22f), door.MidY + (door.Height * 0.1f), size * 0.008f, doorLight);
        }

        // Roof under a deep, rounded blanket of snow that overhangs the eaves.
        var eaveL = x - (half * 1.14f);
        var eaveR = x + (half * 1.14f);
        var eaveY = wallTop + (size * 0.03f);
        using (var fascia = new SKPaint { IsAntialias = true, Color = new SKColor(0x24, 0x16, 0x14) })
        {
            c.DrawRect(eaveL + (size * 0.02f), eaveY - (size * 0.03f), eaveR - eaveL - (size * 0.04f), size * 0.045f, fascia);
        }

        using var roof = new SKPath();
        roof.MoveTo(eaveL, eaveY);
        roof.LineTo(x - (half * 0.78f), ridge + (size * 0.02f));
        roof.QuadTo(x, ridge - (size * 0.07f), x + (half * 0.78f), ridge + (size * 0.02f));
        roof.LineTo(eaveR, eaveY);
        var lumps = 7;
        for (var k = 1; k <= lumps; k++)
        {
            var px = eaveR - ((eaveR - eaveL) * k / lumps);
            var mx = eaveR - ((eaveR - eaveL) * (k - 0.5f) / lumps);
            roof.QuadTo(mx, eaveY + (size * R(rng, 0.03f, 0.05f)), px, eaveY);
        }

        roof.Close();
        using (var shader = SKShader.CreateLinearGradient(new SKPoint(0, ridge - (size * 0.07f)), new SKPoint(0, eaveY + (size * 0.04f)), new[] { SKColors.White, new SKColor(0xE2, 0xEA, 0xF8), new SKColor(0xA8, 0xBC, 0xDC) }, new[] { 0f, 0.6f, 1f }, SKShaderTileMode.Clamp))
        using (var snow = new SKPaint { IsAntialias = true, Shader = shader, ImageFilter = SKImageFilter.CreateDropShadow(0, size * 0.01f, size * 0.015f, size * 0.015f, SKColors.Black.WithAlpha(90)) })
        {
            c.DrawPath(roof, snow);
        }

        // Icicles.
        using var ice = new SKPaint { IsAntialias = true, Color = new SKColor(0xDD, 0xEE, 0xFF, 220) };
        for (var ix = eaveL + (size * 0.06f); ix < eaveR - (size * 0.04f); ix += size * R(rng, 0.035f, 0.07f))
        {
            var len = size * R(rng, 0.03f, 0.09f);
            using var icicle = new SKPath();
            icicle.MoveTo(ix - (size * 0.01f), eaveY + (size * 0.02f));
            icicle.LineTo(ix + (size * 0.01f), eaveY + (size * 0.02f));
            icicle.LineTo(ix, eaveY + (size * 0.02f) + len);
            icicle.Close();
            c.DrawPath(icicle, ice);
        }

        // A drift banked against the foot of the walls.
        using var drift = new SKPaint { IsAntialias = true, Color = WinterSnowWhite };
        c.DrawOval(new SKRect(x - (half * 1.25f), ground - (size * 0.04f), x - (half * 0.2f), ground + (size * 0.04f)), drift);
        c.DrawOval(new SKRect(x + (half * 0.25f), ground - (size * 0.035f), x + (half * 1.25f), ground + (size * 0.04f)), drift);
    }

    // ---------- Christmas things ----------

    /// <summary>A five-point star path.</summary>
    private static SKPath WinterStarPath(float cx, float cy, float outer, float inner)
    {
        var p = new SKPath();
        for (var i = 0; i < 10; i++)
        {
            var a = (-Math.PI / 2) + (i * Math.PI / 5);
            var r = i % 2 == 0 ? outer : inner;
            var px = cx + (float)(Math.Cos(a) * r);
            var py = cy + (float)(Math.Sin(a) * r);
            if (i == 0)
            {
                p.MoveTo(px, py);
            }
            else
            {
                p.LineTo(px, py);
            }
        }

        p.Close();
        return p;
    }

    /// <summary>A golden star with a glow and long flares, for the top of a tree.</summary>
    private static void WinterTopStar(SKCanvas c, float x, float y, float r)
    {
        WinterGlow(c, x, y, r * 4.5f, new SKColor(0xFF, 0xD8, 0x70, 120), add: true);
        WinterTwinkle(c, x, y, r * 3.2f, new SKColor(0xFF, 0xF0, 0xC0));
        using var star = WinterStarPath(x, y, r, r * 0.45f);
        using var shader = SKShader.CreateRadialGradient(new SKPoint(x - (r * 0.2f), y - (r * 0.3f)), r * 1.2f, new[] { new SKColor(0xFF, 0xFB, 0xE0), new SKColor(0xFF, 0xD0, 0x40), new SKColor(0xC8, 0x8A, 0x10) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp);
        using var gold = new SKPaint { IsAntialias = true, Shader = shader };
        c.DrawPath(star, gold);
    }

    /// <summary>
    /// A glossy glass bauble: deep colour with a lit side, a bright window reflection, a warm rim of reflected light
    /// at the bottom, and a gold cap. pattern 1 adds a glitter band, 2 stripes.
    /// </summary>
    private static void WinterBauble(SKCanvas c, float x, float y, float r, SKColor colour, int pattern = 0, Random? rng = null, bool cap = true, bool shadow = true)
    {
        using var disc = new SKPath();
        disc.AddCircle(x, y, r);
        using (var shader = SKShader.CreateRadialGradient(
            new SKPoint(x - (r * 0.35f), y - (r * 0.4f)), r * 1.55f,
            new[] { Lighten(colour, 0.5f), colour, WinterMix(colour, SKColors.Black, 0.55f), WinterMix(colour, SKColors.Black, 0.35f) },
            new[] { 0f, 0.35f, 0.82f, 1f },
            SKShaderTileMode.Clamp))
        using (var glass = new SKPaint { IsAntialias = true, Shader = shader })
        {
            if (shadow)
            {
                glass.ImageFilter = SKImageFilter.CreateDropShadow(r * 0.04f, r * 0.12f, r * 0.15f, r * 0.15f, SKColors.Black.WithAlpha(130));
            }

            c.DrawPath(disc, glass);
        }

        c.Save();
        c.ClipPath(disc, antialias: true);
        if (pattern == 1)
        {
            using var band = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0xE0, 0x90, 200) };
            c.DrawRect(x - r, y - (r * 0.16f), r * 2, r * 0.32f, band);
            if (rng is not null)
            {
                using var glitter = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(220) };
                for (var k = 0; k < 26; k++)
                {
                    c.DrawCircle(x + R(rng, -r, r), y + R(rng, -r * 0.16f, r * 0.16f), r * R(rng, 0.015f, 0.04f), glitter);
                }
            }
        }
        else if (pattern == 2)
        {
            using var stripe = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = r * 0.12f, Color = SKColors.White.WithAlpha(150) };
            for (var k = -2; k <= 2; k++)
            {
                c.DrawOval(new SKRect(x - (r * 1.1f), y + (k * r * 0.42f) - (r * 0.12f), x + (r * 1.1f), y + (k * r * 0.42f) + (r * 0.12f)), stripe);
            }
        }

        // Warm light reflected from below, and the bright window.
        using (var rim = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = r * 0.12f, Color = new SKColor(0xFF, 0xD8, 0xA0, 110), MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.06f) })
        {
            c.DrawArc(new SKRect(x - (r * 0.92f), y - (r * 0.92f), x + (r * 0.92f), y + (r * 0.92f)), 20, 110, false, rim);
        }

        c.Restore();

        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.05f))
        using (var shine = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(200), MaskFilter = blur })
        {
            c.Save();
            c.RotateDegrees(-35, x - (r * 0.4f), y - (r * 0.42f));
            c.DrawRoundRect(new SKRect(x - (r * 0.62f), y - (r * 0.58f), x - (r * 0.2f), y - (r * 0.3f)), r * 0.1f, r * 0.1f, shine);
            c.Restore();
            shine.Color = SKColors.White.WithAlpha(120);
            c.DrawCircle(x + (r * 0.3f), y + (r * 0.45f), r * 0.07f, shine);
        }

        if (cap)
        {
            using var metal = SKShader.CreateLinearGradient(new SKPoint(x - (r * 0.22f), 0), new SKPoint(x + (r * 0.22f), 0), new[] { new SKColor(0xFF, 0xF1, 0xB8), new SKColor(0xD8, 0xA8, 0x3A), new SKColor(0x8A, 0x60, 0x14) }, null, SKShaderTileMode.Clamp);
            using var capPaint = new SKPaint { IsAntialias = true, Shader = metal };
            c.DrawRoundRect(new SKRect(x - (r * 0.22f), y - r - (r * 0.16f), x + (r * 0.22f), y - r + (r * 0.1f)), r * 0.05f, r * 0.05f, capPaint);
        }
    }

    /// <summary>A wrapped present standing on (x, ground): shaded box, overhanging lid, ribbon and a looped bow.</summary>
    private static void WinterPresent(SKCanvas c, float x, float ground, float bw, float bh, SKColor colour, SKColor ribbon, bool snowy = false)
    {
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, bw * 0.06f))
        using (var shadow = new SKPaint { IsAntialias = true, Color = SKColors.Black.WithAlpha(110), MaskFilter = blur })
        {
            c.DrawOval(new SKRect(x - (bw * 0.62f), ground - (bw * 0.05f), x + (bw * 0.62f), ground + (bw * 0.07f)), shadow);
        }

        var box = new SKRect(x - (bw / 2), ground - bh, x + (bw / 2), ground);
        using (var shader = SKShader.CreateLinearGradient(new SKPoint(box.Left, box.Top), new SKPoint(box.Right, box.Bottom), new[] { Lighten(colour, 0.2f), colour, WinterMix(colour, SKColors.Black, 0.45f) }, new[] { 0f, 0.45f, 1f }, SKShaderTileMode.Clamp))
        using (var paper = new SKPaint { IsAntialias = true, Shader = shader })
        {
            c.DrawRect(box, paper);
        }

        var lid = new SKRect(box.Left - (bw * 0.04f), box.Top - (bh * 0.02f), box.Right + (bw * 0.04f), box.Top + (bh * 0.2f));
        using (var shader = SKShader.CreateLinearGradient(new SKPoint(lid.Left, 0), new SKPoint(lid.Right, 0), new[] { Lighten(colour, 0.28f), WinterMix(colour, SKColors.Black, 0.25f) }, null, SKShaderTileMode.Clamp))
        using (var top = new SKPaint { IsAntialias = true, Shader = shader })
        {
            c.DrawRect(lid, top);
        }

        using (var under = new SKPaint { IsAntialias = true, Color = SKColors.Black.WithAlpha(70) })
        {
            c.DrawRect(box.Left, lid.Bottom, box.Width, bh * 0.03f, under);
        }

        var band = bw * 0.14f;
        using (var shader = SKShader.CreateLinearGradient(new SKPoint(x - (band / 2), 0), new SKPoint(x + (band / 2), 0), new[] { Lighten(ribbon, 0.5f), ribbon, WinterMix(ribbon, SKColors.Black, 0.3f) }, null, SKShaderTileMode.Clamp))
        using (var tape = new SKPaint { IsAntialias = true, Shader = shader })
        {
            c.DrawRect(x - (band / 2), lid.Top, band, ground - lid.Top, tape);
        }

        if (snowy)
        {
            using var snow = new SKPaint { IsAntialias = true, Color = WinterSnowWhite };
            c.DrawRoundRect(new SKRect(lid.Left, lid.Top - (bh * 0.06f), lid.Right, lid.Top + (bh * 0.05f)), bh * 0.05f, bh * 0.05f, snow);
        }

        // Bow: two loops, two tails and a knot.
        var bowY = lid.Top - (bw * 0.02f);
        using var loop = new SKPaint { IsAntialias = true, Color = ribbon };
        using var inner = new SKPaint { IsAntialias = true, Color = WinterMix(ribbon, SKColors.Black, 0.4f) };
        foreach (var side in new[] { -1, 1 })
        {
            c.Save();
            c.RotateDegrees(side * -28, x, bowY);
            var oval = new SKRect(side < 0 ? x - (bw * 0.3f) : x, bowY - (bw * 0.09f), side < 0 ? x : x + (bw * 0.3f), bowY + (bw * 0.05f));
            c.DrawOval(oval, loop);
            var hole = oval;
            hole.Inflate(-oval.Width * 0.28f, -oval.Height * 0.3f);
            hole.Offset(side * oval.Width * 0.05f, 0);
            c.DrawOval(hole, inner);
            c.Restore();

            using var tail = new SKPath();
            tail.MoveTo(x, bowY);
            tail.LineTo(x + (side * bw * 0.16f), bowY + (bw * 0.16f));
            tail.LineTo(x + (side * bw * 0.1f), bowY + (bw * 0.17f));
            tail.Close();
            c.DrawPath(tail, loop);
        }

        using var knot = new SKPaint { IsAntialias = true, Color = Lighten(ribbon, 0.25f) };
        c.DrawCircle(x, bowY - (bw * 0.01f), bw * 0.05f, knot);
    }

    /// <summary>A big satin bow centred on (x, y), size across, with two tails hanging down.</summary>
    private static void WinterBow(SKCanvas c, float x, float y, float size, SKColor colour)
    {
        using var shadow = SKImageFilter.CreateDropShadow(0, size * 0.04f, size * 0.05f, size * 0.05f, SKColors.Black.WithAlpha(140));
        using var shader = SKShader.CreateLinearGradient(new SKPoint(x, y - (size * 0.3f)), new SKPoint(x, y + (size * 0.5f)), new[] { Lighten(colour, 0.3f), colour, WinterMix(colour, SKColors.Black, 0.4f) }, new[] { 0f, 0.4f, 1f }, SKShaderTileMode.Clamp);
        using var satin = new SKPaint { IsAntialias = true, Shader = shader, ImageFilter = shadow };
        using var inner = new SKPaint { IsAntialias = true, Color = WinterMix(colour, SKColors.Black, 0.45f) };
        foreach (var side in new[] { -1, 1 })
        {
            using var tail = new SKPath();
            tail.MoveTo(x, y);
            tail.QuadTo(x + (side * size * 0.15f), y + (size * 0.35f), x + (side * size * 0.24f), y + (size * 0.62f));
            tail.LineTo(x + (side * size * 0.12f), y + (size * 0.56f));
            tail.QuadTo(x + (side * size * 0.06f), y + (size * 0.3f), x - (side * size * 0.04f), y + (size * 0.04f));
            tail.Close();
            c.DrawPath(tail, satin);
        }

        foreach (var side in new[] { -1, 1 })
        {
            using var loop = new SKPath();
            loop.MoveTo(x, y);
            loop.CubicTo(x + (side * size * 0.2f), y - (size * 0.38f), x + (side * size * 0.55f), y - (size * 0.25f), x + (side * size * 0.45f), y + (size * 0.02f));
            loop.CubicTo(x + (side * size * 0.38f), y + (size * 0.16f), x + (side * size * 0.15f), y + (size * 0.12f), x, y);
            loop.Close();
            c.DrawPath(loop, satin);
            c.DrawOval(new SKRect(x + (side * size * 0.12f) - (size * 0.07f), y - (size * 0.1f), x + (side * size * 0.12f) + (size * 0.07f), y + (size * 0.02f)), inner);
        }

        using var knot = new SKPaint { IsAntialias = true, Color = Lighten(colour, 0.15f) };
        c.DrawRoundRect(new SKRect(x - (size * 0.07f), y - (size * 0.08f), x + (size * 0.07f), y + (size * 0.08f)), size * 0.03f, size * 0.03f, knot);
    }

    /// <summary>A string of fairy lights drooping from <paramref name="from"/> to <paramref name="to"/>, sagging by <paramref name="sag"/>.</summary>
    private static void WinterLightString(SKCanvas c, SKPoint from, SKPoint to, float sag, int bulbs, float s, Random rng, SKColor[]? colours = null)
    {
        colours ??= WinterBulbColours;
        var ctrl = new SKPoint((from.X + to.X) / 2, ((from.Y + to.Y) / 2) + (sag * 2));
        using (var wire = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.0025f, Color = new SKColor(0x10, 0x18, 0x12, 220) })
        using (var path = new SKPath())
        {
            path.MoveTo(from);
            path.QuadTo(ctrl, to);
            c.DrawPath(path, wire);
        }

        var offset = rng.Next(colours.Length);
        for (var k = 0; k < bulbs; k++)
        {
            var t = (k + 0.5f) / bulbs;
            var px = ((1 - t) * (1 - t) * from.X) + (2 * (1 - t) * t * ctrl.X) + (t * t * to.X);
            var py = ((1 - t) * (1 - t) * from.Y) + (2 * (1 - t) * t * ctrl.Y) + (t * t * to.Y);
            var colour = colours[(k + offset) % colours.Length];
            using (var socket = new SKPaint { IsAntialias = true, Color = new SKColor(0x1A, 0x22, 0x1A) })
            {
                c.DrawRect(px - (s * 0.003f), py, s * 0.006f, s * 0.008f, socket);
            }

            WinterGlow(c, px, py + (s * 0.017f), s * 0.05f, colour.WithAlpha(110), add: true);
            using var bulb = new SKPaint { IsAntialias = true, Color = Lighten(colour, 0.35f) };
            c.DrawOval(new SKRect(px - (s * 0.006f), py + (s * 0.006f), px + (s * 0.006f), py + (s * 0.026f)), bulb);
            using var hot = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(220) };
            c.DrawOval(new SKRect(px - (s * 0.0025f), py + (s * 0.012f), px + (s * 0.0025f), py + (s * 0.022f)), hot);
        }
    }

    /// <summary>A leafy pine bough along a line of points: layers of needles, darker behind, lighter on top.</summary>
    private static void WinterBough(SKCanvas c, SKPoint[] spine, float thickness, Random rng, float density = 1f)
    {
        using (var twig = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = thickness * 0.08f, Color = new SKColor(0x3A, 0x26, 0x18) })
        using (var path = new SKPath())
        {
            path.MoveTo(spine[0]);
            for (var i = 1; i < spine.Length; i++)
            {
                path.LineTo(spine[i]);
            }

            c.DrawPath(path, twig);
        }

        var layers = new[]
        {
            (Colours: new[] { new SKColor(0x0A, 0x2E, 0x18), new SKColor(0x0E, 0x38, 0x1E) }, Len: 1f, Width: 0.045f),
            (Colours: new[] { new SKColor(0x1A, 0x55, 0x2E), new SKColor(0x22, 0x66, 0x36) }, Len: 0.85f, Width: 0.04f),
            (Colours: new[] { new SKColor(0x3A, 0x86, 0x4E), new SKColor(0x5C, 0xA0, 0x62) }, Len: 0.55f, Width: 0.03f)
        };
        using var needle = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round };
        foreach (var layer in layers)
        {
            needle.StrokeWidth = Math.Max(1.2f, thickness * layer.Width);
            for (var i = 0; i < spine.Length - 1; i++)
            {
                SKPoint a = spine[i], b = spine[i + 1];
                var len = (float)Math.Sqrt(((b.X - a.X) * (b.X - a.X)) + ((b.Y - a.Y) * (b.Y - a.Y)));
                var dir = Math.Atan2(b.Y - a.Y, b.X - a.X);
                var steps = (int)Math.Max(1, len / (thickness * 0.07f / density));
                for (var k = 0; k < steps; k++)
                {
                    var t = k / (float)steps;
                    var px = a.X + ((b.X - a.X) * t);
                    var py = a.Y + ((b.Y - a.Y) * t);
                    for (var n = 0; n < 2; n++)
                    {
                        var side = rng.NextDouble() < 0.5 ? -1 : 1;
                        var ang = dir + (side * R(rng, 0.5f, 1.35f));
                        var l = thickness * layer.Len * R(rng, 0.35f, 0.6f);
                        var sx = px + R(rng, -thickness * 0.05f, thickness * 0.05f);
                        var sy = py + R(rng, -thickness * 0.05f, thickness * 0.05f);
                        needle.Color = layer.Colours[rng.Next(layer.Colours.Length)];
                        c.DrawLine(sx, sy, sx + (float)(Math.Cos(ang) * l), sy + (float)(Math.Sin(ang) * l), needle);
                    }
                }
            }
        }
    }

    /// <summary>A glossy holly leaf from (x, y) pointing along <paramref name="angle"/> degrees.</summary>
    private static void WinterHollyLeaf(SKCanvas c, float x, float y, float len, float angle)
    {
        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(angle);
        const int Spikes = 4;
        float Half(float t) => len * 0.2f * (float)Math.Pow(Math.Sin(Math.PI * Math.Clamp(t, 0, 1)), 0.7);

        using var leaf = new SKPath();
        leaf.MoveTo(0, 0);
        for (var k = 1; k <= Spikes; k++)
        {
            var t = k / (Spikes + 0.6f);
            var tm = (k - 0.5f) / (Spikes + 0.6f);
            leaf.QuadTo(len * tm, -Half(tm) * 0.55f, len * t, -Half(t) * 1.25f);
        }

        leaf.QuadTo(len * 0.92f, -len * 0.04f, len, 0);
        for (var k = Spikes; k >= 1; k--)
        {
            var t = (k - 0.25f) / (Spikes + 0.6f);
            var tm = (k + 0.25f) / (Spikes + 0.6f);
            leaf.QuadTo(len * Math.Min(tm, 0.95f), Half(tm) * 0.55f, len * t, Half(t) * 1.25f);
        }

        leaf.QuadTo(len * 0.05f, len * 0.05f, 0, 0);
        leaf.Close();

        using (var shader = SKShader.CreateLinearGradient(new SKPoint(0, -len * 0.25f), new SKPoint(len * 0.3f, len * 0.25f), new[] { new SKColor(0x3C, 0x9A, 0x55), new SKColor(0x17, 0x5C, 0x2E), new SKColor(0x0A, 0x33, 0x18) }, new[] { 0f, 0.45f, 1f }, SKShaderTileMode.Clamp))
        using (var fill = new SKPaint { IsAntialias = true, Shader = shader, ImageFilter = SKImageFilter.CreateDropShadow(len * 0.01f, len * 0.04f, len * 0.04f, len * 0.04f, SKColors.Black.WithAlpha(140)) })
        {
            c.DrawPath(leaf, fill);
        }

        c.Save();
        c.ClipPath(leaf, antialias: true);
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, len * 0.03f))
        using (var sheen = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(70), MaskFilter = blur })
        {
            c.DrawOval(new SKRect(len * 0.15f, -len * 0.16f, len * 0.7f, -len * 0.04f), sheen);
        }

        c.Restore();
        using (var rib = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = len * 0.018f, Color = new SKColor(0xA5, 0xD6, 0x8A, 150) })
        using (var mid = new SKPath())
        {
            mid.MoveTo(0, 0);
            mid.QuadTo(len * 0.5f, -len * 0.02f, len * 0.92f, 0);
            c.DrawPath(mid, rib);
        }

        c.Restore();
    }

    /// <summary>A shiny red berry.</summary>
    private static void WinterBerry(SKCanvas c, float x, float y, float r)
    {
        using (var shader = SKShader.CreateRadialGradient(new SKPoint(x - (r * 0.35f), y - (r * 0.35f)), r * 1.4f, new[] { new SKColor(0xFF, 0x6E, 0x6E), new SKColor(0xD3, 0x1B, 0x1B), new SKColor(0x5A, 0x06, 0x0A) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp))
        using (var berry = new SKPaint { IsAntialias = true, Shader = shader, ImageFilter = SKImageFilter.CreateDropShadow(0, r * 0.15f, r * 0.2f, r * 0.2f, SKColors.Black.WithAlpha(140)) })
        {
            c.DrawCircle(x, y, r, berry);
        }

        using var shine = new SKPaint { IsAntialias = true, Color = SKColors.White.WithAlpha(210) };
        c.DrawCircle(x - (r * 0.35f), y - (r * 0.35f), r * 0.2f, shine);
    }

    /// <summary>A holly sprig: three glossy leaves fanned from a point, with a cluster of berries over them.</summary>
    private static void WinterHollySprig(SKCanvas c, float x, float y, float size, float rotation, Random rng)
    {
        foreach (var a in new[] { -42f, 0f, 44f })
        {
            WinterHollyLeaf(c, x, y, size * R(rng, 0.9f, 1.05f), rotation + a + R(rng, -6, 6));
        }

        var br = size * 0.075f;
        foreach (var (dx, dy) in new[] { (-0.6f, -0.3f), (0.7f, -0.5f), (0.1f, 0.6f), (0.9f, 0.5f) })
        {
            WinterBerry(c, x + (dx * br * 1.6f), y + (dy * br * 1.6f), br * R(rng, 0.9f, 1.1f));
        }
    }

    /// <summary>An ice-crystal snowflake: six feathered arms with a hexagon heart, softly glowing.</summary>
    private static void WinterCrystal(SKCanvas c, float x, float y, float r, float rotation, byte alpha, float blur = 0)
    {
        using var path = new SKPath();
        for (var arm = 0; arm < 6; arm++)
        {
            var a = (rotation + (arm * 60)) * Math.PI / 180;
            float dx = (float)Math.Cos(a), dy = (float)Math.Sin(a);
            path.MoveTo(x + (dx * r * 0.16f), y + (dy * r * 0.16f));
            path.LineTo(x + (dx * r), y + (dy * r));
            foreach (var (at, len) in new[] { (0.32f, 0.2f), (0.55f, 0.3f), (0.78f, 0.18f) })
            {
                var bx = x + (dx * r * at);
                var by = y + (dy * r * at);
                foreach (var side in new[] { -1, 1 })
                {
                    var b = a + (side * Math.PI / 3);
                    path.MoveTo(bx, by);
                    path.LineTo(bx + ((float)Math.Cos(b) * r * len), by + ((float)Math.Sin(b) * r * len));
                }
            }
        }

        for (var k = 0; k <= 6; k++)
        {
            var a = ((rotation + 30) + (k * 60)) * Math.PI / 180;
            var px = x + ((float)Math.Cos(a) * r * 0.18f);
            var py = y + ((float)Math.Sin(a) * r * 0.18f);
            if (k == 0)
            {
                path.MoveTo(px, py);
            }
            else
            {
                path.LineTo(px, py);
            }
        }

        using var glowBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, (r * 0.06f) + blur);
        using var glow = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = r * 0.12f, Color = new SKColor(0xB8, 0xD8, 0xFF, (byte)(alpha * 0.45f)), MaskFilter = glowBlur };
        c.DrawPath(path, glow);
        using var lineBlur = blur > 0 ? SKMaskFilter.CreateBlur(SKBlurStyle.Normal, blur) : null;
        using var line = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round, StrokeWidth = r * 0.05f, Color = new SKColor(0xF4, 0xFA, 0xFF, alpha), MaskFilter = lineBlur };
        c.DrawPath(path, line);
    }

    // ---------- Snowfall: a moonlit valley, hills receding into the mist, a cottage with its windows lit ----------

    /// <summary>The Snow theme's first scene. With <paramref name="backdropOnly"/>, just the sky and far hills (the Christmas tree scene builds on them).</summary>
    private static void WinterSnowScene(SKCanvas c, int w, int h, Random rng, bool backdropOnly)
    {
        float s = Math.Min(w, h);
        var wide = w > h;

        // Night sky, deepest at the top.
        WinterWash(c, w, 0, h * 0.8f, new[] { new SKColor(0x04, 0x0B, 0x22, 225), new SKColor(0x0C, 0x1E, 0x42, 170), new SKColor(0x2C, 0x48, 0x78, 90) }, new[] { 0f, 0.5f, 1f });
        WinterStars(c, w, h * 0.5f, wide ? 150 : 120, s, rng);

        // The tree scene has lights strung along the top, so its moon sits lower, over on the right.
        var moon = backdropOnly
            ? new SKPoint(w * (wide ? 0.6f : 0.84f), h * (wide ? 0.2f : 0.25f))
            : new SKPoint(w * (wide ? 0.13f : 0.18f), h * 0.17f);
        var mr = s * (backdropOnly ? 0.055f : 0.07f);
        WinterMoon(c, moon.X, moon.Y, mr, rng);

        // Far hills, pale and misty.
        var far = WinterRidgeLine(h * 0.72f, h * 0.05f, s, 0.9f, rng);
        using (WinterRidge(c, w, h, far, new SKColor(0x6A, 0x86, 0xB4, 235), new SKColor(0x47, 0x60, 0x90, 235), new SKColor(0xD8, 0xE6, 0xFF, 140), s))
        {
        }

        WinterTreeLine(c, w, far, s, rng, s * 0.035f, s * 0.06f, s * 0.05f, new SKColor(0x2C, 0x42, 0x6A), new SKColor(0x3E, 0x58, 0x84), new SKColor(0x9C, 0xB2, 0xD6), s * 0.002f);
        Mist(c, w, h, h * 0.7f, s, rng, 45);
        if (backdropOnly)
        {
            return;
        }

        // Crystals hanging in the sky, clear of the moon; one big, out of focus, close to the lens.
        var placed = new List<(float X, float Y, float R)> { (moon.X, moon.Y, mr * 2.2f) };
        for (var i = 0; i < (wide ? 4 : 3); i++)
        {
            var r = s * R(rng, 0.035f, 0.055f);
            if (FindClear(rng, placed, r, w * 0.3f, w * 0.95f, h * 0.07f, h * 0.27f, 1.6f) is { } at)
            {
                WinterCrystal(c, at.X, at.Y, r, R(rng, 0, 60), 220);
                placed.Add((at.X, at.Y, r));
            }
        }

        WinterCrystal(c, w * 0.96f, h * 0.06f, s * 0.15f, 12, 70, s * 0.006f);

        // Middle hills with a cottage and pines.
        var mid = WinterRidgeLine(h * 0.82f, h * 0.035f, s, 0.7f, rng);
        using (WinterRidge(c, w, h, mid, new SKColor(0xB4, 0xC8, 0xE6), new SKColor(0x86, 0x9E, 0xC8), new SKColor(0xFF, 0xFF, 0xFF, 160), s))
        {
        }

        var cottageX = w * (wide ? 0.74f : 0.7f);
        var cottageSize = s * 0.2f;
        for (var i = 0; i < (wide ? 7 : 5); i++)
        {
            var px = R(rng, 0, w);
            if (Math.Abs(px - cottageX) < cottageSize * 0.8f)
            {
                continue;
            }

            WinterPine(c, px, mid(px) + (s * 0.01f), s * R(rng, 0.08f, 0.13f), new SKColor(0x1E, 0x3A, 0x48), new SKColor(0x3C, 0x60, 0x6E), new SKColor(0xDE, 0xEA, 0xFA), 0.05f, rng, 5);
        }

        WinterCottage(c, cottageX, mid(cottageX) + (s * 0.012f), cottageSize, rng, s);
        WinterPine(c, cottageX + (cottageSize * 0.78f), mid(cottageX + (cottageSize * 0.78f)) + (s * 0.015f), s * 0.17f, new SKColor(0x12, 0x30, 0x26), new SKColor(0x2C, 0x5A, 0x44), WinterSnowWhite, 0.06f, rng, 6);

        // Foreground drift with big pines framing the left.
        var near = WinterRidgeLine(h * 0.92f, h * 0.025f, s, 0.6f, rng);
        using (WinterRidge(c, w, h, near, WinterSnowWhite, new SKColor(0xC4, 0xD4, 0xEE), SKColors.Transparent, s))
        {
        }

        WinterPine(c, w * 0.05f, h * 1.02f, h * 0.36f, new SKColor(0x0A, 0x22, 0x1A), new SKColor(0x22, 0x4A, 0x38), new SKColor(0xE6, 0xEE, 0xFA), 0.035f, rng, 7);
        WinterPine(c, w * 0.15f, h * 1.01f, h * 0.24f, new SKColor(0x0C, 0x28, 0x1E), new SKColor(0x26, 0x50, 0x3C), new SKColor(0xE6, 0xEE, 0xFA), 0.04f, rng, 6);
        WinterGlints(c, w, h, near, s, rng, 50);
        WinterSnowfall(c, w, h, s, rng, wide ? 220 : 170);
    }

    // ---------- New Year: fireworks bursting over a city skyline and the river ----------

    private static void WinterFireworksScene(SKCanvas c, int w, int h, Random rng)
    {
        float s = Math.Min(w, h);
        var wide = w > h;
        var water = h * 0.9f;

        WinterWash(c, w, 0, h, new[] { new SKColor(0x03, 0x05, 0x16, 230), new SKColor(0x0C, 0x0E, 0x30, 175), new SKColor(0x2A, 0x16, 0x46, 140), new SKColor(0x05, 0x06, 0x14, 230) }, new[] { 0f, 0.45f, 0.8f, 1f });
        WinterStars(c, w, h * 0.4f, 70, s, rng);

        var palette = new[]
        {
            new SKColor(0xFF, 0xC8, 0x50), new SKColor(0xFF, 0x4F, 0x7B), new SKColor(0x5C, 0xD6, 0xFF),
            new SKColor(0xB4, 0x8C, 0xFF), new SKColor(0x6E, 0xF2, 0xA8), new SKColor(0xFF, 0x8A, 0x3C)
        };

        // Shuffle the colours so neighbouring bursts never match.
        for (var i = palette.Length - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (palette[i], palette[j]) = (palette[j], palette[i]);
        }

        // A warm glow over the city at the horizon.
        WinterGlowOval(c, w / 2f, water - (h * 0.06f), w * 0.75f, h * 0.2f, new SKColor(0xFF, 0x7A, 0x50, 70), add: true);

        // Bursts: big ones high up framing the title, small ones low and far off, half behind the buildings.
        var bursts = new List<(float X, float Y, float R, SKColor Colour, int Kind)>
        {
            (w * (wide ? 0.13f : 0.2f), h * 0.17f, s * 0.16f, palette[0], 0),
            (w * (wide ? 0.84f : 0.78f), h * 0.14f, s * 0.17f, palette[1], 1),
            (w * 0.5f, h * 0.08f, s * 0.08f, new SKColor(0xFF, 0xD8, 0x80), 2),
            (w * (wide ? 0.34f : 0.06f), h * (wide ? 0.07f : 0.55f), s * (wide ? 0.07f : 0.06f), palette[2], 0),
            (w * (wide ? 0.08f : 0.94f), h * 0.62f, s * 0.06f, palette[3], 1)
        };
        if (wide)
        {
            bursts.Add((w * 0.64f, h * 0.06f, s * 0.07f, palette[4], 0));
            bursts.Add((w * 0.95f, h * 0.6f, s * 0.05f, palette[5], 2));
        }

        // Smoke left hanging in the sky, lit by the bursts.
        foreach (var b in bursts)
        {
            for (var k = 0; k < 4; k++)
            {
                var r = b.R * R(rng, 0.5f, 0.9f);
                using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, r * 0.5f);
                using var smoke = new SKPaint { IsAntialias = true, MaskFilter = blur, Color = WinterMix(new SKColor(0x60, 0x58, 0x80), b.Colour, 0.35f).WithAlpha(30) };
                c.DrawOval(new SKRect(b.X - r + R(rng, -b.R * 0.5f, b.R * 0.5f), b.Y + R(rng, -r * 0.2f, r * 0.6f), b.X + r + R(rng, -b.R * 0.5f, b.R * 0.5f), b.Y + R(rng, r * 0.5f, r * 1f)), smoke);
            }
        }

        foreach (var b in bursts)
        {
            // Trails stop short of the title: the high bursts only show the last stretch of the climb.
            var from = b.Y > h * 0.5f ? water - (h * 0.1f) : Math.Min(b.Y + (b.R * 1.4f), h * 0.32f);
            if (from > b.Y + (b.R * 0.3f))
            {
                WinterLaunchTrail(c, b.X + R(rng, -s * 0.01f, s * 0.01f), from, b.X, b.Y + (b.R * 0.15f), b.Colour, s, rng);
            }

            WinterFirework(c, b.X, b.Y, b.R, b.Colour, palette[(Array.IndexOf(palette, b.Colour) + 3) % palette.Length], b.Kind, rng);
        }

        // Far towers, a big wheel, then the near skyline with the clock tower at midnight.
        WinterCity(c, w, water, s, rng, h * 0.08f, h * 0.2f, new SKColor(0x1A, 0x1A, 0x3A), new SKColor(0xFF, 0xD8, 0x90, 120), 0.18f, true);
        var wheelX = w * (wide ? 0.2f : 0.18f);
        var wheelR = s * 0.13f;
        WinterFerrisWheel(c, wheelX, water - (wheelR * 1.2f), wheelR, s, rng);
        WinterCity(c, w, water, s, rng, h * 0.04f, h * 0.11f, new SKColor(0x09, 0x09, 0x16), new SKColor(0xFF, 0xD0, 0x80, 200), 0.3f, false);
        ClockTower(c, w * (wide ? 0.6f : 0.62f), water, s * 0.3f);

        // The river: dark water with the fireworks and windows rippling in it.
        WinterWash(c, w, water, h, new[] { new SKColor(0x0A, 0x0C, 0x24, 250), new SKColor(0x03, 0x04, 0x10, 255) });
        using var ripple = new SKPaint { IsAntialias = true, StrokeCap = SKStrokeCap.Round, BlendMode = SKBlendMode.Plus };
        foreach (var b in bursts)
        {
            for (var y = water + (s * 0.006f); y < h; y += s * 0.008f)
            {
                var depth = (y - water) / (h - water);
                var half = b.R * R(rng, 0.15f, 0.55f) * (1 - (depth * 0.5f));
                ripple.StrokeWidth = s * 0.003f;
                ripple.Color = b.Colour.WithAlpha((byte)(90 * (1 - depth) * Math.Min(1, b.R / (s * 0.1f))));
                var off = R(rng, -b.R * 0.15f, b.R * 0.15f);
                c.DrawLine(b.X - half + off, y, b.X + half + off, y, ripple);
            }
        }

        for (var i = 0; i < (wide ? 70 : 55); i++)
        {
            var y = R(rng, water + (s * 0.005f), h);
            var x = R(rng, 0, w);
            ripple.StrokeWidth = s * 0.0025f;
            ripple.Color = new SKColor(0xFF, 0xD0, 0x80, (byte)rng.Next(40, 110));
            c.DrawLine(x, y, x + (s * R(rng, 0.008f, 0.03f)), y, ripple);
        }

        RiverWall(c, w, water, s, new SKColor(0x2A, 0x28, 0x40), new SKColor(0xFF, 0xD8, 0x90, 120));

        // Glittering sparks drifting down, off the title.
        using var spark = new SKPaint { IsAntialias = true, BlendMode = SKBlendMode.Plus };
        for (var i = 0; i < 60; i++)
        {
            float px = R(rng, 0, w), py = R(rng, 0, h * 0.8f);
            if (Math.Abs(px - (w / 2f)) < w * 0.45f && Math.Abs(py - (h / 2f)) < h * 0.17f)
            {
                continue;
            }

            spark.Color = palette[rng.Next(palette.Length)].WithAlpha((byte)rng.Next(120, 230));
            c.DrawCircle(px, py, R(rng, s * 0.0015f, s * 0.004f), spark);
        }
    }

    /// <summary>The rising trail of a rocket: a faint wavering streak, brightest at the top.</summary>
    private static void WinterLaunchTrail(SKCanvas c, float x0, float y0, float x1, float y1, SKColor colour, float s, Random rng)
    {
        using var path = new SKPath();
        path.MoveTo(x0, y0);
        path.QuadTo(((x0 + x1) / 2) + R(rng, -s * 0.03f, s * 0.03f), (y0 + y1) / 2, x1, y1);
        using var shader = SKShader.CreateLinearGradient(new SKPoint(x0, y0), new SKPoint(x1, y1), new[] { colour.WithAlpha(0), colour.WithAlpha(30), Lighten(colour, 0.5f).WithAlpha(170) }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp);
        using var trail = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.003f, StrokeCap = SKStrokeCap.Round, Shader = shader, BlendMode = SKBlendMode.Plus };
        c.DrawPath(path, trail);
    }

    /// <summary>
    /// A firework burst. kind 0: a peony of falling streaks with bright tips and an inner ring of a second colour;
    /// 1: a chrysanthemum of long trails with glittering ends; 2: a golden willow drooping heavily.
    /// </summary>
    private static void WinterFirework(SKCanvas c, float cx, float cy, float radius, SKColor colour, SKColor second, int kind, Random rng)
    {
        WinterGlow(c, cx, cy, radius * 1.9f, colour.WithAlpha(60), add: true);
        WinterGlow(c, cx, cy, radius * 0.35f, Lighten(colour, 0.5f).WithAlpha(60), add: true);
        if (kind == 2)
        {
            colour = new SKColor(0xFF, 0xD0, 0x70);
        }

        var rays = kind == 1 ? rng.Next(50, 64) : rng.Next(38, 50);
        var gravity = radius * (kind == 2 ? 0.75f : 0.18f);
        using var streak = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, BlendMode = SKBlendMode.Plus };
        using var dot = new SKPaint { IsAntialias = true, BlendMode = SKBlendMode.Plus };
        for (var i = 0; i < rays; i++)
        {
            var a = (i * Math.PI * 2 / rays) + R(rng, -0.04f, 0.04f);
            var len = radius * R(rng, 0.82f, 1.02f);
            float dx = (float)Math.Cos(a) * len, dy = (float)Math.Sin(a) * len;
            SKPoint At(float t) => new(cx + (dx * t), cy + (dy * t) + (gravity * t * t));
            var t0 = kind == 2 ? 0.1f : 0.3f;
            var t1 = kind == 2 ? 1.15f : 1f;
            var p0 = At(t0);
            var p2 = At(t1);
            var half = (t1 - t0) / 2;
            var ctrl = new SKPoint(p0.X + (dx * half), p0.Y + ((dy + (2 * gravity * t0)) * half));
            using var path = new SKPath();
            path.MoveTo(p0);
            path.QuadTo(ctrl, p2);
            using var shader = SKShader.CreateLinearGradient(p0, p2, new[] { colour.WithAlpha(0), colour.WithAlpha(kind == 2 ? (byte)160 : (byte)210), Lighten(colour, 0.25f).WithAlpha(255) }, new[] { 0f, 0.65f, 1f }, SKShaderTileMode.Clamp);
            streak.Shader = shader;
            streak.StrokeWidth = Math.Max(1.5f, radius * (kind == 2 ? 0.014f : 0.022f));
            c.DrawPath(path, streak);
            streak.Shader = null;

            if (kind == 2 || kind == 1)
            {
                // Glitter trailing along the end of the stream.
                for (var k = 0; k < 3; k++)
                {
                    var g = At(R(rng, t1 * 0.7f, t1 * 1.05f));
                    dot.Color = SKColors.White.WithAlpha((byte)rng.Next(120, 230));
                    c.DrawCircle(g.X + R(rng, -radius * 0.02f, radius * 0.02f), g.Y + R(rng, 0, radius * 0.05f), radius * R(rng, 0.006f, 0.012f), dot);
                }
            }

            if (kind != 2)
            {
                WinterGlow(c, p2.X, p2.Y, radius * 0.07f, colour.WithAlpha(150), add: true);
                dot.Color = Lighten(colour, 0.7f);
                c.DrawCircle(p2.X, p2.Y, radius * 0.018f, dot);
            }
        }

        if (kind == 0)
        {
            // Inner ring in the second colour.
            var inner = rng.Next(16, 22);
            for (var i = 0; i < inner; i++)
            {
                var a = (i * Math.PI * 2 / inner) + 0.1;
                var px = cx + (float)(Math.Cos(a) * radius * 0.45f);
                var py = cy + (float)(Math.Sin(a) * radius * 0.45f) + (radius * 0.03f);
                WinterGlow(c, px, py, radius * 0.06f, second.WithAlpha(170), add: true);
                dot.Color = Lighten(second, 0.6f);
                c.DrawCircle(px, py, radius * 0.014f, dot);
            }
        }
    }

    /// <summary>A row of city blocks standing on <paramref name="ground"/>: varied roofs (spires, domes, masts), windows lit here and there.</summary>
    private static void WinterCity(SKCanvas c, int w, float ground, float s, Random rng, float minH, float maxH, SKColor colour, SKColor window, float lit, bool far)
    {
        using var block = new SKPaint { IsAntialias = true, Color = colour };
        using var glass = new SKPaint { IsAntialias = true };
        using var beacon = new SKPaint { IsAntialias = true, Color = new SKColor(0xFF, 0x50, 0x50) };
        var x = R(rng, -s * 0.05f, 0);
        while (x < w)
        {
            var bw = s * R(rng, far ? 0.05f : 0.06f, far ? 0.09f : 0.12f);
            var bh = R(rng, minH, maxH);
            var top = ground - bh;
            c.DrawRect(x, top, bw + 1, bh + 2, block);
            var roof = rng.Next(far ? 5 : 4);
            using (var shape = new SKPath())
            {
                switch (roof)
                {
                    case 1:
                        shape.AddRect(new SKRect(x + (bw * 0.2f), top - (bh * 0.12f), x + (bw * 0.8f), top + 1));
                        break;
                    case 2:
                        shape.MoveTo(x + (bw * 0.15f), top + 1);
                        shape.LineTo(x + (bw * 0.5f), top - (bh * 0.35f));
                        shape.LineTo(x + (bw * 0.85f), top + 1);
                        shape.Close();
                        break;
                    case 3:
                        shape.AddRect(new SKRect(x + (bw * 0.48f), top - (bh * 0.3f), x + (bw * 0.52f), top + 1));
                        c.DrawCircle(x + (bw * 0.5f), top - (bh * 0.3f), s * 0.003f, beacon);
                        break;
                    case 4:
                        shape.AddOval(new SKRect(x + (bw * 0.15f), top - (bw * 0.35f), x + (bw * 0.85f), top + (bw * 0.35f)));
                        break;
                }

                c.DrawPath(shape, block);
            }

            var cell = s * (far ? 0.016f : 0.022f);
            for (var wy = top + (cell * 0.8f); wy < ground - cell; wy += cell * 1.3f)
            {
                for (var wx = x + (cell * 0.5f); wx < x + bw - cell; wx += cell)
                {
                    if (rng.NextDouble() < lit)
                    {
                        glass.Color = rng.NextDouble() < 0.85 ? window : new SKColor(0xB8, 0xE0, 0xFF, window.Alpha);
                        c.DrawRect(wx, wy, cell * 0.45f, cell * 0.6f, glass);
                    }
                }
            }

            x += bw + (far ? 0 : s * R(rng, 0, 0.01f));
        }
    }

    /// <summary>A big observation wheel in silhouette, its rim and spokes strung with lights.</summary>
    private static void WinterFerrisWheel(SKCanvas c, float cx, float cy, float r, float s, Random rng)
    {
        var frame = new SKColor(0x10, 0x10, 0x24);
        using var leg = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.006f, StrokeCap = SKStrokeCap.Round, Color = frame };
        var foot = cy + (r * 1.2f);
        c.DrawLine(cx, cy, cx - (r * 0.45f), foot, leg);
        c.DrawLine(cx, cy, cx + (r * 0.45f), foot, leg);

        using var rim = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = s * 0.004f, Color = frame };
        c.DrawCircle(cx, cy, r, rim);
        c.DrawCircle(cx, cy, r * 0.94f, rim);
        rim.StrokeWidth = s * 0.0018f;
        const int Spokes = 16;
        using var cab = new SKPaint { IsAntialias = true, Color = frame };
        for (var i = 0; i < Spokes; i++)
        {
            var a = i * Math.PI * 2 / Spokes;
            float px = cx + (float)(Math.Cos(a) * r), py = cy + (float)(Math.Sin(a) * r);
            c.DrawLine(cx, cy, px, py, rim);
            c.DrawRoundRect(new SKRect(px - (s * 0.009f), py, px + (s * 0.009f), py + (s * 0.016f)), s * 0.003f, s * 0.003f, cab);
        }

        var colours = new[] { new SKColor(0xFF, 0xD0, 0x80), new SKColor(0xFF, 0x7A, 0xB0), new SKColor(0x8A, 0xD8, 0xFF) };
        const int Lights = 40;
        using var bulb = new SKPaint { IsAntialias = true };
        for (var i = 0; i < Lights; i++)
        {
            var a = i * Math.PI * 2 / Lights;
            float px = cx + (float)(Math.Cos(a) * r), py = cy + (float)(Math.Sin(a) * r);
            var colour = colours[(i / 4) % colours.Length];
            WinterGlow(c, px, py, s * 0.012f, colour.WithAlpha(120), add: true);
            bulb.Color = Lighten(colour, 0.5f);
            c.DrawCircle(px, py, s * 0.0025f, bulb);
        }

        WinterGlow(c, cx, cy, s * 0.02f, new SKColor(0xFF, 0xD0, 0x80, 160), add: true);
    }
}
