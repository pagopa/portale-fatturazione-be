namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries.Persistence.Builder;

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
    private static string _sqlNoTipologiaFattura = @"
SELECT	 
		 nc.[internal_organization_id] as IdEnte 
		,e.description as RagioneSociale
		,nc.[contract_id] as  IdContratto 
		,nc.[year] as  Anno
		,nc.[month] as Mese
        ,nc.[TotaleAnalogico] as NotificheTotaleAnalogico
		,nc.[TotaleDigitale]  as NotificheTotaleDigitale
		,nc.[TotaleNotificheAnalogiche] as NotificheTotaleNotificheAnalogiche
		,nc.[TotaleNotificheDigitali]  as NotificheTotaleNotificheDigitali
		,nc.[Totale] as NotificheTotale   
	    ,nc.[TotaleNotificheAnalogiche] + nc.[TotaleNotificheDigitali] as NotificheTotaleNotifiche   
		,r.TotaleAnalogico as RelTotaleAnalogico
		,r.TotaleDigitale  as RelTotaleDigitale
		,r.TotaleNotificheAnalogiche as RelTotaleNotificheAnalogiche
		,r.TotaleNotificheDigitali  as RelTotaleNotificheDigitali
        ,ISNULL(r.[TotaleNotificheAnalogiche],0) + ISNULL(r.[TotaleNotificheDigitali],0) as RelTotaleNotifiche
		,r.Totale  as RelTotale  
		,r.AsseverazioneTotaleAnalogico as RelAsseTotaleAnalogico
		,r.AsseverazioneTotaleDigitale  as RelAsseTotaleDigitale
		,r.AsseverazioneTotaleNotificheAnalogiche as RelAsseTotaleNotificheAnalogiche
		,r.AsseverazioneTotaleNotificheDigitali   as RelAsseTotaleNotificheDigitali
		,r.AsseverazioneTotale as RelAsseTotale   
        ,ISNULL(r.AsseverazioneTotaleNotificheAnalogiche,0) + ISNULL(r.AsseverazioneTotaleNotificheDigitali,0) as RelAsseTotaleNotifiche
		,ISNULL(c.[TotaleAnalogico],0) as ContestazioniTotaleAnalogico
		,ISNULL(c.[TotaleDigitale],0)  as ContestazioniTotaleDigitale
		,ISNULL(c.[TotaleNotificheAnalogiche],0) as ContestazioniTotaleNotificheAnalogiche
		,ISNULL(c.[TotaleNotificheDigitali],0)  as ContestazioniTotaleNotificheDigitali
		,ISNULL(c.[TotaleNotificheAnalogiche],0) + ISNULL(c.[TotaleNotificheDigitali],0) as ContestazioniNotificheTotale   
		,ISNULL(c.[Totale],0) as ContestazioniTotale   
		,nc.[TotaleAnalogico] - ISNULL(c.[TotaleAnalogico],0) - ISNULL(r.[TotaleAnalogico],0) - ISNULL(r.[AsseverazioneTotaleAnalogico],0) as  DiffTotaleAnalogico
		,nc.[TotaleDigitale]  - ISNULL(c.[TotaleDigitale],0) - ISNULL(r.[TotaleDigitale],0)- ISNULL(r.[AsseverazioneTotaleDigitale],0) as DiffTotaleDigitale
		,nc.[TotaleNotificheAnalogiche]  - ISNULL(c.[TotaleNotificheAnalogiche],0) - ISNULL(r.[TotaleNotificheAnalogiche],0) - ISNULL(r.[AsseverazioneTotaleNotificheAnalogiche],0) as DiffTotaleNotificheAnalogiche
		,nc.[TotaleNotificheDigitali]  - ISNULL(c.[TotaleNotificheDigitali],0) - ISNULL(r.[TotaleNotificheDigitali],0)  - ISNULL(r.[AsseverazioneTotaleNotificheDigitali],0) as DiffTotaleNotificheDigitali
		,nc.[Totale]  - ISNULL(c.[Totale],0) - ISNULL(r.[Totale],0)- ISNULL(r.[AsseverazioneTotale],0)  as DiffTotale 
		,nc.TotaleNotificheDigitali + nc.TotaleNotificheAnalogiche - ISNULL(c.TotaleNotificheDigitali,0)- ISNULL(c.TotaleNotificheAnalogiche,0) - ISNULL(r.TotaleNotificheDigitali,0)  - ISNULL(r.TotaleNotificheAnalogiche,0)  - ISNULL(r.AsseverazioneTotaleNotificheAnalogiche,0)  - ISNULL(r.AsseverazioneTotaleNotificheAnalogiche,0) as DiffTotaleNotificheZero
 
FROM [pfd].[NotificheCount] as nc
left outer join pfd.Enti as e
ON e.InternalIstitutionId = nc.internal_organization_id
left outer join 
	(SELECT		
			 [internal_organization_id]  
			,[contract_id]  
			,[year] 
			,[month]  
			,SUM(ISNULL([TotaleAnalogico],0)) as [TotaleAnalogico]
			,SUM(ISNULL([TotaleDigitale],0))  as [TotaleDigitale]
			,SUM(ISNULL([TotaleNotificheAnalogiche],0)) as [TotaleNotificheAnalogiche]
			,SUM(ISNULL([TotaleNotificheDigitali],0))  as [TotaleNotificheDigitali]
			,SUM(ISNULL([Totale],0)) as [Totale]  
			,SUM(ISNULL([AsseverazioneTotaleAnalogico],0)) as [AsseverazioneTotaleAnalogico]
			,SUM(ISNULL([AsseverazioneTotaleDigitale],0))   as [AsseverazioneTotaleDigitale]
			,SUM(ISNULL([AsseverazioneTotaleNotificheAnalogiche],0)) as [AsseverazioneTotaleNotificheAnalogiche]
			,SUM(ISNULL([AsseverazioneTotaleNotificheDigitali],0))   as [AsseverazioneTotaleNotificheDigitali]
			,SUM(ISNULL([AsseverazioneTotale],0)) as [AsseverazioneTotale]
	from pfd.RelTestata  
	group by [internal_organization_id],[contract_id],[year], [month]) r
  ON r.internal_organization_id = nc.internal_organization_id  AND r.contract_id = nc.contract_id and r.year = nc.year and r.month = nc.month
left outer join 
	(SELECT  cr.internal_organization_id
	        ,cr.contract_id 
			,cr.year
			,cr.month
			,ISNULL(cr.[TotaleAnalogico],0) as [TotaleAnalogico]
			,ISNULL(cr.[TotaleDigitale],0)  as [TotaleDigitale]
			,ISNULL(cr.[TotaleNotificheAnalogiche],0) as [TotaleNotificheAnalogiche]
			,ISNULL(cr.[TotaleNotificheDigitali],0)  as [TotaleNotificheDigitali]
			,ISNULL(cr.[Totale],0) as [Totale]   
	FROM ( SELECT
		   [internal_organization_id]
		  ,[contract_id]
		  ,[year]
		  ,[month]
		  ,[TotaleAnalogico]
		  ,[TotaleDigitale]
		  ,[TotaleNotificheAnalogiche]
		  ,[TotaleNotificheDigitali]
		  ,[Totale]
		  ,[Iva]
		  ,[TotaleAnalogicoIva]
		  ,[TotaleDigitaleIva]
		  ,[TotaleIva]
		  ,[TipologiaFattura]
		  , ROW_NUMBER() OVER (PARTITION BY internal_organization_id, contract_id,year,month  ORDER BY  
				(CASE WHEN TipologiaFattura = 'PRIMO SALDO' THEN 'A' 
	    			  WHEN TipologiaFattura = 'SECONDO SALDO' THEN 'B' 
					  END) DESC) AS RowNum     
			FROM  [pfd].ContestazioniStorico
		  ) as cr
		  WHERE RowNum = 1) c
 ON c.internal_organization_id = r.internal_organization_id and c.contract_id = r.contract_id and c.year = r.year and c.month = r.month
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
		,ISNULL(c.[TotaleNotificheAnalogiche],0) + ISNULL(c.[TotaleNotificheDigitali],0) as ContestazioniNotificheTotale   
		,ISNULL(r.[TotaleAnalogico],0) as RelTotaleAnalogico
		,ISNULL(r.[TotaleDigitale],0)  as RelTotaleDigitale
		,ISNULL(r.[TotaleNotificheAnalogiche],0) as RelTotaleNotificheAnalogiche
		,ISNULL(r.[TotaleNotificheDigitali],0)  as RelTotaleNotificheDigitali
		,ISNULL(r.[Totale],0) as RelTotale  
        ,ISNULL(r.[TotaleNotificheAnalogiche],0) + ISNULL(r.[TotaleNotificheDigitali],0) as RelTotaleNotifiche
	    ,ISNULL([AsseverazioneTotaleAnalogico],0) as RelAsseTotaleAnalogico
        ,ISNULL([AsseverazioneTotaleDigitale],0)   as RelAsseTotaleDigitale
        ,ISNULL([AsseverazioneTotaleNotificheAnalogiche],0) as RelAsseTotaleNotificheAnalogiche
        ,ISNULL([AsseverazioneTotaleNotificheDigitali],0)   as RelAsseTotaleNotificheDigitali
        ,ISNULL([AsseverazioneTotaleNotificheAnalogiche],0) + ISNULL([AsseverazioneTotaleNotificheDigitali],0) as RelAsseTotaleNotifiche
        ,ISNULL([AsseverazioneTotale],0) as RelAsseTotale   
		,nc.[TotaleAnalogico] as NotificheTotaleAnalogico
		,nc.[TotaleDigitale]  as NotificheTotaleDigitale
		,nc.[TotaleNotificheAnalogiche] as NotificheTotaleNotificheAnalogiche
		,nc.[TotaleNotificheDigitali]  as NotificheTotaleNotificheDigitali
		,nc.[Totale] as NotificheTotale   
		,nc.[TotaleNotificheAnalogiche] + nc.[TotaleNotificheDigitali] as NotificheTotaleNotifiche   
		,nc.[TotaleAnalogico] - ISNULL(c.[TotaleAnalogico],0) - ISNULL(r.[TotaleAnalogico],0) - ISNULL(r.[AsseverazioneTotaleAnalogico],0) as  DiffTotaleAnalogico
		,nc.[TotaleDigitale]  - ISNULL(c.[TotaleDigitale],0) - ISNULL(r.[TotaleDigitale],0)- ISNULL(r.[AsseverazioneTotaleDigitale],0) as DiffTotaleDigitale
		,nc.[TotaleNotificheAnalogiche]  - ISNULL(c.[TotaleNotificheAnalogiche],0) - ISNULL(r.[TotaleNotificheAnalogiche],0) - ISNULL(r.[AsseverazioneTotaleNotificheAnalogiche],0) as DiffTotaleNotificheAnalogiche
		,nc.[TotaleNotificheDigitali]  - ISNULL(c.[TotaleNotificheDigitali],0) - ISNULL(r.[TotaleNotificheDigitali],0)  - ISNULL(r.[AsseverazioneTotaleNotificheDigitali],0) as DiffTotaleNotificheDigitali
		,nc.[Totale]  - ISNULL(c.[Totale],0) - ISNULL(r.[Totale],0)- ISNULL(r.[AsseverazioneTotale],0)  as DiffTotale 
		,nc.TotaleNotificheDigitali + nc.TotaleNotificheAnalogiche - ISNULL(c.TotaleNotificheDigitali,0)- ISNULL(c.TotaleNotificheAnalogiche,0) - ISNULL(r.TotaleNotificheDigitali,0)  - ISNULL(r.TotaleNotificheAnalogiche,0)  - ISNULL(r.AsseverazioneTotaleNotificheAnalogiche,0)  - ISNULL(r.AsseverazioneTotaleNotificheAnalogiche,0) as DiffTotaleNotificheZero
 
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
    public static string SelectAllNoTipologia()
    {
        return _sqlNoTipologiaFattura;
    }
    public static string SelectAllCount()
    {
        return _sqlCount;
    }
}