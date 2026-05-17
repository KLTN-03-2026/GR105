using frontend.Client.Core.Constants;
using frontend.Client.Features.Workspace.Models;
using frontend.Client.Services.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace frontend.Client.Features.Workspace.Services
{
    public sealed class FileService
    {
        private readonly IApiClient _apiClient;

        public FileService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<List<FileResponse>> GetFilesByWorkspaceIdAsync(Guid workspaceId)
        {
            var response = await _apiClient.GetAsync<List<FileResponse>>($"{ApiRoutes.Files}/{workspaceId}");
            return response ?? new List<FileResponse>();
        }

        public async Task<FileResponse?> GetFileByIdAsync(Guid fileId)
        {
            return await _apiClient.GetAsync<FileResponse>($"{ApiRoutes.Files}/detail/{fileId}");
        }

        public async Task<List<VersionResponse>> GetFileVersionsAsync(Guid fileId)
        {
            var response = await _apiClient.GetAsync<List<VersionResponse>>($"{ApiRoutes.FileVersions}/{fileId}");
            return response ?? new List<VersionResponse>();
        }
    }
}
