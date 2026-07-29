using System.Data;
using Dapper;
using static Dapper.SqlMapper;

namespace PortaleFatture.BE.Infrastructure.Common.Persistence;

public interface IDatabase
{
    Task<GridReader> QueryMultipleAsync<T>(IDbConnection connection, string sqlCommand, object? parameters, IDbTransaction? transaction = null, CommandType type = CommandType.Text, int? commandTimeout = null,
    CommandFlags flag = CommandFlags.Buffered);
    Task<int> ExecuteAsync(IDbConnection connection, string sqlCommand, object? parameters, IDbTransaction? transaction = null, CommandType type = CommandType.Text, int? commandTimeout = null); 
    Task<T?> ExecuteAsync<T>(IDbConnection connection, string sqlCommand, object? parameters, IDbTransaction? transaction = null, CommandType type = CommandType.Text, int? commandTimeout = null); 
    Task<IEnumerable<T>> SelectAsync<T>(IDbConnection connection, string sqlCommand, object? parameters, IDbTransaction? transaction = null, CommandType type = CommandType.Text, int? commandTimeout = null);
    Task<T> SingleAsync<T>(IDbConnection connection, string sqlCommand, object? parameters, IDbTransaction? transaction = null, CommandType type = CommandType.Text, int? commandTimeout = null);
    // Come SingleAsync ma NON lancia su 0 righe: ritorna default(T) (null per reference). Usare quando
    // "non trovato" e' un esito legittimo che l'handler traduce in 404 (es. dettaglio per chiave).
    Task<T?> SingleOrDefaultAsync<T>(IDbConnection connection, string sqlCommand, object? parameters, IDbTransaction? transaction = null, CommandType type = CommandType.Text, int? commandTimeout = null); 
}