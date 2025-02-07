using System.Data;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;

public class FatturaCancellazioneRigheCommandPersistence(FatturaCancellazioneCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly FatturaCancellazioneCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;
    private static readonly string _sqlRigheDaCancellare =
@"INSERT INTO [pfd].[FattureRighe_Eliminate]
      ([FkIdFattura]
      ,[NumeroLinea]
      ,[Testo]
      ,[CodiceMateriale]
      ,[Quantita]
      ,[PrezzoUnitario]
      ,[Imponibile]
      ,[RigaBollo]
      ,[PeriodoRiferimento])
SELECT 
      [FkIdFattura]
      ,[NumeroLinea]
      ,[Testo]
      ,[CodiceMateriale]
      ,[Quantita]
      ,[PrezzoUnitario]
      ,[Imponibile]
      ,[RigaBollo]
      ,[PeriodoRiferimento]
  FROM [pfd].[FattureRighe]
where FkIdFattura IN @IdFatture";

    private static readonly string _sqlRigheDaRipristinate =
@"INSERT INTO [pfd].[FattureRighe]
      ([FkIdFattura]
      ,[NumeroLinea]
      ,[Testo]
      ,[CodiceMateriale]
      ,[Quantita]
      ,[PrezzoUnitario]
      ,[Imponibile]
      ,[RigaBollo]
      ,[PeriodoRiferimento])
SELECT 
      [FkIdFattura]
      ,[NumeroLinea]
      ,[Testo]
      ,[CodiceMateriale]
      ,[Quantita]
      ,[PrezzoUnitario]
      ,[Imponibile]
      ,[RigaBollo]
      ,[PeriodoRiferimento]
  FROM [pfd].[FattureRighe_Eliminate]
where FkIdFattura IN @IdFatture";
    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var sql = _command.Cancellazione == true ? _sqlRigheDaCancellare : _sqlRigheDaRipristinate;
        return await ((IDatabase)this).ExecuteAsync(connection!, sql.Add(schema), _command, transaction);
    }
}