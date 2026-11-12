-- =============================================================================================
-- APP IO (PF-908, modello dati aggiornato il 10/11/2026): anagrafica dei contratti
-- (appio.contracts, da onboarding SelfCare) e documenti contabili (appio.messages_final_report),
-- letti dagli endpoint dell'area SOLO tramite le viste be.vwAppio* (tests/Data/views/92*).
--
-- Deve precedere views/: SQL Server non fa deferred name resolution sulle viste, quindi un
-- CREATE VIEW fallisce se la tabella referenziata non esiste ancora.
--
-- DDL delle tabelle: reale, fornita il 10/11/2026 e riprodotta as-is (salvo la guardia di
-- idempotenza). Nessuna ha PK: la chiave logica del contratto è onboardingtokenid, ed è la
-- colonna su cui le viste fanno la join (messages_final_report.contract_id = onboardingtokenid).
--
-- ⚠️ Sostituisce il modello di ottobre (appio.Contracts con PK contract_id+year_quarter,
-- appio.FinancialReports, appio.FinancialReportPositions). Su un container già inizializzato
-- con quel modello la guardia IF OBJECT_ID salterebbe la CREATE (i nomi sono case-insensitive):
-- serve il rebuild (docker compose down -v && up --build).
--
-- Dati tutti SINTETICI: questo file sta in un repository pubblico.
-- =============================================================================================

IF SCHEMA_ID('appio') IS NULL
    EXEC('CREATE SCHEMA appio');
GO

IF OBJECT_ID('appio.contracts', 'U') IS NULL
CREATE TABLE [appio].[contracts](
    [id] [nvarchar](4000) NULL,
    [internalistitutionid] [nvarchar](4000) NULL,
    [product] [nvarchar](4000) NULL,
    [state] [nvarchar](4000) NULL,
    [filepath] [nvarchar](4000) NULL,
    [filename] [nvarchar](4000) NULL,
    [contenttype] [nvarchar](4000) NULL,
    [onboardingtokenid] [nvarchar](100) NULL,
    [pricingplan] [nvarchar](4000) NULL,
    [institution] [nvarchar](max) NULL,
    [billing] [nvarchar](max) NULL,
    [updatedat] [datetime2](7) NULL,
    [createdat] [datetime2](7) NULL,
    [closedat] [datetime2](7) NULL,
    [isaggregator] [nvarchar](4000) NULL,
    [rootaggregator] [nvarchar](max) NULL,
    [testInstitution] [bit] NULL,
    [notificationtype] [nvarchar](4000) NULL,
    [ingestion_tms] [nvarchar](4000) NULL,
    [year] [nvarchar](4000) NULL,
    [month] [nvarchar](4000) NULL,
    [daily] [nvarchar](4000) NULL,
    [referenceonboardingid] [nvarchar](4000) NULL,
    [dl_processed_tms] [nvarchar](4000) NULL,
    [featureleveltype] [nvarchar](4000) NULL,
    [institution_taxcode] [nvarchar](4000) NULL,
    [institution_description] [nvarchar](4000) NULL,
    [institution_institutiontype] [nvarchar](4000) NULL,
    [billing_vatnumber] [nvarchar](4000) NULL,
    [billing_recipientcode] [nvarchar](4000) NULL,
    [createdat_daily] [date] NULL,
    [closedat_daily] [date] NULL,
    [drop_id] [nvarchar](4000) NULL,
    [silver_updated_at] [datetime2](7) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

-- Seed dei contratti, costruito per i filtri dell'anagrafica:
--   · OB1 e OB4 hanno lo STESSO nome ("Comune Alfa AppIO Test") ma ente, codice fiscale e tipo
--     diversi: un filtro per nome li fonde, uno per id li distingue;
--   · tipi ente distinti = PA, GSP (OB5 ha tipo NULL: escluso dalla vista filtro, presente in griglia);
--   · OB3 ha institution senza digitaladdress: email_PEC NULL, il mapping non deve rompersi;
--   · OB5 ha nome e codice fiscale NULL: escluso dalle viste filtro su nome e codice fiscale;
--   · OB3 non ha documenti contabili: compare in anagrafica, non nei documenti emessi.
IF NOT EXISTS (SELECT 1 FROM appio.contracts WHERE onboardingtokenid LIKE 'APPIO-OB%')
INSERT INTO appio.contracts
 (id, internalistitutionid, product, [state], onboardingtokenid, pricingplan, institution, createdat, updatedat,
  testInstitution, institution_taxcode, institution_description, institution_institutiontype, billing_vatnumber) VALUES
 ('APPIO-K1', 'APPIO-ENTE-1', 'prod-io', 'ACTIVE',    'APPIO-OB1', 'FA', N'{"digitaladdress":"alfa@pec.example.test"}',
  '2025-01-10T09:00:00', '2026-03-01T10:00:00', 0, '00000000001', 'Comune Alfa AppIO Test', 'PA',  '00000000001'),
 ('APPIO-K2', 'APPIO-ENTE-2', 'prod-io', 'ACTIVE',    'APPIO-OB2', 'FA', N'{"digitaladdress":"beta@pec.example.test"}',
  '2025-02-10T09:00:00', '2026-03-02T10:00:00', 0, '00000000002', 'Comune Beta AppIO Test', 'PA',  '00000000002'),
 ('APPIO-K3', 'APPIO-ENTE-3', 'prod-io', 'SUSPENDED', 'APPIO-OB3', 'FA', N'{"description":"Regione Gamma"}',
  '2025-03-10T09:00:00', '2026-03-03T10:00:00', 0, '00000000003', 'Regione Gamma',          'PA',  '00000000003'),
 ('APPIO-K4', 'APPIO-ENTE-4', 'prod-io', 'ACTIVE',    'APPIO-OB4', 'FA', N'{"digitaladdress":"alfa.gsp@pec.example.test"}',
  '2025-04-10T09:00:00', '2026-03-04T10:00:00', 0, '00000000004', 'Comune Alfa AppIO Test', 'GSP', '00000000004'),
 ('APPIO-K5', 'APPIO-ENTE-5', 'prod-io', 'ACTIVE',    'APPIO-OB5', 'FA', NULL,
  '2025-05-10T09:00:00', '2026-03-05T10:00:00', 0, NULL,          NULL,                     NULL,  NULL);
GO

IF OBJECT_ID('appio.messages_final_report', 'U') IS NULL
CREATE TABLE [appio].[messages_final_report](
    [year] [int] NULL,
    [quarter] [int] NULL,
    [contract_id] [nvarchar](100) NULL,
    [Tipo_doc] [nvarchar](4000) NULL,
    [sezionale] [nvarchar](4000) NULL,
    [CODICE_AGGIUNTIVO] [nvarchar](4000) NULL,
    [Denominazione_destinatario] [nvarchar](4000) NULL,
    [indirizzo] [nvarchar](4000) NULL,
    [Citta] [nvarchar](4000) NULL,
    [Prov] [nvarchar](4000) NULL,
    [CAP] [nvarchar](4000) NULL,
    [Stato] [nvarchar](4000) NULL,
    [vat_code] [nvarchar](4000) NULL,
    [Cod_fisc] [nvarchar](4000) NULL,
    [Valuta] [nvarchar](4000) NULL,
    [ID] [int] NULL,
    [Numero] [nvarchar](4000) NULL,
    [Data] [nvarchar](4000) NULL,
    [Bollo] [nvarchar](4000) NULL,
    [CODIFICA_ART] [nvarchar](4000) NULL,
    [Progressivo_Riga] [int] NULL,
    [Codice_articolo] [nvarchar](100) NULL,
    [Descrizione_riga] [nvarchar](4000) NULL,
    [Prezzo_unit] [nvarchar](4000) NULL,
    [Q_ta] [bigint] NULL,
    [Importo] [decimal](18, 6) NULL,
    [Cod_iva] [nvarchar](4000) NULL,
    [Percent_IVA] [nvarchar](4000) NULL,
    [IVA] [nvarchar](4000) NULL,
    [Condizioni] [nvarchar](4000) NULL,
    [CAUSALE] [nvarchar](4000) NULL,
    [ind_tipo_riga] [nvarchar](4000) NULL,
    [Codice_SDI] [nvarchar](4000) NULL,
    [NUM_DOC_RIF] [nvarchar](4000) NULL,
    [DATA_DOC_RIF] [nvarchar](4000) NULL,
    [TIPO_DOC_RIF] [nvarchar](4000) NULL,
    [Tipo_dato] [nvarchar](4000) NULL,
    [Riferimento_testo] [nvarchar](4000) NULL,
    [Riferimento_numero] [nvarchar](4000) NULL,
    [Riferimento_data] [nvarchar](4000) NULL,
    [Anno] [nvarchar](4000) NULL,
    [Rif_Fattura] [nvarchar](4000) NULL,
    [Sconto] [nvarchar](4000) NULL,
    [Data_limite_pagamento] [nvarchar](4000) NULL,
    [Banca] [nvarchar](4000) NULL,
    [Iban_riferimento_per_pagamento] [nvarchar](4000) NULL,
    [CIG] [nvarchar](4000) NULL,
    [indirizzo_pec] [nvarchar](4000) NULL,
    [CUP] [nvarchar](4000) NULL,
    [TIPO_DOC_RIF1] [nvarchar](4000) NULL,
    [campo1] [nvarchar](4000) NULL,
    [campo2] [nvarchar](4000) NULL,
    [internalistitutionid] [nvarchar](4000) NULL,
    [line_source] [nvarchar](100) NULL,
    [billing_iteration] [int] NULL,
    [run_id] [nvarchar](4000) NULL,
    [gold_updated_at] [datetime2](7) NULL,
    [file_path] [nvarchar](1000) NOT NULL
) ON [PRIMARY]
GO

-- Seed dei documenti contabili, costruito per i casi delle viste vwAppioFinalReport*:
--   · anni = 2026, 2025; trimestri distinti (indipendenti dall'anno) = 1, 2, 4;
--   · 2026 Q2, OB1: DUE documenti nello stesso trimestre, APPIO-FR-001 (due righe, 10 + 10) e
--     APPIO-FR-002 (una riga, 5). ImportoTotale delle viste è partizionato per trimestre e NON per
--     Numero: vale 25 su tutte e tre le righe, non 20 e 5. Il seed lo rende visibile apposta;
--   · 2026 Q2, OB2: APPIO-FR-003 (2);
--   · 2026 Q1, OB1: APPIO-FR-004 (3);
--   · 2025 Q4, OB4 (omonimo di OB1): APPIO-FR-005 (1);
--   · APPIO-OB9 non esiste in appio.contracts: la INNER JOIN delle viste lo esclude.
--   Data e Riferimento_data sono nvarchar: dall'11/11/2026 le viste li convertono con
--   TRY_CONVERT(date, TRIM(x), 23), cioè 'AAAA-MM-GG'. APPIO-FR-005 ha apposta Data con spazi
--   (' 2026-01-15 ' -> 2026-01-15, prova del TRIM) e Riferimento_data in formato errato
--   ('31/12/2025' -> NULL: la vista non si rompe).
--   file_path (dal 12/11/2026 nella vista, NON esposto dall'API): un file per trimestre, ripetuto
--   uguale su tutte le righe, nella forma abfss://container@account.dfs.core.windows.net/percorso
--   (account sintetico). Lo legge solo il download api/appio/financialreports/blob/download.
--   gold_updated_at (dal 12/11/2026 nella vista vwAppioFinalReport, esposto dall'API) vale
--   2026-07-16 08:00 su tutte le righe tranne APPIO-FR-005, che lo ha NULL apposta.
IF NOT EXISTS (SELECT 1 FROM appio.messages_final_report WHERE contract_id LIKE 'APPIO-OB%')
INSERT INTO appio.messages_final_report
 ([year], [quarter], contract_id, Tipo_doc, CODICE_AGGIUNTIVO, Valuta, ID, Numero, [Data], Progressivo_Riga, Codice_articolo,
  Descrizione_riga, Q_ta, Importo, Cod_iva, Condizioni, CAUSALE, Riferimento_data, internalistitutionid, billing_iteration, file_path, gold_updated_at) VALUES
 (2026, 2, 'APPIO-OB1', 'TD01', 'AGG1', 'EUR', 1, 'APPIO-FR-001', '2026-07-15', 1, 'ART-A', 'Messaggi',  1000, 10.00, 'IVA22', 'TP02', 'Servizio APP IO Q2', '2026-06-30', 'APPIO-ENTE-1', 1, 'abfss://appioexport@stappioseed.dfs.core.windows.net/export/sap/2026/2/appio-final_report-2026-q2.csv', '2026-07-16T08:00:00'),
 (2026, 2, 'APPIO-OB1', 'TD01', 'AGG1', 'EUR', 1, 'APPIO-FR-001', '2026-07-15', 2, 'ART-B', 'Pagamenti',  500, 10.00, 'IVA22', 'TP02', 'Servizio APP IO Q2', '2026-06-30', 'APPIO-ENTE-1', 1, 'abfss://appioexport@stappioseed.dfs.core.windows.net/export/sap/2026/2/appio-final_report-2026-q2.csv', '2026-07-16T08:00:00'),
 (2026, 2, 'APPIO-OB1', 'TD04', 'AGG1', 'EUR', 2, 'APPIO-FR-002', '2026-07-20', 1, 'ART-A', 'Rettifica',  500,  5.00, 'IVA22', 'TP02', 'Nota Q2',            '2026-06-30', 'APPIO-ENTE-1', 1, 'abfss://appioexport@stappioseed.dfs.core.windows.net/export/sap/2026/2/appio-final_report-2026-q2.csv', '2026-07-16T08:00:00'),
 (2026, 2, 'APPIO-OB2', 'TD01', 'AGG2', 'EUR', 3, 'APPIO-FR-003', '2026-07-15', 1, 'ART-A', 'Messaggi',   200,  2.00, 'IVA22', 'TP02', 'Servizio APP IO Q2', '2026-06-30', 'APPIO-ENTE-2', 1, 'abfss://appioexport@stappioseed.dfs.core.windows.net/export/sap/2026/2/appio-final_report-2026-q2.csv', '2026-07-16T08:00:00'),
 (2026, 1, 'APPIO-OB1', 'TD01', 'AGG1', 'EUR', 4, 'APPIO-FR-004', '2026-04-15', 1, 'ART-A', 'Messaggi',   300,  3.00, 'IVA22', 'TP02', 'Servizio APP IO Q1', '2026-03-31', 'APPIO-ENTE-1', 1, 'abfss://appioexport@stappioseed.dfs.core.windows.net/export/sap/2026/1/appio-final_report-2026-q1.csv', '2026-07-16T08:00:00'),
 (2025, 4, 'APPIO-OB4', 'TD01', 'AGG4', 'EUR', 5, 'APPIO-FR-005', ' 2026-01-15 ', 1, 'ART-A', 'Messaggi',   100,  1.00, 'IVA22', 'TP02', 'Servizio APP IO Q4', '31/12/2025', 'APPIO-ENTE-4', 1, 'abfss://appioexport@stappioseed.dfs.core.windows.net/export/sap/2025/4/appio-final_report-2025-q4.csv', NULL),
 (2026, 2, 'APPIO-OB9', 'TD01', 'AGG9', 'EUR', 9, 'APPIO-FR-009', '2026-07-15', 1, 'ART-A', 'Messaggi',   100,  1.00, 'IVA22', 'TP02', 'Orfano',             '2026-06-30', 'APPIO-ENTE-9', 1, 'abfss://appioexport@stappioseed.dfs.core.windows.net/export/sap/2026/2/appio-final_report-2026-q2.csv', '2026-07-16T08:00:00');
GO
