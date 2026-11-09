-- =============================================================================================
-- Tracciato delle email ai PSP (prodotto pagoPA): [ppa].[PspEmail]. EmailPspService ci scrive il
-- tracciato di ogni invio (InsertTracciatoEmail) e lo rilegge in GetSenderEmail per ricavare il
-- referente e la mail di cortesia.
--
-- DDL: reale, fornita il 06/11/2026 e riprodotta as-is (salvo la guardia di idempotenza). Nessuna PK.
-- Comprende gia' Oggetto/Corpo/Link: alter_ppa_pspemail_add_nullable_columns.sql, che li aggiunge solo
-- se mancano, qui non fa nulla. Va eseguito DOPO questo file (v. entrypoint.sh).
--
-- RIGHE: ancora nessuna, arriveranno insieme a [ppa].[KpiPagamenti_Sconto] (v. ppa_kpmg.sql).
-- =============================================================================================

IF SCHEMA_ID('ppa') IS NULL
    EXEC('CREATE SCHEMA ppa');
GO

IF OBJECT_ID('ppa.PspEmail', 'U') IS NULL
CREATE TABLE [ppa].[PspEmail](
    [IdContratto] [nvarchar](200) NOT NULL,
    [Tipologia] [nvarchar](20) NOT NULL,
    [Anno] [int] NOT NULL,
    [Trimestre] [nvarchar](20) NOT NULL,
    [DataEvento] [nvarchar](250) NOT NULL,
    [Email] [nvarchar](250) NOT NULL,
    [Messaggio] [nvarchar](2000) NOT NULL,
    [RagioneSociale] [nvarchar](1000) NOT NULL,
    [Invio] [bit] NOT NULL,
    [Oggetto] [nvarchar](max) NULL,
    [Corpo] [nvarchar](max) NULL,
    [Link] [nvarchar](max) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
