using System.Data;

namespace backend.Application.Interfaces;

public interface IDbConnectionFactory
{
    IDbConnection Create();
}
