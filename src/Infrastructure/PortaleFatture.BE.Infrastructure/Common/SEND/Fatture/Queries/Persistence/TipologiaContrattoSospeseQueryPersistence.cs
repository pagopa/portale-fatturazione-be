using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public class TipologiaContrattoSospeseQueryPersistence(TipologiaContrattoSospeseQuery command) : DapperBase, IQuery<IEnumerable<TipologiaContrattoDto>>
{
    private readonly TipologiaContrattoSospeseQuery _command = command;
    private static readonly string _sql = ContrattiTipologiaSQLBuilder.SelectTipologiaContrattoSospese();
    public async Task<IEnumerable<TipologiaContrattoDto>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    { 
        return await ((IDatabase)this).SelectAsync<TipologiaContrattoDto>(
        connection!,
        _sql,
        _command,
        transaction);
    }
} 
