/*
  Nota seed: testo della vista fornito da DATA il 05/11/2026 (non ancora nel branch DBProj
  dbproj-pf-735-json-to-SAP). Unica modifica: CREATE OR ALTER al posto di CREATE, come le altre
  viste del seed (idempotente in caso di hot-apply).
*/
CREATE OR ALTER VIEW [be].[vwLockAttiviAzioniPF]
AS
SELECT
     la.FkIdAzione     AS IdAzione
    ,ta.Descrizione    AS Azione
    ,la.lockStart      AS LockStartUTC
    ,la.lockTimeout    AS LockTimeoutUTC
    ,la.FkBatchId      AS BatchId
FROM pfw.LockAzionePF la
    LEFT JOIN pfw.TipoAzione ta ON ta.IdAzione = la.FkIdAzione
WHERE lockTimeout >= GETUTCDATE()
GO
