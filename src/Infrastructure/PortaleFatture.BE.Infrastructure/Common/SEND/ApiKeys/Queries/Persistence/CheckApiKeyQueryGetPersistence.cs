using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries.Persistence;
 
public class CheckApiKeyQueryGetPersistence(ApiKeyQueryGet command, IAesEncryption encryption) : DapperBase, IQuery<IEnumerable<ApiKeyDto>>
{
    private readonly ApiKeyQueryGet _command = command;
    private readonly IAesEncryption _encryption = encryption;
    private static readonly string _sqlSelect = ApiKeySQLBuilder.SelectCheck();

    public async Task<IEnumerable<ApiKeyDto>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        string where = string.Empty;
        if (!String.IsNullOrEmpty(_command.IdEnte))
            where += " AND FkIdEnte=@IdEnte";
        else
            return null!;

        var sql = _sqlSelect + where;
        var key =  await ((IDatabase)this).SelectAsync<ApiKeyDto>(connection!, sql, _command, transaction);
        if(key != null)
        {
            foreach (var k in key) 
                k!.ApiKey = k.ApiKey == null ? null : _encryption.DecryptString(k.ApiKey);  
        }
        else
            return null!;

        return key;
    }
}