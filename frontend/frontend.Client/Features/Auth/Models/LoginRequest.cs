using System.ComponentModel.DataAnnotations;

namespace frontend.Client.Features.Auth.Models
{
    public sealed class LoginRequest
    {
        [Required(ErrorMessage = "Tên đăng nhập/Email là bắt buộc")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        public string Password { get; set; } = string.Empty;
    }
}

