using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Plugin.SeasonalPicks.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>
/// Keeps a folder of symlinks to the picked movies, so a normal Jellyfin library pointed at it
/// shows the picks on every client (TV home rows included). Replaces an external symlink script.
///
/// Safety: only symlinks are ever created or removed here. Real files/folders are left alone.
/// </summary>
public class MirrorWriter
{
    private readonly ILibraryManager _library;
    private readonly ILogger<MirrorWriter> _logger;

    public MirrorWriter(ILibraryManager library, ILogger<MirrorWriter> logger)
    {
        _library = library;
        _logger = logger;
    }

    /// <summary>Makes the folder match the picks. Returns true when links were added or removed.</summary>
    public bool Sync(PluginConfiguration cfg, IReadOnlyList<Guid> itemIds)
    {
        if (string.IsNullOrWhiteSpace(cfg.MirrorPath))
        {
            _logger.LogWarning("SeasonalPicks: mirror folder is enabled but MirrorPath is empty; skipping");
            return false;
        }

        var root = cfg.MirrorPath.TrimEnd('/', '\\');
        Directory.CreateDirectory(root);

        // Desired link name -> target path.
        var desired = new Dictionary<string, string>(StringComparer.Ordinal);
        var targets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in itemIds)
        {
            var item = _library.GetItemById(id);
            if (item?.Path is null)
            {
                continue;
            }

            var target = LinkTarget(item);
            // Skip links into the mirror itself, and a second pick that resolves to an already-linked folder.
            if (target is null || target.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal) || !targets.Add(target))
            {
                continue;
            }

            var name = Path.GetFileName(target.TrimEnd('/', '\\'));
            var unique = name;
            for (var n = 2; desired.ContainsKey(unique); n++)
            {
                unique = Path.GetFileNameWithoutExtension(name) + $" ({n})" + Path.GetExtension(name);
            }

            desired[unique] = target;
        }

        var changed = false;

        // Real files/folders are only removed when asked, and never if the folder overlaps another library.
        var clean = cfg.MirrorRemoveOtherFiles;
        if (clean)
        {
            var others = _library.GetVirtualFolders()
                .Where(v => !v.Locations.Any(l => SamePath(l, root)))
                .SelectMany(v => v.Locations);
            if (!IsSafeToClean(root, others, out var reason))
            {
                _logger.LogWarning("SeasonalPicks: not removing other files from {Root}: {Reason}", root, reason);
                clean = false;
            }
        }

        // Remove stale symlinks (and ones pointing somewhere else now).
        foreach (var entry in new DirectoryInfo(root).EnumerateFileSystemInfos())
        {
            if (entry.LinkTarget is null)
            {
                if (clean && RemoveReal(entry))
                {
                    changed = true;
                }

                continue; // otherwise a real file or folder: leave it
            }

            if (desired.TryGetValue(entry.Name, out var want) && string.Equals(entry.LinkTarget, want, StringComparison.Ordinal))
            {
                desired.Remove(entry.Name); // already correct
                continue;
            }

            try
            {
                entry.Delete(); // deletes the link itself, not its target
                changed = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SeasonalPicks: could not remove link {Link}", entry.FullName);
            }
        }

        foreach (var (name, target) in desired)
        {
            var linkPath = Path.Combine(root, name);
            if (File.Exists(linkPath) || Directory.Exists(linkPath))
            {
                _logger.LogWarning("SeasonalPicks: {Path} exists and is not a link; leaving it", linkPath);
                continue;
            }

            try
            {
                if (Directory.Exists(target))
                {
                    Directory.CreateSymbolicLink(linkPath, target);
                }
                else
                {
                    File.CreateSymbolicLink(linkPath, target);
                }

                changed = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SeasonalPicks: could not link {Link} -> {Target}", linkPath, target);
            }
        }

        _logger.LogInformation("SeasonalPicks: mirror folder {Root} synced ({Count} picks, changed: {Changed})", root, itemIds.Count, changed);
        return changed;
    }

    private bool RemoveReal(FileSystemInfo entry)
    {
        try
        {
            DeleteWithoutFollowingLinks(entry);
            _logger.LogInformation("SeasonalPicks: removed {Path} from the mirror folder (not one of the plugin's links)", entry.FullName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SeasonalPicks: could not remove {Path}", entry.FullName);
            return false;
        }
    }

    /// <summary>Deletes a file or folder tree. Links inside it are removed as links; their targets are never touched.</summary>
    internal static void DeleteWithoutFollowingLinks(FileSystemInfo entry)
    {
        if (entry.LinkTarget is not null || entry is FileInfo)
        {
            entry.Delete(); // a link (to a file or folder) or a plain file
            return;
        }

        var dir = (DirectoryInfo)entry;
        foreach (var child in dir.EnumerateFileSystemInfos())
        {
            DeleteWithoutFollowingLinks(child);
        }

        dir.Delete(false);
    }

    /// <summary>
    /// Only clean a folder that no other library uses: not the same folder, not inside one, not containing one.
    /// Stops a mistyped path (e.g. "/Movies") from wiping real films.
    /// </summary>
    internal static bool IsSafeToClean(string root, IEnumerable<string> otherLibraryLocations, out string reason)
    {
        var r = Normalize(root);
        if (r.Length <= 1 || r.Count(c => c == '/') < 1 || Path.GetPathRoot(root) == root)
        {
            reason = "the folder is a drive or filesystem root";
            return false;
        }

        foreach (var other in otherLibraryLocations)
        {
            var o = Normalize(other);
            if (o == r || r.StartsWith(o + "/", StringComparison.Ordinal) || o.StartsWith(r + "/", StringComparison.Ordinal))
            {
                reason = $"it overlaps another library's folder ({other})";
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    private static string Normalize(string path) => path.Replace('\\', '/').TrimEnd('/');

    private static bool SamePath(string a, string b) => string.Equals(Normalize(a), Normalize(b), StringComparison.Ordinal);

    /// <summary>
    /// Link the movie's own folder when it has one (keeps posters/nfo/subtitles together);
    /// link the file itself when movies sit loose in a library root. Disc folders are linked as-is.
    /// </summary>
    private string? LinkTarget(BaseItem item)
    {
        var path = item.Path;
        if (Directory.Exists(path))
        {
            return path;
        }

        if (!File.Exists(path))
        {
            return null;
        }

        var parent = Path.GetDirectoryName(path);
        if (parent is null)
        {
            return path;
        }

        var roots = _library.GetCollectionFolders(item)
            .SelectMany(f => f.PhysicalLocations)
            .Select(p => p.TrimEnd('/', '\\'));
        var isLibraryRoot = roots.Any(r => string.Equals(r, parent.TrimEnd('/', '\\'), StringComparison.Ordinal));
        return isLibraryRoot || IsSharedFolder(parent) ? path : parent;
    }

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mkv", ".mp4", ".m4v", ".avi", ".mov", ".wmv", ".ts", ".m2ts", ".webm", ".mpg", ".mpeg", ".iso", ".strm"
    };

    /// <summary>
    /// A folder holding several different movies (e.g. "Marvel/"), rather than one movie plus its versions.
    /// Per-movie folders name their files after the folder ("Heat (1995)/Heat (1995) - 4K.mkv").
    /// </summary>
    private static bool IsSharedFolder(string folder)
    {
        var folderName = Path.GetFileName(folder);
        var videos = Directory.EnumerateFiles(folder).Where(f => VideoExtensions.Contains(Path.GetExtension(f))).ToList();
        return videos.Count > 1 && videos.Any(v => !Path.GetFileName(v).StartsWith(folderName, StringComparison.OrdinalIgnoreCase));
    }
}
