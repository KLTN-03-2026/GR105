using Microsoft.JSInterop;
using System.Text.Json;

namespace frontend.Client.Services.Storage
{
    public sealed class LocalStorageService
    {
        private readonly IJSRuntime _jsRuntime;

        public LocalStorageService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task SetItemAsync<T>(string key, T value)
        {
            try
            {
                if (value is string stringValue)
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, stringValue);
                }
                else
                {
                    var json = JsonSerializer.Serialize(value);
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
                }
            }
            catch (InvalidOperationException)
            {
                // Likely SSR
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LocalStorage] SetItem Error: {ex.Message}");
            }
        }

        public async Task<T?> GetItemAsync<T>(string key)
        {
            try
            {
                var data = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
                if (string.IsNullOrEmpty(data)) return default;

                if (typeof(T) == typeof(string))
                {
                    return (T)(object)data;
                }

                return JsonSerializer.Deserialize<T>(data);
            }
            catch (InvalidOperationException)
            {
                // Likely SSR
                return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LocalStorage] GetItem Error: {ex.Message}");
                return default;
            }
        }

        public async Task RemoveItemAsync(string key)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
            }
            catch (InvalidOperationException)
            {
                // Likely SSR
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LocalStorage] RemoveItem Error: {ex.Message}");
            }
        }
    }
}
