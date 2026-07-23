-- be.vwGestioneFattureGriglia (griglia pagina "GestioneFatture"). CREATE OR ALTER per idempotenza.
-- Stati: 0 POSTICIPATA, 1 RIPRISTINATA, 2 CANCELLATA (esclusa dalla vista), 3 ELIMINATA.
CREATE OR ALTER VIEW [be].[vwGestioneFattureGriglia] AS
SELECT
    gf.FkIdEnte           AS Ente,
    e.description         AS RagioneSociale,
    gf.FkTipologiaFattura AS TipologiaFattura,
    gf.Anno,
    gf.Mese,
    gf.Azione             AS Azione,
    gf.DataInserimento    AS DataInserimento,
    gf.DataRipristino     AS DataRipristino,
    gf.Note               AS Note,
    tc.Descrizione        AS TipoContratto,
    tc.IdTipoContratto    AS IdTipoContratto
FROM cfg.GestioneFatture gf
    INNER JOIN pfd.Enti e         ON gf.FkIdEnte = e.InternalIstitutionId
    INNER JOIN pfd.Contratti c    ON c.internalistitutionid = e.InternalIstitutionId
    INNER JOIN pfw.TipoContratto tc ON tc.IdTipoContratto = c.FkIdTipoContratto
WHERE gf.Stato <> 2; -- esclusione fatture CANCELLATA
