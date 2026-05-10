namespace backend.Domain.Entities
{
    public class Workspace
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public Guid OwnerUserId { get; set; }
        public string? InviteCode { get; set; }
        public bool InviteEnabled { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
