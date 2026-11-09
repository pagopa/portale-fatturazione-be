-- =============================================================================================
-- Righe per EmailPspServiceIntegrationTests: ppa.KPMG, ppa.KpiPagamenti_Sconto, ppa.PspEmail.
-- Tutte SINTETICHE (repository pubblico), sul trimestre 2026_1, e appoggiate ai contratti di
-- ppa_contracts.sql. Va eseguito dopo ppa_contracts.sql, ppa_kpmg.sql, ppa_kpi_sconto.sql e
-- ppa_pspemail.sql (v. entrypoint.sh).
--
-- Un caso per ogni ramo che i test verificano:
--   · PSP-T02, referentefattura_mail valorizzata  -> email = referente;   sconto 2,50 -> report sconti
--   · PSP-T01, referente NULL, courtesy_mail       -> email = cortesia;    sconto 0    -> report sconti vuoto
--   · PSP-T03, descrizione_riga vuota              -> escluso da GetSenderEmail (LEN(descrizione_riga) > 0)
--   · ppa.PspEmail: un invio FINANCIAL_ADJUST gia' fatto (Invio = 1) -> CountInvioAdjustment = true
--
-- descrizione_riga contiene i link ai report separati da virgola: il servizio sceglie quello che
-- contiene 'agentquarter' e quello che contiene 'detailed'. Gli URL sono su .invalid (RFC 2606).
-- =============================================================================================

IF NOT EXISTS (SELECT 1 FROM ppa.KPMG WHERE year_quarter = '2026_1' AND contract_id LIKE 'PSP-T%')
INSERT INTO ppa.KPMG
 (contract_id, tipo_doc, denominazione_destinatario, numero, [data], progressivo_riga, codice_articolo,
  descrizione_riga, q_ta, importo, anno, year_quarter)
VALUES
 ('PSP-T02', 'TD01', 'Banca Beta Test',  'PSP-2026-0002', '2026-04-15', 1, 'ART-FIN',
  'https://report.example.invalid/2026_1/PSP-T02_agentquarter.pdf,https://report.example.invalid/2026_1/PSP-T02_detailed.csv',
  1, 1500.00, '2026', '2026_1'),
 ('PSP-T01', 'TD01', 'Banca Alfa Test',  'PSP-2026-0001', '2026-04-15', 1, 'ART-FIN',
  'https://report.example.invalid/2026_1/PSP-T01_detailed.csv,https://report.example.invalid/2026_1/PSP-T01_agentquarter.pdf',
  1, 800.00, '2026', '2026_1'),
 ('PSP-T03', 'TD01', 'Cassa Gamma',      'PSP-2026-0003', '2026-04-15', 1, 'ART-FIN',
  '',
  1, 300.00, '2026', '2026_1');
GO

IF NOT EXISTS (SELECT 1 FROM ppa.KpiPagamenti_Sconto WHERE year_quarter = '2026_1' AND recipient_id LIKE 'PSP-T%')
INSERT INTO ppa.KpiPagamenti_Sconto
 (psp_id, recipient_id, year_quarter, trx_total, value_total, KpiOk, PercSconto, value_discount, linkReport, KpiList, FlagMQ)
VALUES
 ('PSP-T02', 'PSP-T02', '2026_1', 1000, 1500.00, 1, 2.50, 37.50,
  'https://report.example.invalid/2026_1/PSP-T02_sconti.pdf', 'KPI1,KPI2', 0),
 ('PSP-T01', 'PSP-T01', '2026_1',  500,  800.00, 0, 0.00,  0.00,
  'https://report.example.invalid/2026_1/PSP-T01_sconti.pdf', 'KPI1', 0);
GO

IF NOT EXISTS (SELECT 1 FROM ppa.PspEmail WHERE Trimestre = '2026_1' AND IdContratto = 'PSP-T02')
INSERT INTO ppa.PspEmail
 (IdContratto, Tipologia, Anno, Trimestre, DataEvento, Email, Messaggio, RagioneSociale, Invio)
VALUES
 ('PSP-T02', 'FINANCIAL_ADJUST', 2026, '2026_1', '2026-05-02 10:00:00', 'ref@beta.invalid',
  'Invio sintetico del seed', 'Banca Beta Test', 1);
GO
