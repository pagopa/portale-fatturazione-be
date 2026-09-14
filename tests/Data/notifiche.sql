-- =============================================================================================
-- Notifiche SEND: tabelle e seed per POST api/notifiche/pagopa (BE-SMOKE-03 del testbook backend).
--
-- Catena letta dall'endpoint (NotificaSQLBuilder.SelectAll/SelectAllCount): nessuna vista, solo
-- tabelle. pfd.Enti e pfd.Contratti arrivano gia' da setup.sql.
--
--   pfd.Notifiche              n   FROM
--   pfd.Enti                   e   INNER JOIN   (gia' seedata)
--   pfd.Contratti              c   INNER JOIN   (gia' seedata)  -> attenzione: senza contratto la
--                                                                  notifica sparisce in silenzio
--   pfd.Notifiche_CodiceOggetto g  LEFT JOIN
--   pfw.Contestazioni          t   LEFT JOIN
--   pfw.FlagContestazione      f   INNER JOIN su ISNULL(t.FkIdFlagContestazione, 1)
--   pfw.TipoContestazione      a   LEFT JOIN
--
-- Due conseguenze da tenere presenti scrivendo altri seed qui:
--  1) l'INNER JOIN su FlagContestazione con ISNULL(...,1) rende la riga 1 (Non Contestata)
--     OBBLIGATORIA: senza, TUTTE le notifiche non contestate spariscono dal risultato;
--  2) l'INNER JOIN su Contratti moltiplica le righe se un ente ha piu' contratti — nel seed
--     ogni ente ne ha uno solo, quindi il conteggio resta 1:1 con le notifiche.
--
-- pfd.NotificheCount non e' letta da questa rotta (la usano i conteggi/periodi), ma e' la tabella
-- che rende visibili le notifiche sul portale: la creiamo e seediamo per coerenza del periodo.
--
-- DDL allineate a INFORMATION_SCHEMA del DB reale (estratto 2026-08-06).
-- =============================================================================================

IF OBJECT_ID('pfd.Notifiche', 'U') IS NULL
CREATE TABLE [pfd].[Notifiche](
	[contract_id] [nvarchar](max) NULL,
	[tax_code] [nvarchar](100) NOT NULL,
	[vat_number] [nvarchar](100) NOT NULL,
	[zip_code] [nvarchar](200) NULL,
	[foreign_state] [nvarchar](200) NULL,
	[number_of_pages] [int] NULL,
	[g_envelope_weight] [nvarchar](200) NULL,
	[cost_eurocent] [bigint] NULL,
	[timeline_category] [nvarchar](max) NULL,
	[paper_product_type] [nvarchar](200) NULL,
	[event_id] [nvarchar](400) NULL,
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
	[TipologiaFattura] [nvarchar](40) NULL,
	[Fatturabile] [bit] NULL,
	[Consolidatore] [nvarchar](100) NULL,
	[is_deleted] [bit] NOT NULL CONSTRAINT [DF_Notifiche_is_deleted] DEFAULT ((0)),
	[deleted_date] [datetime2](7) NOT NULL CONSTRAINT [DF_Notifiche_deleted_date] DEFAULT ('9999-12-31')
);
GO

IF OBJECT_ID('pfd.Notifiche_CodiceOggetto', 'U') IS NULL
CREATE TABLE [pfd].[Notifiche_CodiceOggetto](
	[event_id] [nvarchar](400) NULL,
	[iun] [nvarchar](100) NULL,
	[year] [int] NULL,
	[month] [int] NULL,
	[CodiceOggetto] [nvarchar](400) NOT NULL
);
GO

IF OBJECT_ID('pfd.NotificheCount', 'U') IS NULL
CREATE TABLE [pfd].[NotificheCount](
	[internal_organization_id] [nvarchar](100) NOT NULL,
	[contract_id] [nvarchar](400) NULL,
	[year] [int] NOT NULL,
	[month] [int] NOT NULL,
	[TotaleAnalogico] [decimal](9, 2) NULL,
	[TotaleDigitale] [decimal](9, 2) NULL,
	[TotaleNotificheAnalogiche] [int] NULL,
	[TotaleNotificheDigitali] [int] NULL,
	[Totale] [decimal](9, 2) NULL,
	[Iva] [decimal](4, 2) NOT NULL CONSTRAINT [DF_NotificheCount_Iva] DEFAULT ((22)),
	[TotaleAnalogicoIva] [decimal](9, 2) NULL,
	[TotaleDigitaleIva] [decimal](9, 2) NULL,
	[TotaleIva] [decimal](9, 2) NULL
);
GO

-- Versione e' un rowversion (timestamp): generata dal motore, mai valorizzata negli INSERT.
IF OBJECT_ID('pfw.FlagContestazione', 'U') IS NULL
CREATE TABLE [pfw].[FlagContestazione](
	[IdFlagContestazione] [tinyint] NOT NULL,
	[FlagContestazione] [nvarchar](300) NOT NULL,
	[Descrizione] [nvarchar](700) NULL,
	[Versione] [timestamp] NOT NULL,
	[LastUpdate] [datetime2](7) NOT NULL CONSTRAINT [DF_FlagContestazione_LastUpdate] DEFAULT (GETDATE()),
 CONSTRAINT [PK_FlagContestazione] PRIMARY KEY CLUSTERED ([IdFlagContestazione] ASC)
);
GO

IF OBJECT_ID('pfw.TipoContestazione', 'U') IS NULL
CREATE TABLE [pfw].[TipoContestazione](
	[IdTipoContestazione] [int] NOT NULL,
	[TipoContestazione] [nvarchar](200) NOT NULL,
	[Versione] [timestamp] NOT NULL,
	[LastUpdate] [datetime2](7) NOT NULL CONSTRAINT [DF_TipoContestazione_LastUpdate] DEFAULT (GETDATE()),
	[FlagContestazione] [varchar](10) NOT NULL,
	[DataModificaFlag] [datetime] NULL,
 CONSTRAINT [PK_TipoContestazione] PRIMARY KEY CLUSTERED ([IdTipoContestazione] ASC)
);
GO

IF OBJECT_ID('pfw.Contestazioni', 'U') IS NULL
CREATE TABLE [pfw].[Contestazioni](
	[IdContestazione] [int] IDENTITY(1,1) NOT NULL,
	[FkIdNotifica] [nvarchar](400) NOT NULL,
	[FkIdTipoContestazione] [int] NOT NULL,
	[NoteEnte] [nvarchar](max) NULL,
	[NoteSend] [nvarchar](max) NULL,
	[NoteRecapitista] [nvarchar](max) NULL,
	[NoteConsolidatore] [nvarchar](max) NULL,
	[RispostaEnte] [nvarchar](max) NULL,
	[FkIdFlagContestazione] [tinyint] NOT NULL,
	[Onere] [nvarchar](100) NULL,
	[DataInserimentoEnte] [datetime] NOT NULL CONSTRAINT [DF_Contestazioni_DataInserimentoEnte] DEFAULT (GETDATE()),
	[DataModificaEnte] [datetime] NULL,
	[DataInserimentoSend] [datetime] NULL,
	[DataModificaSend] [datetime] NULL,
	[DataInserimentoRecapitista] [datetime] NULL,
	[DataModificaRecapitista] [datetime] NULL,
	[DataInserimentoConsolidatore] [datetime] NULL,
	[DataModificaConsolidatore] [datetime] NULL,
	[DataChiusura] [datetime] NULL,
	[Anno] [smallint] NOT NULL,
	[Mese] [smallint] NOT NULL,
 CONSTRAINT [PK_Contestazioni] PRIMARY KEY CLUSTERED ([IdContestazione] ASC)
);
GO

-- Lookup StatoContestazione (v. docs/dominio-glossario.md). La riga 1 e' quella che tiene in vita
-- le notifiche NON contestate nell'INNER JOIN: non rimuoverla.
IF NOT EXISTS (SELECT 1 FROM pfw.FlagContestazione)
INSERT INTO pfw.FlagContestazione (IdFlagContestazione, FlagContestazione, Descrizione) VALUES
 (1, N'Non Contestata',          N'Nessuna contestazione aperta'),
 (2, N'Annullata',               N'Contestazione ritirata dall''Ente'),
 (3, N'Contestata Ente',         N'Contestazione aperta dall''Ente'),
 (4, N'Risposta Send',           N'Risposta del supporto SEND'),
 (5, N'Risposta Recapitista',    N'Risposta del Recapitista'),
 (6, N'Risposta Consolidatore',  N'Risposta del Consolidatore'),
 (7, N'Risposta Ente',           N'Risposta dell''Ente'),
 (8, N'Accettata',               N'Contestazione accolta: costo a carico di SEND'),
 (9, N'Chiusa',                  N'Contestazione rifiutata: notifica fatturabile');
GO

IF NOT EXISTS (SELECT 1 FROM pfw.TipoContestazione)
INSERT INTO pfw.TipoContestazione (IdTipoContestazione, TipoContestazione, FlagContestazione) VALUES
 (1, N'Mancato recapito',   'S'),
 (2, N'Indirizzo errato',   'S');
GO

-- ---------------------------------------------------------------------------------------------
-- Seed: ente1 (TOKEN-E1), periodo dedicato 2026/3, tre notifiche:
--   EVT-3001 analogica  NON contestata  -> esce con Contestazione='Non Contestata' (ramo ISNULL)
--   EVT-3002 analogica  CONTESTATA (stato 3, tipo 1) + CodiceOggetto -> esercita i 3 JOIN opzionali
--   EVT-3003 digitale   NON contestata, gia' fatturata (TipologiaFattura='PRIMO SALDO')
-- ---------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM pfd.Notifiche WHERE event_id IN ('EVT-3001','EVT-3002','EVT-3003'))
INSERT INTO pfd.Notifiche
 (contract_id, tax_code, vat_number, zip_code, foreign_state, number_of_pages, g_envelope_weight,
  cost_eurocent, timeline_category, paper_product_type, event_id, iun, notification_sent_at,
  internal_organization_id, event_timestamp, recipient_index, recipient_type, recipient_id,
  [year], [month], daily, item_code, notification_request_id, recipient_tax_id, notificationtype,
  Recapitista, Consolidatore, TipologiaFattura, Fatturabile)
VALUES
 ('TOKEN-E1', 'RSSMRA80A01H501U', '12345678901', '00100', NULL, 2, '20', 210, 'SEND_ANALOG_DOMICILE', 'AR',
  'EVT-3001', 'IUN-3001', '2026-03-02', '11111111-1111-1111-1111-111111111111', '2026-03-05T10:00:00',
  '0', 'PF', 'REC-3001', 2026, 3, '2026-03-05', 'IC-3001', 'NRQ-3001', 'TAX-3001', 'AnalogicoARNazionali',
  'Recapitista Uno', 'Consolidatore Uno', NULL, 1),
 ('TOKEN-E1', 'VRDLGI75B02F205X', '12345678901', '20100', NULL, 3, '35', 350, 'SEND_ANALOG_DOMICILE', '890',
  'EVT-3002', 'IUN-3002', '2026-03-03', '11111111-1111-1111-1111-111111111111', '2026-03-06T11:30:00',
  '0', 'PF', 'REC-3002', 2026, 3, '2026-03-06', 'IC-3002', 'NRQ-3002', 'TAX-3002', 'Analogico890',
  'Recapitista Uno', 'Consolidatore Uno', NULL, 0),
 ('TOKEN-E1', 'BNCPLA90C03L219Z', '12345678901', NULL, NULL, NULL, NULL, 100, 'SEND_DIGITAL_DOMICILE', NULL,
  'EVT-3003', 'IUN-3003', '2026-03-04', '11111111-1111-1111-1111-111111111111', '2026-03-07T09:15:00',
  '0', 'PF', 'REC-3003', 2026, 3, '2026-03-07', 'IC-3003', 'NRQ-3003', 'TAX-3003', 'Digitali',
  NULL, NULL, 'PRIMO SALDO', 1);
GO

IF NOT EXISTS (SELECT 1 FROM pfd.Notifiche_CodiceOggetto WHERE event_id = 'EVT-3002')
INSERT INTO pfd.Notifiche_CodiceOggetto (event_id, iun, [year], [month], CodiceOggetto) VALUES
 ('EVT-3002', 'IUN-3002', 2026, 3, 'CODOGG-3002');
GO

IF NOT EXISTS (SELECT 1 FROM pfw.Contestazioni WHERE FkIdNotifica = 'EVT-3002')
INSERT INTO pfw.Contestazioni
 (FkIdNotifica, FkIdTipoContestazione, FkIdFlagContestazione, NoteEnte, Onere, DataInserimentoEnte, Anno, Mese)
VALUES
 ('EVT-3002', 1, 3, N'Notifica mai recapitata', N'Recapitista', '2026-03-10', 2026, 3);
GO

-- Il "count" mensile: senza questa riga le notifiche del periodo non sono visibili sul portale.
IF NOT EXISTS (SELECT 1 FROM pfd.NotificheCount WHERE internal_organization_id = '11111111-1111-1111-1111-111111111111' AND [year] = 2026 AND [month] = 3)
INSERT INTO pfd.NotificheCount
 (internal_organization_id, contract_id, [year], [month], TotaleAnalogico, TotaleDigitale,
  TotaleNotificheAnalogiche, TotaleNotificheDigitali, Totale, Iva, TotaleAnalogicoIva, TotaleDigitaleIva, TotaleIva)
VALUES
 ('11111111-1111-1111-1111-111111111111', 'TOKEN-E1', 2026, 3, 5.60, 1.00, 2, 1, 6.60, 22, 6.83, 1.22, 8.05);
GO

-- =============================================================================================
-- Calendario delle contestazioni: e' il "cancello" temporale di ogni azione su una contestazione
-- (v. docs/business-contestazioni.md). Letto da CalendarioContestazioneQueryGetPersistence, che
-- l'handler AzioneContestazioneQueryGetByIdNotifica interroga per il periodo DELLA NOTIFICA.
--
-- DDL reale del DB (fornita dal team DB, 2026-08-13), riprodotta as-is: nessuna PK, nessun indice.
--
-- ⚠️ Tre trappole, tutte per la stessa causa — CalendarioContestazioneQueryGetPersistence avvolge
--    la query in un `catch { return null; }`, e l'handler traduce il null in un calendario con
--    Valid/ValidVerifica = false, cioe' TUTTI I PERMESSI NEGATI. Qualunque errore di lettura si
--    presenta quindi come "finestra chiusa", non come errore:
--
--   1) la tabella ASSENTE non fa fallire nulla: nega e basta. E' il motivo per cui
--      AzioneContestazioneIntegrationTests ha una guardia in [SetUp] che si auto-ignora invece di
--      fidarsi di una suite verde;
--   2) DataVerifica e' NULLABLE a DB ma e' `DateTime` NON nullable su CalendarioContestazione:
--      una riga con DataVerifica NULL fa fallire il mapping Dapper -> catch -> tutto negato.
--      Per questo qui e' sempre valorizzata;
--   3) stesso discorso per DataChiusuraContestazioni e DataFineRisposteContestazioni, che
--      SelectAll() aliasa su ChiusuraContestazioni/TempoRisposta (anch'esse non nullable).
--      Da notare che quei due nomi di colonna, dichiarati negli attributi [Column] dell'entita',
--      NON esistono nella tabella reale: funziona solo perche' SelectByAnnoMese non li seleziona
--      e SelectAll li aliasa a mano.
--
-- Le date sono volutamente ESTREME e non realistiche: la suite confronta con GETDATE(), quindi una
-- finestra "aperta" scritta con date plausibili scadrebbe da sola col passare dei mesi, e i test
-- diventerebbero verdi a vuoto (v. trappola 1). 2026/3 e' aperta per sempre, 2026/4 chiusa per
-- sempre.
-- =============================================================================================

IF OBJECT_ID('pfw.ContestazioniCalendario', 'U') IS NULL
CREATE TABLE [pfw].[ContestazioniCalendario](
	[MeseContestazione] [int] NOT NULL,
	[AnnoContestazione] [int] NOT NULL,
	[DataInizio] [datetime] NOT NULL,
	[DataFine] [datetime] NOT NULL,
	[DataVerifica] [datetime] NULL,
	[DataCalcoloPrimoSecondo] [datetime] NULL,
	[DataChiusuraContestazioni] [datetime] NULL,
	[DataFineRisposteContestazioni] [datetime] NULL
);
GO

-- 2026/3 -> finestra APERTA (DataFine/DataVerifica nel futuro remoto): e' il periodo delle tre
--           notifiche seedate sopra, quindi quello su cui le asserzioni positive hanno senso.
-- 2026/4 -> finestra CHIUSA (tutte le date nel passato): serve a provare end-to-end che il
--           calendario e' davvero il cancello, con la notifica EVT-3004 qui sotto.
IF NOT EXISTS (SELECT 1 FROM pfw.ContestazioniCalendario WHERE AnnoContestazione = 2026 AND MeseContestazione IN (3, 4))
INSERT INTO pfw.ContestazioniCalendario
 (MeseContestazione, AnnoContestazione, DataInizio, DataFine, DataVerifica,
  DataCalcoloPrimoSecondo, DataChiusuraContestazioni, DataFineRisposteContestazioni)
VALUES
 (3, 2026, '2026-03-05', '2099-12-31', '2099-12-31', '2099-12-31', '2099-12-31', '2099-12-31'),
 (4, 2026, '2026-04-05', '2026-05-05', '2026-05-20', '2026-05-25', '2026-05-20', '2026-05-15');
GO

-- EVT-3004: contestata (stato 3) come EVT-3002, ma nel periodo 2026/4 a finestra chiusa. Unica
-- differenza rilevante: il periodo. Fatturabile = 0, cosi' l'unico "no" possibile viene dal
-- calendario e non dal lock di fatturazione.
IF NOT EXISTS (SELECT 1 FROM pfd.Notifiche WHERE event_id = 'EVT-3004')
INSERT INTO pfd.Notifiche
 (contract_id, tax_code, vat_number, zip_code, foreign_state, number_of_pages, g_envelope_weight,
  cost_eurocent, timeline_category, paper_product_type, event_id, iun, notification_sent_at,
  internal_organization_id, event_timestamp, recipient_index, recipient_type, recipient_id,
  [year], [month], daily, item_code, notification_request_id, recipient_tax_id, notificationtype,
  Recapitista, Consolidatore, TipologiaFattura, Fatturabile)
VALUES
 ('TOKEN-E1', 'MRTFNC85D04H501K', '12345678901', '00100', NULL, 2, '20', 210, 'SEND_ANALOG_DOMICILE', 'AR',
  'EVT-3004', 'IUN-3004', '2026-04-02', '11111111-1111-1111-1111-111111111111', '2026-04-06T10:00:00',
  '0', 'PF', 'REC-3004', 2026, 4, '2026-04-06', 'IC-3004', 'NRQ-3004', 'TAX-3004', 'AnalogicoARNazionali',
  'Recapitista Uno', 'Consolidatore Uno', NULL, 0);
GO

IF NOT EXISTS (SELECT 1 FROM pfw.Contestazioni WHERE FkIdNotifica = 'EVT-3004')
INSERT INTO pfw.Contestazioni
 (FkIdNotifica, FkIdTipoContestazione, FkIdFlagContestazione, NoteEnte, Onere, DataInserimentoEnte, Anno, Mese)
VALUES
 ('EVT-3004', 1, 3, N'Contestazione su periodo ormai chiuso', N'Recapitista', '2026-04-10', 2026, 4);
GO
