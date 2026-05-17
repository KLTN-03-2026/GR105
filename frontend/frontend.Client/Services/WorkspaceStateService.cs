using System.ComponentModel;
using frontend.Client.Models;

namespace frontend.Client.Services;

public class WorkspaceStateService : INotifyPropertyChanged
{
    private Workspace? _currentWorkspace;

    public Workspace? CurrentWorkspace
    {
        get => _currentWorkspace;
        set
        {
            if (_currentWorkspace != value)
            {
                _currentWorkspace = value;
                OnPropertyChanged(nameof(CurrentWorkspace));
                OnPropertyChanged(nameof(HasActiveWorkspace));
            }
        }
    }

    public bool HasActiveWorkspace => CurrentWorkspace != null;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
