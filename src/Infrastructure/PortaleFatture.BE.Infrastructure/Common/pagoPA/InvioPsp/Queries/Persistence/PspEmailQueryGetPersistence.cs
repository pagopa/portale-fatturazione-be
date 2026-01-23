using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Extensions;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Queries;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.InvioPsp.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.InvioPsp.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.InvioPsp.Queries.Persistence;
 
public sealed class PspEmailQueryGetPersistence(PspEmailQueryGet command) : DapperBase, IQuery<IEnumerable<PspEmailDto>>
{
    private readonly PspEmailQueryGet _command = command;


    private static readonly string _sql = PspEmailSQLBuilder.SelectAll();
    private static readonly string _orderBy = PspEmailSQLBuilder.OrderByQuarters();
    public async Task<IEnumerable<PspEmailDto>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
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

        where.AddInOrder(" Trimestre IN @Quarters");

        if (!_command.ContractIds.IsNullNotAny())
        {
            where.AddInOrder(" IdContratto IN @ContractIds");
            parameters.ContractIds = _command.ContractIds;
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

        return await ((IDatabase)this).SelectAsync<PspEmailDto>(
            connection!,
            sql,
            parameters,
            transaction,
            CommandType.Text);
    }
}