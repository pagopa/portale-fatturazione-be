using System.Data;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Report.Commands.Persistence;

public class ReportCreateCommandPersistence(ReportCreateCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<long>
{
    public bool RequiresTransaction => false;
    private readonly ReportCreateCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;

    private static readonly string _sqlInsert = @"
INSERT INTO [schema].[Report]
           ([Json]
           ,[Anno]
           ,[Mese]
           ,[Prodotto]
           ,[Stato]
           ,[DataInserimento]
           ,[Storage]
           ,[LinkDocumento]
           ,[ContentLanguage]
           ,[ContentType]
           ,[FkIdTipologiaReport]
           ,[Hash])
     VALUES
           (@Json
           ,@Anno
           ,@Mese
           ,@Prodotto
           ,@Stato
           ,@DataInserimento
           ,@Storage
           ,@LinkDocumento
           ,@ContentLanguage
           ,@ContentType
           ,@FkIdTipologiaReport
           ,@Hash)
Select SCOPE_IDENTITY() 'SCOPE_IDENTITY';
";

    public async Task<long> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).ExecuteAsync(connection!, _sqlInsert.Add(schema), _command, transaction);
    }
}