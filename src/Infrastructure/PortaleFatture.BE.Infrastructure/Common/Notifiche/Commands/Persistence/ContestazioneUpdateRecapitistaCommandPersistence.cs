using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Notifiche.Commands.Persistence;

public class ContestazioneUpdateRecapitistaCommandPersistence(ContestazioneUpdateRecapitistaCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly ContestazioneUpdateRecapitistaCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;

    private static readonly string _sqlUpdate = @"
UPDATE [schema][Contestazioni]
   SET [NoteRecapitista] = @NoteRecapitista
      ,[DataInserimentoRecapitista] = @DataInserimentoRecapitista
      ,[DataModificaRecapitista] = @DataModificaRecapitista 
      ,[FkIdFlagContestazione]= @StatoContestazione
      ,[Onere]= @Onere
      ,[DataChiusura]= @DataChiusura
 WHERE FkIdNotifica=@IdNotifica AND FkIdFlagContestazione=@ExpectedStatoContestazione
";

    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
       return await ((IDatabase)this).ExecuteAsync(connection!, _sqlUpdate.Add(schema), _command, transaction); 
    }
}