using System;

namespace frontend.Client.State
{
    public class WorkspaceState
    {
        public Guid? CurrentWorkspaceId { get; private set; }
        public string? CurrentWorkspaceName { get; private set; }
        
        public event Action? OnChange;

        public void SetCurrentWorkspace(Guid id, string name)
        {
            CurrentWorkspaceId = id;
            CurrentWorkspaceName = name;
            NotifyStateChanged();
        }

        public void ClearWorkspace()
        {
            CurrentWorkspaceId = null;
            CurrentWorkspaceName = null;
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
