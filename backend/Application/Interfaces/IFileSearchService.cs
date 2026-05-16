using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using backend.Application.DTOs.File;

namespace backend.Application.Interfaces
{
    public interface IFileSearchService
    {
        Task<List<FileSearchResultDto>> SearchAsync(Guid workspaceId, string keyword);
        Task InvalidateWorkspaceSearchCacheAsync(Guid workspaceId);
    }
}