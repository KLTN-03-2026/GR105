using System.ComponentModel.DataAnnotations;

namespace backend.Application.DTOs.User
{
    public class UpdateProfileRequest
    {
        [Required]
        [MaxLength(255)]
        public string Username { get; set; } = string.Empty;
    }
}