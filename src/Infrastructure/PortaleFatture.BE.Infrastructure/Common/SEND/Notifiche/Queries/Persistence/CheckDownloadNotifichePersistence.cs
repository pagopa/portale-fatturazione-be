using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence;

public class CheckDownloadNotifichePersistence(CheckDownloadNotificheQuery command) : DapperBase, IQuery<CheckDownloadNotificheDto>
{
    private readonly CheckDownloadNotificheQuery _command = command;
    private static readonly string _sqlAll = NotificaSQLBuilder.CheckDownloadNotifiche();
    public async Task<CheckDownloadNotificheDto> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var sql = _sqlAll;
        return await ((IDatabase)this).SingleAsync<CheckDownloadNotificheDto>(
            connection!, sql.Add(schema), _command, transaction);
    }
}