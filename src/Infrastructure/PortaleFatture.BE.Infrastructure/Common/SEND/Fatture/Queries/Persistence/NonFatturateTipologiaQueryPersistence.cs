using System.Data;
using System.Dynamic;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public class NonFatturateTipologiaQueryPersistence(NonFatturateTipologiaQueryRicerca command) : DapperBase, IQuery<IEnumerable<AnniMesiTipologiaDto>?>
{
    private readonly NonFatturateTipologiaQueryRicerca _command = command;
    private static readonly string _sql = FattureQueryRicercaBuilder.SelectPeriodoNonInviate(); 
    public async Task<IEnumerable<AnniMesiTipologiaDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var tipoFattura = _command.TipologiaFattura;
        var filterByTipologia = tipoFattura?.Any() == true ? 1 : 0;

        return await ((IDatabase)this).SelectAsync<AnniMesiTipologiaDto>(
            connection!, _sql,
            new
            {
                TipologiaFattura = tipoFattura,
                FilterByTipologia = filterByTipologia
            }
            , transaction);
    }
}