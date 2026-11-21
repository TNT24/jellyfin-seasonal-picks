using System;
using System.Globalization;
using System.Linq;
using System.Xml.Serialization;

namespace Jellyfin.Plugin.SeasonalPicks.Configuration;

/// <summary>
/// A calendar window (e.g. Oct 1 - Oct 31) and how to recognise movies that belong to it.
/// Keywords/tags are a strong match; genres are a weaker one.
/// </summary>
public class SeasonRule
{
    public string Name { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    /// <summary>Cover tint, hex (e.g. "#FF7518").</summary>
    public string Color { get; set; } = "#3A6EA5";

    /// <summary>Cover decoration: auto (from the name), or a <c>CoverTheme</c> name such as Snow, Halloween or None.</summary>
    public string Theme { get; set; } = "auto";

    /// <summary>Layout of the generated cover (title, background, darkness ...).</summary>
    public CoverStyle Cover { get; set; } = new();

    public int StartMonth { get; set; } = 1;

    public int StartDay { get; set; } = 1;

    public int EndMonth { get; set; } = 12;

    public int EndDay { get; set; } = 31;

    public string[] Genres { get; set; } = Array.Empty<string>();

    public string[] Tags { get; set; } = Array.Empty<string>();

    /// <summary>Whole-word matches in the title or overview.</summary>
    public string[] Keywords { get; set; } = Array.Empty<string>();

    /// <summary>Weight multiplier for matching movies while the season is active.</summary>
    public double Boost { get; set; } = 3;

    /// <summary>Keep keyword/tag matches out of picks when the season is NOT active (no Christmas films in July).</summary>
    public bool HideOutOfSeason { get; set; } = true;

    /// <summary>
    /// For seasons tied to Easter (its date moves every year): days relative to Easter Sunday, e.g. -14 and 1.
    /// When both are set they replace the fixed month/day window.
    /// </summary>
    public int? StartEasterOffset { get; set; }

    public int? EndEasterOffset { get; set; }

    [XmlIgnore]
    public bool IsEasterBased => StartEasterOffset.HasValue && EndEasterOffset.HasValue;

    /// <summary>
    /// For seasons tied to Chinese (Lunar) New Year, which also moves every year: days relative to its first day,
    /// e.g. 0 and 14 for the fortnight up to the Lantern Festival. When both are set they replace the fixed window.
    /// </summary>
    public int? StartLunarOffset { get; set; }

    public int? EndLunarOffset { get; set; }

    [XmlIgnore]
    public bool IsLunarBased => StartLunarOffset.HasValue && EndLunarOffset.HasValue && !IsEasterBased;

    /// <summary>This year's window when it moves with Easter or Lunar New Year; null for a fixed one.</summary>
    private (DateTime Start, DateTime End)? MovingWindow(int year)
    {
        if (IsEasterBased)
        {
            var easter = EasterSunday(year);
            return (easter.AddDays(StartEasterOffset!.Value), easter.AddDays(EndEasterOffset!.Value));
        }

        if (IsLunarBased)
        {
            var lunar = LunarNewYear(year);
            return (lunar.AddDays(StartLunarOffset!.Value), lunar.AddDays(EndLunarOffset!.Value));
        }

        return null;
    }

    public bool IsActive(DateTime date)
    {
        if (MovingWindow(date.Year) is { } window)
        {
            var day = date.Date;
            return day >= window.Start && day <= window.End;
        }

        var today = (date.Month * 100) + date.Day;
        var start = (StartMonth * 100) + StartDay;
        var end = (EndMonth * 100) + EndDay;
        return start <= end
            ? today >= start && today <= end
            : today >= start || today <= end; // wraps the new year, e.g. Dec 20 - Jan 5
    }

    /// <summary>Whole weeks since this year's window opened (0 in its first week, and whenever it isn't active).</summary>
    public int WeekOfSeason(DateTime date)
    {
        if (!IsActive(date))
        {
            return 0;
        }

        var day = date.Date;
        DateTime start;
        if (MovingWindow(day.Year) is { } window)
        {
            start = window.Start;
        }
        else
        {
            // A window that wraps the new year and is in its January part opened last year.
            var year = (day.Month * 100) + day.Day >= (StartMonth * 100) + StartDay ? day.Year : day.Year - 1;
            var month = Math.Clamp(StartMonth, 1, 12);
            start = new DateTime(year, month, Math.Clamp(StartDay, 1, DateTime.DaysInMonth(year, month)));
        }

        return Math.Max(0, (day - start).Days / 7);
    }

    /// <summary>Western (Gregorian) Easter Sunday: the anonymous Gregorian algorithm.</summary>
    public static DateTime EasterSunday(int year)
    {
        int a = year % 19, b = year / 100, c = year % 100, d = b / 4, e = b % 4;
        int f = (b + 8) / 25, g = (b - f + 1) / 3, h = ((19 * a) + b - d - g + 15) % 30;
        int i = c / 4, k = c % 4, l = (32 + (2 * e) + (2 * i) - h - k) % 7;
        int m = (a + (11 * h) + (22 * l)) / 451;
        int month = (h + l - (7 * m) + 114) / 31, day = ((h + l - (7 * m) + 114) % 31) + 1;
        return new DateTime(year, month, day);
    }

    private static readonly ChineseLunisolarCalendar Lunar = new();

    /// <summary>The first day of Chinese (Lunar) New Year in a Gregorian year, from .NET's Chinese lunisolar calendar.</summary>
    public static DateTime LunarNewYear(int year)
    {
        var y = Math.Clamp(year, Lunar.MinSupportedDateTime.Year + 1, Lunar.MaxSupportedDateTime.Year - 1);
        return Lunar.ToDateTime(y, 1, 1, 0, 0, 0, 0);
    }

    /// <summary>File-name-safe key for this season's uploaded cover.</summary>
    [XmlIgnore]
    public string CoverKey => Slug(Name);

    public static string Slug(string name)
    {
        var chars = name.ToLowerInvariant().Where(char.IsAsciiLetterOrDigit).ToArray();
        return chars.Length > 0 ? new string(chars) : "season";
    }

    public static SeasonRule[] Defaults() => new[]
    {
        new SeasonRule
        {
            Name = "Halloween",
            Color = "#FF7518",
            StartMonth = 10, StartDay = 1, EndMonth = 10, EndDay = 31,
            Genres = new[] { "Horror" },
            // Tags: TMDb keywords, which Jellyfin stores as tags.
            Tags = new[] { "halloween", "trick or treating", "haunted house", "jack-o'-lantern" },
            Keywords = new[]
            {
                "halloween", "trick-or-treat", "trick or treat", "jack-o'-lantern", "pumpkin",
                "haunted", "haunting", "ghost", "ghosts", "poltergeist", "witch", "witches",
                "vampire", "vampires", "dracula", "frankenstein", "mummy", "zombie", "zombies",
                "werewolf", "werewolves", "possessed", "exorcism", "exorcist",
                "headless horseman", "sleepy hollow", "hocus pocus", "beetlejuice", "casper", "addams family"
            },
            Boost = 3
        },
        new SeasonRule
        {
            Name = "Christmas",
            Color = "#C8102E",
            StartMonth = 11, StartDay = 20, EndMonth = 12, EndDay = 31,
            Tags = new[] { "christmas", "holiday", "christmas party", "christmas eve", "christmas tree", "christmas movie", "santa claus" },
            Keywords = new[]
            {
                "christmas", "xmas", "christmas eve", "santa", "santa claus", "north pole", "reindeer", "rudolph",
                "mistletoe", "sleigh", "yuletide", "jingle", "nativity", "scrooge", "grinch", "krampus",
                "nutcracker", "home alone", "polar express"
            },
            Boost = 4
        },
        new SeasonRule
        {
            Name = "Easter",
            Color = "#8CC152",
            StartEasterOffset = -14, EndEasterOffset = 1, // two weeks before Easter Sunday to Easter Monday
            StartMonth = 3, StartDay = 22, EndMonth = 4, EndDay = 26, // shown only if the Easter offsets are removed
            Tags = new[] { "easter", "easter bunny", "easter egg", "easter egg hunt" },
            Keywords = new[]
            {
                "easter", "easter bunny", "easter egg", "egg hunt", "peter rabbit",
                "prince of egypt", "ten commandments", "passion of the christ"
            },
            Boost = 2.5
        },
        new SeasonRule
        {
            Name = "Valentine's",
            Color = "#E0457B",
            StartMonth = 2, StartDay = 1, EndMonth = 2, EndDay = 14,
            Genres = new[] { "Romance" },
            Tags = new[] { "valentine's day" },
            Keywords = new[] { "valentine", "valentine's", "valentine's day", "cupid" },
            Boost = 2.5
        },
        new SeasonRule
        {
            Name = "New Year",
            Color = "#D4AF37",
            Theme = "Fireworks",
            StartMonth = 12, StartDay = 27, EndMonth = 1, EndDay = 2,
            Tags = new[] { "new year's eve", "new year", "new year's party", "countdown" },
            Keywords = new[] { "new year's eve", "new year's", "new year", "new years eve", "stroke of midnight", "countdown to midnight" },
            Boost = 3.5
        },

        // Days whose date moves each year. Like film days they push their films forward without hiding them.
        new SeasonRule
        {
            Name = "Pancake Day",
            Color = "#E0A030",
            Theme = "Pancakes",
            StartEasterOffset = -47, EndEasterOffset = -47, // Shrove Tuesday, 47 days before Easter Sunday
            StartMonth = 2, StartDay = 17, EndMonth = 2, EndDay = 17,
            Tags = new[] { "pancakes", "cooking", "chef", "restaurant" },
            Keywords = new[] { "pancake", "pancakes", "crepe", "crepes", "crêpe", "waffle", "waffles", "chef", "ratatouille", "chocolat", "julie & julia", "the menu", "the hundred-foot journey" },
            Boost = 3,
            HideOutOfSeason = false
        },
        new SeasonRule
        {
            Name = "Chinese New Year",
            Color = "#C62828",
            Theme = "LunarNewYear",
            StartLunarOffset = 0, EndLunarOffset = 14, // New Year's Day to the Lantern Festival
            StartMonth = 2, StartDay = 1, EndMonth = 2, EndDay = 15,
            Tags = new[] { "chinese new year", "lunar new year", "martial arts", "kung fu", "china", "hong kong" },
            Keywords = new[]
            {
                "chinese new year", "lunar new year", "spring festival", "kung fu", "hong kong", "shanghai", "beijing",
                "mulan", "kung fu panda", "crouching tiger", "house of flying daggers", "in the mood for love",
                "the farewell", "crazy rich asians", "everything everywhere all at once", "turning red", "raya"
            },
            Boost = 4,
            HideOutOfSeason = false
        },

        // Film days: a film's own day. They push its films to the front on the day but never hide them the rest of the year.
        FilmDay("Star Wars Day", "#FFE81F", 5, 4, 5, 5, "Lightsabers",
            new[] { "star wars" },
            new[] { "star wars", "jedi", "sith", "skywalker", "darth vader", "rogue one", "the force awakens", "the empire strikes back" }),
        FilmDay("Ghostbusters Day", "#4CAF50", 6, 8, 6, 8, "Ghosts",
            new[] { "ghostbusters" },
            new[] { "ghostbusters", "ghostbuster", "stay puft", "slimer" }),
        FilmDay("Back to Hogwarts", "#740001", 9, 1, 9, 1, "Castle",
            new[] { "harry potter", "wizarding world" },
            new[] { "harry potter", "hogwarts", "fantastic beasts", "wizarding world", "dumbledore", "voldemort" }),

        // UK dates. Like film days they don't hide their films the rest of the year.
        FilmDay("St Patrick's Day", "#169B62", 3, 17, 3, 17, "Shamrock",
            new[] { "ireland", "irish", "dublin, ireland", "st. patrick's day" },
            new[]
            {
                "ireland", "irish", "dublin", "belfast", "galway", "leprechaun", "st. patrick's day", "saint patrick",
                "the commitments", "banshees of inisherin", "the quiet man", "the guard", "in bruges", "the wind that shakes the barley"
            }),
        FilmDay("Bonfire Night", "#FF6D00", 11, 1, 11, 5, "Bonfire",
            new[] { "guy fawkes", "bonfire", "fireworks", "gunpowder plot" },
            new[] { "guy fawkes", "bonfire night", "gunpowder plot", "v for vendetta", "remember, remember", "fireworks" })
    };

    private static SeasonRule FilmDay(string name, string color, int sm, int sd, int em, int ed, string theme, string[] tags, string[] keywords) => new()
    {
        Name = name,
        Color = color,
        Theme = theme,
        StartMonth = sm, StartDay = sd, EndMonth = em, EndDay = ed,
        Tags = tags,
        Keywords = keywords,
        Boost = 5,
        HideOutOfSeason = false
    };
}
