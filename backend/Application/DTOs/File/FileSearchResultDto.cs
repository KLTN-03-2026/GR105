using System;

namespace backend.Application.DTOs.File
{
    public class FileSearchResultDto
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;
        public Guid? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}