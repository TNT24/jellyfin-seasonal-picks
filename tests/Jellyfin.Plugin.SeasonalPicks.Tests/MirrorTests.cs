using System;
using System.IO;
using Jellyfin.Plugin.SeasonalPicks.Services;
using Xunit;

namespace Jellyfin.Plugin.SeasonalPicks.Tests;

public class MirrorTests
{
    [Theory]
    [InlineData("/Suggested", new[] { "/Movies", "/TV" }, true)]
    [InlineData("/Movies", new[] { "/Movies", "/TV" }, false)]         // is another library
    [InlineData("/Movies/Suggested", new[] { "/Movies" }, false)]      // inside another library
    [InlineData("/media", new[] { "/media/Movies" }, false)]           // contains another library
    [InlineData("/Movies/", new[] { "/Movies" }, false)]               // trailing slash doesn't sneak past
    [InlineData("/MoviesSuggested", new[] { "/Movies" }, true)]        // similar name is fine
    [InlineData("/", new string[0], false)]                            // filesystem root
    public void Cleaning_is_refused_when_the_folder_overlaps_another_library(string root, string[] others, bool expected)
    {
        Assert.Equal(expected, MirrorWriter.IsSafeToClean(root, others, out var reason));
        Assert.Equal(expected, reason.Length == 0);
    }

    [Fact]
    public void Removing_a_real_folder_never_follows_links_inside_it()
    {
        var tmp = Directory.CreateTempSubdirectory("sp-mirror-");
        try
        {
            // Something the link points at, outside the folder being removed. It must survive.
            var precious = Directory.CreateDirectory(Path.Combine(tmp.FullName, "precious"));
            File.WriteAllText(Path.Combine(precious.FullName, "film.mkv"), "x");

            var junk = Directory.CreateDirectory(Path.Combine(tmp.FullName, "junk"));
            File.WriteAllText(Path.Combine(junk.FullName, "thumb.jpg"), "x");
            Directory.CreateDirectory(Path.Combine(junk.FullName, "@eaDir", "sub"));

            var linked = false;
            try
            {
                Directory.CreateSymbolicLink(Path.Combine(junk.FullName, "link-to-precious"), precious.FullName);
                linked = true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Windows without developer mode can't create symlinks; the rest of the test still runs.
            }

            MirrorWriter.DeleteWithoutFollowingLinks(junk);

            Assert.False(Directory.Exists(junk.FullName));
            Assert.True(File.Exists(Path.Combine(precious.FullName, "film.mkv")), linked ? "link target was deleted" : "precious folder was deleted");
        }
        finally
        {
            tmp.Delete(true);
        }
    }

    [Theory]
    [InlineData("folder.jpg", true)]
    [InlineData("poster.PNG", true)]
    [InlineData("cover.webp", true)]
    [InlineData("backdrop.jpg", false)]
    [InlineData("folder.nfo", false)]
    [InlineData("Heat (1995).mkv", false)]
    public void Only_primary_artwork_files_are_replaced_in_a_library_folder(string name, bool expected)
    {
        Assert.Equal(expected, CoverService.IsLibraryArtwork(Path.Combine("/config/root/default/Suggested", name)));
    }
}
