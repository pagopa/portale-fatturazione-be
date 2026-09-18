-- Tabelle del flusso PEC "Regolare Esecuzione" (Azure Function SendEmail).
--
-- Perche' servono: EmailRelService LEGGE i destinatari dalla vista [pfd].[EmailRel] e SCRIVE su
-- [pfd].[RelEmail] (tracking degli invii reali) oppure su [stg].[RelEmailPreview] (anteprime,
-- quando preview=true). Senza questi tre oggetti la function fallisce con "Invalid object name".
--
-- ATTENZIONE ai nomi quasi identici, e' la trappola di quest'area:
--   [pfd].[EmailRel]        = VISTA, i destinatari a cui inviare      (views/93_EmailRel.sql)
--   [pfd].[RelEmail]        = tabella, il LOG di cio' che e' stato inviato
--   [stg].[RelEmailPreview] = tabella, le anteprime (nessun invio)
--
-- [pfd].[RelPecEmail] e' una delle quattro sorgenti della vista: sta qui perche' la vista non si
-- puo' creare se le sue tabelle non esistono (per le viste SQL Server non fa deferred name
-- resolution), quindi questo script deve girare PRIMA del blocco views/ dell'entrypoint.
--
-- DDL riprodotte dal DB reale, non derivate dalle colonne lette dal codice.

-- [pfd].[RelPecEmail] -----------------------------------------------------------------------
-- Una PEC per ente (PK su FkIdEnte) e una PEC non condivisibile fra enti (UK_Pec): i due vincoli
-- sono contratto, non indici di performance — riprodurli qui e' cio' che rende riproducibile un
-- eventuale test sul rifiuto del duplicato.
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id
               WHERE s.name = 'pfd' AND t.name = 'RelPecEmail')
BEGIN
    CREATE TABLE [pfd].[RelPecEmail](
        [FkIdEnte] [nvarchar](50) NOT NULL,
        [contract_id] [nvarchar](200) NULL,
        [pec] [nvarchar](350) NOT NULL,
     CONSTRAINT [PK_RelPecEmail] PRIMARY KEY CLUSTERED ([FkIdEnte] ASC),
     CONSTRAINT [UK_Pec] UNIQUE NONCLUSTERED ([pec] ASC)
    );
END
GO

-- [pfd].[RelEmail] --------------------------------------------------------------------------
-- Tracking: una riga per ogni tentativo di invio. Invio = 1 solo se la PEC e' partita davvero.
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id
               WHERE s.name = 'pfd' AND t.name = 'RelEmail')
BEGIN
    CREATE TABLE [pfd].[RelEmail](
        [FkIdEnte] [nvarchar](50) NOT NULL,
        [contract_id] [nvarchar](200) NOT NULL,
        [TipologiaFattura] [nvarchar](20) NOT NULL,
        [year] [int] NOT NULL,
        [month] [int] NOT NULL,
        [DataEvento] [nvarchar](250) NOT NULL,
        [Pec] [nvarchar](250) NOT NULL,
        [Messaggio] [nvarchar](2000) NOT NULL,
        [Oggetto] [nvarchar](250) NULL,
        [Corpo] [nvarchar](2000) NULL,
        [RagioneSociale] [nvarchar](1000) NOT NULL,
        [Invio] [bit] NOT NULL,
        [TipoComunicazione] [nvarchar](50) NULL,
        [Sospesa] [bit] NULL,
        [Multipla] [bit] NULL,
        [TipoContratto] [nvarchar](250) NULL,
        [Fase] [int] NULL
    );
END
GO

-- [stg].[RelEmailPreview] -------------------------------------------------------------------
-- Anteprime: ci finisce il messaggio composto quando preview=true, senza spedire nulla. E' la
-- tabella su cui si verifica un giro "a vuoto" della function.
-- Differenza dalla gemella di tracking, da conoscere prima di seedare: qui Oggetto e Corpo sono
-- NOT NULL e Messaggio e' NULL, in RelEmail e' l'opposto.
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id
               WHERE s.name = 'stg' AND t.name = 'RelEmailPreview')
BEGIN
    CREATE TABLE [stg].[RelEmailPreview](
        [FkIdEnte] [nvarchar](50) NOT NULL,
        [contract_id] [nvarchar](200) NOT NULL,
        [TipologiaFattura] [nvarchar](20) NOT NULL,
        [year] [int] NOT NULL,
        [month] [int] NOT NULL,
        [DataEvento] [nvarchar](250) NOT NULL,
        [Pec] [nvarchar](250) NOT NULL,
        [Messaggio] [nvarchar](2000) NULL,
        [Oggetto] [nvarchar](250) NOT NULL,
        [Corpo] [nvarchar](2000) NOT NULL,
        [RagioneSociale] [nvarchar](1000) NOT NULL,
        [Invio] [bit] NOT NULL,
        [TipoComunicazione] [nvarchar](50) NULL,
        [Sospesa] [bit] NULL,
        [Multipla] [bit] NULL,
        [TipoContratto] [nvarchar](250) NULL,
        [Fase] [int] NULL
    );
END
GO

-- Seed ---------------------------------------------------------------------------------------
-- Una PEC per ente1, che e' l'unico ente che soddisfa TUTTE le condizioni della query di
-- EmailRelService (_sqlSelect, ramo tipoComunicazione = 'REL'):
--   - ha gia' una pfd.RelTestata SECONDO SALDO 2026/2 con Totale = 300.00 (la query vuole Totale > 0)
--   - ha FkIdTipoContratto = 2, cioe' PAC (la query fa INNER JOIN con WHERE tp.Descrizione = 'PAC':
--     su un ente PAL non torna nulla, ed e' il motivo per cui ente3 non va bene)
-- Dominio .invalid (RFC 2606): questi file stanno in un repository pubblico.
--
-- NB: nella vista la PEC e' ISNULL(ISNULL(DatiFatturazione.PEC, RelPecEmail.pec), Enti.digitalAddress),
-- quindi una PEC gia' presente in pfw.DatiFatturazione per lo stesso ente VINCE su questa.
IF NOT EXISTS (SELECT 1 FROM pfd.RelPecEmail WHERE FkIdEnte = '11111111-1111-1111-1111-111111111111')
INSERT INTO pfd.RelPecEmail (FkIdEnte, contract_id, pec) VALUES
 ('11111111-1111-1111-1111-111111111111', 'TOKEN-E1', 'ente1@pec.invalid');
GO
