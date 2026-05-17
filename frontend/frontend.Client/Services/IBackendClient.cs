using System.Net.Http;
using System.Net.Http.Json;

namespace frontend.Client.Services;

public interface IBackendClient
{
    Task<T?> GetFromJsonAsync<T>(string requestUri);
    Task<HttpResponseMessage> PostAsJsonAsync<T>(string requestUri, T value);
}

public class BackendClient : IBackendClient
{
    private readonly HttpClient _httpClient;

    public BackendClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<T?> GetFromJsonAsync<T>(string requestUri)
    {
        return await _httpClient.GetFromJsonAsync<T>(requestUri);
    }

    public async Task<HttpResponseMessage> PostAsJsonAsync<T>(string requestUri, T value)
    {
        return await _httpClient.PostAsJsonAsync(requestUri, value);
    }
}
