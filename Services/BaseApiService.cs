using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Cinefin.ServerPlugin.Services
{
    public abstract class BaseApiService
    {
        protected readonly HttpClient HttpClient;

        protected BaseApiService(IHttpClientFactory httpClientFactory)
        {
            HttpClient = httpClientFactory.CreateClient();
        }

        protected async Task<T?> GetAsync<T>(string url, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Api-Key", apiKey);
            var response = await HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }

        protected async Task PostAsync<T>(string url, string apiKey, T data)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("X-Api-Key", apiKey);
            request.Content = JsonContent.Create(data);
            var response = await HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
    }
}
