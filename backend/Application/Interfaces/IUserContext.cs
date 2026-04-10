namespace backend.Application.Interfaces
{
    public interface IUserContext
    {
        Guid UserId { get; }
        string GlobalRole { get; }
        bool IsAuthenticated { get; }
    }
}
