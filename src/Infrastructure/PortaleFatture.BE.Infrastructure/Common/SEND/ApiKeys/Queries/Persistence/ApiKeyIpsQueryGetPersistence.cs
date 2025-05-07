using System.Data;
using System.Linq;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries.Persistence;

public class ApiKeyIpsQueryGetPersistence(ApiKeyIpsQueryGet command, IAesEncryption encryption) : DapperBase, IQuery<IEnumerable<ApiKeyIpsDto>?>
{
    private readonly ApiKeyIpsQueryGet _command = command;
    private readonly IAesEncryption _encryption = encryption;
    private static readonly string _sqlSelect = ApiKeySQLBuilder.SelectAllIps();
    private static readonly string _sqlOrderBY = ApiKeySQLBuilder.OrderByIps();

    public async Task<IEnumerable<ApiKeyIpsDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var where = string.Empty;
        if (!String.IsNullOrEmpty(_command.IdEnte))
            where += " WHERE FkIdEnte=@IdEnte";
        else
            return null;

        var sql = _sqlSelect + where + _sqlOrderBY;
        var keys = await ((IDatabase)this).SelectAsync<ApiKeyIpsDto?>(connection!, sql, _command, transaction);
        if(keys == null)
            return null;

        foreach (var key in keys) 
            key!.IpAddress = key.IpAddress == null ? null : _encryption.DecryptString(key.IpAddress);

        var kk = keys.OrderBy(x => x!.DataCreazione);
        return kk!;
    }
}