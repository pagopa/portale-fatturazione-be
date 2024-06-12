namespace PortaleFatture.BE.Infrastructure.Common.Fatture.Queries.Persistence.Builder;

public static class FattureRelExcelBuilder
{
    private static string _sqlUnion = @"
SELECT 
t.IdFattura as IdFattura, 
e.description as RagioneSociale,
t.FkIdTipoDocumento as TipoDocumento, 
t.FkIdEnte as IdEnte, 
t.DataFattura as DataFattura, 
t.TotaleFattura as TotaleFatturaImponibile,
r.CodiceMateriale as CodiceMateriale,
r.Imponibile as RigaImponibile, 
t.CodiceContratto as IdContratto,
t.AnnoRiferimento as Anno,
t.MeseRiferimento as Mese,
rr.[TipologiaFattura] as TipologiaFattura, 
rr.[TotaleAnalogico] as RelTotaleAnalogico,
rr.[TotaleDigitale] as RelTotaleDigitale,
rr.[TotaleNotificheAnalogiche] as RelTotaleNotificheAnalogiche,
rr.[TotaleNotificheDigitali] as RelTotaleNotificheDigitali,
ISNULL(rr.[TotaleNotificheAnalogiche],0) + ISNULL(rr.[TotaleNotificheDigitali],0) as RelTotaleNotifiche,
rr.[Totale] as RelTotale,
rr.[TotaleAnalogicoIva] as RelTotaleIvatoAnalogico,
rr.[TotaleDigitaleIva] as RelTotaleIvatoDigitale,
rr.[TotaleIva] as RelTotaleIvato,
rr.[Caricata] as Caricata,
rr.[RelFatturata] as RelFatturata
FROM pfd.FattureTestata t
LEFT OUTER join pfd.Enti e
ON e.InternalIstitutionId = t.FkIdEnte
INNER JOIN pfd.FattureRighe r
ON t.IdFattura = r.FkIdFattura
left outer JOIN  [pfd].[RelTestata] rr
ON rr.year = t.AnnoRiferimento 
AND rr.month = t.MeseRiferimento 
AND rr.TipologiaFattura = t.FkTipologiaFattura
AND rr.internal_organization_id = t.FkIdEnte 
AND rr.contract_id = t.CodiceContratto
WHERE rr.month= @mese and rr.year=@anno and rr.TipologiaFattura=@tipologiafattura
UNION
SELECT 
t.IdFattura as IdFattura, 
e.description as RagioneSociale,
t.FkIdTipoDocumento as TipoDocumento, 
t.FkIdEnte as IdEnte, 
t.DataFattura as DataFattura, 
t.TotaleFattura as TotaleFatturaImponibile,
r.CodiceMateriale as CodiceMateriale,
r.Imponibile as RigaImponibile, 
t.CodiceContratto as IdContratto,
t.AnnoRiferimento as Anno,
t.MeseRiferimento as Mese, 
@tipologiafattura as TipologiaFattura, 
NULL as RelTotaleAnalogico,
NULL as RelTotaleDigitale,
NULL as RelTotaleNotificheAnalogiche,
NULL as RelTotaleNotificheDigitali,
NULL as RelTotaleNotifiche,
NULL as RelTotale,
NULL as RelTotaleIvatoAnalogico,
NULL as RelTotaleIvatoDigitale,
NULL as RelTotaleIvato,
NULL as Caricata,
NULL as RelFatturata
FROM pfd.FattureTestata t
LEFT OUTER join pfd.Enti e
ON e.InternalIstitutionId = t.FkIdEnte
INNER JOIN pfd.FattureRighe r
ON t.IdFattura = r.FkIdFattura 
where 
t.AnnoRiferimento=@anno and 
t.MeseRiferimento=@mese and 
t.FkTipologiaFattura=@tipologiafattura and
r.CodiceMateriale like '%STORNO%'
and t.FkIdEnte NOT IN
(SELECT rr.internal_organization_id from pfd.RelTestata rr
WHERE rr.month= @mese and rr.year=@anno and rr.TipologiaFattura=@tipologiafattura)

";
    private static string _sqlNoteSenzaRel = @"
SELECT 
t.IdFattura as IdFattura, 
e.description as RagioneSociale,
t.FkIdTipoDocumento as TipoDocumento, 
t.FkIdEnte as IdEnte, 
t.DataFattura as DataFattura, 
t.TotaleFattura as TotaleFatturaImponibile,
r.CodiceMateriale as CodiceMateriale,
r.Imponibile as RigaImponibile, 
t.CodiceContratto as IdContratto,
t.AnnoRiferimento as Anno,
t.MeseRiferimento as Mese, 
@tipologiafattura as TipologiaFattura
FROM pfd.FattureTestata t
LEFT OUTER join pfd.Enti e
ON e.InternalIstitutionId = t.FkIdEnte
INNER JOIN pfd.FattureRighe r
ON t.IdFattura = r.FkIdFattura 
where 
t.AnnoRiferimento=@anno and 
t.MeseRiferimento=@mese and 
t.FkTipologiaFattura=@tipologiafattura and
r.CodiceMateriale like '%STORNO%'
and t.FkIdEnte NOT IN
(SELECT rr.internal_organization_id from pfd.RelTestata rr
WHERE rr.month= @mese and rr.year=@anno and rr.TipologiaFattura=@tipologiafattura)
";

    private static string _sqlRel = @"
SELECT 
t.IdFattura as IdFattura, 
e.description as RagioneSociale,
t.FkIdTipoDocumento as TipoDocumento, 
t.FkIdEnte as IdEnte, 
t.DataFattura as DataFattura, 
t.TotaleFattura as TotaleFatturaImponibile,
r.CodiceMateriale as CodiceMateriale,
r.Imponibile as RigaImponibile, 
t.CodiceContratto as IdContratto,
t.AnnoRiferimento as Anno,
t.MeseRiferimento as Mese,
rr.[TipologiaFattura], 
rr.[TotaleAnalogico] as RelTotaleAnalogico,
rr.[TotaleDigitale] as RelTotaleDigitale,
rr.[TotaleNotificheAnalogiche] as RelTotaleNotificheAnalogiche,
rr.[TotaleNotificheDigitali] as RelTotaleNotificheDigitali,
ISNULL(rr.[TotaleNotificheAnalogiche],0) + ISNULL(rr.[TotaleNotificheDigitali],0) as RelTotaleNotifiche,
rr.[Totale] as RelTotale,
rr.[TotaleAnalogicoIva] as RelTotaleIvatoAnalogico,
rr.[TotaleDigitaleIva] as RelTotaleIvatoDigitale,
rr.[TotaleIva] as RelTotaleIvato,
rr.[Caricata] as Caricata,
rr.[RelFatturata]
FROM pfd.FattureTestata t
LEFT OUTER join pfd.Enti e
ON e.InternalIstitutionId = t.FkIdEnte
INNER JOIN pfd.FattureRighe r
ON t.IdFattura = r.FkIdFattura
INNER JOIN  [pfd].[RelTestata] rr
ON rr.year = t.AnnoRiferimento 
AND rr.month = t.MeseRiferimento 
AND rr.TipologiaFattura = t.FkTipologiaFattura
AND rr.internal_organization_id = t.FkIdEnte 
AND rr.contract_id = t.CodiceContratto
WHERE rr.month= @mese and rr.year=@anno and rr.TipologiaFattura=@tipologiafattura
";
 

    public static string SelectRel()
    {
        return _sqlRel;
    }

    public static string SelectNoteSenzaRel()
    {
        return _sqlNoteSenzaRel;
    }

    public static string SelectUnion()
    {
        return _sqlUnion;
    }
} 