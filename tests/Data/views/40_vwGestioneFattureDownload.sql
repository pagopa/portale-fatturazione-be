-- be.vwGestioneFattureDownload: export delle fatture censite in cfg.GestioneFatture.
-- Come la griglia ma con le Note "appiattite" via OPENJSON sul tipo json (feature SQL Server 2025)
-- e aggregate con STRING_AGG. Esclude le CANCELLATA (Stato = 2).
CREATE OR ALTER VIEW [be].[vwGestioneFattureDownload] AS
SELECT
    gf.FkIdEnte           AS Ente,
    e.description         AS RagioneSociale,
    gf.FkTipologiaFattura AS TipologiaFattura,
    gf.Anno,
    gf.Mese,
    gf.Azione             AS Azione,
    CONVERT(VARCHAR(20), gf.DataInserimento, 120) AS DataInserimento,
    CONVERT(VARCHAR(20), gf.DataRipristino, 120)  AS DataRipristino,
    tc.Descrizione        AS TipoContratto,
    tc.IdTipoContratto    AS IdTipoContratto,
    STRING_AGG(CONCAT(CONVERT(VARCHAR(20), j.Data, 120), ' ', j.Testo), CHAR(13) + CHAR(10)) AS Note,
    gf.FkIdFattura        AS IdFattura   -- aggiunta 2026-07-28 (bigint)
FROM cfg.GestioneFatture gf
    INNER JOIN pfd.Enti e           ON gf.FkIdEnte = e.InternalIstitutionId
    INNER JOIN pfd.Contratti c      ON c.internalistitutionid = e.InternalIstitutionId
    INNER JOIN pfw.TipoContratto tc ON tc.IdTipoContratto = c.FkIdTipoContratto
    -- 2026-07-28: CROSS APPLY -> OUTER APPLY, cosi' una fattura con Note '[]'/NULL non viene esclusa
    -- dal download (prima l'INNER apply su array vuoto dava 0 righe e la faceva sparire).
    OUTER APPLY OPENJSON(gf.Note) WITH (Data datetime2 '$.Data', Testo nvarchar(max) '$.Testo') j
WHERE gf.Stato <> 2
GROUP BY gf.FkIdEnte, e.description, gf.FkTipologiaFattura, gf.Anno, gf.Mese, gf.Azione,
         gf.FkIdFattura, gf.DataInserimento, gf.DataRipristino, tc.Descrizione, tc.IdTipoContratto;
