using backend.Application.DTOs.Feedback;
using backend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbacksController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;
        private readonly IUserContext _userContext;

        public FeedbacksController(IFeedbackService feedbackService, IUserContext userContext)
        {
            _feedbackService = feedbackService;
            _userContext = userContext;
        }

        [HttpPost]
        public async Task<IActionResult> CreateFeedback([FromBody] CreateFeedbackRequest request)
        {
            var userId = _userContext.UserId;
            if (userId == Guid.Empty) return Unauthorized();

            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _feedbackService.CreateFeedbackAsync(userId, request);
            
            return Ok(new { message = "Feedback submitted successfully." });
        }
    }
}