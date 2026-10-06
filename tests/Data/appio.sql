-- =============================================================================================
-- APP IO (PF-908): anagrafica dei contratti del prodotto APP IO, letta dagli endpoint dell'area
-- tramite la vista [be].[vwAppioContracts] (proiezione 1:1, in views/92_vwAppioContracts.sql).
--
-- Deve precedere views/: per le viste SQL Server non fa deferred name resolution, quindi il
-- CREATE VIEW fallisce se appio.Contracts non esiste ancora.
--
-- DDL della tabella: reale, fornita il 01/10/2026 e riprodotta as-is (salvo la guardia di
-- idempotenza). La PK è (contract_id, year_quarter): lo stesso contratto compare una volta per
-- trimestre, come in ppa.Contracts.
--
-- Formato dei valori verificato su righe reali il 06/10/2026 (DEV): year_quarter = 'AAAA_T',
-- year_month = 'AAAAMM' senza separatore, ed è il mese SUCCESSIVO alla fine del trimestre
-- (2026_1 -> 202604), come in ppa.Contracts. Ne dipendono il default "trimestre più
-- recente" (MAX(year_quarter), confronto fra stringhe) e il calcolo degli anni (Split("_")).
-- Se in produzione il formato è diverso, va allineato qui.
--
-- Dati tutti SINTETICI: questo file sta in un repository pubblico.
-- =============================================================================================

IF SCHEMA_ID('appio') IS NULL
    EXEC('CREATE SCHEMA appio');
GO

IF OBJECT_ID('appio.Contracts', 'U') IS NULL
CREATE TABLE [appio].[Contracts](
    [contract_id] [nvarchar](50) NOT NULL,
    [name] [nvarchar](500) NULL,
    [tax_code] [nvarchar](50) NULL,
    [vat_code] [nvarchar](50) NULL,
    [vat_group] [decimal](18, 2) NULL,
    [sdi_code] [nvarchar](50) NULL,
    [year_month] [varchar](7) NULL,
    [year_quarter] [nvarchar](10) NOT NULL,
 CONSTRAINT [PK_Contracts] PRIMARY KEY CLUSTERED
(
    [contract_id] ASC,
    [year_quarter] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

-- Seed costruito per i filtri di AppIoContrattiSQLBuilder:
--   · trimestre più recente = 2026_2, con 3 contratti (C1, C2, C3): è il default senza filtro;
--   · C1 compare su tre trimestri (la PK lo permette): la ricerca per trimestre deve isolarli;
--   · C4 esiste solo in 2025_4: escluso dal default, incluso chiedendo quel trimestre;
--   · "AppIO Test" è nel nome di C1, C2 e C4 ma non di C3: ricerca per nome;
--   · C3 ha vat_group e sdi_code NULL: il mapping non deve rompersi sui NULL;
--   · anni distinti = 2026, 2025; trimestri distinti = 2026_2, 2026_1, 2025_4.
IF NOT EXISTS (SELECT 1 FROM appio.Contracts WHERE contract_id LIKE 'APPIO-C%')
INSERT INTO appio.Contracts (contract_id, name, tax_code, vat_code, vat_group, sdi_code, year_month, year_quarter) VALUES
 ('APPIO-C1', 'Comune Alfa AppIO Test',  '00000000001', '00000000001', 0, 'AAAAAA1', '202601', '2025_4'),
 ('APPIO-C1', 'Comune Alfa AppIO Test',  '00000000001', '00000000001', 0, 'AAAAAA1', '202604', '2026_1'),
 ('APPIO-C1', 'Comune Alfa AppIO Test',  '00000000001', '00000000001', 0, 'AAAAAA1', '202607', '2026_2'),
 ('APPIO-C2', 'Comune Beta AppIO Test',  '00000000002', '00000000002', 1, 'BBBBBB2', '202607', '2026_2'),
 ('APPIO-C3', 'Regione Gamma',           '00000000003', '00000000003', NULL, NULL,  '202607', '2026_2'),
 ('APPIO-C4', 'Comune Delta AppIO Test', '00000000004', '00000000004', 0, 'DDDDDD4', '202601', '2025_4');
GO

-- =============================================================================================
-- Documenti contabili APP IO (PF-908): appio.FinancialReports e appio.FinancialReportPositions,
-- lette tramite be.vwAppioFinancialReport e be.vwAppioFinancialReportPositions
-- (views/92b_*, views/92c_*) da AppIoFinancialReportSQLBuilder.
--
-- DDL: colonne, tipi, nullabilità e PK reali (INFORMATION_SCHEMA del DB DEV, 06/10/2026). I nomi
-- dei vincoli PK non sono stati estratti: quelli qui sotto sono nostri. Le PK sono
--   FinancialReports         (recipient_id, year_quarter, codice_articolo)
--   FinancialReportPositions (contract_id, year_quarter, codice_articolo, progressivo_riga)
-- e sono ciò che garantisce che le LEFT JOIN del builder non moltiplichino le righe.
-- Il 06/10/2026 entrambe le viste erano vuote in DEV: formati di numero e articolo sono sintetici.
-- =============================================================================================

IF OBJECT_ID('appio.FinancialReports', 'U') IS NULL
CREATE TABLE [appio].[FinancialReports](
    [name] [nvarchar](max) NULL,
    [category] [nvarchar](100) NOT NULL,
    [current_trx] [int] NULL,
    [value] [decimal](38, 14) NULL,
    [codice_articolo] [nvarchar](100) NOT NULL,
    [year_quarter] [nvarchar](10) NOT NULL,
    [recipient_id] [nvarchar](20) NOT NULL,
 CONSTRAINT [PK_AppioFinancialReports] PRIMARY KEY CLUSTERED
(
    [recipient_id] ASC,
    [year_quarter] ASC,
    [codice_articolo] ASC
)
) ON [PRIMARY]
GO

IF OBJECT_ID('appio.FinancialReportPositions', 'U') IS NULL
CREATE TABLE [appio].[FinancialReportPositions](
    [contract_id] [nvarchar](25) NOT NULL,
    [tipo_doc] [nvarchar](max) NULL,
    [vat_code] [nvarchar](max) NULL,
    [cod_fisc] [nvarchar](max) NULL,
    [valuta] [nvarchar](max) NULL,
    [id] [int] NULL,
    [numero] [nvarchar](max) NULL,
    [data] [datetime] NULL,
    [codifica_art] [nvarchar](max) NULL,
    [progressivo_riga] [int] NOT NULL,
    [codice_articolo] [nvarchar](200) NOT NULL,
    [descrizione_riga] [nvarchar](max) NULL,
    [prezzo_unit] [nvarchar](max) NULL,
    [q_ta] [int] NULL,
    [importo] [decimal](38, 14) NULL,
    [cod_iva] [nvarchar](max) NULL,
    [percent_iva] [nvarchar](max) NULL,
    [iva] [nvarchar](max) NULL,
    [codice_sdi] [nvarchar](max) NULL,
    [rif_fattura] [nvarchar](max) NULL,
    [year_quarter] [nvarchar](10) NOT NULL,
 CONSTRAINT [PK_AppioFinancialReportPositions] PRIMARY KEY CLUSTERED
(
    [contract_id] ASC,
    [year_quarter] ASC,
    [codice_articolo] ASC,
    [progressivo_riga] ASC
)
) ON [PRIMARY]
GO

-- Seed costruito per i casi di AppIoFinancialReportSQLBuilder:
--   · trimestri con posizioni = 2026_2, 2026_1, 2025_4 (anni 2026, 2025): sono le posizioni a
--     decidere i filtri, come KPMG per pagoPA;
--   · default senza filtri = 2026_2: due documenti, APPIO-FR-001 (C1, due righe) e APPIO-FR-002 (C2);
--   · C1 ha il suo financial report: il nome in griglia viene da lì ("... FR"), non dal contratto;
--   · C2 NON ha financial report: nome dal contratto, categoria NULL;
--   · C5 (2025_4) non ha né contratto né report: nome NULL; vat_code NULL -> VatCode = cod_fisc;
--   · C3 ha una riga di financial report in 2026_2 ma nessuna posizione: compare nel foglio
--     financial-report del download, NON in griglia.
IF NOT EXISTS (SELECT 1 FROM appio.FinancialReportPositions WHERE contract_id LIKE 'APPIO-C%')
INSERT INTO appio.FinancialReportPositions
 (contract_id, tipo_doc, vat_code, cod_fisc, valuta, id, numero, data, codifica_art, progressivo_riga, codice_articolo,
  descrizione_riga, prezzo_unit, q_ta, importo, cod_iva, percent_iva, iva, codice_sdi, rif_fattura, year_quarter) VALUES
 ('APPIO-C1', 'TD01', '00000000001', '00000000001', 'EUR', 1, 'APPIO-FR-001', '2026-07-15', 'INT', 1, 'ART-A',
  'Messaggi', '0,01', 1000, 10.00, 'IVA22', '22', '2,20', 'AAAAAA1', NULL, '2026_2'),
 ('APPIO-C1', 'TD01', '00000000001', '00000000001', 'EUR', 1, 'APPIO-FR-001', '2026-07-15', 'INT', 2, 'ART-B',
  'Pagamenti', '0,02', 500, 10.00, 'IVA22', '22', '2,20', 'AAAAAA1', NULL, '2026_2'),
 ('APPIO-C2', 'TD01', '00000000002', '00000000002', 'EUR', 2, 'APPIO-FR-002', '2026-07-15', 'INT', 1, 'ART-A',
  'Messaggi', '0,01', 200, 2.00, 'IVA22', '22', '0,44', 'BBBBBB2', NULL, '2026_2'),
 ('APPIO-C1', 'TD01', '00000000001', '00000000001', 'EUR', 3, 'APPIO-FR-003', '2026-04-15', 'INT', 1, 'ART-A',
  'Messaggi', '0,01', 300, 3.00, 'IVA22', '22', '0,66', 'AAAAAA1', NULL, '2026_1'),
 ('APPIO-C5', 'TD04', NULL,          '00000000005', 'EUR', 4, 'APPIO-FR-004', '2026-01-15', 'INT', 1, 'ART-A',
  'Messaggi', '0,01', 100, 1.00, 'IVA22', '22', '0,22', NULL, 'APPIO-FR-000', '2025_4');
GO

IF NOT EXISTS (SELECT 1 FROM appio.FinancialReports WHERE recipient_id LIKE 'APPIO-C%')
INSERT INTO appio.FinancialReports (name, category, current_trx, value, codice_articolo, year_quarter, recipient_id) VALUES
 ('Comune Alfa AppIO FR', 'CAT-MSG', 1000, 10.00, 'ART-A', '2026_2', 'APPIO-C1'),
 ('Comune Alfa AppIO FR', 'CAT-PAG', 500,  10.00, 'ART-B', '2026_2', 'APPIO-C1'),
 ('Comune Alfa AppIO FR', 'CAT-MSG', 300,  3.00,  'ART-A', '2026_1', 'APPIO-C1'),
 ('Regione Gamma FR',     'CAT-MSG', 50,   0.50,  'ART-A', '2026_2', 'APPIO-C3');
GO
