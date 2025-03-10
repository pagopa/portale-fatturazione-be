using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public class FattureQueryInvioSapMultiploPersistence(FattureInvioSapMultiploQuery command) : DapperBase, IQuery<IEnumerable<FatturaInvioMultiploSap>?>
{
    private readonly FattureInvioSapMultiploQuery _command = command;
    private static readonly string _sql = FattureQueryRicercaBuilder.SelectFattureInvioMultiploSap(); 
    public async Task<IEnumerable<FatturaInvioMultiploSap>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    { 
        return await ((IDatabase)this).SelectAsync<FatturaInvioMultiploSap>(
        connection!,
        _sql,
        null,
        transaction);
    }
}