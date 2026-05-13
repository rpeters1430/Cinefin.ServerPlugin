using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cinefin.ServerPlugin.Services
{
    public abstract class BaseApiService
    {
        protected readonly HttpClient HttpClient;
        protected readonly ILogger Logger;

        protected BaseApiService(IHttpClientFactory httpClientFactory, ILogger logger)
        {
            HttpClient = httpClientFactory.CreateClient();
            Logger = logger;
        }

        private void AddHeaders(HttpRequestMessage request, string apiKey)
        {
            request.Headers.Add("X-Api-Key", apiKey);
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            
            var config = Plugin.Instance.Configuration;
            if (!string.IsNullOrEmpty(config.ProxyUsername) && !string.IsNullOrEmpty(config.ProxyPassword))
            {
                var authBytes = Encoding.UTF8.GetBytes($"{config.ProxyUsername}:{config.ProxyPassword}");
                var authHeader = Convert.ToBase64String(authBytes);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);
            }
        }

        protected async Task<T?> GetAsync<T>(string url, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            AddHeaders(request, apiKey);
            
            var response = await HttpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Logger.LogError("API request failed. Status: {StatusCode}, URL: {Url}, Response: {Content}", 
                    response.StatusCode, url, errorContent);
                throw new HttpRequestException($"API request failed with status code {response.StatusCode}. Details: {errorContent}");
            }

            return await response.Content.ReadFromJsonAsync<T>();
        }

        protected async Task PostAsync<T>(string url, string apiKey, T data)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            AddHeaders(request, apiKey);
            request.Content = JsonContent.Create(data);
            
            var response = await HttpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Logger.LogError("API request failed. Status: {StatusCode}, URL: {Url}, Response: {Content}", 
                    response.StatusCode, url, errorContent);
                throw new HttpRequestException($"API request failed with status code {response.StatusCode}. Details: {errorContent}");
            }
        }
    }
}
