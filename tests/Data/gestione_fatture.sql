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

-- pfd.FattureTestata: solo le colonne usate dalle SP azione
IF OBJECT_ID('pfd.FattureTestata', 'U') IS NULL
CREATE TABLE pfd.FattureTestata (
    IdFattura           BIGINT        NOT NULL PRIMARY KEY, -- bigint sul DB reale (finding overflow lato C#)
    FkIdEnte            NVARCHAR(50)  NOT NULL,             -- nvarchar sul DB reale (la SP lo vuole uniqueidentifier)
    FkTipologiaFattura  NVARCHAR(50)  NOT NULL,
    AnnoRiferimento     INT           NOT NULL,
    MeseRiferimento     INT           NOT NULL,
    FatturaInviata      BIT           NULL
);

-- pfd.FattureTestata_Eliminate: controllata da CANCELLA
IF OBJECT_ID('pfd.FattureTestata_Eliminate', 'U') IS NULL
CREATE TABLE pfd.FattureTestata_Eliminate (
    IdFattura           BIGINT        NOT NULL PRIMARY KEY,
    FkIdEnte            NVARCHAR(50)  NOT NULL,
    FkTipologiaFattura  NVARCHAR(50)  NOT NULL,
    AnnoRiferimento     INT           NOT NULL,
    MeseRiferimento     INT           NOT NULL
);

-- cfg.GestioneFatture: tutte le colonne scritte/lette dalle SP
IF OBJECT_ID('cfg.GestioneFatture', 'U') IS NULL
CREATE TABLE cfg.GestioneFatture (
    Id                      INT IDENTITY(1,1) PRIMARY KEY,
    FkIdFattura             BIGINT        NULL,
    FkIdEnte                NVARCHAR(50)  NOT NULL,
    FkTipologiaFattura      NVARCHAR(50)  NOT NULL,
    Anno                    INT           NOT NULL,
    Mese                    INT           NOT NULL,
    DataInserimento         DATETIME      NULL,
    DataCancellazione       DATETIME      NULL,
    DataRipristino          DATETIME      NULL,
    DataEliminazione        DATETIME      NULL,
    IdUtenteInserimento     NVARCHAR(50)  NULL,
    IdUtenteCancellazione   NVARCHAR(50)  NULL,
    IdUtenteRipristino      NVARCHAR(50)  NULL,
    IdUtenteEliminazione    NVARCHAR(50)  NULL,
    Stato                   INT           NOT NULL, -- 0=POSTICIPATA 1=RIPRISTINATA 2=CANCELLATA 3=ELIMINATA
    Azione                  NVARCHAR(50)  NULL,
    Note                    JSON          NULL      -- tipo nativo: richiede SQL Server 2025
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
    INSERT INTO pfd.FattureTestata_Eliminate (IdFattura, FkIdEnte, FkTipologiaFattura, AnnoRiferimento, MeseRiferimento)
    SELECT IdFattura, FkIdEnte, FkTipologiaFattura, AnnoRiferimento, MeseRiferimento
    FROM pfd.FattureTestata WHERE IdFattura = @IdFattura;
    DELETE FROM pfd.FattureTestata WHERE IdFattura = @IdFattura;
    RETURN 1; -- successo (>0)
END
GO

-- Seed deterministico: fatture non inviate.
-- 1001-1002 SALDO  -> per POSTICIPA / RIPRISTINA / CANCELLA
-- 2001      ANTICIPO -> per ELIMINA (percorso distruttivo, ora sicuro su DB usa-e-getta)
IF NOT EXISTS (SELECT 1 FROM pfd.FattureTestata WHERE IdFattura IN (1001,1002,2001))
INSERT INTO pfd.FattureTestata (IdFattura, FkIdEnte, FkTipologiaFattura, AnnoRiferimento, MeseRiferimento, FatturaInviata)
VALUES
 (1001, '11111111-1111-1111-1111-111111111111', 'SECONDO SALDO', 2026, 7, 0),
 (1002, '22222222-2222-2222-2222-222222222222', 'PRIMO SALDO',   2026, 6, NULL),
 (2001, '33333333-3333-3333-3333-333333333333', 'ANTICIPO',      2026, 5, 0);
GO

-- Enti/Contratti seed per i JOIN delle viste be.vwGestioneFatture* (le tabelle esistono da setup.sql)
IF NOT EXISTS (SELECT 1 FROM pfd.Enti WHERE InternalIstitutionId = '11111111-1111-1111-1111-111111111111')
INSERT INTO pfd.Enti (InternalIstitutionId, description) VALUES
 ('11111111-1111-1111-1111-111111111111', 'Ente Test 1'),
 ('22222222-2222-2222-2222-222222222222', 'Ente Test 2'),
 ('33333333-3333-3333-3333-333333333333', 'Ente Test 3');

IF NOT EXISTS (SELECT 1 FROM pfd.Contratti WHERE internalistitutionid = '11111111-1111-1111-1111-111111111111')
INSERT INTO pfd.Contratti (internalistitutionid, FkIdTipoContratto) VALUES
 ('11111111-1111-1111-1111-111111111111', 2),  -- PAC
 ('22222222-2222-2222-2222-222222222222', 2),  -- PAC
 ('33333333-3333-3333-3333-333333333333', 1);  -- PAL
GO

-- Righe PERSISTENTI in cfg.GestioneFatture per i test di LETTURA (griglia/download/modifica).
-- Id 900x: dedicate alle letture, distinte dalle 100x/200x usate (e ripulite) dai test azione.
-- Stato 2 (CANCELLATA) e' escluso dalle viste: ne mettiamo una per verificarlo.
IF NOT EXISTS (SELECT 1 FROM cfg.GestioneFatture WHERE FkIdFattura IN (9001,9002,9003,9004))
INSERT INTO cfg.GestioneFatture (FkIdFattura, FkIdEnte, FkTipologiaFattura, Anno, Mese, DataInserimento, DataRipristino, IdUtenteInserimento, Stato, Azione, Note)
VALUES
 (9001, '11111111-1111-1111-1111-111111111111', 'SECONDO SALDO', 2026, 7, GETDATE(), NULL,     'seed', 0, 'POSTICIPATA',  N'{"Data":"2026-07-01T00:00:00","Testo":"seed-posticipata"}'),
 (9002, '22222222-2222-2222-2222-222222222222', 'PRIMO SALDO',   2026, 6, GETDATE(), GETDATE(), 'seed', 1, 'RIPRISTINATA', N'{"Data":"2026-06-01T00:00:00","Testo":"seed-ripristinata"}'),
 (9003, '33333333-3333-3333-3333-333333333333', 'ANTICIPO',      2026, 5, GETDATE(), NULL,     'seed', 3, 'ELIMINATA',    N'{"Data":"2026-05-01T00:00:00","Testo":"seed-eliminata"}'),
 (9004, '11111111-1111-1111-1111-111111111111', 'SECONDO SALDO', 2026, 4, GETDATE(), NULL,     'seed', 2, 'CANCELLATA',   N'{"Data":"2026-04-01T00:00:00","Testo":"seed-cancellata-esclusa"}');
GO
