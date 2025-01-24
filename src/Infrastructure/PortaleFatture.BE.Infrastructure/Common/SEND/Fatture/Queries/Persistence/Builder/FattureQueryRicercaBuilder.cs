namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

public static class FattureQueryRicercaBuilder
{
    private static string _sqlStored = @"pfd.GenerateJsonSAP";
    private static string _sqlViewByIdEnte = @"SELECT
     listaFatture =    (

    SELECT 
        CAST(FT.TotaleFattura AS DECIMAL(10, 2)) AS 'fattura.totale', 
        FT.Progressivo AS 'fattura.numero',
        CONVERT(VARCHAR, FT.DataFattura, 23) AS 'fattura.dataFattura',
        FT.FkProdotto AS 'fattura.prodotto',
        CAST(FT.MeseRiferimento as varchar(2)) + '/' + CAST(FT.AnnoRiferimento as VARCHAR(4)) AS 'fattura.identificativo',
        FT.FkTipologiaFattura AS 'fattura.tipologiaFattura', -- Changed this line
        FT.FkIdEnte AS 'fattura.istitutioID',
        FT.CodiceContratto AS 'fattura.onboardingTokenID',
        FT.FkIdTipoDocumento AS 'fattura.tipoDocumento',
        FT.Divisa AS 'fattura.divisa',
        FT.MetodoPagamento AS 'fattura.metodoPagamento', 
		CONCAT(REPLACE( ftc.Causale,'[percentuale]', ISNULL(ftc.PercentualeAnticipo,'')),' ' ,CAST(FT.MeseRiferimento as varchar(2)), '/' , CAST(FT.AnnoRiferimento as VARCHAR(4))) as 'fattura.causale',
        FT.SplitPayment AS 'fattura.split',
        ISNULL(FT.Sollecito, '') AS 'fattura.sollecito',
        (
            SELECT
                ISNULL(tC.[Descrizione],'') AS 'tipologia',  
                '' AS 'riferimentoNumeroLinea',
                ISNULL(FTn.IdDocumento,'') AS 'idDocumento',
                CONVERT(VARCHAR,FTn.DataDocumento, 23) AS 'data',
                ISNULL(FTn.NumItem,'') AS 'numItem',
                ISNULL(FTn.CodCommessa,'') AS 'codiceCommessaConvenzione',
                ISNULL(FTn.Cup,'') AS 'CUP',
                ISNULL(FTn.Cig,'') AS 'CIG'
            FROM [pfd].[FattureTestata] FTn
            LEFT JOIN [pfw].[DatiFatturazione] dF ON ISNULL(FTn.FkIdDatiFatturazione,'') = ISNULL(dF.IdDatiFatturazione,'')
            LEFT JOIN [pfw].[TipoCommessa] tC ON dF.[FkTipoCommessa] = tC.[TipoCommessa]
               where FTn.IdFattura = FT.IdFattura
            FOR JSON PATH
        ) AS 'fattura.datiGeneraliDocumento',
        (
            SELECT
                FR.NumeroLinea AS 'numerolinea',
                ISNULL(FR.Testo,'') AS 'testo',
                FR.CodiceMateriale AS 'codiceMateriale',
                FR.Quantita AS 'quantita',
                CAST(FR.PrezzoUnitario AS DECIMAL(10, 2))  AS 'prezzoUnitario',
                CAST(FR.Imponibile AS DECIMAL(10, 2))  AS 'imponibile'
            FROM [pfd].[FattureRighe] FR
			LEFT JOIN [pfw].[CodiciMateriali] CM
			ON FR.CodiceMateriale = CM.CodiceMateriale
            WHERE FT.IdFattura = FR.FkIdFattura
			ORDER BY CM.Ordinamento
            FOR JSON PATH
        ) AS 'fattura.posizioni'
    FROM
        [pfd].[FattureTestata] FT 
	INNER JOIN pfd.Contratti c ON c.onboardingtokenid = FT.CodiceContratto
	INNER JOIN pfw.FatturaTestataConfig ftc ON ftc.FkTipologiaFattura = FT.FkTipologiaFattura AND ftc.FKIdTipoContratto = c.FkIdTipoContratto
    where FT.AnnoRiferimento = @AnnoRiferimento
	and FT.MeseRiferimento = @MeseRiferimento
    and FT.FkIdEnte = @IdEnte
	and FT.FkTipologiaFattura IN @TipologiaFattura
	and FT.FkIdEnte <> '4a4149af-172e-4950-9cc8-63ccc9a6d865' --esclusione pagopa
	ORDER BY FT.FkTipologiaFattura, FT.Progressivo
    FOR JSON PATH, INCLUDE_NULL_VALUES )";


    private static string _sqlSelectIdsByParameters = @"
SELECT idfattura
FROM   [pfd].[fatturetestata]
WHERE  annoriferimento = @anno
       AND meseriferimento = @mese
       AND fktipologiafattura = @TipologiaFattura
       AND [fatturainviata] = @StatoAtteso 
       AND FkIdEnte <> '4a4149af-172e-4950-9cc8-63ccc9a6d865' --esclusione pagopa ";

    private static string _orderByFattureInvioSap = @"
    ORDER BY
      ordine,
      [FkTipologiaFattura];";

    private static string _sqlVerifyModifica = @"
SELECT 
    CASE 
        WHEN @TipologiaFattura = 'ANTICIPO' THEN  
            CASE 
                WHEN (
                    SELECT COUNT([IdFattura]) 
                    FROM [pfd].[FattureTestata] t2 
                    WHERE t2.FkTipologiaFattura = @TipologiaFattura
					AND AnnoRiferimento = @anno and MeseRiferimento=@mese
                    AND t2.FkIdEnte IN (
                          SELECT t1.FkIdEnte 
                          FROM [pfd].[FattureTestata] t1 
                          WHERE t1.FkTipologiaFattura = 'PRIMO SALDO'
						  AND AnnoRiferimento = @anno and MeseRiferimento=@mese
                      )
                ) = 0 THEN 0
                ELSE 2
            END
        WHEN @TipologiaFattura = 'ACCONTO' THEN --verifica PAC
            CASE 
                WHEN (
                    SELECT COUNT([IdFattura]) 
                    FROM [pfd].[FattureTestata] t2 
                    WHERE t2.FkTipologiaFattura = @TipologiaFattura
					AND AnnoRiferimento = @anno and MeseRiferimento=@mese
                    AND t2.FkIdEnte NOT IN (
                          SELECT t1.FkIdEnte 
                          FROM [pfd].[FattureTestata] t1 
                          WHERE t1.FkTipologiaFattura = 'PRIMO SALDO'
						  AND AnnoRiferimento = @anno and MeseRiferimento=@mese
                      )
                ) > 0 THEN 0
                ELSE 2
            END
        WHEN @TipologiaFattura = 'PRIMO SALDO' OR @TipologiaFattura = 'SECONDO SALDO' THEN  --verifica semestrale/annuale
		      CASE 
                WHEN (
                    SELECT COUNT([IdFattura]) 
                    FROM [pfd].[FattureTestata] t2 
						WHERE 
							(
								t2.FkTipologiaFattura LIKE '%VAR.%'
								AND t2.AnnoRiferimento = @anno
								AND t2.MeseRiferimento >= @mese
							)
							OR 
							(
								t2.FkTipologiaFattura = @TipologiaFattura
								AND t2.AnnoRiferimento = @anno
								AND t2.MeseRiferimento > @mese
							)
                ) > 0 THEN 2
                ELSE 0
            END
        WHEN @TipologiaFattura LIKE '%VAR.%' THEN  --verifica semestrale/annuale
		      CASE 
                WHEN (
                    SELECT COUNT([IdFattura]) 
                    FROM [pfd].[FattureTestata] t2 
						WHERE 
							(
								t2.FkTipologiaFattura LIKE '%VAR.%'
								AND t2.AnnoRiferimento = @anno
								AND t2.MeseRiferimento > @mese
							) 
                ) > 0 THEN 2
                ELSE 0
            END
        ELSE 1
    END AS ExtraCondition,
    CASE 
        WHEN COUNT([IdFattura]) > 0 THEN 2 
        ELSE 0 
    END AS Modifica,
    @TipologiaFattura AS TipologiaFattura
FROM [pfd].[FattureTestata] t
WHERE AnnoRiferimento = @anno and MeseRiferimento=@mese
AND t.FkTipologiaFattura IN @ListaTipologiaFattura; 
";

    private static string _sqlFattureInvioSap = @"
WITH filterData AS (
  SELECT *
  FROM [pfd].[FattureTestata]
  WHERE FkIdEnte <> '4a4149af-172e-4950-9cc8-63ccc9a6d865'
)
SELECT  
  [FkTipologiaFattura] AS TipologiaFattura,
  COUNT(*) AS NumeroFatture,
  t.AnnoRiferimento,
  t.MeseRiferimento,
    CASE 
        WHEN t.FatturaInviata IS NULL THEN 2 
        ELSE CONVERT(INT, t.FatturaInviata) 
    END AS Azione, 
  CASE
        WHEN [FkTipologiaFattura] = 'ANTICIPO' THEN 1
        WHEN [FkTipologiaFattura] = 'ACCONTO' THEN 2
        WHEN [FkTipologiaFattura] = 'PRIMO SALDO' THEN 3
        WHEN [FkTipologiaFattura] = 'SECONDO SALDO' THEN 4
        WHEN [FkTipologiaFattura] = 'VAR. SEMESTRALE' THEN 5
        ELSE 6
  END AS ordine
FROM
  filterData t 
";

    private static string _groupByFattureInvioSap = @"
  group by [FkTipologiaFattura],  
           [FatturaInviata],
		   [AnnoRiferimento],
		   [MeseRiferimento] 
";

    private static string _sqlViewCancellate = @"
SELECT
     listaFatture =    ( 
    SELECT 
        CAST(FT.TotaleFattura AS DECIMAL(10, 2)) AS 'fattura.totale', 
        FT.Progressivo AS 'fattura.numero', 
		FT.IdFattura AS 'fattura.idfattura',
        CONVERT(VARCHAR, FT.DataFattura, 23) AS 'fattura.dataFattura',
        FT.FkProdotto AS 'fattura.prodotto',
        CAST(FT.MeseRiferimento as varchar(2)) + '/' + CAST(FT.AnnoRiferimento as VARCHAR(4)) AS 'fattura.identificativo',
        FT.FkTipologiaFattura AS 'fattura.tipologiaFattura', -- Changed this line
        FT.FkIdEnte AS 'fattura.istitutioID',
        FT.CodiceContratto AS 'fattura.onboardingTokenID',
        FT.FkIdTipoDocumento AS 'fattura.tipoDocumento',
        FT.Divisa AS 'fattura.divisa',
        FT.MetodoPagamento AS 'fattura.metodoPagamento', 
		CONCAT(REPLACE( ftc.Causale,'[percentuale]', ISNULL(ftc.PercentualeAnticipo,'')),' ' ,CAST(FT.MeseRiferimento as varchar(2)), '/' , CAST(FT.AnnoRiferimento as VARCHAR(4))) as 'fattura.causale',
        FT.SplitPayment AS 'fattura.split', 
		3 AS 'fattura.inviata',
        ISNULL(FT.Sollecito, '') AS 'fattura.sollecito',
        (
            SELECT
                ISNULL(tC.[Descrizione],'') AS 'tipologia',  
                '' AS 'riferimentoNumeroLinea',
                ISNULL(FTn.IdDocumento,'') AS 'idDocumento',
                CONVERT(VARCHAR,FTn.DataDocumento, 23) AS 'data',
                ISNULL(FTn.NumItem,'') AS 'numItem',
                ISNULL(FTn.CodCommessa,'') AS 'codiceCommessaConvenzione',
                ISNULL(FTn.Cup,'') AS 'CUP',
                ISNULL(FTn.Cig,'') AS 'CIG'
            FROM [pfd].[FattureTestata_Eliminate] FTn
            LEFT JOIN [pfw].[DatiFatturazione] dF ON ISNULL(FTn.FkIdDatiFatturazione,'') = ISNULL(dF.IdDatiFatturazione,'')
            LEFT JOIN [pfw].[TipoCommessa] tC ON dF.[FkTipoCommessa] = tC.[TipoCommessa]
               where FTn.IdFattura = FT.IdFattura
            FOR JSON PATH
        ) AS 'fattura.datiGeneraliDocumento',
        (
            SELECT
                FR.NumeroLinea AS 'numerolinea',
                ISNULL(FR.Testo,'') AS 'testo',
                FR.CodiceMateriale AS 'codiceMateriale',
                FR.Quantita AS 'quantita',
                CAST(FR.PrezzoUnitario AS DECIMAL(10, 2))  AS 'prezzoUnitario',
                CAST(FR.Imponibile AS DECIMAL(10, 2))  AS 'imponibile'
            FROM [pfd].[FattureRighe_Eliminate] FR
			LEFT JOIN [pfw].[CodiciMateriali] CM
			ON FR.CodiceMateriale = CM.CodiceMateriale
            WHERE FT.IdFattura = FR.FkIdFattura
			ORDER BY CM.Ordinamento
            FOR JSON PATH
        ) AS 'fattura.posizioni'
    FROM
        [pfd].[FattureTestata_Eliminate] FT 
	INNER JOIN pfd.Contratti c ON c.onboardingtokenid = FT.CodiceContratto
	INNER JOIN pfw.FatturaTestataConfig ftc ON ftc.FkTipologiaFattura = FT.FkTipologiaFattura AND ftc.FKIdTipoContratto = c.FkIdTipoContratto
    where FT.AnnoRiferimento = @AnnoRiferimento
	and FT.MeseRiferimento = @MeseRiferimento
	and FT.FkTipologiaFattura IN @TipologiaFattura
	and FT.FkIdEnte <> '4a4149af-172e-4950-9cc8-63ccc9a6d865' --esclusione pagopa
	ORDER BY FT.FkTipologiaFattura, FT.Progressivo
    FOR JSON PATH, INCLUDE_NULL_VALUES)";

    private static string _sqlView = @"SELECT
     listaFatture =    (

    SELECT 
        CAST(FT.TotaleFattura AS DECIMAL(10, 2)) AS 'fattura.totale', 
        FT.Progressivo AS 'fattura.numero',
		FT.IdFattura AS 'fattura.idfattura', 
        CONVERT(VARCHAR, FT.DataFattura, 23) AS 'fattura.dataFattura',
        FT.FkProdotto AS 'fattura.prodotto',
        CAST(FT.MeseRiferimento as varchar(2)) + '/' + CAST(FT.AnnoRiferimento as VARCHAR(4)) AS 'fattura.identificativo',
        FT.FkTipologiaFattura AS 'fattura.tipologiaFattura', -- Changed this line
        FT.FkIdEnte AS 'fattura.istitutioID',
        FT.CodiceContratto AS 'fattura.onboardingTokenID',
        FT.FkIdTipoDocumento AS 'fattura.tipoDocumento',
        FT.Divisa AS 'fattura.divisa',
        FT.MetodoPagamento AS 'fattura.metodoPagamento', 
		CONCAT(REPLACE( ftc.Causale,'[percentuale]', ISNULL(ftc.PercentualeAnticipo,'')),' ' ,CAST(FT.MeseRiferimento as varchar(2)), '/' , CAST(FT.AnnoRiferimento as VARCHAR(4))) as 'fattura.causale',
        FT.SplitPayment AS 'fattura.split', 
        CASE 
            WHEN FT.FatturaInviata IS NULL THEN 2 
            ELSE CONVERT(INT, FT.FatturaInviata) 
        END AS 'fattura.inviata',
        ISNULL(FT.Sollecito, '') AS 'fattura.sollecito',
        (
            SELECT
                ISNULL(tC.[Descrizione],'') AS 'tipologia',  
                '' AS 'riferimentoNumeroLinea',
                ISNULL(FTn.IdDocumento,'') AS 'idDocumento',
                CONVERT(VARCHAR,FTn.DataDocumento, 23) AS 'data',
                ISNULL(FTn.NumItem,'') AS 'numItem',
                ISNULL(FTn.CodCommessa,'') AS 'codiceCommessaConvenzione',
                ISNULL(FTn.Cup,'') AS 'CUP',
                ISNULL(FTn.Cig,'') AS 'CIG'
            FROM [pfd].[FattureTestata] FTn
            LEFT JOIN [pfw].[DatiFatturazione] dF ON ISNULL(FTn.FkIdDatiFatturazione,'') = ISNULL(dF.IdDatiFatturazione,'')
            LEFT JOIN [pfw].[TipoCommessa] tC ON dF.[FkTipoCommessa] = tC.[TipoCommessa]
               where FTn.IdFattura = FT.IdFattura
            FOR JSON PATH
        ) AS 'fattura.datiGeneraliDocumento',
        (
            SELECT
                FR.NumeroLinea AS 'numerolinea',
                ISNULL(FR.Testo,'') AS 'testo',
                FR.CodiceMateriale AS 'codiceMateriale',
                FR.Quantita AS 'quantita',
                CAST(FR.PrezzoUnitario AS DECIMAL(10, 2))  AS 'prezzoUnitario',
                CAST(FR.Imponibile AS DECIMAL(10, 2))  AS 'imponibile'
            FROM [pfd].[FattureRighe] FR
			LEFT JOIN [pfw].[CodiciMateriali] CM
			ON FR.CodiceMateriale = CM.CodiceMateriale
            WHERE FT.IdFattura = FR.FkIdFattura
			ORDER BY CM.Ordinamento
            FOR JSON PATH
        ) AS 'fattura.posizioni'
    FROM
        [pfd].[FattureTestata] FT 
	INNER JOIN pfd.Contratti c ON c.onboardingtokenid = FT.CodiceContratto
	INNER JOIN pfw.FatturaTestataConfig ftc ON ftc.FkTipologiaFattura = FT.FkTipologiaFattura AND ftc.FKIdTipoContratto = c.FkIdTipoContratto
    where FT.AnnoRiferimento = @AnnoRiferimento
	and FT.MeseRiferimento = @MeseRiferimento
	[condition_tipologiafattura]
	and FT.FkIdEnte <> '4a4149af-172e-4950-9cc8-63ccc9a6d865' --esclusione pagopa
	ORDER BY FT.FkTipologiaFattura, FT.Progressivo
    FOR JSON PATH, INCLUDE_NULL_VALUES )";

    private static string _sqlAnni = @"
SELECT  
      distinct AnnoRiferimento
   FROM [pfd].[FattureTestata]
";

    private static string _sqlMesi = @"
SELECT  
      distinct MeseRiferimento
   FROM [pfd].[FattureTestata]
";

    private static string _sqlSelectTipologiaFatturaAnnoMese = @"
SELECT  
      distinct FkTipologiaFattura,
     CASE
        WHEN [FkTipologiaFattura] = 'ANTICIPO' THEN 1
        WHEN [FkTipologiaFattura] = 'ACCONTO' THEN 2
        WHEN [FkTipologiaFattura] = 'PRIMO SALDO' THEN 3
        WHEN [FkTipologiaFattura] = 'SECONDO SALDO' THEN 4
        WHEN [FkTipologiaFattura] = 'VAR. SEMESTRALE' THEN 5
        ELSE 6 
  END AS ordine
  		   FROM [pfd].[FattureTestata]
     where AnnoRiferimento=@anno and MeseRiferimento=@mese
";

    public static string OrderByYear()
    {
        return " ORDER BY AnnoRiferimento desc";
    }
    public static string OrderByMonth()
    {
        return " ORDER BY MeseRiferimento desc";
    }

    public static string SelectStored()
    {
        return _sqlStored;
    }

    public static string SelectView()
    {
        return _sqlView;
    }

    public static string SelectViewCancellate()
    {
        return _sqlViewCancellate;
    }

    public static string SelectFattureInvioSap()
    {
        return _sqlFattureInvioSap;
    }
    public static string SelectVerificaModificaFatture()
    {
        return _sqlVerifyModifica;
    }

    public static string SqlSelectIdsByParameters()
    {
        return _sqlSelectIdsByParameters;
    }

    public static string GroupByFattureInvioSap()
    {
        return _groupByFattureInvioSap;
    }

    public static string OrderByFattureInvioSap()
    {
        return _orderByFattureInvioSap;
    }
    public static string SelectViewByIdEnte()
    {
        return _sqlViewByIdEnte;
    }
    public static string SelectAnni()
    {
        return _sqlAnni;
    }

    public static string SelectMesi()
    {
        return _sqlMesi;
    }

    public static string SelectTipologiaFatturaAnnoMese()
    {
        return _sqlSelectTipologiaFatturaAnnoMese;
    }
}
