using frontend.Client.Features.Auth.Models;
using frontend.Client.Services.Storage;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace frontend.Client.Core.State
{
    public sealed class AuthState
    {
        private readonly ICookieService _cookieService;
        public bool IsAuthenticated { get; private set; }
        public bool IsInitialized { get; private set; }
        public CurrentUser? User { get; private set; }
        public string? Token { get; private set; }

        public event Action? OnChange;

        public AuthState(ICookieService cookieService)
        {
            _cookieService = cookieService;
        }

        public async Task InitializeAsync()
        {
            if (IsInitialized) return;

            try
            {
                var token = await _cookieService.GetCookieAsync("access_token");

                if (!string.IsNullOrEmpty(token))
                {
                    Token = token;
                    var handler = new JwtSecurityTokenHandler();

                    if (handler.CanReadToken(token))
                    {
                        var jwtToken = handler.ReadJwtToken(token);

                        if (jwtToken.ValidTo > DateTime.UtcNow)
                        {
                            var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub || c.Type == ClaimTypes.NameIdentifier || c.Type == "id")?.Value;
                            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email || c.Type == "email" || c.Type == ClaimTypes.Email)?.Value;
                            // Check for both short name "role" and long URI
                            var role = jwtToken.Claims.FirstOrDefault(c => c.Type == "role" || c.Type == ClaimTypes.Role)?.Value;

                            if (userId != null)
                            {
                                var username = jwtToken.Claims.FirstOrDefault(c => c.Type == "username" || c.Type == "name" || c.Type == ClaimTypes.Name)?.Value;
                                
                                User = new CurrentUser
                                {
                                    Id = Guid.TryParse(userId, out var guid) ? guid : Guid.Empty,
                                    Email = email ?? "",
                                    GlobalRole = role ?? "user",
                                    Username = username ?? email?.Split('@')[0] ?? "User"
                                };
                                IsAuthenticated = true;
                            }
                        }
                        else
                        {
                            Token = null;
                            await _cookieService.DeleteCookieAsync("access_token");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AuthState] Initialization Error: {ex.Message}");
            }
            finally
            {
                IsInitialized = true;
                NotifyStateChanged();
            }
        }

        public void Set(CurrentUser user, string? token = null)
        {
            User = user;
            if (!string.IsNullOrEmpty(token))
            {
                Token = token;
            }
            IsAuthenticated = true;
            IsInitialized = true;
            NotifyStateChanged();
        }

        public void Clear()
        {
            User = null;
            Token = null;
            IsAuthenticated = false;
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
