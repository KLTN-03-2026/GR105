using frontend.Client.Core.Constants;
using frontend.Client.Features.Workspace.Models;
using frontend.Client.Services.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace frontend.Client.Features.Workspace.Services
{
    public sealed class WorkspaceService
    {
        private readonly IApiClient _apiClient;

        public WorkspaceService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<List<WorkspaceResponse>> GetOwnedWorkspacesAsync()
        {
            var response = await _apiClient.GetAsync<List<WorkspaceResponse>>(ApiRoutes.Workspaces);
            return response ?? new List<WorkspaceResponse>();
        }

        public async Task<WorkspaceResponse?> GetWorkspaceByIdAsync(System.Guid id)
        {
            return await _apiClient.GetAsync<WorkspaceResponse>($"{ApiRoutes.Workspaces}/{id}");
        }

        public async Task<WorkspaceResponse?> CreateWorkspaceAsync(CreateWorkspaceRequest request)
        {
            return await _apiClient.PostAsync<CreateWorkspaceRequest, WorkspaceResponse>(ApiRoutes.Workspaces, request);
        }
    }
}
