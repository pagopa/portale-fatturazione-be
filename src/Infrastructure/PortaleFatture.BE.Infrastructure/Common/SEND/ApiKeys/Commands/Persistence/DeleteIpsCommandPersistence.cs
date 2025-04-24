using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Commands.Persistence;

public class DeleteIpsCommandPersistence(DeleteIpsCommand command) : DapperBase, IQuery<int?>
{
    private readonly DeleteIpsCommand _command = command;
    private static readonly string _sqlInsert =
        $@"
DELETE FROM [pfw].[ApiKeysIPs]
      WHERE ipaddress=@IpAddress AND fkIdEnte=@FkIdEnte
";

    public async Task<int?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).ExecuteAsync(connection!, _sqlInsert, _command, transaction);
    }
}