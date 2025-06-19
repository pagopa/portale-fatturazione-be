namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.EntiPrivati.Queries.Persistence.Builder;
 
internal static class ReportPrivatiECSQLBuilder
{
    private static string _sqlSelectEC = @"
SELECT 
       [internalistitutionid] as InternalInstitutionId
      ,[taxcode]
      ,[description] as RagioneSociale
      ,[CodiceArticolo] 
      ,SUM([ASYNC_numero_tot]) as totaleasync
      ,SUM([ASYNC_valore_tot]) as valoreasync
      ,SUM([SYNC_numero_tot]) as totalesync
      ,SUM([SYNC_valore_tot]) as valoresync
      ,p.[year_quarter] as YearQuarter
	  ,p.recipient_id as RecipientId
	  ,c.name as name
	  ,p.parentID
	  ,p.parentDescription
	  ,IIF(p.parentID IS NULL, 'NO', 'SI') as mandatario
  FROM [ppa].[report_privati] p 
  	left outer join [ppa].[FinancialReports] r
	ON p.recipient_id = r.recipient_id
	and p.year_quarter = r.year_quarter
    and p.CodiceArticolo = r.codice_articolo
    left outer join ppa.Contracts c
    on p.recipient_id = c.contract_id
    AND p.year_quarter = c.year_quarter
";

    public static string SelectEC()
    {
        return _sqlSelectEC;
    }
    public static string GroupByEC()
    {
        return " group by p.internalistitutionid, CodiceArticolo, p.year_quarter, taxcode, description, p.recipient_id, c.name, p.parentID, p.parentDescription";
    }

    public static string OrderByEC()
    {
        return "  ORDER BY  p.parentID desc, p.description, CodiceArticolo";
    }
}