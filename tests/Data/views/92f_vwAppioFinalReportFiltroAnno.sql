-- DDL reale fornita il 10/11/2026, riprodotta as-is (solo CREATE -> CREATE OR ALTER per l'hot-apply).
-- Tabelle e seed in tests/Data/appio.sql (APP IO, PF-908).
-- Filtro Anno della pagina documenti emessi.
CREATE OR ALTER VIEW [be].[vwAppioFinalReportFiltroAnno]
AS

SELECT DISTINCT fr.[year] as Anno
FROM appio.messages_final_report fr
WHERE fr.[year] is not null
GO
