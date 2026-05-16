using System.ComponentModel.DataAnnotations;

namespace backend.Application.DTOs.Workspace
{
    public class AcceptInviteRequest
    {
        [Required]
        public string Token { get; set; } = default!;
    }
}