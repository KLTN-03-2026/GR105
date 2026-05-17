using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using frontend.Client.Services.Storage;
using frontend.Client.Core.State;

namespace frontend.Client.Services.Http
{
    public interface IApiClient
    {
        Uri? BaseAddress { get; }
        Task<T?> GetAsync<T>(string url);
        Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data);
        Task<TResponse?> PostAsync<TResponse>(string url, object? data = null);
        Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest data);
        Task<TResponse?> DeleteAsync<TResponse>(string url);
        Task<TResponse?> PostMultipartAsync<TResponse>(string url, MultipartFormDataContent content);
    }

    public sealed class ApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ICookieService _cookieService;
        private readonly AuthState _authState;
        private readonly JsonSerializerOptions _jsonOptions;

        public Uri? BaseAddress => _httpClient.BaseAddress;

        public ApiClient(HttpClient httpClient, ICookieService cookieService, AuthState authState)
        {
            _httpClient = httpClient;
            _cookieService = cookieService;
            _authState = authState;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        private async Task AddAuthHeader(HttpRequestMessage request)
        {
            try
            {
                // 1. Try to get token from memory
                string? token = _authState.Token;

                // 2. Fallback to Cookie
                if (string.IsNullOrEmpty(token))
                {
                    token = await _cookieService.GetCookieAsync("access_token");
                }

                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] Error adding auth header: {ex.Message}");
            }
        }

        private async Task<T?> ProcessResponse<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            try
            {
                var apiResponse = JsonSerializer.Deserialize<frontend.Client.Core.Models.ApiResponse<T>>(content, _jsonOptions);
                if (apiResponse != null && apiResponse.Success)
                {
                    return apiResponse.Data;
                }
            }
            catch { /* ignored */ }

            try
            {
                return JsonSerializer.Deserialize<T>(content, _jsonOptions);
            }
            catch
            {
                return default;
            }
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = JsonContent.Create(data, options: _jsonOptions);
                await AddAuthHeader(request);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return await ProcessResponse<TResponse>(response);
                }

                return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API POST Exception: {ex.Message}");
                return default;
            }
        }

        public async Task<TResponse?> PostAsync<TResponse>(string url, object? data = null)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                if (data != null)
                {
                    request.Content = JsonContent.Create(data, options: _jsonOptions);
                }
                await AddAuthHeader(request);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return await ProcessResponse<TResponse>(response);
                }

                return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API POST Exception: {ex.Message}");
                return default;
            }
        }

        public async Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest data)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Put, url);
                request.Content = JsonContent.Create(data, options: _jsonOptions);
                await AddAuthHeader(request);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return await ProcessResponse<TResponse>(response);
                }

                return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API PUT Exception: {ex.Message}");
                return default;
            }
        }

        public async Task<TResponse?> DeleteAsync<TResponse>(string url)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Delete, url);
                await AddAuthHeader(request);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return await ProcessResponse<TResponse>(response);
                }

                return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API DELETE Exception: {ex.Message}");
                return default;
            }
        }

        public async Task<TResponse?> PostMultipartAsync<TResponse>(string url, MultipartFormDataContent content)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = content;
                await AddAuthHeader(request);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return await ProcessResponse<TResponse>(response);
                }

                return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API POST Multipart Exception: {ex.Message}");
                return default;
            }
        }

        public async Task<T?> GetAsync<T>(string url)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                await AddAuthHeader(request);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return await ProcessResponse<T>(response);
                }

                return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API GET Exception: {ex.Message}");
                return default;
            }
        }
    }
}
