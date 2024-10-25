using System.Data;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Commands;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Commands.Persistence;

public class MessaggioCreateCommandPersistence(MessaggioCreateCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly MessaggioCreateCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;

    private static string _sqlInsert = @"INSERT INTO [pfd].[Messaggi]
           ([IdEnte]
           ,[IdUtente]
           ,[Json]
           ,[Anno]
           ,[Mese]
           ,[Prodotto]
           ,[GruppoRuolo]
           ,[Auth]
           ,[Stato]
           ,[DataInserimento]  
           ,[LinkDocumento]
           ,[ContentLanguage]
           ,[ContentType]
           ,[TipologiaDocumento]
           ,[CategoriaDocumento] 
           ,[Lettura]
           ,[Hash]
           ,[FKIdReport])
     VALUES
           (@IdEnte
           ,@IdUtente
           ,@Json
           ,@Anno
           ,@Mese
           ,@Prodotto
           ,@GruppoRuolo
           ,@Auth
           ,@Stato
           ,@DataInserimento  
           ,@LinkDocumento
           ,@ContentLanguage 
           ,@ContentType
           ,@TipologiaDocumento
           ,@CategoriaDocumento
           ,@Lettura
           ,@Hash
           ,@IdReport)";

    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).ExecuteAsync(connection!, _sqlInsert.Add(schema), _command, transaction);
    }
}