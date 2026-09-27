using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SeasonalPicks.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>
/// Seasonal cover art for the playlist and mirror library. Covers are stored on the server,
/// so every client (TV apps included) shows them.
/// Order: uploaded cover for the season, then a generated poster collage in the season colour.
/// </summary>
public class CoverService
{
    public const string DefaultKey = "default";

    private static readonly string[] Extensions = { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly ILibraryManager _library;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<CoverService> _logger;

    public CoverService(ILibraryManager library, IFileSystem fileSystem, ILogger<CoverService> logger)
    {
        _library = library;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <summary>Primary artwork file names Jellyfin looks for in a folder (folder.jpg, poster.png, cover.webp ...).</summary>
    internal static bool IsLibraryArtwork(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
        return name is "folder" or "poster" or "cover" or "default"
            && Extensions.Contains(Path.GetExtension(path).ToLowerInvariant());
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // In use or already gone: the next cover update cleans it up.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string Folder
    {
        get
        {
            var root = Plugin.Instance?.DataFolderPath ?? Path.Combine(AppContext.BaseDirectory, "SeasonalPicks");
            var dir = Path.Combine(root, "covers");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    /// <summary>Pictures uploaded to go behind a generated cover (the "Picture" background).</summary>
    private static string BackgroundFolder
    {
        get
        {
            var dir = Path.Combine(Folder, "backgrounds");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static bool IsValidKey(string key) => key.Length is > 0 and <= 64 && key.All(ch => char.IsAsciiLetterLower(ch) || char.IsAsciiDigit(ch));

    /// <summary>A finished cover uploaded for a season, used as it is.</summary>
    public string? FindUploaded(string key) => Find(Folder, key);

    public void SaveUpload(string key, byte[] data, string mimeType) => Save(Folder, key, data, mimeType);

    public void DeleteUpload(string key) => Delete(Folder, key);

    /// <summary>A picture uploaded to go behind a season's generated cover.</summary>
    public string? FindBackground(string key) => Find(BackgroundFolder, key);

    public void SaveBackground(string key, byte[] data, string mimeType) => Save(BackgroundFolder, key, data, mimeType);

    public void DeleteBackground(string key) => Delete(BackgroundFolder, key);

    /// <summary>
    /// Move a season's uploads to its new key after a rename. Anything already uploaded under the new key wins.
    /// </summary>
    public void RenameKey(string from, string to)
    {
        foreach (var dir in new[] { Folder, BackgroundFolder })
        {
            var source = Find(dir, from);
            if (source is null || Find(dir, to) is not null)
            {
                continue;
            }

            File.Move(source, Path.Combine(dir, to + Path.GetExtension(source)));
        }
    }

    private static string? Find(string dir, string key)
        => Extensions.Select(e => Path.Combine(dir, key + e)).FirstOrDefault(File.Exists);

    private static void Save(string dir, string key, byte[] data, string mimeType)
    {
        Delete(dir, key);
        var ext = mimeType switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg"
        };
        File.WriteAllBytes(Path.Combine(dir, key + ext), data);
    }

    private static void Delete(string dir, string key)
    {
        foreach (var e in Extensions)
        {
            var p = Path.Combine(dir, key + e);
            if (File.Exists(p))
            {
                File.Delete(p);
            }
        }
    }

    public static string MimeFor(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => "image/jpeg"
    };

    /// <summary>Put the cover for the current season on an item (playlist or library).</summary>
    public async Task ApplyAsync(BaseItem target, PluginConfiguration cfg, SeasonRule? season, string title, IReadOnlyList<Guid> pickIds, bool wide, CancellationToken ct)
    {
        try
        {
            var cover = Resolve(cfg, season, title, pickIds, wide);
            if (cover is null)
            {
                return;
            }

            // Not saved through Jellyfin's image saver: that puts the file in the item's metadata folder, where
            // Jellyfin's own collage generator (playlists, libraries) treats it as its own and replaces it on the
            // next refresh. An image stored anywhere else is left alone, so the cover lives in our data folder.
            var ext = cover.Value.MimeType switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg"
            };

            // A library (collection folder) also picks up images from its own folder on every scan, which would bring
            // back any older artwork. So its cover goes there, replacing what was there before.
            if (target is CollectionFolder && !string.IsNullOrEmpty(target.Path) && Directory.Exists(target.Path))
            {
                foreach (var old in Directory.EnumerateFiles(target.Path).Where(IsLibraryArtwork))
                {
                    TryDelete(old);
                }

                var libraryPath = Path.Combine(target.Path, "folder" + ext);
                await File.WriteAllBytesAsync(libraryPath, cover.Value.Data, ct).ConfigureAwait(false);
                target.SetImagePath(ImageType.Primary, 0, _fileSystem.GetFileInfo(libraryPath));
                await target.UpdateToRepositoryAsync(ItemUpdateType.ImageUpdate, ct).ConfigureAwait(false);
                _logger.LogInformation("SeasonalPicks: cover set on library {Item}", target.Name);
                return;
            }

            var dir = Path.Combine(Folder, "applied");
            Directory.CreateDirectory(dir);
            var prefix = target.Id.ToString("N");

            // New file name each time: clients (TVs especially) cache images by path and date.
            var path = Path.Combine(dir, $"{prefix}-{DateTime.UtcNow.Ticks}{ext}");
            await File.WriteAllBytesAsync(path, cover.Value.Data, ct).ConfigureAwait(false);

            target.SetImagePath(ImageType.Primary, 0, _fileSystem.GetFileInfo(path));
            await target.UpdateToRepositoryAsync(ItemUpdateType.ImageUpdate, ct).ConfigureAwait(false);
            _logger.LogInformation("SeasonalPicks: cover set on {Item}", target.Name);

            foreach (var old in Directory.EnumerateFiles(dir, prefix + "-*").Where(f => !string.Equals(f, path, StringComparison.Ordinal)))
            {
                TryDelete(old);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "SeasonalPicks: could not set cover on {Item}", target.Name);
        }
    }

    /// <summary>The cover bytes that would be used, or null to leave Jellyfin's own image alone.</summary>
    public (byte[] Data, string MimeType)? Resolve(PluginConfiguration cfg, SeasonRule? season, string title, IReadOnlyList<Guid> pickIds, bool wide)
    {
        var key = season?.CoverKey ?? DefaultKey;
        var uploaded = FindUploaded(key);
        if (uploaded is not null)
        {
            return (File.ReadAllBytes(uploaded), MimeFor(uploaded));
        }

        if (!cfg.AutoGenerateCovers)
        {
            return null;
        }

        try
        {
            var bytes = season is null
                ? Design(pickIds, key, null, cfg.DefaultCoverColor, cfg.DefaultCoverTheme, cfg.DefaultCover, title, wide)
                : Design(pickIds, key, season.Name, season.Color, season.Theme, season.Cover, title, wide);
            return bytes is null ? null : (bytes, "image/jpeg");
        }
        catch (Exception ex)
        {
            // Most likely a SkiaSharp mismatch with the server; uploads still work.
            _logger.LogWarning(ex, "SeasonalPicks: cover generation failed; upload a cover in the plugin settings instead");
            return null;
        }
    }

    /// <summary>
    /// A generated cover for one design. <paramref name="seasonName"/> is null for the out-of-season cover;
    /// <paramref name="label"/> is the playlist or library name. Used for real covers and the settings page's live preview.
    /// </summary>
    public byte[]? Design(IReadOnlyList<Guid> pickIds, string key, string? seasonName, string color, string? theme, CoverStyle? style, string label, bool wide)
    {
        style ??= new CoverStyle();
        var posters = pickIds
            .Select(id => _library.GetItemById(id))
            .Select(i => i?.GetImageInfo(ImageType.Primary, 0))
            .Where(img => img is not null && img.IsLocalFile && File.Exists(img.Path))
            .Select(img => img!.Path)
            .ToList();

        var name = seasonName ?? label;
        var coverTheme = seasonName is null && string.IsNullOrWhiteSpace(theme) ? CoverTheme.Sparkle : CoverArt.ThemeFor(theme, name);
        var background = IsValidKey(key) ? FindBackground(key) : null;
        return Generate(posters, color, name, label, wide, coverTheme, style, background);
    }

    /// <summary>Kept separate so a missing/mismatched SkiaSharp fails here, inside the caller's try.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static byte[]? Generate(
        IReadOnlyList<string> posterPaths,
        string hexColor,
        string name,
        string label,
        bool wide,
        CoverTheme theme = CoverTheme.Sparkle,
        CoverStyle? style = null,
        string? backgroundPath = null)
    {
        style ??= new CoverStyle();
        int width = wide ? 1280 : 1000, height = wide ? 720 : 1000;
        var tint = SKColor.TryParse(hexColor, out var c) ? c : new SKColor(0x3A, 0x6E, 0xA5);

        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(Darken(tint, 0.35f));

        var background = style.Background?.Trim().ToLowerInvariant();
        if (background == "picture" && backgroundPath is not null && DrawPicture(canvas, backgroundPath, width, height))
        {
            // Drawn full-size.
        }
        else if (background == "plain")
        {
            using var gradient = new SKPaint
            {
                Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0), new SKPoint(width * 0.3f, height),
                    new[] { CoverArt.Lighten(tint, 0.1f), Darken(tint, 0.3f) },
                    SKShaderTileMode.Clamp)
            };
            canvas.DrawRect(0, 0, width, height, gradient);
        }
        else
        {
            DrawPosterGrid(canvas, posterPaths, width, height, wide);
        }

        // Season colour wash, then darken so the decorations and title stand out from the background.
        using (var wash = new SKPaint { Color = tint.WithAlpha(Percent(style.ColourWash, 240)) })
        {
            canvas.DrawRect(0, 0, width, height, wash);
        }

        using (var dim = new SKPaint { Color = Darken(tint, 0.2f).WithAlpha(Percent(style.Darkness, 220)) })
        {
            canvas.DrawRect(0, 0, width, height, dim);
        }

        var decorations = Percent(style.Decorations, 255);
        if (decorations > 0)
        {
            // Drawn onto a layer so the whole scene can be faded, letting a background picture show through.
            using var layer = new SKPaint { Color = SKColors.White.WithAlpha(decorations) };
            canvas.SaveLayer(layer);
            CoverArt.DrawTheme(canvas, theme, width, height, tint, name);
            canvas.Restore();
        }

        var caption = string.IsNullOrWhiteSpace(style.Title) ? name : style.Title.Trim();
        string? labelText = !style.ShowLabel ? null : string.IsNullOrWhiteSpace(style.Label) ? label : style.Label.Trim();
        CoverArt.DrawTitle(canvas, width, height, tint, caption, labelText, style);

        using var snapshot = surface.Snapshot();
        using var data = snapshot.Encode(SKEncodedImageFormat.Jpeg, 88);
        return data?.ToArray();
    }

    /// <summary>0-100 setting to an alpha, clamped.</summary>
    private static byte Percent(int value, int max) => (byte)(Math.Clamp(value, 0, 100) * max / 100);

    /// <summary>Poster grid, each poster centre-cropped into its cell.</summary>
    private static void DrawPosterGrid(SKCanvas canvas, IReadOnlyList<string> posterPaths, int width, int height, bool wide)
    {
        int cols = wide ? 5 : 3, rows = 2;
        var cellW = (float)width / cols;
        var cellH = (float)height / rows;
        var cells = cols * rows;
        for (var i = 0; i < cells && posterPaths.Count > 0; i++)
        {
            using var bmp = SKBitmap.Decode(posterPaths[i % posterPaths.Count]);
            if (bmp is null)
            {
                continue;
            }

            var dest = new SKRect((i % cols) * cellW, (i / cols) * cellH, ((i % cols) + 1) * cellW, ((i / cols) + 1) * cellH);
            DrawCropped(canvas, bmp, dest);
        }
    }

    /// <summary>An uploaded background picture, centre-cropped to fill the cover. False if it can't be read.</summary>
    private static bool DrawPicture(SKCanvas canvas, string path, int width, int height)
    {
        using var bmp = SKBitmap.Decode(path);
        if (bmp is null)
        {
            return false;
        }

        DrawCropped(canvas, bmp, new SKRect(0, 0, width, height));
        return true;
    }

    private static void DrawCropped(SKCanvas canvas, SKBitmap bmp, SKRect dest)
    {
        var scale = Math.Max(dest.Width / bmp.Width, dest.Height / bmp.Height);
        var srcW = dest.Width / scale;
        var srcH = dest.Height / scale;
        var src = new SKRect((bmp.Width - srcW) / 2, (bmp.Height - srcH) / 2, ((bmp.Width - srcW) / 2) + srcW, ((bmp.Height - srcH) / 2) + srcH);
        using var image = SKImage.FromBitmap(bmp);
        using var paint = new SKPaint { IsAntialias = true };
        canvas.DrawImage(image, src, dest, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear), paint);
    }

    private static SKColor Darken(SKColor c, float factor)
        => new((byte)(c.Red * factor), (byte)(c.Green * factor), (byte)(c.Blue * factor));
}
