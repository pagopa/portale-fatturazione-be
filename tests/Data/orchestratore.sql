-- =============================================================================================
-- Area Orchestratore: le due tabelle di calendario che mancavano al seed.
--
-- Sono le uniche dipendenze di [pfd].[vOrchestratore] che il DB di test non aveva; tutte le altre
-- (pfw.ContestazioniCalendario, pfd.RelTestata, pfd.NotificheCount, pfd.FattureTestata) sono gia'
-- create altrove. Senza queste due il CREATE VIEW fallisce: per le viste SQL Server NON applica la
-- deferred name resolution, quindi la vista va creata dopo di loro (v. ordine nell'entrypoint).
--
-- DDL reale del DB, fornita dal team DB il 31/08/2026, riprodotta as-is (aggiunte solo le guardie
-- IF NOT EXISTS per l'idempotenza).
--
-- ⚠️ NESSUN codice C# legge queste due tabelle: nel backend entrano solo attraverso la vista, e
--    nessuna entita'/DTO ci e' mappata sopra. Sono tabelle del team Data, che le scrive e le
--    rilegge dalle pipeline Synapse (cfg.CalendarioFatturazione guida l'emissione delle fatture,
--    cfg.CalendarioVarSemestrale i conguagli semestrali - v. docs/pipeline-dati-send.md).
--    Conseguenza pratica: una colonna sbagliata qui si manifesta come colonna vuota nella griglia
--    Orchestratore, non come errore di mapping.
--
-- ⚠️ cfg.CalendarioFatturazione e' una TABELLA TEMPORALE (SYSTEM_VERSIONING ON): ValidFrom/ValidTo
--    sono GENERATED ALWAYS e HIDDEN, quindi non vanno mai elencate in un INSERT e non compaiono in
--    SELECT *. Per lo stesso motivo non si puo' fare DROP TABLE senza prima disattivare il
--    versioning. E' riprodotta cosi' perche' e' cosi' in produzione, non perche' i test ne abbiano
--    bisogno.
-- =============================================================================================

IF SCHEMA_ID('cfg') IS NULL EXEC ('CREATE SCHEMA cfg;');
GO

-- ---------------------------------------------------------------------------------------------
-- cfg.CalendarioVarSemestrale - colonne lette dalla vista: Tipologia, MeseRel, AnnoRel, DataEsecuzione
-- ---------------------------------------------------------------------------------------------
IF OBJECT_ID('cfg.CalendarioVarSemestrale', 'U') IS NULL
CREATE TABLE [cfg].[CalendarioVarSemestrale](
    [Tipologia] [nvarchar](20) NOT NULL,
    [MeseRel] [int] NOT NULL,
    [AnnoRel] [int] NOT NULL,
    [DataEsecuzione] [datetime] NOT NULL,
 CONSTRAINT [PK_CalVarSemestrale] PRIMARY KEY CLUSTERED
(
    [Tipologia] ASC,
    [MeseRel] ASC,
    [AnnoRel] ASC
)
);
GO

-- ---------------------------------------------------------------------------------------------
-- cfg.CalendarioFatturazione (+ tabella di storico) - colonne lette dalla vista:
-- AnnoRiferimento, MeseRiferimento, TipologiaFattura, Fase, DataEsecuzione, DataFatturazione,
-- CicloEffettuato. Semestre non e' proiettata.
-- ---------------------------------------------------------------------------------------------
IF OBJECT_ID('cfg.CalendarioFatturazione_history', 'U') IS NULL
CREATE TABLE [cfg].[CalendarioFatturazione_history](
    [AnnoRiferimento] [int] NOT NULL,
    [MeseRiferimento] [int] NOT NULL,
    [TipologiaFattura] [nvarchar](15) NOT NULL,
    [Fase] [int] NOT NULL,
    [DataEsecuzione] [datetime] NOT NULL,
    [DataFatturazione] [datetime] NULL,
    [CicloEffettuato] [bit] NOT NULL,
    [Semestre] [nvarchar](15) NULL,
    [ValidFrom] [datetime2](7) NOT NULL,
    [ValidTo] [datetime2](7) NOT NULL
) WITH (DATA_COMPRESSION = PAGE);
GO

IF OBJECT_ID('cfg.CalendarioFatturazione', 'U') IS NULL
CREATE TABLE [cfg].[CalendarioFatturazione](
    [AnnoRiferimento] [int] NOT NULL,
    [MeseRiferimento] [int] NOT NULL,
    [TipologiaFattura] [nvarchar](15) NOT NULL,
    [Fase] [int] NOT NULL CONSTRAINT [DF_CalendarioFatturazione_Fase_v3] DEFAULT ((1)),
    [DataEsecuzione] [datetime] NOT NULL,
    [DataFatturazione] [datetime] NULL,
    [CicloEffettuato] [bit] NOT NULL CONSTRAINT [DF_CalendarioFatturazione_CicloEffettuato_v3] DEFAULT ((0)),
    [Semestre] [nvarchar](15) NULL,
    [ValidFrom] [datetime2](7) GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
    [ValidTo] [datetime2](7) GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
 CONSTRAINT [PK_CalendarioFatturazione_v2] PRIMARY KEY CLUSTERED
(
    [AnnoRiferimento] ASC,
    [MeseRiferimento] ASC,
    [TipologiaFattura] ASC,
    [Fase] ASC,
    [DataEsecuzione] ASC
),
    PERIOD FOR SYSTEM_TIME ([ValidFrom], [ValidTo])
) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [cfg].[CalendarioFatturazione_history]));
GO

-- =============================================================================================
-- SEED
--
-- Regola che governa tutte le date qui sotto: la vista confronta con GETDATE() in cinque rami su
-- otto, quindi una data "plausibile" cambierebbe stato col passare dei mesi e i test diventerebbero
-- verdi (o rossi) da soli. Si usano quindi solo estremi: 2020 = passato per sempre,
-- 2099 = futuro per sempre. Stessa scelta gia' fatta per pfw.ContestazioniCalendario.
--
-- ⚠️ Le asserzioni sui test vanno scritte cercando la riga per Anno/Mese/Tipologia/Fase, MAI sul
--    numero totale di righe: i due rami "IMPORT DATI" della vista non leggono righe, le GENERANO
--    con un CROSS JOIN dei 12 mesi, e per l'anno corrente si fermano a MONTH(GETDATE())+1. Il
--    conteggio complessivo della vista cambia quindi a ogni cambio di mese, senza che nessuno
--    tocchi il seed.
-- =============================================================================================

-- ---------------------------------------------------------------------------------------------
-- Ramo "Variazione Semestrale REL": copre i tre esiti possibili di Esecuzione.
-- Il join con pfd.RelTestata e' su 'var. semestrale' HARDCODED nella vista (non sulla colonna
-- Tipologia della riga): la riga 2026/5 fa match con la REL VAR. SEMESTRALE gia' seedata in
-- gestione_fatture.sql, ed e' l'unico modo per ottenere un "Eseguito" senza aggiungere REL nuove.
-- ---------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM cfg.CalendarioVarSemestrale WHERE AnnoRel = 2026)
INSERT INTO cfg.CalendarioVarSemestrale (Tipologia, MeseRel, AnnoRel, DataEsecuzione)
VALUES
 -- REL presente + data passata -> Esecuzione 1 (Eseguito), Count 1
 (N'VAR. SEMESTRALE',  5, 2026, '2020-05-20'),
 -- nessuna REL + data passata  -> Esecuzione 2 (Eseguito no data), Count NULL
 (N'VAR. SEMESTRALE', 11, 2026, '2020-11-20'),
 -- data futura                 -> Esecuzione 0 (Programmato)
 (N'VAR. SEMESTRALE', 12, 2026, '2099-12-31');
GO

-- ---------------------------------------------------------------------------------------------
-- Ramo "Fatturazione": copre i tre esiti di Esecuzione e ENTRAMBI i rami del CASE sulla Fase.
--
-- La Fase mostrata NON e' il numero: la vista traduce Fase=1 in 'FATT.' solo per PRIMO SALDO /
-- ANTICIPO / ACCONTO / VAR. SEMESTRALE, e tutto il resto (altre tipologie, oppure Fase <> 1) in
-- 'FATT. REL FIRM.'. Le righe 2026/2 (SECONDO SALDO, Fase 1) e 2026/6 Fase 2 coprono i due modi
-- diversi di finire nel ramo else.
--
-- ⚠️ Il join che valorizza Count e' su (FkTipologiaFattura, DataFattura) con UGUAGLIANZA ESATTA
--    sulla data: DataFattura e' datetime2 su pfd.FattureTestata, DataFatturazione e' datetime qui.
--    Le date del seed sono tutte a mezzanotte, quindi il match regge; una qualunque componente
--    oraria lo farebbe fallire in silenzio (Count NULL). Le DataFatturazione qui sotto sono
--    volutamente uguali alle DataFattura delle fatture gia' seedate:
--      '2026-06-01' -> fatture 1002 e 3001 (PRIMO SALDO)   => Count 2, non 1: il conteggio e' per
--                      (tipologia, data), NON per ente
--      '2026-02-01' -> fattura 8001 (SECONDO SALDO)        => Count 1
-- ---------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM cfg.CalendarioFatturazione WHERE AnnoRiferimento = 2026)
INSERT INTO cfg.CalendarioFatturazione
 (AnnoRiferimento, MeseRiferimento, TipologiaFattura, Fase, DataEsecuzione, DataFatturazione, CicloEffettuato, Semestre)
VALUES
 -- ciclo effettuato -> Esecuzione 1 (Eseguito); Fase 1 + PRIMO SALDO -> 'FATT.'; Count 2
 (2026,  6, N'PRIMO SALDO',   1, '2020-06-20', '2026-06-01', 1, NULL),
 -- ciclo effettuato -> Esecuzione 1; SECONDO SALDO non e' nella whitelist -> 'FATT. REL FIRM.'; Count 1
 (2026,  2, N'SECONDO SALDO', 1, '2020-02-20', '2026-02-01', 1, NULL),
 -- ciclo NON effettuato e data gia' passata -> Esecuzione 3 (Errore); Count NULL
 (2026,  3, N'PRIMO SALDO',   1, '2020-03-20', NULL,         0, NULL),
 -- stessa tipologia della prima ma Fase 2 -> 'FATT. REL FIRM.' (il ramo else scatta anche sulla fase)
 (2026,  6, N'PRIMO SALDO',   2, '2020-07-20', NULL,         1, NULL),
 -- data futura -> Esecuzione 0 (Programmato); Fase 1 + ANTICIPO -> 'FATT.'
 (2026, 12, N'ANTICIPO',      1, '2099-12-31', NULL,         0, NULL);
GO
