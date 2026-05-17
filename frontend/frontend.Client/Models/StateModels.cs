namespace frontend.Client.Models;

public class WorkspaceStateModel
{
    public Workspace? CurrentWorkspace { get; set; }
    public bool HasActiveWorkspace => CurrentWorkspace != null;
}

public class AuthStateModel
{
    public bool IsAuthenticated { get; set; }
    public User? CurrentUser { get; set; }
}
