using System.Data;
using MailKit.Search;
using PortaleFatture.BE.Core.Exceptions;
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
        var anno = _command.AnnoRiferimento;
        var mese = _command.MeseRiferimento;
        var tipologiaFattura = _command.TipologiaFattura;
        var sql = string.Empty;

        var query = new
        {
            AnnoRiferimento = anno,
            MeseRiferimento = mese,
            TipologiaFattura = tipologiaFattura
        };

        if (anno != null)
            sql += _sql + " AND AnnoRiferimento = @AnnoRiferimento";
        else throw new ValidationException("Passare un anno di riferimento");

        if (anno != null)
            sql +=  " AND MeseRiferimento = @MeseRiferimento";
        else throw new ValidationException("Passare un mese di riferimento");

        if (anno != null)
            sql += " AND FkTipologiaFattura = @TipologiaFattura";
        else throw new ValidationException("Passare una tipologia di fattura");

        return await ((IDatabase)this).SelectAsync<FatturaInvioMultiploSapPeriodo>(
        connection!,
        sql!,
        query,
        transaction);
    }
}