using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Report.Dto;
using PortaleFatture.BE.Infrastructure.Common.Report.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.Report.Queries.Persistence;

public class ReportQueryGetByIdPersistence(ReportQueryGetById command) : DapperBase, IQuery<ReportDto?>
{
    private readonly ReportQueryGetById _command = command;
    private static readonly string _sqlSelectAll = ReportSQLBuilder.SelectAll();  
    public async Task<ReportDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    { 
        var where = " AND IdReport=@IdReport";   
 
        var sql = _sqlSelectAll + where;  
        var query = new  
        {
            IdReport = _command.IdReport 
        }; 

       return (await ((IDatabase)this).SelectAsync<ReportDto>(
            connection!,
            sql,
            query,
            transaction)).FirstOrDefault(); 
    }
}