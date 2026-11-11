-- DDL reale fornita il 10/11/2026, riprodotta as-is (solo CREATE -> CREATE OR ALTER per l'hot-apply).
-- Tabelle e seed in tests/Data/appio.sql (APP IO, PF-908).
-- Filtro Trimestre della pagina documenti emessi: 1-4, indipendente dall'anno.
CREATE OR ALTER VIEW [be].[vwAppioFinalReportFiltroTrimestre]
AS

SELECT DISTINCT fr.[quarter] as Trimestre
FROM appio.messages_final_report fr
WHERE fr.[quarter] is not null
GO
