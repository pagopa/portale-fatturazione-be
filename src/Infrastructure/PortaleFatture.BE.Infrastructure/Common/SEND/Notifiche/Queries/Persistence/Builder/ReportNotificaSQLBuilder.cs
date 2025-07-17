namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence.Builder;

internal static class ReportNotificaSQLBuilder
{
    private static string _sqlCount = @"
SELECT Count(n.internal_organization_id) 
  FROM [pfw].[ReportNotifiche] n
  inner join pfd.TipologiaReport r
  on r.IdTipologiaReport = FkIdTipologiaReport
  left outer join pfd.Enti e
  on e.InternalIstitutionId =  n.internal_organization_id
";
    private static string _sql = @"
SELECT [report_id] as ReportId
      ,[unique_id] as UniqueId
      ,[json]
      ,[anno]
      ,[mese]
      ,[internal_organization_id] as InternalOrganizationId
      ,[contract_id] as ContractId
      ,[utente_id] as UtenteId
      ,[prodotto]
      ,[stato]
      ,[data_inserimento] as DataInserimento
      ,[data_fine] as DataFine
      ,[storage]
      ,[nomedocumento]
      ,[link]
      ,[content_language] as ContentLanguage
      ,[content_type] as ContentType
      ,[FkIdTipologiaReport]
	  , r.CategoriaDocumento as CategoriaDocumento
	  , r.TipologiaDocumento as TipologiaDocumento
	  , e.description as RagioneSociale
      ,[hash]
      ,[letto]
      ,[data_lettura] as DataLettura
      ,Count as count
  FROM [pfw].[ReportNotifiche] n
  inner join pfd.TipologiaReport r
  on r.IdTipologiaReport = FkIdTipologiaReport
  left outer join pfd.Enti e
  on e.InternalIstitutionId =  n.internal_organization_id
";

    private static string _offSet = " OFFSET (@page-1)*@size ROWS FETCH NEXT @size ROWS ONLY";
    public static string OffSet()
    {
        return _offSet;
    }

    public static string OrderBy()
    {
        return " ORDER BY data_inserimento [ordinamento]";
    }

    public static string SelectAll()
    {
        return _sql;
    }

    public static string SelectAllCount()
    {
        return _sqlCount;
    }
}