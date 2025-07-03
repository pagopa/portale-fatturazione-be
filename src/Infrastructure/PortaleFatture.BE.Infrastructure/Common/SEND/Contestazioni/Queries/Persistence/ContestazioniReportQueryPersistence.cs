using System.Data;
using System.Dynamic;
using Dapper;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence;

public class ContestazioniReportQueryPersistence(ContestazioniReportQuery command) : DapperBase, IQuery<ReportContestazioniList?>
{
    private readonly ContestazioniReportQuery _command = command;
    private static readonly string _sql = ContestazioniMassiveSQLBuilder.SelectReports();
    private static readonly string _orderBy = ContestazioniMassiveSQLBuilder.OrderByReports();
    private static readonly string _offSet = ContestazioniMassiveSQLBuilder.OffSetReports();
    private static readonly string _sqlCount = ContestazioniMassiveSQLBuilder.SelectCountReports();
    public async Task<ReportContestazioniList?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var list = new ReportContestazioniList();

        var page = _command.Page;
        var size = _command.Size;
        var offset = _offSet;
        var orderBy = _orderBy;

        dynamic parameters = new ExpandoObject();
        var where = string.Empty;
        if (page.HasValue)
            parameters.Page = page;
        else
            offset = string.Empty;

        if (size.HasValue)
            parameters.Size = size;
        else
            offset = string.Empty;

        if (!string.IsNullOrEmpty(_command.Anno))
        {
            where += " WHERE anno=@anno ";
            parameters.Anno = _command.Anno;
        } else
        {
            where += " WHERE 1=1"; 
        } 

        if (!string.IsNullOrEmpty(_command.Mese))
        {
            where += "  AND mese=@mese ";
            parameters.Mese = _command.Mese;
        }

        if (!_command.IdEnti.IsNullNotAny())
        {
            where += " AND internal_organization_id IN @IdEnti";
            parameters.IdEnti = _command.IdEnti;
        }

        if (!_command.IdTipologiaReports.IsNullNotAny())
        {
            where += " AND FkIdTipologiaReport IN @IdTipologiaReports";
            parameters.IdTipologiaReports = _command.IdTipologiaReports;
        }

        var sql = _sql + where + orderBy + offset;
        var sqlCount = _sqlCount + where;

        var sqlMultiple = String.Join(";", sql, sqlCount);
        using var values = await ((IDatabase)this).QueryMultipleAsync<ReportContestazioni>(
            connection!,
            sqlMultiple,
            parameters,
            transaction,
            CommandType.Text,
            null,
            CommandFlags.NoCache);

        list.Reports = await values.ReadAsync<ReportContestazioni>();
        list.Count = await values.ReadFirstAsync<int>();
        return list;
    }
}