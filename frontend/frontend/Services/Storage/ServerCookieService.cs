using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace frontend.Services.Storage
{
    public sealed class ServerCookieService : frontend.Client.Services.Storage.ICookieService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IJSRuntime _jsRuntime;

        public ServerCookieService(IHttpContextAccessor httpContextAccessor, IJSRuntime jsRuntime)
        {
            _httpContextAccessor = httpContextAccessor;
            _jsRuntime = jsRuntime;
        }

        public async Task SetCookieAsync(string name, string value, int? minutes = null)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("cookieHelper.setCookie", name, value, minutes);
            }
            catch (Exception)
            {
                try
                {
                    var options = new CookieOptions
                    {
                        Path = "/",
                        HttpOnly = false,
                        SameSite = SameSiteMode.Lax,
                        Secure = false // Changed to false for local development
                    };

                    if (minutes.HasValue)
                    {
                        options.Expires = DateTimeOffset.UtcNow.AddMinutes(minutes.Value);
                    }

                    var context = _httpContextAccessor.HttpContext;
                    if (context != null && !context.Response.HasStarted)
                    {
                        context.Response.Cookies.Append(name, value, options);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ServerCookieService] SetCookie Error: {ex.Message}");
                }
            }
        }

        public Task<string?> GetCookieAsync(string name)
        {
            var value = _httpContextAccessor.HttpContext?.Request.Cookies[name];
            return Task.FromResult(value);
        }

        public async Task DeleteCookieAsync(string name)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("cookieHelper.deleteCookie", name);
            }
            catch
            {
                var context = _httpContextAccessor.HttpContext;
                if (context != null && !context.Response.HasStarted)
                {
                    context.Response.Cookies.Delete(name);
                }
            }
        }
    }
}
