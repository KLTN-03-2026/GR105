namespace backend.Application.DTOs.Workspace
{
    public class WorkspaceResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public Guid OwnerUserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
