namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

public static class FattureRelExcelBuilder
{
    private static string _sqlNoteSenzaRel = @"
SELECT 
t.IdFattura as IdFattura, 
e.description as RagioneSociale,
t.FkIdTipoDocumento as TipoDocumento, 
t.FkIdEnte as IdEnte, 
t.DataFattura as DataFattura,  
t.Progressivo as Progressivo,
-t.TotaleFattura as TotaleFatturaImponibile,
r.CodiceMateriale as CodiceMateriale,
r.Imponibile as RigaImponibile, 
t.CodiceContratto as IdContratto,
t.AnnoRiferimento as Anno,
t.MeseRiferimento as Mese, 
@tipologiafattura as TipologiaFattura,
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

    private static string _sqlRel = @"
SELECT 
	t.IdFattura as IdFattura, 
	e.description as RagioneSociale,
	t.FkIdTipoDocumento as TipoDocumento, 
	t.FkIdEnte as IdEnte, 
	t.DataFattura as DataFattura,
	t.Progressivo as Progressivo,
	CAST(
		CASE
		WHEN t.FkIdTipoDocumento  = 'TD04'  
			THEN -t.TotaleFattura
		ELSE t.TotaleFattura
				 END AS decimal(18,2)) as TotaleFatturaImponibile, 
	r.CodiceMateriale as CodiceMateriale,
	r.Imponibile as RigaImponibile, 
	t.CodiceContratto as IdContratto,
	t.AnnoRiferimento as Anno,
	t.MeseRiferimento as Mese,
	rr.[TipologiaFattura], 
	ISNULL(rr.[TotaleAnalogico],0) as RelTotaleAnalogico,
	ISNULL(rr.[TotaleDigitale],0) as RelTotaleDigitale,
	ISNULL(rr.[TotaleNotificheAnalogiche],0) as RelTotaleNotificheAnalogiche,
	ISNULL(rr.[TotaleNotificheDigitali],0) as RelTotaleNotificheDigitali,
	ISNULL(rr.[TotaleNotificheAnalogiche],0) + ISNULL(rr.[TotaleNotificheDigitali],0) as RelTotaleNotifiche,
	ISNULL(rr.[Totale],0) as RelTotale,
	ISNULL(rr.[TotaleAnalogicoIva],0)  as RelTotaleIvatoAnalogico,
	ISNULL(rr.[TotaleDigitaleIva],0)  as RelTotaleIvatoDigitale,
	ISNULL(rr.[TotaleIva],0)  as RelTotaleIvato,
	rr.[Caricata] as Caricata,
	rr.[RelFatturata]
	FROM pfd.FattureTestata t
	LEFT OUTER join pfd.Enti e
	ON e.InternalIstitutionId = t.FkIdEnte
    LEFT OUTER join pfd.FattureRighe r
	ON t.IdFattura = r.FkIdFattura
	LEFT OUTER join  [pfd].[RelTestata] rr
	ON rr.year = t.AnnoRiferimento 
	AND rr.month = t.MeseRiferimento 
	AND rr.TipologiaFattura = t.FkTipologiaFattura
	AND rr.internal_organization_id = t.FkIdEnte 
	AND rr.contract_id = t.CodiceContratto 
    LEFT JOIN pfd.RelTestata rt ON t.fkidente = rt.internal_organization_id 
								and t.annoriferimento = rt.year
								and t.meseriferimento = rt.month
                                and t.FkTipologiaFattura = rt.TipologiaFattura
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

    private static readonly string _orderByRel = @"
order by CAST(
	CASE
	WHEN t.FkIdTipoDocumento  = 'TD04'  
		THEN -t.TotaleFattura
	ELSE t.TotaleFattura
             END AS decimal(18,2)) desc";
    public static string OrderByRel()
    {
        return _orderByRel;
    }
}