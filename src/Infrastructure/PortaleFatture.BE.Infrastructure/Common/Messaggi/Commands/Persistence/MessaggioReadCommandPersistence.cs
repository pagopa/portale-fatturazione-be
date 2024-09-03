using System.Data;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Messaggi.Commands.Persistence;

public class MessaggioReadCommandPersistence(MessaggioReadCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly MessaggioReadCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;

    private static string _sqlUpdate = @"
UPDATE [pfd].[Messaggi]
SET  
    Lettura = @Lettura
WHERE IdUtente = @IdUtente and IdMessaggio=@IdMessaggio";

    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).ExecuteAsync(connection!, _sqlUpdate.Add(schema), _command, transaction);
    }
}