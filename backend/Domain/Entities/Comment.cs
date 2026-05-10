namespace backend.Domain.Entities
{
    public class Comment
    {
        public Guid Id { get; set; }
        public Guid FileId { get; set; }
        public Guid? VersionId { get; set; }
        public Guid UserId { get; set; }
        public string Content { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
