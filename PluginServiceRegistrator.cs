using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Cinefin.ServerPlugin.Services;

namespace Cinefin.ServerPlugin
{
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddSingleton<SonarrService>();
            serviceCollection.AddSingleton<RadarrService>();
            serviceCollection.AddSingleton<OverseerrService>();
        }
    }
}
