namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries.Persistence.Builder;

internal static class RelTestataSQLBuilder
{ 
    private static string _sqlAnni = @"
SELECT distinct(year)
  FROM [pfd].[RelTestata] 
";

    private static string _sqlMesi = @"
SELECT distinct(month)
  FROM [pfd].[RelTestata]
";

    private static string _sqlDistinctTipologiaFatturaPagoPA = @"
SELECT TipologiaFattura
FROM (
    SELECT DISTINCT
           TipologiaFattura,
           year,
           month, 
           CASE 
            WHEN TipologiaFattura LIKE '%PRIMO%' THEN 1
            WHEN TipologiaFattura LIKE '%SECONDO%' THEN 2
            WHEN TipologiaFattura LIKE '%SEMESTRALE%' THEN 3
               ELSE 4  -- Default case
           END as ordine
      FROM pfd.RelTestata
) AS t
";
    private static string _sqlDistinctTipologiaFattura = @"
SELECT TipologiaFattura
FROM (
    SELECT DISTINCT
           TipologiaFattura,
           year,
           month,
           internal_organization_id,
           CASE 
            WHEN TipologiaFattura LIKE '%PRIMO%' THEN 1
            WHEN TipologiaFattura LIKE '%SECONDO%' THEN 2
            WHEN TipologiaFattura LIKE '%SEMESTRALE%' THEN 3
               ELSE 4  -- Default case
           END as ordine
      FROM pfd.RelTestata
) AS t
";

    private static string _sqlCount = @"
SELECT Count(internal_organization_id) 
  FROM [pfd].[RelTestata] t
  inner join pfd.Enti e
  on e.InternalIstitutionId =t.internal_organization_id
  left outer join pfd.Contratti c
  on c.internalistitutionid = e.InternalIstitutionId
";
    private static string _sql = @"
SELECT [internal_organization_id] as IdEnte
      ,[description] as RagioneSociale
      ,[contract_id] as IdContratto
      ,[TipologiaFattura]
      ,t.[year] as anno
      ,t.[month] as mese
      ,[TotaleAnalogico]
      ,[TotaleDigitale]
      ,[TotaleNotificheAnalogiche]
      ,[TotaleNotificheDigitali]
      ,[Totale]      
      ,[Iva]
      ,[TotaleAnalogicoIva]
      ,[TotaleDigitaleIva]
      ,[TotaleIva] 
      ,[Caricata]
      ,[AsseverazioneTotaleAnalogico]
      ,[AsseverazioneTotaleDigitale]
      ,[AsseverazioneTotaleNotificheAnalogiche]
      ,[AsseverazioneTotaleNotificheDigitali]
      ,[AsseverazioneTotale]
      ,[AsseverazioneTotaleAnalogicoIva]
      ,[AsseverazioneTotaleDigitaleIva]
      ,[AsseverazioneTotaleIva]
      ,[FlagConguaglio]
	  ,tp.Descrizione as TipologiaContratto
  FROM [pfd].[RelTestata] t
  inner join pfd.Enti e
  on e.InternalIstitutionId =internal_organization_id
  left outer join pfd.Contratti c
  on c.internalistitutionid = e.InternalIstitutionId
  inner join pfw.TipoContratto tp
  on c.FkIdTipoContratto = tp.IdTipoContratto
";

    private static string _sqlDettaglio = @"
SELECT [internal_organization_id] as IdEnte
      ,[description] as RagioneSociale
      ,[contract_id] as IdContratto
      ,[TipologiaFattura]
      ,[year] as anno
      ,[month] as mese
      ,[TotaleAnalogico]
      ,[TotaleDigitale]
      ,[TotaleNotificheAnalogiche]
      ,[TotaleNotificheDigitali]
      ,[Totale]
      ,[Iva]
      ,[TotaleAnalogicoIva]
      ,[TotaleDigitaleIva]
      ,[TotaleIva] 
      ,[AsseverazioneTotaleAnalogico]
      ,[AsseverazioneTotaleDigitale]
      ,[AsseverazioneTotaleNotificheAnalogiche]
      ,[AsseverazioneTotaleNotificheDigitali]
      ,[AsseverazioneTotale]
      ,[AsseverazioneTotaleAnalogicoIva]
      ,[AsseverazioneTotaleDigitaleIva]
      ,[AsseverazioneTotaleIva]
	  ,IdDocumento
	  ,Cup
	  ,DataDocumento
      ,CASE WHEN f.IdDatiFatturazione IS NULL THEN 0 ELSE 1 END as DatiFatturazione
      ,[Caricata] 
  FROM [pfd].[RelTestata] t
  inner join pfd.Enti e
  on e.InternalIstitutionId =t.internal_organization_id
  left outer join pfw.DatiFatturazione f
  on f.FkIdEnte = t.internal_organization_id
";

    private static string _offSet = " OFFSET (@page-1)*@size ROWS FETCH NEXT @size ROWS ONLY";
    public static string OffSet()
    {
        return _offSet;
    }

    public static string OrderByPagoPA()
    {
        return " ORDER BY t.year DESC, t.month, [Totale] DESC";
    }

    public static string OrderBy()
    {
        return " ORDER BY t.year DESC, t.month, [Totale] DESC";
    }

    public static string SelectAll()
    {
        return _sql;
    }

    public static string SelectDettaglio()
    {
        return _sqlDettaglio;
    }

    public static string SelectAllCount()
    {
        return _sqlCount;
    }

    public static string SelectDistinctTipologiaFattura()
    {
        return _sqlDistinctTipologiaFattura;
    }

    public static string SelectDistinctTipologiaFatturaPagoPA()
    {
        return _sqlDistinctTipologiaFatturaPagoPA;
    }

    public static string OrderByDistinctTipologiaFattura()
    {
        return " ORDER BY ordine ";
    }

    public static string SelectAnni()
    {
        return _sqlAnni;
    }

    public static string SelectMesi()
    {
        return _sqlMesi;
    }

    public static string OrderByMonth = " order by month desc";

    public static string OrderByYear= " order by year desc";

    public static string GroupByOrderByYear = " group by year, internal_organization_id, contract_id order by year desc";

    public static string GroupByMonthByYear = " group by year, month, internal_organization_id, contract_id  order by month desc";
}