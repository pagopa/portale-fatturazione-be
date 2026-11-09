-- Vista legacy pfd.v* (senza la 'w'). DDL reale fornita il 06/11/2026, riprodotta as-is
-- (solo CREATE -> CREATE OR ALTER per l'hot-apply).
-- Gemella di pfd.vFattureAnticipo sulle tabelle di archivio *_Eliminate (fatture escluse dalla
-- fatturazione, v. FattureWhiteList). Letta in INNER JOIN da FattureModuloCommessaExcelBuilder.
CREATE OR ALTER VIEW [pfd].[vFattureAnticipoEliminate]
AS
WITH fatturerigheanalogico
     AS (SELECT fkidfattura,
                Isnull(imponibile, 0) AS ImponibileAnalogico
         FROM   pfd.fatturerighe_eliminate
         WHERE  ( codicemateriale IN ( 'ANTICIPO NA 50%', 'ANTICIPO NA 30%', 'ANT NA ST 30%','ANT NA ST 50%') ))
,
     fatturerighedigitale
     AS (SELECT fkidfattura,
                Isnull(imponibile, 0) AS ImponibileDigitale
         FROM   pfd.fatturerighe_eliminate AS FattureRighe_Eliminate_1
         WHERE  ( codicemateriale IN ( 'ANTICIPO ND 50%', 'ANTICIPO ND 30%', 'ANT ND ST 30%','ANT ND ST 50%' ) ))
SELECT DISTINCT rt.idfattura,
                Isnull(fa.imponibileanalogico, 0) AS ImponibileAnalogico,
                Isnull(fd.imponibiledigitale, 0)  AS ImponibileDigitale,
                rt.totalefattura,
                rt.fkidente                       AS IdEnte,
                rt.annoriferimento                AS Anno,
                rt.meseriferimento                AS Mese,
                rt.fktipologiafattura             AS TipologiaFattura,
                rt.progressivo                    AS progressivo
FROM   pfd.fatturetestata_eliminate AS rt
       LEFT OUTER JOIN fatturerigheanalogico AS fa
                    ON rt.idfattura = fa.fkidfattura
       LEFT OUTER JOIN fatturerighedigitale AS fd
                    ON rt.idfattura = fd.fkidfattura
WHERE  ( rt.fktipologiafattura = 'ANTICIPO' )
GO
