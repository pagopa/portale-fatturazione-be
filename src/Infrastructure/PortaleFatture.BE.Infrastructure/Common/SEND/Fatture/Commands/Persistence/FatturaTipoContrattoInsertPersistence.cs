using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using System.Data;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;
 
public class FatturaTipoContrattoInsertPersistence(FatturaModificaTipoContrattoCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
 
    private readonly FatturaModificaTipoContrattoCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;

    private static readonly string _sqlInsert = @"
DECLARE @RowsAffected INT;
INSERT INTO [pfd].[ContrattiTipologia]
           ([FkIdEnte]
           ,[FkIdTipoContratto]
           ,[FkIdContratto]
           ,[DataInserimento] 
           ,[IdUtente]
           ,[FkIdTipoContrattoPrecedente])
     VALUES
           (@IdEnte
           ,@TipoContratto
           ,@IdContratto
           ,@DataInserimento
           ,@IdUtente
           ,@TipoContrattoPrecedente);

SET @RowsAffected = @@ROWCOUNT;

IF @RowsAffected = 1
BEGIN
    UPDATE [pfd].[Contratti]
       SET 
         [FkIdTipoContratto] = @TipoContratto
      
     WHERE internalistitutionid=@IdEnte AND onboardingtokenid=@IdContratto
 
    SET @RowsAffected = @RowsAffected + @@ROWCOUNT;
END
 
SELECT @RowsAffected AS FinalRowCount;
";

    public bool RequiresTransaction => true;
    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).ExecuteAsync<int>(connection!, _sqlInsert, _command, transaction);
    }
}