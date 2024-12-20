using System.Data;
using System.Dynamic;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Extensions;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Queries.Persistence;

public sealed class KPIPagamentiMatriceQueryPersistence(KPIPagamentiMatriceQuery command) : DapperBase, IQuery<IEnumerable<KPIPagamentiMatriceDto>>
{
    private readonly KPIPagamentiMatriceQuery _command = command;


    private static readonly string _sql = KPIPagamentiSQLBuilder.SelectMatriceByQuarter();
    private static readonly string _orderBy = KPIPagamentiSQLBuilder.OrderMatriceByQuarter();
    public async Task<IEnumerable<KPIPagamentiMatriceDto>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {

        dynamic parameters = new ExpandoObject(); 
        var (year, start, end) = _command.YearQuarter!.GetYearMonthQuarter();
        var where = " where YEAR([end]) = @year AND MONTH([start]) >=@start AND MONTH([end]) <= @end";
        parameters.Year = year;
        parameters.Start = start;
        parameters.End = end;

        var orderBy = _orderBy;
        var sql = _sql + where + orderBy; 

        return await ((IDatabase)this).SelectAsync<KPIPagamentiMatriceDto>(
           connection!,
           sql,
           parameters,
           transaction);
    }
}