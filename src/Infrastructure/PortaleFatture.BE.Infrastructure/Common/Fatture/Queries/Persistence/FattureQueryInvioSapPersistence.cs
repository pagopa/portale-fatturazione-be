using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.Queries.Persistence;

public class FattureQueryInvioSapPersistence(FattureInvioSapQuery command) : DapperBase, IQuery<IEnumerable<FatturaInvioSap>?>
{
    private readonly FattureInvioSapQuery _command = command;
    private static readonly string _sql = FattureQueryRicercaBuilder.SelectFattureInvioSap();
    private static readonly string _groupBy = FattureQueryRicercaBuilder.GroupByFattureInvioSap();
    private static readonly string _orderBy = FattureQueryRicercaBuilder.OrderByFattureInvioSap();
    public async Task<IEnumerable<FatturaInvioSap>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    { 
        var anno = _command.Anno;
        var mese = _command.Mese;
        string? sql;
        var query = new
        {
            AnnoRiferimento = anno,
            MeseRiferimento = mese 
        };

        if (anno != null && mese != null)
        {
            sql = _sql + " WHERE t.AnnoRiferimento = @AnnoRiferimento AND t.MeseRiferimento = @MeseRiferimento " + _groupBy + _orderBy;
        }
        else
        {
            sql = _sql + _groupBy + _orderBy;
        }   

        return await ((IDatabase)this).SelectAsync<FatturaInvioSap>(
        connection!,
        sql,
        query,
        transaction); 
    }
}