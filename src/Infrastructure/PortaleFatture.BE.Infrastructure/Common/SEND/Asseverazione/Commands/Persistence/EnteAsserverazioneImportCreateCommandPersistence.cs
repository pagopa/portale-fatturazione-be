using System.Data;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Asseverazione.Commands.Persistence;

public class EnteAsserverazioneImportCreateCommandPersistence(List<EnteAsserverazioneImportCreateCommand> commands, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly List<EnteAsserverazioneImportCreateCommand> _commands = commands;
    IStringLocalizer<Localization> _localizer = localizer;

    private static readonly string _sqlInsert = @"  
    INSERT INTO [pfd].[Asseverazione]
           ([InternalIstitutionId]
           ,[Asseverazione]
           ,[Data]
           ,[Timestamp]
           ,[IdUtente]
           ,[RagioneSociale]
           ,[Descrizione])
     VALUES
           (@IdEnte
           ,@TipoAsseverazione
           ,@DataAsseverazione
           ,@TimeStamp
           ,@IdUtente
           ,@RagioneSociale
           ,@Descrizione) 
";

    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).ExecuteAsync(connection!, _sqlInsert.Add(schema), _commands, transaction);
    }
}