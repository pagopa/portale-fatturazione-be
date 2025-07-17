using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Commands.Persistence;

public sealed class ReportNotificheUpdateByIdCommandPersistence(ReportNotificheUpdateByIdCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly ReportNotificheUpdateByIdCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;

    private static readonly string _sqlInsert = @" 
UPDATE [pfw].[ReportNotifiche]
SET [unique_id] = @UniqueId, 
    [link] = @LinkDocumento
WHERE internal_organization_id=@InternalOrganizationId AND report_id=@idreport;
";

    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).ExecuteAsync(connection!, _sqlInsert.Add(schema), _command, transaction);
    }
}