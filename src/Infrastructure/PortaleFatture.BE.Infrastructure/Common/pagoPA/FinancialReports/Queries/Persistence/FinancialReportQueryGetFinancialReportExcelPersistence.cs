﻿using System.Data;
using System.Dynamic;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Extensions;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Queries.Persistence;

public sealed class FinancialReportQueryGetFinancialReportExcelPersistence(FinancialReportQueryGetFinancialReportExcel command) : DapperBase, IQuery<IEnumerable<FinancialReportDto>>
{
    private readonly FinancialReportQueryGetFinancialReportExcel _command = command;


    private static readonly string _sql = FinancialReportSQLBuilder.SelectFinancialReport();
    private static readonly string _orderBy = FinancialReportSQLBuilder.OrderByFinancialReport();
    public async Task<IEnumerable<FinancialReportDto>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
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

        where.AddInOrder(" r.year_quarter IN @Quarters");

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

        return await ((IDatabase)this).SelectAsync<FinancialReportDto>(
            connection!,
            sql,
            parameters,
            transaction,
            CommandType.Text);
    }
}