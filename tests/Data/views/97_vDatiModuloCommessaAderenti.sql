-- Vista legacy pfd.v*. DDL reale fornita dal team DB, riprodotta as-is (CREATE -> CREATE OR ALTER).
-- Due rami in UNION: gli enti presenti nell'ultimo export (MAX(DataExport)) di
-- pfw.DatiModuloCommessaAderenti con matrix=1, e tutti gli altri enti di pfd.Enti con matrix=0,
-- ricavandone la provincia da SUBSTRING(istatCode, 1, 3).
CREATE OR ALTER VIEW [pfd].[vDatiModuloCommessaAderenti]
AS
SELECT * FROM (
    SELECT
        [DataExport], [Internalistitutionid], [Segmento], [MacrocategoriaVendita],
        [SottocategoriaVendita], [Provincia], [Regione],
        CASE
            WHEN [SottocategoriaVendita] IN ('Comuni', 'Aziende Sanitarie Locali') THEN 1
            WHEN [SottocategoriaVendita] IN ('Province', 'Consorzi Tra Amministrazioni Locali', 'Acquedotto', 'Regione', 'Camere Di Commercio',
                                             'Altri Enti Locali', 'Ordini Professionali', 'Non Definito', 'Enti Regionali',
                                             'Unioni Di Comuni E Loro Consorzi E Associazioni', 'Utility', 'Altro', 'Università Pubb.',
                                             'Altri Enti', 'Cons_uni', 'Tpl', 'Comunita'' Montane E Loro Consorzi E Associazioni')
                 OR [SottocategoriaVendita] IS NULL THEN 2
            WHEN [SottocategoriaVendita] IN ('Enti Ministeriali', 'Riscossore', 'Ministeri', 'Previdenza', 'Aci') THEN 3
            ELSE 3
        END AS TipoDistribuzione,
        1 as matrix
    FROM [pfw].[DatiModuloCommessaAderenti]
    WHERE [DataExport] = (SELECT MAX([DataExport]) FROM [pfw].[DatiModuloCommessaAderenti])
      AND regione is not null

    UNION

    SELECT
        (SELECT MAX([DataExport]) FROM [pfw].[DatiModuloCommessaAderenti]) AS [DataExport],
        e.[InternalIstitutionId] AS [Internalistitutionid],
        t.[Descrizione] AS [Segmento],
        'Altro' AS [MacrocategoriaVendita],
        'Altro' AS [SottocategoriaVendita],
        p.CodiceIstat as [Provincia],
        r.CodiceIstat as [Regione],
        CASE WHEN r.CodiceIstat IS NULL THEN 3 ELSE 2 END AS TipoDistribuzione,
        0 as matrix
    FROM [pfd].[Enti] e
    LEFT JOIN [pfd].[Contratti] c ON e.[InternalIstitutionId] = c.[internalistitutionid]
    LEFT JOIN [pfw].[TipoContratto] t ON c.[FkIdTipoContratto] = t.[IdTipoContratto]
    LEFT JOIN [pfw].[province] p ON SUBSTRING(e.[istatCode], 1, 3) = p.[CodiceIstat]
    LEFT JOIN [pfw].[regioni] r ON p.[CodiceIstatRegione] = r.[CodiceIstat]
    WHERE e.[InternalIstitutionId] NOT IN (
        SELECT [Internalistitutionid]
        FROM [pfw].[DatiModuloCommessaAderenti]
        WHERE [DataExport] = (SELECT MAX([DataExport]) FROM [pfw].[DatiModuloCommessaAderenti])
          AND [Internalistitutionid] IS NOT NULL
    )
) AS risultati;
GO
