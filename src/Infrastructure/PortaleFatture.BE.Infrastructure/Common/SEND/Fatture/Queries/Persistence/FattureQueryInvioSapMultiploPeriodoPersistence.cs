using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public class FattureQueryInvioSapMultiploPeriodoPersistence(FattureInvioSapMultiploPeriodoQuery command) : DapperBase, IQuery<IEnumerable<FatturaInvioMultiploSapPeriodo>?>
{
    private readonly FattureInvioSapMultiploPeriodoQuery _command = command;
    private static readonly string _sql = FattureQueryRicercaBuilder.SelectFattureInvioMultiploSapPeriodo(); 
    public async Task<IEnumerable<FatturaInvioMultiploSapPeriodo>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        // Filtri OPZIONALI gestiti DENTRO la SQL con il pattern (@Param IS NULL OR col = @Param)
        // (vedi FattureQueryRicercaBuilder._sqlFattureInvioMultiploSapPeriodo): qui basta passare i tre
        // parametri, anche a null. Nessuna costruzione dinamica della WHERE.
        var query = new
        {
            _command.AnnoRiferimento,
            _command.MeseRiferimento,
            _command.TipologiaFattura
        };

        return await ((IDatabase)this).SelectAsync<FatturaInvioMultiploSapPeriodo>(
            connection!,
            _sql,
            query,
            transaction);
    }
}