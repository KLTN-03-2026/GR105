using backend.Application.DTOs.Admin;
using backend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        // ==========================
        // 1. Quản lý tài khoản (ad_uc1)
        // ==========================
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _adminService.GetAllUsersAsync(search, page, pageSize);
            return Ok(new
            {
                Data = result.Items,
                TotalCount = result.TotalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateAdminUserRequest request)
        {
            var userId = await _adminService.CreateUserAsync(request);
            return Ok(new { Id = userId, Message = "User created successfully." });
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateAdminUserRequest request)
        {
            var success = await _adminService.UpdateUserAsync(id, request);
            if (!success) return NotFound(new { Message = "User not found." });
            return Ok(new { Message = "User updated successfully." });
        }

        [HttpPut("users/{id}/lock")]
        public async Task<IActionResult> ToggleUserLockStatus(Guid id)
        {
            var success = await _adminService.ToggleUserLockStatusAsync(id);
            if (!success) return NotFound(new { Message = "User not found." });
            return Ok(new { Message = "User lock status toggled successfully." });
        }

        // ==========================
        // 2. Quản lý Workspace (ad_uc2)
        // ==========================
        [HttpGet("workspaces")]
        public async Task<IActionResult> GetAllWorkspaces([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _adminService.GetAllWorkspacesAsync(page, pageSize);
            return Ok(new
            {
                Data = result.Items,
                TotalCount = result.TotalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        [HttpGet("workspaces/{id}")]
        public async Task<IActionResult> GetWorkspaceById(Guid id)
        {
            var workspace = await _adminService.GetWorkspaceByIdAsync(id);
            if (workspace == null) return NotFound(new { Message = "Workspace not found." });
            return Ok(workspace);
        }

        [HttpDelete("workspaces/{id}")]
        public async Task<IActionResult> DeleteWorkspace(Guid id)
        {
            var success = await _adminService.DeleteWorkspaceAsync(id);
            if (!success) return NotFound(new { Message = "Workspace not found." });
            return Ok(new { Message = "Workspace deleted successfully. This was a hard delete." });
        }

        // ==========================
        // 3. Giám sát Log hệ thống (ad_uc3)
        // ==========================
        [HttpGet("logs")]
        public async Task<IActionResult> GetAllActivityLogs(
            [FromQuery] Guid? userId,
            [FromQuery] Guid? workspaceId,
            [FromQuery] string? action,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _adminService.GetAllActivityLogsAsync(userId, workspaceId, action, page, pageSize);
            return Ok(new
            {
                Data = result.Items,
                TotalCount = result.TotalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        // ==========================
        // 4. Quản lý Feedback (ad_uc4)
        // ==========================
        [HttpGet("feedbacks")]
        public async Task<IActionResult> GetAllFeedbacks([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _adminService.GetAllFeedbacksAsync(status, page, pageSize);
            return Ok(new
            {
                Data = result.Items,
                TotalCount = result.TotalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        [HttpPut("feedbacks/{id}/status")]
        public async Task<IActionResult> UpdateFeedbackStatus(Guid id, [FromBody] UpdateFeedbackStatusRequest request)
        {
            var success = await _adminService.UpdateFeedbackStatusAsync(id, request.Status);
            if (!success) return NotFound(new { Message = "Feedback not found." });
            return Ok(new { Message = "Feedback status updated successfully." });
        }

        // ==========================
        // 5. Giám sát yêu cầu cấp lại mật khẩu (ad_uc5)
        // ==========================
        [HttpGet("password-reset-requests")]
        public async Task<IActionResult> GetAllPasswordResetRequests([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _adminService.GetAllPasswordResetRequestsAsync(page, pageSize);
            return Ok(new
            {
                Data = result.Items,
                TotalCount = result.TotalCount,
                Page = page,
                PageSize = pageSize
            });
        }
    }
}
