namespace PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.Queries.Persistence.Builder;

internal class OrchestratoreSQLBuilder
{
    private static string _sqlCount = @"
SELECT 
    count(*)
FROM  pfd.vOrchestratore
";


    private static string _sql = @"
SELECT 
    Anno,
    Mese,
    Tipologia,
	Fase,
	CAST(DataEsecuzione AS DATE) as DataEsecuzione,
	DataFineContestazioni,
    ChiusuraContestazioni,
    TempoRisposta,
	CAST(DataFatturazione AS DATE) as DataFatturazione,
	Esecuzione,
	Count
FROM  pfd.vOrchestratore
";


    private static string _offSet = " OFFSET (@page-1)*@size ROWS FETCH NEXT @size ROWS ONLY";
    public static string OffSet()
    {
        return _offSet;
    }

    public static string OrderBy()
    {
        return " ORDER BY ISNULL(DataEsecuzione, DataFineContestazioni) [ordinamento]";
    }

    public static string SelectAll()
    {
        return _sql;
    }

    public static string SelectAllCount()
    {
        return _sqlCount;
    }

    public static string SelectTipologie()
    {
        return $@"
SELECT DISTINCT 
       [Tipologia] 
  FROM [pfd].[vOrchestratore]
";
    }

    public static string SelectFasi()
    {
        return $@"
SELECT DISTINCT 
       [Fase] 
  FROM [pfd].[vOrchestratore]
";
    }
}