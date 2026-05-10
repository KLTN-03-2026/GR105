using backend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace backend.API.Hubs
{
    [Authorize]
    public class WorkspaceHub : Hub
    {
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IUserContext _userContext;

        public WorkspaceHub(IWorkspaceRepository workspaceRepository, IUserContext userContext)
        {
            _workspaceRepository = workspaceRepository;
            _userContext = userContext;
        }

        // Bắt buộc Client truyền fileId để join đúng room của file
        // Tham số workspaceId được truyền để double check quyền truy cập (Tránh User ngoài mò fileId).
        public async Task JoinFileGroup(Guid workspaceId, Guid fileId)
        {
            var userId = _userContext.UserId;
            if (userId == Guid.Empty)
            {
                throw new HubException("Unauthorized.");
            }

            bool isMember = await _workspaceRepository.IsUserInWorkspaceAsync(userId, workspaceId);
            if (!isMember)
            {
                throw new HubException("Bạn không có quyền truy cập vào Workspace này.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"file_{fileId}");
        }

        public async Task LeaveFileGroup(Guid fileId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"file_{fileId}");
        }
    }
}
