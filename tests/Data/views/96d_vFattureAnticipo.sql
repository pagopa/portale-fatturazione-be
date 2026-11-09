-- Vista legacy pfd.v* (senza la 'w'). DDL reale fornita il 06/11/2026, riprodotta as-is
-- (solo CREATE -> CREATE OR ALTER per l'hot-apply).
-- Fatture ANTICIPO con l'imponibile analogico e digitale letto dalle righe per CodiceMateriale.
-- Letta dall'export Excel del modulo commessa nei report delle fatture
-- (FattureModuloCommessaExcelBuilder, ramo ANTICIPO di FattureReportEndpointE2ETests).
CREATE OR ALTER VIEW [pfd].[vFattureAnticipo]
AS

WITH fatturerigheanalogico
     AS (SELECT fkidfattura,
                Isnull(imponibile, 0) AS ImponibileAnalogico
         FROM   pfd.fatturerighe
         WHERE  ( codicemateriale IN ( 'ANTICIPO NA 50%', 'ANTICIPO NA 30%', 'ANT NA ST 30%','ANT NA ST 50%') ))
,
     fatturerighedigitale
     AS (SELECT fkidfattura,
                Isnull(imponibile, 0) AS ImponibileDigitale
         FROM   pfd.fatturerighe
         WHERE  ( codicemateriale IN ( 'ANTICIPO ND 50%', 'ANTICIPO ND 30%', 'ANT ND ST 30%','ANT ND ST 50%' ) ))
SELECT DISTINCT rt.idfattura,
       Isnull(fa.imponibileanalogico, 0) AS ImponibileAnalogico,
       Isnull(fd.imponibiledigitale, 0)  AS ImponibileDigitale,
       rt.totalefattura,
       rt.fkidente                       AS IdEnte,
       rt.annoriferimento                AS Anno,
       rt.meseriferimento                AS Mese,
       rt.fktipologiafattura             AS TipologiaFattura,
       rt.progressivo                    AS progressivo,
       rt.FatturaInviata                 AS FatturaInviata
FROM   pfd.fatturetestata AS rt
       LEFT OUTER JOIN fatturerigheanalogico AS fa
                    ON rt.idfattura = fa.fkidfattura
       LEFT OUTER JOIN fatturerighedigitale AS fd
                    ON rt.idfattura = fd.fkidfattura
WHERE  ( rt.fktipologiafattura = 'ANTICIPO' )


GO
