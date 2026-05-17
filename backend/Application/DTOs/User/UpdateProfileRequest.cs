using System.ComponentModel.DataAnnotations;

namespace backend.Application.DTOs.User
{
    public class UpdateProfileRequest
    {
        [MaxLength(255)]
        public string? Username { get; set; }

        [MaxLength(1000)]
        public string? Bio { get; set; }

        [MaxLength(255)]
        public string? Role { get; set; }

        [MaxLength(255)]
        public string? Team { get; set; }

        [MaxLength(255)]
        public string? Division { get; set; }
    }
}
