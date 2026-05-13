using System;
using System.Net.Http;
using System.Net.Http.Json;
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

        protected async Task<T?> GetAsync<T>(string url, string apiKey)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("X-Api-Key", apiKey);
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                
                var response = await HttpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    Logger.LogError("API request failed with status code {StatusCode} for URL {Url}", response.StatusCode, url);
                    return default;
                }

                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error occurred during GET request to {Url}", url);
                throw;
            }
        }

        protected async Task PostAsync<T>(string url, string apiKey, T data)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("X-Api-Key", apiKey);
                request.Content = JsonContent.Create(data);
                
                var response = await HttpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    Logger.LogError("API request failed with status code {StatusCode} for URL {Url}", response.StatusCode, url);
                }
                
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error occurred during POST request to {Url}", url);
                throw;
            }
        }
    }
}
