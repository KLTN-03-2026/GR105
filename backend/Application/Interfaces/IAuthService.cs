using backend.Application.DTOs.Auth;

namespace backend.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task ForgotPasswordAsync(string email);
        Task ResetPasswordAsync(string token, string newPassword);
    }
}
