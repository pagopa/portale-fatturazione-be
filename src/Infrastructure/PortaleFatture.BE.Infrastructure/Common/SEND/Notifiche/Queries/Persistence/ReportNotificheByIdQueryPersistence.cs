using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence;

public sealed class ReportNotificheByIdQueryPersistence(ReportNotificheByIdQueryCommand command) : DapperBase, IQuery<ReportNotificheListDto?>
{
    private readonly ReportNotificheByIdQueryCommand _command = command;
    private static readonly string _sqlSelectById = ReportNotificaSQLBuilder.SelectAll(); 

    public async Task<ReportNotificheListDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {  
        var where = $" WHERE e.InternalIstitutionId = @idEnte AND report_id=@IdReport";

        var sql = _sqlSelectById + where;
        return await ((IDatabase)this).SingleAsync<ReportNotificheListDto>(
            connection!,
            sql,
            _command); 
    }
}