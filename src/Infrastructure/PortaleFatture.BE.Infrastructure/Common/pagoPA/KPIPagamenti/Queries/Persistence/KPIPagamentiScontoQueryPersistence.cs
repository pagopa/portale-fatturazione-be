using System.Data;
using System.Dynamic;
using Dapper;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Extensions;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Extensions;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Queries.Persistence;

public sealed class KPIPagamentiScontoQueryPersistence(KPIPagamentiScontoQuery command) : DapperBase, IQuery<GridKPIPagamentiScontoReportListDto>
{
    private readonly KPIPagamentiScontoQuery _command = command;


    private static readonly string _sql = KPIPagamentiSQLBuilder.SelectSconto();
    private static readonly string _orderBy = KPIPagamentiSQLBuilder.OrderSconto(); 
    private static readonly string _sqlCount = KPIPagamentiSQLBuilder.SelectCountSconto();
    public async Task<GridKPIPagamentiScontoReportListDto> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var fr = new GridKPIPagamentiScontoReportListDto();
        List<string> where = [];

        var orderBy = _orderBy;

        dynamic parameters = new ExpandoObject();

        if (_command.Quarters.IsNullNotAny())
        {
            parameters.Quarters = new List<string>();
            foreach (var quarter in new[] { "_1", "_2", "_3", "_4" })
                parameters.Quarters.Add($"{_command.Year}{quarter}");
        }
        else
            parameters.Quarters = _command.Quarters;

        where.AddInOrder(" kk.year_quarter IN @Quarters"); 

        if (!_command.RecipientIds.IsNullNotAny())
        {
            where.AddInOrder(" kk.recipient_id IN @RecipientIds");
            parameters.RecipientIds = _command.RecipientIds;
        }

        if (!string.IsNullOrEmpty(_command.MembershipId))
        {
            where.AddInOrder(" c.membership_id = @MembershipId");
            parameters.MembershipId = _command.MembershipId;
        } 
 
        if (!string.IsNullOrEmpty(_command.ProviderName))
        {
            where.AddInOrder(" cc.provider_name = @ProviderName");
            parameters.ProviderName = _command.ProviderName;
        }

        if (!string.IsNullOrEmpty(_command.RecipientId))
        {
            where.AddInOrder(" kk.recipient_id  = @RecipientId");
            parameters.RecipientId = _command.RecipientId;
        }

        var sWhere = string.Empty;

        if (!where.IsNullNotAny())
        {
            for (var i = 0; i < where.Count; i++)
            {
                if (i == 0)
                    where[i] = " WHERE " + where[i];
                else
                    where[i] = " AND " + where[i];
            }
            sWhere = string.Join(" ", where);
        }

        var sql = _sql + sWhere + orderBy;  
        var sqlCount = _sqlCount + sWhere;

        var sqlMultiple = String.Join(";", sql, sqlCount);
        using var values = await ((IDatabase)this).QueryMultipleAsync<KPIPagamentiScontoDto>(
            connection!,
            sqlMultiple,
            parameters,
            transaction,
            CommandType.Text,
            null,
            CommandFlags.NoCache); 

        IEnumerable<KPIPagamentiScontoDto> sconti = await values.ReadAsync<KPIPagamentiScontoDto>(); 
        fr.Count = await values.ReadFirstAsync<int>();
        fr.KPIPagamentiScontoReports = sconti.Map();
        return fr;  
    }
}