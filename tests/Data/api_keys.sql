-- =============================================================================================
-- ApiKeys: chiavi e whitelist IP delle Integration API (endpoint api/apikey/*), gestite in autonomia
-- dagli aderenti dal portale. È l'unica superficie di autenticazione del progetto che non aveva
-- copertura — v. la sezione "command di scrittura" in coverage/test-backlog.md.
--
-- pfw.Log (storico delle azioni) esiste già in setup.sql: il command sulle chiavi ci scrive dentro
-- e fa rollback se quell'INSERT non riesce.
--
-- ⚠️ GLI INDICI QUI SONO PARTE DEL CONTRATTO, non un dettaglio di performance:
--   UQ_APIKEY     univoco su [ApiKey]     (solo la colonna, NON la coppia con l'ente)
--   UQ_IPAddress  univoco su [IPAddress]  (idem)
-- CreateIpsCommandPersistence intercetta `SqlException 2601` (violazione di indice univoco) e la
-- traduce in -1: senza questi indici il ramo del rifiuto non sarebbe riproducibile e i test
-- sarebbero finti. La conseguenza dell'unicità globale su IPAddress è testata da
-- ApiKeysCommandIntegrationTests: due enti diversi NON possono registrare lo stesso IP.
-- =============================================================================================

IF OBJECT_ID('pfw.ApiKeys', 'U') IS NULL
BEGIN
    CREATE TABLE [pfw].[ApiKeys](
        [FkIdEnte] [nvarchar](100) NOT NULL,
        [ApiKey] [nvarchar](400) NOT NULL,
        [DataCreazione] [datetime] NOT NULL,
        [DataModifica] [datetime] NULL,
        [Attiva] [bit] NOT NULL
    );
    CREATE UNIQUE INDEX [UQ_APIKEY] ON [pfw].[ApiKeys] ([ApiKey]);
END
GO

IF OBJECT_ID('pfw.ApiKeysIPs', 'U') IS NULL
BEGIN
    CREATE TABLE [pfw].[ApiKeysIPs](
        [FkIdEnte] [nvarchar](100) NOT NULL,
        [DataCreazione] [datetime] NOT NULL,
        [IPAddress] [nvarchar](400) NOT NULL
    );
    CREATE UNIQUE INDEX [UQ_IPAddress] ON [pfw].[ApiKeysIPs] ([IPAddress]);
END
GO

-- DDL reale (confermata dal team DB il 06/08/2026: due sole colonne, nessuna PK — coincide con quella
-- che era stata derivata dal codice). È la lista degli enti ABILITATI alle Integration API, letta da
-- `_sqlCheck`: SELECT FkIdEnte FROM pfw.EntiApiKeys WHERE attiva=1.
IF OBJECT_ID('pfw.EntiApiKeys', 'U') IS NULL
CREATE TABLE [pfw].[EntiApiKeys](
    [FkIdEnte] [nvarchar](100) NOT NULL,
    [Attiva] [bit] NOT NULL
);
GO

-- Ente1 ed ente3 abilitati, ente2 NO: servono tutti e tre, perché ENTRAMBI i command (chiave e IP)
-- chiamano la stessa verifica e sollevano SecurityException("Ente non registrato!") se l'ente non è
-- qui dentro con attiva=1. Ente3 serve agli scenari fra aderenti diversi.
--
-- Nota sulla forma reale (estratto del 06/08/2026): in produzione questa tabella ha pochissime righe
-- — gli aderenti integrati sono una manciata — e sono **tutte con Attiva = 1**. Il caso "ente non
-- abilitato" si presenta quindi per ASSENZA dalla tabella, non con un flag a 0. Qui teniamo comunque
-- ente2 con Attiva = 0 perché esercita lo stesso ramo (`WHERE attiva = 1` non lo seleziona) restando
-- però esplicito su cosa il test sta verificando.
IF NOT EXISTS (SELECT 1 FROM pfw.EntiApiKeys)
INSERT INTO pfw.EntiApiKeys (FkIdEnte, Attiva) VALUES
 ('11111111-1111-1111-1111-111111111111', 1),
 ('22222222-2222-2222-2222-222222222222', 0),
 ('33333333-3333-3333-3333-333333333333', 1);
GO

-- ⚠️ CHIAVI E INDIRIZZI SONO CIFRATI A RIPOSO: gli handler passano ApiKey e IPAddress per
-- IAesEncryption.EncryptString prima dell'INSERT, quindi le colonne contengono ciphertext, non
-- '203.0.113.10'. Conseguenze per chi scrive test o interroga il DB a mano:
--   · una WHERE sul valore in chiaro non trova mai nulla (né per leggere né per ripulire);
--   · l'unicità di UQ_IPAddress vale sul ciphertext — che però è DETERMINISTICO (AesEncryption usa
--     un IV a zero), quindi lo stesso indirizzo produce sempre la stessa stringa e il vincolo si
--     comporta come se agisse sul valore in chiaro.
