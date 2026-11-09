-- =============================================================================================
-- Righe dei documenti contabili PSP (prodotto pagoPA): [ppa].[KPMG], attraversata da
-- EmailPspService (GetSenderEmail / GetSenderEmailAdjustment / CountInvioAdjustment) in join con
-- [ppa].[Contracts] e [ppa].[KpiPagamenti_Sconto].
--
-- DDL: reale, fornita il 06/11/2026 e riprodotta as-is (salvo la guardia di idempotenza). La PK e'
-- (contract_id, progressivo_riga, codice_articolo, year_quarter): una riga per articolo del documento,
-- per trimestre.
--
-- RIGHE: ancora nessuna. Arriveranno insieme alle DDL di [ppa].[PspEmail] e [ppa].[KpiPagamenti_Sconto],
-- sintetiche (repository pubblico) e costruite per i tre casi dei test di EmailPspServiceIntegrationTests:
-- email del referente, ripiego sulla mail di cortesia, adjustment. Finche' mancano le altre due tabelle
-- quei test restano gialli (TestDb.SkipSeOggettoAssente).
-- =============================================================================================

IF SCHEMA_ID('ppa') IS NULL
    EXEC('CREATE SCHEMA ppa');
GO

IF OBJECT_ID('ppa.KPMG', 'U') IS NULL
CREATE TABLE [ppa].[KPMG](
    [contract_id] [nvarchar](25) NOT NULL,
    [tipo_doc] [nvarchar](max) NULL,
    [sezionale] [nvarchar](max) NULL,
    [codice_aggiuntivo] [nvarchar](max) NULL,
    [denominazione_destinatario] [nvarchar](max) NULL,
    [indirizzo] [nvarchar](max) NULL,
    [citta] [nvarchar](max) NULL,
    [prov] [nvarchar](max) NULL,
    [cap] [nvarchar](max) NULL,
    [stato] [nvarchar](max) NULL,
    [vat_code] [nvarchar](max) NULL,
    [cod_fisc] [nvarchar](max) NULL,
    [valuta] [nvarchar](max) NULL,
    [id] [int] NULL,
    [numero] [nvarchar](max) NULL,
    [data] [datetime] NULL,
    [bollo] [nvarchar](max) NULL,
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
    [condizioni] [nvarchar](max) NULL,
    [causale] [nvarchar](max) NULL,
    [ind_tipo_riga] [int] NULL,
    [codice_sdi] [nvarchar](max) NULL,
    [num_doc_rif] [nvarchar](max) NULL,
    [data_doc_rif] [nvarchar](max) NULL,
    [tipo_doc_rif] [nvarchar](max) NULL,
    [tipo_dato] [nvarchar](max) NULL,
    [riferimento_testo] [nvarchar](max) NULL,
    [riferimento_numero] [nvarchar](max) NULL,
    [riferimento_data] [datetime] NULL,
    [anno] [nvarchar](max) NULL,
    [rif_fattura] [nvarchar](max) NULL,
    [sconto] [nvarchar](max) NULL,
    [data_limite_pagamento] [nvarchar](max) NULL,
    [banca] [nvarchar](max) NULL,
    [iban_riferimento_per_pagamento] [nvarchar](max) NULL,
    [cig] [nvarchar](max) NULL,
    [indirizzo_pec] [nvarchar](max) NULL,
    [cup] [nvarchar](max) NULL,
    [tipo_doc_rif1] [nvarchar](max) NULL,
    [year_quarter] [nvarchar](10) NOT NULL,
 CONSTRAINT [PK__KPMG__A7C8AB96C9D19B5A] PRIMARY KEY CLUSTERED
(
    [contract_id] ASC,
    [progressivo_riga] ASC,
    [codice_articolo] ASC,
    [year_quarter] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
