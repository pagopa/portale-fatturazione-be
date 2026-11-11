-- DDL reale fornita il 10/11/2026, aggiornata l'11/11/2026, riprodotta as-is
-- (solo CREATE -> CREATE OR ALTER per l'hot-apply).
-- Tabelle e seed in tests/Data/appio.sql (APP IO, PF-908).
-- Filtro "nome ente" della pagina di anagrafica APP IO. Dall'11/11/2026 espone institution_id
-- (= internalistitutionid, l'ente, NON il contratto onboardingtokenid). Due enti omonimi restano
-- due righe distinte (nel seed APPIO-ENTE-1 e APPIO-ENTE-4).
CREATE OR ALTER VIEW [be].[vwAppioContractsFiltroNome]
AS

SELECT DISTINCT c.internalistitutionid AS institution_id, c.institution_description as institution_name
FROM appio.contracts c
WHERE c.institution_description is not null
GO
