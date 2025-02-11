using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using System.Data;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;
 
public class FatturaTipoContrattoUpdatePersistence(FatturaModificaTipoContrattoCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
 
    private readonly FatturaModificaTipoContrattoCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;

    private static readonly string _sqlUpdate = @"
    UPDATE [pfd].[ContrattiTipologia]
       SET [DataCancellazione] = @DataCancellazione
     WHERE FkIdEnte=@IdEnte and FkIdContratto=@IdContratto AND DataCancellazione is null";

    public bool RequiresTransaction => true; 

    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).ExecuteAsync(connection!, _sqlUpdate, _command, transaction);
    }
}