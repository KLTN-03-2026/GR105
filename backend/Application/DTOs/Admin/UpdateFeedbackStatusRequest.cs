using System.ComponentModel.DataAnnotations;

namespace backend.Application.DTOs.Admin
{
    public class UpdateFeedbackStatusRequest
    {
        [Required]
        public string Status { get; set; } = string.Empty; // "open" or "resolved"
    }
}
