-- DDL reale fornita il 06/10/2026, riprodotta as-is (solo CREATE -> CREATE OR ALTER per l'hot-apply).
-- Tabella e seed in tests/Data/appio.sql; letta da AppIoFinancialReportSQLBuilder (PF-908).
/*
  Data creazione:        29/09/2026
  Data ultima modifica:  29/09/2026
  Descrizione:           Visualizza Financial Report APPIO
  Target utilizzo:       Pagina Documenti Emessi APPIO in Portale Fatturazione
  Versione:              1.0
*/
CREATE OR ALTER VIEW [be].[vwAppioFinancialReportPositions]
AS

SELECT
      frp.[contract_id]
    , frp.[tipo_doc]
    , frp.[vat_code]
    , frp.[cod_fisc]
    , frp.[valuta]
    , frp.[id]
    , frp.[numero]
    , frp.[data]
    , frp.[codifica_art]
    , frp.[progressivo_riga]
    , frp.[codice_articolo]
    , frp.[descrizione_riga]
    , frp.[prezzo_unit]
    , frp.[q_ta]
    , frp.[importo]
    , frp.[cod_iva]
    , frp.[percent_iva]
    , frp.[iva]
    , frp.[codice_sdi]
    , frp.[rif_fattura]
    , frp.[year_quarter]
FROM appio.FinancialReportPositions frp

-- da aggiungere join con appio.FinancialReportPositions
GO
