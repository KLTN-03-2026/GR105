using System;

namespace frontend.Client.Features.Support.Models
{
    public class CreateFeedbackRequest
    {
        public string Content { get; set; } = string.Empty;
    }

    public class FeedbackResponse
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
