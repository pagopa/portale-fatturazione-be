namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence.Builder;

public static class TipoFatturaSQLBuilder
{
    private static string _sqlByIdEnte = @"
SELECT distinct 
      [FkTipologiaFattura],
	    CASE
			WHEN [FkTipologiaFattura] = 'ANTICIPO' THEN 1
			WHEN [FkTipologiaFattura] = 'ACCONTO' THEN 2
			WHEN [FkTipologiaFattura] = 'PRIMO SALDO' THEN 3
			WHEN [FkTipologiaFattura] = 'SECONDO SALDO' THEN 4
			WHEN [FkTipologiaFattura] = 'VAR. SEMESTRALE' THEN 5
			ELSE 6
		END AS ordine
      FROM [pfd].[FattureTestata]
WHERE [AnnoRiferimento]=@anno AND [MeseRiferimento]=@mese AND [FkIdEnte]=@idEnte AND [FatturaInviata] = 1
ORDER BY [FkTipologiaFattura] 
";

    private static string _sql = @"
SELECT distinct
      [FkTipologiaFattura],
	    CASE
			WHEN [FkTipologiaFattura] = 'ANTICIPO' THEN 1
			WHEN [FkTipologiaFattura] = 'ACCONTO' THEN 2
			WHEN [FkTipologiaFattura] = 'PRIMO SALDO' THEN 3
			WHEN [FkTipologiaFattura] = 'SECONDO SALDO' THEN 4
			WHEN [FkTipologiaFattura] = 'VAR. SEMESTRALE' THEN 5
			ELSE 6
		END AS ordine
      FROM [pfd].[FattureTestata]
WHERE [AnnoRiferimento]=@anno AND [MeseRiferimento]=@mese
ORDER BY ordine, [FkTipologiaFattura] 
";

    private static string _sqlCancellate = @"
SELECT distinct
      [FkTipologiaFattura] ,
	    CASE
			WHEN [FkTipologiaFattura] = 'ANTICIPO' THEN 1
			WHEN [FkTipologiaFattura] = 'ACCONTO' THEN 2
			WHEN [FkTipologiaFattura] = 'PRIMO SALDO' THEN 3
			WHEN [FkTipologiaFattura] = 'SECONDO SALDO' THEN 4
			WHEN [FkTipologiaFattura] = 'VAR. SEMESTRALE' THEN 5
			ELSE 6
		END AS ordine
      FROM [pfd].[FattureTestata_Eliminate]
WHERE [AnnoRiferimento]=@anno AND [MeseRiferimento]=@mese
ORDER BY ordine, [FkTipologiaFattura] 
";

    public static string SelectAll()
    {
        return _sql;
    }

    public static string SelectAllCancellate()
    {
        return _sqlCancellate;
    } 
    public static string SelectAllByIdEnte()
    {
        return _sqlByIdEnte;
    }
}