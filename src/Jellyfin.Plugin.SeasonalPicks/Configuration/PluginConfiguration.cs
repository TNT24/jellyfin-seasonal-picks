using System;
using System.Linq;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.SeasonalPicks.Configuration;

/// <summary>
/// Plugin settings. Arrays (not lists) on purpose: XmlSerializer appends to pre-filled lists,
/// which would duplicate the default seasons on every load.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    // ---- Picking ----
    public int PickCount { get; set; } = 25;

    /// <summary>Movies played within this many days are never picked.</summary>
    public int RecentlyWatchedDays { get; set; } = 60;

    /// <summary>Played movies become "rediscover" candidates once last played this many days ago.</summary>
    public int RediscoverAfterDays { get; set; } = 365;

    /// <summary>Share of picks reserved for in-season movies (when a season is active).</summary>
    public int SeasonalPercent { get; set; } = 40;

    /// <summary>Share of the remaining (non-seasonal) picks that are rediscoveries; the rest are unwatched.</summary>
    public int RediscoverPercent { get; set; } = 30;

    /// <summary>Share of picks reserved for unwatched new releases (0 = off; they still get a boost).</summary>
    public int NewReleasePercent { get; set; } = 20;

    /// <summary>A film counts as a new release for this many days after its release date.</summary>
    public int NewReleaseDays { get; set; } = 365;

    /// <summary>Skip movies rated below this community rating (0 = off). Unrated movies are kept.</summary>
    public double MinCommunityRating { get; set; }

    /// <summary>Movie library ids to pick from. Empty = every movie library the user can see.</summary>
    public string[] LibraryIds { get; set; } = Array.Empty<string>();

    /// <summary>Movies with any of these tags are never picked.</summary>
    public string[] ExcludeTags { get; set; } = Array.Empty<string>();

    /// <summary>Users that get picks. Empty = all users.</summary>
    public string[] EnabledUserIds { get; set; } = Array.Empty<string>();

    /// <summary>No first-listed genre may fill more than this share of the list (0 = off). Seasonal picks are exempt but count.</summary>
    public int MaxPerGenrePercent { get; set; } = 30;

    /// <summary>Use collections (box sets): favour the next film in one you've started, skip films whose earlier parts are unwatched.</summary>
    public bool CollectionAware { get; set; } = true;

    /// <summary>When a user finishes (or marks played) a pick, swap it for a new one straight away.</summary>
    public bool ReplaceWatchedPicks { get; set; } = true;

    // ---- Output: playlists (work on every client, including TVs) ----
    public bool EnablePlaylists { get; set; } = true;

    public const string DefaultName = "Suggested";

    public string PlaylistName { get; set; } = DefaultName;

    /// <summary>Append the active season, e.g. "Suggested - Halloween".</summary>
    public bool PlaylistNameShowsSeason { get; set; } = true;

    /// <summary>Write each pick's reason into the playlist's description (shown on the playlist page on TVs too).</summary>
    public bool PlaylistShowsReasons { get; set; } = true;

    /// <summary>While a season is on, give each user an extra playlist of just that season's films, e.g. "Halloween Picks".</summary>
    public bool EnableSeasonPlaylists { get; set; } = true;

    public int SeasonPlaylistCount { get; set; } = 20;

    /// <summary>Season playlist name; "{season}" is replaced by the season's name.</summary>
    public string SeasonPlaylistName { get; set; } = "{season} Picks";

    // ---- Output: mirrored library folder (replacement for a symlink script) ----
    public bool EnableMirror { get; set; }

    /// <summary>Folder (as seen by the server/container) that a Jellyfin library points at. Its contents are managed by the plugin.</summary>
    public string MirrorPath { get; set; } = string.Empty;

    /// <summary>Whose history drives the shared folder.</summary>
    public string MirrorUserId { get; set; } = string.Empty;

    public bool MirrorTriggerLibraryScan { get; set; } = true;

    /// <summary>Also delete real files/folders in the mirror folder (anything that isn't one of the plugin's links). Refused if the folder overlaps another library.</summary>
    public bool MirrorRemoveOtherFiles { get; set; }

    // ---- Output: web client screen ----
    public bool EnableWebScreen { get; set; } = true;

    public bool EnableHomeRow { get; set; } = true;

    // ---- Covers (server-side images, so they show on TVs too) ----
    /// <summary>Set the playlist and mirror-library cover from the active season.</summary>
    public bool EnableSeasonalCovers { get; set; } = true;

    /// <summary>When a season has no uploaded cover, build one from the picks' posters in the season colour.</summary>
    public bool AutoGenerateCovers { get; set; } = true;

    /// <summary>Tint for the out-of-season (default) cover.</summary>
    public string DefaultCoverColor { get; set; } = "#3A6EA5";

    /// <summary>Decoration for the out-of-season cover.</summary>
    public string DefaultCoverTheme { get; set; } = "Sparkle";

    /// <summary>Layout of the out-of-season cover.</summary>
    public CoverStyle DefaultCover { get; set; } = new();

    // ---- Seasons ----
    public SeasonRule[] Seasons { get; set; } = SeasonRule.Defaults();

    /// <summary>Which one-time settings updates have run (see <see cref="MigrateToLatest"/>).</summary>
    public int ConfigVersion { get; set; }

    /// <summary>
    /// One-time updates to settings saved by older versions. Returns true when something changed (and should be saved).
    /// v1: every season hides its films out of season; Easter is added.
    /// </summary>
    public bool MigrateToLatest()
    {
        var start = ConfigVersion;

        if (ConfigVersion < 1)
        {
            // Holidays hide their films out of season; film days (added later, hidden = false in the defaults) don't.
            var defaultsV1 = SeasonRule.Defaults();
            foreach (var s in Seasons)
            {
                s.HideOutOfSeason = defaultsV1.FirstOrDefault(d => string.Equals(d.Name, s.Name, StringComparison.OrdinalIgnoreCase))?.HideOutOfSeason ?? true;
            }

            if (!Seasons.Any(s => string.Equals(s.Name, "Easter", StringComparison.OrdinalIgnoreCase)))
            {
                Seasons = Seasons.Concat(SeasonRule.Defaults().Where(s => s.Name == "Easter")).ToArray();
            }

            ConfigVersion = 1;
        }

        if (ConfigVersion < 2)
        {
            // v2: longer keyword and tag lists (famous titles, TMDb keywords). Merged into same-named seasons,
            // keeping whatever the user added or changed.
            var defaults = SeasonRule.Defaults();
            foreach (var s in Seasons)
            {
                var d = defaults.FirstOrDefault(x => string.Equals(x.Name, s.Name, StringComparison.OrdinalIgnoreCase));
                if (d is null)
                {
                    continue;
                }

                s.Keywords = s.Keywords.Concat(d.Keywords).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                s.Tags = s.Tags.Concat(d.Tags).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            }

            ConfigVersion = 2;
        }

        if (ConfigVersion < 3)
        {
            // v3: the default name is now "Suggested". Only the old default is renamed; a custom name is kept.
            if (string.IsNullOrWhiteSpace(PlaylistName) || string.Equals(PlaylistName.Trim(), "For You", StringComparison.OrdinalIgnoreCase))
            {
                PlaylistName = DefaultName;
            }

            ConfigVersion = 3;
        }

        if (ConfigVersion < 4)
        {
            // v4: Summer removed (the user asked for it); New Year and the film days added if missing.
            var kept = Seasons.Where(s => !string.Equals(s.Name, "Summer", StringComparison.OrdinalIgnoreCase)).ToList();
            // Only the seasons new in this version, so one the user deleted earlier isn't brought back.
            var added = new[] { "New Year", "Star Wars Day", "Mario Day", "Ghostbusters Day", "Back to Hogwarts", "Star Trek Day", "Hobbit Day", "Back to the Future Day", "Groundhog Day" };
            foreach (var d in SeasonRule.Defaults().Where(d => added.Contains(d.Name)))
            {
                if (!kept.Any(s => string.Equals(s.Name, d.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    kept.Add(d);
                }
            }

            Seasons = kept.ToArray();
            ConfigVersion = 4;
        }

        if (ConfigVersion < 5)
        {
            // v5: St Patrick's Day and Bonfire Night.
            var added = new[] { "St Patrick's Day", "Bonfire Night" };
            Seasons = Seasons.Concat(SeasonRule.Defaults().Where(d => added.Contains(d.Name)
                && !Seasons.Any(s => string.Equals(s.Name, d.Name, StringComparison.OrdinalIgnoreCase)))).ToArray();
            ConfigVersion = 5;
        }

        if (ConfigVersion < 6)
        {
            // v6: some film days removed at the user's request; the rest get their own cover designs.
            var removed = new[] { "Mario Day", "Star Trek Day", "Back to the Future Day", "Groundhog Day", "Hobbit Day" };
            Seasons = Seasons.Where(s => !removed.Contains(s.Name, StringComparer.OrdinalIgnoreCase)).ToArray();

            // Only switch designs that are still what an earlier version set, so a chosen decoration is kept.
            var newThemes = new[]
            {
                (Name: "Star Wars Day", Old: "Space", New: "Lightsabers"),
                (Name: "Ghostbusters Day", Old: "Sparkle", New: "Ghosts"),
                (Name: "Back to Hogwarts", Old: "Sparkle", New: "Castle"),
                (Name: "Bonfire Night", Old: "Fireworks", New: "Bonfire")
            };
            foreach (var s in Seasons)
            {
                var change = newThemes.FirstOrDefault(t => string.Equals(t.Name, s.Name, StringComparison.OrdinalIgnoreCase));
                if (change.Name is not null && string.Equals(s.Theme, change.Old, StringComparison.OrdinalIgnoreCase))
                {
                    s.Theme = change.New;
                }
            }

            ConfigVersion = 6;
        }

        if (ConfigVersion < 7)
        {
            // v7: Pancake Day and Chinese New Year.
            var added = new[] { "Pancake Day", "Chinese New Year" };
            Seasons = Seasons.Concat(SeasonRule.Defaults().Where(d => added.Contains(d.Name)
                && !Seasons.Any(s => string.Equals(s.Name, d.Name, StringComparison.OrdinalIgnoreCase)))).ToArray();
            ConfigVersion = 7;
        }

        return ConfigVersion != start;
    }
}
