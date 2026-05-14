using System;
using System.Net.Http;
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
            // Named client that tolerates self-signed / internal CA certificates common in reverse-proxy
            // homelab setups. HttpClientFactory manages lifetime and connection pooling.
            serviceCollection.AddHttpClient(BaseApiService.HttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, cert, chain, errors) =>
                    {
                        // Always accept if the plugin config opts in to ignoring SSL errors,
                        // otherwise fall back to default validation.
                        return Plugin.Instance?.Configuration.IgnoreSslErrors == true
                            || errors == System.Net.Security.SslPolicyErrors.None;
                    },
                    AllowAutoRedirect = true,
                })
                .SetHandlerLifetime(TimeSpan.FromMinutes(5));

            serviceCollection.AddSingleton<SonarrService>();
            serviceCollection.AddSingleton<RadarrService>();
            serviceCollection.AddSingleton<OverseerrService>();
        }
    }
}
