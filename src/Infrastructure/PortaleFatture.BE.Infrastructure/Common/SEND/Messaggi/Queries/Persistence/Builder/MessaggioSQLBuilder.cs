namespace PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Queries.Persistence.Builder;

internal static class MessaggioSQLBuilder
{

    private static string _sqlCount = "SELECT COUNT(m.IdUtente) FROM [pfd].[Messaggi] m";
    private static string _sql = @"SELECT m.[IdMessaggio]
      ,m.[IdEnte]
      ,m.[IdUtente]
      ,m.[Json]
      ,m.[Anno]
      ,m.[Mese]
      ,m.[Prodotto]
      ,m.[GruppoRuolo]
      ,m.[Auth]
      ,m.[Stato]
      ,m.[DataInserimento] as DataInserimento
      ,m.[DataStepCorrente]
      ,m.[LinkDocumento]
      ,m.[ContentLanguage]
      ,m.[ContentType]
      ,m.[TipologiaDocumento]
      ,m.[CategoriaDocumento]
      ,m.[Lettura]
      ,m.[Hash]
      ,m.[FKIdReport] as IdReport
	  ,r.Hash as rhash
	  ,e.description as RagioneSociale
FROM[pfd].[Messaggi] m
left outer join pfd.Enti e
on e.InternalIstitutionId = m.IdEnte
left outer join
pfd.Report r
ON m.[FKIdReport] = r.IdReport";

    public static string SelectCount()
    {
        return _sqlCount;
    }
    public static string Select()
    {
        return _sql;
    }

    private static string _offSet = " OFFSET (@page-1)*@size ROWS FETCH NEXT @size ROWS ONLY";
    public static string OffSet()
    {
        return _offSet;
    }

    public static string OrderBy()
    {
        return " ORDER BY m.DataInserimento desc";
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