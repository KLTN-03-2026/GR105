using System.Data;
using backend.Application.Interfaces;
using backend.Infrastructure.Persistence;

namespace backend.Infrastructure.Repositories; // Hoặc Persistence nếu thích tách

public class DbSession : IDbSession
{
    private readonly IDbConnectionFactory _connectionFactory;
    public IDbConnection Connection { get; }
    public IDbTransaction? Transaction { get; private set; }

    public DbSession(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
        Connection = _connectionFactory.Create();
        Connection.Open();
    }

    public void BeginTransaction()
    {
        Transaction = Connection.BeginTransaction();
    }

    public void Commit()
    {
        Transaction?.Commit();
        DisposeTransaction();
    }

    public void Rollback()
    {
        Transaction?.Rollback();
        DisposeTransaction();
    }

    public void Dispose()
    {
        Transaction?.Dispose();
        Connection?.Dispose();
    }

    private void DisposeTransaction()
    {
        Transaction?.Dispose();
        Transaction = null;
    }
}
