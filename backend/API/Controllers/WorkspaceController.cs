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
    }
}
