using System.Data;
namespace PortaleFatture.BE.Infrastructure.Common.Persistence;

public interface IDatabase
{
    Task<int> ExecuteAsync(IDbConnection connection, string sqlCommand, object? parameters, IDbTransaction? transaction = null, CommandType type = CommandType.Text, int? commandTimeout = null); 
    Task<T?> ExecuteAsync<T>(IDbConnection connection, string sqlCommand, object? parameters, IDbTransaction? transaction = null, CommandType type = CommandType.Text, int? commandTimeout = null); 
    Task<IEnumerable<T>> SelectAsync<T>(IDbConnection connection, string sqlCommand, object? parameters, IDbTransaction? transaction = null, CommandType type = CommandType.Text, int? commandTimeout = null);
    Task<T> SingleAsync<T>(IDbConnection connection, string sqlCommand, object? parameters, IDbTransaction? transaction = null, CommandType type = CommandType.Text, int? commandTimeout = null); 
}