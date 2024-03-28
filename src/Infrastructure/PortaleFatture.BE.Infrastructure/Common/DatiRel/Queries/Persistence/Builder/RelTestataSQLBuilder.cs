namespace PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries.Persistence.Builder;

internal static class RelTestataSQLBuilder
{
    private static string _sqlCount = @"
SELECT Count(internal_organization_id) 
  FROM [pfd].[RelTestata] t
  inner join pfd.Enti e
  on e.InternalIstitutionId =t.internal_organization_id
";
    private static string _sql = @"
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
      ,[Caricata]
  FROM [pfd].[RelTestata] t
  inner join pfd.Enti e
  on e.InternalIstitutionId =t.internal_organization_id
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

    public static string OrderBy()
    {
        return " ORDER BY year DESC, month, [Totale] DESC";
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
}