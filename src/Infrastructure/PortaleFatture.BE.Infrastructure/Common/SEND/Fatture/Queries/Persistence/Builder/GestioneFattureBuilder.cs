using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

 internal class GestioneFattureBuilder
{
    private static string _sqlGestioneFattureList = @"
       
     SELECT 
    CONCAT(w.FkIdEnte, w.Anno, w.Mese, w.Stato) AS Id,
    description AS RagioneSociale,
    w.FkIdEnte AS IdEnte,
    Anno,
    Mese,
    [DataInserimento],
    [DataRipristino],
    [DataCancellazione],
    w.[FkTipologiaFattura] AS TipologiaFattura,
    c.FkIdTipoContratto AS IdTipoContratto,
    tc.Descrizione AS TipoContratto,
    w.Stato,
    Note,
    Azione
        FROM [cfg].[GestioneFatture] w
    INNER JOIN pfd.Enti e ON e.InternalIstitutionId = w.FkIdEnte
    INNER JOIN pfd.Contratti c ON c.internalistitutionid = e.InternalIstitutionId
    INNER JOIN pfw.TipoContratto tc ON tc.IdTipoContratto = c.FkIdTipoContratto
        ";
    /*
     * LEFT JOIN [pfd].[FattureTestata] ft 
        ON ft.FkTipologiaFattura = w.FkTipologiaFattura
        AND ft.AnnoRiferimento = w.Anno
        AND ft.MeseRiferimento = w.Mese
        AND ft.FkIdEnte = w.FkIdEnte
     */
  

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
      FROM [cfg].[GestioneFatture] w
      inner join pfd.Enti e
      on e.InternalIstitutionId =  w.FkIdEnte
      inner join pfd.Contratti c
      on c.internalistitutionid = e.InternalIstitutionId
      inner join pfw.TipoContratto tc
      on tc.IdTipoContratto = c.FkIdTipoContratto
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
            FROM [cfg].[GestioneFatture]
            GROUP BY Anno
            HAVING COUNT(CASE WHEN Stato <> 2 THEN 1 END) >= 1
          ORDER BY Anno DESC 
    ";
    }

    public static string SelectGestioneFattureMesi()
    {
        return $@"
            SELECT DISTINCT  mese 
            FROM [cfg].[GestioneFatture] 
    ";
    }
    public static string OrderByGestioneFattureMesi()
    {
        return $@"
            ORDER BY mese DESC; 
    ";
    }


    private static string _sqlGestioneFattureTipologiaFattura = @"
    SELECT
    FkTipologiaFattura,
    CASE
        WHEN FkTipologiaFattura = 'ANTICIPO'        THEN 1
        WHEN FkTipologiaFattura = 'ACCONTO'         THEN 2
        WHEN FkTipologiaFattura = 'PRIMO SALDO'     THEN 3
        WHEN FkTipologiaFattura = 'SECONDO SALDO'   THEN 4
        WHEN FkTipologiaFattura = 'VAR. SEMESTRALE' THEN 5
        ELSE 6
    END AS ordine
        FROM [cfg].[GestioneFatture]
        GROUP BY FkTipologiaFattura
        HAVING COUNT(CASE WHEN Stato <> 2 THEN 1 END) >= 1
        ORDER BY ordine
    ";

    public static string SelectGestioneFattureTipologiaFattura()
    {
        return _sqlGestioneFattureTipologiaFattura;
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

