-- DDL reale fornita il 10/11/2026, riprodotta as-is (solo CREATE -> CREATE OR ALTER per l'hot-apply).
-- Tabelle e seed in tests/Data/appio.sql (APP IO, PF-908).
-- Filtro Codice Fiscale Ente della pagina documenti emessi (legge i contratti, non i documenti).
CREATE OR ALTER VIEW [be].[vwAppioFinalReportFiltroCodFiscaleEnte]
AS

SELECT DISTINCT c.institution_taxcode as CodFiscaleEnte
FROM appio.contracts c
WHERE c.institution_taxcode is not null
GO
