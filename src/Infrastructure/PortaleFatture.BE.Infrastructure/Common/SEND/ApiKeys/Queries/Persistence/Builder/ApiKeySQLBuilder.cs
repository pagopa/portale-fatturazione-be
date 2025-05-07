namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries.Persistence.Builder;

internal static class ApiKeySQLBuilder
{
    private static string _sqlIps = @"
SELECT [FkIdEnte] as IdEnte
      ,[DataCreazione] 
      ,[IPAddress]
  FROM [pfw].[ApiKeysIPs] 
";

    private static string _sql = @"
SELECT [FkIdEnte] as IdEnte
      ,[ApiKey]
      ,[DataCreazione]
      ,[DataModifica] 
      ,[Attiva]
  FROM [pfw].[ApiKeys]
";

    private static string _sqlCheck = @"
SELECT  [FkIdEnte] as IdEnte
  FROM [pfw].[EntiApiKeys] 
WHERE attiva=1
";

    public static string SelectAll()
    {
        return _sql;
    }
    public static string SelectAllIps()
    {
        return _sqlIps;
    }

    public static string OrderByIps()
    {
        return "  order by DataCreazione ASC;";
    }
    public static string SelectCheck()
    {
        return _sqlCheck;
    }
}