using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Report.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Report.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Report.Queries.Persistence;

public class ReportQueryGetByRicercaPersistence(ReportQueryGetByRicerca command) : DapperBase, IQuery<IEnumerable<ReportDto>?>
{
    private readonly ReportQueryGetByRicerca _command = command;
    private static readonly string _sqlSelectAll = ReportSQLBuilder.SelectAll();
    private static readonly string _orderBy = ReportSQLBuilder.OrderBy();
    public async Task<IEnumerable<ReportDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var where = string.Empty;
        var anno = _command.Anno;
        var mese = _command.Mese;

        if (anno.HasValue)
            where += " AND anno=@anno";
        if (mese.HasValue)
            where += " AND mese=@mese";

        where += " AND t.CategoriaDocumento = @CategoriaDocumento";

        var sql = _sqlSelectAll + where + _orderBy;
        var query = new
        {
            Anno = anno,
            Mese = mese,
            _command.CategoriaDocumento
        };

        return await ((IDatabase)this).SelectAsync<ReportDto>(
             connection!,
             sql,
             query,
             transaction);
    }
}