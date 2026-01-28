using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;
 

public class FatturePeriodoEnteSospeseQueryPersistence(FatturePeriodoEnteSospeseQuery command) : DapperBase, IQuery<IEnumerable<FatturePeriodoDto>>
{
    private readonly FatturePeriodoEnteSospeseQuery _command = command;
    private static readonly string _sql = FattureQueryRicercaBuilder.SelectPeriodoSospeseEnte(); 
    public async Task<IEnumerable<FatturePeriodoDto>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).SelectAsync<FatturePeriodoDto>(
            connection!, _sql,  new {
                _command.IdEnte
            }, transaction);
    }
}