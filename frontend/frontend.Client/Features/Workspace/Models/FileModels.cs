using System;

namespace frontend.Client.Features.Workspace.Models
{
    public class FileResponse
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public string Title { get; set; } = default!;
        public string OwnerName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int LatestVersionNumber { get; set; }
    }

    public class VersionResponse
    {
        public Guid Id { get; set; }
        public Guid FileId { get; set; }
        public int VersionNumber { get; set; }
        public bool IsFull { get; set; }
        public long FileSize { get; set; }
        public string? Checksum { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
