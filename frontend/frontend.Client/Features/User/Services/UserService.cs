using frontend.Client.Core.Constants;
using frontend.Client.Features.User.Models;
using frontend.Client.Services.Http;
using System.Threading.Tasks;

namespace frontend.Client.Features.User.Services
{
    public sealed class UserService
    {
        private readonly IApiClient _apiClient;

        public UserService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<UserProfileResponse?> GetProfileAsync()
        {
            return await _apiClient.GetAsync<UserProfileResponse>(ApiRoutes.UserProfile);
        }

        public async Task<bool> UpdateProfileAsync(UpdateProfileRequest request)
        {
            var response = await _apiClient.PutAsync<UpdateProfileRequest, object>(ApiRoutes.UserProfile, request);
            return response != null;
        }
    }
}
