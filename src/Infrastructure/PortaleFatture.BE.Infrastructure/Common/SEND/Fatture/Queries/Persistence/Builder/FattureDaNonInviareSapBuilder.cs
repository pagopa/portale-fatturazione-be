using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

internal class FattureDaNonInviareSapBuilder
{
    private static string _sqlEsclusioneFattureList = @"
       
     SELECT 
    CONCAT(w.FkIdEnte, w.Anno, w.Mese, w.Stato) AS Id,
    description AS RagioneSociale,
    w.FkIdEnte AS IdEnte,
    Anno,
    Mese,
    [DataInserimento],
    [DataRipristino],
    w.[FkTipologiaFattura] AS TipologiaFattura,
    c.FkIdTipoContratto AS IdTipoContratto,
    tc.Descrizione AS TipoContratto,
    w.Stato
        FROM [cfg].[FattureStaging] w
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

    public static string SelectEsclusioneFattureList()
    {
        return _sqlEsclusioneFattureList;
    }

    public static string OrderByEsclusioneFatture()
    {
        return " ORDER BY anno DESC, mese ";
    }

    private static string _sqlEsclusioneFattureCount = @"
     SELECT 
      count(*)
      FROM [cfg].[FattureStaging] w
      inner join pfd.Enti e
      on e.InternalIstitutionId =  w.FkIdEnte
      inner join pfd.Contratti c
      on c.internalistitutionid = e.InternalIstitutionId
      inner join pfw.TipoContratto tc
      on tc.IdTipoContratto = c.FkIdTipoContratto
     ";

    public static string SelectEsclusioneFattureCount()
    {
        return _sqlEsclusioneFattureCount;
    }

    private static string _offSet = " OFFSET (@page-1)*@size ROWS FETCH NEXT @size ROWS ONLY";
    public static string OffSet()
    {
        return _offSet;
    }


    public static string SelectEsclusioneFattureAnni()
    {
        return $@"
          SELECT Anno
            FROM [cfg].[FattureStaging]
            GROUP BY Anno
            HAVING COUNT(CASE WHEN Stato <> 2 THEN 1 END) >= 1
          ORDER BY Anno DESC 
    ";
    }

    public static string SelectEsclusioneFattureMesi() 
    {
        return $@"
            SELECT DISTINCT  mese 
            FROM [cfg].[FattureStaging]  
    ";
    }
    public static string OrderByEsclusioneFattureMesi()
    {
        return $@"
            ORDER BY mese DESC; 
    ";
    }


    private static string _sqlEsclusioneFattureTipologiaFattura = @"
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
        FROM [cfg].[FattureStaging]
        GROUP BY FkTipologiaFattura
        HAVING COUNT(CASE WHEN Stato <> 2 THEN 1 END) >= 1
        ORDER BY ordine
    ";

    public static string SelectEsclusioneFattureTipologiaFattura()
    {
        return _sqlEsclusioneFattureTipologiaFattura;
    }
}
