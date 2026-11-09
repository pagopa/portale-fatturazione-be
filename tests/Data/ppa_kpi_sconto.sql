-- =============================================================================================
-- Sconti KPI dei PSP (prodotto pagoPA): [ppa].[KpiPagamenti_Sconto], letta da EmailPspService in join
-- con [ppa].[KPMG] e [ppa].[Contracts] per il link al report degli sconti (PercSconto > 0).
-- La join del backend e' su recipient_id = KPMG.contract_id (non su psp_id) e su year_quarter ('AAAA_T').
--
-- DDL: reale, fornita il 06/11/2026 e riprodotta as-is (salvo la guardia di idempotenza). Nessuna PK.
-- =============================================================================================

IF SCHEMA_ID('ppa') IS NULL
    EXEC('CREATE SCHEMA ppa');
GO

IF OBJECT_ID('ppa.KpiPagamenti_Sconto', 'U') IS NULL
CREATE TABLE [ppa].[KpiPagamenti_Sconto](
    [psp_id] [nvarchar](max) NULL,
    [recipient_id] [nvarchar](max) NULL,
    [year_quarter] [nvarchar](max) NULL,
    [trx_total] [bigint] NULL,
    [value_total] [decimal](38, 14) NULL,
    [KpiOk] [int] NULL,
    [PercSconto] [decimal](5, 2) NULL,
    [value_discount] [decimal](38, 14) NULL,
    [linkReport] [nvarchar](max) NULL,
    [KpiList] [nvarchar](max) NULL,
    [FlagMQ] [bit] NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
