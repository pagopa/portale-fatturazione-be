using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Queries.Persistence;

public class CountMessaggiQueryGetByIdUtentePersistence(CountMessaggiQueryGetByIdUtente command) : DapperBase, IQuery<int?>
{
    private readonly CountMessaggiQueryGetByIdUtente _command = command;
    private static readonly string _sqlSelectCount = MessaggioSQLBuilder.SelectCount();
    public async Task<int?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var idUtente = _command.AuthenticationInfo.Id;
        var where = " WHERE IdUtente=@IdUtente AND lettura=0";

        return await ((IDatabase)this).SingleAsync<int>(
            connection!,
            _sqlSelectCount + where,
            new { IdUtente = idUtente },
            transaction);
    }
}