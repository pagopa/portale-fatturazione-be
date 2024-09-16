using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Report.Dto;
using PortaleFatture.BE.Infrastructure.Common.Report.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.Report.Queries.Persistence;

public class MatriceCostoRecapitistiDataPersistence(MatriceCostoRecapitistiData command) : DapperBase, IQuery<IEnumerable<MatriceCostoRecapitistiDataDto>?>
{
    private readonly MatriceCostoRecapitistiData _command = command;
    private static readonly string _sqlSelectAll = ReportSQLBuilder.SelectMatriceCostoRecapitistiData();  
    public async Task<IEnumerable<MatriceCostoRecapitistiDataDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return (await ((IDatabase)this).SelectAsync<MatriceCostoRecapitistiDataDto>(
             connection!,
             _sqlSelectAll,
             null,
             transaction));
    }
}