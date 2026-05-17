using backend.Application.DTOs;
using backend.Application.DTOs.User;
using Microsoft.AspNetCore.Mvc;
using backend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IUserContext _userContext;

        public UserController(IUserService userService, IUserContext userContext)
        {
            _userService = userService;
            _userContext = userContext;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var user = await _userService.GetByIdAsync(id);

            if (user == null) return NotFound();

            var result = new UserResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                GlobalRole = user.GlobalRole,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return Ok(result);
        }

        [HttpGet("me")]
        [Authorize] 
        public async Task<IActionResult> GetMe()
        {
            var currentUserId = _userContext.UserId;

            if (currentUserId == Guid.Empty)
                return Unauthorized();

            var user = await _userService.GetByIdAsync(currentUserId);

            if (user == null) return NotFound();

            var result = new UserResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                GlobalRole = user.GlobalRole,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return Ok(result);
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var currentUserId = _userContext.UserId;
            if (currentUserId == Guid.Empty) return Unauthorized();

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updated = await _userService.UpdateProfileAsync(currentUserId, request);
            if (!updated) return BadRequest(new { message = "Failed to update profile." });

            return Ok(new { message = "Profile updated successfully." });
        }

        [HttpPut("me/password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var currentUserId = _userContext.UserId;
            if (currentUserId == Guid.Empty) return Unauthorized();

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updated = await _userService.ChangePasswordAsync(currentUserId, request);
            if (!updated) return BadRequest(new { message = "Failed to change password." });

            return Ok(new { message = "Password changed successfully." });
            }

            [HttpGet("me/logs")]
            [Authorize]
            public async Task<IActionResult> GetMyActivityLogs([FromQuery] int limit = 50, [FromQuery] int offset = 0)
            {
            var currentUserId = _userContext.UserId;
            if (currentUserId == Guid.Empty) return Unauthorized();

            var logs = await _userService.GetActivityLogsAsync(currentUserId, limit, offset);
            return Ok(logs);
            }
            }
            }