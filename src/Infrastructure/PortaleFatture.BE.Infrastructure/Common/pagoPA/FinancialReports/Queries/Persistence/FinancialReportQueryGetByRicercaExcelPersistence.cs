using System.Data;
using System.Dynamic;
using Dapper;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Extensions;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Extensions;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Queries.Persistence;

public sealed class FinancialReportQueryGetByRicercaExcelPersistence(FinancialReportQueryGetByRicercaExcel command) : DapperBase, IQuery<IEnumerable<GridFinancialReportDto>>
{
    private readonly FinancialReportQueryGetByRicercaExcel _command = command;


    private static readonly string _sql = FinancialReportSQLBuilder.SelectAll(); 
    private static readonly string _orderBy = FinancialReportSQLBuilder.OrderBy(); 
    public async Task<IEnumerable<GridFinancialReportDto>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    { 
        List<string> where = [];

        var orderBy = _orderBy;

        dynamic parameters = new ExpandoObject();

        if (!_command.Quarters.IsNullNotAny())
        {
            where.AddInOrder(" k.year_quarter IN @Quarters");
            parameters.Quarters = _command.Quarters;
        }

        if (!_command.ContractIds.IsNullNotAny())
        {
            where.AddInOrder(" c.contract_id IN @ContractIds");
            parameters.ContractIds = _command.ContractIds;
        }
        if (!string.IsNullOrEmpty(_command.MembershipId))
        {
            where.AddInOrder(" c.membership_id = @MembershipId");
            parameters.MembershipId = _command.MembershipId;
        }
        if (!string.IsNullOrEmpty(_command.RecipientId))
        {
            where.AddInOrder(" c.recipient_id = @RecipientId");
            parameters.RecipientId = _command.RecipientId;
        }

        if (!string.IsNullOrEmpty(_command.ABI))
        {
            where.AddInOrder(@" 
 (c.provider_names LIKE '%'+ @abi +',%' 
  OR c.provider_names LIKE '%,' +  @abi + '%'
  OR c.provider_names LIKE '%ABI' + @abi + '%'
  OR c.abi = @abi
  OR c.abi = 'ABI' + @abi) ");
            parameters.ABI = _command.ABI;
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

        IEnumerable<GridFinancialReportDto> financialReports = await ((IDatabase)this).SelectAsync<GridFinancialReportDto>(
            connection!,
            sql,
            parameters,
            transaction,
            CommandType.Text);

        return financialReports.Mapv2();
    }
}