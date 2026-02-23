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
    public static string SelectKeyByEnte()
    {
        return $@"
SELECT a.[FkIdEnte] as IdEnte
      ,[ApiKey]
      ,[DataCreazione]
      ,[DataModifica] 
      ,a.[Attiva]
	  ,e.description as RagioneSociale 
	  ,c.onboardingtokenid as IdContratto
	  ,c.FkIdTipoContratto as IdTipoContratto
	  ,c.product as Prodotto
	  ,e.institutionType as Profilo
  FROM [pfw].[ApiKeys] a 
  inner join pfw.EntiAPiKeys ea
  on a.FkIdEnte = ea.FkIdEnte and ea.Attiva = 1
  inner join pfd.enti e
  on e.InternalIstitutionId = a.FkIdEnte
  inner join pfd.contratti c
  on e.InternalIstitutionId  = c.InternalIstitutionId 
";
    }
}