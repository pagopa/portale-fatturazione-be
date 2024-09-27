using System.Data;
using Dapper;
using static Dapper.SqlMapper;

namespace PortaleFatture.BE.Infrastructure.Common.Persistence;

public abstract class DapperBase : IDatabase
{ 
    private async Task<T> Execute<T>(Task<T> task, IDbConnection connection, IDbTransaction? transaction)
    {
        if (transaction is null)
            using (connection)
                return (await task);
        else
            return (await task);
    }

    public async Task<int> ExecuteAsync(
    IDbConnection connection,
    string sqlCommand,
    object? parameters,
    IDbTransaction? transaction,
    CommandType type,
    int? commandTimeout = null)
    {
        var task = connection.ExecuteAsync(sqlCommand, parameters, transaction, commandTimeout, type);
        return await Execute(task, connection, transaction);
    }

    public async Task<T?> ExecuteAsync<T>(
        IDbConnection connection,
        string sqlCommand,
        object? parameters,
        IDbTransaction? transaction,
        CommandType type,
        int? commandTimeout = null)
    {
        var task = connection.QueryFirstAsync<T>(sqlCommand, parameters, transaction, commandTimeout, type);
        return await Execute(task, connection, transaction);
    }

    public async Task<IEnumerable<T>> SelectAsync<T>(
        IDbConnection connection,
        string sqlQuery,
        object? parameters,
        IDbTransaction? transaction,
        CommandType type,
        int? commandTimeout = null)
    {
        var task = connection.QueryAsync<T>(sqlQuery, parameters, transaction, commandTimeout, type);
        return await Execute(task, connection, transaction);
    }

    public async Task<GridReader> QueryMultipleAsync<T>(
    IDbConnection connection,
    string sqlQuery,
    object? parameters,
    IDbTransaction? transaction,
    CommandType type,
    int? commandTimeout = null,
    CommandFlags flag = CommandFlags.Buffered)
    {
        var commandDefinition = new CommandDefinition(sqlQuery, parameters, transaction, commandTimeout, type, flag);
        return await connection.QueryMultipleAsync(commandDefinition);
    } 
 
    public async Task<T> SingleAsync<T>(
        IDbConnection connection,
        string sqlQuery,
        object? parameters,
        IDbTransaction? transaction,
        CommandType type,
        int? commandTimeout = null)
    {
        var task = connection.QuerySingleAsync<T>(sqlQuery, parameters, transaction, commandTimeout, type);
        return await Execute(task, connection, transaction);
    }
}