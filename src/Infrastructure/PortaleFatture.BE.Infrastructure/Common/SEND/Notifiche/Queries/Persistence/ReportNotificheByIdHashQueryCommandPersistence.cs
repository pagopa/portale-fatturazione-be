using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence;

public sealed class ReportNotificheByIdHashQueryCommandPersistence(ReportNotificheByIdHashQueryCommand command) : DapperBase, IQuery<ReportNotificheListDto?>
{
    private readonly ReportNotificheByIdHashQueryCommand _command = command;
    private static readonly string _sqlSelectById = ReportNotificaSQLBuilder.SelectAll(); 

    public async Task<ReportNotificheListDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {  
        var where = $" WHERE n.internal_organization_id = @idEnte AND n.hash=@Hash AND n.stato=@stato";
       
        var sql = _sqlSelectById + where;
        try
        {
            return await ((IDatabase)this).SingleAsync<ReportNotificheListDto>(
                connection!,
                sql,
                _command);
        }
        catch 
        { 
            return null;
        }
    }
}