using System.Data;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Commands;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Commands.Persistence;

public class MessaggioUpdateStateCommandPersistence(MessaggioUpdateStateCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly MessaggioUpdateStateCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;

    private static string _sqlUpdate = @"
UPDATE [pfd].[Messaggi]
SET  
    Stato = @Stato 
WHERE IdMessaggio = @IdMessaggio AND IdUtente = @IdUtente";

    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).ExecuteAsync(connection!, _sqlUpdate.Add(schema), _command, transaction);
    }
}