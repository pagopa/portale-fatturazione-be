using System.Data;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Notifiche.Commands.Persistence;

public class RelUploadCreateCommandPersistence(RelUploadCreateCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly RelUploadCreateCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;

    private static readonly string _sqlInsert = @" 
    INSERT INTO [schema][RelUpload]
               ([FkIdEnte]
               ,[contract_id]
               ,[TipologiaFattura]
               ,[year]
               ,[month]
               ,[DataEvento]
               ,[IdUtente]
               ,[Azione]
               ,[Hash])
         VALUES
               (@IdEnte
               ,@IdContratto
               ,@TipologiaFattura
               ,@Anno
               ,@Mese
               ,@DataEvento
               ,@IdUtente
               ,@Azione
               ,@Hash) 
";

    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).ExecuteAsync(connection!, _sqlInsert.Add(schema), _command, transaction);
    }
}