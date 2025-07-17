using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Commands.Persistence;

public sealed class ReportNotificheCreateCommandPersistence(ReportNotificheCreateCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly ReportNotificheCreateCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;

    private static readonly string _sqlInsert = @"
INSERT INTO [pfw].[ReportNotifiche]
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
           ,[storage]
           ,[link]
           ,[content_language]
           ,[content_type]
           ,[FkIdTipologiaReport]
           ,[hash]
           ,[letto])
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
           ,@Storage
           ,@Link
           ,@ContentLanguage
           ,@ContentType
           ,(SELECT IdTipologiaReport FROM [pfd].[TipologiaReport] WHERE CategoriaDocumento = 'REPORT NOTIFICHE')
           ,@Hash
           ,@Letto);
Select SCOPE_IDENTITY() 'SCOPE_IDENTITY';
";

    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).ExecuteAsync<int>(connection!, _sqlInsert.Add(schema), _command, transaction);
    }
}