using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

internal class GestioneFattureQueryBuilder
{
    private static string _sqlGestioneFattureList = @"
       
     SELECT [Ente]
      ,[RagioneSociale]
      ,[TipologiaFattura]
      ,[Anno]
      ,[Mese]
      ,[Azione]
      ,[DataInserimento]
      ,[DataRipristino]
      ,[Note]
      ,[TipoContratto] 
      ,[IdTipoContratto]
     FROM [be].[vwGestioneFattureGriglia]";

    public static string SelectGestioneFattureList()
    {
        return _sqlGestioneFattureList;
    }

    public static string OrderByGestioneFatture()
    {
        return " ORDER BY anno DESC, mese ";
    }

    private static string _sqlGestioneFattureCount = @"
     SELECT 
      count(*)
      FROM [be].[vwGestioneFattureGriglia]
     ";

    public static string SelectGestioneFattureCount()
    {
        return _sqlGestioneFattureCount;
    }

    private static string _offSet = " OFFSET (@page-1)*@size ROWS FETCH NEXT @size ROWS ONLY";
    public static string OffSet()
    {
        return _offSet;
    }


    public static string SelectGestioneFattureAnni()
    {
        return $@"
          SELECT Anno
            FROM [be].[vwGestioneFattureGriglia]
            GROUP BY Anno
            ORDER BY Anno DESC 
    ";
    }

    public static string SelectGestioneFattureMesi()
    {
        return $@"
            SELECT DISTINCT  mese 
            FROM [be].[vwGestioneFattureGriglia]
    ";
    }
    public static string OrderByGestioneFattureMesi()
    {
        return $@"
            ORDER BY mese DESC; 
    ";
    }


    private static string _sqlGestioneFattureTipologiaFattura = @"
     SELECT tipologiaFattura
FROM [be].[vwGestioneFattureGriglia]
    ";

    public static string SelectGestioneFattureTipologiaFattura()
    {
        return _sqlGestioneFattureTipologiaFattura;
    }

    public static string SelectGestioneFattureTipologiaFatturaGroupOrder()
    {
        return @"
            GROUP BY tipologiaFattura
            ORDER BY 
                CASE 
                    WHEN tipologiaFattura = 'ANTICIPO'        THEN 1
                    WHEN tipologiaFattura = 'ACCONTO'         THEN 2
                    WHEN tipologiaFattura = 'PRIMO SALDO'     THEN 3
                    WHEN tipologiaFattura = 'SECONDO SALDO'   THEN 4
                    WHEN tipologiaFattura = 'VAR. SEMESTRALE' THEN 5
                    ELSE 6
                END
        ";
    }


    public static string SelectGestioneFattureAllFilterPosticipa()
    {
        return @"
            SELECT
            [AnnoRiferimento]
            ,[MeseRiferimento]
            ,[FkIdEnte]
            ,[FkTipologiaFattura]
            ,[IdFattura]
            FROM [be].[vwGestioneFattureFormPosticipa]   
        ";
    }

    public static string SelectGestioneFattureAllFilterElimina()
    {
        return @"
            SELECT
            [AnnoRiferimento]
            ,[MeseRiferimento]
            ,[FkIdEnte]
            ,[FkTipologiaFattura]
            ,[IdFattura]
            FROM [be].[vwGestioneFattureFormElimina]
        ";
    }

    public static string SelectGestioneFattureAllFilterOrderAnno()
    {
        return @"
            ORDER BY AnnoRiferimento DESC; 
        ";
    }


    public static string SelectGestioneFattureModificaAnni()
    {
        return $@"
          SELECT DISTINCT Anno
            FROM [be].[vwGestioneFattureFormAnniMesi]
    ";
    }

    public static string SelectGestioneFattureModificaMesi()
    {
        return $@"
          SELECT DISTINCT Mese
            FROM [be].[vwGestioneFattureFormAnniMesi]
    ";
    }


    private static string _sqlGestioneFattureListDownload = @"
       
     SELECT [Ente]
      ,[RagioneSociale]
      ,[TipologiaFattura]
      ,[Anno]
      ,[Mese]
      ,[Azione]
      ,[DataInserimento]
      ,[DataRipristino]
      ,[TipoContratto] 
      ,[IdTipoContratto]
      ,[Note]
     FROM [be].[vwGestioneFattureDownload]";

    public static string SelectGestioneFattureListDownload()
    {
        return _sqlGestioneFattureListDownload;
    }


    private static string _sqlGestioneFattureCountDownload = @"
     SELECT 
      count(*)
      FROM [be].[vwGestioneFattureDownload]
     ";

    public static string SelectGestioneFattureCountDownload()
    {
        return _sqlGestioneFattureCountDownload;
    }



    public static string SelectGestioneFattureVerificaAzione()
    {
        return $@"
          SELECT DISTINCT Anno, Mese
           FROM[be].[vwGestioneFattureFormAnniMesi]
        ";
    }

    /// <summary>
    /// Query per la generazione del report di gestione fatture
    /// </summary>
    private static readonly string _sqlReport = @"
        SELECT [IdEnte]
            ,[Ragione Sociale] AS RagioneSociale
            ,[IdContratto]
            ,[TipologiaFattura]
            ,[NumeroFattura]
            ,[TipoDocumento]
            ,[Anno]
            ,[Mese]
            ,[TotaleNotificheAnalogiche]
            ,[TotaleNotificheDigitali]
            ,[TotaleNotifiche]
            ,[TotaleImponibileAnalogico]
            ,[TotaleImponibileDigitale]
            ,[TotaleImponibile]
            ,[TotaleIvatoAnalogico]
            ,[TotaleIvatoDigitale]
            ,[TotaleIvato]
            ,[Firmata]
            ,[TotaleFatturaImponibile]
            ,[TipoContratto]
            ,[Stato]
        FROM [be].[vwGestioneFattureReport]
        WHERE (@FilterByTipologia = 0 OR [TipologiaFattura] IN @TipologiaFattura)
    ";

    /// <summary>
    /// Query per la selezione del report di gestione fatture
    /// </summary>
    /// <returns>La query SQL per selezionare il report di gestione fatture</returns>
    public static string SelectReport() => _sqlReport;

}
