using DocumentFormat.OpenXml.Wordprocessing;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.Queries.Persistence.Builder;

public static class FattureQueryRicercaBuilder
{
    private static string _sqlStored = @"pfd.GenerateJsonSAP";
    private static string _sqlView = @"SELECT
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
	and FT.FkTipologiaFattura IN @TipologiaFattura
	and FT.FkIdEnte <> '4a4149af-172e-4950-9cc8-63ccc9a6d865' --esclusione pagopa
	ORDER BY FT.FkTipologiaFattura, FT.Progressivo
    FOR JSON PATH, INCLUDE_NULL_VALUES )";
    public static string SelectStored()
    {
        return _sqlStored;
    }

    public static string SelectView()
    {
        return _sqlView;
    }
} 