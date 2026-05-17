namespace backend.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public string GlobalRole { get; set; } = default!;
        public string? Bio { get; set; }
        public string? Role { get; set; }
        public string? Team { get; set; }
        public string? Division { get; set; }
        public bool IsLocked { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}