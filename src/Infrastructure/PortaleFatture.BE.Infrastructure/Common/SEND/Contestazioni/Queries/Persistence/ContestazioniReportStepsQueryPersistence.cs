using System.Data;
using System.Dynamic;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence;

public class ContestazioniReportStepsQueryPersistence(ContestazioniReportStepQuery command) : DapperBase, IQuery<IEnumerable<ReportContestazioneStepsDto>?>
{
    private readonly ContestazioniReportStepQuery _command = command;
    private static readonly string _sql = ContestazioniMassiveSQLBuilder.SelectReportSteps(); 
    public async Task<IEnumerable<ReportContestazioneStepsDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {  
        return await ((IDatabase)this).SelectAsync<ReportContestazioneStepsDto>(
            connection!,
            _sql,
            _command,
            transaction); 
    }
}