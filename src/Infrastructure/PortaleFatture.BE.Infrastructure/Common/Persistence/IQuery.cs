using System.Data; 
namespace PortaleFatture.BE.Infrastructure.Common.Persistence; 
public interface IQuery<T>
{
    Task<T> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default);
}