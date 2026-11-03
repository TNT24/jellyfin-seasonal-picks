using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>One film in a Discord message. <paramref name="Poster"/>: its uploaded poster (<c>attachment://…</c>), if any.</summary>
public sealed record DiscordFilm(Guid ItemId, string Name, int? Year, string Reason, string? Poster = null);

/// <summary>Builds Discord webhook payloads. No network access, so it can be unit tested.</summary>
public static class DiscordMessages
{
    // Discord's limits: 2000 characters of content, 10 embeds, 256 per embed title, 4096 per description,
    // and 6000 across all embeds' text in one message.
    internal const int MaxContent = 2000;
    internal const int MaxEmbedText = 6000;
    private const int MaxEmbeds = 10;
    private const int MaxTitle = 256;
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
    /// One user's top picks for the day: one card per film, with its uploaded poster, or else the poster from the server
    /// address when that's set. The server address also links each title to Jellyfin.
    /// </summary>
    public static JsonObject DailyPicks(string userName, IReadOnlyList<string> seasons, IReadOnlyList<DiscordFilm> films, string color, string publicServerUrl)
    {
        var baseUrl = publicServerUrl.Trim().TrimEnd('/');
        var content = $"🍿 **Today's picks for {EscapeAndTruncate(userName, MaxName)}**" + (seasons.Count > 0 ? $" · {EscapeAndTruncate(string.Join(" & ", seasons), MaxName * 2)}" : string.Empty);
        var embeds = films.Take(MaxEmbeds).Select(f =>
        {
            var embed = new JsonObject
            {
                ["title"] = EscapeAndTruncate(f.Year is int y ? $"{f.Name} ({y.ToString(CultureInfo.InvariantCulture)})" : f.Name, MaxTitle - 1),
                ["description"] = EscapeAndTruncate(f.Reason, MaxReason),
                ["color"] = Color(color),
            };
            if (baseUrl.Length > 0 && f.ItemId != Guid.Empty)
            {
                var id = f.ItemId.ToString("N", CultureInfo.InvariantCulture);
                embed["url"] = $"{baseUrl}/web/#/details?id={id}";
                embed["thumbnail"] = new JsonObject { ["url"] = $"{baseUrl}/Items/{id}/Images/Primary?maxHeight=300" };
            }

            if (f.Poster is not null)
            {
                embed["thumbnail"] = new JsonObject { ["url"] = f.Poster };
            }

            return embed;
        }).ToList();

        // Long titles and reasons on ten cards can pass Discord's 6000-character total, and then it refuses the whole
        // message (every day). Drop cards from the end until it fits.
        while (embeds.Count > 1 && embeds.Sum(EmbedTextLength) > MaxEmbedText)
        {
            embeds.RemoveAt(embeds.Count - 1);
        }

        return Message(content, embeds);
    }

    public static JsonObject Test()
        => Message("✅ **Seasonal Picks** can post to this channel.", []);

    /// <summary>"#RRGGBB" to Discord's colour number; the Jellyfin blue if it isn't one.</summary>
    internal static int Color(string? hex)
    {
        var s = (hex ?? string.Empty).Trim().TrimStart('#');
        return s.Length == 6 && int.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value) ? value : DefaultColor;
    }

    /// <summary>The characters Discord counts towards an embed's share of the 6000 limit (here: title and description).</summary>
    internal static int EmbedTextLength(JsonObject embed)
        => (embed["title"]?.GetValue<string>().Length ?? 0) + (embed["description"]?.GetValue<string>().Length ?? 0);

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
