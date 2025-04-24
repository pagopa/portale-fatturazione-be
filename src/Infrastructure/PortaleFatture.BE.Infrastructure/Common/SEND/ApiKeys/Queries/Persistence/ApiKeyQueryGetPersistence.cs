using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries.Persistence;
 
public class ApiKeyQueryGetPersistence(ApiKeyQueryGet command) : DapperBase, IQuery<IEnumerable<ApiKeyDto>>
{
    private readonly ApiKeyQueryGet _command = command;
    private static readonly string _sqlSelect = ApiKeySQLBuilder.SelectAll();

    public async Task<IEnumerable<ApiKeyDto>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        string where = string.Empty;
        if (!String.IsNullOrEmpty(_command.IdEnte))
            where += " WHERE FkIdEnte=@IdEnte"; 
        var sql = _sqlSelect + where;
        return await ((IDatabase)this).SelectAsync<ApiKeyDto>(connection!, sql, _command, transaction);
    }
}