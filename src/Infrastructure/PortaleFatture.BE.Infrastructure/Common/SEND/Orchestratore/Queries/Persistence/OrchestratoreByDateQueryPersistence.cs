using System.Data;
using System.Dynamic;
using Dapper;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.Queries.Persistence;

public class OrchestratoreByDateQueryPersistence(OrchestratoreByDateQuery command) : DapperBase, IQuery<OrchestratoreDto?>
{
    private readonly OrchestratoreByDateQuery _command = command;
    private static readonly string _sqlSelectAll = OrchestratoreSQLBuilder.SelectAll();
    private static readonly string _sqlSelectAllCount = OrchestratoreSQLBuilder.SelectAllCount();
    private static readonly string _offSet = OrchestratoreSQLBuilder.OffSet();
    private static readonly string _orderBy = OrchestratoreSQLBuilder.OrderBy();

    public async Task<OrchestratoreDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var orchestratore = new OrchestratoreDto();
        var where = string.Empty;
        var page = _command.Page;
        var size = _command.Size;
        var inizio = _command.Init;
        var fine = _command.End;
        var stati = _command.Stati;
        var ordinamento = _command.Ordinamento.HasValue ? (_command.Ordinamento == 0 ? "ASC": "DESC") : "ASC";

        if (inizio.HasValue && fine.HasValue)
            where += " WHERE ISNULL(DataEsecuzione, DataFineContestazioni) BETWEEN @inizio AND @fine";

        if (inizio.HasValue && !fine.HasValue)
            where += " WHERE ISNULL(DataEsecuzione, DataFineContestazioni)>= @inizio";

        if (!inizio.HasValue && fine.HasValue)
            where += " WHERE ISNULL(DataEsecuzione, DataFineContestazioni)<= @fine";

        if (!inizio.HasValue && !fine.HasValue)
            where += $" WHERE ISNULL(DataEsecuzione, DataFineContestazioni) >= '0001-01-01'";

        if (!stati.IsNullNotAny())
            where += $" AND Esecuzione IN @Stati";

        var orderBy = _orderBy.Replace("[ordinamento]", ordinamento);

        var select = _sqlSelectAll;
        var selectCount = _sqlSelectAllCount;
        if (page == null && size == null)
            select += where + orderBy;
        else
            select += where + orderBy + _offSet;

        selectCount += where;

        var sql = string.Join(";", select, selectCount);

        dynamic parameters = new ExpandoObject();
        if (page.HasValue)
            parameters.Page = page;
        if (size.HasValue)
            parameters.Size = size;
        if (inizio.HasValue)
            parameters.Inizio = inizio;
        if (fine.HasValue)
            parameters.Fine = fine;

        if (fine.HasValue)
            parameters.Fine = fine;

        if (!stati.IsNullNotAny())
            parameters.Stati = stati; 

        using (var values = await ((IDatabase)this).QueryMultipleAsync<OrchestratoreItem>(
            connection!,
            sql,
            parameters,
            transaction,
            CommandType.Text,
            320,
            CommandFlags.NoCache))
        {
            orchestratore.Items = await values.ReadAsync<OrchestratoreItem>();
            orchestratore.Count = await values.ReadFirstAsync<int>();
        }

        return orchestratore;
    }
}