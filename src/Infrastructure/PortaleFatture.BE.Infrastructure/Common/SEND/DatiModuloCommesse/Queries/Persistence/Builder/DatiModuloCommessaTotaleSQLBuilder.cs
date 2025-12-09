using Dapper;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence.Builder;

public class DatiModuloCommessaTotaleSQLBuilder
{
    private static string WhereByAnno()
    {
        DatiModuloCommessaTotale? obj;
        var fieldIdEnte = nameof(@obj.IdEnte).GetColumn<DatiModuloCommessaTotale>();
        var fieldAnno = nameof(@obj.AnnoValidita);
        var fieldProdotto = nameof(@obj.Prodotto).GetColumn<DatiModuloCommessaTotale>();
        return string.Join(" AND ",
            $"{fieldIdEnte} = @{nameof(@obj.IdEnte)}",
            $"{fieldAnno} = @{nameof(@obj.AnnoValidita)}",
            $"{fieldProdotto} = @{nameof(@obj.Prodotto)}");
    }
    private static string WhereById()
    {
        DatiModuloCommessaTotale? obj;
        var fieldIdEnte = nameof(@obj.IdEnte).GetColumn<DatiModuloCommessaTotale>();
        var fieldAnno = nameof(@obj.AnnoValidita);
        var fieldMese = nameof(@obj.MeseValidita);
        var fieldProdotto = nameof(@obj.Prodotto).GetColumn<DatiModuloCommessaTotale>();
        return string.Join(" AND ",
            $"{fieldIdEnte} = @{nameof(@obj.IdEnte)}",
            $"{fieldAnno} = @{nameof(@obj.AnnoValidita)}",
            $"{fieldMese} = @{nameof(@obj.MeseValidita)}",
            $"{fieldProdotto} = @{nameof(@obj.Prodotto)}");
    }

    private static SqlBuilder CreateSelect()
    {
        DatiModuloCommessaTotale? @obj = null;
        var builder = new SqlBuilder();
        builder.Select(nameof(@obj.IdTipoContratto).GetAsColumn<DatiModuloCommessaTotale>());
        builder.Select(nameof(@obj.Prodotto).GetAsColumn<DatiModuloCommessaTotale>());
        builder.Select(nameof(@obj.IdCategoriaSpedizione).GetAsColumn<DatiModuloCommessaTotale>());
        builder.Select(nameof(@obj.IdEnte).GetAsColumn<DatiModuloCommessaTotale>());
        builder.Select(nameof(@obj.Stato).GetAsColumn<DatiModuloCommessaTotale>());
        builder.Select(nameof(@obj.MeseValidita));
        builder.Select(nameof(@obj.AnnoValidita));
        builder.Select(nameof(@obj.TotaleCategoria));
        builder.Select(nameof(@obj.PercentualeCategoria));
        builder.Select(nameof(@obj.Totale));
        return builder;
    }

    public static string SelectBy()
    {
        var tableName = nameof(DatiModuloCommessaTotale);
        tableName = tableName.GetTable<DatiModuloCommessaTotale>();
        var builder = CreateSelect();
        var where = WhereById();
        builder.Where(where);
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName} /**where**/ ");
        return builderTemplate.RawSql;
    }

    public static string SelectByAnno()
    {
        var tableName = nameof(DatiModuloCommessaTotale);
        tableName = tableName.GetTable<DatiModuloCommessaTotale>();
        var builder = CreateSelect();
        var where = WhereByAnno();
        builder.Where(where);
        builder.OrderBy($"{OrderByMeseValidita()} DESC");
        var builderTemplate = builder.AddTemplate($"Select /**select**/ from [schema]{tableName} /**where**/ /**orderby**/");
        return builderTemplate.RawSql;
    }

    private static string _sqlPrevisionaleByAnnoAndMesePagoPA = $@" 
WITH 
-- Costanti calcolate una sola volta con orario italiano
DateConstants AS (
    SELECT 
        YEAR(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time') AS CurrentYear,
        MONTH(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time') AS CurrentMonth,
        DAY(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time') AS CurrentDay,
        YEAR(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time') * 100 + 
        MONTH(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time') AS CurrentYearMonth,
        
        -- Parametri per il periodo modificabile dal frame della configurazione (default 1-19)
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
        
        -- Parametri per framelegale (stessa logica di frame)
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

-- Tipi spedizione (semplificata)
Categorie AS ( 
    SELECT IdTipoSpedizione AS FkIdTipoSpedizione
    FROM [pfw].TipoSpedizione 
), 

-- Configurazione con filtri obbligatori @annofiltro e @mese
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
      AND v.Year = @annofiltro      -- OBBLIGATORIO
      AND v.Month = @mese           -- OBBLIGATORIO
),

-- Dati esistenti con filtri obbligatori e condizione per eliminare righe vuote
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
    WHERE t.FkProdotto = @prodotto   
      AND t.AnnoValidita = @annofiltro    -- OBBLIGATORIO
      AND t.MeseValidita = @mese          -- OBBLIGATORIO
      AND t.FkIdEnte IS NOT NULL          -- SCARTA I RECORD CON ENTE NULL
      [whereIdenti] 
      -- CONDIZIONE PER ELIMINARE RIGHE VUOTE: almeno un valore deve essere popolato
      AND (
          t.NumeroNotificheInternazionali IS NOT NULL 
          OR t.NumeroNotificheNazionali IS NOT NULL 
          OR t.ValoreInternazionali IS NOT NULL 
          OR t.ValoreNazionali IS NOT NULL
          OR t.FkIdTipoContratto IS NOT NULL
          OR t.FkIdStato IS NOT NULL
      ) 
),

-- Quarter lookup table per performance
QuarterLookup AS (
    SELECT 1 AS Month, 'Q1' AS QuarterSuffix UNION ALL
    SELECT 2, 'Q1' UNION ALL SELECT 3, 'Q1' UNION ALL
    SELECT 4, 'Q2' UNION ALL SELECT 5, 'Q2' UNION ALL SELECT 6, 'Q2' UNION ALL
    SELECT 7, 'Q3' UNION ALL SELECT 8, 'Q3' UNION ALL SELECT 9, 'Q3' UNION ALL
    SELECT 10, 'Q4' UNION ALL SELECT 11, 'Q4' UNION ALL SELECT 12, 'Q4'
),

-- Configurazione espansa per record mancanti dalla configurazione
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
)

-- OUTPUT FINALE OTTIMIZZATO
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
    -- DataChiusuraLegale usando framelegale
    COALESCE(cf.datavaliditalegale, 
        CASE 
            WHEN dt.MeseValidita = 1 THEN 
                DATEFROMPARTS(dt.AnnoValidita - 1, 12, dc.GiornoFineLegale)
            ELSE 
                DATEFROMPARTS(dt.AnnoValidita, dt.MeseValidita - 1, dc.GiornoFineLegale)
        END
    ) AS DataChiusuraLegale,
    COALESCE(cf.Quarter, CONCAT(dt.AnnoValidita, '-', ql.QuarterSuffix)) AS Quarter,
    -- LOGICA AGGIORNATA: se oggi è oltre il frame, tutti i record diventano non modificabili
    CASE 
        WHEN ISNULL(cf.Source, 'archiviato') = 'archiviato' THEN CAST(0 AS BIT)
        WHEN dc.CurrentDay > dc.GiornoFine THEN CAST(0 AS BIT)
        ELSE CAST(1 AS BIT)
    END AS modifica,
    e.description AS RagioneSociale,
	t.Descrizione AS TipologiaContratto

FROM DatiTotali dt
CROSS APPLY DateConstants dc
LEFT JOIN ConfigFuture cf ON dt.AnnoValidita = cf.Year AND dt.MeseValidita = cf.Month AND dt.FkIdEnte = cf.fkidente
LEFT JOIN QuarterLookup ql ON dt.MeseValidita = ql.Month
LEFT JOIN [pfd].[Enti] e ON dt.FkIdEnte = e.InternalIstitutionId
INNER JOIN [pfd].[Contratti] c ON e.InternalIstitutionId = c.InternalIstitutionId
INNER JOIN [pfw].TipoContratto t ON t.IdTipoContratto = c.FkIdTipoContratto
WHERE (@idTipoContratto IS NULL OR c.FkIdTipoContratto = @idTipoContratto)

ORDER BY Year, Month, FkIdEnte, FkIdTipoSpedizione
";

    private static string _sqlPrevisionaleByAnnoAndIdEnte = $@"
WITH 
-- Costanti calcolate una sola volta con orario italiano
DateConstants AS (
    SELECT 
        CAST(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time' AS DATE) AS CurrentDate,
        YEAR(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time') AS CurrentYear,
        MONTH(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time') AS CurrentMonth,
        DAY(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time') AS CurrentDay,
        YEAR(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time') * 100 + 
        MONTH(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time') AS CurrentYearMonth,
        
        -- Calcola l'inizio del trimestre del primo mese configurato
        (SELECT TOP 1 
            v2.Year * 100 + 
            CASE 
                WHEN v2.Month BETWEEN 1 AND 3 THEN 1    -- Q1: inizia da Gennaio
                WHEN v2.Month BETWEEN 4 AND 6 THEN 4    -- Q2: inizia da Aprile  
                WHEN v2.Month BETWEEN 7 AND 9 THEN 7    -- Q3: inizia da Luglio
                WHEN v2.Month BETWEEN 10 AND 12 THEN 10 -- Q4: inizia da Ottobre
            END
         FROM [pfd].[vConfigurazioneDatiModuloCommessa] v2
         WHERE v2.annoRiferimento = YEAR(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time') 
           AND v2.meseRiferimento = MONTH(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time')
		   AND v2.FkIdEnte = @idente
         ORDER BY v2.Year, v2.Month
        ) AS MinConfigYearMonth,
           
        -- Parametro per framelegale (per DataChiusuraLegale)
        COALESCE(
            (SELECT 
                CASE 
                    WHEN CHARINDEX('-', framelegale) > 0 THEN
                        CAST(RIGHT(framelegale, LEN(framelegale) - CHARINDEX('-', framelegale)) AS INT)
                    ELSE 31
                END
             FROM [cfg].[FrameModuloCommessa]
             WHERE anno = YEAR(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time') 
               AND mese = MONTH(GETDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'Central European Standard Time')),
            19
        ) AS GiornoFineLegale
),

-- Tipo contratto dell'ente
TipoContrattoEnte AS (
    SELECT TOP 1 c.FkIdTipoContratto
    FROM [pfd].[Contratti] c
    WHERE c.internalistitutionid = @idente 
),

-- Tipi spedizione
Categorie AS ( 
    SELECT IdTipoSpedizione AS FkIdTipoSpedizione
    FROM [pfw].TipoSpedizione 
), 

-- Configurazione con filtro dinamico basato su @annoFiltro
ConfigFuture AS (
    SELECT DISTINCT 
        v.Year,
        v.Month,
        v.Source,
        v.Quarter,
        v.datavalidita,
        v.datavaliditalegale
    FROM [pfd].[vConfigurazioneDatiModuloCommessa] v
    CROSS APPLY DateConstants dc
    WHERE v.annoRiferimento = dc.CurrentYear 
      AND v.meseRiferimento = dc.CurrentMonth
	  AND v.fkidente =@idente
      AND (
          -- Se @annoFiltro è specificato: filtra per quell'anno
          (@annofiltro IS NOT NULL AND v.Year = @annofiltro)
          OR
          -- Se @annoFiltro è NULL: mostra dal primo mese DEL TRIMESTRE della configurazione in poi
          (@annofiltro IS NULL AND (v.Year * 100 + v.Month) >= dc.MinConfigYearMonth)
      )
      AND (
          -- Filtro per @mese opzionale
          (@mese IS NOT NULL AND v.Month = @mese)
          OR
          (@mese IS NULL)
      )
),

-- Dati esistenti con filtro dinamico per annoValidita
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
    CROSS APPLY DateConstants dc
    WHERE t.FkIdEnte = @idente 
      AND t.FkProdotto = @prodotto   
      AND (
          -- Se @annoFiltro è specificato: filtra per quell'anno
          (@annofiltro IS NOT NULL AND t.AnnoValidita = @annofiltro)
          OR
          -- Se @annoFiltro è NULL: dal primo mese DEL TRIMESTRE della configurazione in poi
          (@annofiltro IS NULL AND (t.AnnoValidita * 100 + t.MeseValidita) >= dc.MinConfigYearMonth)
      )
      AND (
          -- Filtro per @mese opzionale
          (@mese IS NOT NULL AND t.MeseValidita = @mese)
          OR
          (@mese IS NULL)
      )
),

-- Quarter lookup table per performance
QuarterLookup AS (
    SELECT 1 AS Month, 'Q1' AS QuarterSuffix UNION ALL
    SELECT 2, 'Q1' UNION ALL SELECT 3, 'Q1' UNION ALL
    SELECT 4, 'Q2' UNION ALL SELECT 5, 'Q2' UNION ALL SELECT 6, 'Q2' UNION ALL
    SELECT 7, 'Q3' UNION ALL SELECT 8, 'Q3' UNION ALL SELECT 9, 'Q3' UNION ALL
    SELECT 10, 'Q4' UNION ALL SELECT 11, 'Q4' UNION ALL SELECT 12, 'Q4'
),

-- Generatore di mesi per completare i trimestri
MesiTrimestreCompleto AS (
    SELECT DISTINCT
        dc.CurrentYear AS Year,
        m.Month,
        dc.CurrentYear * 100 + m.Month AS YearMonth,
        'archiviato' AS Source,
        CONCAT(dc.CurrentYear, '-', ql.QuarterSuffix) AS Quarter,
        c.FkIdTipoSpedizione,
        CASE 
            WHEN m.Month = 1 THEN DATEFROMPARTS(dc.CurrentYear - 1, 12, 19)
            ELSE DATEFROMPARTS(dc.CurrentYear, m.Month - 1, 19)
        END AS datavalidita,
        CASE 
            WHEN m.Month = 1 THEN DATEFROMPARTS(dc.CurrentYear - 1, 12, dc.GiornoFineLegale)
            ELSE DATEFROMPARTS(dc.CurrentYear, m.Month - 1, dc.GiornoFineLegale)
        END AS datavaliditalegale
    FROM DateConstants dc
    CROSS JOIN QuarterLookup ql
    CROSS JOIN (SELECT 1 AS Month UNION SELECT 2 UNION SELECT 3 UNION SELECT 4 UNION 
                SELECT 5 UNION SELECT 6 UNION SELECT 7 UNION SELECT 8 UNION SELECT 9 UNION 
                SELECT 10 UNION SELECT 11 UNION SELECT 12) m
    CROSS JOIN Categorie c
    WHERE @annofiltro IS NULL 
      AND (dc.CurrentYear * 100 + m.Month) >= dc.MinConfigYearMonth
      AND ql.Month = m.Month
      AND (
          -- Filtro per @mese opzionale
          (@mese IS NOT NULL AND m.Month = @mese)
          OR
          (@mese IS NULL)
      )
      -- Escludi mesi che già esistono in DatiTotali o ConfigFuture
      AND NOT EXISTS (
          SELECT 1 FROM DatiTotali dt 
          WHERE dt.AnnoValidita = dc.CurrentYear
            AND dt.MeseValidita = m.Month
            AND dt.FkIdTipoSpedizione = c.FkIdTipoSpedizione
      )
      AND NOT EXISTS (
          SELECT 1 FROM ConfigFuture cf
          WHERE cf.Year = dc.CurrentYear
            AND cf.Month = m.Month
      )
),

-- Generatore di tutti i mesi per l'anno specificato
AllMonths AS (
    SELECT Month, QuarterSuffix
    FROM QuarterLookup
),

-- Configurazione espansa per record mancanti dalla configurazione
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

-- Mesi mancanti SOLO quando anno è specificato
MesiMancanti AS (
    SELECT 
        @annofiltro AS Year,
        am.Month,
        @annofiltro * 100 + am.Month AS YearMonth,
        'archiviato' AS Source,
        CONCAT(@annofiltro, '-', am.QuarterSuffix) AS Quarter,
        c.FkIdTipoSpedizione,
        CASE 
            WHEN am.Month = 1 THEN 
                DATEFROMPARTS(@annofiltro - 1, 12, 19)
            ELSE 
                DATEFROMPARTS(@annofiltro, am.Month - 1, 19)
        END AS datavalidita,
        CASE 
            WHEN am.Month = 1 THEN 
                DATEFROMPARTS(@annofiltro - 1, 12, (SELECT GiornoFineLegale FROM DateConstants))
            ELSE 
                DATEFROMPARTS(@annofiltro, am.Month - 1, (SELECT GiornoFineLegale FROM DateConstants))
        END AS datavaliditalegale
    FROM AllMonths am
    CROSS JOIN Categorie c
    CROSS APPLY DateConstants dc
    WHERE @annofiltro IS NOT NULL
      AND NOT EXISTS (
        SELECT 1 FROM DatiTotali dt 
        WHERE dt.AnnoValidita = @annofiltro
          AND dt.MeseValidita = am.Month
          AND dt.FkIdTipoSpedizione = c.FkIdTipoSpedizione
    )
    AND NOT EXISTS (
        SELECT 1 FROM ConfigFuture cf
        WHERE cf.Year = @annofiltro
          AND cf.Month = am.Month
    )
    AND (
        -- Filtro per @mese opzionale
        (@mese IS NOT NULL AND am.Month = @mese)
        OR
        (@mese IS NULL)
    )
)

-- ============================================================================
-- OUTPUT FINALE CON LOGICA MODIFICABILITÀ CORRETTA
-- ============================================================================
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
    -- DataChiusura: la data REALE di scadenza (19 del mese precedente)
    COALESCE(cf.datavalidita, 
        CASE 
            WHEN dt.MeseValidita = 1 THEN 
                DATEFROMPARTS(dt.AnnoValidita - 1, 12, 19)
            ELSE 
                DATEFROMPARTS(dt.AnnoValidita, dt.MeseValidita - 1, 19)
        END
    ) AS DataChiusura,
    -- DataChiusuraLegale: riferimento legale (framelegale del mese precedente)
    COALESCE(cf.datavaliditalegale, 
        CASE 
            WHEN dt.MeseValidita = 1 THEN 
                DATEFROMPARTS(dt.AnnoValidita - 1, 12, dc.GiornoFineLegale)
            ELSE 
                DATEFROMPARTS(dt.AnnoValidita, dt.MeseValidita - 1, dc.GiornoFineLegale)
        END
    ) AS DataChiusuraLegale,
    COALESCE(cf.Quarter, CONCAT(dt.AnnoValidita, '-', ql.QuarterSuffix)) AS Quarter,
    -- LOGICA CORRETTA: confronta data corrente con DataChiusura specifica del record
    CASE 
        WHEN ISNULL(cf.Source, 'archiviato') = 'archiviato' THEN CAST(0 AS BIT)
        WHEN dc.CurrentDate > COALESCE(cf.datavalidita, 
            CASE 
                WHEN dt.MeseValidita = 1 THEN 
                    DATEFROMPARTS(dt.AnnoValidita - 1, 12, 19)
                ELSE 
                    DATEFROMPARTS(dt.AnnoValidita, dt.MeseValidita - 1, 19)
            END
        ) THEN CAST(0 AS BIT)
        ELSE CAST(1 AS BIT)
    END AS modifica

FROM DatiTotali dt
CROSS APPLY DateConstants dc
LEFT JOIN ConfigFuture cf ON dt.AnnoValidita = cf.Year AND dt.MeseValidita = cf.Month
LEFT JOIN QuarterLookup ql ON dt.MeseValidita = ql.Month

UNION ALL

-- ConfigEspansa: record dalla configurazione senza dati
SELECT
    ce.Year AS AnnoValidita,
    ce.Month AS MeseValidita,
    ce.FkIdTipoSpedizione,
    NULL AS NumeroNotificheInternazionali,
    NULL AS NumeroNotificheNazionali,
    NULL AS ValoreInternazionali,
    NULL AS ValoreNazionali,
    ce.Source,
    @idente AS FkIdEnte,
    (SELECT FkIdTipoContratto FROM TipoContrattoEnte) AS FkIdTipoContratto,
    @prodotto AS FkProdotto,
    NULL AS FkIdStato,
    ce.Year,
    ce.Month,
    NULL AS DataInserimento,
    ce.datavalidita AS DataChiusura,
    ce.datavaliditalegale AS DataChiusuraLegale,
    ce.Quarter,
    -- LOGICA CORRETTA: confronta data corrente con DataChiusura
    CASE 
        WHEN ce.Source = 'archiviato' THEN CAST(0 AS BIT)
        WHEN dc.CurrentDate > ce.datavalidita THEN CAST(0 AS BIT)
        ELSE CAST(1 AS BIT)
    END AS modifica

FROM ConfigEspansa ce
CROSS APPLY DateConstants dc

UNION ALL

-- MesiMancanti: mesi vuoti quando si filtra per anno specifico
SELECT
    mm.Year AS AnnoValidita,
    mm.Month AS MeseValidita,
    mm.FkIdTipoSpedizione,
    NULL AS NumeroNotificheInternazionali,
    NULL AS NumeroNotificheNazionali,
    NULL AS ValoreInternazionali,
    NULL AS ValoreNazionali,
    mm.Source,
    @idente AS FkIdEnte,
    (SELECT FkIdTipoContratto FROM TipoContrattoEnte) AS FkIdTipoContratto,
    @prodotto AS FkProdotto,
    NULL AS FkIdStato,
    mm.Year,
    mm.Month,
    NULL AS DataInserimento,
    mm.datavalidita AS DataChiusura,
    mm.datavaliditalegale AS DataChiusuraLegale,
    mm.Quarter,
    -- Archiviato: sempre non modificabile
    CAST(0 AS BIT) AS modifica

FROM MesiMancanti mm
CROSS APPLY DateConstants dc

UNION ALL

-- MesiTrimestreCompleto: completa i mesi mancanti del trimestre
SELECT
    mtc.Year AS AnnoValidita,
    mtc.Month AS MeseValidita,
    mtc.FkIdTipoSpedizione,
    NULL AS NumeroNotificheInternazionali,
    NULL AS NumeroNotificheNazionali,
    NULL AS ValoreInternazionali,
    NULL AS ValoreNazionali,
    mtc.Source,
    @idente AS FkIdEnte,
    (SELECT FkIdTipoContratto FROM TipoContrattoEnte) AS FkIdTipoContratto,
    @prodotto AS FkProdotto,
    NULL AS FkIdStato,
    mtc.Year,
    mtc.Month,
    NULL AS DataInserimento,
    mtc.datavalidita AS DataChiusura,
    mtc.datavaliditalegale AS DataChiusuraLegale,
    mtc.Quarter,
    -- Archiviato: sempre non modificabile
    CAST(0 AS BIT) AS modifica

FROM MesiTrimestreCompleto mtc
CROSS APPLY DateConstants dc

ORDER BY Year, Month, FkIdTipoSpedizione;
";
    public static string SelectPrevisionaleByAnnoAndIdEnte()
    {
        return _sqlPrevisionaleByAnnoAndIdEnte;
    }

    public static string SelectPrevisionaleByAnnoAndMesePagoPA()
    {
        return _sqlPrevisionaleByAnnoAndMesePagoPA;
    }
    private static string OrderByMeseValidita()
    {
        DatiModuloCommessaTotale? obj;
        var fieldMeseValidita = nameof(@obj.MeseValidita);
        return $"{fieldMeseValidita}";
    }


    private static string _selectValoriRegioni = $@" 
SELECT DISTINCT *
FROM (
    SELECT 
        da.Internalistitutionid,
        ISNULL(dr.Anno, @anno) as anno,
        ISNULL(dr.Mese, @mese) as mese,
        p.Provincia,
        r.Regione,
        p.CodiceIstat AS istatProvincia,
        r.CodiceIstat AS istatRegione,
        dr.[AR],
        dr.[890] as A890,
        0 as isRegione,
        ISNULL(dr.Calcolato, 0) as Calcolato,
        1 as Obbligatorio
    FROM pfd.vDatiModuloCommessaAderenti da
    LEFT JOIN pfw.Regioni r
        ON r.CodiceIstat = da.Regione
    LEFT JOIN pfw.Province p
        ON p.CodiceIstatRegione = da.Provincia
    LEFT JOIN pfw.DatiModuloCommessaRegioni dr
        ON dr.Internalistitutionid = da.Internalistitutionid
        AND dr.Anno = @anno
        AND dr.Mese = @mese
        AND dr.Provincia = da.Provincia
        AND dr.Regione = da.Regione
    WHERE da.Internalistitutionid = @idente 
    AND p.Provincia IS NULL
    AND NOT EXISTS (
        SELECT 1 
        FROM pfw.DatiModuloCommessaRegioni dr_check
        WHERE dr_check.Internalistitutionid = da.Internalistitutionid
        AND dr_check.Anno = @anno
        AND dr_check.Mese = @mese
        AND dr_check.Regione = da.Regione
        AND dr_check.Provincia IS NULL
    ) 
    UNION ALL
    
    SELECT 
        dmcr.Internalistitutionid,
        dmcr.Anno,
        dmcr.Mese,
        NULL AS Provincia,
        r.Regione,
        dmcr.Provincia AS istatProvincia,
        dmcr.Regione AS istatRegione,
        dmcr.[AR],
        dmcr.[890],
        1 as isRegione,
        ISNULL(dmcr.Calcolato, 0) as Calcolato,
        CASE 
            WHEN EXISTS (
                SELECT 1 FROM pfd.vDatiModuloCommessaAderenti da2 
                WHERE da2.Internalistitutionid = dmcr.Internalistitutionid 
                AND da2.Regione = dmcr.Regione
            ) THEN 1 
            ELSE 0 
        END as Obbligatorio
    FROM pfw.DatiModuloCommessaRegioni dmcr
    LEFT JOIN pfw.Regioni r ON r.CodiceIstat = dmcr.Regione
    WHERE dmcr.Internalistitutionid = @idente
    AND dmcr.Anno = @anno
    AND dmcr.Mese = @mese
    AND dmcr.Provincia IS NULL
    
) AS unione
WHERE Provincia IS NULL;
";

    public static string SelectValoriRegioni()
    {
        return _selectValoriRegioni;
    } 
}