using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>One film in a Discord message.</summary>
public sealed record DiscordFilm(Guid ItemId, string Name, int? Year, string Reason);

/// <summary>Builds Discord webhook payloads. No network access, so it can be unit tested.</summary>
public static class DiscordMessages
{
    // Discord: up to 10 embeds and 6000 characters per message.
    private const int MaxEmbeds = 10;
    private const int MaxReason = 300;
    private const int ListSeasonFilmsUpTo = 10;
    private const int DefaultColor = 0x00A4DC;

    /// <summary>A season (or special day) has started. <paramref name="lastDay"/> equal to <paramref name="today"/> = a one-day event.</summary>
    public static JsonObject SeasonStarted(string season, string color, DateTime today, DateTime lastDay, string? playlistName, int playlistUsers, IReadOnlyList<DiscordFilm> films, string publicServerUrl)
    {
        var baseUrl = publicServerUrl.Trim().TrimEnd('/');
        var oneDay = lastDay.Date <= today.Date;
        var content = oneDay
            ? $"🎉 **It's {Escape(season)}!**"
            : $"🎉 **{Escape(season)} is here!** Until {lastDay.ToString("d MMMM", CultureInfo.InvariantCulture)}.";
        if (playlistName is not null && playlistUsers > 0)
        {
            content += $" {Plural(playlistUsers, "user has", "users have")} a new **{Escape(playlistName)}** playlist.";
        }

        var embeds = new List<JsonObject>();
        if (films.Count > 0)
        {
            var lines = films.Take(ListSeasonFilmsUpTo).Select(f => "• " + Link(f, baseUrl)).ToList();
            embeds.Add(new JsonObject
            {
                ["title"] = oneDay ? "Something to watch today" : "In the mood? From your library",
                ["description"] = string.Join('\n', lines),
                ["color"] = Color(color),
            });
        }

        return Message(content, embeds);
    }

    /// <summary>A season has ended and its playlists were removed.</summary>
    public static JsonObject SeasonEnded(string season, bool hadPlaylists)
        => Message(
            $"👋 **{Escape(season)} is over.**" + (hadPlaylists ? " Its playlists have been removed until next time." : " See you next time."),
            []);

    /// <summary>One user's top picks for the day: one card per film, with its poster when the server address is set.</summary>
    public static JsonObject DailyPicks(string userName, IReadOnlyList<string> seasons, IReadOnlyList<DiscordFilm> films, string color, string publicServerUrl)
    {
        var baseUrl = publicServerUrl.Trim().TrimEnd('/');
        var content = $"🍿 **Today's picks for {Escape(userName)}**" + (seasons.Count > 0 ? $" · {Escape(string.Join(" & ", seasons))}" : string.Empty);
        var embeds = films.Take(MaxEmbeds).Select(f =>
        {
            var embed = new JsonObject
            {
                ["title"] = Truncate(f.Year is int y ? $"{f.Name} ({y.ToString(CultureInfo.InvariantCulture)})" : f.Name, 250),
                ["description"] = Escape(Truncate(f.Reason, MaxReason)),
                ["color"] = Color(color),
            };
            if (baseUrl.Length > 0 && f.ItemId != Guid.Empty)
            {
                var id = f.ItemId.ToString("N", CultureInfo.InvariantCulture);
                embed["url"] = $"{baseUrl}/web/#/details?id={id}";
                embed["thumbnail"] = new JsonObject { ["url"] = $"{baseUrl}/Items/{id}/Images/Primary?maxHeight=300" };
            }

            return embed;
        }).ToList();
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

    private static string Link(DiscordFilm f, string baseUrl)
    {
        var text = "**" + Escape(f.Name) + "**" + (f.Year is int y ? $" ({y.ToString(CultureInfo.InvariantCulture)})" : string.Empty);
        return baseUrl.Length > 0 && f.ItemId != Guid.Empty
            ? $"[{text}]({baseUrl}/web/#/details?id={f.ItemId.ToString("N", CultureInfo.InvariantCulture)})"
            : text;
    }

    private static JsonObject Message(string content, List<JsonObject> embeds)
    {
        var message = new JsonObject
        {
            ["username"] = "Jellyfin Seasonal Picks",
            ["content"] = content,

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

    private static string Truncate(string text, int length) => text.Length <= length ? text : text[..length] + "…";

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
