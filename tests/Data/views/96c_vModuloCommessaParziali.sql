-- Vista legacy pfd.v* (senza la 'w'). DDL reale fornita il 06/11/2026, riprodotta as-is
-- (solo CREATE -> CREATE OR ALTER per l'hot-apply).
-- Notifiche e valori del Modulo Commessa per ente/anno/mese, affiancati per tipo di spedizione
-- (1 = A/R, 2 = 890, 3 = digitale). Letta dall'export Excel del modulo commessa dentro i report delle
-- fatture (FattureModuloCommessaExcelPersistence, ramo ANTICIPO di FattureReportEndpointE2ETests).
CREATE OR ALTER VIEW [pfd].[vModuloCommessaParziali]
AS
WITH ModuloCommessaTotaleAnalogicoAR AS (
    SELECT
        FKIdEnte,
        AnnoValidita,
        MeseValidita,
        SUM(ISNULL(NumeroNotificheNazionali, 0)) AS NumeroNotificheNazionaliAR,
        SUM(ISNULL(NumeroNotificheInternazionali, 0)) AS NumeroNotificheInternazionaliAR,
        ValoreNazionali as ValoreNazionaliAR,
        ValoreInternazionali as ValoreInternazionaliAR
    FROM pfw.DatiModuloCommessa
    WHERE FkIdTipoSpedizione = 1
    GROUP BY FKIdEnte, AnnoValidita, MeseValidita, FkIdTipoSpedizione, ValoreNazionali, ValoreInternazionali
),
ModuloCommessaTotaleAnalogico890 AS (
    SELECT
        FKIdEnte,
        AnnoValidita,
        MeseValidita,
        SUM(ISNULL(NumeroNotificheNazionali, 0)) AS NumeroNotificheNazionali890,
        SUM(ISNULL(NumeroNotificheInternazionali, 0)) AS NumeroNotificheInternazionali890,
        ValoreNazionali as ValoreNazionali890,
        ValoreInternazionali as ValoreInternazionali890
    FROM pfw.DatiModuloCommessa
    WHERE FkIdTipoSpedizione = 2
    GROUP BY FKIdEnte, AnnoValidita, MeseValidita, FkIdTipoSpedizione, ValoreNazionali, ValoreInternazionali
),
ModuloCommessaTotaleDigitali AS (
    SELECT
        FKIdEnte,
        AnnoValidita,
        MeseValidita,
        SUM(ISNULL(NumeroNotificheNazionali, 0)) AS NumeroNotificheNazionaliDigitali,
        SUM(ISNULL(NumeroNotificheInternazionali, 0)) AS NumeroNotificheInternazionaliDigitali,
        ValoreNazionali as ValoreNazionaliDigitali,
        ValoreInternazionali as ValoreInternazionaliDigitali
    FROM pfw.DatiModuloCommessa
    WHERE FkIdTipoSpedizione = 3
    GROUP BY FKIdEnte, AnnoValidita, MeseValidita, FkIdTipoSpedizione, ValoreNazionali, ValoreInternazionali
)
SELECT
    ct.FKIdEnte,
    ct.AnnoValidita,
    ct.MeseValidita,
    ISNULL(NumeroNotificheNazionaliAR, 0) AS NumeroNotificheNazionaliAR,
    ISNULL(NumeroNotificheInternazionaliAR, 0) AS NumeroNotificheInternazionaliAR,
    ISNULL(NumeroNotificheNazionali890, 0) AS NumeroNotificheNazionali890,
    ISNULL(NumeroNotificheInternazionali890, 0) AS NumeroNotificheInternazionali890,
    ISNULL(NumeroNotificheNazionaliDigitali, 0)  AS NumeroNotificheNazionaliDigitali,
    ISNULL(NumeroNotificheInternazionaliDigitali, 0) AS NumeroNotificheInternazionaliDigitali,
    ISNULL(ValoreNazionaliAR, 0) AS ValoreNazionaliAR,
    ISNULL(ValoreInternazionaliAR, 0) AS ValoreInternazionaliAR,
    ISNULL(ValoreNazionali890, 0) AS ValoreNazionali890,
    ISNULL(ValoreInternazionali890, 0) AS ValoreInternazionali890,
    ISNULL(ValoreNazionaliDigitali, 0)  AS ValoreNazionaliDigitali,
    ISNULL(ValoreInternazionaliDigitali, 0) AS ValoreInternazionaliDigitali
FROM (
    SELECT DISTINCT FKIdEnte, AnnoValidita, MeseValidita
    FROM pfw.DatiModuloCommessa
) AS ct
LEFT JOIN ModuloCommessaTotaleAnalogicoAR AS ctAR
    ON ct.FKIdEnte = ctAR.FKIdEnte
    AND ct.AnnoValidita = ctAR.AnnoValidita
    AND ct.MeseValidita = ctAR.MeseValidita
LEFT JOIN ModuloCommessaTotaleAnalogico890 AS ctA890
    ON ct.FKIdEnte = ctA890.FKIdEnte
    AND ct.AnnoValidita = ctA890.AnnoValidita
    AND ct.MeseValidita = ctA890.MeseValidita
LEFT JOIN ModuloCommessaTotaleDigitali AS ctD
    ON ct.FKIdEnte = ctD.FKIdEnte
    AND ct.AnnoValidita = ctD.AnnoValidita
    AND ct.MeseValidita = ctD.MeseValidita

GO
