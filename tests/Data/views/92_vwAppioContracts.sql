-- DDL reale fornita il 10/11/2026, aggiornata l'11/11/2026, riprodotta as-is
-- (solo ALTER -> CREATE OR ALTER per l'hot-apply).
-- Tabelle e seed in tests/Data/appio.sql (APP IO, PF-908).
-- Griglia della pagina di anagrafica APP IO (AppIoContrattiSQLBuilder).
-- Dall'11/11/2026 espone institution_id (= internalistitutionid).
CREATE OR ALTER VIEW [be].[vwAppioContracts]
AS

SELECT 
      c.onboardingtokenid
    , c.[internalistitutionid] as institution_id
    , c.institution_description as institution_name
    , c.[product] as prodotto
    , c.institution_institutiontype as institution_type
    , JSON_VALUE(c.institution, '$.digitaladdress') as email_PEC
    ,c. [state] as contract_state
    --, c.sdi_code
FROM appio.contracts c

GO
