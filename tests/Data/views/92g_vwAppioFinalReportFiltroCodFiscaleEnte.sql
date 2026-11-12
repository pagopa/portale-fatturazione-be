-- DDL reale, versione del 12/11/2026, riprodotta as-is (solo ALTER -> CREATE OR ALTER per l'hot-apply).
-- Tabelle e seed in tests/Data/appio.sql (APP IO, PF-908).
-- Filtro Codice Fiscale Ente della pagina documenti emessi: dal 12/11/2026 elenca i codici fiscali
-- degli enti con almeno un documento (join con appio.messages_final_report), scartando i NULL.
-- La versione del 10/11/2026 leggeva i soli contratti e proponeva anche enti senza documenti.
CREATE OR ALTER VIEW [be].[vwAppioFinalReportFiltroCodFiscaleEnte]
AS

SELECT c.institution_taxcode as CodFiscaleEnte
FROM appio.messages_final_report f
inner join appio.contracts c
on f.contract_id = c.onboardingtokenid
GROUP BY institution_taxcode
HAVING institution_taxcode IS NOT NULL
GO
