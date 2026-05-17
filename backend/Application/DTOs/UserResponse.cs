namespace backend.Application.DTOs
{
    public class UserResponse
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string GlobalRole { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? Role { get; set; }
        public string? Team { get; set; }
        public string? Division { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}