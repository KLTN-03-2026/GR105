using System.ComponentModel.DataAnnotations;

namespace backend.Application.DTOs.Workspace
{
    public class JoinByCodeRequest
    {
        [Required]
        public string InviteCode { get; set; } = default!;
    }
}