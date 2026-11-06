-- =============================================================================================
-- Seed DB locale per l'Invio a SAP (PF-965, figlio di PF-735).
--
-- Tabelle: testo degli script di DATA (branch DBProj dbproj-pf-735-json-to-SAP, commit 208c761),
-- con il solo IF OBJECT_ID per l'idempotenza. pfd.Messaggi: DDL reale del progetto DB
-- (PagopaDBproj/pfd/Tables/Messaggi.sql), che mancava nel seed.
-- Lookup: righe reali di pfw.TipoAzione e pfw.TipoStatoFattura.
-- Dati: SINTETICI sull'anno dedicato 2003 (il repository e' pubblico: nessun dato copiato da DEV).
-- I casi coperti sono descritti in be-invio-sap.md §13.2; le costanti attese stanno in
-- tests/PortaleFatture.BE.IntegrationTest/InvioSap/InvioSapTestData.cs.
--
-- Deve girare DOPO gestione_fatture.sql (FK verso pfd.FattureTestata, enti di test) e PRIMA di
-- views/ (le viste A0-A3 leggono queste tabelle).
-- =============================================================================================

IF OBJECT_ID('pfw.RegistroInvioSAP', 'U') IS NULL
CREATE TABLE [pfw].[RegistroInvioSAP] (
    [BatchId]           INT            IDENTITY (1, 1) NOT NULL,
    [Stato]             NVARCHAR (50)  NOT NULL,
    [IdUtente]          NVARCHAR (50)  NOT NULL,
    [DataAvvio]         DATETIME2 (7)  NOT NULL,
    [DataCompletamento] DATETIME2 (7)  NULL,
    [NomeFile]          NVARCHAR (255) NULL,
    [PathFile]          NVARCHAR (MAX) NULL,
    [CountFatture]      INT            NOT NULL,
    [DescrizioneErrore] NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_RegistroInvioSAP] PRIMARY KEY CLUSTERED ([BatchId] ASC)
);
GO

IF OBJECT_ID('pfw.TipoStatoFattura', 'U') IS NULL
CREATE TABLE [pfw].[TipoStatoFattura] (
    [IdStato]     INT            NOT NULL,
    [Descrizione] NVARCHAR (255) NOT NULL,
    CONSTRAINT [PK_TipoStatoFattura] PRIMARY KEY CLUSTERED ([IdStato] ASC)
);
GO

IF OBJECT_ID('pfw.DettaglioInvioSAP', 'U') IS NULL
CREATE TABLE [pfw].[DettaglioInvioSAP] (
    [FkBatchId]         INT            NOT NULL,
    [FkIdFattura]       BIGINT         NOT NULL,
    [FkStatoPrecedente] INT            NULL,
    [FkStato]           INT            NOT NULL,
    [NumeroDocStorno]   NVARCHAR (255) NULL,
    [Nota]              JSON           NOT NULL,
    CONSTRAINT [PK_DettaglioInvioSAP_1] PRIMARY KEY CLUSTERED ([FkBatchId] ASC, [FkIdFattura] ASC),
    CONSTRAINT [FK_BatchId] FOREIGN KEY ([FkBatchId]) REFERENCES [pfw].[RegistroInvioSAP] ([BatchId]),
    CONSTRAINT [FK_IdFattura] FOREIGN KEY ([FkIdFattura]) REFERENCES [pfd].[FattureTestata] ([IdFattura]),
    CONSTRAINT [FK_Stato] FOREIGN KEY ([FkStato]) REFERENCES [pfw].[TipoStatoFattura] ([IdStato])
);
GO

IF OBJECT_ID('pfw.LockAzionePF', 'U') IS NULL
CREATE TABLE [pfw].[LockAzionePF] (
    [FkIdAzione]  UNIQUEIDENTIFIER NOT NULL,
    [lockStart]   DATETIME2 (7)    NOT NULL,
    [lockTimeout] DATETIME2 (7)    NOT NULL,
    [FkBatchId]   INT              NULL,
    CONSTRAINT [PK_LockAzionePF] PRIMARY KEY CLUSTERED ([FkIdAzione] ASC)
);
GO

IF OBJECT_ID('pfw.TipoAzione', 'U') IS NULL
CREATE TABLE [pfw].[TipoAzione] (
    [IdAzione]       UNIQUEIDENTIFIER NOT NULL,
    [Descrizione]    NVARCHAR (255)   NOT NULL,
    [Timeout_millis] BIGINT           NOT NULL,
    CONSTRAINT [PK_TipoAzione] PRIMARY KEY CLUSTERED ([IdAzione] ASC)
);
GO

IF OBJECT_ID('pfd.Messaggi', 'U') IS NULL
CREATE TABLE [pfd].[Messaggi] (
    [IdMessaggio]        BIGINT         IDENTITY (1, 1) NOT NULL,
    [IdEnte]             NVARCHAR (50)  NULL,
    [IdUtente]           NVARCHAR (255) NOT NULL,
    [Json]               TEXT           NULL,
    [Anno]               INT            NULL,
    [Mese]               INT            NULL,
    [Prodotto]           NVARCHAR (255) NOT NULL,
    [GruppoRuolo]        NVARCHAR (255) NULL,
    [Auth]               NVARCHAR (255) NOT NULL,
    [Stato]              SMALLINT       NOT NULL,
    [DataInserimento]    DATETIME       NOT NULL,
    [DataStepCorrente]   DATETIME       NULL,
    [LinkDocumento]      NVARCHAR (550) NULL,
    [ContentLanguage]    NVARCHAR (550) NOT NULL,
    [ContentType]        NVARCHAR (550) NULL,
    [TipologiaDocumento] NVARCHAR (550) NOT NULL,
    [CategoriaDocumento] NVARCHAR (550) NULL,
    [Lettura]            BIT            NOT NULL,
    [Hash]               NVARCHAR (64)  NOT NULL,
    [FKIdReport]         BIGINT         NULL,
    CONSTRAINT [PK_Messaggi] PRIMARY KEY CLUSTERED ([IdMessaggio] ASC)
);
GO

-- ---------------------------------------------------------------------------------------------
-- Lookup (righe reali)
-- ---------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM pfw.TipoStatoFattura)
INSERT INTO pfw.TipoStatoFattura (IdStato, Descrizione) VALUES
 (0, 'FATTURA POSTICIPATA'),
 (1, 'FATTURA RIPRISTINATA'),
 (2, 'FATTURA CANCELLATA'),
 (3, 'FATTURA ELIMINATA'),
 (4, 'FATTURA NON INVIATA'),
 (5, 'FATTURA INVIATA');
GO

IF NOT EXISTS (SELECT 1 FROM pfw.TipoAzione WHERE IdAzione = '0B102032-99F2-4AF9-BC5E-EBE844171DCC')
INSERT INTO pfw.TipoAzione (IdAzione, Descrizione, Timeout_millis) VALUES
 ('0B102032-99F2-4AF9-BC5E-EBE844171DCC', 'GeneraJsonFatture', 3600000);
GO

-- ---------------------------------------------------------------------------------------------
-- Fatture 2003 (be-invio-sap.md §13.2)
--   9501 ANTICIPO 2003/1            inviata, solo migrazione 9001      -> assente dalle re-inviate (C3)
--   9502 PRIMO SALDO 2003/1         inviata, 9001 + 9003 ERRORE + 9004 -> conteggio 2, 3 note (C4, C8, C13)
--   9503 PRIMO SALDO 2003/1 TD04    inviata, 9001, IdDocumento ''      -> C11, ente 2
--   9504 VAR. SEMESTRALE 2003/7     inviata, 9005                      -> un solo invio
--   9505, 9506 PRIMO SALDO 2003/2   da inviare                         -> rigo DA_INVIARE, primo invio (C1)
--   9507 PRIMO SALDO 2003/2         inviata, 9002                      -> esclusa dal primo invio
--   9508 PRIMO SALDO 2003/2         NULL (in elaborazione)             -> esclusa, rigo IN_ELABORAZIONE (C2)
--   9509 SECONDO SALDO 2003/2       da inviare                         -> esclusa dal primo invio di PRIMO SALDO
--   9510 ACCONTO 2003/3, ente PagoPA da inviare                        -> la griglia non filtra l'ente (C14)
-- ---------------------------------------------------------------------------------------------
SET IDENTITY_INSERT pfd.FattureTestata ON;
IF NOT EXISTS (SELECT 1 FROM pfd.FattureTestata WHERE IdFattura BETWEEN 9501 AND 9510)
INSERT INTO pfd.FattureTestata
 (IdFattura, FkIdEnte, FkTipologiaFattura, AnnoRiferimento, MeseRiferimento, FatturaInviata,
  FkProdotto, FkIdTipoDocumento, DataFattura, IdentificativoFattura, TotaleFattura, Divisa, MetodoPagamento,
  Progressivo, IdDocumento, Semestre)
VALUES
 (9501, '11111111-1111-1111-1111-111111111111', 'ANTICIPO',        2003, 1, 1,    'prod-pn', 'TD01', '2003-02-10', 'IT-9501', 101.00, 'EUR', 'MP5', 9501, NULL, NULL),
 (9502, '11111111-1111-1111-1111-111111111111', 'PRIMO SALDO',     2003, 1, 1,    'prod-pn', 'TD01', '2003-02-10', 'IT-9502', 102.00, 'EUR', 'MP5', 9502, NULL, NULL),
 (9503, '22222222-2222-2222-2222-222222222222', 'PRIMO SALDO',     2003, 1, 1,    'prod-pn', 'TD04', '2003-02-10', 'IT-9503', -10.00, 'EUR', 'MP5', 9503, '',   NULL),
 (9504, '11111111-1111-1111-1111-111111111111', 'VAR. SEMESTRALE', 2003, 7, 1,    'prod-pn', 'TD01', '2003-08-10', 'IT-9504', 104.00, 'EUR', 'MP5', 9504, NULL, '2003_1'),
 (9505, '11111111-1111-1111-1111-111111111111', 'PRIMO SALDO',     2003, 2, 0,    'prod-pn', 'TD01', '2003-03-10', 'IT-9505', 105.00, 'EUR', 'MP5', 9505, NULL, NULL),
 (9506, '22222222-2222-2222-2222-222222222222', 'PRIMO SALDO',     2003, 2, 0,    'prod-pn', 'TD01', '2003-03-10', 'IT-9506', 106.00, 'EUR', 'MP5', 9506, NULL, NULL),
 (9507, '11111111-1111-1111-1111-111111111111', 'PRIMO SALDO',     2003, 2, 1,    'prod-pn', 'TD01', '2003-03-10', 'IT-9507', 107.00, 'EUR', 'MP5', 9507, NULL, NULL),
 (9508, '22222222-2222-2222-2222-222222222222', 'PRIMO SALDO',     2003, 2, NULL, 'prod-pn', 'TD01', '2003-03-10', 'IT-9508', 108.00, 'EUR', 'MP5', 9508, NULL, NULL),
 (9509, '11111111-1111-1111-1111-111111111111', 'SECONDO SALDO',   2003, 2, 0,    'prod-pn', 'TD01', '2003-03-10', 'IT-9509', 109.00, 'EUR', 'MP5', 9509, NULL, NULL),
 (9510, '4a4149af-172e-4950-9cc8-63ccc9a6d865', 'ACCONTO',         2003, 3, 0,    'prod-pn', 'TD01', '2003-04-10', 'IT-9510', 110.00, 'EUR', 'MP5', 9510, NULL, NULL);
SET IDENTITY_INSERT pfd.FattureTestata OFF;
GO

-- ---------------------------------------------------------------------------------------------
-- Batch 9001-9005
--   9001 migrazione COMPLETATO (data con spazio, Azione GeneraJson: C7)
--   9002 primo invio COMPLETATO del rigo PRIMO SALDO 2003/2 (solo 9507)
--   9003 re-invio in ERRORE: DataCompletamento NULL, DescrizioneErrore valorizzata (C4)
--   9004 re-invio COMPLETATO, stesso NomeFile di 9005 con PathFile diverso (C6)
--   9005 primo invio COMPLETATO di VAR. SEMESTRALE
-- ---------------------------------------------------------------------------------------------
SET IDENTITY_INSERT pfw.RegistroInvioSAP ON;
IF NOT EXISTS (SELECT 1 FROM pfw.RegistroInvioSAP WHERE BatchId BETWEEN 9001 AND 9005)
INSERT INTO pfw.RegistroInvioSAP
 (BatchId, Stato, IdUtente, DataAvvio, DataCompletamento, NomeFile, PathFile, CountFatture, DescrizioneErrore)
VALUES
 (9001, 'COMPLETATO', 'seed-pf965', '2003-03-01T09:00:00', '2003-03-01T09:05:00', 'FatturePagoPa_migrazione.json', 'https://example.invalid/sap/9001.json', 3, NULL),
 (9002, 'COMPLETATO', 'seed-pf965', '2003-04-01T09:00:00', '2003-04-01T09:05:00', 'FatturePagoPa_20032PRIMO SALDO.json', 'https://example.invalid/sap/9002.json', 1, NULL),
 (9003, 'ERRORE',     'seed-pf965', '2003-04-02T09:00:00', NULL,                  NULL, NULL, 1, 'Errore sintetico del seed'),
 (9004, 'COMPLETATO', 'seed-pf965', '2003-04-03T09:00:00', '2003-04-03T09:05:00', 'FatturePagoPa_seed.json', 'https://example.invalid/sap/9004.json', 1, NULL),
 (9005, 'COMPLETATO', 'seed-pf965', '2003-09-01T09:00:00', '2003-09-01T09:05:00', 'FatturePagoPa_seed.json', 'https://example.invalid/sap/9005.json', 1, NULL);
SET IDENTITY_INSERT pfw.RegistroInvioSAP OFF;
GO

IF NOT EXISTS (SELECT 1 FROM pfw.DettaglioInvioSAP WHERE FkBatchId BETWEEN 9001 AND 9005)
INSERT INTO pfw.DettaglioInvioSAP (FkBatchId, FkIdFattura, FkStatoPrecedente, FkStato, NumeroDocStorno, Nota) VALUES
 (9001, 9501, 4, 5, NULL,           '{"Data":"2003-03-01 09:00:00.147","Testo":"Migrazione seed","Azione":"GeneraJson"}'),
 (9001, 9502, 4, 5, NULL,           '{"Data":"2003-03-01 09:00:00.147","Testo":"Migrazione seed","Azione":"GeneraJson"}'),
 (9001, 9503, 4, 5, NULL,           '{"Data":"2003-03-01 09:00:00.147","Testo":"Migrazione seed","Azione":"GeneraJson"}'),
 (9002, 9507, 4, 5, NULL,           '{"Data":"2003-04-01T09:00:00.1234567","Testo":"Primo invio seed","Azione":"GeneraJsonFatture"}'),
 (9003, 9502, 5, 5, 'NC-2003-0001', '{"Data":"2003-04-02T09:00:00.1234567","Testo":"Re-invio seed in errore","Azione":"GeneraJsonFatture"}'),
 (9004, 9502, 5, 5, 'NC-2003-0002', '{"Data":"2003-04-03T09:00:00.1234567","Testo":"Re-invio seed completato","Azione":"GeneraJsonFatture"}'),
 (9005, 9504, 4, 5, NULL,           '{"Data":"2003-09-01T09:00:00.1234567","Testo":"Primo invio seed VAR","Azione":"GeneraJsonFatture"}');
GO

-- Riga di lock SCADUTA e non cancellata (C9): la vista non la mostra, ma la PK su FkIdAzione
-- rifiuta un nuovo INSERT -> 409 LOCK_NON_RILASCIATO. I test che generano la tolgono prima con
-- InvioSapTestData.RilasciaLockComeData(), come farebbe DATA.
IF NOT EXISTS (SELECT 1 FROM pfw.LockAzionePF WHERE FkIdAzione = '0B102032-99F2-4AF9-BC5E-EBE844171DCC')
INSERT INTO pfw.LockAzionePF (FkIdAzione, lockStart, lockTimeout, FkBatchId) VALUES
 ('0B102032-99F2-4AF9-BC5E-EBE844171DCC', '2020-01-01T09:00:00', '2020-01-01T10:00:00', 9003);
GO
