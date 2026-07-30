/*
  Data creazione:        29/07/2026
  Data ultima modifica:  30/07/2026
  Descrizione:           Visualizza fatture Non Fatturate (Eliminate e Posticipate) nella pagina Documenti Emessi
  Target utilizzo:       Pagina Documenti Emessi con filtro Stato = Non Fatturate
  Versione:              1.0
  Nota seed: CREATE OR ALTER (idempotente). Espone [fattura.anno]/[fattura.mese]/[fattura.tipocontratto]
             per i filtri BE. Il BE avvolge la vista in SELECT listaFatture = (... FOR JSON PATH).
*/
CREATE OR ALTER VIEW [be].[vwDocumentiEmessiNonFatturati]
AS

WITH FattureEliminate AS (
    SELECT
        ft.AnnoRiferimento          as 'fattura.anno',
        ft.MeseRiferimento          as 'fattura.mese',
        c.FkIdTipoContratto         as 'fattura.tipocontratto',
        CAST(FT.TotaleFattura AS DECIMAL(10, 2)) AS 'fattura.totale',
        FT.Progressivo              AS 'fattura.numero',
	    FT.IdFattura                AS 'fattura.idfattura',
        CONVERT(VARCHAR, FT.DataFattura, 23) AS 'fattura.dataFattura',
        FT.FkProdotto               AS 'fattura.prodotto',
        CAST(FT.MeseRiferimento as varchar(2)) + '/' + CAST(FT.AnnoRiferimento as VARCHAR(4)) AS 'fattura.identificativo',
        FT.FkTipologiaFattura       AS 'fattura.tipologiaFattura',
        FT.FkIdEnte                 AS 'fattura.istitutioID',
        FT.CodiceContratto          AS 'fattura.onboardingTokenID',
        FT.FkIdTipoDocumento        AS 'fattura.tipoDocumento',
        FT.Divisa                   AS 'fattura.divisa',
        FT.MetodoPagamento          AS 'fattura.metodoPagamento',
	    CONCAT(
            REPLACE(ftc.Causale,'[percentuale]', ISNULL(ftc.PercentualeAnticipo,''))
            ,' '
            ,CAST(FT.MeseRiferimento as varchar(2))
            , '/'
            , CAST(FT.AnnoRiferimento as VARCHAR(4))
        )                           AS 'fattura.causale',
        FT.SplitPayment             AS 'fattura.split',
	    3                           AS 'fattura.inviata', -- Permette di visualizzare nel FE la label ELIMINATA
        ISNULL(FT.Sollecito, '')    AS 'fattura.sollecito',
        (
            SELECT
                ISNULL(tC.[Descrizione],'')             AS 'tipologia',
                ''                                      AS 'riferimentoNumeroLinea',
                ISNULL(FTn.IdDocumento,'')              AS 'idDocumento',
                CONVERT(VARCHAR,FTn.DataDocumento, 23)  AS 'data',
                ISNULL(FTn.NumItem,'')                  AS 'numItem',
                ISNULL(FTn.CodCommessa,'')              AS 'codiceCommessaConvenzione',
                ISNULL(FTn.Cup,'')                      AS 'CUP',
                ISNULL(FTn.Cig,'')                      AS 'CIG'
            FROM
                [pfd].[FattureTestata_Eliminate] FTn
                LEFT JOIN [pfw].[DatiFatturazione] dF
                    ON ISNULL(FTn.FkIdDatiFatturazione,'') = ISNULL(dF.IdDatiFatturazione,'')
                LEFT JOIN [pfw].[TipoCommessa] tC
                    ON dF.[FkTipoCommessa] = tC.[TipoCommessa]
            WHERE
                FTn.IdFattura = FT.IdFattura
            FOR JSON PATH
        ) AS 'fattura.datiGeneraliDocumento',
        (
            SELECT
                FR.NumeroLinea                              AS 'numerolinea',
                ISNULL(FR.Testo,'')                         AS 'testo',
                FR.CodiceMateriale                          AS 'codiceMateriale',
                FR.Quantita AS 'quantita',
                CAST(FR.PrezzoUnitario AS DECIMAL(10, 2))   AS 'prezzoUnitario',
                CAST(FR.Imponibile AS DECIMAL(10, 2))       AS 'imponibile',
			    FR.PeriodoRiferimento                       as 'periodoRiferimento'
            FROM
                [pfd].[FattureRighe_Eliminate] FR
		        LEFT JOIN [pfw].[CodiciMateriali] CM
		            ON FR.CodiceMateriale = CM.CodiceMateriale
            WHERE FT.IdFattura = FR.FkIdFattura
		    ORDER BY CM.Ordinamento
            FOR JSON PATH, INCLUDE_NULL_VALUES
        ) AS 'fattura.posizioni'
    FROM
        [pfd].[FattureTestata_Eliminate] FT
	    INNER JOIN pfd.Contratti c
            ON c.onboardingtokenid = FT.CodiceContratto
            AND c.internalistitutionid  = ft.FkIdEnte
	    INNER JOIN pfw.FatturaTestataConfig ftc
            ON ftc.FkTipologiaFattura = FT.FkTipologiaFattura
            AND ftc.FKIdTipoContratto = c.FkIdTipoContratto
    WHERE
        FT.FkIdEnte <> '4a4149af-172e-4950-9cc8-63ccc9a6d865' -- Escludi ente PagoPA
)
, FatturePosticipate AS (

    -- Seleziona tutte le fatture posticipate da GestioneFatture
    SELECT
        gf.Anno                     AS 'fattura.anno',
        gf.Mese                     AS 'fattura.mese',
        c.FkIdTipoContratto         AS 'fattura.tipocontratto',
        CAST(ft.TotaleFattura AS DECIMAL(10, 2)) AS 'fattura.totale',
        ft.Progressivo              AS 'fattura.numero',
	    gf.FkIdFattura              AS 'fattura.idfattura',
        ft.DataFattura              AS 'fattura.dataFattura',
        ft.FkProdotto               AS 'fattura.prodotto',
        CAST(gf.Mese as varchar(2)) + '/' + CAST(gf.Anno as VARCHAR(4)) AS 'fattura.identificativo',
        gf.FkTipologiaFattura       AS 'fattura.tipologiaFattura',
        gf.FkIdEnte                 AS 'fattura.istitutioID',
        c.onboardingtokenid         AS 'fattura.onboardingTokenID',
        ft.FkIdTipoDocumento        AS 'fattura.tipoDocumento',
        ft.Divisa                   AS 'fattura.divisa',
        ft.MetodoPagamento          AS 'fattura.metodoPagamento',
	    CONCAT(
            REPLACE(ftc.Causale,'[percentuale]', ISNULL(ftc.PercentualeAnticipo,''))
            ,' '
            ,CAST(FT.MeseRiferimento as varchar(2))
            , '/'
            , CAST(FT.AnnoRiferimento as VARCHAR(4))
        )                           AS 'fattura.causale',
        ft.SplitPayment             AS 'fattura.split',
	    4                           AS 'fattura.inviata', -- Questo permette di visualizzare nel FE la label POSTICIPATA
        ISNULL(FT.Sollecito, '')    AS 'fattura.sollecito',
        (
            SELECT
                ISNULL(tC.[Descrizione],'')             AS 'tipologia',
                ''                                      AS 'riferimentoNumeroLinea',
                ISNULL(FTn.IdDocumento,'')              AS 'idDocumento',
                CONVERT(VARCHAR,FTn.DataDocumento, 23)  AS 'data',
                ISNULL(FTn.NumItem,'')                  AS 'numItem',
                ISNULL(FTn.CodCommessa,'')              AS 'codiceCommessaConvenzione',
                ISNULL(FTn.Cup,'')                      AS 'CUP',
                ISNULL(FTn.Cig,'')                      AS 'CIG'
            FROM
                [pfd].[FattureTestata] FTn
                LEFT JOIN [pfw].[DatiFatturazione] dF
                    ON ISNULL(FTn.FkIdDatiFatturazione,'') = ISNULL(dF.IdDatiFatturazione,'')
                LEFT JOIN [pfw].[TipoCommessa] tC
                    ON dF.[FkTipoCommessa] = tC.[TipoCommessa]
            WHERE FTn.IdFattura = FT.IdFattura
            FOR JSON PATH
        ) AS 'fattura.datiGeneraliDocumento',
        (
            SELECT
                FR.NumeroLinea                              AS 'numerolinea',
                ISNULL(FR.Testo,'')                         AS 'testo',
                FR.CodiceMateriale                          AS 'codiceMateriale',
                FR.Quantita                                 AS 'quantita',
                CAST(FR.PrezzoUnitario AS DECIMAL(10, 2))   AS 'prezzoUnitario',
                CAST(FR.Imponibile AS DECIMAL(10, 2))       AS 'imponibile',
		        FR.PeriodoRiferimento                       AS 'periodoRiferimento'
            FROM [pfd].[FattureRighe] FR
	            LEFT JOIN [pfw].[CodiciMateriali] CM
	                ON FR.CodiceMateriale = CM.CodiceMateriale
            WHERE FT.IdFattura = FR.FkIdFattura
	        ORDER BY CM.Ordinamento
            FOR JSON PATH, INCLUDE_NULL_VALUES
        ) AS 'fattura.posizioni'
    FROM
	    cfg.GestioneFatture gf
        LEFT JOIN pfd.FattureTestata ft
            ON gf.Anno = ft.AnnoRiferimento
            AND gf.Mese = ft.MeseRiferimento
            AND gf.FkTipologiaFattura = ft.FkTipologiaFattura
            AND gf.FkIdEnte = ft.FkIdEnte
        INNER JOIN pfd.Contratti c
            ON c.internalistitutionid = gf.FkIdEnte
        INNER JOIN pfw.FatturaTestataConfig ftc
            ON ftc.FkTipologiaFattura = FT.FkTipologiaFattura
            AND ftc.FKIdTipoContratto = c.FkIdTipoContratto
    WHERE
        gf.Stato IN (0)                                             -- Filtro Fatture POSTICIPATE
	    AND gf.FkIdEnte <> '4a4149af-172e-4950-9cc8-63ccc9a6d865'   -- Escludi ente PagoPA
)
, FinalCte AS (
    SELECT
        fe.[fattura.anno], fe.[fattura.mese], fe.[fattura.tipocontratto], fe.[fattura.totale], fe.[fattura.numero],
        fe.[fattura.idfattura], fe.[fattura.dataFattura], fe.[fattura.prodotto], fe.[fattura.identificativo],
        fe.[fattura.tipologiaFattura], fe.[fattura.istitutioID], fe.[fattura.onboardingTokenID], fe.[fattura.tipoDocumento],
        fe.[fattura.divisa], fe.[fattura.metodoPagamento], fe.[fattura.causale], fe.[fattura.split], fe.[fattura.inviata],
        fe.[fattura.sollecito], fe.[fattura.datiGeneraliDocumento], fe.[fattura.posizioni]
    FROM FattureEliminate fe
    UNION ALL
    SELECT
        fp.[fattura.anno], fp.[fattura.mese], fp.[fattura.tipocontratto], fp.[fattura.totale], fp.[fattura.numero],
        fp.[fattura.idfattura], fp.[fattura.dataFattura], fp.[fattura.prodotto], fp.[fattura.identificativo],
        fp.[fattura.tipologiaFattura], fp.[fattura.istitutioID], fp.[fattura.onboardingTokenID], fp.[fattura.tipoDocumento],
        fp.[fattura.divisa], fp.[fattura.metodoPagamento], fp.[fattura.causale], fp.[fattura.split], fp.[fattura.inviata],
        fp.[fattura.sollecito], fp.[fattura.datiGeneraliDocumento], fp.[fattura.posizioni]
    FROM FatturePosticipate fp
)
SELECT
    f.[fattura.anno], f.[fattura.mese], f.[fattura.tipocontratto], f.[fattura.totale], f.[fattura.numero],
    f.[fattura.idfattura], f.[fattura.dataFattura], f.[fattura.prodotto], f.[fattura.identificativo],
    f.[fattura.tipologiaFattura], f.[fattura.istitutioID], f.[fattura.onboardingTokenID], f.[fattura.tipoDocumento],
    f.[fattura.divisa], f.[fattura.metodoPagamento], f.[fattura.causale], f.[fattura.split], f.[fattura.inviata],
    f.[fattura.sollecito], f.[fattura.datiGeneraliDocumento], f.[fattura.posizioni]
FROM FinalCte f
GO
