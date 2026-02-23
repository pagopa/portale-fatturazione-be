using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public class AnniMesiTipologiaByEnteQueryPersistence(AnniMesiTipologiaByEnteQuery command) : DapperBase, IQuery<IEnumerable<AnniMesiTipologiaByEnteDto>?>
{
    private readonly AnniMesiTipologiaByEnteQuery _command = command;
    private static readonly string _sql = FattureQueryRicercaBuilder.SelectAnniMesiTipologia();
    private static readonly string _orderBy = FattureQueryRicercaBuilder.GroupOrderByAnniMesiTipologia();
    public async Task<IEnumerable<AnniMesiTipologiaByEnteDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        string? where = null;
        if (!string.IsNullOrEmpty(_command.IdEnte))
            where = " WHERE FkIdEnte =@IdEnte";
        return await ((IDatabase)this).SelectAsync<AnniMesiTipologiaByEnteDto>(
            connection!, _sql + where + _orderBy, _command, transaction);
    }
} 