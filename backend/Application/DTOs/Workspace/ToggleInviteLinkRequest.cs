using System.ComponentModel.DataAnnotations;

namespace backend.Application.DTOs.Workspace
{
    public class ToggleInviteLinkRequest
    {
        [Required]
        public bool InviteEnabled { get; set; }
    }
}