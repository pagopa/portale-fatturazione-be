using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Report.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Report.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Report.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Report.Queries.Persistence;

public class MatriceCostoRecapitistiPersistence(MatriceCostoRecapitisti command) : DapperBase, IQuery<IEnumerable<MatriceCostoRecapitistiDto>?>
{
    private readonly MatriceCostoRecapitisti _command = command;
    private static readonly string _sqlSelectAll = ReportSQLBuilder.SelectMatriceCostiRecapitisti();
    public async Task<IEnumerable<MatriceCostoRecapitistiDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var query = new
        {
            _command.DataInizioValidita,
            _command.DataFineValidita
        };

        return await ((IDatabase)this).SelectAsync<MatriceCostoRecapitistiDto>(
             connection!,
             _sqlSelectAll,
             query,
             transaction);
    }
}