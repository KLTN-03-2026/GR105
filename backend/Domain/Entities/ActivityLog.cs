namespace backend.Domain.Entities
{
    public class ActivityLog
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public Guid? WorkspaceId { get; set; }
        public string Action { get; set; } = default!; // Enum cast to string or direct string
        public string? EntityType { get; set; }
        public Guid? EntityId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
