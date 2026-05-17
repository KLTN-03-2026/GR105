using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace frontend.Client.Services.Storage
{
    public sealed class CookieService : ICookieService
    {
        private readonly IJSRuntime _jsRuntime;

        public CookieService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task SetCookieAsync(string name, string value, int? minutes = null)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("cookieHelper.setCookie", name, value, minutes);
            }
            catch (InvalidOperationException)
            {
                // SSR - cannot call JS
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CookieService] SetCookie Error: {ex.Message}");
            }
        }

        public async Task<string?> GetCookieAsync(string name)
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string?>("cookieHelper.getCookie", name);
            }
            catch (InvalidOperationException)
            {
                // SSR - cannot call JS
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CookieService] GetCookie Error: {ex.Message}");
                return null;
            }
        }

        public async Task DeleteCookieAsync(string name)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("cookieHelper.deleteCookie", name);
            }
            catch (InvalidOperationException)
            {
                // SSR
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CookieService] DeleteCookie Error: {ex.Message}");
            }
        }
    }
}
