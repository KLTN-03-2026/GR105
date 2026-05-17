using frontend.Client.Core.Constants;
using frontend.Client.Core.State;
using frontend.Client.Features.Auth.Models;
using frontend.Client.Services.Http;
using frontend.Client.Services.Storage;
using frontend.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace frontend.Client.Features.Auth.Services
{
    public sealed class AuthService
    {
        private readonly IApiClient _apiClient;
        private readonly AuthState _authState;
        private readonly LocalStorageService _localStorageService;
        private readonly ICookieService _cookieService;
        private readonly NavigationManager _navigationManager;
        private readonly AuthenticationStateProvider _authStateProvider;

        public AuthService(
            IApiClient apiClient,
            AuthState authState,
            LocalStorageService localStorageService,
            ICookieService cookieService,
            NavigationManager navigationManager,
            AuthenticationStateProvider authStateProvider)
        {
            _apiClient = apiClient;
            _authState = authState;
            _localStorageService = localStorageService;
            _cookieService = cookieService;
            _navigationManager = navigationManager;
            _authStateProvider = authStateProvider;
        }

        public async Task<bool> LoginAsync(LoginRequest request)
        {
            bool success = false;
            string? role = null;

            try
            {
                var response = await _apiClient.PostAsync<LoginRequest, AuthResponse>(ApiRoutes.Login, request);
                var tokenValue = response?.Token ?? response?.AccessToken;

                if (response != null && !string.IsNullOrEmpty(tokenValue))
                {
                    var user = response.User;

                    // 1. Lưu token vào Cookie (60 phút)
                    await _cookieService.SetCookieAsync("access_token", tokenValue, 60);

                    // 2. Lưu token vào LocalStorage
                    await _localStorageService.SetItemAsync("auth_token", tokenValue);

                    // 3. Cập nhật AuthState (Memory)
                    _authState.Set(user, tokenValue);

                    // Notify the AuthenticationStateProvider
                    if (_authStateProvider is CustomAuthStateProvider customProvider)
                    {
                        customProvider.MarkUserAsAuthenticated(tokenValue);
                    }

                    // Robust role check: Check user object first, then fallback to JWT parsing
                    role = user?.GlobalRole;
                    Console.WriteLine($"[AuthService] Role from user object: {role}");

                    if (string.IsNullOrEmpty(role))
                    {
                         try {
                            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                            var jwt = handler.ReadJwtToken(tokenValue);
                            role = jwt.Claims.FirstOrDefault(c => c.Type == "role" || 
                                                                 c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" ||
                                                                 c.Type == "GlobalRole")?.Value;
                            Console.WriteLine($"[AuthService] Role from JWT claims: {role}");
                         } catch (Exception ex) { 
                            Console.WriteLine($"[AuthService] JWT Parse Error: {ex.Message}");
                         }
                    }

                    if (string.IsNullOrEmpty(role)) role = "user";
                    success = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AuthService] Login Error: {ex.Message}");
                return false;
            }

            if (success)
            {
                Console.WriteLine($"[AuthService] Login Success. Final Role: {role}");
                // Always redirect to root; role-based interception is handled at the router level
                _navigationManager.NavigateTo("/", true);
                return true;
            }

            return false;
        }

        public async Task LogoutAsync()
        {
            await _cookieService.DeleteCookieAsync("access_token");
            await _localStorageService.RemoveItemAsync("auth_token");
            _authState.Clear();
            if (_authStateProvider is CustomAuthStateProvider customProvider)
            {
                customProvider.MarkUserAsLoggedOut();
            }
            _navigationManager.NavigateTo("/login", true);
        }
    }
}