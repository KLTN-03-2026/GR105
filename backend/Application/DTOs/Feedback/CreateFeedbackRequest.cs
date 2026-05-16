using System.ComponentModel.DataAnnotations;

namespace backend.Application.DTOs.Feedback
{
    public class CreateFeedbackRequest
    {
        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;
    }
}