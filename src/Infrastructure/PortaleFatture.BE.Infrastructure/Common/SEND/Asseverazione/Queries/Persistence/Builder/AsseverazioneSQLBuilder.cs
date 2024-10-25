namespace PortaleFatture.BE.Infrastructure.Common.SEND.Asseverazione.Queries.Persistence.Builder;

internal static class AsseverazioneSQLBuilder
{

    private static string _sql = @"
SELECT ISNULL(b.[InternalIstitutionId], e.InternalIstitutionId) as IdEnte
      , b.[Asseverazione] as asseverazione
      , b.[Data] as dataasseverazione
      , b.[Descrizione] as Descrizione
	  ,[Timestamp] as timestamp
      ,[IdUtente] as idutente
	  ,e.Asseverazione as calcoloasseverazione
	  ,ISNULL(e.description, b.RagioneSociale) as RagioneSociale
      ,t.Descrizione as TipoContratto
	  ,t.IdTipoContratto as IdTipoContratto
	  ,e.LastModified as dataanagrafica
	  ,c.product as prodotto
FROM (SELECT [InternalIstitutionId]
      ,[Asseverazione]
      ,[Data]
	  ,[Timestamp]
      ,[IdUtente]
	  ,[RagioneSociale] 
      ,[Descrizione]
FROM (
    SELECT 
       [InternalIstitutionId]
      ,[Asseverazione]
      ,[Data]
	  ,[Timestamp]
      ,[IdUtente] 
	  ,[RagioneSociale] 
      ,[Descrizione]
      , ROW_NUMBER() OVER (PARTITION BY [InternalIstitutionId] ORDER BY Timestamp DESC) AS RowNum
    FROM  [pfd].[Asseverazione]
) AS a
WHERE RowNum = 1) AS b
FULL JOIN pfd.Enti e 
ON b.InternalIstitutionId = e.InternalIstitutionId
LEFT OUTER JOIN pfd.Contratti c 
ON e.InternalIstitutionId = c.InternalIstitutionId
LEFT OUTER JOIN pfw.TipoContratto t 
ON t.IdTipoContratto = c.FkIdTipoContratto
WHERE (e.institutionType NOT IN ('REC', 'CON')
OR e.institutionType IS NULL) 
";


    public static string SelectAll()
    {
        return _sql;
    }
}