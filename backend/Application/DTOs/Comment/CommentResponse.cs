namespace backend.Application.DTOs.Comment
{
    public class CommentResponse
    {
        public Guid Id { get; set; }
        public Guid FileId { get; set; }
        public Guid? VersionId { get; set; }
        public string Content { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        public Guid UserId { get; set; }
        public string Username { get; set; } = default!;
    }
}
