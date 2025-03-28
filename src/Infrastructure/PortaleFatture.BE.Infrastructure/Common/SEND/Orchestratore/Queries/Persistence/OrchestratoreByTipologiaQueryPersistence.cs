using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.Queries.Persistence;

public class OrchestratoreByTipologiaQueryPersistence(OrchestratoreByTipologiaQuery command) : DapperBase, IQuery<IEnumerable<string>>
{
    private readonly OrchestratoreByTipologiaQuery _command = command;
    private static readonly string _sqlSelectAll = OrchestratoreSQLBuilder.SelectTipologie(); 

    public async Task<IEnumerable<string>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    { 
       return await ((IDatabase)this).SelectAsync<string>(
                connection!, _sqlSelectAll, null, transaction);
    }
}