# Changelog

The settings page shows this list under **What's new**. The newest version goes first.

## 1.4.0 - 2026-10-27
- Each pick's reason is now a readable line under the title, instead of small text over the poster.
- The home row's cards are the same size as Jellyfin's other home rows.
- Choose where the home row goes: at the top, after any of the first six rows, or at the bottom.
- Choose portrait (poster) or thumb (landscape) cards for the home row.

## 1.3.2 - 2026-10-24
- Fixed the web Suggested screen and home row sometimes not appearing, especially alongside Theme Builder or other plugins that add to the web client. The plugin now keeps retrying at startup, and checks every few minutes that the web client still has the screen.

## 1.3.1 - 2026-10-13
- Now also runs on Jellyfin 10.11. Each release comes in two builds, one for 10.11 and one for 12, and Jellyfin installs the right one by itself.

## 1.3.0 - 2026-10-13
- Longer seasons now change their cover each week: Halloween (pumpkin patch, graveyard, haunted house, witch's cauldron), Christmas (tree, living room by the fire, Santa's sleigh, baubles and holly), winter (snowfall, snowy forest), Easter (bunny and eggs, basket of chicks), Valentine's (floating hearts, hearts and roses, heart balloons) and Chinese New Year (red lanterns, lantern festival).
- New Scene option in the cover designer: change each week, or always use one scene. It also lets you preview every scene.
- New days: Pancake Day (Shrove Tuesday, follows Easter) and Chinese New Year (New Year's Day to the Lantern Festival, follows the lunar calendar). Dates relative to Chinese New Year are written lunar+0, lunar+14 and so on.
- Remastered covers for New Year, Star Wars Day, Ghostbusters Day, Back to Hogwarts, St Patrick's Day, Bonfire Night and the out-of-season cover.
- Covers are more detailed, and decorations no longer cross the title or each other.
- The plugin now updates itself: new releases are installed by Jellyfin's Update Plugins task.

## 1.2.2 - 2026-10-13
- New logo, shown in the plugin catalog and on the plugin's page in the dashboard.
- Rewritten README with install steps, a getting started guide and answers to common questions.

## 1.2.1 - 2026-10-11
- Season rows on the settings page are no longer squashed: the name and dates show in full, and on phones the badges and the On box move to a second line.
- The settings page uses more of a wide screen, and the Picks settings sit in three tidy columns.
- Buttons such as Remove and Upload picture are readable again, and "Remove finished cover" only shows when there is an uploaded cover.
- What's new loads again on Jellyfin 12.1.
- On phones, the chosen tab scrolls into view.
- The Suggested screen fills the whole screen in older browsers.

## 1.2.0 - 2026-09-27
- Seasonal Picks now has its own entry in the dashboard side menu.
- New look for the settings page: tabs, a header showing what's on today and what's next, and seasons that fold up into a one-line summary with a cover thumbnail.
- The Save buttons stay at the bottom of the screen while you scroll.
- This changelog, under What's new.

## 1.1.0 - 2026-09-27
- Cover designer with a live preview: decoration, title text, size, position and colour, the label, and the background (posters, your own picture or plain colour), with colour wash, darkness and decoration sliders.
- Add a day or season from a ready-made list: Alien Day, James Bond Day, Talk Like a Pirate Day, Sherlock Holmes Day, Pride Month, Remembrance and Winter Warmers, or a blank one.
- Uploaded covers and pictures now follow a season when you rename it.

## 1.0.9 - 2026-09-27
- New cover scenes: crossed lightsabers (Star Wars Day), a decorated Christmas tree, ghosts, a lit castle (Back to Hogwarts) and a bonfire.
- Removed Mario Day, Star Trek Day, Back to the Future Day, Groundhog Day and Hobbit Day.

## 1.0.8 - 2026-09-27
- Added St Patrick's Day and Bonfire Night.
- Shamrock covers, and an Easter bunny on the Easter cover.

## 1.0.7 - 2026-09-27
- Removed the Summer season.
- Added New Year and film days (Star Wars Day and others), which push their films forward on the day without hiding them the rest of the year.
- Fireworks and space cover designs.

## 1.0.6 - 2026-09-27
- Everything is now called "Suggested".
- More detailed bats on the Halloween cover.

## 1.0.5 - 2026-09-27
- Cover titles use Noto Sans, Jellyfin's own font, so they look right on any server.

## 1.0.4 - 2026-09-26
- Themed covers (snow, pumpkins and bats, eggs, hearts) with the season's name large in the centre.

## 1.0.3 - 2026-09-26
- Added the Easter season, which follows Easter's real date each year.
- Every season keeps its films out of picks for the rest of the year.
- Longer keyword and tag lists, including famous titles.
- A share of the picks is kept for new releases.

## 1.0.2 - 2026-09-26
- Cover art on the Suggested library.
- Option to clear anything in the mirror folder that isn't one of the plugin's links.

## 1.0.1 - 2026-09-26
- Fixed playlist covers being replaced by Jellyfin's default collage.

## 1.0.0 - 2026-09-26
- First release: picks from each user's watch history and the time of year, as playlists, a mirror library folder, a web Suggested screen and seasonal covers.
