namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.EntiPrivati.Queries.Persistence.Builder;

 
internal static class ReportPrivatiVBSSQLBuilder
{
    private static string _sqlSelectVBS = @"
SELECT  
       p.[recipient_id] as RecipientId
      ,[CodiceArticolo]
      ,SUM([numero_tot]) as numero
      ,SUM([valore_tot]) as valore
      ,SUM([ASYNC_numero_tot]) as totalesync
      ,SUM([ASYNC_valore_tot]) as valoreasync
      ,SUM([SYNC_numero_tot]) as totalesync
      ,SUM([SYNC_valore_tot]) as valoresync
      ,p.[year_quarter] as YearQuarter
  FROM [ppa].[report_privati] p
  	left outer join [ppa].[FinancialReports] r
	ON p.recipient_id = r.recipient_id
	and p.year_quarter = r.year_quarter
    and p.CodiceArticolo = r.codice_articolo
    left outer join ppa.Contracts c
    on p.recipient_id = c.contract_id
    AND p.year_quarter = c.year_quarter
";

    public static string SelectVBS()
    {
        return _sqlSelectVBS;
    }
    public static string GroupByVBS()
    {
        return " group by p.recipient_id, CodiceArticolo, p.year_quarter";
    }
}