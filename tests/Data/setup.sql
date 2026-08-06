IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'pfw')
    BEGIN
    EXEC ('CREATE SCHEMA pfw;');
    END;

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'pfd')
    BEGIN
    EXEC ('CREATE SCHEMA pfd;');
    END; 

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'stg')
	BEGIN
	EXEC ('CREATE SCHEMA stg;');
	END;

IF OBJECT_ID('[stg].[PspEmailPreview]', 'U') IS NULL
BEGIN
	CREATE TABLE [stg].[PspEmailPreview]
	(
		[Id] BIGINT IDENTITY(1,1) NOT NULL,
		[IdContratto] NVARCHAR(100) NULL,
		[Tipologia] NVARCHAR(100) NULL,
		[Anno] INT NULL,
		[Trimestre] NVARCHAR(20) NULL,
		[DataEvento] NVARCHAR(50) NULL,
		[Email] NVARCHAR(320) NULL,
		[Oggetto] NVARCHAR(MAX) NULL,
		[Corpo] NVARCHAR(MAX) NULL,
		[Link] NVARCHAR(MAX) NULL,
		[RagioneSociale] NVARCHAR(500) NULL,
		[Invio] BIT NOT NULL CONSTRAINT [DF_PspEmailPreview_Invio] DEFAULT(0),
		[TipoContratto] NVARCHAR(100) NULL,
		CONSTRAINT [PK_PspEmailPreview] PRIMARY KEY CLUSTERED ([Id] ASC)
	);
END;

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'ppa')
	BEGIN
	EXEC ('CREATE SCHEMA ppa;');
	END;

IF OBJECT_ID('[ppa].[PspEmail]', 'U') IS NOT NULL
BEGIN
	IF COL_LENGTH('ppa.PspEmail', 'Oggetto') IS NULL
	BEGIN
		ALTER TABLE [ppa].[PspEmail] ADD [Oggetto] NVARCHAR(MAX) NULL;
	END;

	IF COL_LENGTH('ppa.PspEmail', 'Corpo') IS NULL
	BEGIN
		ALTER TABLE [ppa].[PspEmail] ADD [Corpo] NVARCHAR(MAX) NULL;
	END;

	IF COL_LENGTH('ppa.PspEmail', 'Link') IS NULL
	BEGIN
		ALTER TABLE [ppa].[PspEmail] ADD [Link] NVARCHAR(MAX) NULL;
	END;
END;

CREATE TABLE pfw.CategoriaSpedizione (
	IdCategoriaSpedizione int IDENTITY(1,1) NOT NULL,
	Descrizione nvarchar(250) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Tipo nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK__Categori__0E5B57A455A55002 PRIMARY KEY (IdCategoriaSpedizione)
);
 

CREATE TABLE pfw.Form (
	IdForm int NOT NULL,
	Descrizione varchar(250) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK__Form__007D03D97A238DBC PRIMARY KEY (IdForm)
);
 

CREATE TABLE pfw.Log (
	FkIdEnte nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	IdUtente nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DataEvento datetime NOT NULL,
	DescrizioneEvento nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	JsonTransazione text COLLATE SQL_Latin1_General_CP1_CI_AS NULL
);
 

CREATE TABLE pfw.Prodotti (
	Prodotto nvarchar(15) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CONSTRAINT PK__Prodotti__3EE5F2F6209BD2F1 PRIMARY KEY (Prodotto)
);

 

CREATE TABLE pfw.Stato (
	Stato nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Default] bit NULL,
	CONSTRAINT PK__Stato__BA803DA6AD8CBD05 PRIMARY KEY (Stato)
);

 

CREATE TABLE pfw.Step (
	IdStep int NOT NULL,
	Descrizione varchar(250) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK__Step__A3FC8BAD5173134C PRIMARY KEY (IdStep)
);

 

CREATE TABLE pfw.TipoCommessa (
	TipoCommessa nvarchar(1) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Descrizione nvarchar(250) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK__TipoComm__E5339F96F1E97E24 PRIMARY KEY (TipoCommessa)
);

 

CREATE TABLE pfw.TipoContratto (
	IdTipoContratto bigint IDENTITY(1,1) NOT NULL,
	Descrizione nvarchar(3) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CONSTRAINT PK__TipoCont__D8826341BD065CDF PRIMARY KEY (IdTipoContratto)
);

 

CREATE TABLE pfw.DatiFatturazione (
	IdDatiFatturazione bigint IDENTITY(1,1) NOT NULL,
	-- Nullabilita' e ordine colonne allineati a un estratto della tabella reale (2247 righe,
	-- 2026-07-24). Le colonne opzionali arrivano quasi sempre come stringa vuota, non NULL, ma il
	-- NULL esiste: dichiararle NOT NULL faceva fallire i test con violazioni fasulle.
	--   Cup           1 NULL, 2059 su 2247 stringa vuota, lunghezza max 15
	--   Cig           NULL nel 100% delle righe: mai scritta dal codice, colonna vestigiale
	--   CodCommessa   1 NULL, lunghezza max 99
	--   DataDocumento 4 NULL  -> conferma l'assert "IsNull(DataDocumento)" dei test create
	--   IdDocumento   1 NULL, 1795 stringa vuota, lunghezza max 20
	--   Map           NULL nel 100% delle righe
	Cup nvarchar(15) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Cig nvarchar(10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CodCommessa nvarchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DataDocumento datetime NULL,
	SplitPayment bit NOT NULL,
	FkIdEnte nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	IdDocumento nvarchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DataCreazione datetime NOT NULL,
	DataModifica datetime NULL,
	[Map] nvarchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	FkTipoCommessa nvarchar(1) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	PEC nvarchar(250) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	FkProdotto nvarchar(15) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	-- presente nell'entita' Core DatiFatturazione e nell'INSERT: senza, la SELECT costruita da
	-- DatiFatturazioneSQLBuilder fallisce con "Invalid column name 'NotaLegale'"
	NotaLegale bit NOT NULL CONSTRAINT DF_DatiFatturazione_NotaLegale DEFAULT(0),
	-- scritta da DatiFatturazioneCreateCommandPersistence/UpdateCommandPersistence (PF-705).
	-- Lunghezza 7: nei dati reali il codice destinatario SDI e' 6-7 caratteri (911 righe NULL),
	-- coerente con la specifica SDI. La lunghezza DICHIARATA non e' stata verificata: se il DB
	-- reale la tiene piu' larga, allargare anche qui.
	CodiceSDI nvarchar(7) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK__DatiFatt__5A190E0F178A5F57 PRIMARY KEY (IdDatiFatturazione),
	CONSTRAINT FkProdotto_DatiFatturazione FOREIGN KEY (FkProdotto) REFERENCES pfw.Prodotti(Prodotto),
	CONSTRAINT FkTipoCommessaDatiFatturazione FOREIGN KEY (FkTipoCommessa) REFERENCES pfw.TipoCommessa(TipoCommessa)
); 
 

CREATE TABLE pfw.DatiFatturazioneContatti (
	FkIdDatiFatturazione bigint NOT NULL,
	Email nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CONSTRAINT FkIdDatiFatturazioneContatti FOREIGN KEY (FkIdDatiFatturazione) REFERENCES pfw.DatiFatturazione(IdDatiFatturazione)
);

 

CREATE TABLE pfw.DatiModuloCommessaTotali (
	FkIdEnte nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	FkIdTipoContratto bigint NOT NULL,
	FkProdotto nvarchar(15) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	AnnoValidita int NOT NULL,
	MeseValidita int NOT NULL,
	FkIdCategoriaSpedizione int NOT NULL,
	-- allineamento al codice attuale (DatiModuloCommessaCreateTotaleCommandPersistence): la MERGE usa
	-- FkIdStato (non FkStato) e scrive anche PercentualeCategoria/Totale/Fatturabile.
	FkIdStato nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	TotaleCategoria decimal(9,2) NOT NULL,
	PercentualeCategoria int NULL,
	Totale decimal(9,2) NULL,
	Fatturabile bit NULL,
	CONSTRAINT PK_DatiModuloCommessaTotali PRIMARY KEY (FkIdEnte,FkIdTipoContratto,FkProdotto,AnnoValidita,MeseValidita,FkIdCategoriaSpedizione),
	CONSTRAINT FK_DatiModuloCommessaTotali_CategoriaSpedizione FOREIGN KEY (FkIdCategoriaSpedizione) REFERENCES pfw.CategoriaSpedizione(IdCategoriaSpedizione),
	CONSTRAINT FK_DatiModuloCommessaTotali_Prodotti FOREIGN KEY (FkProdotto) REFERENCES pfw.Prodotti(Prodotto),
	CONSTRAINT FK_DatiModuloCommessaTotali_Stato FOREIGN KEY (FkIdStato) REFERENCES pfw.Stato(Stato),
	CONSTRAINT FK_DatiModuloCommessaTotali_TipoContratto FOREIGN KEY (FkIdTipoContratto) REFERENCES pfw.TipoContratto(IdTipoContratto)
);

 

CREATE TABLE pfw.PercentualeAnticipo (
	FkProdotto nvarchar(15) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	FkIdTipoContratto bigint NOT NULL,
	FkIdCategoriaSpedizione int NOT NULL,
	Percentuale int NOT NULL,
	Descrizione varchar(250) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	DataInizioValidita datetime NOT NULL,
	DataFineValidita datetime NULL,
	DataCreazione datetime NOT NULL,
	DataModifica datetime NULL,
	CONSTRAINT FkIdCategoriaSpedizioneAnticipo FOREIGN KEY (FkIdCategoriaSpedizione) REFERENCES pfw.CategoriaSpedizione(IdCategoriaSpedizione),
	CONSTRAINT FkIdTipoContratto FOREIGN KEY (FkIdTipoContratto) REFERENCES pfw.TipoContratto(IdTipoContratto),
	CONSTRAINT FkProdotto FOREIGN KEY (FkProdotto) REFERENCES pfw.Prodotti(Prodotto)
);

 

CREATE TABLE pfw.Scadenziario (
	FkIdProdotto nvarchar(15) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	FkIdStep int NOT NULL,
	FkIdForm int NULL,
	Ordine int NOT NULL,
	GiornoInizio int NOT NULL,
	OraInizio time NULL,
	GiornoFine int NOT NULL,
	OraFine time NULL,
	ProcessoBatch bit NULL,
	CONSTRAINT FK_Scadenziario_Form FOREIGN KEY (FkIdForm) REFERENCES pfw.Form(IdForm),
	CONSTRAINT FK_Scadenziario_Prodotti FOREIGN KEY (FkIdProdotto) REFERENCES pfw.Prodotti(Prodotto),
	CONSTRAINT FK_Scadenziario_Step FOREIGN KEY (FkIdStep) REFERENCES pfw.Step(IdStep)
);

 

CREATE TABLE pfw.TipoSpedizione (
	IdTipoSpedizione int IDENTITY(1,1) NOT NULL,
	Descrizione nvarchar(250) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	FkIdCategoriaSpedizione int NOT NULL,
	Tipo nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK__TipoSped__33BE995DD9717E6A PRIMARY KEY (IdTipoSpedizione),
	CONSTRAINT FkIdCategoriaSpedizione FOREIGN KEY (FkIdCategoriaSpedizione) REFERENCES pfw.CategoriaSpedizione(IdCategoriaSpedizione)
);
 

CREATE TABLE pfw.CostoNotifiche (
	MediaNotificaNazionale decimal(5,2) NOT NULL,
	MediaNotificaInternazionale decimal(5,2) NULL,
	FkIdTipoSpedizione int NOT NULL,
	FkIdTipoContratto bigint NOT NULL,
	FkProdotto nvarchar(15) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	DataInizioValidita datetime NOT NULL,
	DataFineValidita datetime NULL,
	DataCreazione datetime NOT NULL,
	DataModifica datetime NULL,
	Descrizione varchar(250) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT FkProdottoNotifiche FOREIGN KEY (FkProdotto) REFERENCES pfw.Prodotti(Prodotto),
	CONSTRAINT FkTipoContratto FOREIGN KEY (FkIdTipoContratto) REFERENCES pfw.TipoContratto(IdTipoContratto),
	CONSTRAINT FkTipoSpedizione FOREIGN KEY (FkIdTipoSpedizione) REFERENCES pfw.TipoSpedizione(IdTipoSpedizione)
);

 

-- Tabella scritta da DatiModuloCommessaValoriRegioniInsertCommandPersistence.
-- NB: definizione INFERITA da INSERT + ValoriRegioneDto (nel seed mancava del tutto) —
-- da confrontare con la definizione reale di produzione. Colonna [890]: nome che inizia per cifra.
CREATE TABLE pfw.DatiModuloCommessaRegioni (
	Internalistitutionid nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Anno int NOT NULL,
	Mese int NOT NULL,
	Provincia nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Regione nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	AR int NULL,
	[890] int NULL
);

CREATE TABLE pfw.DatiModuloCommessa (
	NumeroNotificheNazionali int NOT NULL,
	NumeroNotificheInternazionali int NOT NULL,
	FkIdTipoSpedizione int NOT NULL,
	DataCreazione datetime NOT NULL,
	DataModifica datetime NULL,
	FkIdEnte nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	FKIdTipoContratto bigint NOT NULL,
	FkIdStato nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	FkProdotto nvarchar(15) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	AnnoValidita int NOT NULL,
	MeseValidita int NOT NULL,
	-- allineamento alla definizione reale di pfw.DatiModuloCommessa (valori/prezzi calcolati).
	-- NB: in produzione FkIdEnte/FkIdStato sono nvarchar(100); qui restano 50 per coerenza con le
	-- FK verso pfw.Stato/Prodotti gia' definite in questo seed.
	ValoreNazionali decimal(9,2) NULL DEFAULT ((0)),
	PrezzoNazionali decimal(9,2) NULL DEFAULT ((0)),
	ValoreInternazionali decimal(9,2) NULL DEFAULT ((0)),
	PrezzoInternazionali decimal(9,2) NULL DEFAULT ((0)),
	CONSTRAINT PK_DatiModuloCommessa PRIMARY KEY (FkIdEnte,FKIdTipoContratto,FkProdotto,AnnoValidita,MeseValidita,FkIdTipoSpedizione),
	CONSTRAINT FKIdStatoCommessa FOREIGN KEY (FkIdStato) REFERENCES pfw.Stato(Stato),
	CONSTRAINT FK_DatiModuloCommessa_Prodotti FOREIGN KEY (FkProdotto) REFERENCES pfw.Prodotti(Prodotto),
	CONSTRAINT FK_DatiModuloCommessa_TipoContratto FOREIGN KEY (FKIdTipoContratto) REFERENCES pfw.TipoContratto(IdTipoContratto),
	CONSTRAINT FK_DatiModuloCommessa_TipoSpedizione FOREIGN KEY (FkIdTipoSpedizione) REFERENCES pfw.TipoSpedizione(IdTipoSpedizione)
);

INSERT INTO  pfw.TipoContratto
( Descrizione)
VALUES('PAL');
INSERT INTO pfw.TipoContratto
(Descrizione)
VALUES('PAC');

INSERT INTO  pfw.Prodotti
(Prodotto)
VALUES('prod-pn'); 

-- NB: valorizzare anche Tipo: il codice (DatiModuloCommessaExtensions.GetTotali) individua la
-- categoria digitale con Tipo.Contains("digitale"). Con solo Descrizione si ottiene un NRE.
INSERT INTO pfw.CategoriaSpedizione
(Tipo, Descrizione)
VALUES('Analogico', 'Analogico');

INSERT INTO pfw.CategoriaSpedizione
(Tipo, Descrizione)
VALUES('Digitale', 'Digitale');

INSERT INTO pfw.TipoSpedizione
(Tipo, Descrizione, FkIdCategoriaSpedizione)
VALUES('Analog. A/R', 'Numero complessivo delle notifiche da processare in via analogica tramite Raccomandata A/R nel mese di riferimento
', 1);

INSERT INTO pfw.TipoSpedizione
(Tipo, Descrizione, FkIdCategoriaSpedizione)
VALUES('Analog. L. 890/82', 'Numero complessivo delle notifiche da processare in via analogica del tipo notifica ex L. 890/1982 nel mese di riferimento
', 1);

INSERT INTO pfw.TipoSpedizione
(Tipo, Descrizione, FkIdCategoriaSpedizione)
VALUES('Digitale', 'Numero complessivo delle notifiche da processare in via digitale nel mese di riferimento', 2);

INSERT INTO pfw.TipoCommessa (TipoCommessa,Descrizione) VALUES
	 (N'1',N'Ordine'),
	 (N'2',N'Contratto');

INSERT INTO pfw.Stato (Stato,[Default]) VALUES
	 (N'Apera/Non Caricato',0),
	 (N'Aperta/Caricato',1),
	 (N'Archiviato',0),
	 (N'Chiusa/Caricato',0),
	 (N'Chiusa/Stimato',0);
 
CREATE TABLE pfd.Contratti (
	internalistitutionid nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	product nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	filename nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	onboardingtokenid nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	pricingplan nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	updatedat nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	createdat datetime2 NULL,
	closedat datetime2 NULL,
	[year] int NULL,
	[month] int NULL,
	daily nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LastModified datetime2 NULL,
	FkIdTipoContratto bigint NULL,
	-- letta da EnteSQLBuilder.SelectContrattoByIdEnte ("c.codiceSDI as codiceSDI"), usata
	-- dall'handler DatiFatturazioneCreate per decidere se saltare la verifica del recipient code
	codiceSDI nvarchar(7) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
);
 
CREATE TABLE pfd.Enti (
	InternalIstitutionId nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	institutionType nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	description nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	digitalAddress nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	address nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	originId nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	-- letta da EnteSQLBuilder: "ISNULL(originIdPadre, e.originId) as CodiceIPA"
	originIdPadre nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	zipCode nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	istatCode nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	city nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	country nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	county nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	subUnitCode nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	subUnitType nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	paymentServiceProvider nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	vatnumber nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	publicservices nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LastModified datetime2 NULL,
	Category nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK__Enti__16EA308B3ABC1682 PRIMARY KEY (InternalIstitutionId)
);
-- =============================================================================================
-- pfw.Utenti: interrogata/scritta da UtenteCreateCommand, che ogni chiamata a GET api/auth/profilo
-- esegue (upsert dell'ultimo accesso). Senza la tabella la rotta risponde 500 con
-- "Invalid object name 'pfw.utenti'" — un 500 che sembra un problema di autenticazione e non lo e'.
-- Volutamente SENZA righe di seed: e' l'handler stesso a inserirle al primo accesso, ed e' proprio
-- quel comportamento che i test verificano. DDL reale fornita dal team DB.
-- =============================================================================================
IF OBJECT_ID('pfw.Utenti', 'U') IS NULL
CREATE TABLE [pfw].[Utenti](
	[FkIdEnte] [nvarchar](100) NOT NULL,
	[IdUtente] [nvarchar](510) NOT NULL,
	[DataPrimo] [datetime] NOT NULL,
	[DataUltimo] [datetime] NOT NULL,
 CONSTRAINT [PK_Utenti] PRIMARY KEY CLUSTERED ([FkIdEnte] ASC, [IdUtente] ASC)
);
GO
