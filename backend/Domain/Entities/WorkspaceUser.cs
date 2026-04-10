namespace backend.Domain.Entities
{
    public class WorkspaceUser
    {
        public Guid UserId { get; set; }
        public Guid WorkspaceId { get; set; }
        public string Role { get; set; } = default!;
        public DateTime JoinedAt { get; set; }
    }
}
