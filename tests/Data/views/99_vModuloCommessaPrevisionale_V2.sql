-- Vista legacy pfd.v*. DDL reale fornita dal team DB, riprodotta as-is (CREATE -> CREATE OR ALTER).
-- Confronta la distribuzione regionale dichiarata (pfw.DatiModuloCommessaRegioni) col totale
-- nazionale del modulo commessa (spedizioni 1 e 2): serve a intercettare compilazioni sbagliate.
-- TotaleCoperturaRegionale: 'VALIDO' se coincidono, 'ECCESSIVO'/'INSUFFICIENTE' altrimenti,
-- 'DISTRIBUZIONE REGIONALE NON COMPILATA' se manca del tutto.
-- ⚠️ Usa pfw.DatiModuloCommessaRegioni.Calcolato, colonna che il setup.sql scritto a mano NON aveva:
-- viene aggiunta da modulo_commessa.sql (tipo da confermare sul DB reale).
CREATE OR ALTER VIEW [pfd].[vModuloCommessaPrevisionale_V2] AS
WITH cte_regioni AS (
    SELECT anno, mese, Internalistitutionid,
        SUM(AR) AS [AR], SUM([890]) AS AR_890,
        SUM(ISNULL(AR,0)) + SUM(ISNULL([890],0)) AS TotaleRegioni,
        MAX(CAST(Calcolato as int)) as Calcolato
    FROM pfw.DatiModuloCommessaRegioni mcr
    INNER JOIN pfw.Regioni r ON r.CodiceIstat = mcr.Regione
    GROUP BY Internalistitutionid, anno, mese
),
cte_regioni_distribuzione AS (
    SELECT anno, mese, Internalistitutionid, r.Regione,
        SUM(ISNULL(AR,0) + ISNULL([890],0)) OVER (PARTITION BY Anno, Mese, Internalistitutionid, mcr.Regione, Calcolato) AS TotaleRegioni,
        AR, [890], Calcolato
    FROM pfw.DatiModuloCommessaRegioni mcr
    INNER JOIN pfw.Regioni r ON r.CodiceIstat = mcr.Regione
),
cte_regioni_distribuzione_mc AS (
    SELECT AnnoValidita, MeseValidita, FkIdEnte, SUM(NumeroNotificheNazionali) AS TotaleModuloCommessa
    FROM pfw.DatiModuloCommessa
    WHERE FkIdTipoSpedizione IN (1, 2)
    GROUP BY AnnoValidita, MeseValidita, FkIdEnte
),
cte_moduloCommessa AS (
    SELECT AnnoValidita, MeseValidita, sum(TotaleModuloCommessa) as TotaleModuloCommessa,
        sum(AR) as AR, sum([890]) as [890], FkIdEnte
    FROM (
        SELECT AnnoValidita, MeseValidita, SUM(NumeroNotificheNazionali) AS TotaleModuloCommessa, FkIdTipoSpedizione,
            case when FkIdTipoSpedizione = 1 then SUM(NumeroNotificheNazionali) else 0 end as AR,
            case when FkIdTipoSpedizione = 2 then SUM(NumeroNotificheNazionali) else 0 end as [890],
            FkIdEnte
        FROM pfw.DatiModuloCommessa
        WHERE FkIdTipoSpedizione IN (1, 2)
        GROUP BY FkIdEnte, AnnoValidita, MeseValidita, FkIdTipoSpedizione
    ) a
    GROUP BY FkIdEnte, AnnoValidita, MeseValidita
),
cte_distribuzione_regionale AS (
    SELECT Anno, mese, FkIdEnte, e.[description] AS Ente, 'DETTAGLIO' as TipoReport,
        TotaleModuloCommessa, rd.AR, rd.[890], TotaleRegioni, Regione, Calcolato,
        ISNULL(FORMAT((CAST(ISNULL(rd.[AR],0) AS DECIMAL(10, 2)) / NULLIF(TotaleModuloCommessa, 0)) * 100.0, 'N2'), 0) AS AR_REGIONI_PERC,
        ISNULL(FORMAT((CAST(ISNULL(rd.[890],0) AS DECIMAL(10, 2)) / NULLIF(TotaleModuloCommessa, 0)) * 100.0, 'N2'), 0) AS [890_REGIONI_PERC],
        ISNULL(FORMAT((CAST(ISNULL(TotaleRegioni,0) AS DECIMAL(10, 2)) / NULLIF(TotaleModuloCommessa, 0)) * 100.0, 'N2'), 0) AS TOTALE_REGIONI_PERC
    FROM cte_regioni_distribuzione_mc rdmc
    LEFT JOIN cte_regioni_distribuzione rd ON rdmc.AnnoValidita = rd.Anno AND rdmc.MeseValidita = rd.Mese AND rdmc.FkIdEnte = rd.Internalistitutionid
    INNER JOIN pfd.Enti e ON rdmc.FkIdEnte = e.InternalIstitutionId
),
cte_final AS (
    SELECT mc.AnnoValidita AS [Anno], mc.MeseValidita AS [Mese], mc.FkIdEnte AS IdEnte,
        e.[description] AS Ente, 'TOTALI' as TipoReport, mc.TotaleModuloCommessa, mc.AR, mc.[890],
        mcr.TotaleRegioni, NULL AS Regione, mcr.Calcolato,
        ISNULL(FORMAT((CAST(ISNULL(mcr.AR,0) AS DECIMAL(10, 2)) / NULLIF(mc.TotaleModuloCommessa, 0)) * 100.0, 'N2'), 0) AS AR_REGIONI_PERC,
        ISNULL(FORMAT((CAST(ISNULL(mcr.AR_890,0) AS DECIMAL(10, 2)) / NULLIF(mc.TotaleModuloCommessa, 0)) * 100.0, 'N2'), 0) AS [890_REGIONI_PERC],
        ISNULL(FORMAT((CAST(ISNULL(mcr.AR_890,0) + ISNULL(mcr.AR,0) AS DECIMAL(10, 2)) / NULLIF(mc.TotaleModuloCommessa, 0)) * 100.0, 'N2'), 0) AS TOTALE_REGIONI_PERC,
        CASE WHEN TotaleRegioni > TotaleModuloCommessa THEN 'ECCESSIVO'
             WHEN TotaleRegioni = TotaleModuloCommessa THEN 'VALIDO'
             WHEN TotaleRegioni < TotaleModuloCommessa THEN 'INSUFFICIENTE'
             ELSE 'DISTRIBUZIONE REGIONALE NON COMPILATA' END AS [TotaleCoperturaRegionale]
    FROM cte_regioni mcr
    RIGHT JOIN cte_moduloCommessa mc ON mcr.Anno = mc.AnnoValidita AND mcr.Mese = mc.MeseValidita AND mcr.Internalistitutionid = mc.FkIdEnte
    INNER JOIN pfd.Enti e ON mc.FkIdEnte = e.InternalIstitutionId
    UNION ALL
    SELECT dr.Anno, dr.Mese, dr.FkIdEnte AS IdEnte, dr.Ente, dr.TipoReport, dr.TotaleModuloCommessa,
        dr.AR, dr.[890], dr.TotaleRegioni, dr.Regione, dr.Calcolato,
        dr.AR_REGIONI_PERC, dr.[890_REGIONI_PERC], dr.TOTALE_REGIONI_PERC, '' AS [TotaleCoperturaRegionale]
    FROM cte_distribuzione_regionale dr
)
SELECT Anno, Mese, IdEnte, Ente, TipoReport, TotaleModuloCommessa, AR, [890], TotaleRegioni,
    Regione, Calcolato, AR_REGIONI_PERC, [890_REGIONI_PERC], TOTALE_REGIONI_PERC, [TotaleCoperturaRegionale]
FROM cte_final;
GO
