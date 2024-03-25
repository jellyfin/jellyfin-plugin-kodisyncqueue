using Jellyfin.Plugin.KodiSyncQueue.EntryPoints;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.KodiSyncQueue;

public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddHostedService<LibrarySyncNotification>();
        serviceCollection.AddHostedService<UserSyncNotification>();
    }
}
