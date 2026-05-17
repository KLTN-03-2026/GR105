using System;

namespace frontend.Client.Core.State
{
    public class LayoutStateService
    {
        public string Title { get; private set; } = "Dashboard";
        public string ActiveSidebarItem { get; private set; } = "";
        public string ActiveWorkspaceTab { get; private set; } = "documents";
        public Guid CurrentWorkspaceId { get; private set; } = Guid.Empty;
        public bool IsWorkspaceOwner { get; private set; }

        public bool ShowMenu { get; private set; }
        public bool ShowSearch { get; private set; }
        public bool ShowJoin { get; private set; }
        public bool ShowProfile { get; private set; }

        public event Action? OnChange;

        public void SetPageMetadata(string title, string activeItem = "")
        {
            if (Title != title || ActiveSidebarItem != activeItem)
            {
                Title = title;
                ActiveSidebarItem = activeItem;
                NotifyStateChanged();
            }
        }

        public void SetWorkspaceMetadata(string title, string activeTab, Guid workspaceId, bool isOwner = false)
        {
            Title = title;
            ActiveWorkspaceTab = activeTab;
            CurrentWorkspaceId = workspaceId;
            IsWorkspaceOwner = isOwner;
            NotifyStateChanged();
        }

        public void ToggleMenu() { ShowMenu = !ShowMenu; NotifyStateChanged(); }
        public void ToggleSearch() { ShowSearch = !ShowSearch; NotifyStateChanged(); }
        public void ToggleJoin() { ShowJoin = !ShowJoin; NotifyStateChanged(); }
        public void ToggleProfile() { ShowProfile = !ShowProfile; NotifyStateChanged(); }

        public void CloseAllModals()
        {
            ShowMenu = false;
            ShowSearch = false;
            ShowJoin = false;
            ShowProfile = false;
            NotifyStateChanged();
        }

        public void NotifyStateChanged() => OnChange?.Invoke();
    }
}
