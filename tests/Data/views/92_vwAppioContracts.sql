-- DDL reale fornita il 01/10/2026, riprodotta as-is (solo CREATE -> CREATE OR ALTER per l'hot-apply).
-- Proiezione 1:1 di appio.Contracts (tabella e seed in tests/Data/appio.sql), letta da
-- AppIoContrattiSQLBuilder per gli endpoint APP IO (PF-908).
CREATE OR ALTER VIEW [be].[vwAppioContracts]
AS

SELECT
	  c.contract_id
	, c.name
	, c.tax_code
	, c.vat_code
	, c.vat_group
	, c.sdi_code
	, year_month
	, year_quarter
FROM appio.Contracts c
GO
