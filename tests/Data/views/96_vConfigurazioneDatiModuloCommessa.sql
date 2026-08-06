-- Vista legacy pfd.v* (senza la 'w'). DDL reale fornita dal team DB, riprodotta as-is
-- (solo CREATE -> CREATE OR ALTER per l'hot-apply).
-- Esplode con STRING_SPLIT le liste 'anno/mese' delle colonne obbligatorio/facoltativo/archiviato
-- di cfg.ConfigurazioneDatiModuloCommessa e ne ricava datavalidita/datavaliditalegale usando le
-- finestre di cfg.FrameModuloCommessa (frame = tecnica, framelegale = legale).
-- ⚠️ Il campo Source dipende da GETDATE(): per il mese CORRENTE un 'obbligatorio' fuori finestra
-- diventa 'archiviato'. Un test che asserisce su Source va scritto su periodi non correnti.
CREATE OR ALTER VIEW [pfd].[vConfigurazioneDatiModuloCommessa]
AS
WITH split AS (
    SELECT cmd.FkIdEnte, cmd.anno as annoRiferimento, cmd.mese as meseRiferimento,
        fc.frame, fc.framelegale, 'obbligatorio' AS Source, TRIM(value) AS DatePart
    FROM [cfg].[ConfigurazioneDatiModuloCommessa] cmd
    INNER JOIN [cfg].[FrameModuloCommessa] fc ON fc.anno = cmd.anno AND fc.mese = cmd.mese
    CROSS APPLY STRING_SPLIT(cmd.obbligatorio, ';')
    WHERE cmd.obbligatorio IS NOT NULL
    UNION ALL
    SELECT cmd.FkIdEnte, cmd.anno as annoRiferimento, cmd.mese as meseRiferimento,
        fc.frame, fc.framelegale, 'facoltativo' AS Source, TRIM(value) AS DatePart
    FROM [cfg].[ConfigurazioneDatiModuloCommessa] cmd
    INNER JOIN [cfg].[FrameModuloCommessa] fc ON fc.anno = cmd.anno AND fc.mese = cmd.mese
    CROSS APPLY STRING_SPLIT(cmd.facoltativo, ';')
    WHERE cmd.facoltativo IS NOT NULL
    UNION ALL
    SELECT cmd.FkIdEnte, cmd.anno as annoRiferimento, cmd.mese as meseRiferimento,
        fc.frame, fc.framelegale, 'archiviato' AS Source, TRIM(value) AS DatePart
    FROM [cfg].[ConfigurazioneDatiModuloCommessa] cmd
    INNER JOIN [cfg].[FrameModuloCommessa] fc ON fc.anno = cmd.anno AND fc.mese = cmd.mese
    CROSS APPLY STRING_SPLIT(cmd.archiviato, ';')
    WHERE cmd.archiviato IS NOT NULL
),
cmc AS (
    SELECT FkIdEnte, annoRiferimento, meseRiferimento, frame, framelegale, Source,
        CAST(LEFT(DatePart, CHARINDEX('/', DatePart) - 1) AS INT) AS [Year],
        CAST(RIGHT(DatePart, LEN(DatePart) - CHARINDEX('/', DatePart)) AS INT) AS [Month]
    FROM split
    WHERE CHARINDEX('/', DatePart) > 0
),
frame_parsed AS (
    SELECT *,
        CASE WHEN CHARINDEX('-', frame) > 0 THEN CAST(LEFT(frame, CHARINDEX('-', frame) - 1) AS INT) ELSE 1 END AS giornoInizio,
        CASE WHEN CHARINDEX('-', frame) > 0 THEN CAST(RIGHT(frame, LEN(frame) - CHARINDEX('-', frame)) AS INT) ELSE 31 END AS giornoFine,
        CASE WHEN CHARINDEX('-', framelegale) > 0 THEN CAST(RIGHT(framelegale, LEN(framelegale) - CHARINDEX('-', framelegale)) AS INT) ELSE 31 END AS giornoFineLegale
    FROM cmc
)
SELECT FkIdEnte, annoRiferimento, meseRiferimento,
    CASE WHEN YEAR(GETDATE()) = annoRiferimento AND MONTH(GETDATE()) = meseRiferimento THEN
            CASE WHEN Source = 'obbligatorio' AND DAY(GETDATE()) NOT BETWEEN giornoInizio AND giornoFine
                 THEN 'archiviato' ELSE Source END
         ELSE Source END AS Source,
    [Year], [Month],
    CASE WHEN [Month] BETWEEN 1 AND 3 THEN CAST([year] as nvarchar(5)) + '-Q1'
         WHEN [Month] BETWEEN 4 AND 6 THEN CAST([year] as nvarchar(5)) + '-Q2'
         WHEN [Month] BETWEEN 7 AND 9 THEN CAST([year] as nvarchar(5)) + '-Q3'
         WHEN [Month] BETWEEN 10 AND 12 THEN CAST([year] as nvarchar(5)) + '-Q4'
         ELSE NULL END AS Quarter,
    CASE WHEN giornoFine > DAY(EOMONTH(DATEFROMPARTS(annoRiferimento, meseRiferimento, 1)))
         THEN EOMONTH(DATEFROMPARTS(annoRiferimento, meseRiferimento, 1))
         ELSE DATEFROMPARTS(annoRiferimento, meseRiferimento, giornoFine) END AS datavalidita,
    CASE WHEN giornoFineLegale > DAY(EOMONTH(DATEFROMPARTS(annoRiferimento, meseRiferimento, 1)))
         THEN EOMONTH(DATEFROMPARTS(annoRiferimento, meseRiferimento, 1))
         ELSE DATEFROMPARTS(annoRiferimento, meseRiferimento, giornoFineLegale) END AS datavaliditalegale
FROM frame_parsed;
GO
