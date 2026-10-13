using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>
/// The few server calls that differ between Jellyfin 12 and 10.11, so the rest of the plugin is written once.
/// The 10.11 build is compiled with JELLYFIN_10_11 defined.
/// </summary>
internal static class ServerCompat
{
    /// <summary>Every user on the server.</summary>
    public static IEnumerable<User> AllUsers(this IUserManager users)
#if JELLYFIN_10_11
        => users.Users;
#else
        => users.GetUsers();
#endif

    /// <summary>The user's play state for each item, by item id. Items with no saved state are left out.</summary>
    public static IReadOnlyDictionary<Guid, UserItemData> UserDataFor(this IUserDataManager userData, IReadOnlyList<BaseItem> items, User user)
    {
#if JELLYFIN_10_11
        // 10.11 has no batch lookup: one call per item.
        var result = new Dictionary<Guid, UserItemData>();
        foreach (var item in items)
        {
            if (!result.ContainsKey(item.Id) && userData.GetUserData(user, item) is { } data)
            {
                result[item.Id] = data;
            }
        }

        return result;
#else
        return userData.GetUserDataBatch(items.ToList(), user);
#endif
    }
}
