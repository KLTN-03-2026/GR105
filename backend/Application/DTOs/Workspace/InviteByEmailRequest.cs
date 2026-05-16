using System.ComponentModel.DataAnnotations;

namespace backend.Application.DTOs.Workspace
{
    public class InviteByEmailRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;
    }
}