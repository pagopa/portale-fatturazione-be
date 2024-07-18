using System.Data;

namespace PortaleFatture.BE.Infrastructure.Common.Persistence; 
 
public interface ICommand
{
    bool RequiresTransaction { get; }
    Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default);
}
public interface ICommand<T> 
{
    bool RequiresTransaction { get; }
    Task<T> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default);
}  