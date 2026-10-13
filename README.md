<p align="center">
  <img src="docs/logo.png" width="320" alt="Seasonal Picks logo">
</p>

<h1 align="center">Seasonal Picks for Jellyfin</h1>

<p align="center">
  Picks movies for each user from their <b>watch history</b> and the <b>time of year</b>,
  and puts them where every client can see them, <b>TV apps included</b>.
</p>

<p align="center">
  <a href="https://github.com/TNT24/jellyfin-seasonal-picks/releases/latest"><img src="https://img.shields.io/github/v/release/TNT24/jellyfin-seasonal-picks?label=release" alt="Latest release"></a>
  <img src="https://img.shields.io/badge/Jellyfin-12.1-00a4dc" alt="Jellyfin 12.1">
  <img src="https://img.shields.io/badge/works%20on-TV%20%7C%20mobile%20%7C%20web-aa5cc3" alt="Works on TV, mobile and web">
</p>

---

## Why?

Jellyfin's home screen shows what you're already watching and what was added last, but it never says "it's October,
how about a horror film you haven't seen in two years?" Web-only add-ons can't help on a TV either: Tizen and webOS apps
bundle their own web client, so injected screens never reach them.

Seasonal Picks works out a fresh list for each user every day and publishes it in ways every app already understands:

| Output | Where it shows | Needs |
|---|---|---|
| **Suggested playlist** per user (private, refreshed daily; a pick is swapped out as soon as it's watched; the description lists why each film was picked) | Every app: TV, mobile, web (Playlists) | Nothing |
| **Season playlists**, e.g. "Halloween Picks", while a season is on (removed when it ends) | Every app | Nothing |
| **Mirror library folder**: symlinks to one user's picks | Every app, as its own library tile and home row | A library pointed at the folder |
| **Seasonal covers** on the playlist and mirror library | Every app | Nothing (SkiaSharp ships with Jellyfin) |
| **Suggested screen** (user menu) and **home row** with reroll / "not now" / "never" | Web client only | [File Transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation) |

## Features

- **Knows what you've seen.** Recently watched and half-watched films are left out; long-forgotten favourites come back as rediscoveries.
- **Follows the calendar.** Halloween, Christmas, Easter, Valentine's and New Year out of the box, plus special days like Pancake Day, Chinese New Year and Star Wars Day. Holiday films stay hidden the rest of the year.
- **Keeps the list moving.** Watch a pick or mark it played and it's replaced straight away. Last refresh's picks are down-weighted so the list rotates.
- **Understands collections and copies.** The next film in a series you've started is favoured, and 4K and HD copies of the same film count as one.
- **Draws its own covers.** Playlist and library covers are drawn on the server for each season, with a new scene each week in the longer seasons. Design them in a live-preview designer, or upload your own.
- **Preview before you commit.** See exactly what any user would get with the settings on the page, without writing anything.

---

## Installation

### From the plugin repository (recommended)

1. In Jellyfin, open **Dashboard → Plugins → Repositories** (the gear on the Catalog tab in some versions) and click **+**.
2. Add the repository:
   - **Name:** `Seasonal Picks`
   - **URL:**
     ```
     https://raw.githubusercontent.com/TNT24/jellyfin-seasonal-picks/main/manifest.json
     ```
3. Go to **Catalog**, find **Seasonal Picks** under *General*, and click **Install**.
4. Optional: install [File Transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation) for the web client's Suggested screen and home row.
5. **Restart Jellyfin**, then clear the browser cache.

New releases appear in the catalog as updates.

### Manual install

1. Download the zip from the [latest release](https://github.com/TNT24/jellyfin-seasonal-picks/releases/latest), or build it yourself (see [Building from source](#building-from-source)).
2. Extract the four files into `SeasonalPicks/` in your Jellyfin plugins folder:

   | Platform | Plugins folder |
   |---|---|
   | Windows | `%ProgramData%\Jellyfin\Server\plugins\` |
   | Linux | `/var/lib/jellyfin/plugins/` |
   | Docker | `/config/plugins/` |
3. Restart Jellyfin.

---

## Getting started

1. Open **Seasonal Picks** from the Jellyfin sidebar, or from *Dashboard → Plugins*.
2. Look over the shares and windows (the defaults are a good start) and the seasons list.
3. Use **Preview** at the bottom of the page to see what a user would get.
4. Click **Save and refresh everyone now**.

After that the refresh runs daily at 04:00 (*Dashboard → Scheduled Tasks → Refresh Seasonal Picks* to change it).

## How picks work

Movies only. Every candidate lands in one bucket:

- **Excluded**: watched within *Hide recently watched* (60 days), half-watched (already in Continue Watching), hidden or snoozed by the user.
- **Unwatched**: never played. "Recently added" (last 30 days) gets a boost.
- **New releases**: unwatched films released within *Counts as new for* (365 days). They get their own *New releases share* (20%) of the list and a boost in the rest.
- **Rediscover**: last played more than *Rediscover after* (365 days) ago. Favourites and liked movies, and longer gaps, weigh more.
- Anything between those two windows is cooling down and skipped.
- **Copies** of the same film (same TMDb/IMDb id, e.g. 4K and HD, or another library) count as one film. The widest copy is picked, and watching any copy counts.
- **Collections** (box sets, in release order): the next film in a collection you've started is favoured ("Next in …"); an unwatched film whose earlier parts are unwatched is skipped. Turn off with *Follow collections*.

Then:

1. If a season is active, **In-season share** (40%) of the picks come from movies matching it.
2. **Rediscover share** (30%) of the rest are rediscoveries; the remainder are unwatched.
3. A short pool is backfilled from the others. Last refresh's picks are down-weighted so the list rotates.
4. Community rating nudges the weight (unrated = neutral).
5. No first-listed genre may fill more than *Max share per genre* (30%) of the list while other genres are available. In-season picks are exempt but count towards it.

When a user finishes a pick or marks it played, it's replaced straight away (playlist, season playlist and, for the mirror user, the mirror folder). "Not now" / "Never" on the web screen do the same.

## Seasons

Each season has a date window (can wrap the new year), keywords (whole words in title/overview), tags, genres, a boost and a colour. Keyword/tag hits are strong matches; genre hits are weaker. **Hide out of season** keeps strong matches out for the rest of the year.

| Built in | Dates |
|---|---|
| Halloween | October |
| Christmas | Nov 20 – Dec 31 |
| Easter | Two weeks before Easter Sunday to Easter Monday, following the real date each year |
| Valentine's | Feb 1 – 14 |
| New Year | Dec 27 – Jan 2 |

Each comes with common words, famous titles and TMDb keyword tags. Edit, add or remove them in the plugin settings. Dates that move each year are written relative to Easter Sunday (`easter-14` / `easter+1`) or to Chinese New Year (`lunar+0` / `lunar+14`).

There are also **special days** that push their films to the front on the day but never hide them the rest of the year: Chinese New Year (New Year's Day to the Lantern Festival, following the lunar calendar), Pancake Day (Shrove Tuesday, 47 days before Easter), St Patrick's Day (Mar 17), Star Wars Day (May 4–5), Ghostbusters Day (Jun 8), Back to Hogwarts (Sep 1) and Bonfire Night (Nov 1–5).

**Add a day or season** offers ready-made extras (Alien Day, James Bond Day, Talk Like a Pirate Day, Sherlock Holmes Day, Pride Month, Remembrance, Winter Warmers) plus a blank day or season to fill in yourself.

## Covers

For the active season, in this order:

1. A finished cover you uploaded for that season, used as it is (settings page, per season, plus one "out of season" cover).
2. Otherwise a generated cover: the current picks' posters in the season colour, decorated for the season, with the name large in the centre. Square for playlists, 16:9 for the library tile. Decorations are kept clear of the title.

Longer seasons change scene every week, then start again:

| Season | Scenes, week by week |
|---|---|
| Halloween | Pumpkin patch, graveyard, haunted house, witch's cauldron |
| Christmas | Christmas tree, living room by the fire, Santa's sleigh, baubles and holly |
| Winter (snow) | Snowfall, snowy forest |
| Easter | Bunny and eggs, basket of chicks |
| Valentine's | Floating hearts, hearts and roses, heart balloons |
| Chinese New Year | Red lanterns, lantern festival |

Special days have one scene each: fireworks over a clock tower (New Year), a stack of pancakes (Pancake Day), a rainbow and a pot of gold (St Patrick's Day), clashing lightsabers, ghosts over the city, a castle on a lake with floating candles, and a bonfire. Out of season it's a night at the pictures.

**Design cover** (per season, and for out of season) opens a designer with a live preview. It sets the decoration and **scene** (change each week, or always one), the title text, size, position (top, middle or bottom) and colour, the small label above the title, and the line under it. The background can be the picks' posters, **your own picture** (upload one and the decorations and title are drawn over it), or plain season colour. Sliders set the colour wash, darkness and how strong the decorations are. The preview is drawn by the server, so it's exactly what TVs will show.

Covers update on every refresh. Uploads follow a season when you rename it (moved on Save).

## Good to know

**Why does a Christmas film disappear in July?**
Holidays hide their films the rest of the year. A film that matches several seasons belongs to the one with the highest
boost, so *Christmas Vacation* is a Christmas film (shown in December, hidden in July). Genre-only matches (e.g. Horror
for Halloween) only boost in season; they never hide.

**Which date does it use?**
The **server's local date**. Set `TZ` on the container (e.g. `TZ=Europe/London`) or it runs on UTC.

**Replacing a Synology script with the mirror folder**
1. **Disable the Synology script first**, so the two don't fight over the folder.
2. In the settings, enable **Mirror library folder**, set the path *as the container sees it* (the folder your custom
   library already uses is fine) and pick whose history drives it.
3. Save and refresh. The plugin removes the old symlinks there and writes new ones.

**Is the mirror folder safe?**
The plugin only ever creates or deletes **symlinks** in that folder. Real files and folders are never touched. Movies in
their own folder are linked by folder (keeps posters, nfo and subtitles); loose files are linked by file. Movies in the
mirror library share watched state with the originals (Jellyfin matches them by provider IDs), and the mirror library is
never used as a pick source.

**Where is everything stored?**
Settings live in `/config/plugins/configurations/…`; per-user state (`users/*.json`) and covers (`covers/*`) live in the
plugin's data folder.

**Troubleshooting**
- **No web screen**: check the server log for `SeasonalPicks: registered web client injection`. If it says File
  Transformation wasn't found, install it and restart. Then clear site data in the browser.
- **Cover didn't change**: TVs cache images hard. Back out of the library and reopen. Check the log for
  `cover generation failed`; uploading covers always works.
- **Picks look stale**: run the scheduled task, or press Reroll on the web screen.

## API

All under `/SeasonalPicks`, authenticated as the calling user:

- `GET Picks`, `POST Refresh`, `POST Picks/{id}/Swap?forever=true|false`, `POST Unhide`
- Admin: `POST RefreshAll`, `POST Preview?userId=` (body: settings JSON, or empty for the saved ones), `GET Changelog`
- Admin, covers: `POST|DELETE Covers/{key}` (finished cover), `POST|DELETE Covers/{key}/Background` (picture behind the design), `POST Covers/{key}/Rename?to=`, `GET Covers/{key}/Preview?wide=`, `POST Covers/Render?wide=&useUpload=` (body: a design from the settings page)

---

## Building from source

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download) and PowerShell.

```powershell
.\build.ps1                                                 # builds dist\SeasonalPicks\ and dist\SeasonalPicks_<version>.zip
dotnet test tests\Jellyfin.Plugin.SeasonalPicks.Tests
```

The build produces the DLL, `meta.json`, the logo and the font licence. `meta.json`'s changelog comes from the newest entry in
[CHANGELOG.md](CHANGELOG.md), and the build stops if that entry doesn't match the project version.

The tests cover the pick rules (seasons, hide out of season, history windows, copies, collections, genre cap, season
playlists, replacements), settings and state files, cover rendering, and that the page scripts parse.

### Publishing a release

1. Add an entry at the top of `CHANGELOG.md` and bump the version in the `.csproj` (a test checks the two match). The
   settings page lists every release under **What's new**.
2. Run `.\build.ps1`. It also adds the version to `manifest.json`.
3. Create the GitHub release tagged `v<version>` (e.g. `v1.2.0`) and attach the zip:
   ```powershell
   gh release create v<version> dist\SeasonalPicks_<version>.0.zip   # e.g. v1.2.2 and SeasonalPicks_1.2.2.0.zip
   ```
4. Commit and push `manifest.json`. Jellyfin servers will see the update.

## Credits

Generated covers use [Noto Sans](https://fonts.google.com/noto/specimen/Noto+Sans) (SIL Open Font License). This plugin
is not affiliated with the Jellyfin project.
