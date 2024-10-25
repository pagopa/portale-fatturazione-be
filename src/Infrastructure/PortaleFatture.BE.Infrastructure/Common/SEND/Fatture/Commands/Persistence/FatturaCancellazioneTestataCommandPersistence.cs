using System.Data;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;

public class FatturaCancellazioneTestataCommandPersistence(FatturaCancellazioneCommand command, IStringLocalizer<Localization> localizer) : DapperBase, ICommand<int>
{
    public bool RequiresTransaction => false;
    private readonly FatturaCancellazioneCommand _command = command;
    IStringLocalizer<Localization> _localizer = localizer;
    private static readonly string _sqlTestateDaCancellare = @"
INSERT INTO [pfd].[FattureTestata_Eliminate]
      ([IdFattura]
      ,[FkProdotto]
      ,[FkIdTipoDocumento]
      ,[FkTipologiaFattura]
      ,[FkIdEnte]
      ,[FkIdDatiFatturazione]
      ,[DataFattura]
      ,[IdentificativoFattura]
      ,[TotaleFattura]
      ,[Divisa]
      ,[MetodoPagamento]
      ,[AnnoRiferimento]
      ,[MeseRiferimento]
      ,[CausaleFattura]
      ,[Sollecito]
      ,[CodiceContratto]
      ,[SplitPayment]
      ,[Cup]
      ,[Cig]
      ,[IdDocumento]
      ,[DataDocumento]
      ,[NumItem]
      ,[CodCommessa]
      ,[Progressivo])
SELECT 
      [IdFattura]
      ,[FkProdotto]
      ,[FkIdTipoDocumento]
      ,[FkTipologiaFattura]
      ,[FkIdEnte]
      ,[FkIdDatiFatturazione]
      ,[DataFattura]
      ,[IdentificativoFattura]
      ,[TotaleFattura]
      ,[Divisa]
      ,[MetodoPagamento]
      ,[AnnoRiferimento]
      ,[MeseRiferimento]
      ,[CausaleFattura]
      ,[Sollecito]
      ,[CodiceContratto]
      ,[SplitPayment]
      ,[Cup]
      ,[Cig]
      ,[IdDocumento]
      ,[DataDocumento]
      ,[NumItem]
      ,[CodCommessa]
      ,[Progressivo]  
  FROM [pfd].[FattureTestata]
where IdFattura IN @IdFatture";

    private static readonly string _sqlTestateDaRipristinare = @"
    SET IDENTITY_INSERT [pfd].[FattureTestata] ON;
    INSERT INTO [pfd].[FattureTestata]
    ([IdFattura]
          ,[FkProdotto]
          ,[FkIdTipoDocumento]
          ,[FkTipologiaFattura]
          ,[FkIdEnte]
          ,[FkIdDatiFatturazione]
          ,[DataFattura]
          ,[IdentificativoFattura]
          ,[TotaleFattura]
          ,[Divisa]
          ,[MetodoPagamento]
          ,[AnnoRiferimento]
          ,[MeseRiferimento]
          ,[CausaleFattura]
          ,[Sollecito]
          ,[CodiceContratto]
          ,[SplitPayment]
          ,[Cup]
          ,[Cig]
          ,[IdDocumento]
          ,[DataDocumento]
          ,[NumItem]
          ,[CodCommessa]
          ,[Progressivo]
          ,[FatturaInviata])
    SELECT 
       [IdFattura]
      ,[FkProdotto]
      ,[FkIdTipoDocumento]
      ,[FkTipologiaFattura]
      ,[FkIdEnte]
      ,[FkIdDatiFatturazione]
      ,[DataFattura]
      ,[IdentificativoFattura]
      ,[TotaleFattura]
      ,[Divisa]
      ,[MetodoPagamento]
      ,[AnnoRiferimento]
      ,[MeseRiferimento]
      ,[CausaleFattura]
      ,[Sollecito]
      ,[CodiceContratto]
      ,[SplitPayment]
      ,[Cup]
      ,[Cig]
      ,[IdDocumento]
      ,[DataDocumento]
      ,[NumItem]
      ,[CodCommessa]
      ,[Progressivo]
      ,0
    FROM [pfd].[FattureTestata_Eliminate]
    where IdFattura IN @IdFatture;
    SET IDENTITY_INSERT [pfd].[FattureTestata] OFF;";

    public async Task<int> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var sql = _command.Cancellazione == true ? _sqlTestateDaCancellare : _sqlTestateDaRipristinare;
        return await ((IDatabase)this).ExecuteAsync(connection!, sql.Add(schema), _command, transaction);
    }
}