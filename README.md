# Seasonal Picks (Jellyfin 12.1 plugin)

Picks movies for each user from their watch history and the time of year, then puts them where every client can see them, **TV apps included**.

## What you get

| Output | Where it shows | Needs |
|---|---|---|
| **Suggested playlist** per user (private, refreshed daily; a pick is swapped out as soon as it's watched; the description lists why each film was picked) | Every app: TV, mobile, web (Playlists) | nothing |
| **Season playlists**, e.g. "Halloween Picks", while a season is on (removed when it ends) | Every app | nothing |
| **Mirror library folder**: symlinks to one user's picks | Every app, as its own library tile + home row (like your Synology setup) | a library pointed at the folder |
| **Seasonal covers** on the playlist and mirror library | Every app | nothing (SkiaSharp ships with Jellyfin) |
| **Suggested screen** (user menu) + **home row** with reroll / "not now" / "never" | Web client only | [File Transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation) |

Tizen/webOS apps bundle their own web client, so injected screens never reach them. That's why the TV path is playlists, the mirror library, and covers.

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

**Preview** at the bottom of the settings page shows what any user would get with the settings on the page, without writing anything. Use it before the first real refresh.

### Seasons

Each season has a date window (can wrap the new year), keywords (whole words in title/overview), tags, genres, a boost and a colour. Keyword/tag hits are strong matches; genre hits are weaker. **Hide out of season** keeps strong matches out for the rest of the year.

Defaults: Halloween (Oct), Christmas (Nov 20 – Dec 31), Easter (two weeks before Easter Sunday to Easter Monday, following the real date each year), Valentine's (Feb 1–14), New Year (Dec 27 – Jan 2). Each comes with common words, famous titles and TMDb keyword tags. Edit, add or remove them in the plugin settings; Easter-style dates are written `easter-14` / `easter+1`.

There are also special days that push their films to the front on the day but never hide them the rest of the year: Star Wars Day (May 4–5), Ghostbusters Day (Jun 8), Back to Hogwarts (Sep 1), St Patrick's Day (Mar 17) and Bonfire Night (Nov 1–5).

**Add a day or season** offers ready-made extras (Alien Day, James Bond Day, Talk Like a Pirate Day, Sherlock Holmes Day, Pride Month, Remembrance, Winter Warmers) plus a blank day or season to fill in yourself.

Holidays hide their films the rest of the year. A film that matches several seasons belongs to the one with the highest boost, so *Christmas Vacation* is a Christmas film (shown in December, hidden in July). Genre-only matches (e.g. Horror for Halloween) only boost in season; they never hide.

Season dates use the **server's local date**. Set `TZ` on the container (e.g. `TZ=Europe/London`) or it runs on UTC.

### Covers

For the active season, in this order:
1. A finished cover you uploaded for that season, used as it is (settings page, per season, plus one "out of season" cover).
2. Otherwise a generated cover: the current picks' posters in the season colour, decorated for the season (a Christmas tree in the snow, pumpkins/bats/moon, an Easter bunny with eggs, hearts, fireworks, shamrocks, crossed lightsabers, ghosts, a lit castle, a bonfire, or sparkles out of season) with the name large in the centre. Square for playlists, 16:9 for the library tile.

**Design cover** (per season, and for out of season) opens a designer with a live preview. It sets the decoration, the title text, size, position (top, middle or bottom) and colour, the small label above the title, and the line under it. The background can be the picks' posters, **your own picture** (upload one and the decorations and title are drawn over it), or plain season colour. Sliders set the colour wash, darkness and how strong the decorations are. The preview is drawn by the server, so it's exactly what TVs will show.

Covers update on every refresh. Uploads follow a season when you rename it (moved on Save).

## Install

```powershell
.\build.ps1
```

1. Copy `dist\SeasonalPicks\Jellyfin.Plugin.SeasonalPicks.dll` to `/config/plugins/SeasonalPicks/` in the container.
2. Install **File Transformation** (for the web screen) if it isn't already.
3. Restart the container, then clear the browser cache.
4. Dashboard → Plugins → **Seasonal Picks**: set things up, then **Save and refresh everyone now**.

The refresh runs daily at 04:00 (Dashboard → Scheduled Tasks → *Refresh Seasonal Picks* to change it).

### Replacing the Synology script (mirror folder)

1. **Disable the Synology script first**, so the two don't fight over the folder.
2. In the settings, enable **Mirror library folder**, set the path *as the container sees it* (the folder your custom library already uses is fine) and pick whose history drives it.
3. Save and refresh. The plugin removes the old symlinks there and writes new ones.

Safety: the plugin only ever creates or deletes **symlinks** in that folder. Real files and folders are never touched. Movies in their own folder are linked by folder (keeps posters, nfo and subtitles); loose files are linked by file. Movies in the mirror library share watched state with the originals (Jellyfin matches them by provider IDs), and the mirror library is never used as a pick source.

## API (for the curious)

All under `/SeasonalPicks`, authenticated as the calling user:

- `GET Picks`, `POST Refresh`, `POST Picks/{id}/Swap?forever=true|false`, `POST Unhide`
- Admin: `POST RefreshAll`, `POST|DELETE Covers/{key}`, `GET Covers/{key}/Preview?wide=`, `POST Preview?userId=` (body: settings JSON, or empty for the saved ones)

State lives in `/config/plugins/configurations/…` (settings) and the plugin's data folder (`users/*.json`, `covers/*`).

## Tests

```powershell
dotnet test tests\Jellyfin.Plugin.SeasonalPicks.Tests
```

Covers the pick rules (seasons, hide out of season, history windows, copies, collections, genre cap, season playlists, replacements), settings and state files, cover rendering, and that the page scripts parse.

## Troubleshooting

- **No web screen**: check the server log for `SeasonalPicks: registered web client injection`. If it says File Transformation wasn't found, install it and restart. Then clear site data in the browser.
- **Cover didn't change**: TVs cache images hard. Back out of the library and reopen. Check the log for `cover generation failed`; uploading covers always works.
- **Picks look stale**: run the scheduled task, or press Reroll on the web screen.
