using Jellyfin.Plugin.SeasonalPicks.Services;
using Jellyfin.Plugin.SeasonalPicks.Web;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.SeasonalPicks;

/// <summary>Nothing here runs unless registered (see PLUGIN_NOTES.md section 3).</summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<PickStore>();
        serviceCollection.AddSingleton<PickEngine>();
        serviceCollection.AddSingleton<MirrorWriter>();
        serviceCollection.AddSingleton<CoverService>();
        serviceCollection.AddSingleton<PicksService>();
        serviceCollection.AddHostedService<WebInjectionService>();
        serviceCollection.AddHostedService<WatchedListener>();
    }
}
