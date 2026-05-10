namespace backend.Application.DTOs.Admin
{
    public class AdminPasswordResetRequestResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiredAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
