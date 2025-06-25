using System.Data;
using System.Dynamic;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence;

public class ContestazioniReportByIdQueryPersistence(ContestazioniReportStepQuery command) : DapperBase, IQuery<IEnumerable<ReportContestazioni>?>
{
    private readonly ContestazioniReportStepQuery _command = command;
    private static readonly string _sql = ContestazioniMassiveSQLBuilder.SelectReportById(); 
    public async Task<IEnumerable<ReportContestazioni>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {  
        return await ((IDatabase)this).SelectAsync<ReportContestazioni>(
            connection!,
            _sql,
            _command,
            transaction); 
    }
}