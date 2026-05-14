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
            // Standard *arr header
            request.Headers.Add("X-Api-Key", apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            // Standard browser-like user agent to avoid proxy blocks
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36");

            var config = Plugin.Instance?.Configuration;
            var username = proxyUser ?? config?.ProxyUsername;
            var password = proxyPass ?? config?.ProxyPassword;

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var authBytes = Encoding.UTF8.GetBytes($"{username}:{password}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
            }
        }

        protected string NormalizeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;
            var normalized = url.Trim().TrimEnd('/');
            if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                normalized = "https://" + normalized;
            }
            return normalized;
        }

        private string AppendApiKey(string url, string apiKey)
        {
            var separator = url.Contains('?') ? "&" : "?";
            return $"{url}{separator}apikey={apiKey}";
        }

        protected async Task<T?> GetAsync<T>(string url, string apiKey, string? proxyUser = null, string? proxyPass = null)
        {
            var finalUrl = AppendApiKey(url, apiKey);
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, finalUrl);
                AddHeaders(request, apiKey, proxyUser, proxyPass);

                var response = await HttpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Logger.LogError("GET failed. Status: {StatusCode}, URL: {Url}, Body: {Content}",
                        response.StatusCode, url, errorContent);
                    throw new HttpRequestException($"Request failed with status {response.StatusCode}. Details: {errorContent}");
                }

                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (Exception ex) when (ex is not HttpRequestException)
            {
                Logger.LogError(ex, "GET failed due to exception. URL: {Url}", url);
                var message = ex.Message;
                if (ex.InnerException != null) message += " -> " + ex.InnerException.Message;
                throw new HttpRequestException($"Connection failed: {message}", ex);
            }
        }

        protected async Task PostAsync<T>(string url, string apiKey, T data, string? proxyUser = null, string? proxyPass = null)
        {
            var finalUrl = AppendApiKey(url, apiKey);
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, finalUrl);
                AddHeaders(request, apiKey, proxyUser, proxyPass);
                request.Content = JsonContent.Create(data);

                var response = await HttpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Logger.LogError("POST failed. Status: {StatusCode}, URL: {Url}, Body: {Content}",
                        response.StatusCode, url, errorContent);
                    throw new HttpRequestException($"Request failed with status {response.StatusCode}. Details: {errorContent}");
                }
            }
            catch (Exception ex) when (ex is not HttpRequestException)
            {
                Logger.LogError(ex, "POST failed due to exception. URL: {Url}", url);
                var message = ex.Message;
                if (ex.InnerException != null) message += " -> " + ex.InnerException.Message;
                throw new HttpRequestException($"Connection failed: {message}", ex);
            }
        }
    }
}
