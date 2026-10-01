-- =============================================================================================
-- Anagrafica PSP (prodotto pagoPA): [ppa].[Contracts], letta direttamente (senza vista) dalle rotte
-- api/v2/pagopa/psps* tramite PSPSQLBuilder.
--
-- DDL: reale, fornita il 01/10/2026 e riprodotta as-is (salvo la guardia di idempotenza). La PK è
-- (contract_id, year_quarter): lo stesso contratto compare una volta per trimestre.
--
-- Formato dei valori ricalcato su righe reali (01/10/2026), con dati tutti SINTETICI: questo file sta
-- in un repository pubblico, e le righe reali contengono PEC, email nominative e codici fiscali.
--   · year_quarter 'AAAA_T' (2026_1), year_month 'AAAAMM' (202604) — senza trattino, benché la colonna
--     sia varchar(7);
--   · provider_names: elenco separato da virgole che mescola BIC e codici 'ABInnnnn', in qualunque
--     ordine, talvolta vuoto; abi: il codice senza prefisso ('08457');
--   · sdd è una STRINGA ('FALSE'), non un bit; vat_group 1.00 / 0.00.
-- =============================================================================================

IF SCHEMA_ID('ppa') IS NULL
    EXEC('CREATE SCHEMA ppa');
GO

IF OBJECT_ID('ppa.Contracts', 'U') IS NULL
CREATE TABLE [ppa].[Contracts](
    [contract_id] [nvarchar](100) NOT NULL,
    [document_name] [nvarchar](max) NULL,
    [provider_names] [nvarchar](510) NULL,
    [signed_date] [date] NULL,
    [contract_type] [nvarchar](200) NULL,
    [name] [nvarchar](1000) NULL,
    [abi] [nvarchar](100) NULL,
    [tax_code] [nvarchar](100) NULL,
    [vat_code] [nvarchar](100) NULL,
    [vat_group] [decimal](18, 2) NULL,
    [pec_mail] [nvarchar](1000) NULL,
    [courtesy_mail] [nvarchar](1000) NULL,
    [referentefattura_mail] [nvarchar](1000) NULL,
    [sdd] [nvarchar](100) NULL,
    [sdi_code] [nvarchar](100) NULL,
    [membership_id] [nvarchar](100) NULL,
    [recipient_id] [nvarchar](100) NULL,
    [year_month] [varchar](7) NULL,
    [year_quarter] [nvarchar](20) NOT NULL,
 CONSTRAINT [PK_Contracts] PRIMARY KEY CLUSTERED
(
    [contract_id] ASC,
    [year_quarter] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

-- Seed costruito per i filtri di PSPQueryGetByRicercaPersistence e PSPQueryGetByNamePersistence:
--   · trimestre più recente = 2026_1, con 5 contratti (T01…T05): è il default senza filtro;
--   · T01 compare anche in 2025_4, T06 SOLO in 2025_4 (escluso dal default);
--   · filtro ABI, un contratto per ramo del predicato a cinque OR:
--       T01  provider 'ABI01234'           → '%ABI' + abi + '%'
--       T02  provider 'BICBETA1,ABI05678'  → '%ABI' + abi + '%' (codice in coda)
--       T03  provider 'ABI01235,BICGAMMA'  → '%ABI' + abi + '%' (codice in testa), e falso positivo
--            per prefisso: cercando '0123' escono sia T01 sia T03
--       T04  provider NULL, abi '09999'    → solo abi = @abi
--       T05  provider '', abi 'ABI07777'   → solo abi = 'ABI' + @abi
--   · membership_id: grp-1 (T01, T02), grp-2 (T03, T04); recipient_id univoci;
--   · "Banca" nel nome di T01, T02, T06; T05 con signed_date, vat_group e sdi_code NULL.
IF NOT EXISTS (SELECT 1 FROM ppa.Contracts WHERE contract_id LIKE 'PSP-T%')
INSERT INTO ppa.Contracts
 (contract_id, document_name, provider_names, signed_date, contract_type, name, abi, tax_code, vat_code, vat_group,
  pec_mail, courtesy_mail, referentefattura_mail, sdd, sdi_code, membership_id, recipient_id, year_month, year_quarter) VALUES
 ('PSP-T01', 'CI901_A_Banca Alfa Test', 'ABI01234',          '2020-01-30', 'A', 'Banca Alfa Test',  '01234', '00000000101', '00000000101', 1.00,
  'alfa@pec.invalid',  'alfa@test.invalid',  NULL,                 'FALSE', 'AAAAAA1', 'grp-1', 'rcp-1', '202604', '2026_1'),
 ('PSP-T01', 'CI901_A_Banca Alfa Test', 'ABI01234',          '2020-01-30', 'A', 'Banca Alfa Test',  '01234', '00000000101', '00000000101', 1.00,
  'alfa@pec.invalid',  'alfa@test.invalid',  NULL,                 'FALSE', 'AAAAAA1', 'grp-1', 'rcp-1', '202601', '2025_4'),
 ('PSP-T02', 'CI902_B_Banca Beta Test', 'BICBETA1,ABI05678', '2020-02-07', 'B', 'Banca Beta Test',  '05678', '00000000102', '00000000102', 0.00,
  'beta@pec.invalid',  'beta@test.invalid',  'ref@beta.invalid',   'FALSE', 'BBBBBB2', 'grp-1', 'rcp-2', '202604', '2026_1'),
 ('PSP-T03', 'CI903_A_Cassa Gamma',     'ABI01235,BICGAMMA', '2020-03-12', 'A', 'Cassa Gamma',      '01235', '00000000103', '00000000103', 1.00,
  'gamma@pec.invalid', 'gamma@test.invalid', NULL,                 'FALSE', 'CCCCCC3', 'grp-2', 'rcp-3', '202604', '2026_1'),
 ('PSP-T04', 'CI904_A_Cassa Delta',     NULL,                '2020-03-12', 'A', 'Cassa Delta',      '09999', '00000000104', '00000000104', 1.00,
  'delta@pec.invalid', 'delta@test.invalid', NULL,                 'FALSE', 'DDDDDD4', 'grp-2', 'rcp-4', '202604', '2026_1'),
 ('PSP-T05', 'CI905_B_Istituto Epsilon', '',                 NULL,         'B', 'Istituto Epsilon', 'ABI07777', '00000000105', '00000000105', NULL,
  'eps@pec.invalid',   'eps@test.invalid',   NULL,                 'FALSE', NULL,      'grp-3', 'rcp-5', '202604', '2026_1'),
 ('PSP-T06', 'CI906_A_Banca Zeta Test', 'ABI06666',          '2019-12-20', 'A', 'Banca Zeta Test',  '06666', '00000000106', '00000000106', 0.00,
  'zeta@pec.invalid',  'zeta@test.invalid',  NULL,                 'FALSE', 'FFFFFF6', 'grp-4', 'rcp-6', '202601', '2025_4');
GO
