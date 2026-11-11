
/*
  Data creazione:        27/10/2026
  Data ultima modifica:  10/11/2026
  Descrizione:           Visualizza i gruppi delle fatture emesse nella pagina Invio a SAP, esclude le fatture in Gestione Fatture
  Target utilizzo:       Pagina Invio a SAP
  Versione:              1.1
*/

CREATE OR ALTER VIEW [be].[vwInvioSAPGriglia]
AS

WITH cte_fattureEmesse AS (
    SELECT
         ft.[FkTipologiaFattura]    AS TipologiaFattura
        ,ft.FatturaInviata            AS StatoFattura        -- Lo stato della fattura è sempre preso da pfd.FattureTestata
        ,ft.AnnoRiferimento            AS Anno
        ,ft.MeseRiferimento            AS Mese
        ,ft.TotaleFattura            AS TotaleFattura
    FROM pfd.FattureTestata ft
    WHERE NOT EXISTS (                                    -- Esclusione delle fatture presenti in Gestione Fatture
        SELECT 1
        FROM cfg.GestioneFatture gf
        WHERE gf.FkIdFattura = ft.IdFattura
            AND gf.Stato IN (0, 3)                        -- Esclusione delle fatture Posticipate oppure Eliminate
    )
)
SELECT
     TipologiaFattura        AS [TipologiaFattura]
    ,StatoFattura            AS [StatoFattura]
    ,COUNT(*)                AS [NumeroFatture]
    ,Anno                    AS [Anno]
    ,Mese                    AS [Mese]
    ,SUM(TotaleFattura)        AS [Importo Totale]
FROM cte_fattureEmesse
GROUP BY TipologiaFattura, StatoFattura, Anno, Mese

--WHERE ft.AnnoRiferimento = 2024 AND ft.MeseRiferimento = 2 AND FkTipologiaFattura = 'ANTICIPO'
GO
