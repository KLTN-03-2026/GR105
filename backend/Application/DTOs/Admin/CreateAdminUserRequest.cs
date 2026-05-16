using System.ComponentModel.DataAnnotations;

namespace backend.Application.DTOs.Admin
{
    public class CreateAdminUserRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "user"; // "user" or "admin"
    }
}
