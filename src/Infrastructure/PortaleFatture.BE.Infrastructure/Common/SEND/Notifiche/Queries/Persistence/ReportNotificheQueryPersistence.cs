using System.Data;
using System.Dynamic;
using Dapper;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence;

public sealed class ReportNotificheQueryPersistence(ReportNotificheQueryCommand command) : DapperBase, IQuery<ReportNotificheListCountDto?>
{
    private readonly ReportNotificheQueryCommand _command = command;
    private static readonly string _sqlSelectAll = ReportNotificaSQLBuilder.SelectAll();
    private static readonly string _sqlSelectAllCount = ReportNotificaSQLBuilder.SelectAllCount();
    private static readonly string _offSet = ReportNotificaSQLBuilder.OffSet();
    private static readonly string _orderBy = ReportNotificaSQLBuilder.OrderBy();

    public async Task<ReportNotificheListCountDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var report = new ReportNotificheListCountDto();
        var where = string.Empty;
        var page = _command.Page;
        var size = _command.Size;
        var inizio = _command.Init;
        var fine = _command.End;
        var ordinamento = _command.Ordinamento.HasValue ? (_command.Ordinamento == 0 ? "ASC" : "DESC") : "ASC";
        var idEnte = _command.AuthenticationInfo.IdEnte;

        where+= $" WHERE e.InternalIstitutionId = @idEnte";

        if (inizio.HasValue && fine.HasValue)
            where += " AND data_inserimento BETWEEN @inizio AND @fine";

        if (inizio.HasValue && !fine.HasValue)
            where += " AND data_inserimento>= @inizio";

        if (!inizio.HasValue && fine.HasValue)
            where += " AND ISNULL(data_fine, data_inserimento)<= @fine";

        if (!inizio.HasValue && !fine.HasValue)
            where += $" AND ISNULL(data_fine, data_inserimento) >= CAST('0001-01-01' AS DATETIME2)";

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
        parameters.idEnte = idEnte;

        if (page.HasValue)
            parameters.Page = page;
        if (size.HasValue)
            parameters.Size = size;

        if (inizio.HasValue)
            parameters.Inizio = inizio;

        if (fine.HasValue)
            parameters.Fine = fine; 

        using (var values = await ((IDatabase)this).QueryMultipleAsync<ReportNotificheListDto>(
            connection!,
            sql,
            parameters,
            transaction,
            CommandType.Text,
            320,
            CommandFlags.NoCache))
        {
            report.Items = await values.ReadAsync<ReportNotificheListDto>();
            report.Count = await values.ReadFirstAsync<int>();
        }

        return report;
    }
}