using System;
using System.Collections.Generic;
using Jellyfin.Plugin.SeasonalPicks.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.SeasonalPicks;

/// <summary>
/// Seasonal Picks: history- and calendar-aware movie picks, delivered as per-user playlists,
/// an optional mirrored library folder, and a web "For You" screen.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public static readonly Guid PluginGuid = new("6e3ba9d2-8036-4708-9ce7-35413b443d8f");

    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        if (Configuration.MigrateToLatest())
        {
            SaveConfiguration();
        }
    }

    public static Plugin? Instance { get; private set; }

    public override string Name => "Seasonal Picks";

    public override Guid Id => PluginGuid;

    public override string Description =>
        "Picks movies from your watch history and the time of year, as playlists (works on TV), a library folder, and a web Suggested screen.";

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "SeasonalPicks",
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html"
            }
        };
    }
}
