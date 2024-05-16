namespace PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries.Persistence.Builder;

internal static class RelTestataQuadraturaSQLBuilder
{
    private static string _sqlCount = @"
SELECT Count(nc.[internal_organization_id]) 
FROM [pfd].[NotificheCount] as nc
  left outer join pfd.Enti as e
  ON e.InternalIstitutionId = nc.internal_organization_id
  left outer join pfd.RelTestata as r
  ON r.internal_organization_id = nc.internal_organization_id
  AND r.contract_id = nc.contract_id
  AND r.year= nc.year
  AND r.month = nc.month
  AND r.TipologiaFattura = @TipologiaFattura 
  left outer join pfd.ContestazioniStorico as c
  ON c.internal_organization_id = nc.internal_organization_id
  AND c.contract_id = nc.contract_id
  AND c.year= nc.year
  AND c.month = nc.month
  AND c.TipologiaFattura=@TipologiaFattura 
";
    private static string _sql = @"
SELECT 
		 nc.[internal_organization_id] as IdEnte
		,e.description as RagioneSociale
		,nc.[contract_id] as  IdContratto
		,r.TipologiaFattura as TipologiaFattura
		,nc.[year] as  Anno
		,nc.[month] as Mese
		,ISNULL(c.[TotaleAnalogico],0) as ContestazioniTotaleAnalogico
		,ISNULL(c.[TotaleDigitale],0)  as ContestazioniTotaleDigitale
		,ISNULL(c.[TotaleNotificheAnalogiche],0) as ContestazioniTotaleNotificheAnalogiche
		,ISNULL(c.[TotaleNotificheDigitali],0)  as ContestazioniTotaleNotificheDigitali
		,ISNULL(c.[Totale],0) as ContestazioniTotale   
		,ISNULL(r.[TotaleAnalogico],0) as RelTotaleAnalogico
		,ISNULL(r.[TotaleDigitale],0)  as RelTotaleDigitale
		,ISNULL(r.[TotaleNotificheAnalogiche],0) as RelTotaleNotificheAnalogiche
		,ISNULL(r.[TotaleNotificheDigitali],0)  as RelTotaleNotificheDigitali
		,ISNULL(r.[Totale],0) as RelTotale  
	    ,ISNULL([AsseverazioneTotaleAnalogico],0) as RelAsseTotaleAnalogico
        ,ISNULL([AsseverazioneTotaleDigitale],0)   as RelAsseTotaleDigitale
        ,ISNULL([AsseverazioneTotaleNotificheAnalogiche],0) as RelAsseTotaleNotificheAnalogiche
        ,ISNULL([AsseverazioneTotaleNotificheDigitali],0)   as RelAsseTotaleNotificheDigitali
        ,ISNULL([AsseverazioneTotale],0) as RelAsseTotale   
		,nc.[TotaleAnalogico] as NotificheTotaleAnalogico
		,nc.[TotaleDigitale]  as NotificheTotaleDigitale
		,nc.[TotaleNotificheAnalogiche] as NotificheTotaleNotificheAnalogiche
		,nc.[TotaleNotificheDigitali]  as NotificheTotaleNotificheDigitali
		,nc.[Totale] as NotificheTotale   
		,nc.[TotaleAnalogico] - ISNULL(c.[TotaleAnalogico],0) - ISNULL(r.[TotaleAnalogico],0) - ISNULL(r.[AsseverazioneTotaleAnalogico],0) as  DiffTotaleAnalogico
		,nc.[TotaleDigitale]  - ISNULL(c.[TotaleDigitale],0) - ISNULL(r.[TotaleDigitale],0)- ISNULL(r.[AsseverazioneTotaleDigitale],0) as DiffTotaleDigitale
		,nc.[TotaleNotificheAnalogiche]  - ISNULL(c.[TotaleNotificheAnalogiche],0) - ISNULL(r.[TotaleNotificheAnalogiche],0) - ISNULL(r.[AsseverazioneTotaleNotificheAnalogiche],0) as DiffTotaleNotificheAnalogiche
		,nc.[TotaleNotificheDigitali]  - ISNULL(c.[TotaleNotificheDigitali],0) - ISNULL(r.[TotaleNotificheDigitali],0)  - ISNULL(r.[AsseverazioneTotaleNotificheDigitali],0) as DiffTotaleNotificheDigitali
		,nc.[Totale]  - ISNULL(c.[Totale],0) - ISNULL(r.[Totale],0)- ISNULL(r.[AsseverazioneTotale],0)  as DiffTotale 
		,nc.TotaleNotificheDigitali + nc.TotaleNotificheAnalogiche - ISNULL(c.TotaleNotificheDigitali,0)- ISNULL(c.TotaleNotificheAnalogiche,0) - ISNULL(r.TotaleNotificheDigitali,0)  - ISNULL(r.TotaleNotificheAnalogiche,0)  - ISNULL(r.AsseverazioneTotaleNotificheAnalogiche,0)  - ISNULL(r.AsseverazioneTotaleNotificheAnalogiche,0) as DiffTotaleNotificheZero
	    ,ISNULL(nc.TotaleNotificheDigitali,0) + ISNULL(nc.TotaleNotificheAnalogiche,0) as TotaleNotificheCount
		,ISNULL(c.TotaleNotificheDigitali,0) + ISNULL(c.TotaleNotificheAnalogiche,0) as TotaleNotificheContestateCount
		,ISNULL(r.TotaleNotificheDigitali,0) + ISNULL(r.TotaleNotificheAnalogiche,0) + ISNULL(r.AsseverazioneTotaleNotificheAnalogiche,0) + ISNULL(r.AsseverazioneTotaleNotificheAnalogiche,0) as TotaleNotificheRelCount
  FROM [pfd].[NotificheCount] as nc
  left outer join pfd.Enti as e
  ON e.InternalIstitutionId = nc.internal_organization_id
  left outer join pfd.RelTestata as r
  ON r.internal_organization_id = nc.internal_organization_id
  AND r.contract_id = nc.contract_id
  AND r.year= nc.year
  AND r.month = nc.month
  AND r.TipologiaFattura=@TipologiaFattura 
  left outer join pfd.ContestazioniStorico as c
  ON c.internal_organization_id = nc.internal_organization_id
  AND c.contract_id = nc.contract_id
  AND c.year= nc.year
  AND c.month = nc.month
  AND c.TipologiaFattura=@TipologiaFattura 
";
 
    private static string _offSet = " OFFSET (@page-1)*@size ROWS FETCH NEXT @size ROWS ONLY";
    public static string OffSet()
    {
        return _offSet;
    }

    public static string OrderBy()
    {
        return "  ORDER BY r.[Totale] DESC";
    }

    public static string SelectAll()
    {
        return _sql;
    }
 
    public static string SelectAllCount()
    {
        return _sqlCount;
    }
}