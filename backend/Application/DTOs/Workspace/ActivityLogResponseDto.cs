using System;

namespace backend.Application.DTOs.Workspace
{
    public class ActivityLogResponseDto
    {
        public Guid Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public Guid? EntityId { get; set; }
        public string? EntityName { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public Guid? UserId { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
    }
}