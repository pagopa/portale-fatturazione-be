using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Report.Dto;
using PortaleFatture.BE.Infrastructure.Common.Report.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.Report.Queries.Persistence;

public class MatriceCostoRecapitistiPersistence(MatriceCostoRecapitisti command) : DapperBase, IQuery<IEnumerable<MatriceCostoRecapitistiDto>?>
{
    private readonly MatriceCostoRecapitisti _command = command;
    private static readonly string _sqlSelectAll = ReportSQLBuilder.SelectMatriceCostiRecapitisti();  
    public async Task<IEnumerable<MatriceCostoRecapitistiDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var query = new
        {
            DataInizioValidita = _command.DataInizioValidita,
            DataFineValidita = _command.DataFineValidita
        };

        return (await ((IDatabase)this).SelectAsync<MatriceCostoRecapitistiDto>(
             connection!,
             _sqlSelectAll,
             query,
             transaction));
    }
}