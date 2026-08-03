-- Vista report Fatture Sospese (ramo NOTE: storni senza rel). DDL reale fornito dal team DB.
-- Solo righe con CodiceMateriale like '%STORNO%', fatture SENZA riga in MesiFatture, ente <> pagopa.
-- Non espone RelNonFirmata: il builder SQL aggiunge '' come placeholder per allineare la UNION.
-- CREATE OR ALTER per idempotenza all'hot-apply.
CREATE OR ALTER VIEW [be].[vwFattureSospeseNoteReport]
AS
SELECT
t.IdFattura as IdFattura,
e.description as RagioneSociale,
t.FkIdTipoDocumento as TipoDocumento,
t.FkIdEnte as IdEnte,
t.DataFattura as DataFattura,
t.Progressivo as Progressivo,
-t.TotaleFattura as TotaleFatturaImponibile,
r.CodiceMateriale as CodiceMateriale,
CASE
    WHEN (r.CodiceMateriale LIKE 'STORN%' AND r.CodiceMateriale LIKE '%NA%')
    OR (r.CodiceMateriale LIKE 'STORN%' AND r.CodiceMateriale LIKE '%ND%' )
    THEN CAST(r.Imponibile AS DECIMAL(10, 2))*-1
    ELSE CAST(r.Imponibile AS DECIMAL(10, 2))
END
AS RigaImponibile,
t.CodiceContratto as IdContratto,
t.AnnoRiferimento as Anno,
t.MeseRiferimento as Mese,
t.FkTipologiaFattura As TipologiaFattura,
0 as RelTotaleAnalogico,
0 as RelTotaleDigitale,
0 as RelTotaleNotificheAnalogiche,
0 as RelTotaleNotificheDigitali,
0 as RelTotaleNotifiche,
0 as RelTotale,
0 as RelTotaleIvatoAnalogico,
0 as RelTotaleIvatoDigitale,
0 as RelTotaleIvato,
NULL as Caricata,
NULL as RelFatturata,
c.FkIdTipoContratto,
tp.Descrizione as TipologiaContratto
,[FatturaInviata]
FROM pfd.tmpFattureTestata t
LEFT OUTER join pfd.Enti e
    ON e.InternalIstitutionId = t.FkIdEnte
LEFT JOIN pfd.Contratti c
    ON c.onboardingtokenid = t.CodiceContratto
    AND c.internalistitutionid = e.InternalIstitutionId
inner join pfw.TipoContratto tp
    ON c.FkIdTipoContratto = tp.IdTipoContratto
INNER JOIN pfd.tmpFattureRighe r
    ON t.IdFattura = r.FkIdFattura
left join pfd.MesiFatture mf
on mf.FkIdFatturaTmp = t.IdFattura

 WHERE r.CodiceMateriale like '%STORNO%'
and mf.FkIdFatturaTmp is null
AND t.FkIdEnte <> '4a4149af-172e-4950-9cc8-63ccc9a6d865'
