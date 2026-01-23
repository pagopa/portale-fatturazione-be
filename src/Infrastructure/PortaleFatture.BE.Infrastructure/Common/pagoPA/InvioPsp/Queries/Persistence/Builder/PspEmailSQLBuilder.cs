namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.InvioPsp.Queries.Persistence.Builder;
 
internal static class PspEmailSQLBuilder
{
    private static string _sqlSelect = @"
SELECT [IdContratto] as Psp
      ,[Tipologia] as Tipologia
      ,[Anno]
      ,[Trimestre]
      ,[DataEvento]
      ,[Email]
      ,[Messaggio]
      ,[RagioneSociale]
      ,[Invio]
  FROM [ppa].[PspEmail]
";

    public static string SelectAll()
    {
        return _sqlSelect;
    }
    public static string OrderByQuarters()
    {
        return " order by Trimestre desc";
    }
}