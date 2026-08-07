-- =============================================================================
-- Seed DB locale per i test CRUD/azione di Gestione Fatture (PF-672).
-- Ricrea il SOTTOINSIEME di schema che le stored procedure be.spGestioneFattura*
-- toccano: schema cfg/be, pfd.FattureTestata(+_Eliminate), cfg.GestioneFatture, le tabelle
-- *_Eliminate tmp e CreditoSospesoStorico toccate da pfd.EliminaFattura, e fatture seed deterministiche.
--
-- Le SP reali (be.spGestioneFattura{Posticipa,Elimina,Ripristina,Cancella} e pfd.EliminaFattura) NON
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

-- pfd.EliminaFattura: NON piu' uno stub. La SP REALE (owner: team DB, versione 30/06/2026) vive ora in
-- tests/Data/sp/05_pfdEliminaFattura.sql (script autorevole, CREATE OR ALTER), applicata dall'entrypoint
-- DOPO questo file. La SP ELIMINA fa EXEC @rc = pfd.EliminaFattura @IdFattura e prosegue solo se @rc > 0.
-- La SP reale sposta davvero la fattura in *_Eliminate e cancella da FattureTestata/tmp*/MesiFatture/
-- CreditoSospesoStorico: le tabelle *_Eliminate tmp e CreditoSospesoStorico che le mancavano sono
-- create qui sotto. I test (happy-path e requisiti) e i loro helper di restore erano gia' scritti per
-- questo contratto (spostano in _Eliminate e re-inseriscono in FattureTestata).
--
-- pfd.tmpFattureTestata_Eliminate: destinazione del MERGE (ON 1=0 -> sempre INSERT) della SP reale.
-- Modellata su pfd.tmpFattureTestata: IdFattura e' bigint SEMPLICE (la SP lo inserisce esplicitamente,
-- niente IDENTITY/PK, come pfd.FattureTestata_Eliminate), SENZA FlagFatturata, CON FlagProceduraWhiteList
-- (assume il default 0, coerente col commento nella SP).
IF OBJECT_ID('pfd.tmpFattureTestata_Eliminate', 'U') IS NULL
CREATE TABLE [pfd].[tmpFattureTestata_Eliminate](
	[IdFattura] [bigint] NOT NULL,
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
	[FatturaInviata] [bit] NULL,
	[Semestre] [nvarchar](15) NULL,
	[FlagProceduraWhiteList] [bit] NOT NULL CONSTRAINT [DF_tmpFattureTestata_Eliminate_FlagProceduraWhiteList] DEFAULT ((0))
);
GO

-- pfd.tmpFattureRighe_Eliminate: destinazione dell'INSERT righe tmp della SP reale (mirror di tmpFattureRighe).
IF OBJECT_ID('pfd.tmpFattureRighe_Eliminate', 'U') IS NULL
CREATE TABLE [pfd].[tmpFattureRighe_Eliminate](
	[FkIdFattura] [bigint] NOT NULL,
	[NumeroLinea] [int] NOT NULL,
	[Testo] [nvarchar](max) NULL,
	[CodiceMateriale] [nvarchar](100) NOT NULL,
	[Quantita] [int] NOT NULL,
	[PrezzoUnitario] [float] NOT NULL,
	[Imponibile] [float] NOT NULL,
	[RigaBollo] [bit] NOT NULL,
	[PeriodoRiferimento] [nvarchar](7) NULL
);
GO

-- pfd.CreditoSospesoStorico: la SP reale la usa SOLO in "DELETE ... WHERE FkIdFattura = @IdFattura".
-- DDL minimale plausibile (il team DB non ha ancora fornito quello reale): basta FkIdFattura per la DELETE;
-- le altre colonne sono indicative. SENZA vincoli/righe seed -> la DELETE trova 0 righe, nessun errore.
IF OBJECT_ID('pfd.CreditoSospesoStorico', 'U') IS NULL
CREATE TABLE [pfd].[CreditoSospesoStorico](
	[FkIdEnte] [nvarchar](100) NULL,
	[AnnoRiferimento] [int] NULL,
	[MeseRiferimento] [int] NULL,
	[FKTipologiaFattura] [nvarchar](15) NULL,
	[FkIdFattura] [bigint] NULL,
	[Importo] [decimal](9, 2) NULL,
	[DataMovimento] [datetime] NULL
);
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

-- Emessa "normale" per il ramo SelectView (Documenti Emessi non cancellate): 6001, CodiceContratto che
-- matcha onboardingtokenid ente1, TotaleFattura>0, FatturaInviata=1 (fuori da vwDettaglioFattureDaInviare),
-- NON in cfg.GestioneFatture. Periodo dedicato 2024/3.
UPDATE pfd.Contratti SET onboardingtokenid = 'TOKEN-E1'
 WHERE internalistitutionid = '11111111-1111-1111-1111-111111111111' AND onboardingtokenid IS NULL;
GO
SET IDENTITY_INSERT pfd.FattureTestata ON;
IF NOT EXISTS (SELECT 1 FROM pfd.FattureTestata WHERE IdFattura = 6001)
INSERT INTO pfd.FattureTestata
 (IdFattura, FkIdEnte, FkTipologiaFattura, AnnoRiferimento, MeseRiferimento, FatturaInviata,
  FkProdotto, FkIdTipoDocumento, DataFattura, IdentificativoFattura, TotaleFattura, Divisa, MetodoPagamento, Progressivo, CodiceContratto)
VALUES
 (6001, '11111111-1111-1111-1111-111111111111', 'SECONDO SALDO', 2024, 3, 1,
  'prod-pn', 'TD01', '2024-03-01', 'IT-6001', 1000.00, 'EUR', 'MP5', 6001, 'TOKEN-E1');
SET IDENTITY_INSERT pfd.FattureTestata OFF;
IF NOT EXISTS (SELECT 1 FROM pfd.FattureRighe WHERE FkIdFattura = 6001)
INSERT INTO pfd.FattureRighe
 (FkIdFattura, NumeroLinea, Testo, CodiceMateriale, Quantita, PrezzoUnitario, Imponibile, RigaBollo, PeriodoRiferimento)
VALUES
 (6001, 1, 'riga emessa 1', 'MAT-A', 1, 1000.00, 1000.00, 0, '03/2024');
GO

-- =============================================================================================
-- Report Fatture Sospese: tabelle tmp* + MesiFatture per be.vwFattureSospeseReport e
-- be.vwFattureSospeseNoteReport (lette da FattureSospeseRelExcelHandler, 3 bucket view/union/note).
-- DDL REALI forniti dal team DB; qui SENZA foreign key (convenzione del seed: no parent tables non
-- usate come pfd.FattureTipoDocumento/FattureTipologia/pfw.Prodotti).
-- Le tmpFatture* sono ISOLATE dalle pfd.FattureTestata "normali": nessuna interferenza con gli altri test.
-- Periodo DEDICATO: Anno 2026 / Mese 2 / 'SECONDO SALDO' (coincide coi default Conf del test sospesi).
-- =============================================================================================
IF OBJECT_ID('pfd.tmpFattureTestata', 'U') IS NULL
CREATE TABLE [pfd].[tmpFattureTestata](
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
	[FatturaInviata] [bit] NULL CONSTRAINT [DF_tmpFattureTestata_FatturaInviata] DEFAULT ((0)),
	[Semestre] [nvarchar](15) NULL,
	[FlagFatturata] [bit] NOT NULL CONSTRAINT [DF_tmpFattureTestata_FlagFatturata] DEFAULT ((0)),
 CONSTRAINT [PK_tmpFatturaTestata] PRIMARY KEY CLUSTERED ([IdFattura] ASC),
 CONSTRAINT [UQ_tmpFattureTestata_Ente_Periodo_Tipologia] UNIQUE NONCLUSTERED
	([FkIdEnte] ASC, [AnnoRiferimento] ASC, [MeseRiferimento] ASC, [FkTipologiaFattura] ASC)
);
GO

IF OBJECT_ID('pfd.tmpFattureRighe', 'U') IS NULL
CREATE TABLE [pfd].[tmpFattureRighe](
	[FkIdFattura] [bigint] NOT NULL,
	[NumeroLinea] [int] NOT NULL,
	[Testo] [nvarchar](max) NULL,
	[CodiceMateriale] [nvarchar](100) NOT NULL,
	[Quantita] [int] NOT NULL,
	[PrezzoUnitario] [float] NOT NULL,
	[Imponibile] [float] NOT NULL,
	[RigaBollo] [bit] NOT NULL,
	[PeriodoRiferimento] [nvarchar](7) NULL
);
GO

-- tmpRelTestata: le colonne Asseverazione* (non referenziate dalle viste sospese) sono omesse; nel seed
-- la tabella resta VUOTA per il periodo -> il "NOT IN (rel)" del ramo note e' soddisfatto per tutti.
IF OBJECT_ID('pfd.tmpRelTestata', 'U') IS NULL
CREATE TABLE [pfd].[tmpRelTestata](
	[internal_organization_id] [nvarchar](50) NOT NULL,
	[contract_id] [nvarchar](200) NOT NULL,
	[TipologiaFattura] [nvarchar](20) NOT NULL,
	[year] [int] NOT NULL,
	[month] [int] NOT NULL,
	[TotaleAnalogico] [decimal](9, 2) NULL,
	[TotaleDigitale] [decimal](9, 2) NULL,
	[TotaleNotificheAnalogiche] [int] NULL,
	[TotaleNotificheDigitali] [int] NULL,
	[Totale] [decimal](9, 2) NULL,
	[Iva] [decimal](4, 2) NOT NULL CONSTRAINT [DF_tmpRelTestata_Iva] DEFAULT ((22)),
	[TotaleAnalogicoIva] [decimal](9, 2) NULL,
	[TotaleDigitaleIva] [decimal](9, 2) NULL,
	[TotaleIva] [decimal](9, 2) NULL,
	[Caricata] [tinyint] NULL CONSTRAINT [DF_tmpRelTestata_Caricata] DEFAULT ((0)),
	[RelFatturata] [bit] NOT NULL CONSTRAINT [DF_tmpRelTestata_RelFatturata] DEFAULT ((0)),
	[FlagConguaglio] [nvarchar](25) NULL,
 CONSTRAINT [PK_tmpRelTestata] PRIMARY KEY CLUSTERED
	([internal_organization_id] ASC, [contract_id] ASC, [TipologiaFattura] ASC, [year] ASC, [month] ASC)
);
GO

IF OBJECT_ID('pfd.MesiFatture', 'U') IS NULL
CREATE TABLE [pfd].[MesiFatture](
	[FkIdEnte] [nvarchar](100) NOT NULL,
	[AnnoRiferimento] [int] NOT NULL,
	[MeseRiferimento] [int] NOT NULL,
	[FKTipologiaFattura] [nvarchar](15) NOT NULL,
	[FkIdFattura] [bigint] NULL,
	[FkIdFatturaTmp] [bigint] NOT NULL,
	[FlagEliminata] [bit] NOT NULL CONSTRAINT [DF_MesiFatture_FlagEliminata] DEFAULT ((0)),
 CONSTRAINT [PK_MesiFatture] PRIMARY KEY CLUSTERED
	([FkIdEnte] ASC, [AnnoRiferimento] ASC, [MeseRiferimento] ASC, [FKTipologiaFattura] ASC, [FkIdFatturaTmp] ASC)
);
GO

-- Ramo VIEW: 7001 ente1 (TOKEN-E1, PAC/tipo2), 2 righe non-storno, CON MesiFatture -> RelNonFirmata='Rel in attesa di firma'.
-- Ramo NOTE: 7002 ente3 (TOKEN-E3, PAL/tipo1), riga STORNO, SENZA MesiFatture, SENZA rel -> RelNonFirmata=''.
-- Nota: 7002 (mf NULL) compare anche nel ramo VIEW (RelNonFirmata='') -- corretto: la UNION dedupa.
SET IDENTITY_INSERT pfd.tmpFattureTestata ON;
IF NOT EXISTS (SELECT 1 FROM pfd.tmpFattureTestata WHERE IdFattura IN (7001,7002))
INSERT INTO pfd.tmpFattureTestata
 (IdFattura, FkProdotto, FkIdTipoDocumento, FkTipologiaFattura, FkIdEnte, DataFattura, IdentificativoFattura,
  TotaleFattura, Divisa, MetodoPagamento, AnnoRiferimento, MeseRiferimento, CodiceContratto, Progressivo, FatturaInviata, FlagFatturata)
VALUES
 (7001, 'prod-pn', 'TD01', 'SECONDO SALDO', '11111111-1111-1111-1111-111111111111', '2026-02-01', 'IT-7001',
  1000.00, 'EUR', 'MP5', 2026, 2, 'TOKEN-E1', 7001, 0, 0),
 (7002, 'prod-pn', 'TD01', 'SECONDO SALDO', '33333333-3333-3333-3333-333333333333', '2026-02-01', 'IT-7002',
  250.00,  'EUR', 'MP5', 2026, 2, 'TOKEN-E3', 7002, 0, 0);
SET IDENTITY_INSERT pfd.tmpFattureTestata OFF;
GO

IF NOT EXISTS (SELECT 1 FROM pfd.tmpFattureRighe WHERE FkIdFattura IN (7001,7002))
INSERT INTO pfd.tmpFattureRighe
 (FkIdFattura, NumeroLinea, Testo, CodiceMateriale, Quantita, PrezzoUnitario, Imponibile, RigaBollo, PeriodoRiferimento)
VALUES
 (7001, 1, 'riga sosp 1', 'MAT-A',          1, 400.00, 400.00, 0, '02/2026'),
 (7001, 2, 'riga sosp 2', 'MAT-B',          1, 600.00, 600.00, 0, '02/2026'),
 (7002, 1, 'storno ant',  'STORNO ANT. NA', 1, 250.00, 250.00, 0, '02/2026');
GO

-- Solo 7001 ha la riga MesiFatture (marker "rel in attesa di firma" per il contratto PAC/tipo2).
IF NOT EXISTS (SELECT 1 FROM pfd.MesiFatture WHERE FkIdFatturaTmp = 7001)
INSERT INTO pfd.MesiFatture (FkIdEnte, AnnoRiferimento, MeseRiferimento, FKTipologiaFattura, FkIdFattura, FkIdFatturaTmp, FlagEliminata)
VALUES
 ('11111111-1111-1111-1111-111111111111', 2026, 2, 'SECONDO SALDO', NULL, 7001, 0);
GO

-- =============================================================================================
-- Report EMESSE non-sospese (extension ReportFatture -> FattureRelExcelQuery, che legge pfd.FattureTestata
-- + FattureRighe + RelTestata + Contratti + TipoContratto). Serve una fattura "regolare" con RelTestata
-- MATCHATA nello stesso periodo del seed sospesi (2026/2/SECONDO SALDO), cosi' il report genera i fogli
-- "Regolari Esecuzioni"/"Enti Fatt." (che NON devono contenere 'Rel Non Firmata') mentre il sotto-foglio
-- "...Sospesi" (da FattureRelSospeseExcelDto, dal seed tmp*) la contiene. Test: FattureReportEndpoint{E2E,Integration}Tests.
-- pfd.RelUpload (assente nel seed) e' richiesta da RelNonFatturateQuery (SelectAll): solo LEFT JOIN -> tabella
-- vuota. DDL reale fornito dal team DB (le colonne NOT NULL non danno fastidio: nessuna riga seedata).
IF OBJECT_ID('pfd.RelUpload', 'U') IS NULL
CREATE TABLE [pfd].[RelUpload](
	[FkIdEnte] [nvarchar](50) NOT NULL,
	[contract_id] [nvarchar](200) NOT NULL,
	[TipologiaFattura] [nvarchar](20) NOT NULL,
	[year] [int] NOT NULL,
	[month] [int] NOT NULL,
	[DataEvento] [datetime] NOT NULL,
	[IdUtente] [nvarchar](255) NOT NULL,
	[Azione] [nvarchar](5) NOT NULL,
	[Hash] [nvarchar](128) NOT NULL
);
GO

-- Fattura regolare 8001 (ente1/SECONDO SALDO/2026/2, TOKEN-E1). FatturaInviata=1 per NON finire in
-- vwDettaglioFattureDaInviare. La riga NON e' storno -> non tocca i bucket storno.
SET IDENTITY_INSERT pfd.FattureTestata ON;
IF NOT EXISTS (SELECT 1 FROM pfd.FattureTestata WHERE IdFattura = 8001)
INSERT INTO pfd.FattureTestata
 (IdFattura, FkIdEnte, FkTipologiaFattura, AnnoRiferimento, MeseRiferimento, FatturaInviata,
  FkProdotto, FkIdTipoDocumento, DataFattura, IdentificativoFattura, TotaleFattura, Divisa, MetodoPagamento, Progressivo, CodiceContratto)
VALUES
 (8001, '11111111-1111-1111-1111-111111111111', 'SECONDO SALDO', 2026, 2, 1,
  'prod-pn', 'TD01', '2026-02-01', 'IT-8001', 1500.00, 'EUR', 'MP5', 8001, 'TOKEN-E1');
SET IDENTITY_INSERT pfd.FattureTestata OFF;
GO

IF NOT EXISTS (SELECT 1 FROM pfd.FattureRighe WHERE FkIdFattura = 8001)
INSERT INTO pfd.FattureRighe
 (FkIdFattura, NumeroLinea, Testo, CodiceMateriale, Quantita, PrezzoUnitario, Imponibile, RigaBollo, PeriodoRiferimento)
VALUES
 (8001, 1, 'riga report ss', 'MAT-A', 1, 1500.00, 1500.00, 0, '02/2026');
GO

-- RelTestata MATCHATA (org+contract+tipologia+anno+mese) richiesta dal WHERE di _sqlRel; caricata=0/relfatturata=0.
IF NOT EXISTS (SELECT 1 FROM pfd.RelTestata WHERE internal_organization_id='11111111-1111-1111-1111-111111111111' AND [year]=2026 AND [month]=2 AND TipologiaFattura='SECONDO SALDO')
INSERT INTO pfd.RelTestata
 (internal_organization_id, contract_id, TipologiaFattura, [year], [month], TotaleAnalogico, TotaleDigitale,
  TotaleNotificheAnalogiche, TotaleNotificheDigitali, Totale, TotaleAnalogicoIva, TotaleDigitaleIva, TotaleIva, Caricata, RelFatturata)
VALUES
 ('11111111-1111-1111-1111-111111111111', 'TOKEN-E1', 'SECONDO SALDO', 2026, 2, 100.00, 200.00, 10, 20, 300.00, 122.00, 244.00, 366.00, 0, 0);
GO

-- =============================================================================================
-- Regressione CASING ente (fix match case-insensitive in FattureQueryRicercaPersistence).
-- Posticipata 2026/7: cfg.GestioneFatture.FkIdEnte in MAIUSCOLO mentre pfd.Enti/FattureTestata/Contratti
-- sono lowercase. Le JOIN SQL della vista sono case-insensitive -> la riga esce con istitutioID MAIUSCOLO;
-- il match C# case-sensitive la scartava (404). GUID con lettere hex per rendere il casing significativo.
-- =============================================================================================
IF NOT EXISTS (SELECT 1 FROM pfd.Enti WHERE InternalIstitutionId = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee')
INSERT INTO pfd.Enti (InternalIstitutionId, description) VALUES
 ('aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee', 'Ente Casing Test');

IF NOT EXISTS (SELECT 1 FROM pfd.Contratti WHERE internalistitutionid = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee')
INSERT INTO pfd.Contratti (internalistitutionid, FkIdTipoContratto, onboardingtokenid) VALUES
 ('aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee', 2, 'TOKEN-CASE');
GO

-- FattureTestata lowercase (system-of-record) per il periodo della posticipata; la vista LEFT JOINa ft
-- su gf.FkIdEnte=ft.FkIdEnte (case-insensitive) e ne serve l'INNER con FatturaTestataConfig.
SET IDENTITY_INSERT pfd.FattureTestata ON;
IF NOT EXISTS (SELECT 1 FROM pfd.FattureTestata WHERE IdFattura = 9101)
INSERT INTO pfd.FattureTestata
 (IdFattura, FkIdEnte, FkTipologiaFattura, AnnoRiferimento, MeseRiferimento, FatturaInviata,
  FkProdotto, FkIdTipoDocumento, DataFattura, IdentificativoFattura, TotaleFattura, Divisa, MetodoPagamento, Progressivo, CodiceContratto)
VALUES
 (9101, 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee', 'SECONDO SALDO', 2026, 7, 0,
  'prod-pn', 'TD01', '2026-07-01', 'IT-9101', 700.00, 'EUR', 'MP5', 9101, 'TOKEN-CASE');
SET IDENTITY_INSERT pfd.FattureTestata OFF;

IF NOT EXISTS (SELECT 1 FROM pfd.FattureRighe WHERE FkIdFattura = 9101)
INSERT INTO pfd.FattureRighe
 (FkIdFattura, NumeroLinea, Testo, CodiceMateriale, Quantita, PrezzoUnitario, Imponibile, RigaBollo, PeriodoRiferimento)
VALUES
 (9101, 1, 'riga casing', 'MAT-A', 1, 700.00, 700.00, 0, '07/2026');
GO

-- cfg.GestioneFatture: FkIdEnte in MAIUSCOLO (il bug: casing diverso da pfd.Enti). Stato=0 (POSTICIPATA).
-- La vista emette [fattura.istitutioID] = gf.FkIdEnte (MAIUSCOLO) e [fattura.idfattura] = gf.FkIdFattura (9101).
IF NOT EXISTS (SELECT 1 FROM cfg.GestioneFatture WHERE FkIdFattura = 9101)
INSERT INTO cfg.GestioneFatture (FkIdFattura, FkIdEnte, FkTipologiaFattura, Anno, Mese, DataInserimento, IdUtenteInserimento, Stato, Azione, Note)
VALUES
 (9101, 'AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE', 'SECONDO SALDO', 2026, 7, GETDATE(), 'seed', 0, 'POSTICIPATA', N'[]');
GO

-- =============================================================================================
-- Effetto azioni sulle liste: RIPRISTINATA (Stato=1) e CANCELLATA (Stato=2) devono RIENTRARE nella
-- ricerca EMESSE (SelectView filtra gf.Stato <> 0 OR IS NULL), a differenza della POSTICIPATA (Stato=0).
-- Inoltre servono a evidenziare la discrepanza col vwDettaglioFattureDaInviare (che esclude QUALUNQUE riga
-- in cfg.GestioneFatture): una ripristinata compare in emesse ma resta esclusa dal "da inviare".
-- Periodi dedicati ente1 2024/5 e 2024/6, FatturaInviata=0 (candidate anche a "da inviare"). TOKEN-E1.
-- =============================================================================================
SET IDENTITY_INSERT pfd.FattureTestata ON;
IF NOT EXISTS (SELECT 1 FROM pfd.FattureTestata WHERE IdFattura IN (6002,6003))
INSERT INTO pfd.FattureTestata
 (IdFattura, FkIdEnte, FkTipologiaFattura, AnnoRiferimento, MeseRiferimento, FatturaInviata,
  FkProdotto, FkIdTipoDocumento, DataFattura, IdentificativoFattura, TotaleFattura, Divisa, MetodoPagamento, Progressivo, CodiceContratto)
VALUES
 (6002, '11111111-1111-1111-1111-111111111111', 'SECONDO SALDO', 2024, 5, 0,
  'prod-pn', 'TD01', '2024-05-01', 'IT-6002', 900.00, 'EUR', 'MP5', 6002, 'TOKEN-E1'),
 (6003, '11111111-1111-1111-1111-111111111111', 'SECONDO SALDO', 2024, 6, 0,
  'prod-pn', 'TD01', '2024-06-01', 'IT-6003', 950.00, 'EUR', 'MP5', 6003, 'TOKEN-E1');
SET IDENTITY_INSERT pfd.FattureTestata OFF;

IF NOT EXISTS (SELECT 1 FROM pfd.FattureRighe WHERE FkIdFattura IN (6002,6003))
INSERT INTO pfd.FattureRighe
 (FkIdFattura, NumeroLinea, Testo, CodiceMateriale, Quantita, PrezzoUnitario, Imponibile, RigaBollo, PeriodoRiferimento)
VALUES
 (6002, 1, 'riga ripristinata', 'MAT-A', 1, 900.00, 900.00, 0, '05/2024'),
 (6003, 1, 'riga cancellata',   'MAT-A', 1, 950.00, 950.00, 0, '06/2024');
GO

-- cfg: 6002 RIPRISTINATA (Stato=1), 6003 CANCELLATA (Stato=2) sugli stessi periodi.
IF NOT EXISTS (SELECT 1 FROM cfg.GestioneFatture WHERE FkIdFattura IN (6002,6003))
INSERT INTO cfg.GestioneFatture (FkIdFattura, FkIdEnte, FkTipologiaFattura, Anno, Mese, DataInserimento, DataRipristino, DataCancellazione, IdUtenteInserimento, Stato, Azione, Note)
VALUES
 (6002, '11111111-1111-1111-1111-111111111111', 'SECONDO SALDO', 2024, 5, GETDATE(), GETDATE(), NULL,      'seed', 1, 'RIPRISTINATA', N'[]'),
 (6003, '11111111-1111-1111-1111-111111111111', 'SECONDO SALDO', 2024, 6, GETDATE(), NULL,      GETDATE(), 'seed', 2, 'CANCELLATA',   N'[]');
GO

-- =============================================================================================
-- Dettaglio REL: [be].[vwRelDettaglio], letta da GET api/rel/pagopa/{id} (BE-SMOKE-02 del testbook).
-- L'id di rotta e' la RelTestataKey serializzata: {IdEnte}_{IdContratto}_{Tipologia con '-' al posto
-- degli spazi}_{Anno}_{Mese}.
--
-- Tre periodi, tutti su ente1 (TOKEN-E1), scelti liberi per non interferire con i seed esistenti
-- (2024 non-fatturate, 2025 griglia, 2026/2 sospese+report, 2026/7 casing):
--
--   2026/5  PRIMO SALDO   -> caso COMPLETO: RelTestata + MesiFatture + tmp testata/righe con storni.
--                            Verifica la mappatura dei CodiceMateriale sui 4 bucket e il segno negativo.
--   2026/9  PRIMO SALDO   -> caso TRAPPOLA 1: RelTestata presente ma NESSUNA riga di staging
--                            (MesiFatture/tmp*), come un periodo storico ripulito. TotaliCumulati e'
--                            in INNER JOIN -> la vista non restituisce nulla -> SingleAsync -> 500.
--   2026/10 PRIMO SALDO   -> caso TRAPPOLA 2 (fan-out): staging completo, ma l'ente ha DUE righe in
--                            pfw.DatiFatturazione. Il LEFT JOIN della vista e' solo su FkIdEnte, senza
--                            anno/mese/tipologia -> 2 righe -> SingleAsync -> 500.
--                            Usa un ente DEDICATO (9999...) per non alterare gli altri test: qualunque
--                            riga DatiFatturazione su ente1 renderebbe fan-out anche il caso 2026/5.
-- =============================================================================================

IF NOT EXISTS (SELECT 1 FROM pfd.Enti WHERE InternalIstitutionId = '99999999-9999-9999-9999-999999999999')
INSERT INTO pfd.Enti (InternalIstitutionId, description) VALUES
 ('99999999-9999-9999-9999-999999999999', 'Ente Rel Fanout');
GO

-- Staging del caso completo (2026/5) e del caso fan-out (2026/10).
SET IDENTITY_INSERT pfd.tmpFattureTestata ON;
IF NOT EXISTS (SELECT 1 FROM pfd.tmpFattureTestata WHERE IdFattura IN (9201,9202))
INSERT INTO pfd.tmpFattureTestata
 (IdFattura, FkProdotto, FkIdTipoDocumento, FkTipologiaFattura, FkIdEnte, DataFattura, IdentificativoFattura,
  TotaleFattura, Divisa, MetodoPagamento, AnnoRiferimento, MeseRiferimento, CodiceContratto, Progressivo, FatturaInviata, FlagFatturata)
VALUES
 (9201, 'prod-pn', 'TD01', 'PRIMO SALDO', '11111111-1111-1111-1111-111111111111', '2026-05-01', 'IT-9201',
  800.00, 'EUR', 'MP5', 2026, 5, 'TOKEN-E1', 9201, 0, 0),
 (9202, 'prod-pn', 'TD01', 'PRIMO SALDO', '99999999-9999-9999-9999-999999999999', '2026-10-01', 'IT-9202',
  100.00, 'EUR', 'MP5', 2026, 10, 'TOKEN-E9', 9202, 0, 0);
SET IDENTITY_INSERT pfd.tmpFattureTestata OFF;
GO

-- 9201: un materiale per ciascuno dei 4 bucket + una riga NON storno che deve restare fuori dai totali.
-- Attesi in vista (il moltiplicatore * -1 della vista li rende negativi):
--   Anticipo_StornoAnalogico -100, Anticipo_StornoDigitale -50,
--   Acconto_StornoAnalogico   -30, Acconto_StornoDigitale  -20,
--   Anticipo_StornoTotale    -150, Acconto_StornoTotale    -50, StornoTotale -200.
IF NOT EXISTS (SELECT 1 FROM pfd.tmpFattureRighe WHERE FkIdFattura IN (9201,9202))
INSERT INTO pfd.tmpFattureRighe
 (FkIdFattura, NumeroLinea, Testo, CodiceMateriale, Quantita, PrezzoUnitario, Imponibile, RigaBollo, PeriodoRiferimento)
VALUES
 (9201, 1, 'storno anticipo analogico', 'STORNO ANTICIPO NA', 1, 100.00, 100.00, 0, '05/2026'),
 (9201, 2, 'storno anticipo digitale',  'STORNO ANTICIPO ND', 1,  50.00,  50.00, 0, '05/2026'),
 (9201, 3, 'storno acconto analogico',  'STORNO ACCONTO NA',  1,  30.00,  30.00, 0, '05/2026'),
 (9201, 4, 'storno acconto digitale',   'STORNO ACCONTO ND',  1,  20.00,  20.00, 0, '05/2026'),
 (9201, 5, 'consumo del periodo',       'MAT-A',              1, 600.00, 600.00, 0, '05/2026'),
 (9202, 1, 'consumo del periodo',       'MAT-A',              1, 100.00, 100.00, 0, '10/2026');
GO

IF NOT EXISTS (SELECT 1 FROM pfd.MesiFatture WHERE FkIdFatturaTmp IN (9201,9202))
INSERT INTO pfd.MesiFatture (FkIdEnte, AnnoRiferimento, MeseRiferimento, FKTipologiaFattura, FkIdFattura, FkIdFatturaTmp, FlagEliminata)
VALUES
 ('11111111-1111-1111-1111-111111111111', 2026,  5, 'PRIMO SALDO', NULL, 9201, 0),
 ('99999999-9999-9999-9999-999999999999', 2026, 10, 'PRIMO SALDO', NULL, 9202, 0);
GO

-- Testate REL dei tre periodi. La 2026/9 e' volutamente SENZA staging (caso trappola 1).
-- Le colonne Asseverazione* vanno valorizzate anche se non interessano il caso: su RelTestataDettaglioDto
-- sono decimal/int NON nullable, quindi un NULL a DB fa fallire il mapping Dapper (InvalidCastException
-- -> 500) prima ancora di arrivare all'asserzione. Vale per qualunque nuova riga di RelTestata nel seed.
IF NOT EXISTS (SELECT 1 FROM pfd.RelTestata WHERE contract_id IN ('TOKEN-E1','TOKEN-E9') AND [year]=2026 AND [month] IN (5,9,10) AND TipologiaFattura='PRIMO SALDO')
INSERT INTO pfd.RelTestata
 (internal_organization_id, contract_id, TipologiaFattura, [year], [month], TotaleAnalogico, TotaleDigitale,
  TotaleNotificheAnalogiche, TotaleNotificheDigitali, Totale, TotaleAnalogicoIva, TotaleDigitaleIva, TotaleIva, Caricata, RelFatturata,
  AsseverazioneTotaleAnalogico, AsseverazioneTotaleDigitale, AsseverazioneTotaleNotificheAnalogiche, AsseverazioneTotaleNotificheDigitali,
  AsseverazioneTotale, AsseverazioneTotaleAnalogicoIva, AsseverazioneTotaleDigitaleIva, AsseverazioneTotaleIva)
VALUES
 ('11111111-1111-1111-1111-111111111111', 'TOKEN-E1', 'PRIMO SALDO', 2026,  5, 700.00, 300.00, 70, 30, 1000.00, 854.00, 366.00, 1220.00, 1, 0,
  0, 0, 0, 0, 0, 0, 0, 0),
 ('11111111-1111-1111-1111-111111111111', 'TOKEN-E1', 'PRIMO SALDO', 2026,  9, 111.00, 222.00, 11, 22,  333.00, 135.42, 270.84,  406.26, 0, 0,
  0, 0, 0, 0, 0, 0, 0, 0),
 ('99999999-9999-9999-9999-999999999999', 'TOKEN-E9', 'PRIMO SALDO', 2026, 10, 100.00,   0.00, 10,  0,  100.00, 122.00,   0.00,  122.00, 0, 0,
  0, 0, 0, 0, 0, 0, 0, 0);
GO

-- Due righe DatiFatturazione sullo stesso ente: e' esattamente cio' che innesca il fan-out del
-- LEFT JOIN (solo su FkIdEnte). Ente dedicato, quindi nessun altro test ne risente.
IF NOT EXISTS (SELECT 1 FROM pfw.DatiFatturazione WHERE FkIdEnte = '99999999-9999-9999-9999-999999999999')
INSERT INTO pfw.DatiFatturazione
 (Cup, Cig, CodCommessa, DataDocumento, SplitPayment, FkIdEnte, IdDocumento, DataCreazione, FkTipoCommessa, PEC, FkProdotto, NotaLegale)
VALUES
 -- FkTipoCommessa deve esistere in pfw.TipoCommessa (FK FkTipoCommessaDatiFatturazione): 1=Ordine, 2=Contratto.
 ('CUP-FANOUT-1', NULL, 'COMM-1', '2026-10-01', 0, '99999999-9999-9999-9999-999999999999', 'DOC-1', GETDATE(), '1', 'fanout@pec.it', 'prod-pn', 0),
 ('CUP-FANOUT-2', NULL, 'COMM-2', '2026-10-02', 0, '99999999-9999-9999-9999-999999999999', 'DOC-2', GETDATE(), '1', 'fanout@pec.it', 'prod-pn', 0);
GO

-- =============================================================================================
-- pfd.FattureWhiteList — esclusione di enti dalla fatturazione (endpoint api/fatture/pagopa/whitelist/*).
-- Semantica: una riga con DataFine NULL e' un'esclusione ATTIVA per Ente/Anno/Mese/Tipologia; la
-- "cancellazione" e' un soft-delete che valorizza DataFine, non un DELETE.
--
-- DDL reale fornita dal team DB. Nessuna FK dichiarata (com'e' all'origine): le JOIN su Enti/Contratti
-- stanno nella query di lettura, non nello schema.
--
-- ⚠️ `IdUtente` e' NOT NULL: il command lo valorizza da AuthenticationInfo.Id, quindi un'identita'
-- senza Id fa fallire l'INSERT a DB, non a monte. Da tenere presente scrivendo test o fixture.
--
-- I test usano l'ANNO 2099 come sandbox e ripuliscono per anno: non tocca nessun altro dato del seed.
-- =============================================================================================
IF OBJECT_ID('pfd.FattureWhiteList', 'U') IS NULL
CREATE TABLE [pfd].[FattureWhiteList](
	[IdLista] [int] IDENTITY(1,1) NOT NULL,
	[FkIdEnte] [nvarchar](100) NOT NULL,
	[Anno] [int] NOT NULL,
	[Mese] [int] NOT NULL,
	[DataInizio] [datetime] NOT NULL,
	[DataFine] [datetime] NULL,
	[FkTipologiaFattura] [nvarchar](100) NOT NULL,
	[IdUtente] [nvarchar](100) NOT NULL,
 CONSTRAINT [PK_FattureWhiteList] PRIMARY KEY CLUSTERED ([IdLista] ASC)
);
GO

-- =============================================================================================
-- pfd.RelRighe — le righe/notifiche di una REL, lette da RelRigheQueryGetByIdPersistence per
-- generare il "Report di dettaglio notifiche" (Azure Function CreateRelRighe/CreateRelSospese).
-- DDL reale fornita dal team DB.
--
-- ⚠️ Il seed è costruito per DISCRIMINARE un'invariante che il codice esprime in modo fragile e che
-- la documentazione dichiara CORRETTA (business-fatturazione.md, § SEM. SOSPESI):
--
--   la scelta fra "filtra per semestre (FlagConguaglio)" e "filtra per anno/mese" avviene con una
--   RICERCA TESTUALE sul nome della tipologia — contains("var"|"semestrale"|"annuale") — che
--   **'SEM. SOSPESI' NON intercetta**, perché è abbreviato. Quindi SEM. SOSPESI è filtrata per
--   anno/mese, ed è voluto: le sue righe conservano il periodo di riferimento originale.
--
-- Le righe qui sotto rendono la differenza osservabile: se qualcuno "normalizzasse" quella stringa
-- (l'intervento che verrebbe naturale in una pulizia), i test cambierebbero risultato.
-- Test: RelRigheFiltroPeriodoIntegrationTests.
-- =============================================================================================
IF OBJECT_ID('pfd.RelRighe', 'U') IS NULL
CREATE TABLE [pfd].[RelRighe](
	[contract_id] [nvarchar](400) NOT NULL,
	[tax_code] [nvarchar](100) NOT NULL,
	[vat_number] [nvarchar](100) NOT NULL,
	[zip_code] [nvarchar](200) NULL,
	[foreign_state] [nvarchar](200) NULL,
	[number_of_pages] [int] NULL,
	[g_envelope_weight] [nvarchar](200) NULL,
	[cost] [decimal](9, 2) NULL,
	[timeline_category] [nvarchar](max) NULL,
	[paper_product_type] [nvarchar](200) NULL,
	[event_id] [nvarchar](400) NOT NULL,
	[iun] [nvarchar](100) NULL,
	[notification_sent_at] [nvarchar](max) NULL,
	[internal_organization_id] [nvarchar](100) NULL,
	[event_timestamp] [nvarchar](max) NULL,
	[recipient_index] [nvarchar](200) NULL,
	[recipient_type] [nvarchar](20) NULL,
	[recipient_id] [nvarchar](100) NULL,
	[year] [int] NULL,
	[month] [int] NULL,
	[daily] [nvarchar](max) NULL,
	[item_code] [nvarchar](max) NOT NULL,
	[notification_request_id] [nvarchar](max) NOT NULL,
	[recipient_tax_id] [nvarchar](max) NOT NULL,
	[notificationtype] [nvarchar](40) NULL,
	[Recapitista] [nvarchar](100) NULL,
	[invoincingtimestamp] [nvarchar](max) NULL,
	[TipologiaFattura] [nvarchar](40) NOT NULL,
	[IdFlagContestazione] [tinyint] NOT NULL,
	[FlagConguaglio] [nvarchar](50) NULL,
	[AnnoNotifica] [int] NULL,
	[MeseNotifica] [int] NULL,
	[TipologiaRel] [nvarchar](20) NULL,
 CONSTRAINT [PK_RelRighe] PRIMARY KEY CLUSTERED ([event_id] ASC)
);
GO

-- Ente1 / TOKEN-E1. Periodo di riferimento 2026/5, tranne dove indicato.
--   REL-PS-1/2      PRIMO SALDO   2026/5   -> attese sul PRIMO SALDO
--   REL-ASS-1       ASSEVERAZIONE 2026/5   -> DEVE uscire insieme al PRIMO SALDO (OR esplicito nel codice)
--   REL-SS-MAG      SEM. SOSPESI  2026/5   -> attesa chiedendo 2026/5
--   REL-SS-GIU      SEM. SOSPESI  2026/6   -> stesso semestre, mese diverso: NON deve uscire
--                                             (è la prova che SEM. SOSPESI filtra per anno/mese)
--   REL-VS-MAG/GIU  VAR. SEMESTRALE, mesi diversi, stesso FlagConguaglio '2026-S1'
--                                          -> devono uscire ENTRAMBE (filtro per semestre)
IF NOT EXISTS (SELECT 1 FROM pfd.RelRighe WHERE event_id LIKE 'REL-%')
INSERT INTO pfd.RelRighe
 (contract_id, tax_code, vat_number, event_id, iun, internal_organization_id, [year], [month],
  item_code, notification_request_id, recipient_tax_id, notificationtype, cost,
  TipologiaFattura, IdFlagContestazione, FlagConguaglio)
VALUES
 ('TOKEN-E1','TAX1','VAT1','REL-PS-1','IUN-PS-1','11111111-1111-1111-1111-111111111111',2026,5,'IC','NRQ','RTX','Digitali',1.00,'PRIMO SALDO',1,NULL),
 ('TOKEN-E1','TAX1','VAT1','REL-PS-2','IUN-PS-2','11111111-1111-1111-1111-111111111111',2026,5,'IC','NRQ','RTX','Analogico890',3.50,'PRIMO SALDO',1,NULL),
 ('TOKEN-E1','TAX1','VAT1','REL-ASS-1','IUN-ASS-1','11111111-1111-1111-1111-111111111111',2026,5,'IC','NRQ','RTX','Digitali',1.00,'ASSEVERAZIONE',1,NULL),
 ('TOKEN-E1','TAX1','VAT1','REL-SS-MAG','IUN-SS-MAG','11111111-1111-1111-1111-111111111111',2026,5,'IC','NRQ','RTX','Digitali',2.00,'SEM. SOSPESI',1,'2026-S1'),
 ('TOKEN-E1','TAX1','VAT1','REL-SS-GIU','IUN-SS-GIU','11111111-1111-1111-1111-111111111111',2026,6,'IC','NRQ','RTX','Digitali',2.00,'SEM. SOSPESI',1,'2026-S1'),
 ('TOKEN-E1','TAX1','VAT1','REL-VS-MAG','IUN-VS-MAG','11111111-1111-1111-1111-111111111111',2026,5,'IC','NRQ','RTX','Digitali',4.00,'VAR. SEMESTRALE',1,'2026-S1'),
 ('TOKEN-E1','TAX1','VAT1','REL-VS-GIU','IUN-VS-GIU','11111111-1111-1111-1111-111111111111',2026,6,'IC','NRQ','RTX','Digitali',4.00,'VAR. SEMESTRALE',1,'2026-S1');
GO

-- Testate REL richieste da RelRigheQueryGetByIdHandler: prima di leggere le righe l'handler cerca la
-- TESTATA del periodo e ne prende il FlagConguaglio (sovrascrivendo quello passato nella query).
-- Senza la testata fa FirstOrDefault()! su una lista vuota -> NullReferenceException.
-- Servono quindi le testate di SEM. SOSPESI e VAR. SEMESTRALE 2026/5 (quella del PRIMO SALDO c'è già,
-- seedata per vwRelDettaglio). Il FlagConguaglio '2026-S1' è ciò che fa uscire entrambi i mesi nel
-- ramo conguaglio.
IF NOT EXISTS (SELECT 1 FROM pfd.RelTestata WHERE contract_id='TOKEN-E1' AND [year]=2026 AND [month]=5 AND TipologiaFattura IN ('SEM. SOSPESI','VAR. SEMESTRALE'))
INSERT INTO pfd.RelTestata
 (internal_organization_id, contract_id, TipologiaFattura, [year], [month], TotaleAnalogico, TotaleDigitale,
  TotaleNotificheAnalogiche, TotaleNotificheDigitali, Totale, TotaleAnalogicoIva, TotaleDigitaleIva, TotaleIva,
  Caricata, RelFatturata, FlagConguaglio,
  AsseverazioneTotaleAnalogico, AsseverazioneTotaleDigitale, AsseverazioneTotaleNotificheAnalogiche,
  AsseverazioneTotaleNotificheDigitali, AsseverazioneTotale, AsseverazioneTotaleAnalogicoIva,
  AsseverazioneTotaleDigitaleIva, AsseverazioneTotaleIva)
VALUES
 ('11111111-1111-1111-1111-111111111111','TOKEN-E1','SEM. SOSPESI',   2026,5, 2.00,2.00,1,1,4.00,2.44,2.44,4.88,0,0,'2026-S1', 0,0,0,0,0,0,0,0),
 ('11111111-1111-1111-1111-111111111111','TOKEN-E1','VAR. SEMESTRALE',2026,5, 4.00,4.00,1,1,8.00,4.88,4.88,9.76,0,0,'2026-S1', 0,0,0,0,0,0,0,0);
GO
