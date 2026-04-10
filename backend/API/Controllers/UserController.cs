using backend.Application.DTOs;
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

        // Inject thêm IUserContext để lấy thông tin user từ Token
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
                Email = user.Email
            };

            return Ok(result);
        }

        // Endpoint mới: Lấy thông tin của chính user đang đăng nhập qua Token
        [HttpGet("me")]
        [Authorize] 
        public async Task<IActionResult> GetMe()
        {
            // Lấy ID tự động từ JWT Token thông qua UserContext
            var currentUserId = _userContext.UserId;

            if (currentUserId == Guid.Empty)
                return Unauthorized();

            var user = await _userService.GetByIdAsync(currentUserId);

            if (user == null) return NotFound();

            var result = new UserResponse
            {
                Id = user.Id,
                Email = user.Email
            };

            return Ok(result);
        }
    }
}