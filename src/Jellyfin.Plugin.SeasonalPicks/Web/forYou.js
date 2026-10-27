/* Seasonal Picks: web client "For You" screen and home row.
 * Injected into index.html via File Transformation. TV apps don't run this;
 * they get the For You playlist and mirror library instead. */
(function () {
    'use strict';

    var ENABLED = __SP_ENABLED__;
    var HOME_ROW = __SP_HOME_ROW__;
    var HOME_POS = __SP_HOME_POS__; // 0 = top, n = after the nth home row, -1 = bottom
    var HOME_THUMB = __SP_HOME_THUMB__; // landscape (Thumb / backdrop) cards instead of posters
    var TITLE = __SP_TITLE__;
    if (!ENABLED || window.__seasonalPicksLoaded) return;
    window.__seasonalPicksLoaded = true;

    var CACHE_MS = 2 * 60 * 1000;
    var cache = null;
    var cacheAt = 0;
    var cacheUser = null;
    var overlay = null;
    var filter = 'all';

    // ---------- helpers ----------
    function esc(s) {
        return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    function api(method, path, json) {
        return ApiClient.fetch({
            url: ApiClient.getUrl(path),
            type: method,
            dataType: json === false ? undefined : 'json'
        });
    }

    function getPicks(force) {
        var uid = ApiClient.getCurrentUserId();
        if (!force && cache && cacheUser === uid && Date.now() - cacheAt < CACHE_MS) return Promise.resolve(cache);
        return api('GET', 'SeasonalPicks/Picks').then(setCache);
    }

    function setCache(data) {
        cache = data;
        cacheAt = Date.now();
        cacheUser = ApiClient.getCurrentUserId();
        return data;
    }

    function poster(id, h) {
        return ApiClient.getUrl('Items/' + id + '/Images/Primary', { fillHeight: h, quality: 90 });
    }

    // Landscape image: the Thumb if there is one, else the first backdrop (swapped in by onImageError).
    function thumb(id, w, type) {
        return ApiClient.getUrl('Items/' + id + '/Images/' + (type || 'Thumb'), { fillWidth: w, quality: 90 });
    }

    function onImageError(e) {
        var img = e.target;
        if (img.tagName !== 'IMG' || !img.dataset.fallback) return;
        img.src = img.dataset.fallback;
        delete img.dataset.fallback;
    }

    function detailsHref(id) {
        return '#/details?id=' + id + '&serverId=' + ApiClient.serverId();
    }

    function runtime(min) {
        if (!min) return '';
        var h = Math.floor(min / 60), m = min % 60;
        return h ? h + 'h ' + (m ? m + 'm' : '') : m + 'm';
    }

    function seasonTitle(data) {
        return data.activeSeasons && data.activeSeasons.length ? data.activeSeasons.join(' & ') : '';
    }

    function accent(data) {
        return (data.activeSeasonColors && data.activeSeasonColors[0]) || '#3A6EA5';
    }

    // ---------- styles ----------
    function injectStyles() {
        if (document.getElementById('sp-styles')) return;
        var css = [
            '.sp-overlay{position:fixed;top:0;right:0;bottom:0;left:0;z-index:10000;background:rgba(12,12,16,.97);color:#fff;overflow-y:auto;font-family:inherit;-webkit-overflow-scrolling:touch}',
            '.sp-wrap{max-width:1600px;margin:0 auto;padding:24px 16px 48px}',
            '.sp-head{display:flex;flex-wrap:wrap;gap:12px;align-items:center;justify-content:space-between;margin-bottom:16px}',
            '.sp-head h1{margin:0;font-size:1.9em;font-weight:600}',
            '.sp-seasons{display:flex;gap:8px;flex-wrap:wrap;margin-top:6px}',
            '.sp-chip{display:inline-flex;align-items:center;gap:6px;padding:3px 10px;border-radius:999px;font-size:.85em;background:rgba(255,255,255,.08)}',
            '.sp-dot{width:8px;height:8px;border-radius:50%}',
            '.sp-actions{display:flex;gap:8px;flex-wrap:wrap}',
            '.sp-btn{appearance:none;border:0;border-radius:8px;padding:9px 14px;font:inherit;font-size:.95em;cursor:pointer;color:#fff;background:rgba(255,255,255,.1)}',
            '.sp-btn:hover,.sp-btn:focus-visible{background:rgba(255,255,255,.2);outline:none}',
            '.sp-btn-primary{background:var(--sp-accent);color:var(--sp-accent-ink)}',
            '.sp-btn-primary:hover,.sp-btn-primary:focus-visible{filter:brightness(1.1);background:var(--sp-accent)}',
            '.sp-btn[disabled]{opacity:.5;cursor:default}',
            '.sp-filters{display:flex;gap:8px;flex-wrap:wrap;margin:0 0 18px}',
            '.sp-filter{border:1px solid rgba(255,255,255,.18);background:transparent}',
            '.sp-filter[aria-pressed="true"]{background:#fff;color:#111;border-color:#fff}',
            '.sp-grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(150px,1fr));gap:20px 16px}',
            '@media (max-width:600px){.sp-grid{grid-template-columns:repeat(auto-fill,minmax(110px,1fr));gap:14px 10px}.sp-head h1{font-size:1.5em}}',
            '.sp-card{position:relative;min-width:0}',
            '.sp-poster{display:block;position:relative;aspect-ratio:2/3;border-radius:10px;overflow:hidden;background:#222 center/cover no-repeat}',
            '.sp-poster img{width:100%;height:100%;object-fit:cover;display:block}',
            '.sp-reason{display:-webkit-box;-webkit-box-orient:vertical;-webkit-line-clamp:2;overflow:hidden;margin-top:4px;padding-left:8px;border-left:3px solid var(--k);font-size:.82em;line-height:1.35;opacity:.9}',
            '.sp-k-Seasonal{--k:var(--sp-accent)}.sp-k-Unwatched{--k:#4FA3FF}.sp-k-Rediscover{--k:#35C28A}.sp-k-NewRelease{--k:#C792EA}',
            '.sp-name{margin-top:8px;font-size:.95em;font-weight:500;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
            '.sp-sub{font-size:.8em;opacity:.65;margin-top:2px}',
            '.sp-card-actions{position:absolute;top:6px;right:6px;display:flex;gap:4px;opacity:0;transition:opacity .15s}',
            '.sp-card:hover .sp-card-actions,.sp-card:focus-within .sp-card-actions{opacity:1}',
            '@media (hover:none){.sp-card-actions{opacity:1}}',
            '.sp-icon-btn{appearance:none;border:0;width:30px;height:30px;border-radius:50%;cursor:pointer;background:rgba(0,0,0,.7);color:#fff;font-size:14px;line-height:30px;padding:0}',
            '.sp-icon-btn:hover,.sp-icon-btn:focus-visible{background:#fff;color:#111;outline:none}',
            '.sp-foot{margin-top:28px;font-size:.85em;opacity:.6;display:flex;gap:12px;flex-wrap:wrap;align-items:center}',
            '.sp-foot button{background:none;border:0;color:inherit;text-decoration:underline;cursor:pointer;font:inherit;padding:0}',
            '.sp-empty{padding:48px 0;text-align:center;opacity:.7}',
            '.sp-home .sp-row{display:flex;gap:12px;overflow-x:auto;padding:4px 3.3% 12px;scroll-snap-type:x proximity}',
            // Roughly the widths of Jellyfin's own portrait rows, so this row lines up with the others.
            '.sp-home .sp-row .sp-card{flex:0 0 auto;width:30vw;scroll-snap-align:start}',
            '@media (min-width:600px){.sp-home .sp-row .sp-card{width:20vw}}',
            '@media (min-width:1000px){.sp-home .sp-row .sp-card{width:14vw}}',
            '@media (min-width:1600px){.sp-home .sp-row .sp-card{width:11vw}}',
            '.sp-poster.sp-landscape{aspect-ratio:16/9}',
            '.sp-home-thumb .sp-row .sp-card{width:60vw}',
            '@media (min-width:600px){.sp-home-thumb .sp-row .sp-card{width:36vw}}',
            '@media (min-width:1000px){.sp-home-thumb .sp-row .sp-card{width:24vw}}',
            '@media (min-width:1600px){.sp-home-thumb .sp-row .sp-card{width:19vw}}',
            '.sp-home .sectionTitleContainer{display:flex;align-items:center;gap:12px;margin-bottom:10px}',
            // No scrollbar; the arrow buttons (or a swipe) scroll the row instead.
            '.sp-home .sp-row{scrollbar-width:none}',
            '.sp-home .sp-row::-webkit-scrollbar{display:none}',
            '.sp-home .sp-tools{display:flex;gap:6px;margin-left:auto;margin-right:3.3%}',
            '.sp-home .sp-arrow{width:36px;padding:9px 0;font-size:1.1em;line-height:1}',
            '@media (hover:none){.sp-home .sp-arrow{display:none}}'
        ].join('\n');
        var style = document.createElement('style');
        style.id = 'sp-styles';
        style.textContent = css;
        document.head.appendChild(style);
    }

    function inkFor(hex) {
        var m = /^#?([0-9a-f]{6})$/i.exec(hex || '');
        if (!m) return '#fff';
        var n = parseInt(m[1], 16), r = (n >> 16) & 255, g = (n >> 8) & 255, b = n & 255;
        var lum = function (c) { c /= 255; return c <= 0.03928 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4); };
        return 0.2126 * lum(r) + 0.7152 * lum(g) + 0.0722 * lum(b) > 0.4 ? '#111' : '#fff';
    }

    function applyAccent(el, data) {
        var a = accent(data);
        el.style.setProperty('--sp-accent', a);
        el.style.setProperty('--sp-accent-ink', inkFor(a));
    }

    // ---------- cards ----------
    function cardHtml(p, withActions, h, landscape) {
        var sub = [p.year, runtime(p.runtimeMinutes), p.rating ? '★ ' + p.rating.toFixed(1) : ''].filter(Boolean).join(' · ');
        var img = landscape
            ? '<img loading="lazy" alt="" src="' + esc(thumb(p.id, h)) + '" data-fallback="' + esc(thumb(p.id, h, 'Backdrop/0')) + '">'
            : '<img loading="lazy" alt="" src="' + esc(poster(p.id, h)) + '">';
        return '<div class="sp-card" data-id="' + esc(p.id) + '" data-kind="' + esc(p.kind) + '">' +
            '<a class="sp-poster' + (landscape ? ' sp-landscape' : '') + '" href="' + detailsHref(p.id) + '" title="' + esc(p.overview || p.name) + '">' +
            img + '</a>' +
            (withActions
                ? '<div class="sp-card-actions">' +
                  '<button class="sp-icon-btn" data-act="snooze" title="Not now (swap for 30 days)" aria-label="Not now">↻</button>' +
                  '<button class="sp-icon-btn" data-act="hide" title="Never suggest this" aria-label="Never suggest">✕</button></div>'
                : '') +
            '<div class="sp-name">' + esc(p.name) + '</div>' +
            '<div class="sp-sub">' + esc(sub) + '</div>' +
            (p.reason ? '<div class="sp-reason sp-k-' + esc(p.kind) + '" title="' + esc(p.reason) + '">' + esc(p.reason) + '</div>' : '') +
            '</div>';
    }

    // ---------- overlay screen ----------
    function openOverlay() {
        injectStyles();
        if (!overlay) {
            overlay = document.createElement('div');
            overlay.className = 'sp-overlay';
            overlay.setAttribute('role', 'dialog');
            overlay.setAttribute('aria-modal', 'true');
            overlay.setAttribute('aria-label', TITLE);
            overlay.addEventListener('click', onOverlayClick);
            document.addEventListener('keydown', function (e) {
                if (e.key === 'Escape' && overlay && overlay.isConnected) closeOverlay();
            });
        }
        overlay.innerHTML = '<div class="sp-wrap"><div class="sp-empty">Loading your picks…</div></div>';
        document.body.appendChild(overlay);
        document.documentElement.style.overflow = 'hidden';
        getPicks(false).then(renderOverlay, renderError);
    }

    function closeOverlay() {
        if (overlay) overlay.remove();
        document.documentElement.style.overflow = '';
    }

    function renderError() {
        if (!overlay) return;
        overlay.querySelector('.sp-wrap').innerHTML =
            '<div class="sp-empty">Couldn’t load picks. <button class="sp-btn" data-act="close">Close</button></div>';
    }

    function renderOverlay(data) {
        if (!overlay || !overlay.isConnected) return;
        applyAccent(overlay, data);
        var chips = (data.activeSeasons || []).map(function (s, i) {
            return '<span class="sp-chip"><span class="sp-dot" style="background:' + esc(data.activeSeasonColors[i]) + '"></span>' + esc(s) + ' season</span>';
        }).join('');
        var counts = { all: data.picks.length, Seasonal: 0, NewRelease: 0, Unwatched: 0, Rediscover: 0 };
        data.picks.forEach(function (p) { counts[p.kind] = (counts[p.kind] || 0) + 1; });
        var filters = [['all', 'All'], ['Seasonal', 'In season'], ['NewRelease', 'New releases'], ['Unwatched', 'New to you'], ['Rediscover', 'Rediscover']]
            .filter(function (f) { return f[0] === 'all' || counts[f[0]]; })
            .map(function (f) {
                return '<button class="sp-btn sp-filter" data-filter="' + f[0] + '" aria-pressed="' + (filter === f[0]) + '">' + f[1] + ' (' + counts[f[0]] + ')</button>';
            }).join('');
        var shown = data.picks.filter(function (p) { return filter === 'all' || p.kind === filter; });
        var updated = data.generatedAt ? new Date(data.generatedAt).toLocaleString() : 'never';

        overlay.querySelector('.sp-wrap').innerHTML =
            '<div class="sp-head"><div><h1>' + esc(TITLE) + '</h1><div class="sp-seasons">' + chips + '</div></div>' +
            '<div class="sp-actions">' +
            '<button class="sp-btn sp-btn-primary" data-act="reroll">↻ Reroll</button>' +
            (data.playlistId ? '<button class="sp-btn" data-act="playlist">Open playlist</button>' : '') +
            '<button class="sp-btn" data-act="close" aria-label="Close">✕</button></div></div>' +
            '<div class="sp-filters">' + filters + '</div>' +
            (shown.length
                ? '<div class="sp-grid">' + shown.map(function (p) { return cardHtml(p, true, 420); }).join('') + '</div>'
                : '<div class="sp-empty">Nothing here right now. Try Reroll.</div>') +
            '<div class="sp-foot"><span>Updated ' + esc(updated) + '</span>' +
            (data.hiddenCount ? '<span>' + data.hiddenCount + ' hidden or snoozed · <button data-act="unhide">bring them back</button></span>' : '') +
            '</div>';
        overlay.dataset.playlist = data.playlistId || '';
    }

    function onOverlayClick(e) {
        var btn = e.target.closest('button,[data-act],a.sp-poster');
        if (!btn) return;
        if (btn.matches('a.sp-poster')) { closeOverlay(); return; } // let the link navigate
        var f = btn.getAttribute('data-filter');
        if (f) { filter = f; renderOverlay(cache); return; }
        var act = btn.getAttribute('data-act');
        if (act === 'close') closeOverlay();
        else if (act === 'playlist') { closeOverlay(); location.hash = detailsHref(overlay.dataset.playlist); }
        else if (act === 'reroll') {
            btn.disabled = true;
            btn.textContent = 'Picking…';
            api('POST', 'SeasonalPicks/Refresh').then(setCache).then(function (d) { renderOverlay(d); refreshHomeRow(); }, renderError);
        } else if (act === 'unhide') {
            api('POST', 'SeasonalPicks/Unhide', false).then(function () { return getPicks(true); }).then(renderOverlay, renderError);
        } else if (act === 'snooze' || act === 'hide') {
            var card = btn.closest('.sp-card');
            card.style.opacity = '.35';
            api('POST', 'SeasonalPicks/Picks/' + card.getAttribute('data-id') + '/Swap?forever=' + (act === 'hide'))
                .then(setCache).then(function (d) { renderOverlay(d); refreshHomeRow(); }, renderError);
        }
    }

    // ---------- home row ----------
    var homeBusy = false;

    function addHomeRow() {
        if (!HOME_ROW || homeBusy || !ApiClient.getCurrentUserId()) return;
        var container = document.querySelector('.homeSectionsContainer');
        if (!container || container.querySelector('[data-sp-home]')) return;
        homeBusy = true;
        getPicks(false).then(function (data) {
            homeBusy = false;
            if (!data.picks.length || container.querySelector('[data-sp-home]') || !container.isConnected) return;
            injectStyles();
            var section = document.createElement('div');
            section.className = 'verticalSection sp-home' + (HOME_THUMB ? ' sp-home-thumb' : '');
            section.setAttribute('data-sp-home', '1');
            section.addEventListener('error', onImageError, true); // error doesn't bubble; capture it
            applyAccent(section, data);
            fillHomeRow(section, data);
            section.addEventListener('click', function (e) {
                var btn = e.target.closest('[data-act]');
                if (!btn) return;
                var act = btn.getAttribute('data-act');
                if (act === 'see-all') { e.preventDefault(); openOverlay(); }
                else if (act === 'prev' || act === 'next') {
                    var row = section.querySelector('.sp-row');
                    row.scrollBy({ left: (act === 'next' ? 1 : -1) * row.clientWidth * 0.9, behavior: 'smooth' });
                }
            });
            section.addEventListener('scroll', function () { updateArrows(section); }, true); // scroll doesn't bubble
            placeHomeRow(container, section);
            updateArrows(section);
        }, function () { homeBusy = false; });
    }

    // Home rows are .section0, .section1 … in the user's chosen order; they exist (empty) before they load.
    function placeHomeRow(container, section) {
        var after = HOME_POS > 0 ? container.querySelector(':scope > .section' + (HOME_POS - 1)) : null;
        if (after) after.after(section);
        else if (HOME_POS === 0) container.insertBefore(section, container.firstChild);
        else container.appendChild(section); // bottom, or fewer rows than asked for
    }

    function fillHomeRow(section, data) {
        var season = seasonTitle(data);
        section.innerHTML =
            '<div class="sectionTitleContainer sectionTitleContainer-cards padded-left">' +
            '<h2 class="sectionTitle sectionTitle-cards">' + esc(TITLE) + (season ? ' · ' + esc(season) : '') + '</h2>' +
            '<div class="sp-tools">' +
            '<button class="sp-btn sp-arrow" data-act="prev" aria-label="Scroll left">‹</button>' +
            '<button class="sp-btn sp-arrow" data-act="next" aria-label="Scroll right">›</button>' +
            '<button class="sp-btn" data-act="see-all">See all</button></div></div>' +
            '<div class="sp-row">' + data.picks.slice(0, 15).map(function (p) { return HOME_THUMB ? cardHtml(p, false, 480, true) : cardHtml(p, false, 300); }).join('') + '</div>';
    }

    function updateArrows(section) {
        var row = section.querySelector('.sp-row');
        if (!row) return;
        section.querySelector('[data-act="prev"]').disabled = row.scrollLeft <= 1;
        section.querySelector('[data-act="next"]').disabled = row.scrollLeft + row.clientWidth >= row.scrollWidth - 1;
    }

    function refreshHomeRow() {
        var section = document.querySelector('[data-sp-home]');
        if (section && cache) { fillHomeRow(section, cache); updateArrows(section); }
    }

    // ---------- boot ----------
    function start() {
        var scheduled = false;
        var observer = new MutationObserver(function () {
            if (scheduled) return;
            scheduled = true;
            requestAnimationFrame(function () {
                scheduled = false;
                addHomeRow();
            });
        });
        observer.observe(document.body, { childList: true, subtree: true });
        window.addEventListener('resize', function () {
            var section = document.querySelector('[data-sp-home]');
            if (section) updateArrows(section);
        });
        addHomeRow();
        window.SeasonalPicks = { open: openOverlay };
    }

    (function waitForClient(tries) {
        if (window.ApiClient && window.Dashboard && ApiClient.getCurrentUserId) start();
        else if (tries < 240) setTimeout(function () { waitForClient(tries + 1); }, 250);
    })(0);
})();
