using backend.Application.DTOs.Workspace;
using backend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class WorkspaceController : ControllerBase
    {
        private readonly IWorkspaceService _workspaceService;
        private readonly IUserContext _userContext;

        public WorkspaceController(IWorkspaceService workspaceService, IUserContext userContext)
        {
            _workspaceService = workspaceService;
            _userContext = userContext;
        }

        [HttpPost]
        public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceRequest request)
        {
            var userId = _userContext.UserId;
            if (userId == Guid.Empty) return Unauthorized();

            var response = await _workspaceService.CreateWorkspaceAsync(request, userId);
            return StatusCode(201, response);
        }

        [HttpGet]
        public async Task<IActionResult> GetOwnedWorkspaces()
        {
            var userId = _userContext.UserId;
            if (userId == Guid.Empty) return Unauthorized();

            var response = await _workspaceService.GetOwnedWorkspacesAsync(userId);
            return Ok(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWorkspace(Guid id, [FromBody] UpdateWorkspaceRequest request)
        {
            var userId = _userContext.UserId;
            if (userId == Guid.Empty) return Unauthorized();

            await _workspaceService.UpdateWorkspaceAsync(id, request, userId);
            return Ok(new { message = "Workspace updated successfully." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWorkspace(Guid id)
        {
            var userId = _userContext.UserId;
            if (userId == Guid.Empty) return Unauthorized();

            await _workspaceService.DeleteWorkspaceAsync(id, userId);
            return Ok(new { message = "Workspace deleted successfully." });
        }

        // --- Quản lý thành viên (UC3) ---

        [HttpPost("{id}/members")]
        public async Task<IActionResult> AddMember(Guid id, [FromBody] AddWorkspaceMemberRequest request)
        {
            var userId = _userContext.UserId;
            if (userId == Guid.Empty) return Unauthorized();

            await _workspaceService.AddMemberAsync(id, request, userId);
            return Ok(new { message = "Member added successfully." });
        }

        [HttpGet("{id}/members")]
        public async Task<IActionResult> GetMembers(Guid id)
        {
            var userId = _userContext.UserId;
            if (userId == Guid.Empty) return Unauthorized();

            var members = await _workspaceService.GetMembersAsync(id, userId);
            return Ok(members);
        }

        [HttpDelete("{id}/members/{userIdToRemove}")]
        public async Task<IActionResult> RemoveMember(Guid id, Guid userIdToRemove)
        {
            var userId = _userContext.UserId;
            if (userId == Guid.Empty) return Unauthorized();

            await _workspaceService.RemoveMemberAsync(id, userIdToRemove, userId);
            return Ok(new { message = "Member removed successfully." });
        }

        // --- Quản lý lời mời & Tham gia (UC8) ---

        [HttpPost("{id}/invite-code")]
        public async Task<IActionResult> GenerateInviteCode(Guid id)
        {
            var userId = _userContext.UserId;
            if (userId == Guid.Empty) return Unauthorized();

            var response = await _workspaceService.GenerateInviteCodeAsync(id, userId);
            return Ok(response);
        }

        [HttpPut("{id}/invite-status")]
        public async Task<IActionResult> ToggleInviteLink(Guid id, [FromBody] ToggleInviteLinkRequest request)
        {
            var userId = _userContext.UserId;
            if (userId == Guid.Empty) return Unauthorized();

            await _workspaceService.ToggleInviteLinkAsync(id, request.InviteEnabled, userId);
            return Ok(new { message = "Invite status updated successfully." });
        }

        [HttpPost("join-by-code")]
        public async Task<IActionResult> JoinByCode([FromBody] JoinByCodeRequest request)
        {
            var userId = _userContext.UserId;
            if (userId == Guid.Empty) return Unauthorized();

            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _workspaceService.JoinByCodeAsync(request.InviteCode, userId);
            return Ok(new { message = "Joined workspace successfully." });
        }

        [HttpPost("{id}/invites")]
        public async Task<IActionResult> InviteByEmail(Guid id, [FromBody] InviteByEmailRequest request)
        {
            var userId = _userContext.UserId;
            if (userId == Guid.Empty) return Unauthorized();

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var token = await _workspaceService.InviteByEmailAsync(id, request.Email, userId);
            // Trả về token để test Postman dễ dàng
            return Ok(new { message = "Invitation sent successfully.", token = token });
        }

        [HttpPost("invites/accept")]
        public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteRequest request)
        {
            var userId = _userContext.UserId;
            if (userId == Guid.Empty) return Unauthorized();

            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _workspaceService.AcceptInviteAsync(request.Token, userId);
            return Ok(new { message = "Joined workspace successfully." });
        }

        [HttpPost("{id}/leave")]
        public async Task<IActionResult> LeaveWorkspace(Guid id)
        {
            var userId = _userContext.UserId;
            if (userId == Guid.Empty) return Unauthorized();

            await _workspaceService.LeaveWorkspaceAsync(id, userId);
            return Ok(new { message = "Left workspace successfully." });
        }

        // --- Activity Logs (UC*.2) ---

        [HttpGet("{id}/logs")]
        public async Task<IActionResult> GetActivityLogs(Guid id, [FromQuery] int limit = 50, [FromQuery] int offset = 0)
        {
            var userId = _userContext.UserId;
            if (userId == Guid.Empty) return Unauthorized();

            // We can resolve IActivityLogRepository directly or inject it into the controller. 
            // For simplicity, let's just get it from services to keep constructor clean since it's just a direct read.
            var logRepo = HttpContext.RequestServices.GetRequiredService<IActivityLogRepository>();
            var workspaceRepo = HttpContext.RequestServices.GetRequiredService<IWorkspaceRepository>();
            
            // Check access
            var hasAccess = await workspaceRepo.IsMemberAsync(id, userId);
            if (!hasAccess) return Forbid();

            var logs = await logRepo.GetWorkspaceLogsAsync(id, limit, offset);
            return Ok(logs);
        }
    }
}