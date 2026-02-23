using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public class FattureEliminateQueryPersistence(FattureEliminateQuery command) : DapperBase, IQuery<IEnumerable<FatturaDocContabileRawDto>>
{
    private readonly FattureEliminateQuery _command = command;
    private static readonly string _sql = FattureQueryRicercaBuilder.SelectEliminateEnte();

    public async Task<IEnumerable<FatturaDocContabileRawDto>> Execute(
        IDbConnection? connection,
        string schema,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var anno = _command.Anno;
        var mese = _command.Mese;
        var tipoFattura = _command.TipologiaFattura;
        var filterByTipologia = tipoFattura?.Any() == true ? 1 : 0;
        var filterByDateFattura = _command.DateFattura?.Any() == true ? 1 : 0;
        var dateFattura = _command.DateFattura;

        return await ((IDatabase)this).SelectAsync<FatturaDocContabileRawDto>(
            connection!,
            _sql,
            new
            {
                _command.IdEnte,
                Anno = anno,
                Mese = mese,
                TipologiaFattura = tipoFattura,
                FilterByTipologia = filterByTipologia,
                FilterByDateFattura = filterByDateFattura,
                DateFattura = dateFattura
            },
            transaction);
    }
}
