using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Commands.Persistence;

public sealed class ReportNotificheUpdateLettoCommandPersistence(ReportNotificheUpdateLettoCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly ReportNotificheUpdateLettoCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;

    private static readonly string _sqlInsert = @" 
UPDATE [pfw].[ReportNotifiche]
SET [letto] = @letto,
    [data_lettura] =@datalettura 
WHERE [report_id] = @IdReport AND internal_organization_id=@InternalOrganizationId;
";

    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).ExecuteAsync(connection!, _sqlInsert.Add(schema), _command, transaction);
    }
}