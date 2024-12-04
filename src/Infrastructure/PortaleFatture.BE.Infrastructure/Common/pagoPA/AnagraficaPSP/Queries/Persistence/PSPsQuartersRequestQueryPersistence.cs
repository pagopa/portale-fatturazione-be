using System.Data;
using System.Dynamic;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Queries.Persistence;

public sealed class PSPsQuartersRequestQueryPersistence(PSPsQuartersRequestQuery command) : DapperBase, IQuery<IEnumerable<string>>
{
    private readonly PSPsQuartersRequestQuery _command = command;


    private static readonly string _sql = PSPSQLBuilder.SelectQuarters();
    private static readonly string _orderBy = PSPSQLBuilder.OrderByQuarters();
    public async Task<IEnumerable<string>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var where = string.Empty;
        dynamic parameters = new ExpandoObject();

        if (!string.IsNullOrEmpty(_command.Year))
        {
            where = " WHERE year_quarter like '%' + @year + '%'";
            parameters.Year = _command.Year;
        }

        var orderBy = _orderBy;
        var sql = _sql + where + orderBy;


        return await ((IDatabase)this).SelectAsync<string>(
           connection!,
           sql,
           parameters,
           transaction);
    }
}