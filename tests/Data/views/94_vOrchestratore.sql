-- Vista legacy pfd.v*. DDL reale estratta da PRODUZIONE il 31/08/2026 con OBJECT_DEFINITION,
-- riprodotta senza modifiche semantiche: cambiati solo CREATE -> CREATE OR ALTER (per l'hot-apply)
-- e l'indentazione, che OBJECT_DEFINITION restituisce collassata su una riga.
--
-- ⚠️ EPISODIO DA RICORDARE (31/08/2026): la vista risultava presente in PRODUZIONE ma ASSENTE IN UAT,
--    dove quindi 4 delle 5 rotte dell'area rispondevano 500 e la pagina Processi/Orchestratore era di
--    fatto rotta. Segnalato al team Data e ALLINEATO in giornata: oggi c'e' in DEV, UAT e PROD.
--    Si tiene nota perche' la causa resta strutturale e puo' ripetersi su qualunque oggetto DB: non
--    esiste una pipeline automatica di migrazione fra ambienti, l'allineamento e' manuale via schema
--    compare (v. docs/cicd-release.md). Un 500 su un'area che in locale funziona merita quindi come
--    primo controllo "l'oggetto esiste su quell'ambiente?", prima di guardare il codice.
--
-- Letta da OrchestratoreSQLBuilder (SelectAll, SelectAllCount, SelectTipologie, SelectFasi) ->
-- 3 persistence -> POST api/orchestratore, POST api/orchestratore/download,
-- GET api/orchestratore/tipologie, GET api/orchestratore/fasi. Nessuna Azure Function la usa.
-- GET api/orchestratore/stati e' l'unica rotta dell'area che non tocca il DB.
--
-- Tre cose da sapere prima di scriverci dei test:
--
--   1) IL CAST E' CIO' CHE TIENE IN PIEDI IL RAMO "SENZA DATE". DataEsecuzione e DataFatturazione
--      sono CAST(... AS DATE) qui nel SELECT esterno, quindi sono `date`; DataFineContestazioni,
--      ChiusuraContestazioni e TempoRisposta NO, restano `datetime` (arrivano da DataFine/
--      DataVerifica di pfw.ContestazioniCalendario). Quando non si passano ne' init ne' end,
--      OrchestratoreByDateQueryPersistence filtra con
--          WHERE ISNULL(DataEsecuzione, DataFineContestazioni) >= '0001-01-01'
--      e ISNULL restituisce il tipo del PRIMO argomento: il confronto avviene quindi in `date`,
--      dominio che parte dall'anno 1 e accetta quel letterale. Togliere il CAST "per pulizia"
--      farebbe diventare `datetime` il confronto (dominio dal 1753) e quella chiamata un 500.
--
--   2) Esecuzione NON PUO' ESSERE NULL: tutti e otto i rami la calcolano con un CASE che ha sempre
--      un ELSE costante. E' il motivo per cui OrchestratoreItem.DescrizioneEsecuzione, che fa
--      Esecuzione!.Value, oggi non esplode - il difetto resta nel C# ma da questa vista non e'
--      raggiungibile. Count invece e' nullable (RelCount di una LEFT JOIN, oppure NULL letterale).
--
--   3) I DUE RAMI "IMPORT DATI" NON LEGGONO RIGHE, LE GENERANO: CROSS JOIN dei 12 mesi su ogni anno
--      presente in pfd.NotificheCount (anni passati) piu' l'anno corrente troncato a
--      MONTH(GETDATE())+1. Il numero di righe della vista cambia quindi a ogni cambio di mese anche
--      a seed fermo -> mai asserire su un conteggio totale. Il loro Count e' inoltre sfasato di un
--      mese di proposito (n.month_n = m - 1, con gennaio che pesca da dicembre dell'anno prima).
--
-- ❓ Da chiarire col team Data: nel ramo "SECONDO SALDO / FINE CONT." DataFineContestazioni viene da
--    DataVerifica, ma ChiusuraContestazioni e TempoRisposta sono calcolate su DataFine (+30/+45),
--    non su DataVerifica - mentre il ramo gemello del primo saldo usa DataFine per tutte e tre.
--    Ha l'aria del copia-incolla; se e' voluto va documentato.
CREATE OR ALTER VIEW [pfd].[vOrchestratore]
AS
SELECT
    Anno,
    Mese,
    Tipologia,
    Fase,
    DataFineContestazioni,
    ChiusuraContestazioni,
    TempoRisposta,
    CAST(DataEsecuzione AS DATE) as DataEsecuzione,
    CAST(DataFatturazione AS DATE) as DataFatturazione,
    Esecuzione,
    Count
FROM
(
    -- PRIMO SALDO REL Section
    SELECT
        [AnnoContestazione] as Anno,
        [MeseContestazione] as Mese,
        'PRIMO SALDO' AS Tipologia,
        'REL' as Fase,
        DataFine AS DataFineContestazioni,
        NULL AS ChiusuraContestazioni,
        NULL AS TempoRisposta,
        DataCalcoloPrimoSecondo AS DataEsecuzione,
        NULL as DataFatturazione,
        CASE
            WHEN ISNULL(ct.RelCount, 0) = 0 AND CAST(GETDATE() AS DATE) >= CAST(DATEADD(DAY, 1, DataFine) AS DATE) THEN 2
            WHEN ISNULL(ct.RelCount, 0) > 0 THEN 1
            ELSE 0
        END AS Esecuzione,
        ct.RelCount AS Count
    FROM
        [pfw].[ContestazioniCalendario] cc
    LEFT JOIN (
        SELECT
            month,
            year,
            TipologiaFattura,
            COUNT(*) AS RelCount
        FROM
            pfd.RelTestata
        GROUP BY
            month,
            year,
            TipologiaFattura
    ) ct
        ON cc.[MeseContestazione] = ct.month
        AND cc.[AnnoContestazione] = ct.year
        AND 'PRIMO SALDO' = ct.TipologiaFattura

    UNION ALL

    -- Variazione Semestrale REL Section
    SELECT
        c.AnnoRel as Anno,
        c.MeseRel as Mese,
        c.Tipologia as Tipologia,
        'REL' as Fase,
        NULL AS DataFineContestazioni,
        NULL AS ChiusuraContestazioni,
        NULL AS TempoRisposta,
        CAST(c.DataEsecuzione AS DATE) AS DataEsecuzione,
        NULL as DataFatturazione,
        CASE
            WHEN ISNULL(ct.RelCount, 0) = 0 AND CAST(GETDATE() AS DATE) >= c.DataEsecuzione THEN 2
            WHEN ISNULL(ct.RelCount, 0) > 0 THEN 1
            ELSE 0
        END AS Esecuzione,
        ct.RelCount AS Count
    FROM
        [cfg].[CalendarioVarSemestrale] c
    LEFT JOIN (
        SELECT
            month,
            year,
            TipologiaFattura,
            COUNT(*) AS RelCount
        FROM
            pfd.RelTestata
        GROUP BY
            month,
            year,
            TipologiaFattura
    ) ct
        ON c.MeseRel = ct.month
        AND c.AnnoRel = ct.year
        AND 'var. semestrale' = ct.TipologiaFattura

    UNION ALL

    -- PRIMO SALDO FINE CONT. Section
    SELECT
        [AnnoContestazione] as Anno,
        [MeseContestazione] as Mese,
        'PRIMO SALDO' AS Tipologia,
        'FINE CONT.' as Fase,
        DataFine AS DataFineContestazioni,
        DATEADD(day, 30, DataFine) AS ChiusuraContestazioni,  -- +30 giorni
        DATEADD(day, 45, DataFine) AS TempoRisposta,  -- +45 giorni
        DataFine AS DataEsecuzione,
        NULL as DataFatturazione,
        CASE
            WHEN DataFine > GETDATE() THEN 0
            ELSE 1
        END AS Esecuzione,
        NULL AS Count
    FROM
        [pfw].[ContestazioniCalendario]

    UNION ALL

    -- SECONDO SALDO REL Section
    SELECT
        [AnnoContestazione] as Anno,
        [MeseContestazione] as Mese,
        'SECONDO SALDO' AS Tipologia,
        'REL' as Fase,
        DataVerifica AS DataFineContestazioni,
        NULL AS ChiusuraContestazioni,
        NULL AS TempoRisposta,
        CAST(DATEADD(DAY, 1, DataVerifica) AS DATE) AS DataEsecuzione,
        NULL as DataFatturazione,
        CASE
            WHEN ISNULL(ct.RelCount, 0) = 0 AND CAST(GETDATE() AS DATE) >= CAST(DATEADD(DAY, 1, DataVerifica) AS DATE) THEN 2
            WHEN ISNULL(ct.RelCount, 0) > 0 THEN 1
            ELSE 0
        END AS Esecuzione,
        ct.RelCount AS Count
    FROM
        [pfw].[ContestazioniCalendario] cc
    LEFT JOIN (
        SELECT
            month,
            year,
            TipologiaFattura,
            COUNT(*) AS RelCount
        FROM
            pfd.RelTestata
        GROUP BY
            month,
            year,
            TipologiaFattura
    ) ct
        ON cc.[MeseContestazione] = ct.month
        AND cc.[AnnoContestazione] = ct.year
        AND 'SECONDO SALDO' = ct.TipologiaFattura

    UNION ALL

    -- SECONDO SALDO FINE CONT. Section
    SELECT
        [AnnoContestazione] as Anno,
        [MeseContestazione] as Mese,
        'SECONDO SALDO' AS Tipologia,
        'FINE CONT.' as Fase,
        DataVerifica AS DataFineContestazioni,
        DATEADD(day, 30, DataFine) AS ChiusuraContestazioni,  -- +30 giorni
        DATEADD(day, 45, DataFine) AS TempoRisposta,  -- +45 giorni
        DataVerifica AS DataEsecuzione,
        NULL as DataFatturazione,
        CASE
            WHEN DataVerifica > GETDATE() THEN 0
            ELSE 1
        END AS Esecuzione,
        NULL AS Count
    FROM
        [pfw].[ContestazioniCalendario]

    UNION ALL

    -- IMPORT DATI NOTIFICHE (Previous Years) Section
    SELECT
        y.year AS Anno,
        m AS Mese,
        'IMPORT DATI' AS Tipologia,
        'NOTIFICHE' AS Fase,
        NULL AS DataFineContestazioni,
        NULL AS ChiusuraContestazioni,
        NULL AS TempoRisposta,
        DATEFROMPARTS(y.year, m, 5) AS DataEsecuzione,
        NULL AS DataFatturazione,
        CASE
            WHEN CAST(GETDATE() AS DATE) >= DATEFROMPARTS(y.year, m, 5)
                 AND ISNULL(n.CountEnti, 0) = 0 THEN 3
            WHEN CAST(GETDATE() AS DATE) >= DATEFROMPARTS(y.year, m, 5) THEN 1
            ELSE 0
        END AS Esecuzione,
        ISNULL(n.CountEnti, 0) AS Count
    FROM
        (VALUES (1), (2), (3), (4), (5), (6), (7), (8), (9), (10), (11), (12)) AS T(m) -- All months (1-12)
    CROSS JOIN
        (
            SELECT DISTINCT year
            FROM pfd.NotificheCount
            WHERE year < YEAR(GETDATE()) -- Only previous years
        ) y
    LEFT JOIN
        (
            SELECT
                year AS year_n,
                month AS month_n,
                COUNT(*) AS CountEnti
            FROM pfd.NotificheCount
            GROUP BY year, month
        ) AS n
        ON
            (n.year_n = y.year AND n.month_n = m - 1) -- Normal case (previous month in same year)
            OR (m = 1 AND n.year_n = y.year - 1 AND n.month_n = 12) -- January case (previous year December)

    UNION ALL

    -- IMPORT DATI NOTIFICHE (Current Year) Section
    SELECT
        y.year AS Anno,
        Months.m AS Mese,
        'IMPORT DATI' AS Tipologia,
        'NOTIFICHE' AS Fase,
        NULL AS DataFineContestazioni,
        NULL AS ChiusuraContestazioni,
        NULL AS TempoRisposta,
        DATEFROMPARTS(y.year, Months.m, 5) AS DataEsecuzione,
        NULL AS DataFatturazione,
        CASE
            WHEN CAST(GETDATE() AS DATE) >= DATEFROMPARTS(y.year, Months.m, 5)
                 AND ISNULL(n.CountEnti, 0) = 0 THEN 3
            WHEN CAST(GETDATE() AS DATE) >= DATEFROMPARTS(y.year, Months.m, 5) THEN 1
            ELSE 0
        END AS Esecuzione,
        ISNULL(n.CountEnti, 0) AS Count
    FROM
        (VALUES (1), (2), (3), (4), (5), (6), (7), (8), (9), (10), (11), (12)) AS Months(m)
    CROSS JOIN
        (
            SELECT YEAR(GETDATE()) AS year
        ) y
    LEFT JOIN
        (
            SELECT
                year AS year_n,
                month AS month_n,
                COUNT(*) AS CountEnti
            FROM pfd.NotificheCount
            WHERE year IN (YEAR(GETDATE()), YEAR(GETDATE()) - 1) -- Include current and previous year
            GROUP BY year, month
        ) AS n
        ON
            (n.year_n = y.year AND n.month_n = Months.m - 1) -- Normal case (previous month in same year)
            OR (Months.m = 1 AND n.year_n = y.year - 1 AND n.month_n = 12) -- January case (previous year December)
    WHERE
        Months.m <= CASE
            WHEN MONTH(GETDATE()) = 12 THEN 12
            ELSE MONTH(GETDATE()) + 1
        END

    UNION ALL

    -- Fatturazione Section
    SELECT
        cf.[AnnoRiferimento] as Anno,
        cf.[MeseRiferimento] as Mese,
        cf.[TipologiaFattura] as Tipologia,
        CASE
            WHEN cf.[Fase] = 1 AND cf.[TipologiaFattura] = 'PRIMO SALDO' THEN 'FATT.'
            WHEN cf.[Fase] = 1 AND cf.[TipologiaFattura] = 'ANTICIPO' THEN 'FATT.'
            WHEN cf.[Fase] = 1 AND cf.[TipologiaFattura] = 'ACCONTO' THEN 'FATT.'
            WHEN cf.[Fase] = 1 AND cf.[TipologiaFattura] = 'VAR. SEMESTRALE' THEN 'FATT.'
            ELSE 'FATT. REL FIRM.'
        END AS Fase,
        NULL as DataFineContestazioni,
        NULL AS ChiusuraContestazioni,
        NULL AS TempoRisposta,
        cf.[DataEsecuzione] as DataEsecuzione,
        cf.[DataFatturazione] as DataFatturazione,
        CASE
            WHEN cf.[CicloEffettuato] = 0 AND CAST(GETDATE() AS DATE) >= CAST(DATEADD(DAY, 1, cf.[DataEsecuzione]) AS DATE) THEN 3
            WHEN cf.[CicloEffettuato] = 1 THEN 1
            ELSE 0
        END AS Esecuzione,
        f.CountFatture AS Count
    FROM
        [cfg].[CalendarioFatturazione] cf
    LEFT JOIN (
        SELECT
            FkTipologiaFattura,
            COUNT(*) AS CountFatture,
            DataFattura
        FROM
            pfd.FattureTestata
        GROUP BY
            FkTipologiaFattura,
            DataFattura
    ) f
        ON f.FkTipologiaFattura = cf.[TipologiaFattura]
        AND f.DataFattura = cf.DataFatturazione
) AS combined_results
