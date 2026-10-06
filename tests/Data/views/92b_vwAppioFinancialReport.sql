-- DDL reale fornita il 06/10/2026, riprodotta as-is (solo CREATE -> CREATE OR ALTER per l'hot-apply).
-- Tabella e seed in tests/Data/appio.sql; letta da AppIoFinancialReportSQLBuilder (PF-908).
-- ⚠️ Il commento "da aggiungere join" è del team Data: se la join con le posizioni entra qui, le
-- query del builder (che la fanno già in C#) vanno riviste.
/*
  Data creazione:        29/09/2026
  Data ultima modifica:  29/09/2026
  Descrizione:           Visualizza Financial Report APPIO
  Target utilizzo:       Pagina Documenti Emessi APPIO in Portale Fatturazione
  Versione:              1.0
*/
CREATE OR ALTER VIEW [be].[vwAppioFinancialReport]
AS

SELECT
    fr.name
  , fr.category
  , fr.current_trx
  , fr.value
  , fr.codice_articolo
  , fr.year_quarter
  , recipient_id
FROM appio.FinancialReports fr

-- da aggiungere join con appio.FinancialReportPositions
GO
