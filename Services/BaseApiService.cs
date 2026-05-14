using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cinefin.ServerPlugin.Services
{
    public abstract class BaseApiService
    {
        public const string HttpClientName = "cinefin";

        protected readonly HttpClient HttpClient;
        protected readonly ILogger Logger;

        protected BaseApiService(IHttpClientFactory httpClientFactory, ILogger logger)
        {
            HttpClient = httpClientFactory.CreateClient(HttpClientName);
            HttpClient.Timeout = TimeSpan.FromSeconds(30);
            Logger = logger;
        }

        // proxyUser/proxyPass override the saved config values — used by test-connection calls
        // so we never need to mutate global plugin state.
        private void AddHeaders(HttpRequestMessage request, string apiKey, string? proxyUser = null, string? proxyPass = null)
        {
            request.Headers.Add("X-Api-Key", apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var config = Plugin.Instance?.Configuration;
            var username = proxyUser ?? config?.ProxyUsername;
            var password = proxyPass ?? config?.ProxyPassword;

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var authBytes = Encoding.UTF8.GetBytes($"{username}:{password}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
            }
        }

        protected async Task<T?> GetAsync<T>(string url, string apiKey, string? proxyUser = null, string? proxyPass = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            AddHeaders(request, apiKey, proxyUser, proxyPass);

            var response = await HttpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Logger.LogError("GET failed. Status: {StatusCode}, URL: {Url}, Body: {Content}",
                    response.StatusCode, url, errorContent);
                throw new HttpRequestException($"Request to {url} failed ({response.StatusCode}): {errorContent}");
            }

            return await response.Content.ReadFromJsonAsync<T>();
        }

        protected async Task PostAsync<T>(string url, string apiKey, T data, string? proxyUser = null, string? proxyPass = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            AddHeaders(request, apiKey, proxyUser, proxyPass);
            request.Content = JsonContent.Create(data);

            var response = await HttpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Logger.LogError("POST failed. Status: {StatusCode}, URL: {Url}, Body: {Content}",
                    response.StatusCode, url, errorContent);
                throw new HttpRequestException($"Request to {url} failed ({response.StatusCode}): {errorContent}");
            }
        }
    }
}
