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

public class FattureRiepilogoQueryPersistence (FattureRiepilogoQueryRicerca command) : DapperBase, IQuery<IEnumerable<FatturaRiepilogoDto>>
{
    private readonly FattureRiepilogoQueryRicerca _command = command;
    private static readonly string _sql = FattureQueryRicercaBuilder.SelectAllRiepilogo();

    public async Task<IEnumerable<FatturaRiepilogoDto>> Execute(
        IDbConnection? connection,
        string schema,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var anno = _command.Anno;
        var mese = _command.Mese;
        var filterByEnte = _command.IdEnti?.Any() == true ? 1 : 0;
        var idEnti = _command.IdEnti;
        //var tipoFattura = _command.TipologiaFattura;

        return await ((IDatabase)this).SelectAsync<FatturaRiepilogoDto>(
            connection!,
            _sql,
            new
            {
                Anno = anno,
                Mese = mese,
                FilterByEnte = filterByEnte,
                IdEnti = idEnti
            },
            transaction);
    }
}
