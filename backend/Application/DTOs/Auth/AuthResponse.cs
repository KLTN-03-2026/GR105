namespace backend.Application.DTOs.Auth
{
    public class AuthResponse
    {
        public string Token { get; set; } = default!;
        public UserResponse User { get; set; } = default!;
    }
}
