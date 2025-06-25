using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence;

public class ContestazioniStepQueryPersistence(ContestazioniStepsQuery command) : DapperBase, IQuery<IEnumerable<ContestazioneStep>?>
{
    private readonly ContestazioniStepsQuery _command = command;
    private static readonly string _sql = ContestazioniMassiveSQLBuilder.SelectSteps(); 
    public async Task<IEnumerable<ContestazioneStep>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {

        return await ((IDatabase)this).SelectAsync<ContestazioneStep>(
            connection!, _sql, _command, transaction); 
    }
}