namespace backend.Domain.Entities
{
    public class Feedback
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Status { get; set; } = "open";
        public DateTime CreatedAt { get; set; }
    }
}