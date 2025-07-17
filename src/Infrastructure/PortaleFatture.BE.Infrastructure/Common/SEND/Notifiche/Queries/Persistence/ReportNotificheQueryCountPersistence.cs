using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence.Builder;
using System.Data;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence;

public sealed class ReportNotificheQueryCountPersistence(ReportNotificheQueryCount command) : DapperBase, IQuery<int?>
{
    private readonly ReportNotificheQueryCount _command = command;
    private static readonly string _sql  = NotificaSQLBuilder.SelectRichiesteNotificheCount();

    public async Task<int?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).SingleAsync<int>(
             connection!,
             _sql,
             _command,
             transaction);
    }
}