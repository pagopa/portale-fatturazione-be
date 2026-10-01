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
-- ⚠️ Formato dei valori NON verificato su righe reali: si assume quello di ppa.Contracts,
-- year_quarter = 'AAAA_T' e year_month = 'AAAA-MM'. Ne dipendono il default "trimestre più
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
 ('APPIO-C1', 'Comune Alfa AppIO Test',  '00000000001', '00000000001', 0, 'AAAAAA1', '2025-12', '2025_4'),
 ('APPIO-C1', 'Comune Alfa AppIO Test',  '00000000001', '00000000001', 0, 'AAAAAA1', '2026-03', '2026_1'),
 ('APPIO-C1', 'Comune Alfa AppIO Test',  '00000000001', '00000000001', 0, 'AAAAAA1', '2026-06', '2026_2'),
 ('APPIO-C2', 'Comune Beta AppIO Test',  '00000000002', '00000000002', 1, 'BBBBBB2', '2026-06', '2026_2'),
 ('APPIO-C3', 'Regione Gamma',           '00000000003', '00000000003', NULL, NULL,  '2026-06', '2026_2'),
 ('APPIO-C4', 'Comune Delta AppIO Test', '00000000004', '00000000004', 0, 'DDDDDD4', '2025-12', '2025_4');
GO
