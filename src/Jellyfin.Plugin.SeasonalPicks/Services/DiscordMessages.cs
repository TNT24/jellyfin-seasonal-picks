using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>
/// One film in a Discord message. <paramref name="Poster"/>: its uploaded poster (<c>attachment://…</c>), if any;
/// <paramref name="Rating"/>: its age rating, such as PG.
/// </summary>
public sealed record DiscordFilm(Guid ItemId, string Name, int? Year, string Reason, string? Poster = null, TimeSpan? Runtime = null, string? Rating = null);

/// <summary>Builds Discord webhook payloads. No network access, so it can be unit tested.</summary>
public static class DiscordMessages
{
    // Discord's limits: 2000 characters of content, 4096 per embed description, and 6000 across all embeds' text in
    // one message (one picks card stays well under that: 4096 + a short footer).
    internal const int MaxContent = 2000;
    internal const int MaxDescription = 4096;
    private const int MaxPicks = 10;
    private const int MaxReason = 300;
    private const int MaxName = 200;
    private const int ListSeasonFilmsUpTo = 10;
    private const int DefaultColor = 0x00A4DC;

    /// <summary>A season (or special day) has started. <paramref name="lastDay"/> equal to <paramref name="today"/> = a one-day event.</summary>
    /// <param name="banner">The season's cover, shown full width under the films (<c>attachment://…</c>); null = none.</param>
    public static JsonObject SeasonStarted(string season, string color, DateTime today, DateTime lastDay, string? playlistName, int playlistUsers, IReadOnlyList<DiscordFilm> films, string publicServerUrl, string? banner = null)
    {
        var baseUrl = publicServerUrl.Trim().TrimEnd('/');
        var oneDay = lastDay.Date <= today.Date;
        var name = EscapeAndTruncate(season, MaxName);
        var content = oneDay
            ? $"🎉 **It's {name}!**"
            : $"🎉 **{name} is here!** Until {lastDay.ToString("d MMMM", CultureInfo.InvariantCulture)}.";
        if (playlistName is not null && playlistUsers > 0)
        {
            content += $" {Plural(playlistUsers, "user has", "users have")} a new **{EscapeAndTruncate(playlistName, MaxName)}** playlist.";
        }

        var embeds = new List<JsonObject>();
        if (films.Count > 0 || banner is not null)
        {
            var embed = new JsonObject { ["color"] = Color(color) };
            if (films.Count > 0)
            {
                embed["title"] = oneDay ? "Something to watch today" : "In the mood? From your library";
                embed["description"] = string.Join('\n', films.Take(ListSeasonFilmsUpTo).Select(f => "• " + Link(f, baseUrl)));
            }

            if (banner is not null)
            {
                embed["image"] = new JsonObject { ["url"] = banner };
            }

            embeds.Add(embed);
        }

        return Message(content, embeds);
    }

    /// <summary>A season has ended. <paramref name="hadPlaylists"/>: it had playlists, which have now been removed.</summary>
    public static JsonObject SeasonEnded(string season, bool hadPlaylists)
        => Message(
            $"👋 **{EscapeAndTruncate(season, MaxName)} is over.**" + (hadPlaylists ? " Its playlists have been removed until next time." : " See you next time."),
            []);

    /// <summary>
    /// One user's top picks for the day, as a single card: a numbered list (title, year, runtime, age rating and the
    /// reason), the top pick's poster beside it, and the season in the footer. The server address links each title to
    /// Jellyfin, and gives the poster when none was uploaded.
    /// </summary>
    /// <param name="lastDay">The last day of the one active season, for "until …"; null when there are none or several.</param>
    public static JsonObject DailyPicks(string userName, IReadOnlyList<string> seasons, IReadOnlyList<DiscordFilm> films, string color, string publicServerUrl, DateTime today, DateTime? lastDay = null)
    {
        var baseUrl = publicServerUrl.Trim().TrimEnd('/');
        var content = $"🍿 **Today's picks for {EscapeAndTruncate(userName, MaxName)}**" + (seasons.Count > 0 ? $" · {EscapeAndTruncate(string.Join(" & ", seasons), MaxName * 2)}" : string.Empty);
        if (films.Count == 0)
        {
            return Message(content, []);
        }

        // Long titles and reasons can pass Discord's 4096 characters per description, and then it refuses the whole
        // message (every day). Leave the last films off until it fits.
        var description = new StringBuilder();
        foreach (var (film, i) in films.Take(MaxPicks).Select((f, i) => (f, i)))
        {
            var line = PickLine(film, i + 1, baseUrl);
            if (description.Length > 0 && description.Length + 1 + line.Length > MaxDescription)
            {
                break;
            }

            description.Append(description.Length > 0 ? "\n" : string.Empty).Append(line);
        }

        var embed = new JsonObject
        {
            ["description"] = description.ToString(),
            ["color"] = Color(color),
            ["footer"] = new JsonObject { ["text"] = Footer(seasons, today, lastDay) },
        };

        var top = films[0];
        var thumbnail = top.Poster
            ?? (baseUrl.Length > 0 && top.ItemId != Guid.Empty ? $"{baseUrl}/Items/{top.ItemId.ToString("N", CultureInfo.InvariantCulture)}/Images/Primary?maxHeight=300" : null);
        if (thumbnail is not null)
        {
            embed["thumbnail"] = new JsonObject { ["url"] = thumbnail };
        }

        return Message(content, [embed]);
    }

    /// <summary>"1h 36m", or "45m" under an hour.</summary>
    internal static string Runtime(TimeSpan time)
    {
        var minutes = (int)Math.Round(time.TotalMinutes);
        return minutes >= 60
            ? $"{(minutes / 60).ToString(CultureInfo.InvariantCulture)}h {(minutes % 60).ToString(CultureInfo.InvariantCulture)}m"
            : $"{minutes.ToString(CultureInfo.InvariantCulture)}m";
    }

    /// <summary>"**1.** [Title](link) · 1993 · 1h 36m · PG", then the reason in italics on the next line.</summary>
    private static string PickLine(DiscordFilm f, int number, string baseUrl)
    {
        var name = EscapeAndTruncate(f.Name, MaxName);
        var title = baseUrl.Length > 0 && f.ItemId != Guid.Empty
            ? $"[{name}]({baseUrl}/web/#/details?id={f.ItemId.ToString("N", CultureInfo.InvariantCulture)})"
            : $"**{name}**";
        var details = new List<string>();
        if (f.Year is int y)
        {
            details.Add(y.ToString(CultureInfo.InvariantCulture));
        }

        if (f.Runtime is TimeSpan t && t >= TimeSpan.FromMinutes(1))
        {
            details.Add(Runtime(t));
        }

        if (!string.IsNullOrWhiteSpace(f.Rating))
        {
            details.Add(EscapeAndTruncate(f.Rating.Trim(), 20));
        }

        // The bold number (not "1.") keeps Discord from turning the lines into its own list, which renumbers them.
        var line = $"**{number.ToString(CultureInfo.InvariantCulture)}.** {title}" + string.Concat(details.Select(d => " · " + d));
        return string.IsNullOrWhiteSpace(f.Reason) ? line : line + "\n*" + EscapeAndTruncate(f.Reason.Trim(), MaxReason) + "*";
    }

    /// <summary>"Halloween · until 31 October · new picks every morning", or only the last part outside a season.</summary>
    private static string Footer(IReadOnlyList<string> seasons, DateTime today, DateTime? lastDay)
    {
        var parts = new List<string>();
        if (seasons.Count > 0)
        {
            var names = string.Join(" & ", seasons);
            parts.Add(names.Length <= MaxName * 2 ? names : names[..(MaxName * 2)] + "…");
            if (seasons.Count == 1 && lastDay is DateTime last)
            {
                parts.Add(last.Date <= today.Date ? "today only" : "until " + last.ToString("d MMMM", CultureInfo.InvariantCulture));
            }
        }

        parts.Add("new picks every morning");
        return string.Join(" · ", parts);
    }

    public static JsonObject Test()
        => Message("✅ **Seasonal Picks** can post to this channel.", []);

    /// <summary>"#RRGGBB" to Discord's colour number; the Jellyfin blue if it isn't one.</summary>
    internal static int Color(string? hex)
    {
        var s = (hex ?? string.Empty).Trim().TrimStart('#');
        return s.Length == 6 && int.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value) ? value : DefaultColor;
    }

    /// <summary>
    /// Escapes markdown, then cuts to <paramref name="length"/> characters (plus "…"). Escaping first means the result
    /// really is within the limit, and a cut never leaves a lone backslash that would escape the "…".
    /// </summary>
    internal static string EscapeAndTruncate(string text, int length)
    {
        var escaped = Escape(text);
        if (escaped.Length <= length)
        {
            return escaped;
        }

        var cut = escaped[..length];
        var trailing = cut.Length - cut.TrimEnd('\\').Length;
        if (trailing % 2 == 1)
        {
            cut = cut[..^1];
        }

        return cut + "…";
    }

    private static string Link(DiscordFilm f, string baseUrl)
    {
        var text = "**" + EscapeAndTruncate(f.Name, MaxName) + "**" + (f.Year is int y ? $" ({y.ToString(CultureInfo.InvariantCulture)})" : string.Empty);
        return baseUrl.Length > 0 && f.ItemId != Guid.Empty
            ? $"[{text}]({baseUrl}/web/#/details?id={f.ItemId.ToString("N", CultureInfo.InvariantCulture)})"
            : text;
    }

    private static JsonObject Message(string content, List<JsonObject> embeds)
    {
        var message = new JsonObject
        {
            ["username"] = "Jellyfin Seasonal Picks",
            ["content"] = content.Length <= MaxContent ? content : content[..(MaxContent - 1)] + "…",

            // Titles and user names could contain "@everyone"; never let a message ping anyone.
            ["allowed_mentions"] = new JsonObject { ["parse"] = new JsonArray() },
        };
        if (embeds.Count > 0)
        {
            message["embeds"] = new JsonArray(embeds.Select(e => (JsonNode)e).ToArray());
        }

        return message;
    }

    private static string Plural(int count, string one, string many)
        => count.ToString(CultureInfo.InvariantCulture) + " " + (count == 1 ? one : many);

    private static string Escape(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (c is '*' or '_' or '`' or '~' or '|' or '\\' or '>' or '[' or ']')
            {
                sb.Append('\\');
            }

            sb.Append(c);
        }

        return sb.ToString();
    }
}
