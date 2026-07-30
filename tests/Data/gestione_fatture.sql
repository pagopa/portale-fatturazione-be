-- =============================================================================
-- Seed DB locale per i test CRUD/azione di Gestione Fatture (PF-672).
-- Ricrea il SOTTOINSIEME di schema che le stored procedure be.spGestioneFattura*
-- toccano: schema cfg/be, pfd.FattureTestata(+_Eliminate), cfg.GestioneFatture,
-- uno stub di pfd.EliminaFattura, e alcune fatture seed deterministiche.
--
-- Le 4 SP reali (be.spGestioneFattura{Posticipa,Elimina,Ripristina,Cancella}) NON
-- sono qui: vanno messe nei file tests/Data/sp/*.sql (script autorevoli), eseguiti
-- dall'entrypoint DOPO questo file. Richiede SQL Server 2025 per il tipo nativo json.
-- =============================================================================

-- schema mancanti (pfw/pfd/stg/ppa sono gia' creati da setup.sql)
IF SCHEMA_ID('cfg') IS NULL EXEC ('CREATE SCHEMA cfg;');
IF SCHEMA_ID('be')  IS NULL EXEC ('CREATE SCHEMA be;');

-- pfd.FattureTestata: DDL REALE (estratto dal DB il 2026-07-24), non piu' uno stub.
-- Due scelte deliberate rispetto all'originale:
--  1) le 4 FOREIGN KEY sono omesse. Due puntano a tabelle che nel seed non esistono
--     (pfd.FattureTipoDocumento, pfd.FattureTipologia) e di cui non abbiamo il DDL; quella verso
--     pfd.Enti bloccherebbe i test che usano enti sintetici (Guid casuali) di proposito.
--  2) IDENTITY e' mantenuta perche' e' reale e cambia il comportamento degli insert: gli id
--     deterministici del seed richiedono SET IDENTITY_INSERT (v. sotto e negli helper dei test).
-- Nota di fedelta': FkTipologiaFattura e' nvarchar(15) qui e nvarchar(30) in _Eliminate,
-- FkIdEnte nvarchar(50) qui e nvarchar(100) in _Eliminate. L'asimmetria e' del DB reale.
IF OBJECT_ID('pfd.FattureTestata', 'U') IS NULL
CREATE TABLE [pfd].[FattureTestata](
	[IdFattura] [bigint] IDENTITY(1,1) NOT NULL,
	[FkProdotto] [nvarchar](15) NOT NULL,
	[FkIdTipoDocumento] [nvarchar](4) NOT NULL,
	[FkTipologiaFattura] [nvarchar](15) NOT NULL,
	[FkIdEnte] [nvarchar](50) NOT NULL,
	[FkIdDatiFatturazione] [bigint] NULL,
	[DataFattura] [datetime2](7) NOT NULL,
	[IdentificativoFattura] [nvarchar](50) NOT NULL,
	[TotaleFattura] [float] NOT NULL,
	[Divisa] [nvarchar](3) NOT NULL,
	[MetodoPagamento] [nvarchar](3) NOT NULL,
	[AnnoRiferimento] [int] NOT NULL,
	[MeseRiferimento] [int] NOT NULL,
	[CausaleFattura] [nvarchar](250) NULL,
	[Sollecito] [nvarchar](250) NULL,
	[CodiceContratto] [nvarchar](50) NULL,
	[SplitPayment] [bit] NULL,
	[Cup] [nvarchar](15) NULL,
	[Cig] [nvarchar](10) NULL,
	[IdDocumento] [nvarchar](20) NULL,
	[DataDocumento] [datetime] NULL,
	[NumItem] [nvarchar](50) NULL,
	[CodCommessa] [nvarchar](100) NULL,
	[Progressivo] [bigint] NULL,
	[FatturaInviata] [bit] NULL CONSTRAINT [DF_FattureTestata_FatturaInviata] DEFAULT ((0)),
	[Semestre] [nvarchar](15) NULL,
	[FaseFatturazione] [int] NULL,
 CONSTRAINT [PK__FattureT__A29CBA9F4CB91487] PRIMARY KEY CLUSTERED ([IdFattura] ASC)
);

-- pfd.RelTestata: DDL reale (estratto dal DB il 2026-07-24). Serve alla vista
-- be.vwGestioneFattureReport, che ci prende totali notifiche/imponibili/IVA e il flag Caricata.
IF OBJECT_ID('pfd.RelTestata', 'U') IS NULL
CREATE TABLE [pfd].[RelTestata](
	[internal_organization_id] [nvarchar](100) NOT NULL,
	[contract_id] [nvarchar](400) NOT NULL,
	[TipologiaFattura] [nvarchar](40) NOT NULL,
	[year] [int] NOT NULL,
	[month] [int] NOT NULL,
	[TotaleAnalogico] [decimal](9, 2) NULL,
	[TotaleDigitale] [decimal](9, 2) NULL,
	[TotaleNotificheAnalogiche] [int] NULL,
	[TotaleNotificheDigitali] [int] NULL,
	[Totale] [decimal](9, 2) NULL,
	[Iva] [decimal](4, 2) NOT NULL CONSTRAINT [DF_RelTestata_Iva] DEFAULT ((22)),
	[TotaleAnalogicoIva] [decimal](9, 2) NULL,
	[TotaleDigitaleIva] [decimal](9, 2) NULL,
	[TotaleIva] [decimal](9, 2) NULL,
	[Caricata] [tinyint] NULL CONSTRAINT [DF_RelTestata_Caricata] DEFAULT ((0)),
	[AsseverazioneTotaleAnalogico] [decimal](9, 2) NULL,
	[AsseverazioneTotaleDigitale] [decimal](9, 2) NULL,
	[AsseverazioneTotaleNotificheAnalogiche] [int] NULL,
	[AsseverazioneTotaleNotificheDigitali] [int] NULL,
	[AsseverazioneTotale] [decimal](9, 2) NULL,
	[AsseverazioneTotaleAnalogicoIva] [decimal](9, 2) NULL,
	[AsseverazioneTotaleDigitaleIva] [decimal](9, 2) NULL,
	[AsseverazioneTotaleIva] [decimal](9, 2) NULL,
	[RelFatturata] [bit] NOT NULL CONSTRAINT [DF_RelTestata_RelFatturata] DEFAULT ((0)),
	[FlagConguaglio] [nvarchar](50) NULL,
 CONSTRAINT [PK_RelTestata] PRIMARY KEY CLUSTERED
(
	[internal_organization_id] ASC,
	[contract_id] ASC,
	[TipologiaFattura] ASC,
	[year] ASC,
	[month] ASC
)
);
GO

-- pfd.FattureTestata_Eliminate: DDL REALE (estratto dal DB il 2026-07-24). Controllata da CANCELLA.
-- Nel DB reale NON ha chiave primaria ne' IDENTITY (a differenza di FattureTestata): la stessa
-- fattura puo' quindi comparirci piu' volte. Riprodotto fedelmente.
IF OBJECT_ID('pfd.FattureTestata_Eliminate', 'U') IS NULL
CREATE TABLE [pfd].[FattureTestata_Eliminate](
	[IdFattura] [bigint] NOT NULL,
	[FkProdotto] [nvarchar](15) NOT NULL,
	[FkIdTipoDocumento] [nvarchar](4) NOT NULL,
	[FkTipologiaFattura] [nvarchar](30) NOT NULL,
	[FkIdEnte] [nvarchar](100) NOT NULL,
	[FkIdDatiFatturazione] [bigint] NULL,
	[DataFattura] [datetime2](7) NOT NULL,
	[IdentificativoFattura] [nvarchar](50) NOT NULL,
	[TotaleFattura] [float] NOT NULL,
	[Divisa] [nvarchar](3) NOT NULL,
	[MetodoPagamento] [nvarchar](3) NOT NULL,
	[AnnoRiferimento] [int] NOT NULL,
	[MeseRiferimento] [int] NOT NULL,
	[CausaleFattura] [nvarchar](250) NULL,
	[Sollecito] [nvarchar](250) NULL,
	[CodiceContratto] [nvarchar](50) NULL,
	[SplitPayment] [bit] NULL,
	[Cup] [nvarchar](15) NULL,
	[Cig] [nvarchar](10) NULL,
	[IdDocumento] [nvarchar](20) NULL,
	[DataDocumento] [datetime] NULL,
	[NumItem] [nvarchar](50) NULL,
	[CodCommessa] [nvarchar](100) NULL,
	[Progressivo] [bigint] NULL,
	[FlagProceduraWhiteList] [bit] NOT NULL CONSTRAINT [DF_FattureTestata_Eliminate_FlagProceduraWhiteList] DEFAULT ((0)),
	[Semestre] [nvarchar](15) NULL,
	[FatturaInviata] [bit] NULL,
	[FaseFatturazione] [int] NULL
);

-- cfg.GestioneFatture: DDL REALE. Note importanti sullo schema:
--   1) NON esiste una chiave surrogata Id IDENTITY. La PRIMARY KEY e' COMPOSTA su
--      (FkIdEnte, FkTipologiaFattura, Anno, Mese, Stato): due righe con lo stesso stato per lo stesso
--      periodo sono IMPOSSIBILI (il DB le rifiuta).
--   2) NON esistono DataEliminazione ne' IdUtenteEliminazione: l'eliminazione (Stato=3) traccia su
--      IdUtenteInserimento/DataInserimento (il record nasce eliminato).
--   3) Aggiornamento 2026-07-28: FkIdFattura ora e' BIGINT (era int) -> allineato a
--      pfd.FattureTestata.IdFattura, chiuso il disallineamento di tipo. Il lato C# resta da allineare:
--      GestioneFattureAzioneCommandPersistence passa @IdFattura come DbType.Int32 e il command lo
--      tiene come int? -> un IdFattura oltre int.MaxValue non sarebbe rappresentabile.
--   4) Aggiornamento 2026-07-28: Note ha DEFAULT ('[]') -> nasce come ARRAY JSON vuoto, cosi' il
--      JSON_MODIFY(..., 'append $', ...) delle SP concatena correttamente in array anche alla prima nota.
IF OBJECT_ID('cfg.GestioneFatture', 'U') IS NULL
CREATE TABLE [cfg].[GestioneFatture](
	[FkIdEnte] [nvarchar](50) NOT NULL,
	[FkTipologiaFattura] [nvarchar](50) NOT NULL,
	[Anno] [int] NOT NULL,
	[Mese] [int] NOT NULL,
	[FkIdFattura] [bigint] NULL,
	[DataInserimento] [datetime] NOT NULL CONSTRAINT [DF_GestioneFatture_DataInserimento] DEFAULT (getdate()),
	[DataCancellazione] [datetime] NULL,
	[DataRipristino] [datetime] NULL,
	[IdUtenteInserimento] [nvarchar](50) NOT NULL,
	[IdUtenteCancellazione] [nvarchar](50) NULL,
	[IdUtenteRipristino] [nvarchar](50) NULL,
	[Stato] [int] NOT NULL,           -- 0=POSTICIPATA 1=RIPRISTINATA 2=CANCELLATA 3=ELIMINATA
	[Azione] [nvarchar](50) NOT NULL,
	[Note] [json] NULL CONSTRAINT [DF_GestioneFatture_Note] DEFAULT ('[]'),  -- tipo nativo, richiede SQL Server 2025
 CONSTRAINT [PK_GestioneFatture] PRIMARY KEY CLUSTERED
	([FkIdEnte] ASC, [FkTipologiaFattura] ASC, [Anno] ASC, [Mese] ASC, [Stato] ASC)
);
GO

-- Tabelle richieste dalla vista be.vwDocumentiEmessiNonFatturati (righe + config + codici materiali).
-- DDL reali forniti dal team DB; qui SENZA foreign key (convenzione del seed: no parent tables non usate).
IF OBJECT_ID('pfd.FattureRighe', 'U') IS NULL
CREATE TABLE [pfd].[FattureRighe](
	[FkIdFattura] [bigint] NOT NULL,
	[NumeroLinea] [int] NOT NULL,
	[Testo] [nvarchar](max) NULL,
	[CodiceMateriale] [nvarchar](100) NOT NULL,
	[Quantita] [int] NOT NULL,
	[PrezzoUnitario] [float] NOT NULL,
	[Imponibile] [float] NOT NULL,
	[RigaBollo] [bit] NOT NULL,
	[PeriodoRiferimento] [nvarchar](7) NULL,
	[PeriodoFatturazione] [nvarchar](7) NULL
);
GO

IF OBJECT_ID('pfd.FattureRighe_Eliminate', 'U') IS NULL
CREATE TABLE [pfd].[FattureRighe_Eliminate](
	[FkIdFattura] [bigint] NOT NULL,
	[NumeroLinea] [int] NOT NULL,
	[Testo] [nvarchar](max) NULL,
	[CodiceMateriale] [nvarchar](100) NOT NULL,
	[Quantita] [int] NOT NULL,
	[PrezzoUnitario] [float] NOT NULL,
	[Imponibile] [float] NOT NULL,
	[RigaBollo] [bit] NOT NULL,
	[PeriodoRiferimento] [nvarchar](7) NULL,
	[PeriodoFatturazione] [nvarchar](7) NULL
);
GO

IF OBJECT_ID('pfw.FatturaTestataConfig', 'U') IS NULL
CREATE TABLE [pfw].[FatturaTestataConfig](
	[FKProdotto] [nvarchar](15) NOT NULL,
	[FKIdTipoContratto] [bigint] NOT NULL,
	[FkTipologiaFattura] [nvarchar](15) NOT NULL,
	[FKTipoDocumentoFattura] [nvarchar](4) NOT NULL,
	[FKTipoDocumentoNotaCredito] [nvarchar](4) NOT NULL,
	[FKIdMetodoPagamento] [int] NOT NULL,
	[PercentualeAnticipo] [int] NULL,
	[Divisa] [nvarchar](3) NOT NULL,
	[ProceduraSollecito] [nvarchar](5) NULL,
	[DataCreazione] [datetime] NULL,
	[DataModifica] [datetime] NULL,
	[Causale] [nvarchar](50) NULL,
 CONSTRAINT [PK_FatturaTestataConfig] PRIMARY KEY CLUSTERED
	([FKProdotto] ASC, [FKIdTipoContratto] ASC, [FkTipologiaFattura] ASC)
);
GO

IF OBJECT_ID('pfw.CodiciMateriali', 'U') IS NULL
CREATE TABLE [pfw].[CodiciMateriali](
	[IdCodiceMateriale] [int] IDENTITY(1,1) NOT NULL,
	[CodiceMateriale] [nvarchar](100) NOT NULL,
	[Descrizione] [nvarchar](max) NOT NULL,
	[Ordinamento] [int] NULL,
 CONSTRAINT [PK_CodiciMateriali] PRIMARY KEY CLUSTERED ([IdCodiceMateriale] ASC)
);
GO

-- Stub di pfd.EliminaFattura: la SP ELIMINA fa EXEC @rc = pfd.EliminaFattura @IdFattura
-- e prosegue solo se @rc > 0. Qui simuliamo il successo spostando la fattura in _Eliminate.
IF OBJECT_ID('pfd.EliminaFattura', 'P') IS NOT NULL DROP PROCEDURE pfd.EliminaFattura;
GO
CREATE PROCEDURE pfd.EliminaFattura @IdFattura INT
AS
BEGIN
    SET NOCOUNT ON;
    -- Stub: simula solo l'esito di successo. NON popola pfd.FattureTestata_Eliminate: nel flusso reale
    -- lo spostamento in _Eliminate avviene lato processo DATA (dopo il calcolo), non in questa SP sincrona.
    -- Questo e' coerente con RF06 (una pre-eliminata, prima del calcolo DATA, resta cancellabile).
    RETURN 1; -- successo (>0)
END
GO

-- Seed deterministico: fatture non inviate.
-- 1001-1002 SALDO  -> per POSTICIPA / RIPRISTINA / CANCELLA
-- 2001      ANTICIPO -> per ELIMINA (percorso distruttivo, ora sicuro su DB usa-e-getta)
IF NOT EXISTS (SELECT 1 FROM pfd.FattureTestata WHERE IdFattura IN (1001,1002,2001,2002,3001))
-- IdFattura e' IDENTITY sul DB reale, ma i test si appoggiano a id deterministici (1001, 2002, ...):
-- servono quindi IDENTITY_INSERT e la lista colonne esplicita. Le colonne NOT NULL che non
-- interessano gli scenari di Gestione Fatture (prodotto, tipo documento, importi, divisa...) sono
-- riempite con valori plausibili e costanti: contano solo perche' la tabella non le accetta NULL.
SET IDENTITY_INSERT pfd.FattureTestata ON;
INSERT INTO pfd.FattureTestata
 (IdFattura, FkIdEnte, FkTipologiaFattura, AnnoRiferimento, MeseRiferimento, FatturaInviata,
  FkProdotto, FkIdTipoDocumento, DataFattura, IdentificativoFattura, TotaleFattura, Divisa, MetodoPagamento,
  Progressivo)
VALUES
 (1001, '11111111-1111-1111-1111-111111111111', 'SECONDO SALDO', 2026, 7, 0,
  'prod-pn', 'TD01', '2026-07-01', 'IT-1001', 1220.00, 'EUR', 'MP5', 1001),
 (1002, '22222222-2222-2222-2222-222222222222', 'PRIMO SALDO',   2026, 6, NULL,
  'prod-pn', 'TD01', '2026-06-01', 'IT-1002', 610.00,  'EUR', 'MP5', 1002),
 (2001, '33333333-3333-3333-3333-333333333333', 'ANTICIPO',      2026, 5, 0,
  'prod-pn', 'TD01', '2026-05-01', 'IT-2001', 305.00,  'EUR', 'MP5', 2001),
 -- ELIMINA ok, POSTICIPA no
 (2002, '33333333-3333-3333-3333-333333333333', 'ACCONTO',       2026, 5, 0,
  'prod-pn', 'TD01', '2026-05-01', 'IT-2002', 122.00,  'EUR', 'MP5', 2002),
 -- ente INPS: ELIMINA ok (eccezione)
 (3001, '53b40136-65f2-424b-acfb-7fae17e35c60', 'PRIMO SALDO',   2026, 6, 0,
  'prod-pn', 'TD01', '2026-06-01', 'IT-3001', 9999.00, 'EUR', 'MP5', 3001);
SET IDENTITY_INSERT pfd.FattureTestata OFF;
GO

-- Enti/Contratti seed per i JOIN delle viste be.vwGestioneFatture* (le tabelle esistono da setup.sql)
IF NOT EXISTS (SELECT 1 FROM pfd.Enti WHERE InternalIstitutionId = '11111111-1111-1111-1111-111111111111')
INSERT INTO pfd.Enti (InternalIstitutionId, description) VALUES
 ('11111111-1111-1111-1111-111111111111', 'Ente Test 1'),
 ('22222222-2222-2222-2222-222222222222', 'Ente Test 2'),
 ('33333333-3333-3333-3333-333333333333', 'Ente Test 3'),
 ('53b40136-65f2-424b-acfb-7fae17e35c60', 'Ente INPS');

IF NOT EXISTS (SELECT 1 FROM pfd.Contratti WHERE internalistitutionid = '11111111-1111-1111-1111-111111111111')
INSERT INTO pfd.Contratti (internalistitutionid, FkIdTipoContratto) VALUES
 ('11111111-1111-1111-1111-111111111111', 2),  -- PAC
 ('22222222-2222-2222-2222-222222222222', 2),  -- PAC
 ('33333333-3333-3333-3333-333333333333', 1),  -- PAL
 ('53b40136-65f2-424b-acfb-7fae17e35c60', 2);  -- INPS = PAC
GO

-- Righe PERSISTENTI in cfg.GestioneFatture per i test di LETTURA (griglia/download/modifica).
-- Id 900x: dedicate alle letture, distinte dalle 100x/200x usate (e ripulite) dai test azione.
-- Stato 2 (CANCELLATA) e' escluso dalle viste: ne mettiamo una per verificarlo.
IF NOT EXISTS (SELECT 1 FROM cfg.GestioneFatture WHERE FkIdFattura IN (9001,9002,9003,9004))
-- ATTENZIONE al periodo: con la PRIMARY KEY reale (FkIdEnte, FkTipologiaFattura, Anno, Mese, Stato)
-- queste righe persistenti OCCUPANO una chiave. Se stessero sullo stesso periodo delle fatture usate
-- dai test di scrittura (2026/5-7), una POSTICIPA di quel periodo violerebbe la PK e la SP
-- risponderebbe 0 -- facendo fallire i test happy path per un motivo che non c'entra col codice.
-- Percio' stanno tutte sul 2025: i test di lettura non filtrano per anno, quelli di scrittura sul 2026.
INSERT INTO cfg.GestioneFatture (FkIdFattura, FkIdEnte, FkTipologiaFattura, Anno, Mese, DataInserimento, DataRipristino, IdUtenteInserimento, Stato, Azione, Note)
VALUES
 (9001, '11111111-1111-1111-1111-111111111111', 'SECONDO SALDO', 2025, 1, GETDATE(), NULL,      'seed', 0, 'POSTICIPATA',  N'{"Data":"2025-01-01T00:00:00","Testo":"seed-posticipata"}'),
 (9002, '22222222-2222-2222-2222-222222222222', 'PRIMO SALDO',   2025, 2, GETDATE(), GETDATE(), 'seed', 1, 'RIPRISTINATA', N'{"Data":"2025-02-01T00:00:00","Testo":"seed-ripristinata"}'),
 (9003, '33333333-3333-3333-3333-333333333333', 'ANTICIPO',      2025, 3, GETDATE(), NULL,      'seed', 3, 'ELIMINATA',    N'{"Data":"2025-03-01T00:00:00","Testo":"seed-eliminata"}'),
 (9004, '11111111-1111-1111-1111-111111111111', 'SECONDO SALDO', 2025, 4, GETDATE(), NULL,      'seed', 2, 'CANCELLATA',   N'{"Data":"2025-04-01T00:00:00","Testo":"seed-cancellata-esclusa"}');
GO

-- =============================================================================================
-- Dati per la vista be.vwDocumentiEmessiNonFatturati (Non Fatturate = Eliminate + Posticipate).
-- Periodi/id DEDICATI (Anno 2024, IdFattura 4001/5001/5002) per non interferire con gli altri test.
-- =============================================================================================

IF NOT EXISTS (SELECT 1 FROM pfw.CodiciMateriali WHERE CodiceMateriale IN ('MAT-A','MAT-B'))
INSERT INTO pfw.CodiciMateriali (CodiceMateriale, Descrizione, Ordinamento) VALUES
 ('MAT-A', 'Materiale A', 1),
 ('MAT-B', 'Materiale B', 2);

IF NOT EXISTS (SELECT 1 FROM pfw.FatturaTestataConfig WHERE FKProdotto='prod-pn' AND FkTipologiaFattura IN ('ANTICIPO','ACCONTO','SECONDO SALDO'))
INSERT INTO pfw.FatturaTestataConfig
 (FKProdotto, FKIdTipoContratto, FkTipologiaFattura, FKTipoDocumentoFattura, FKTipoDocumentoNotaCredito, FKIdMetodoPagamento, PercentualeAnticipo, Divisa, Causale)
VALUES
 ('prod-pn', 1, 'ANTICIPO',      'TD01', 'TD04', 5, NULL, 'EUR', 'Anticipo'),
 ('prod-pn', 1, 'ACCONTO',       'TD01', 'TD04', 5, NULL, 'EUR', 'Acconto'),
 ('prod-pn', 2, 'SECONDO SALDO', 'TD01', 'TD04', 5, NULL, 'EUR', 'Secondo saldo');

UPDATE pfd.Contratti SET onboardingtokenid = 'TOKEN-E3'
 WHERE internalistitutionid = '33333333-3333-3333-3333-333333333333' AND onboardingtokenid IS NULL;
GO

-- Ramo ELIMINATE: 5001 CON righe (posizioni valorizzate), 5002 SENZA righe (posizioni NULL).
IF NOT EXISTS (SELECT 1 FROM pfd.FattureTestata_Eliminate WHERE IdFattura IN (5001,5002))
INSERT INTO pfd.FattureTestata_Eliminate
 (IdFattura, FkProdotto, FkIdTipoDocumento, FkTipologiaFattura, FkIdEnte, DataFattura, IdentificativoFattura,
  TotaleFattura, Divisa, MetodoPagamento, AnnoRiferimento, MeseRiferimento, CodiceContratto, SplitPayment, Progressivo, FatturaInviata)
VALUES
 (5001, 'prod-pn', 'TD01', 'ANTICIPO', '33333333-3333-3333-3333-333333333333', '2024-02-01', 'IT-5001',
  500.00, 'EUR', 'MP5', 2024, 2, 'TOKEN-E3', 0, 5001, 0),
 (5002, 'prod-pn', 'TD01', 'ACCONTO',  '33333333-3333-3333-3333-333333333333', '2024-02-01', 'IT-5002',
  200.00, 'EUR', 'MP5', 2024, 2, 'TOKEN-E3', 0, 5002, 0);

IF NOT EXISTS (SELECT 1 FROM pfd.FattureRighe_Eliminate WHERE FkIdFattura = 5001)
INSERT INTO pfd.FattureRighe_Eliminate
 (FkIdFattura, NumeroLinea, Testo, CodiceMateriale, Quantita, PrezzoUnitario, Imponibile, RigaBollo, PeriodoRiferimento)
VALUES
 (5001, 1, 'riga elim 1', 'MAT-A', 1, 300.00, 300.00, 0, '02/2024'),
 (5001, 2, 'riga elim 2', 'MAT-B', 1, 200.00, 200.00, 0, '02/2024');
-- 5002: NESSUNA riga -> [fattura.posizioni] sara' NULL nella vista.
GO

-- Ramo POSTICIPATE: fattura 4001 (ente1/SECONDO SALDO/2024/1) + riga Stato=0 + righe.
SET IDENTITY_INSERT pfd.FattureTestata ON;
IF NOT EXISTS (SELECT 1 FROM pfd.FattureTestata WHERE IdFattura = 4001)
INSERT INTO pfd.FattureTestata
 (IdFattura, FkIdEnte, FkTipologiaFattura, AnnoRiferimento, MeseRiferimento, FatturaInviata,
  FkProdotto, FkIdTipoDocumento, DataFattura, IdentificativoFattura, TotaleFattura, Divisa, MetodoPagamento, Progressivo, CodiceContratto)
VALUES
 (4001, '11111111-1111-1111-1111-111111111111', 'SECONDO SALDO', 2024, 1, 0,
  'prod-pn', 'TD01', '2024-01-01', 'IT-4001', 800.00, 'EUR', 'MP5', 4001, 'TOKEN-E1');
SET IDENTITY_INSERT pfd.FattureTestata OFF;

IF NOT EXISTS (SELECT 1 FROM cfg.GestioneFatture WHERE FkIdFattura = 4001)
INSERT INTO cfg.GestioneFatture (FkIdFattura, FkIdEnte, FkTipologiaFattura, Anno, Mese, DataInserimento, IdUtenteInserimento, Stato, Azione, Note)
VALUES
 (4001, '11111111-1111-1111-1111-111111111111', 'SECONDO SALDO', 2024, 1, GETDATE(), 'seed', 0, 'POSTICIPATA', N'[]');

IF NOT EXISTS (SELECT 1 FROM pfd.FattureRighe WHERE FkIdFattura = 4001)
INSERT INTO pfd.FattureRighe
 (FkIdFattura, NumeroLinea, Testo, CodiceMateriale, Quantita, PrezzoUnitario, Imponibile, RigaBollo, PeriodoRiferimento)
VALUES
 (4001, 1, 'riga post 1', 'MAT-A', 1, 500.00, 500.00, 0, '01/2024'),
 (4001, 2, 'riga post 2', 'MAT-B', 1, 300.00, 300.00, 0, '01/2024');
GO
