-- DDL reale fornita il 10/11/2026, aggiornata l'11/11/2026 (ultima modifica DATA 09/11/2026),
-- riprodotta as-is (solo ALTER -> CREATE OR ALTER per l'hot-apply).
-- Tabelle e seed in tests/Data/appio.sql (APP IO, PF-908).
-- Dettaglio di un documento emesso APP IO. Dall'11/11/2026 espone onboardingtokenid (filtro per
-- contratto) e converte data_report/data_riferimento a date. Trimestre 'AAAA_T' (year_quarter)
-- più year e quarter separati, come la vista padre.
CREATE OR ALTER VIEW [be].[vwAppioFinalReportDetails]
AS

SELECT 
    -- anagrafica
      c.institution_description as nome_ente
      , c.onboardingtokenid as onboardingtokenid
      , c.[product] as prodotto
      , cast(c.createdat as date) as data_creazione
      --, c.[state] as stato_contratto
      , c.pricingplan
      , c.billing_vatnumber as p_iva
      , c.institution_taxcode as CodTributario
      , cast(c.updatedat as date) as ultimo_aggiornamento
    -- documento contabile
      , fr.Numero
      , concat(fr.[year], '_', fr.[quarter]) as year_quarter 
      , fr.[year] as [year]
      ,fr.quarter As [quarter]
      , fr.Tipo_doc
      , fr.Valuta
      , fr.CODICE_AGGIUNTIVO
      --, fr.[Data] as data_report
      ,TRY_CONVERT(date, TRIM(fr.[Data]), 23) as data_report
      --, fr.[Riferimento_data] as data_riferimento
      ,TRY_CONVERT(date, TRIM(fr.Riferimento_data), 23) as data_riferimento
      , Sum(fr.Importo) over (partition by c.onboardingtokenid, c.internalistitutionid, fr.year, fr.quarter) as ImportoTotale
    -- posizioni
      , fr.Codice_articolo
      , fr.Q_ta
      , fr.Importo
      , fr.Cod_iva
      , fr.Condizioni
      , fr.CAUSALE
FROM appio.messages_final_report fr
inner join appio.contracts c
    on fr.contract_id = c.onboardingtokenid


GO
