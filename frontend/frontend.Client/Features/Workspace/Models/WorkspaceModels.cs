using System;

namespace frontend.Client.Features.Workspace.Models
{
    public class WorkspaceResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public Guid OwnerUserId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateWorkspaceRequest
    {
        public string Name { get; set; } = default!;
    }
}
