using System.Text.Json.Serialization;
using frontend.Client.Features.Auth.Models;

namespace frontend.Client.Features.Auth.Models
{
    public sealed class AuthResponse
    {
        // Use both to be safe, or just match backend exactly
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("accessToken")] // Fallback if name changes
        public string AccessToken { get => Token; set => Token = value; }

        [JsonPropertyName("user")]
        public CurrentUser User { get; set; } = default!;
    }
}
