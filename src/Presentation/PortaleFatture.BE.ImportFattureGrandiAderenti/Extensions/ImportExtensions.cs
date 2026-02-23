using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.ImportFattureGrandiAderenti.Models;

namespace PortaleFatture.BE.ImportFattureGrandiAderenti.Extensions;

internal static class ImportExtensions
{
    public static async Task<List<FatturaImport>> GetFile(this string[] subDirectories, string tipologiaFattura)
    {
        var list = new List<FatturaImport>();
        foreach (var subDir in subDirectories)
        {
            var name = Path.GetFileName(subDir);
            var filesInSubDir = Directory.GetFiles(subDir); 
            if (subDir.Contains(tipologiaFattura))

                foreach (var file in filesInSubDir)
                {
                    var jsonContent = await File.ReadAllTextAsync(file);
                    var jsonObject = jsonContent.Deserialize<FatturaImport>();
                    list.Add(jsonObject); 
                }
        } 
        return list;
    }
    public static string SQLInsertTestata()
            {
        return $@"
INSERT INTO [pfd].[FattureTestata]
           ([FkProdotto]
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
     VALUES
           (@FkProdotto
           ,@FkIdTipoDocumento
           ,@FkTipologiaFattura
           ,@FkIdEnte
           ,@FkIdDatiFatturazione
           ,@DataFattura
           ,@IdentificativoFattura 
           ,@TotaleFattura
           ,@Divisa
           ,@MetodoPagamento
           ,@AnnoRiferimento
           ,@MeseRiferimento
           ,@CausaleFattura
           ,@Sollecito
           ,@CodiceContratto
           ,@SplitPayment
           ,@Cup
           ,@Cig
           ,@IdDocumento
           ,@DataDocumento
           ,@NumItem
           ,@CodCommessa
           ,@Progressivo
           ,1);
SELECT CAST(SCOPE_IDENTITY() as int);
";
    }

    public static string SQLInsertRiga()
    {
        return $@"
INSERT INTO [pfd].[FattureRighe]
           ([FkIdFattura]
           ,[NumeroLinea]
           ,[Testo]
           ,[CodiceMateriale]
           ,[Quantita]
           ,[PrezzoUnitario]
           ,[Imponibile]
           ,[RigaBollo]
           ,[PeriodoRiferimento])
     VALUES
           (@FkIdFattura
           ,@NumeroLinea
           ,@Testo
           ,@CodiceMateriale
           ,@Quantita
           ,@PrezzoUnitario
           ,@Imponibile
           ,@RigaBollo
           ,@PeriodoRiferimento);
";
    }

    public static string SQLSelectIdDatiFatturazione()
    {
        return $@"
SELECT [IdDatiFatturazione] 
  FROM [pfw].[DatiFatturazione]
  where FkIdEnte=@FkIdEnte;
";
    }

}
