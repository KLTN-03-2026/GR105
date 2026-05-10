using System.ComponentModel.DataAnnotations;

namespace backend.Application.DTOs.Admin
{
    public class UpdateAdminUserRequest
    {
        [Required]
        [MinLength(3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty; // "user" or "admin"
    }
}
