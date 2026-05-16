using backend.Application.Common.Exceptions;
using backend.Application.DTOs.Feedback;
using backend.Application.Interfaces;
using backend.Domain.Entities;

namespace backend.Application.Services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly IFeedbackRepository _feedbackRepository;

        public FeedbackService(IFeedbackRepository feedbackRepository)
        {
            _feedbackRepository = feedbackRepository;
        }

        public async Task<bool> CreateFeedbackAsync(Guid userId, CreateFeedbackRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
                throw new ValidationException("Feedback content cannot be empty.");

            var feedback = new Feedback
            {
                UserId = userId,
                Content = request.Content.Trim(),
                Status = "open",
                CreatedAt = DateTime.UtcNow
            };

            await _feedbackRepository.CreateAsync(feedback);
            return true;
        }
    }
}