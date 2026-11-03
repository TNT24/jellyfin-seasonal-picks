using System;
using System.Runtime.CompilerServices;
using SkiaSharp;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>An image uploaded with a Discord message, referenced from an embed as <c>attachment://FileName</c>.</summary>
public sealed record DiscordAttachment(string FileName, byte[] Data, string MimeType)
{
    public string Reference => "attachment://" + FileName;
}

/// <summary>Shrinks posters and covers before they're uploaded, so a message stays well under Discord's upload limit.</summary>
public static class DiscordImages
{
    /// <summary>Posters are shown small on a card; 450 pixels high is plenty and keeps each around 50 KB.</summary>
    public const int PosterHeight = 450;

    /// <summary>The season cover is shown full width, which Discord caps well below this.</summary>
    public const int CoverWidth = 1280;

    /// <summary>Discord's limit is 25 MB for the whole message (10 MB on some servers); an image is never sent past this.</summary>
    public const int MaxBytes = 4 * 1024 * 1024;

    /// <summary>A JPEG of the image no bigger than <paramref name="maxWidth"/> × <paramref name="maxHeight"/>, or null if it can't be read.</summary>
    /// <remarks>Kept separate so a missing/mismatched SkiaSharp fails here, inside the caller's try.</remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static byte[]? Shrink(byte[] encoded, int maxWidth, int maxHeight)
    {
        // SKBitmap.Decode throws rather than returning null when the bytes aren't an image it knows.
        using var encodedData = SKData.CreateCopy(encoded);
        using var codec = SKCodec.Create(encodedData);
        if (codec is null)
        {
            return null;
        }

        using var source = SKBitmap.Decode(codec);
        if (source is null || source.Width <= 0 || source.Height <= 0)
        {
            return null;
        }

        var scale = Math.Min(1.0, Math.Min((double)maxWidth / source.Width, (double)maxHeight / source.Height));
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));

        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        surface.Canvas.Clear(SKColors.Black);
        using var image = SKImage.FromBitmap(source);
        surface.Canvas.DrawImage(image, new SKRect(0, 0, source.Width, source.Height), new SKRect(0, 0, width, height), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        using var snapshot = surface.Snapshot();
        using var data = snapshot.Encode(SKEncodedImageFormat.Jpeg, 85);
        var bytes = data?.ToArray();
        return bytes is { Length: > 0 and <= MaxBytes } ? bytes : null;
    }
}
