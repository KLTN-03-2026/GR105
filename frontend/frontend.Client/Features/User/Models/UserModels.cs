using System;

namespace frontend.Client.Features.User.Models
{
    public class UserProfileResponse
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Team { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
    }

    public class UpdateProfileRequest
    {
        public string Bio { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Team { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
    }
}
