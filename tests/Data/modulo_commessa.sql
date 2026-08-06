-- =============================================================================================
-- Modulo Commessa: tabelle mancanti + seed per le rotte api/v2/modulocommessa/* (BE-SMOKE-04).
--
-- A differenza di Notifiche (solo tabelle) e REL (una vista sola), quest'area e' un ibrido: le
-- query dei *SQLBuilder combinano viste LEGACY pfd.v* (senza la 'w' — v. docs/architettura.md) con
-- tabelle e CTE costruite in C#. Le 4 viste stanno in views/96_..99_.
--
-- Gia' presenti da setup.sql (create ma VUOTE: qui sotto ne seediamo le righe):
--   pfw.DatiModuloCommessa, pfw.DatiModuloCommessaTotali, pfw.DatiModuloCommessaRegioni,
--   pfw.TipoSpedizione (3 righe), pfw.TipoContratto (2 righe), pfd.Enti, pfd.Contratti.
--
-- Create qui perche' assenti dal seed:
--   cfg.ConfigurazioneDatiModuloCommessa, cfg.FrameModuloCommessa,
--   pfw.DatiModuloCommessaAderenti, pfw.Regioni, pfw.Province.
--
-- DDL allineate a INFORMATION_SCHEMA del DB reale (estratto 2026-08-06).
-- =============================================================================================

IF OBJECT_ID('cfg.ConfigurazioneDatiModuloCommessa', 'U') IS NULL
CREATE TABLE [cfg].[ConfigurazioneDatiModuloCommessa](
	[anno] [int] NOT NULL,
	[mese] [int] NOT NULL,
	[FkIdEnte] [nvarchar](100) NOT NULL,
	[DataContratto] [datetime2](7) NULL,
	[obbligatorio] [nvarchar](50) NOT NULL,
	[DaInviare] [nvarchar](50) NOT NULL,
	[facoltativo] [nvarchar](300) NOT NULL,
	[archiviato] [nvarchar](50) NULL
);
GO

IF OBJECT_ID('cfg.FrameModuloCommessa', 'U') IS NULL
CREATE TABLE [cfg].[FrameModuloCommessa](
	[anno] [int] NOT NULL,
	[mese] [int] NOT NULL,
	[frame] [nvarchar](20) NOT NULL,
	[framelegale] [nvarchar](20) NOT NULL
);
GO

IF OBJECT_ID('pfw.DatiModuloCommessaAderenti', 'U') IS NULL
CREATE TABLE [pfw].[DatiModuloCommessaAderenti](
	[DataExport] [datetime] NOT NULL,
	[Internalistitutionid] [nvarchar](50) NOT NULL,
	[Segmento] [nvarchar](50) NULL,
	[MacrocategoriaVendita] [nvarchar](100) NULL,
	[SottocategoriaVendita] [nvarchar](100) NULL,
	[Provincia] [nvarchar](10) NOT NULL,
	[Regione] [nvarchar](10) NOT NULL
);
GO

IF OBJECT_ID('pfw.Regioni', 'U') IS NULL
CREATE TABLE [pfw].[Regioni](
	[CodiceIstat] [nvarchar](50) NOT NULL,
	[Regione] [nvarchar](100) NOT NULL
);
GO

IF OBJECT_ID('pfw.Province', 'U') IS NULL
CREATE TABLE [pfw].[Province](
	[Provincia] [nvarchar](100) NOT NULL,
	[CodiceIstat] [nvarchar](10) NOT NULL,
	[CodiceIstatRegione] [nvarchar](10) NOT NULL
);
GO

-- ---------------------------------------------------------------------------------------------
-- Lookup territoriali. Bastano due regioni/province: le viste le usano per join e percentuali,
-- non per la copertura completa del territorio.
-- Attenzione al legame con pfd.Enti.istatCode: vDatiModuloCommessaAderenti joina le province su
-- SUBSTRING(e.istatCode, 1, 3), quindi il codice ente deve iniziare con il codice ISTAT provincia.
-- ---------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM pfw.Regioni)
INSERT INTO pfw.Regioni (CodiceIstat, Regione) VALUES
 ('12', N'Lazio'),
 ('03', N'Lombardia');
GO

IF NOT EXISTS (SELECT 1 FROM pfw.Province)
INSERT INTO pfw.Province (Provincia, CodiceIstat, CodiceIstatRegione) VALUES
 (N'Roma',   '058', '12'),
 (N'Milano', '015', '03');
GO

-- ---------------------------------------------------------------------------------------------
-- Finestra di compilazione: 'frame' = finestra TECNICA (1-19), 'framelegale' = finestra LEGALE
-- (1-15). La vista vConfigurazioneDatiModuloCommessa le spacchetta sul '-' per calcolare
-- datavalidita/datavaliditalegale (v. docs/business-fatturazione.md).
-- ---------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM cfg.FrameModuloCommessa)
INSERT INTO cfg.FrameModuloCommessa (anno, mese, frame, framelegale) VALUES
 (2026, 4, N'1-19', N'1-15'),
 (2026, 5, N'1-19', N'1-15');
GO

-- ---------------------------------------------------------------------------------------------
-- Configurazione per ente1, periodi 2026/4 e 2026/5. Le tre colonne obbligatorio/facoltativo/
-- archiviato sono LISTE 'anno/mese' separate da ';' che la vista esplode con STRING_SPLIT:
-- una riga di configurazione genera quindi PIU' righe in vista, una per periodo elencato.
-- Nota: 'Source' viene ricalcolato a 'archiviato' se il periodo e' quello corrente e il giorno
-- di oggi cade fuori dalla finestra 'frame' — quindi l'esito di un test che guarda Source dipende
-- dalla data di esecuzione: meglio asserire su periodi passati/futuri, non sul mese corrente.
-- ---------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM cfg.ConfigurazioneDatiModuloCommessa WHERE FkIdEnte = '11111111-1111-1111-1111-111111111111')
INSERT INTO cfg.ConfigurazioneDatiModuloCommessa (anno, mese, FkIdEnte, DataContratto, obbligatorio, DaInviare, facoltativo, archiviato) VALUES
 (2026, 4, '11111111-1111-1111-1111-111111111111', '2025-01-01', N'2026/8', N'2026/8', N'2026/9;2026/10', N'2026/3'),
 (2026, 5, '11111111-1111-1111-1111-111111111111', '2025-01-01', N'2026/9', N'2026/9', N'2026/10',        N'2026/4');
GO

-- Anagrafica di segmentazione: la vista prende SOLO le righe con la MAX(DataExport), poi fa UNION
-- con gli enti non presenti (ricavandone la provincia da pfd.Enti.istatCode). Ente1 e' qui dentro,
-- ente3 no: cosi' il seed esercita entrambi i rami della UNION.
IF NOT EXISTS (SELECT 1 FROM pfw.DatiModuloCommessaAderenti)
INSERT INTO pfw.DatiModuloCommessaAderenti
 (DataExport, Internalistitutionid, Segmento, MacrocategoriaVendita, SottocategoriaVendita, Provincia, Regione) VALUES
 ('2026-04-01', '11111111-1111-1111-1111-111111111111', N'PAC', N'Centrali', N'Ministeri', '058', '12');
GO

-- ---------------------------------------------------------------------------------------------
-- Dati previsionali di ente1 per 2026/5: una riga per TipoSpedizione (1=AR, 2=890, 3=digitale).
-- vModuliCommessa parte dalle DIGITALI (CTE 'digitali' in INNER JOIN): senza la riga con
-- FkIdTipoSpedizione=3 l'ente sparisce del tutto dalla vista, anche avendo AR e 890.
-- ---------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM pfw.DatiModuloCommessa WHERE FkIdEnte = '11111111-1111-1111-1111-111111111111' AND AnnoValidita = 2026 AND MeseValidita = 5)
INSERT INTO pfw.DatiModuloCommessa
 (FkIdEnte, FkProdotto, FkIdTipoSpedizione, NumeroNotificheNazionali, NumeroNotificheInternazionali,
  AnnoValidita, MeseValidita, FKIdTipoContratto, FkIdStato, DataCreazione)
VALUES
 ('11111111-1111-1111-1111-111111111111', 'prod-pn', 1, 600, 10, 2026, 5, 2, 'Aperta/Caricato', GETDATE()),
 ('11111111-1111-1111-1111-111111111111', 'prod-pn', 2, 400,  0, 2026, 5, 2, 'Aperta/Caricato', GETDATE()),
 ('11111111-1111-1111-1111-111111111111', 'prod-pn', 3, 300, 20, 2026, 5, 2, 'Aperta/Caricato', GETDATE());
GO

-- Totali economici per categoria (1=analogico, 2=digitale), letti dalla CTE TotaliEconomici.
IF NOT EXISTS (SELECT 1 FROM pfw.DatiModuloCommessaTotali WHERE FkIdEnte = '11111111-1111-1111-1111-111111111111' AND AnnoValidita = 2026 AND MeseValidita = 5)
INSERT INTO pfw.DatiModuloCommessaTotali
 (FkIdEnte, FkIdCategoriaSpedizione, TotaleCategoria, AnnoValidita, MeseValidita, Totale, FkIdTipoContratto, FkProdotto, FkIdStato)
VALUES
 ('11111111-1111-1111-1111-111111111111', 1, 1000, 2026, 5, 5000.00, 2, 'prod-pn', 'Aperta/Caricato'),
 ('11111111-1111-1111-1111-111111111111', 2,  320, 2026, 5,  320.00, 2, 'prod-pn', 'Aperta/Caricato');
GO

-- ⚠️ Delta di schema: la pfw.DatiModuloCommessaRegioni di setup.sql (scritta a mano) NON ha la
-- colonna 'Calcolato', che vModuloCommessaPrevisionale_V2 usa in MAX(CAST(Calcolato AS int)) e nella
-- PARTITION BY. Senza, la vista non si crea ("Invalid column name 'Calcolato'"). La aggiungiamo qui
-- invece che in setup.sql per tenere il delta visibile.
-- Tipo NON verificato sul DB reale: 'bit' e' compatibile con l'uso della vista (CAST a int), ma se il
-- DB la tiene int/altro, allineare qui e in setup.sql.
IF COL_LENGTH('pfw.DatiModuloCommessaRegioni', 'Calcolato') IS NULL
ALTER TABLE pfw.DatiModuloCommessaRegioni ADD [Calcolato] [bit] NULL;
GO

-- Distribuzione regionale: AR + 890 devono sommare al totale nazionale delle spedizioni 1 e 2
-- (600 + 400 = 1000) perche' vModuloCommessaPrevisionale_V2 dia TotaleCoperturaRegionale='VALIDO'.
-- Con numeri diversi si ottengono 'ECCESSIVO'/'INSUFFICIENTE': e' proprio cio' che la vista serve
-- a intercettare, quindi il seed copre il caso valido e ne lascia traccia per gli altri.
IF NOT EXISTS (SELECT 1 FROM pfw.DatiModuloCommessaRegioni WHERE Internalistitutionid = '11111111-1111-1111-1111-111111111111' AND anno = 2026 AND mese = 5)
INSERT INTO pfw.DatiModuloCommessaRegioni (Internalistitutionid, anno, mese, Regione, AR, [890], Calcolato)
VALUES
 ('11111111-1111-1111-1111-111111111111', 2026, 5, '12', 400, 250, 0),
 ('11111111-1111-1111-1111-111111111111', 2026, 5, '03', 200, 150, 0);
GO
