using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries.Persistence;

public sealed class RelNonFatturateQueryPersistence(RelNonFatturateQuery command) : DapperBase, IQuery<IEnumerable<RelNonFatturataDto>?>
{
    private readonly RelNonFatturateQuery _command = command;
    private static readonly string _sqlSelectAll = RelNonFatturateSQLBuilder.SelectAll();
    public async Task<IEnumerable<RelNonFatturataDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).SelectAsync<RelNonFatturataDto>(
            connection!,
            _sqlSelectAll,
            null,
            transaction);
    }
}