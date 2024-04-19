using System.Data;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Commands;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Notifiche.Commands.Persistence;

public class RelFatturabileByIdEntiCommandPersistence(RelFatturabileByIdEnti command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly RelFatturabileByIdEnti _command = command;
    IStringLocalizer<Localization> _localizer = localizer;

    private static readonly string _sqlUpdate = @"  
    UPDATE [pfd].[Notifiche]
       SET 
           [Fatturabile] = @Fatturabile
     WHERE year=@anno AND month=@mese  AND internal_organization_id IN @EntiIds 
";

    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).ExecuteAsync(connection!, _sqlUpdate.Add(schema), _command, transaction);
    }
}