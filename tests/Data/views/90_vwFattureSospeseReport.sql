-- Vista report Fatture Sospese (ramo VIEW/rel). DDL reale fornito dal team DB.
-- Legge pfd.tmpFattureTestata/tmpFattureRighe/tmpRelTestata/MesiFatture + Enti/Contratti/TipoContratto.
-- Espone RelNonFirmata = 'Rel in attesa di firma' quando il contratto e' PAC (tipo 2) e la fattura ha
-- una riga in MesiFatture. CREATE OR ALTER per essere idempotente all'hot-apply.
CREATE OR ALTER VIEW [be].[vwFattureSospeseReport]
AS
SELECT
t.IdFattura,
e.[description] AS RagioneSociale,
t.FkIdTipoDocumento AS TipoDocumento,
t.FkIdEnte AS IdEnte,
t.DataFattura,
t.Progressivo,
CAST(CASE WHEN t.FkIdTipoDocumento='TD04' THEN -t.TotaleFattura
  ELSE t.TotaleFattura END AS DECIMAL(18,2)) AS TotaleFatturaImponibile,
r.CodiceMateriale,
CASE
WHEN (r.CodiceMateriale LIKE 'STORN%' AND r.CodiceMateriale LIKE '%NA%')
  OR (r.CodiceMateriale LIKE 'STORN%' AND r.CodiceMateriale LIKE '%ND%')
THEN CAST(r.Imponibile AS DECIMAL(10,2))*-1
ELSE CAST(r.Imponibile AS DECIMAL(10,2))
END AS RigaImponibile,
t.CodiceContratto AS IdContratto,
t.AnnoRiferimento AS Anno,
t.MeseRiferimento AS Mese,
t.FkTipologiaFattura As TipologiaFattura,
ISNULL(rr.TotaleAnalogico,0) AS RelTotaleAnalogico,
ISNULL(rr.TotaleDigitale,0) AS RelTotaleDigitale,
ISNULL(rr.TotaleNotificheAnalogiche,0) AS RelTotaleNotificheAnalogiche,
ISNULL(rr.TotaleNotificheDigitali,0) AS RelTotaleNotificheDigitali,
ISNULL(rr.TotaleNotificheAnalogiche,0)+ISNULL(rr.TotaleNotificheDigitali,0) AS RelTotaleNotifiche,
ISNULL(rr.Totale,0) AS RelTotale,
ISNULL(rr.TotaleAnalogicoIva,0) AS RelTotaleIvatoAnalogico,
ISNULL(rr.TotaleDigitaleIva,0) AS RelTotaleIvatoDigitale,
ISNULL(rr.TotaleIva,0) AS RelTotaleIvato,
rr.Caricata,
rr.RelFatturata,
c.FkIdTipoContratto,
tp.Descrizione AS TipologiaContratto,
t.FatturaInviata,
CASE
	WHEN c.FkIdTipoContratto = 2 AND mf.FkIdFatturaTmp IS NOT NULL THEN 'Rel in attesa di firma'
	ELSE ''
	END AS RelNonFirmata
FROM pfd.tmpFattureTestata t
LEFT OUTER JOIN pfd.Enti e
ON e.InternalIstitutionId = t.FkIdEnte
LEFT OUTER JOIN pfd.tmpFattureRighe r
ON t.IdFattura = r.FkIdFattura
LEFT OUTER JOIN [pfd].[tmpRelTestata] rr
ON rr.[year]  = t.AnnoRiferimento
   AND rr.[month] = t.MeseRiferimento
   AND rr.TipologiaFattura = t.FkTipologiaFattura
   AND rr.internal_organization_id = t.FkIdEnte
   AND rr.contract_id = t.CodiceContratto
LEFT JOIN pfd.Contratti c
ON c.onboardingtokenid    = t.CodiceContratto
   AND c.internalistitutionid = e.InternalIstitutionId
INNER JOIN pfw.TipoContratto tp
ON c.FkIdTipoContratto = tp.IdTipoContratto
left join pfd.MesiFatture mf
on mf.FkIdFatturaTmp = t.IdFattura
where t.FlagFatturata = 0
AND (
		mf.FkIdFatturaTmp is null
	OR
		c.FkIdTipoContratto = 2
	)
AND t.FkIdEnte <> '4a4149af-172e-4950-9cc8-63ccc9a6d865' --esclusione pagopa
AND t.TotaleFattura > 0
