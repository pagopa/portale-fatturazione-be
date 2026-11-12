-- DDL reale, versione del 12/11/2026 (script delle 15:24), riprodotta as-is (solo ALTER -> CREATE OR
-- ALTER per l'hot-apply). Tabelle e seed in tests/Data/appio.sql (APP IO, PF-908).
-- Lo script delle 15:24 non ha più il blocco di intestazione standard (Data creazione, Descrizione,
-- Versione) che c'era in quello delle 15:10: qui non lo si aggiunge, la vista resta com'è a DB.
-- Griglia padre/figlio della pagina documenti emessi APP IO. Dall'11/11/2026: colonne year e quarter
-- separate, trimestre 'AAAA_T' come nel dettaglio, institution_taxcode, Data convertita a date.
-- Dal 12/11/2026: gold_updated_at (esposto dall'API) e file_path (percorso del file sul blob storage:
-- serve al download del financial report, l'API non lo espone).
-- ⚠️ ImportoTotale è partizionato per trimestre, non per Numero: con due documenti nello stesso
-- trimestre vale il totale del trimestre su entrambi (punto aperto con DATA).

CREATE OR ALTER VIEW [be].[vwAppioFinalReport]
AS

SELECT
    -- Vista padre
      c.internalistitutionid
      , c.onboardingtokenid
      , c.institution_description
      , c.[product]
      , c.institution_taxcode
      , fr.Numero
      , fr.[year] as [year]
      , fr.[quarter] as [quarter]
      , concat(fr.[year], '_', fr.[quarter]) as trimestre
      , Sum(fr.Importo) over (partition by c.onboardingtokenid, c.internalistitutionid, fr.year, fr.quarter) as ImportoTotale
      , TRY_CONVERT(date, TRIM(fr.[Data]), 23) as [Data]
      ,gold_updated_at
      ,file_path
      --fr.[Data]
    -- Vista figlio
      , fr.Codice_articolo
      , fr.Q_ta
      , fr.Importo
      , fr.Cod_iva
      , fr.Condizioni
      , fr.CAUSALE
FROM appio.messages_final_report fr
inner join appio.contracts c
    on fr.contract_id = c.onboardingtokenid

-- Per la vista padre a FE vanno aggregate le righe per onboardingtokenid, anno, trimestre

GO
