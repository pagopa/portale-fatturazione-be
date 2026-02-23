using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries.Persistence;
 
public class ApiKeyQueryGetByKeyEntePersistence(ApiKeyQueryGetByKeyEnte command, IAesEncryption encryption) : DapperBase, IQuery<ApiKeyEnteDto?>
{
    private readonly ApiKeyQueryGetByKeyEnte _command = command;
    private static readonly string _sqlSelect = ApiKeySQLBuilder.SelectKeyByEnte();
    private readonly IAesEncryption _encryption = encryption;
    public async Task<ApiKeyEnteDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var where = string.Empty;
        if (!String.IsNullOrEmpty(_command.ApiKey))
            where += " WHERE ApiKey=@ApiKey";
        else
            return null;
        _command.ApiKey = _command.ApiKey == null ? null : _encryption.EncryptString(_command.ApiKey);
        var sql = _sqlSelect + where;
        return await ((IDatabase)this).SingleAsync<ApiKeyEnteDto>(connection!, sql, _command, transaction);
    }
}