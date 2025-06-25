using System.Data;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Commands.Persistence;

public class ContestazioneReportCreateCommandPersistence(ContestazioneReportCreateCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly ContestazioneReportCreateCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;

    private static string _sqlInsert = @"INSERT INTO [pfd].[ReportContestazioni]
           ([unique_id]
           ,[json]
           ,[anno]
           ,[mese]
           ,[internal_organization_id]
           ,[contract_id]
           ,[utente_id]
           ,[prodotto]
           ,[stato]
           ,[data_inserimento]
           ,[data_stepcorrente]
           ,[storage]
           ,[nomedocumento]
           ,[link]
           ,[content_language]
           ,[content_type]
           ,[FkIdTipologiaReport]
           ,[hash])
     VALUES
           (@UniqueId
           ,@Json
           ,@Anno
           ,@Mese
           ,@InternalOrganizationId
           ,@ContractId
           ,@UtenteId
           ,@Prodotto
           ,@Stato
           ,@DataInserimento
           ,@DataStepCorrente
           ,@Storage
           ,@nomedocumento
           ,@LinkDocumento
           ,@ContentLanguage
           ,@ContentType
           ,@IdTipologiaReport
           ,@Hash);
Select SCOPE_IDENTITY() 'SCOPE_IDENTITY';
";


    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).ExecuteAsync<int>(connection!, _sqlInsert, _command, transaction);
    }
}