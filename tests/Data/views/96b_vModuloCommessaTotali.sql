-- Vista legacy pfd.v* (senza la 'w'). DDL reale fornita il 06/11/2026, riprodotta as-is
-- (solo CREATE -> CREATE OR ALTER per l'hot-apply).
-- Totali del Modulo Commessa per ente/anno/mese, con analogico (categoria spedizione 1) e digitale (2)
-- affiancati. Legge pfd.Enti.Asseverazione/DataUscitaAsseverazione e
-- pfw.DatiModuloCommessaTotali.TotaleNotificheCategoria: colonne aggiunte al seed lo stesso giorno
-- (setup.sql), dalla DDL reale.
CREATE OR ALTER VIEW [pfd].[vModuloCommessaTotali]
AS
WITH ModuloCommessaTotaleAnalogico AS (
    SELECT
        FKIdEnte,
        AnnoValidita,
        MeseValidita,
        SUM(ISNULL(TotaleCategoria, 0)) AS TotaleAnalogico,
        SUM(ISNULL(TotaleNotificheCategoria, 0)) AS TotaleNotificheAnalogico,
        FkIdTipoContratto as IdTipoContrattoA,
        PercentualeCategoria as PercentualeCategoriaA,
        FkIdStato,
        Fatturabile
    FROM pfw.DatiModuloCommessaTotali
    WHERE FkIdCategoriaSpedizione = 1
    GROUP BY FKIdEnte, AnnoValidita, MeseValidita,FkIdTipoContratto,PercentualeCategoria, FkIdStato, Fatturabile
),
ModuloCommessaTotaleDigitali AS (
    SELECT
        FKIdEnte,
        AnnoValidita,
        MeseValidita,
        SUM(ISNULL(TotaleCategoria, 0)) AS TotaleDigitale,
        SUM(ISNULL(TotaleNotificheCategoria, 0)) AS TotaleNotificheDigitale,
        FkIdTipoContratto as IdTipoContrattoD,
        PercentualeCategoria as PercentualeCategoriaD,
        FkIdStato,
        Fatturabile
    FROM pfw.DatiModuloCommessaTotali
    WHERE FkIdCategoriaSpedizione = 2
    GROUP BY FKIdEnte, AnnoValidita, MeseValidita, FkIdTipoContratto,PercentualeCategoria, FkIdStato, Fatturabile
)
SELECT
    ct.FKIdEnte,
    ct.AnnoValidita,
    ct.MeseValidita,
    ISNULL(ctA.TotaleAnalogico, 0) AS TotaleAnalogico,
    ISNULL(ctD.TotaleDigitale, 0) AS TotaleDigitale,
    ISNULL(ctA.TotaleNotificheAnalogico, 0) AS TotaleNotificheAnalogico,
    ISNULL(ctD.TotaleNotificheDigitale, 0) AS TotaleNotificheDigitale,
    ISNULL(ctA.TotaleAnalogico, 0) + ISNULL(ctD.TotaleDigitale, 0) AS Totale,
    ISNULL(ctA.TotaleNotificheAnalogico, 0) + ISNULL(ctD.TotaleNotificheDigitale, 0) AS TotaleNotifiche,
    ISNULL(ctA.IdTipoContrattoA, ctD.IdTipoContrattoD) as IdTipoContratto,
    PercentualeCategoriaA,
    PercentualeCategoriaD,
    ISNULL(ctA.fkidstato, ctD.FkIdStato) as FkIdStato,
    ctA.Fatturabile,
    e.description as RagioneSociale,
    e.vatnumber as CodiceFiscale,
    tc.Descrizione as TipoContratto,
    e.Asseverazione as Asseverazione,
    e.DataUscitaAsseverazione as DataUscitaAsseverazione
FROM (
    SELECT DISTINCT FKIdEnte, AnnoValidita, MeseValidita
    FROM pfw.DatiModuloCommessaTotali
) AS ct
LEFT JOIN ModuloCommessaTotaleAnalogico AS ctA
    ON ct.FKIdEnte = ctA.FKIdEnte
    AND ct.AnnoValidita = ctA.AnnoValidita
    AND ct.MeseValidita = ctA.MeseValidita
LEFT JOIN ModuloCommessaTotaleDigitali AS ctD
    ON ct.FKIdEnte = ctD.FKIdEnte
    AND ct.AnnoValidita = ctD.AnnoValidita
    AND ct.MeseValidita = ctD.MeseValidita
LEFT OUTER JOIN [pfd].Enti e
ON e.InternalIstitutionId = ct.FkIdEnte
LEFT OUTER JOIN [pfw].[TipoContratto] tc
ON tc.IdTipoContratto = ISNULL(ctA.IdTipoContrattoA, ctD.IdTipoContrattoD)

GO
