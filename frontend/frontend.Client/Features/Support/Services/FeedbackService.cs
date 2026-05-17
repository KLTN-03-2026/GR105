using frontend.Client.Core.Constants;
using frontend.Client.Features.Support.Models;
using frontend.Client.Services.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace frontend.Client.Features.Support.Services
{
    public sealed class FeedbackService
    {
        private readonly IApiClient _apiClient;

        public FeedbackService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<List<FeedbackResponse>> GetFeedbacksAsync()
        {
            var response = await _apiClient.GetAsync<List<FeedbackResponse>>(ApiRoutes.Feedbacks);
            return response ?? new List<FeedbackResponse>();
        }

        public async Task<List<FeedbackResponse>> GetFeedbacksByWorkspaceIdAsync(System.Guid workspaceId)
        {
            var response = await _apiClient.GetAsync<List<FeedbackResponse>>($"{ApiRoutes.WorkspaceFeedbacks}/{workspaceId}");
            return response ?? new List<FeedbackResponse>();
        }

        public async Task<bool> CreateFeedbackAsync(CreateFeedbackRequest request)
        {
            var response = await _apiClient.PostAsync<CreateFeedbackRequest, object>(ApiRoutes.Feedbacks, request);
            return response != null;
        }
    }
}
