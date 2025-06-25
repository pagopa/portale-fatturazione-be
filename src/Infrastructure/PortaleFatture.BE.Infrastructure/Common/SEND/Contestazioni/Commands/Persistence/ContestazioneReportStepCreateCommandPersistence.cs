using System.Data;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Commands.Persistence;

public class ContestazioneReportStepCreateCommandPersistence(ContestazioneReportStepCreateCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly ContestazioneReportStepCreateCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;

    private static string _sqlInsert = @"
INSERT INTO [pfd].[ReportContestazioniRighe]
           ([report_id]
           ,[step] 
           ,[Link] 
           ,[Storage]
           ,[NomeDocumento]
           ,DataCompletamento)
     VALUES
           (@IdReport 
           ,@step 
           ,@LinkDocumento
           ,@Storage
           ,@NomeDocumento
           ,@DataCompletamento) 
";


    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).ExecuteAsync(connection!, _sqlInsert, _command, transaction);
    }
}