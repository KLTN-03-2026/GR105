using System.Data;

namespace backend.Application.Interfaces;

public interface IDbSession : IDisposable
{
    IDbConnection Connection { get; }
    IDbTransaction? Transaction { get; }
    void BeginTransaction();
    void Commit();
    void Rollback();
}
