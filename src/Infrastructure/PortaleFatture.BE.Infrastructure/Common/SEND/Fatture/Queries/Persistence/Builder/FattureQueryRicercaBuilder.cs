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
) AS m
LEFT JOIN ExistingData e 
    ON m.AnnoRiferimento = e.anno AND m.MeseRiferimento = e.mese
WHERE e.mese IS NULL  -- Exclude months that already exist in both tables
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
}
