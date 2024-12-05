using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.SelfCare.Dto;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.SelfCare.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.SelfCare.Queries.Persistence;

public class EnteCodiceSDIQueryGetByIdPersistence(string? idEnte) : DapperBase, IQuery<EnteContrattoDto>
{
    private static readonly string _sqlSelect = EnteSQLBuilder.SelectContrattoByIdEnte();
    private readonly string? _idEnte = idEnte;

    public async Task<EnteContrattoDto> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var where = " WHERE e.InternalIstitutionId=@IdEnte";
        var results = await ((IDatabase)this).SelectAsync<EnteContrattoDto>(connection!, _sqlSelect + where, new { IdEnte = _idEnte }, transaction);
        return results?.FirstOrDefault()!;
    }
}