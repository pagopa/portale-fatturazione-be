-- DDL reale fornita il 10/11/2026, riprodotta as-is (solo CREATE -> CREATE OR ALTER per l'hot-apply).
-- Tabelle e seed in tests/Data/appio.sql (APP IO, PF-908).
-- Filtro Tipo Ente della pagina di anagrafica.
CREATE OR ALTER VIEW [be].[vwAppioContractsFiltroTipoEnte]
AS

SELECT DISTINCT c.institution_institutiontype as TipoEnte
FROM appio.contracts c
WHERE c.institution_institutiontype is not null
GO
