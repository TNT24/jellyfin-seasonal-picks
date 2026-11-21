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
            Tags = new[] { "halloween", "trick or treating", "haunted house", "jack-o'-lantern", "halloween party", "costume party", "witchcraft", "ghost story" },
            Keywords = new[]
            {
                "halloween", "hallowe'en", "all hallows' eve", "all hallows eve", "samhain", "trick-or-treat", "trick or treat",
                "trick-or-treaters", "jack-o'-lantern", "jack-o'-lanterns", "pumpkin", "pumpkins", "pumpkin patch", "costume party",
                "haunted", "haunting", "haunted house", "haunted mansion", "ghost", "ghosts", "poltergeist", "seance", "séance",
                "witch", "witches", "witchcraft", "coven", "warlock", "spooky",
                "vampire", "vampires", "dracula", "frankenstein", "mummy", "zombie", "zombies",
                "werewolf", "werewolves", "possessed", "exorcism", "exorcist",
                "headless horseman", "sleepy hollow", "hocus pocus", "beetlejuice", "casper", "addams family",
                "michael myers", "jack skellington", "nightmare before christmas", "trick 'r treat", "coraline", "corpse bride",
                "frankenweenie", "paranorman", "monster house", "hotel transylvania", "goosebumps", "practical magic"
            },
            Boost = 3
        },
        new SeasonRule
        {
            Name = "Christmas",
            Color = "#C8102E",
            StartMonth = 11, StartDay = 20, EndMonth = 12, EndDay = 31,
            Tags = new[] { "christmas", "holiday", "christmas party", "christmas eve", "christmas tree", "christmas movie", "santa claus", "christmas carol", "christmas present", "hanukkah" },
            Keywords = new[]
            {
                "christmas", "xmas", "christmas eve", "christmas day", "santa", "santa claus", "saint nick", "st. nick",
                "kris kringle", "father christmas", "north pole", "reindeer", "rudolph", "elves' workshop", "santa's workshop",
                "mistletoe", "sleigh", "yuletide", "yule", "jingle", "nativity", "scrooge", "ebenezer", "grinch", "krampus",
                "nutcracker", "gingerbread", "tinsel", "carolers", "carol singers", "advent calendar",
                "hanukkah", "chanukah", "winter wonderland",
                "home alone", "polar express", "it's a wonderful life", "miracle on 34th street", "love actually", "bad santa",
                "jingle all the way", "a christmas story", "christmas vacation"
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
                "easter", "easter bunny", "easter egg", "easter eggs", "egg hunt", "easter sunday", "good friday",
                "palm sunday", "holy week", "the last supper", "jesus christ", "jesus of nazareth", "chocolate eggs",
                "peter rabbit", "watership down", "rise of the guardians",
                "prince of egypt", "ten commandments", "passion of the christ", "ben-hur", "king of kings",
                "jesus christ superstar", "the robe", "the greatest story ever told"
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
            Keywords = new[] { "valentine", "valentines", "valentine's", "valentine's day", "galentine's", "cupid", "cupid's", "love potion", "14th of february", "february 14th" },
            Boost = 2.5
        },
        new SeasonRule
        {
            Name = "New Year",
            Color = "#D4AF37",
            Theme = "Fireworks",
            StartMonth = 12, StartDay = 27, EndMonth = 1, EndDay = 2,
            Tags = new[] { "new year's eve", "new year", "new year's party", "countdown" },
            Keywords = new[]
            {
                "new year's eve", "new year's", "new year", "new years eve", "new year's day", "new year's resolution", "hogmanay",
                "auld lang syne", "stroke of midnight", "countdown to midnight", "midnight kiss", "ball drop", "200 cigarettes"
            },
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
            Keywords = new[]
            {
                "pancake", "pancakes", "pancake day", "shrove tuesday", "mardi gras", "crepe", "crepes", "crêpe", "crêpes",
                "waffle", "waffles", "chef", "chefs", "cook", "cooking", "baker", "bakery", "baking", "patisserie", "culinary",
                "restaurant", "cuisine", "ratatouille", "chocolat", "julie & julia", "the menu", "the hundred-foot journey",
                "babette's feast", "big night", "no reservations", "burnt", "boiling point", "tampopo", "eat drink man woman",
                "like water for chocolate", "the lunchbox", "sweet bean", "the taste of things", "jiro dreams of sushi",
                "cloudy with a chance of meatballs"
            },
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
                "the farewell", "crazy rich asians", "everything everywhere all at once", "turning red", "raya",
                "lantern festival", "dragon dance", "lion dance", "red envelope", "shaolin", "wuxia", "jackie chan", "bruce lee",
                "ip man", "enter the dragon", "drunken master", "kung fu hustle", "big trouble in little china", "shang-chi",
                "the joy luck club", "raise the red lantern", "farewell my concubine", "a touch of zen", "hidden dragon"
            },
            Boost = 4,
            HideOutOfSeason = false
        },

        // Film days: a film's own day. They push its films to the front on the day but never hide them the rest of the year.
        FilmDay("Star Wars Day", "#FFE81F", 5, 4, 5, 5, "Lightsabers",
            new[] { "star wars" },
            new[]
            {
                "star wars", "jedi", "sith", "skywalker", "darth vader", "rogue one", "the force awakens", "the empire strikes back",
                "a new hope", "the phantom menace", "attack of the clones", "clone wars", "the last jedi", "obi-wan", "han solo",
                "chewbacca", "yoda", "mandalorian", "lightsaber", "lightsabers", "death star", "galactic empire", "rebel alliance",
                "ewok", "ewoks", "tatooine"
            }),
        FilmDay("Ghostbusters Day", "#4CAF50", 6, 8, 6, 8, "Ghosts",
            new[] { "ghostbusters" },
            new[] { "ghostbusters", "ghostbuster", "stay puft", "slimer", "proton pack", "ecto-1", "gozer", "zuul", "venkman", "marshmallow man" }),
        FilmDay("Back to Hogwarts", "#740001", 9, 1, 9, 1, "Castle",
            new[] { "harry potter", "wizarding world" },
            new[]
            {
                "harry potter", "hogwarts", "fantastic beasts", "wizarding world", "dumbledore", "voldemort", "hermione",
                "hogsmeade", "quidditch", "diagon alley", "muggle", "muggles", "grindelwald", "newt scamander", "azkaban",
                "philosopher's stone", "sorcerer's stone", "chamber of secrets", "goblet of fire", "order of the phoenix",
                "half-blood prince", "deathly hallows", "gryffindor", "slytherin"
            }),

        // UK dates. Like film days they don't hide their films the rest of the year.
        FilmDay("St Patrick's Day", "#169B62", 3, 17, 3, 17, "Shamrock",
            new[] { "ireland", "irish", "dublin, ireland", "st. patrick's day" },
            new[]
            {
                "ireland", "irish", "dublin", "belfast", "galway", "leprechaun", "st. patrick's day", "saint patrick",
                "the commitments", "banshees of inisherin", "the quiet man", "the guard", "in bruges", "the wind that shakes the barley",
                "connemara", "aran islands", "st. patrick", "st patrick's", "shamrock", "the secret of kells", "song of the sea",
                "wolfwalkers", "the quiet girl", "sing street", "my left foot", "the field", "philomena", "michael collins",
                "the magdalene sisters", "angela's ashes", "waking ned", "the snapper", "leap year", "far and away", "the boondock saints"
            }),
        FilmDay("Bonfire Night", "#FF6D00", 11, 1, 11, 5, "Bonfire",
            new[] { "guy fawkes", "bonfire", "fireworks", "gunpowder plot" },
            new[]
            {
                "guy fawkes", "guy fawkes night", "bonfire night", "bonfire", "gunpowder plot", "gunpowder", "v for vendetta",
                "remember, remember", "fifth of november", "5th of november", "fireworks", "firework", "sparklers", "catherine wheel"
            })
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
