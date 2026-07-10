using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

 internal class GestioneFattureBuilder
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


    public static string SelectGestioneFattureAnniInserisci()
    {
        return $@"

WITH Months AS (
    SELECT 1 AS Mese
    UNION ALL
    SELECT Mese + 1 FROM Months WHERE Mese < 12
),
ExistingData AS ( 
    -- Combine data from FattureTestata and FattureStaging
    SELECT DISTINCT
        ft.annoriferimento AS anno,
        ft.meseriferimento AS mese
    FROM [pfd].[FattureTestata] ft
    WHERE ft.FkTipologiaFattura = @TipologiaFattura
    AND ft.annoriferimento <= @anno

    UNION

    SELECT DISTINCT
        fwl.Anno AS anno,
        fwl.Mese AS mese
    FROM [cfg].[GestioneFatture] fwl
    WHERE fwl.FkTipologiaFattura = @TipologiaFattura
    AND fwl.Anno <= @anno  
    AND fwl.FkIdEnte = @IdEnte  
    AND fwl.Stato <> 0   
)

-- Select missing months for the given years (previous and current)
SELECT 
    m.AnnoRiferimento,
    m.MeseRiferimento,
    @TipologiaFattura AS TipologiaFattura
FROM (
    -- Generate months for previous year and current year
    SELECT @anno - 1 AS AnnoRiferimento, Mese AS MeseRiferimento
    FROM Months
    UNION ALL
    SELECT @anno AS AnnoRiferimento, Mese AS MeseRiferimento
    FROM Months
    UNION ALL
    SELECT @anno + 1 AS AnnoRiferimento, Mese AS MeseRiferimento
    FROM Months 
) AS m
LEFT JOIN ExistingData e 
    ON m.AnnoRiferimento = e.anno AND m.MeseRiferimento = e.mese
WHERE m.AnnoRiferimento = @anno 
--WHERE e.mese IS NULL  -- Exclude months that already exist in both tables
ORDER BY AnnoRiferimento DESC, MeseRiferimento
OPTION (MAXRECURSION 12);
    ";
    }
}

