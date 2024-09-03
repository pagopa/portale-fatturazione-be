namespace PortaleFatture.BE.Infrastructure.Common.Report.Queries.Persistence.Builder;
internal static class ReportSQLBuilder
{
    private static string _sqlSelect = @"
SELECT [IdReport]
      ,[Json]
      ,[Anno]
      ,[Mese]
      ,[Prodotto]
      ,[Stato]
      ,[DataInserimento]
      ,[DataStepCorrente]
      ,[LinkDocumento]
      ,[ContentLanguage]
      ,[ContentType]
      ,[FkIdTipologiaReport]
      ,[Hash]
	  ,TipologiaDocumento
	  ,CategoriaDocumento
      ,Descrizione
  FROM [pfd].[Report] r 
  inner join pfd.TipologiaReport t
  ON r.FkIdTipologiaReport = t.IdTipologiaReport
  WHERE t.Attivo=1 
";

    private static string _orderBy = " order by r.Mese desc, t.IdTipologiaReport";
    public static string SelectAll()
    {
        return _sqlSelect;
    }

    public static string OrderBy()
    {
        return _orderBy;
    }
}