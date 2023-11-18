-- DROP SCHEMA pfw;

CREATE SCHEMA pfw;
-- pfw.CategoriaSpedizione definition

-- Drop table

-- DROP TABLE pfw.CategoriaSpedizione;

CREATE TABLE pfw.CategoriaSpedizione (
	IdCategoriaSpedizione int IDENTITY(1,1) NOT NULL,
	Descrizione nvarchar(250) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Tipo nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK__Categori__0E5B57A455A55002 PRIMARY KEY (IdCategoriaSpedizione)
);


-- pfw.Form definition

-- Drop table

-- DROP TABLE pfw.Form;

CREATE TABLE pfw.Form (
	IdForm int NOT NULL,
	Descrizione varchar(250) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK__Form__007D03D97A238DBC PRIMARY KEY (IdForm)
);


-- pfw.Log definition

-- Drop table

-- DROP TABLE pfw.Log;

CREATE TABLE pfw.Log (
	FkIdEnte nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	IdUtente nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DataEvento datetime NOT NULL,
	DescrizioneEvento nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	JsonTransazione text COLLATE SQL_Latin1_General_CP1_CI_AS NULL
);


-- pfw.Prodotti definition

-- Drop table

-- DROP TABLE pfw.Prodotti;

CREATE TABLE pfw.Prodotti (
	Prodotto nvarchar(15) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CONSTRAINT PK__Prodotti__3EE5F2F6209BD2F1 PRIMARY KEY (Prodotto)
);


-- pfw.Stato definition

-- Drop table

-- DROP TABLE pfw.Stato;

CREATE TABLE pfw.Stato (
	Stato nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Default] bit NULL,
	CONSTRAINT PK__Stato__BA803DA6AD8CBD05 PRIMARY KEY (Stato)
);


-- pfw.Step definition

-- Drop table

-- DROP TABLE pfw.Step;

CREATE TABLE pfw.Step (
	IdStep int NOT NULL,
	Descrizione varchar(250) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK__Step__A3FC8BAD5173134C PRIMARY KEY (IdStep)
);


-- pfw.TipoCommessa definition

-- Drop table

-- DROP TABLE pfw.TipoCommessa;

CREATE TABLE pfw.TipoCommessa (
	TipoCommessa nvarchar(1) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Descrizione nvarchar(250) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK__TipoComm__E5339F96F1E97E24 PRIMARY KEY (TipoCommessa)
);


-- pfw.TipoContratto definition

-- Drop table

-- DROP TABLE pfw.TipoContratto;

CREATE TABLE pfw.TipoContratto (
	IdTipoContratto bigint IDENTITY(1,1) NOT NULL,
	Descrizione nvarchar(3) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CONSTRAINT PK__TipoCont__D8826341BD065CDF PRIMARY KEY (IdTipoContratto)
);


-- pfw.DatiFatturazione definition

-- Drop table

-- DROP TABLE pfw.DatiFatturazione;

CREATE TABLE pfw.DatiFatturazione (
	IdDatiFatturazione bigint IDENTITY(1,1) NOT NULL,
	Cup nvarchar(15) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Cig nvarchar(10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CodCommessa nvarchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	DataDocumento datetime NOT NULL,
	SplitPayment bit NOT NULL,
	FkIdEnte nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	IdDocumento nvarchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	DataCreazione datetime NOT NULL,
	DataModifica datetime NULL,
	[Map] nvarchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	FkTipoCommessa nvarchar(1) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	PEC nvarchar(250) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	FkProdotto nvarchar(15) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CONSTRAINT PK__DatiFatt__5A190E0F178A5F57 PRIMARY KEY (IdDatiFatturazione),
	CONSTRAINT FkProdotto_DatiFatturazione FOREIGN KEY (FkProdotto) REFERENCES pfw.Prodotti(Prodotto),
	CONSTRAINT FkTipoCommessaDatiFatturazione FOREIGN KEY (FkTipoCommessa) REFERENCES pfw.TipoCommessa(TipoCommessa)
);


-- pfw.DatiFatturazioneContatti definition

-- Drop table

-- DROP TABLE pfw.DatiFatturazioneContatti;

CREATE TABLE pfw.DatiFatturazioneContatti (
	FkIdDatiFatturazione bigint NOT NULL,
	Email nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CONSTRAINT FkIdDatiFatturazioneContatti FOREIGN KEY (FkIdDatiFatturazione) REFERENCES pfw.DatiFatturazione(IdDatiFatturazione)
);


-- pfw.DatiModuloCommessaTotali definition

-- Drop table

-- DROP TABLE pfw.DatiModuloCommessaTotali;

CREATE TABLE pfw.DatiModuloCommessaTotali (
	FkIdEnte nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	FkIdTipoContratto bigint NOT NULL,
	FkProdotto nvarchar(15) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	AnnoValidita int NOT NULL,
	MeseValidita int NOT NULL,
	FkIdCategoriaSpedizione int NOT NULL,
	FkStato nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	TotaleCategoria decimal(9,2) NOT NULL,
	CONSTRAINT PK_DatiModuloCommessaTotali PRIMARY KEY (FkIdEnte,FkIdTipoContratto,FkProdotto,AnnoValidita,MeseValidita,FkIdCategoriaSpedizione),
	CONSTRAINT FK_DatiModuloCommessaTotali_CategoriaSpedizione FOREIGN KEY (FkIdCategoriaSpedizione) REFERENCES pfw.CategoriaSpedizione(IdCategoriaSpedizione),
	CONSTRAINT FK_DatiModuloCommessaTotali_Prodotti FOREIGN KEY (FkProdotto) REFERENCES pfw.Prodotti(Prodotto),
	CONSTRAINT FK_DatiModuloCommessaTotali_Stato FOREIGN KEY (FkStato) REFERENCES pfw.Stato(Stato),
	CONSTRAINT FK_DatiModuloCommessaTotali_TipoContratto FOREIGN KEY (FkIdTipoContratto) REFERENCES pfw.TipoContratto(IdTipoContratto)
);


-- pfw.PercentualeAnticipo definition

-- Drop table

-- DROP TABLE pfw.PercentualeAnticipo;

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


-- pfw.Scadenziario definition

-- Drop table

-- DROP TABLE pfw.Scadenziario;

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


-- pfw.TipoSpedizione definition

-- Drop table

-- DROP TABLE pfw.TipoSpedizione;

CREATE TABLE pfw.TipoSpedizione (
	IdTipoSpedizione int IDENTITY(1,1) NOT NULL,
	Descrizione nvarchar(250) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	FkIdCategoriaSpedizione int NOT NULL,
	Tipo nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK__TipoSped__33BE995DD9717E6A PRIMARY KEY (IdTipoSpedizione),
	CONSTRAINT FkIdCategoriaSpedizione FOREIGN KEY (FkIdCategoriaSpedizione) REFERENCES pfw.CategoriaSpedizione(IdCategoriaSpedizione)
);


-- pfw.CostoNotifiche definition

-- Drop table

-- DROP TABLE pfw.CostoNotifiche;

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


-- pfw.DatiModuloCommessa definition

-- Drop table

-- DROP TABLE pfw.DatiModuloCommessa;

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

INSERT INTO pfw.CategoriaSpedizione
(Descrizione)
VALUES('Analogico');

INSERT INTO pfw.CategoriaSpedizione
(Descrizione)
VALUES('Digitale');

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
