-- Vista legacy pfd.v* (senza la 'w'). DDL reale fornita il 06/11/2026, riprodotta as-is
-- (solo CREATE -> CREATE OR ALTER per l'hot-apply).
-- Fatture ACCONTO con l'imponibile di acconto e di storno, analogico e digitale, letto dalle righe
-- per CodiceMateriale; la percentuale di anticipo viene da pfw.FatturaTestataConfig.
-- Letta dai report delle fatture (FattureReportEndpointE2ETests, rami ANTICIPO/ACCONTO).
-- ⚠️ Nota sul codice di DATA, riprodotto com'e': DATEFROMPARTS(YEAR(rt.AnnoRiferimento),
-- MONTH(rt.MeseRiferimento), 31) applica YEAR/MONTH a degli interi, cioe' a date del 1900.
CREATE OR ALTER VIEW [pfd].[vFattureAcconto]
AS
WITH fatturerigheaccontoanalogico AS (

    SELECT FkIdFattura, ISNULL(Imponibile, 0) AS ImponibileAccontoAnalogico
                                    FROM      pfd.FattureRighe
                              WHERE   (CodiceMateriale IN ('ACCONTO NOT.ANL.'))), fatturerigheaccontodigitale AS
    (SELECT FkIdFattura, ISNULL(Imponibile, 0) AS ImponibileAccontoDigitale
     FROM      pfd.FattureRighe AS FattureRighe_3
     WHERE   (CodiceMateriale IN ('ACCONTO NOT.DIG.'))), fatturerighestornoanalogico AS
    (SELECT FkIdFattura, ISNULL(Imponibile, 0) AS ImponibileStornoAnalogico
     FROM      pfd.FattureRighe AS FattureRighe_2
     WHERE   (CodiceMateriale IN ('STORNO 50% ANT.NA', 'ACC STORN 50% ANT.NA'))), fatturerighestornodigitale AS
    (SELECT FkIdFattura, ISNULL(Imponibile, 0) AS ImponibileStornoDigitale
     FROM      pfd.FattureRighe AS FattureRighe_1
     WHERE   (CodiceMateriale IN ('STORNO 50% ANT.ND', 'ACC STORN 50% ANT.ND')))
    SELECT DISTINCT
                      rt.IdFattura, ISNULL(faa.ImponibileAccontoAnalogico, 0) AS ImponibileAccontoAnalogico, ISNULL(fad.ImponibileAccontoDigitale, 0) AS ImponibileAccontoDigitale, ISNULL(fsa.ImponibileStornoAnalogico, 0) AS ImponibileStornoAnalogico,
                      ISNULL(fsd.ImponibileStornoDigitale, 0) AS ImponibileStornoDigitale, rt.TotaleFattura, rt.FkIdEnte AS IdEnte, rt.AnnoRiferimento AS Anno, rt.MeseRiferimento AS Mese, rt.FkTipologiaFattura AS TipologiaFattura, rt.progressivo AS progressivo,
                      e.description AS RagioneSociale, e.vatnumber AS CodiceFiscale, rt.FkIdTipoDocumento AS IdTipoDocumento, c.FkIdTipoContratto AS IdTipoContratto, c.product AS Prodotto, tc.Descrizione AS TipoContratto,
                          (SELECT TOP (1) PercentualeAnticipo
                           FROM      pfw.FatturaTestataConfig
                           WHERE   (FkTipologiaFattura = 'ACCONTO') AND (DataModifica > DATEFROMPARTS(YEAR(rt.AnnoRiferimento), MONTH(rt.MeseRiferimento), 31)) AND (FKIdTipoContratto = c.FkIdTipoContratto)
                           ORDER BY DataModifica DESC) AS percentuale,
                      rt.FatturaInviata                 AS FatturaInviata
    FROM     pfd.FattureTestata AS rt LEFT OUTER JOIN
                      fatturerigheaccontoanalogico AS faa ON rt.IdFattura = faa.FkIdFattura LEFT OUTER JOIN
                      fatturerigheaccontodigitale AS fad ON rt.IdFattura = fad.FkIdFattura LEFT OUTER JOIN
                      fatturerighestornoanalogico AS fsa ON rt.IdFattura = fsa.FkIdFattura LEFT OUTER JOIN
                      fatturerighestornodigitale AS fsd ON rt.IdFattura = fsd.FkIdFattura LEFT OUTER JOIN
                      pfd.Enti AS e ON e.InternalIstitutionId = rt.FkIdEnte LEFT OUTER JOIN
                      pfd.Contratti AS c ON c.internalistitutionid = rt.FkIdEnte INNER JOIN
                      pfw.TipoContratto AS tc ON tc.IdTipoContratto = c.FkIdTipoContratto
    WHERE  (rt.FkTipologiaFattura = 'ACCONTO')

GO
