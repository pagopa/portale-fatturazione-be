namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence.Builder;

public class DatiModuloCommessaDateSQLBuilder
{
    private static string _selectCountDate = $@"
WITH 
DatiTotali AS (
    SELECT DISTINCT
        t.FkIdEnte,
        t.AnnoValidita,
        t.MeseValidita,
        ISNULL(t.DataModifica, t.DataCreazione) AS DataInserimento
    FROM [pfw].[DatiModuloCommessa] t  
    [whereIdenti]  
),
ContrattiUnivoci AS (
    SELECT 
        InternalIstitutionId,
        FkIdTipoContratto,
        createdat,
        ROW_NUMBER() OVER (PARTITION BY InternalIstitutionId ORDER BY createdat DESC) AS rn
    FROM [pfd].[Contratti]
)
SELECT COUNT(*) AS TotalCount
FROM DatiTotali dt
INNER JOIN [pfd].[Enti] e ON dt.FkIdEnte = e.InternalIstitutionId
INNER JOIN ContrattiUnivoci c ON e.InternalIstitutionId = c.InternalIstitutionId AND c.rn = 1
WHERE (@idTipoContratto IS NULL OR c.FkIdTipoContratto = @idTipoContratto)
AND (@dataInizioContratto IS NULL OR c.createdat >= @dataInizioContratto)
AND (@dataFineContratto IS NULL OR c.createdat < DATEADD(DAY, 1, CAST(@dataFineContratto AS DATE))) 
AND (@dataInizioModulo IS NULL 
     OR dt.AnnoValidita * 100 + dt.MeseValidita >= 
        YEAR(@dataInizioModulo) * 100 + MONTH(@dataInizioModulo))
AND (@dataFineModulo IS NULL 
     OR dt.AnnoValidita * 100 + dt.MeseValidita <= 
        YEAR(@dataFineModulo) * 100 + MONTH(@dataFineModulo))
 ";


    private static string _selectDate = $@"
WITH 
-- Costanti calcolate una sola volta con orario italiano
DateConstants AS (
    SELECT 
        YEAR(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time') AS CurrentYear,
        MONTH(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time') AS CurrentMonth,
        DAY(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time') AS CurrentDay,
        YEAR(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time') * 100 + 
        MONTH(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time') AS CurrentYearMonth,
        
        COALESCE(
            (SELECT 
                CASE 
                    WHEN CHARINDEX('-', frame) > 0 THEN
                        CAST(LEFT(frame, CHARINDEX('-', frame) - 1) AS INT)
                    ELSE 1
                END
             FROM [cfg].[framemodulocommessa] 
             WHERE anno = YEAR(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time') 
               AND mese = MONTH(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time')),
            1
        ) AS GiornoInizio,
        COALESCE(
            (SELECT 
                CASE 
                    WHEN CHARINDEX('-', frame) > 0 THEN
                        CAST(RIGHT(frame, LEN(frame) - CHARINDEX('-', frame)) AS INT)
                    ELSE 31
                END
             FROM [cfg].[framemodulocommessa] 
             WHERE anno = YEAR(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time') 
               AND mese = MONTH(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time')),
            19
        ) AS GiornoFine,
        
        COALESCE(
            (SELECT 
                CASE 
                    WHEN CHARINDEX('-', framelegale) > 0 THEN
                        CAST(RIGHT(framelegale, LEN(framelegale) - CHARINDEX('-', framelegale)) AS INT)
                    ELSE 31
                END
             FROM [cfg].[framemodulocommessa] 
             WHERE anno = YEAR(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time') 
               AND mese = MONTH(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time')),
            19
        ) AS GiornoFineLegale
),

Categorie AS ( 
    SELECT IdTipoSpedizione AS FkIdTipoSpedizione
    FROM [pfw].TipoSpedizione 
), 

ConfigFuture AS (
    SELECT DISTINCT 
        v.Year,
        v.Month,
        v.Source,
        v.Quarter,
        v.datavalidita,
        v.datavaliditalegale,
		v.fkidente
    FROM [pfd].[vConfigurazioneDatiModuloCommessa] v
    CROSS APPLY DateConstants dc
    WHERE v.annoRiferimento = dc.CurrentYear 
      AND v.meseRiferimento = dc.CurrentMonth 
),

DatiTotali AS (
    SELECT 
        t.FkIdEnte, 
        t.FkProdotto,
        t.AnnoValidita,
        t.MeseValidita,
        t.AnnoValidita * 100 + t.MeseValidita AS YearMonth,
        ISNULL(t.DataModifica, t.DataCreazione) AS DataInserimento,
        t.FkIdStato,
        t.FkIdTipoContratto,
        t.FkIdTipoSpedizione, 
        t.NumeroNotificheInternazionali,
        t.NumeroNotificheNazionali,
        t.ValoreInternazionali,
        t.ValoreNazionali
    FROM [pfw].[DatiModuloCommessa] t  
    [whereIdenti]  
),

QuarterLookup AS (
    SELECT 1 AS Month, 'Q1' AS QuarterSuffix UNION ALL
    SELECT 2, 'Q1' UNION ALL SELECT 3, 'Q1' UNION ALL
    SELECT 4, 'Q2' UNION ALL SELECT 5, 'Q2' UNION ALL SELECT 6, 'Q2' UNION ALL
    SELECT 7, 'Q3' UNION ALL SELECT 8, 'Q3' UNION ALL SELECT 9, 'Q3' UNION ALL
    SELECT 10, 'Q4' UNION ALL SELECT 11, 'Q4' UNION ALL SELECT 12, 'Q4'
),

ConfigEspansa AS (
    SELECT 
        cf.Year,
        cf.Month,
        cf.Year * 100 + cf.Month AS YearMonth,
        cf.Source,
        cf.Quarter,
        c.FkIdTipoSpedizione,
        cf.datavalidita,
        cf.datavaliditalegale
    FROM ConfigFuture cf
    CROSS JOIN Categorie c
    WHERE NOT EXISTS (
        SELECT 1 FROM DatiTotali dt 
        WHERE dt.AnnoValidita = cf.Year
          AND dt.MeseValidita = cf.Month
          AND dt.FkIdTipoSpedizione = c.FkIdTipoSpedizione
    )
),

ResultSet AS (
    SELECT
        dt.AnnoValidita,
        dt.MeseValidita,
        dt.FkIdTipoSpedizione,
        dt.NumeroNotificheInternazionali,
        dt.NumeroNotificheNazionali,
        dt.ValoreInternazionali,
        dt.ValoreNazionali,
        ISNULL(cf.Source, 'archiviato') AS Source,
        dt.FkIdEnte,
        dt.FkIdTipoContratto,
        dt.FkProdotto,
        dt.FkIdStato,
        dt.AnnoValidita AS Year,
        dt.MeseValidita AS Month,
        dt.DataInserimento,
        COALESCE(cf.datavalidita, 
            CASE 
                WHEN dt.MeseValidita = 1 THEN 
                    DATEFROMPARTS(dt.AnnoValidita - 1, 12, 19)
                ELSE 
                    DATEFROMPARTS(dt.AnnoValidita, dt.MeseValidita - 1, 19)
            END
        ) AS DataChiusura,
        COALESCE(cf.datavaliditalegale, 
            CASE 
                WHEN dt.MeseValidita = 1 THEN 
                    DATEFROMPARTS(dt.AnnoValidita - 1, 12, dc.GiornoFineLegale)
                ELSE 
                    DATEFROMPARTS(dt.AnnoValidita, dt.MeseValidita - 1, dc.GiornoFineLegale)
            END
        ) AS DataChiusuraLegale,
        COALESCE(cf.Quarter, CONCAT(dt.AnnoValidita, '-', ql.QuarterSuffix)) AS Quarter,
        CASE 
            WHEN ISNULL(cf.Source, 'archiviato') = 'archiviato' THEN CAST(0 AS BIT)
            WHEN dc.CurrentDay > dc.GiornoFine THEN CAST(0 AS BIT)
            ELSE CAST(1 AS BIT)
        END AS modifica,
        e.description AS RagioneSociale,
        t.Descrizione AS TipologiaContratto,
        c.createdat AS DataContratto,
        DENSE_RANK() OVER (ORDER BY dt.FkIdEnte, dt.AnnoValidita, dt.MeseValidita) AS EnteRowNum
    FROM DatiTotali dt
    CROSS APPLY DateConstants dc
    LEFT JOIN ConfigFuture cf ON dt.AnnoValidita = cf.Year AND dt.MeseValidita = cf.Month AND dt.FkIdEnte = cf.fkidente
    LEFT JOIN QuarterLookup ql ON dt.MeseValidita = ql.Month
    INNER JOIN [pfd].[Enti] e ON dt.FkIdEnte = e.InternalIstitutionId
    INNER JOIN [pfd].[Contratti] c ON e.InternalIstitutionId = c.InternalIstitutionId
    INNER JOIN [pfw].TipoContratto t ON t.IdTipoContratto = c.FkIdTipoContratto
    WHERE (@idTipoContratto IS NULL OR c.FkIdTipoContratto = @idTipoContratto)
    AND (@dataInizioContratto IS NULL OR c.createdat >= @dataInizioContratto)
    AND (@dataFineContratto IS NULL OR c.createdat < DATEADD(DAY, 1, CAST(@dataFineContratto AS DATE))) 
    AND (@dataInizioModulo IS NULL 
         OR dt.AnnoValidita * 100 + dt.MeseValidita >= 
            YEAR(@dataInizioModulo) * 100 + MONTH(@dataInizioModulo))
    AND (@dataFineModulo IS NULL 
         OR dt.AnnoValidita * 100 + dt.MeseValidita <= 
            YEAR(@dataFineModulo) * 100 + MONTH(@dataFineModulo))
)

SELECT 
    AnnoValidita,
    MeseValidita,
    FkIdTipoSpedizione,
    NumeroNotificheInternazionali,
    NumeroNotificheNazionali,
    ValoreInternazionali,
    ValoreNazionali,
    Source,
    FkIdEnte,
    FkIdTipoContratto,
    FkProdotto,
    FkIdStato,
    Year,
    Month,
    DataInserimento,
    DataChiusura,
    DataChiusuraLegale,
    Quarter,
    modifica,
    RagioneSociale,
    TipologiaContratto,
    DataContratto,
    (SELECT MAX(EnteRowNum) FROM ResultSet) AS TotalEnti,
    CEILING(CAST((SELECT MAX(EnteRowNum) FROM ResultSet) AS FLOAT) / @pageSize) AS TotalPages
FROM ResultSet
WHERE (@pageNumber IS NULL OR @pageSize IS NULL) 
   OR (EnteRowNum > (@pageNumber - 1) * @pageSize AND EnteRowNum <= @pageNumber * @pageSize)
ORDER BY Year, Month, FkIdEnte, FkIdTipoSpedizione
";

    public static string GetSelectDate()
    {
        return _selectDate;
    }
    public static string GetSelectCountDate()
    {
        return _selectCountDate;
    }
} 