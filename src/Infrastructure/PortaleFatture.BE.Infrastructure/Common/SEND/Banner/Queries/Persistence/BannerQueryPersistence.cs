using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Banner.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Banner.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Banner.Queries.Persistence;

public class BannerQueryPersistence() : DapperBase, IQuery<BannerDto?>
{
    private static readonly string _sqlSelect = BannerSQLBuilder.Select();
    public async Task<BannerDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {

        return await ((IDatabase)this).SingleAsync<BannerDto>(
            connection!,
            _sqlSelect,
            transaction);
    }
}