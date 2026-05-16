using System.Text.Json.Serialization;

namespace backend.Application.DTOs.File
{
    public class FileResponse
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public string Title { get; set; } = default!;
        
        [JsonIgnore]
        public string? FolderPath { get; set; }
        
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        [JsonIgnore]
        public DateTime? DeletedAt { get; set; }
        
        public int LatestVersionNumber { get; set; }
    }
}
