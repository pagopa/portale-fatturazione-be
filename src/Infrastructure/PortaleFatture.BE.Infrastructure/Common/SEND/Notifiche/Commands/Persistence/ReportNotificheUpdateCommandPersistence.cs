using System.Data;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Commands.Persistence;

public sealed class ReportNotificheUpdateCommandPersistence(ReportNotificheUpdateCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly ReportNotificheUpdateCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;

    private static readonly string _sqlInsert = @" 
UPDATE [pfw].[ReportNotifiche]
SET [stato] = @stato,
    [data_fine] =@datafine,
    [nomedocumento] = @nomedocumento,
    [Count] = @count
WHERE [unique_id] = @UniqueId AND internal_organization_id=@InternalOrganizationId AND stato=@statoatteso;
";

    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).ExecuteAsync(connection!, _sqlInsert.Add(schema), _command, transaction);
    }
}