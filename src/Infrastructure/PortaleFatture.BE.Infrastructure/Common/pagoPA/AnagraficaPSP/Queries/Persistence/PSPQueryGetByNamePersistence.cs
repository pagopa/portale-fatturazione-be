using System.Data;
using System.Dynamic;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Queries.Persistence;

public sealed class PSPQueryGetByNamePersistence(PSPQueryGetByName command) : DapperBase, IQuery<IEnumerable<ContractIdPSP>>
{
    private readonly PSPQueryGetByName _command = command;
    private static readonly string _sql = PSPSQLBuilder.SelectContractsId();
    private static readonly string _orderBy = PSPSQLBuilder.OrderBy();
    public async Task<IEnumerable<ContractIdPSP>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        dynamic parameters = new ExpandoObject(); 

        var where = " WHERE name like '%' + @name + '%'";
        if (_command.YearQuarter.IsNullNotAny())
            where += " AND year_quarter = (SELECT MAX(year_quarter) FROM [ppa].[Contracts])";
        else
        { 
            where += " AND year_quarter IN @YearQuarter";
            parameters.YearQuarter = _command.YearQuarter;
        }

        var orderBy = _orderBy;
        var name = _command.Name;
        parameters.Name = name;

        var sql = _sql + where + orderBy;

        return await ((IDatabase)this).SelectAsync<ContractIdPSP>(
           connection!,
           sql,
           parameters,
           transaction);
    }
}