namespace backend.Domain.Entities
{
    public class WorkspaceInvitation
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public string Email { get; set; } = default!;
        public string Token { get; set; } = default!;
        public DateTime ExpiredAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}