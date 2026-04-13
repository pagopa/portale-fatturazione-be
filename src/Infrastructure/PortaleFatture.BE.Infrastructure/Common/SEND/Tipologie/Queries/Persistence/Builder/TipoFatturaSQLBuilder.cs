namespace PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence.Builder;

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

    private static string _sqlSospese =
        @"
        SELECT distinct
        t.[FkTipologiaFattura] ,
	    CASE
			WHEN t.[FkTipologiaFattura] = 'ANTICIPO' THEN 1
			WHEN t.[FkTipologiaFattura] = 'ACCONTO' THEN 2
			WHEN t.[FkTipologiaFattura] = 'PRIMO SALDO' THEN 3
			WHEN t.[FkTipologiaFattura] = 'SECONDO SALDO' THEN 4
			WHEN t.[FkTipologiaFattura] = 'VAR. SEMESTRALE' THEN 5
			ELSE 6
		END AS ordine
        FROM [pfd].[tmpFattureTestata] t
        LEFT JOIN [pfd].[FattureTestata] FT_EMESSA 
        ON t.FkIdEnte = FT_EMESSA.FkIdEnte
		AND t.AnnoRiferimento = FT_EMESSA.AnnoRiferimento
        AND t.MeseRiferimento = FT_EMESSA.MeseRiferimento
        AND t.FkTipologiaFattura = FT_EMESSA.FkTipologiaFattura
		LEFT JOIN [pfd].[MesiFatture] MF 
        ON t.IdFattura = MF.FkIdFatturaTmp
        WHERE t.[AnnoRiferimento]=@anno AND t.[MeseRiferimento]=@mese
        AND FlagFatturata = 0
        AND FT_EMESSA.IdFattura IS NULL
        AND MF.FkIdFatturaTmp IS NULL
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

    public static string SelectAllSospese()
    {
        return _sqlSospese;
    }
    public static string SelectAllByIdEnte()
    {
        return _sqlByIdEnte;
    }
}