namespace PortaleFatture.BE.Infrastructure.Common.Report.Queries.Persistence.Builder;
internal static class ReportSQLBuilder
{
    private static string _sqlMatriceCostiRecapitisti = @"
SELECT  
      [geokey] AS GeoKey,
      [foreign_state] AS ForeignState,
      [product] AS Product,
      [recapitista] AS Recapitista,
      [lotto] AS Lotto,
      [costo_plico] AS CostoPlico,
      [costo_foglio] AS CostoFoglio,
      [costo_demat] AS CostoDemat,
      [min] AS Min,
      [max] AS Max,
      [costo] AS Costo,
      [costo_base_20gr] AS CostoBase20Gr,
      [id_recapitista] AS IdRecapitista,
      [DataInizioValidita] AS DataInizioValidita,
      [DataFineValidita] AS DataFineValidita
  FROM [pfd].[MatriceCostiRecapitisti]
  WHERE ([DataInizioValidita] >= @DataInizioValidita)
    AND ([DataFineValidita] <= @DataFineValidita OR [DataFineValidita] IS NULL)
";

    private static string _sqlSelectMatriceCostoRecapitistiData = @"
SELECT distinct 
       [DataInizioValidita]
	  ,[DataFineValidita]
  FROM [pfd].[MatriceCostiRecapitisti]
  Order by DataInizioValidita
";

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

    public static string SelectMatriceCostoRecapitistiData()
    {
        return _sqlSelectMatriceCostoRecapitistiData;
    }

    public static string SelectMatriceCostiRecapitisti()
    {
        return _sqlMatriceCostiRecapitisti;
    } 
}