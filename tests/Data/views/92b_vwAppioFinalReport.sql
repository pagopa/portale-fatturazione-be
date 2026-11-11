-- DDL reale fornita il 10/11/2026, aggiornata l'11/11/2026 (ultima modifica DATA 09/11/2026),
-- riprodotta as-is (solo ALTER -> CREATE OR ALTER per l'hot-apply).
-- Tabelle e seed in tests/Data/appio.sql (APP IO, PF-908).
-- Griglia padre/figlio della pagina documenti emessi APP IO. Dall'11/11/2026: colonne year e quarter
-- separate, trimestre 'AAAA_T' come nel dettaglio, institution_taxcode, Data convertita a date.
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
      , TRY_CONVERT(date, TRIM(fr.[Data]), 23) as Data
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
