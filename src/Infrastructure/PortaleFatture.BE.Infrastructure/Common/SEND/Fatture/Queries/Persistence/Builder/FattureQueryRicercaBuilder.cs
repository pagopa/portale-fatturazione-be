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
                CAST(FR.Imponibile AS DECIMAL(10, 2))  AS 'imponibile',
				FR.PeriodoRiferimento as 'periodoRiferimento'
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
        AND c.internalistitutionid  = ft.FkIdEnte
	INNER JOIN pfw.FatturaTestataConfig ftc ON ftc.FkTipologiaFattura = FT.FkTipologiaFattura AND ftc.FKIdTipoContratto = c.FkIdTipoContratto
    where FT.AnnoRiferimento = @AnnoRiferimento
	and FT.MeseRiferimento = @MeseRiferimento
    and FT.FkIdEnte = @IdEnte
	[condition_tipologiafattura]
	and FT.FkIdEnte <> '4a4149af-172e-4950-9cc8-63ccc9a6d865' --esclusione pagopa
	and FT.TotaleFattura > 0
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
                CAST(FR.Imponibile AS DECIMAL(10, 2))  AS 'imponibile',
				FR.PeriodoRiferimento as 'periodoRiferimento'
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
        AND c.internalistitutionid  = ft.FkIdEnte
	INNER JOIN pfw.FatturaTestataConfig ftc ON ftc.FkTipologiaFattura = FT.FkTipologiaFattura AND ftc.FKIdTipoContratto = c.FkIdTipoContratto
    where FT.AnnoRiferimento = @AnnoRiferimento
	and FT.MeseRiferimento = @MeseRiferimento
	and FT.FkTipologiaFattura IN @TipologiaFattura
	and FT.FkIdEnte <> '4a4149af-172e-4950-9cc8-63ccc9a6d865' --esclusione pagopa
    AND (@FkIdTipoContratto IS NULL OR c.FkIdTipoContratto = @FkIdTipoContratto)  
    AND (
        @FatturaInviata IS NULL  -- Se NULL, mostra tutte
        OR (@FatturaInviata = 2 AND FT.FatturaInviata IS NULL)  -- In elaborazione
        OR (FT.FatturaInviata = @FatturaInviata)  -- 0 o 1
    )
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
                CAST(FR.Imponibile AS DECIMAL(10, 2))  AS 'imponibile',
				FR.PeriodoRiferimento as 'periodoRiferimento'
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
    AND c.internalistitutionid  = ft.FkIdEnte
	INNER JOIN pfw.FatturaTestataConfig ftc ON ftc.FkTipologiaFattura = FT.FkTipologiaFattura AND ftc.FKIdTipoContratto = c.FkIdTipoContratto
    where FT.AnnoRiferimento = @AnnoRiferimento
	and FT.MeseRiferimento = @MeseRiferimento
	[condition_tipologiafattura]
	and FT.FkIdEnte <> '4a4149af-172e-4950-9cc8-63ccc9a6d865' --esclusione pagopa
	and FT.TotaleFattura > 0
    AND (@FkIdTipoContratto IS NULL OR c.FkIdTipoContratto = @FkIdTipoContratto)  
    AND (
        @FatturaInviata IS NULL  -- Se NULL, mostra tutte
        OR (@FatturaInviata = 2 AND FT.FatturaInviata IS NULL)  -- In elaborazione
        OR (FT.FatturaInviata = @FatturaInviata)  -- 0 o 1
    )
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

    private static string _sqlWhiteList = @"
SELECT 
    idLista AS Id,
    description AS RagioneSociale,
    w.FkIdEnte AS IdEnte,
    Anno,
    Mese,
    [DataInizio],
    [DataFine],
    w.[FkTipologiaFattura] AS TipologiaFattura,
    c.FkIdTipoContratto AS IdTipoContratto,
    tc.Descrizione AS TipoContratto,
    CASE 
        WHEN ft.FkTipologiaFattura IS NOT NULL AND 
		ft.AnnoRiferimento = w.Anno 
		AND ft.MeseRiferimento = w.Mese 
		AND ft.FkTipologiaFattura = w.FkTipologiaFattura 
		AND ft.FkIdEnte = w.FkIdEnte
		THEN 0
        ELSE 1
    END AS Cancella
FROM [pfd].[FattureWhiteList] w
INNER JOIN pfd.Enti e ON e.InternalIstitutionId = w.FkIdEnte
INNER JOIN pfd.Contratti c ON c.internalistitutionid = e.InternalIstitutionId
INNER JOIN pfw.TipoContratto tc ON tc.IdTipoContratto = c.FkIdTipoContratto
LEFT JOIN [pfd].[FattureTestata] ft 
    ON ft.FkTipologiaFattura = w.FkTipologiaFattura
    AND ft.AnnoRiferimento = w.Anno
    AND ft.MeseRiferimento = w.Mese
	AND ft.FkIdEnte = w.FkIdEnte
";


    private static string _sqlWhiteListCount = @"
SELECT 
  count(*)
  FROM [pfd].[FattureWhiteList] w
  inner join pfd.Enti e
 on e.InternalIstitutionId =  w.FkIdEnte
 inner join pfd.Contratti c
 on c.internalistitutionid = e.InternalIstitutionId
 inner join pfw.TipoContratto tc
 on tc.IdTipoContratto = c.FkIdTipoContratto
";


    private static string _sqlWhiteListTipologiaFattura = @"
SELECT  
      distinct TipologiaFattura,
     CASE
        WHEN TipologiaFattura = 'ANTICIPO' THEN 1
        WHEN TipologiaFattura = 'ACCONTO' THEN 2
        WHEN TipologiaFattura = 'PRIMO SALDO' THEN 3
        WHEN TipologiaFattura = 'SECONDO SALDO' THEN 4
        WHEN TipologiaFattura = 'VAR. SEMESTRALE' THEN 5
        ELSE 6 
  END AS ordine
  		   FROM [pfd].[FattureTipologia]
order by ordine
";

    private static string _sqlFattureInvioMultiploSap = @"
SELECT 
    COUNT(CASE WHEN FkIdEnte <> '4a4149af-172e-4950-9cc8-63ccc9a6d865' THEN IdFattura END) AS NumeroFatture,
    [FkProdotto], 
    [FkTipologiaFattura] as TipologiaFattura, 
    [AnnoRiferimento],
    [MeseRiferimento],
    SUM(CASE WHEN FkIdEnte <> '4a4149af-172e-4950-9cc8-63ccc9a6d865' THEN TotaleFattura ELSE 0 END) AS Importo,
    CASE 
        WHEN fatturainviata IS NULL THEN 2 
        ELSE 0
    END AS StatoInvio
  FROM [pfd].[FattureTestata]
WHERE fatturainviata = 0 OR fatturainviata is null
GROUP BY 
    [FkProdotto], 
    [FkTipologiaFattura],
    [AnnoRiferimento], 
    [MeseRiferimento],
    CASE 
        WHEN fatturainviata IS NULL THEN 2
        ELSE 0
    END
order by AnnoRiferimento, MeseRiferimento desc
";


    private static string _sqlFattureInvioMultiploSapPeriodo = @"
SELECT 
    [IdFattura] as IdFattura,
    [FkProdotto], 
    [FkTipologiaFattura] as TipologiaFattura, 
    [FkIdEnte] as IdEnte, 
	e.description as RagioneSociale,
    [DataFattura] as DataFattura, 
    [TotaleFattura] as Importo, 
    [AnnoRiferimento],
    [MeseRiferimento]
  FROM [pfd].[FattureTestata] f
  inner join pfd.Enti e
  ON e.InternalIstitutionId = f.FkIdEnte
WHERE (fatturainviata = 0 OR fatturainviata is NULL)
and FkIdEnte <> '4a4149af-172e-4950-9cc8-63ccc9a6d865' 
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

    private static string _selectPeriodoEnte = @"
	SELECT AnnoRiferimento AS anno,
               MeseRiferimento AS mese,
			   FkTipologiaFattura as TipologiaFattura,
			    CAST(DataFattura AS DATE) AS DataFattura 
        FROM [pfd].[FattureTestata]
        WHERE FkIdEnte = @IdEnte 
        GROUP BY AnnoRiferimento, MeseRiferimento, FkTipologiaFattura,  CAST(DataFattura AS DATE) 
        order by anno desc, mese desc
";

    public static string SelectPeriodoEnte()
    {
        return _selectPeriodoEnte;
    }
    
    private static string _selectEmesseEnte = @"
    SELECT 
    t.IdFattura,
    t.DataFattura,
    t.FkProdotto AS Prodotto,
    t.IdentificativoFattura,
    t.FkTipologiaFattura AS TipologiaFattura, '' AS RiferimentoNumeroLinea,
    AnnoRiferimento, 
    MeseRiferimento, 
    FkIdEnte,
    CASE 
        WHEN FkIdTipoDocumento  = 'TD04' 
        THEN TotaleFattura * -1 
        ELSE TotaleFattura  
    END
    AS TotaleFatturaCalc,
    t.IdDocumento,
    t.DataDocumento,
    t.FkIdEnte AS IstitutioId,
    t.TotaleFattura, 
    t.AnnoRiferimento, 
    t.MeseRiferimento, 
    t.NumItem,
    t.CodCommessa AS CodiceCommessaConvenzione,
    t.Cup,
    t.Cig,
    e.[description] AS RagioneSociale,
    t.Divisa,
    t.MetodoPagamento,
    t.CausaleFattura,
    t.SplitPayment,
    CASE
        WHEN t.FatturaInviata IS NULL THEN 2 -- (@FatturaInviata = 2 AND FT.FatturaInviata IS NULL)  -- IS NULL In elaborazione
        ELSE CONVERT(INT, t.FatturaInviata) 
    END AS 'FatturaInviata',
    t.Progressivo,
    t.Sollecito,
    c.onboardingtokenid AS OnboardingTokenId,
    tp.Descrizione AS TipoContratto, 
    c.onboardingtokenid AS CodiceContratto,
    c.onboardingtokenid AS IdContratto,
    t.FkIdTipoDocumento AS TipoDocumento,
    0 AS Elaborazione,
    CONCAT(FORMAT(t.MeseRiferimento,'00'),'/',t.AnnoRiferimento) as PeriodoFatturazione,
    r.NumeroLinea,
    r.Testo,
    NULL as stato,
    r.CodiceMateriale,
    r.Quantita, 
    r.PrezzoUnitario,
    r.Imponibile,
    r.PeriodoRiferimento
    FROM [pfd].[FattureTestata] t
    LEFT OUTER JOIN pfd.FattureRighe r 
    ON r.FkIdFattura = t.IdFattura 
    INNER JOIN pfd.Enti e
    ON t.FkIdEnte = e.InternalIstitutionId
    INNER JOIN [pfd].[Contratti] c
    ON c.internalistitutionid = t.FkIdEnte
    INNER JOIN [pfw].[TipoContratto] tp
    ON c.FkIdTipoContratto = tp.IdTipoContratto
    Where 
    t.FkIdEnte = @IdEnte
    AND FkTipologiaFattura IN ('PRIMO SALDO', 'SECONDO SALDO', 'VAR. SEMESTRALE')
    AND 
    (
        (@FilterByTipologia = 0 OR t.FkTipologiaFattura IN @TipologiaFattura)
        AND (@Anno IS NULL OR t.AnnoRiferimento = @Anno)
        AND (@Mese IS NULL OR t.MeseRiferimento = @Mese)
        AND (@FilterByDateFattura = 0 OR CAST(t.DataFattura AS DATE) IN @DateFattura)
    );
";

    public static string SelectEmesseEnte()
    {
        return _selectEmesseEnte;
    }


    private static string _selectPeriodoSospeseEnte = @"
	SELECT AnnoRiferimento AS anno,
               MeseRiferimento AS mese,
			   FkTipologiaFattura as TipologiaFattura,
			    CAST(DataFattura AS DATE) AS DataFattura 
        FROM [pfd].[tmpFattureTestata]
        WHERE FkIdEnte = @IdEnte
		and FlagFatturata = 0
        GROUP BY AnnoRiferimento, MeseRiferimento, FkTipologiaFattura,  CAST(DataFattura AS DATE) 
        order by anno desc, mese desc
";
    public static string SelectPeriodoSospeseEnte()
    {
        return _selectPeriodoSospeseEnte;
    }


    private static string _sqlCreditoSospeso = @"
            SELECT
            tft.IdFattura,
            tft.DataFattura,
            tft.FkProdotto AS Prodotto,
            tft.IdentificativoFattura,
            cs.ImportoSospeso, 
            css.ImportoSospeso AS ImportoSospesoParziale, 
            tft.FkTipologiaFattura AS TipologiaFattura, 
            '' AS RiferimentoNumeroLinea,
            tft.IdDocumento,
            tft.DataDocumento,
            tft.FkIdEnte AS IstitutioId,
            tft.TotaleFattura, 
            tft.AnnoRiferimento, 
            tft.MeseRiferimento, 
            tft.NumItem,
            tft.CodCommessa AS CodiceCommessaConvenzione,
            tft.Cup,
            tft.Cig,
            e.[description] AS RagioneSociale,
            tft.Divisa,
            tft.MetodoPagamento,
            tft.CausaleFattura,
            tft.SplitPayment,
            CASE 
                WHEN tft.FatturaInviata IS NULL THEN 2 
                ELSE CONVERT(INT, tft.FatturaInviata) 
            END AS 'FatturaInviata',
            tft.Progressivo,
            tft.Sollecito,
            c.onboardingtokenid AS OnboardingTokenId,
            tp.Descrizione AS TipoContratto,
            c.onboardingtokenid AS CodiceContratto, --ft.CodiceContratto MANCA NELLA TMP
            c.onboardingtokenid AS IdContratto,
            tft.FkIdTipoDocumento AS TipoDocumento,
            0 AS Elaborazione,
            CONCAT(FORMAT(tft.MeseRiferimento,'00'),'/',tft.AnnoRiferimento) as PeriodoFatturazione,
	        'sospesa' as stato,
            fr.NumeroLinea,
            fr.Testo,
            fr.CodiceMateriale,
            fr.Quantita, 
            fr.PrezzoUnitario,
            fr.Imponibile,
            fr.PeriodoRiferimento
            FROM pfd.CreditoSospeso cs 
            LEFT OUTER JOIN pfd.CreditoSospesoStorico css 
            ON cs.FkIdEnte = css.FkIdEnte 
            INNER JOIN pfd.Enti e
            ON cs.FkIdEnte = e.InternalIstitutionId
            INNER JOIN [pfd].[Contratti] c
            ON c.internalistitutionid = cs.FkIdEnte --AND c.onboardingtokenid = ft.CodiceContratto
            INNER JOIN [pfw].[TipoContratto] tp
            ON c.FkIdTipoContratto = tp.IdTipoContratto 
            LEFT OUTER JOIN pfd.tmpFattureTestata tft 
            ON tft.IdFattura = css.FkIdFatturaTmp 
            LEFT OUTER JOIN pfd.tmpFattureRighe fr 
            ON fr.FkIdFattura = tft.IdFattura 
            WHERE cs.FkIdEnte = @IdEnte
            and tft.FlagFatturata = 0
            AND (
                (@FilterByTipologia = 0 OR tft.FkTipologiaFattura IN @TipologiaFattura)
                AND (@Anno IS NULL OR tft.AnnoRiferimento = @Anno)
                AND (@Mese IS NULL OR tft.MeseRiferimento = @Mese)
                AND (@FilterByDateFattura = 0 OR CAST(tft.DataFattura AS DATE) IN @DateFattura)
            );
    ";

    public static string SelectCreditoSospeso()
    {
        return _sqlCreditoSospeso;
    }

    private static string _selectEliminateEnte = @"
    SELECT 
        t.IdFattura,
        t.DataFattura,
        t.FkProdotto AS Prodotto,
        CONCAT(FORMAT(t.MeseRiferimento,'00'),'/',t.AnnoRiferimento) as PeriodoFatturazione,
        t.FkTipologiaFattura AS TipologiaFattura,
        t.FkIdEnte AS IstitutioId,
        c.onboardingtokenid AS OnboardingTokenId,
        e.[description] AS RagioneSociale,
        c.onboardingtokenid AS IdContratto,
        t.FkIdTipoDocumento AS TipoDocumento,
        tp.Descrizione AS TipoContratto, 
        t.Divisa,
        t.MetodoPagamento,
        CONCAT(REPLACE(ftc.Causale,'[percentuale]', ISNULL(ftc.PercentualeAnticipo,'')),' ',CAST(t.MeseRiferimento as varchar(2)),'/',CAST(t.AnnoRiferimento as VARCHAR(4))) as CausaleFattura,
        t.SplitPayment,
        3 AS Inviata,
        ISNULL(t.Sollecito,'') AS Sollecito,
        'eliminata' as stato,
        '' as RiferimentoNumeroLinea,
        t.IdDocumento,
        t.DataDocumento,
        t.NumItem,
        t.CodCommessa AS CodiceCommessaConvenzione,
        t.Cup,
        t.Cig,
        CASE 
            WHEN t.FkIdTipoDocumento  = 'TD04' 
            THEN t.TotaleFattura * -1 
            ELSE t.TotaleFattura  
        END AS TotaleFatturaCalc,
        t.TotaleFattura,
        t.Progressivo
    FROM [pfd].[FattureTestata_Eliminate] t
    INNER JOIN pfd.Enti e
        ON t.FkIdEnte = e.InternalIstitutionId
    INNER JOIN [pfd].[Contratti] c
        ON c.internalistitutionid = t.FkIdEnte
        AND c.onboardingtokenid = t.CodiceContratto
    INNER JOIN [pfw].[TipoContratto] tp
        ON c.FkIdTipoContratto = tp.IdTipoContratto
    INNER JOIN [pfw].[FatturaTestataConfig] ftc
        ON ftc.FkTipologiaFattura = t.FkTipologiaFattura
        AND ftc.FKIdTipoContratto = c.FkIdTipoContratto
    WHERE 
        t.FkIdEnte = @IdEnte
        AND 
        (
            (@FilterByTipologia = 0 OR t.FkTipologiaFattura IN @TipologiaFattura)
            AND (@Anno IS NULL OR t.AnnoRiferimento = @Anno)
            AND (@Mese IS NULL OR t.MeseRiferimento = @Mese)
            AND (@FilterByDateFattura = 0 OR CAST(t.DataFattura AS DATE) IN @DateFattura)
        );
    ";

    public static string SelectEliminateEnte()
    {
        return _selectEliminateEnte;
    }

    public static string SelectMesi()
    {
        return _sqlMesi;
    }

    public static string SelectTipologiaFatturaAnnoMese()
    {
        return _sqlSelectTipologiaFatturaAnnoMese;
    }

    public static string SelectWhiteList()
    {
        return _sqlWhiteList;
    }

    public static string OrderByWhiteList()
    {
        return " ORDER BY anno DESC, mese ";
    }

    public static string SelectWhiteListCount()
    {
        return _sqlWhiteListCount;
    }

    private static string _offSet = " OFFSET (@page-1)*@size ROWS FETCH NEXT @size ROWS ONLY";
    public static string OffSet()
    {
        return _offSet;
    }

    public static string SelectWhiteListTipologiaFattura()
    {
        return _sqlWhiteListTipologiaFattura;
    }

    public static string SelectWhiteListAnniInserisci()
    {
        return $@"

WITH Months AS (
    SELECT 1 AS Mese
    UNION ALL
    SELECT Mese + 1 FROM Months WHERE Mese < 12
),
ExistingData AS ( 
    -- Combine data from FattureTestata and FattureWhiteList
    SELECT DISTINCT
        ft.annoriferimento AS anno,
        ft.meseriferimento AS mese
    FROM [pfd].[FattureTestata] ft
    WHERE ft.FkTipologiaFattura = @TipologiaFattura
    AND ft.annoriferimento <= @anno

    UNION

    SELECT DISTINCT
        fwl.Anno AS anno,
        fwl.Mese AS mese
    FROM [pfd].[FattureWhiteList] fwl
    WHERE fwl.FkTipologiaFattura = @TipologiaFattura
    AND fwl.Anno <= @anno  
    AND fwl.FkIdEnte = @IdEnte  
    AND fwl.DataFine IS NULL
)

-- Select missing months for the given years (previous and current)
SELECT 
    m.AnnoRiferimento,
    m.MeseRiferimento,
    @TipologiaFattura AS TipologiaFattura
FROM (
    -- Generate months for previous year and current year
    SELECT @anno - 1 AS AnnoRiferimento, Mese AS MeseRiferimento
    FROM Months
    UNION ALL
    SELECT @anno AS AnnoRiferimento, Mese AS MeseRiferimento
    FROM Months
    UNION ALL
    SELECT @anno + 1 AS AnnoRiferimento, Mese AS MeseRiferimento
    FROM Months 
) AS m
LEFT JOIN ExistingData e 
    ON m.AnnoRiferimento = e.anno AND m.MeseRiferimento = e.mese
--WHERE e.mese IS NULL  -- Exclude months that already exist in both tables
ORDER BY AnnoRiferimento DESC, MeseRiferimento
OPTION (MAXRECURSION 12);
    ";
    }

    public static string SelectWhiteListAnni()
    {
        return $@"
            SELECT DISTINCT  Anno 
            FROM [pfd].[FattureWhiteList] 
            WHERE DataFine IS NULL
            ORDER BY Anno DESC; 
    ";
    }

    public static string SelectWhiteListMesi()
    {
        return $@"
            SELECT DISTINCT  mese 
            FROM [pfd].[FattureWhiteList]  
    ";
    }
    public static string OrderByWhiteListMesi()
    {
        return $@"
            ORDER BY mese DESC; 
    ";
    }

    public static string SelectFattureInvioMultiploSap()
    {
        return _sqlFattureInvioMultiploSap;
    }
    public static string SelectFattureInvioMultiploSapPeriodo()
    {
        return _sqlFattureInvioMultiploSapPeriodo;
    }

    public static string SelectFattureDate()
    {
        return $@"
SELECT CONVERT(varchar(10), DataFattura, 120) as DataFattura, FkTipologiaFattura as TipologiaFattura from pfd.FattureTestata ";
    }

    public static string OrderByFattureDate()
    {
        return @"
group by CONVERT(varchar(10), DataFattura, 120), FkTipologiaFattura 
order by FkTipologiaFattura, CONVERT(varchar(10), DataFattura, 120) desc; 
"; 
    }
    public static string SelectFattureDateCancellate()
    {
        return $@"
SELECT CONVERT(varchar(10), DataFattura, 120) as DataFattura, FkTipologiaFattura as TipologiaFattura from pfd.FattureTestata_Eliminate ";
    }

    public static string SelectAnniMesiTipologia()
    {
        return $@"
SELECT [AnnoRiferimento] as Anno
      ,[MeseRiferimento] as Mese
	  ,FkTipologiaFattura as TipologiaFattura
  FROM [pfd].[FattureTestata]
";
    }

    public static string GroupOrderByAnniMesiTipologia()
    {
        return $@"
  group by
       [FkProdotto] 
      ,[FkTipologiaFattura]
      ,[FkIdEnte] 
	  ,[AnnoRiferimento]
	  ,[MeseRiferimento]
 order by AnnoRiferimento desc, MeseRiferimento desc
";
    }


    public static string SelectDettaglioSospesoEnte()
    {
        return @"
            SELECT 
            tft.IdFattura as IdFattura, 
            e.description as RagioneSociale,
            tft.FkIdTipoDocumento as TipoDocumento, 
            tft.FkIdEnte as IdEnte, 
            tft.DataFattura as DataFattura,
            tft.Progressivo as Progressivo,
            CAST(
                CASE
                    WHEN tft.FkIdTipoDocumento  = 'TD04'  
                        THEN -tft.TotaleFattura
                    ELSE tft.TotaleFattura
                END AS decimal(18,2)
            ) as TotaleFatturaImponibile, 
            tft.CodiceContratto as IdContratto,
            tft.AnnoRiferimento as Anno,
            tft.MeseRiferimento as Mese,
            tft.FkTipologiaFattura AS TipologiaFattura, 
            ISNULL(trt.[TotaleAnalogico],0) as RelTotaleAnalogico,
            ISNULL(trt.[TotaleDigitale],0) as RelTotaleDigitale,
            ISNULL(trt.[TotaleNotificheAnalogiche],0) as RelTotaleNotificheAnalogiche,
            ISNULL(trt.[TotaleNotificheDigitali],0) as RelTotaleNotificheDigitali,
            ISNULL(trt.[TotaleNotificheAnalogiche],0) + ISNULL(trt.[TotaleNotificheDigitali],0) as RelTotaleNotifiche,
            ISNULL(trt.[Totale],0) as RelTotale,
            ISNULL(trt.[TotaleAnalogicoIva],0)  as RelTotaleIvatoAnalogico,
            ISNULL(trt.[TotaleDigitaleIva],0)  as RelTotaleIvatoDigitale,
            ISNULL(trt.[TotaleIva],0)  as RelTotaleIvato,
            trt.[Caricata] as Caricata,
            trt.[RelFatturata],
            c.FkIdTipoContratto,
            tp.Descrizione as TipologiaContratto,
            -- Valori calcolati sulle righe
            ISNULL(aggAnticipo.ImportoAnalogico, 0) AS AnticipoAnalogico,
            ISNULL(aggAnticipo.ImportoDigitale, 0) AS AnticipoDigitale,
            ISNULL(aggAcconto.ImportoAnalogico, 0) AS AccontoAnalogico,
            ISNULL(aggAcconto.ImportoDigitale, 0) AS AccontoDigitale,
            ISNULL(aggStorno.StornoAnalogico, 0) AS StornoAnalogico,
            ISNULL(aggStorno.StornoDigitale, 0) AS StornoDigitale,
            iva.Iva
            -- Campi tmpFattureRighe --> TODO: togliere commento una volta gestita l'aggregazione nel mapping a BE 
            --,tfr.NumeroLinea,
            --tfr.Testo,
            --tfr.CodiceMateriale,
            --tfr.Quantita,
            --tfr.PrezzoUnitario,
            --tfr.Imponibile,
            --tfr.RigaBollo,
            --tfr.PeriodoRiferimento
            FROM pfd.tmpFattureTestata tft
            --LEFT OUTER JOIN pfd.tmpFattureRighe tfr --> TODO:  togliere commento una volta gestita l'aggregazione nel mapping a BE 
            --on tft.IdFattura = tfr.FkIdFattura
            LEFT OUTER join pfd.Enti e
            ON e.InternalIstitutionId = tft.FkIdEnte
            LEFT OUTER join  [pfd].[tmpRelTestata] trt
            ON trt.year = tft.AnnoRiferimento 
            AND trt.month = tft.MeseRiferimento 
            AND trt.TipologiaFattura = tft.FkTipologiaFattura
            AND trt.internal_organization_id = tft.FkIdEnte 
            AND trt.contract_id = tft.CodiceContratto 
            LEFT JOIN pfd.Contratti c
            ON c.internalistitutionid = e.InternalIstitutionId
            INNER JOIN pfw.TipoContratto tp
            ON c.FkIdTipoContratto = tp.IdTipoContratto
            LEFT JOIN pfd.Iva iva
            ON iva.Anno = tft.AnnoRiferimento AND iva.Mese = tft.MeseRiferimento
            -- OUTER APPLY per fatture ANTICIPO
            OUTER APPLY (
                SELECT 
                    SUM(CASE WHEN rt.CodiceMateriale LIKE 'ANT NA%' OR rt.CodiceMateriale LIKE 'ANTICIPO NA%'
                             THEN rt.Imponibile ELSE 0 END) AS ImportoAnalogico,
                    SUM(CASE WHEN rt.CodiceMateriale LIKE 'ANT ND%' OR rt.CodiceMateriale LIKE 'ANTICIPO ND%'
                             THEN rt.Imponibile ELSE 0 END) AS ImportoDigitale
                FROM pfd.tmpFattureRighe rt
                WHERE rt.FkIdFattura = tft.IdFattura
                  AND tft.FkTipologiaFattura = 'ANTICIPO'  -- Filtro nella WHERE
            ) aggAnticipo
            -- OUTER APPLY per fatture ACCONTO
            OUTER APPLY (
                SELECT 
                    SUM(CASE WHEN rt.CodiceMateriale LIKE 'ACCONTO NOT.ANL%' OR rt.CodiceMateriale LIKE 'STORNO 50% ANT.NA%'
                             THEN rt.Imponibile ELSE 0 END) AS ImportoAnalogico,
                    SUM(CASE WHEN rt.CodiceMateriale LIKE 'ACCONTO NOT.DIG%' OR rt.CodiceMateriale LIKE 'STORNO 50% ANT.ND%'
                             THEN rt.Imponibile ELSE 0 END) AS ImportoDigitale
                FROM pfd.tmpFattureRighe rt
                WHERE rt.FkIdFattura = tft.IdFattura
                  AND tft.FkTipologiaFattura = 'ACCONTO'  -- Filtro nella WHERE
            ) aggAcconto
            OUTER APPLY (
                SELECT 
                    SUM(CASE WHEN rt.CodiceMateriale LIKE 'STORN%' AND rt.CodiceMateriale LIKE '%NA'
                             THEN rt.Imponibile ELSE 0 END) AS StornoAnalogico,
                    SUM(CASE WHEN rt.CodiceMateriale LIKE 'STORNO%' AND rt.CodiceMateriale LIKE '%ND'
                             THEN rt.Imponibile ELSE 0 END) AS StornoDigitale
                FROM pfd.tmpFattureRighe rt
                WHERE rt.FkIdFattura = tft.IdFattura
                  AND tft.FkTipologiaFattura = 'PRIMO SALDO'  -- Filtro nella WHERE
            ) aggStorno
            WHERE tft.FkIdEnte = @IdEnte AND tft.IdFattura = @IdFattura";
    }

    /// <summary>
    /// Query per il recupero dei dettagli della fatture emesse per un ente. 
    /// Compresi i dettagli delle fatture sospese comprese nella fattura emessa
    /// </summary>
    /// <returns></returns>
    public static string SelectDettaglioEmessoEnte()
    {
        return @"
            SELECT 
            tft.IdFattura as IdFattura, 
            e.description as RagioneSociale,
            tft.FkIdTipoDocumento as TipoDocumento, 
            tft.FkIdEnte as IdEnte, 
            tft.DataFattura as DataFattura,
            tft.Progressivo as Progressivo,
            CAST(
            CASE
            WHEN tft.FkIdTipoDocumento  = 'TD04'  
                THEN -tft.TotaleFattura
            ELSE tft.TotaleFattura
                 END AS decimal(18,2)) as TotaleFatturaImponibile, 
            tft.CodiceContratto as IdContratto,
            tft.AnnoRiferimento as Anno,
            tft.MeseRiferimento as Mese,
            tft.FkTipologiaFattura AS TipologiaFattura, 
            ISNULL(trt.[TotaleAnalogico],0) as RelTotaleAnalogico,
            ISNULL(trt.[TotaleDigitale],0) as RelTotaleDigitale,
            ISNULL(trt.[TotaleNotificheAnalogiche],0) as RelTotaleNotificheAnalogiche,
            ISNULL(trt.[TotaleNotificheDigitali],0) as RelTotaleNotificheDigitali,
            ISNULL(trt.[TotaleNotificheAnalogiche],0) + ISNULL(trt.[TotaleNotificheDigitali],0) as RelTotaleNotifiche,
            ISNULL(trt.[Totale],0) as RelTotale,
            ISNULL(trt.[TotaleAnalogicoIva],0)  as RelTotaleIvatoAnalogico,
            ISNULL(trt.[TotaleDigitaleIva],0)  as RelTotaleIvatoDigitale,
            ISNULL(trt.[TotaleIva],0)  as RelTotaleIvato,
            trt.[Caricata] as Caricata,
            trt.[RelFatturata],
            c.FkIdTipoContratto,
            tp.Descrizione as TipologiaContratto,
            ISNULL(aggAnticipo.ImportoAnalogico, 0) AS AnticipoAnalogico,
            ISNULL(aggAnticipo.ImportoDigitale, 0) AS AnticipoDigitale,
            ISNULL(aggAcconto.ImportoAnalogico, 0) AS AccontoAnalogico,
            ISNULL(aggAcconto.ImportoDigitale, 0) AS AccontoDigitale,
            ISNULL(aggStorno.StornoAnalogico, 0) AS StornoAnalogico,
            ISNULL(aggStorno.StornoDigitale, 0) AS StornoDigitale
            -- Dettaglio fatture sospese compresee nella fattura emessa
            ,mf.FkIdFatturaTmp As IdFatturaSospesa
            ,tftt.DataFattura AS DataFatturaSospesa
            ,tftt.Progressivo AS ProgressivoSospesa
            ,tftt.FkIdTipoDocumento AS TipoDocumentoSospesa,
            CAST(
            CASE
            WHEN tftt.FkIdTipoDocumento  = 'TD04'  
                THEN -tftt.TotaleFattura
            ELSE tftt.TotaleFattura
                 END AS decimal(18,2)) as TotaleFatturaSospesaImponibile,
            tftt.CodiceContratto as IdContrattoSospesa,
            tftt.AnnoRiferimento as AnnoSospesa,
            tftt.MeseRiferimento as MeseSospesa,
            tftt.FkTipologiaFattura AS TipologiaFatturaSospesa, 
            tftt.TotaleFattura as TotaleFatturaSospesa,
            tftt.MetodoPagamento as MetodoPagamentoSospesa,
            tftt.CausaleFattura as CausaleFatturaSospesa,
            tftt.Sollecito as SollecitoSospesa,
            tftt.SplitPayment as SplitPaymentSospesa,
            tftt.DataDocumento as DataDocumentoSospeso,
            tftt.CodCommessa AS CodiceCommessaSospesa,
            tftt.NumItem AS NumItemSospesa,
            tftt.FatturaInviata AS FatturaInviataSospesa,
            tftt.Semestre AS SemestreSospesa,
            tftt.FlagFatturata AS FlagFatturataSospesa,
            ISNULL(tmprl.[TotaleAnalogico],0) as RelTotaleAnalogicoSospeso,
            ISNULL(tmprl.[TotaleDigitale],0) as RelTotaleDigitaleSospeso,
            ISNULL(tmprl.[TotaleNotificheAnalogiche],0) as RelTotaleNotificheAnalogicheSospeso,
            ISNULL(tmprl.[TotaleNotificheDigitali],0) as RelTotaleNotificheDigitaliSospeso,
            ISNULL(tmprl.[TotaleNotificheAnalogiche],0) + ISNULL(tmprl.[TotaleNotificheDigitali],0) as RelTotaleNotificheSospeso,
            ISNULL(tmprl.[Totale],0) as RelTotaleSospeso,
            ISNULL(tmprl.[TotaleAnalogicoIva],0)  as RelTotaleIvatoAnalogicoSospeso,
            ISNULL(tmprl.[TotaleDigitaleIva],0)  as RelTotaleIvatoDigitaleSospeso,
            ISNULL(tmprl.[TotaleIva],0)  as RelTotaleIvatoSospeso,
            tmprl.[Caricata] as CaricataSospeso,
            tmprl.[RelFatturata] as RelFatturataSospeso,
            iva.Iva
            FROM pfd.FattureTestata tft
            LEFT OUTER JOIN [pfd].[MesiFatture] mf
            ON tft.IdFattura = mf.FkIdFattura
            LEFT OUTER JOIN pfd.Enti e
            ON e.InternalIstitutionId = tft.FkIdEnte
            LEFT OUTER JOIN [pfd].[RelTestata] trt
            ON trt.year = tft.AnnoRiferimento 
            AND trt.month = tft.MeseRiferimento 
            AND trt.TipologiaFattura = tft.FkTipologiaFattura
            AND trt.internal_organization_id = tft.FkIdEnte 
            AND trt.contract_id = tft.CodiceContratto
            LEFT JOIN pfd.Contratti c
            ON c.internalistitutionid = e.InternalIstitutionId
            LEFT JOIN pfw.TipoContratto tp
            ON c.FkIdTipoContratto = tp.IdTipoContratto
            LEFT JOIN pfd.tmpFattureTestata tftt
            ON tftt.IdFattura = mf.FkIdFatturaTmp
            LEFT JOIN pfd.tmpRelTestata tmprl
            ON tftt.FkIdEnte = tmprl.internal_organization_id 
            AND tftt.CodiceContratto = tmprl.contract_id
            AND tftt.FkTipologiaFattura = tmprl.TipologiaFattura
            AND tftt.AnnoRiferimento = tmprl.year
            AND tftt.MeseRiferimento = tmprl.month
            LEFT JOIN pfd.Iva iva
            ON iva.Anno = tft.AnnoRiferimento AND iva.Mese = tft.MeseRiferimento
             -- OUTER APPLY per fatture ANTICIPO
            OUTER APPLY (
                SELECT 
                    SUM(CASE WHEN rt.CodiceMateriale LIKE 'ANT NA%' OR rt.CodiceMateriale LIKE 'ANTICIPO NA%'
                             THEN rt.Imponibile ELSE 0 END) AS ImportoAnalogico,
                    SUM(CASE WHEN rt.CodiceMateriale LIKE 'ANT ND%' OR rt.CodiceMateriale LIKE 'ANTICIPO ND%'
                             THEN rt.Imponibile ELSE 0 END) AS ImportoDigitale
                FROM pfd.FattureRighe rt
                WHERE rt.FkIdFattura = tft.IdFattura
                  AND tft.FkTipologiaFattura = 'ANTICIPO'  -- Filtro nella WHERE
            ) aggAnticipo
            -- OUTER APPLY per fatture ACCONTO
            OUTER APPLY (
                SELECT 
                    SUM(CASE WHEN rt.CodiceMateriale LIKE 'ACCONTO NOT.ANL%' OR rt.CodiceMateriale LIKE 'STORNO 50% ANT.NA%'
                             THEN rt.Imponibile ELSE 0 END) AS ImportoAnalogico,
                    SUM(CASE WHEN rt.CodiceMateriale LIKE 'ACCONTO NOT.DIG%' OR rt.CodiceMateriale LIKE 'STORNO 50% ANT.ND%'
                             THEN rt.Imponibile ELSE 0 END) AS ImportoDigitale
                FROM pfd.FattureRighe rt
                WHERE rt.FkIdFattura = tft.IdFattura
                  AND tft.FkTipologiaFattura = 'ACCONTO'  -- Filtro nella WHERE
            ) aggAcconto
            -- OUTER APPLY per fatture STORNO 'PRIMO SALDO'
            OUTER APPLY (
                SELECT 
                    SUM(CASE WHEN rt.CodiceMateriale LIKE 'STORN%' AND rt.CodiceMateriale LIKE '%NA'
                             THEN rt.Imponibile ELSE 0 END) AS StornoAnalogico,
                    SUM(CASE WHEN rt.CodiceMateriale LIKE 'STORN%' AND rt.CodiceMateriale LIKE '%ND'
                             THEN rt.Imponibile ELSE 0 END) AS StornoDigitale
                FROM pfd.FattureRighe rt
                WHERE rt.FkIdFattura = tft.IdFattura
                  AND tft.FkTipologiaFattura = 'PRIMO SALDO'  -- Filtro nella WHERE
            ) aggStorno
            WHERE tft.FkIdEnte = @IdEnte 
            AND tft.IdFattura = @IdFattura
            AND NOT EXISTS (
                SELECT 1
                FROM pfd.tmpFattureTestata t_all
                INNER JOIN [pfd].[MesiFatture] mf_all 
                    ON t_all.IdFattura = mf_all.FkIdFatturaTmp
                WHERE mf_all.FkIdFattura = tft.IdFattura
                  AND (
                        t_all.AnnoRiferimento * 100 + t_all.MeseRiferimento) >
                        (tft.AnnoRiferimento * 100 + tft.MeseRiferimento)
                      
                )";
            
    }       

    public static string SelectDettaglioSospeso()
    {
        return @"";
    }

    public static string SelectDettaglioEmesso()
    {
        return @"";
    }
}
