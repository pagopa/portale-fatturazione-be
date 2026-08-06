-- Vista legacy pfd.v*. DDL reale fornita dal team DB, riprodotta as-is (CREATE -> CREATE OR ALTER).
-- ⚠️ Parte dalle DIGITALI (CTE 'digitali', INNER JOIN su FkidTipoSpedizione='3'): un ente senza la
-- riga digitale non compare, anche se ha AR e 890. AR/890/totali entrano poi in LEFT JOIN.
CREATE OR ALTER VIEW [pfd].[vModuliCommessa] AS
WITH digitali (FkidEnte, description, vatnumber, FkProdotto, TipoSpedizione, NumeroNotificheNazionali, NumeroNotificheInternazionali, Anno, Mese, AnnoMese, FKIdTipoContratto, Stato) AS (
    SELECT [pfw].[DatiModuloCommessa].FkidEnte, [pfd].[Enti].description, [pfd].[Enti].vatnumber,
        [pfw].[DatiModuloCommessa].FkProdotto, 'digitale',
        [pfw].[DatiModuloCommessa].NumeroNotificheNazionali, [pfw].[DatiModuloCommessa].NumeroNotificheInternazionali,
        [pfw].[DatiModuloCommessa].AnnoValidita, [pfw].[DatiModuloCommessa].[MeseValidita],
        CAST([pfw].[DatiModuloCommessa].AnnoValidita AS varchar) + CAST([pfw].[DatiModuloCommessa].[MeseValidita] as varchar),
        [pfw].DatiModuloCommessa.FKIdTipoContratto, [pfw].DatiModuloCommessa.FkIdStato
    FROM [pfw].[DatiModuloCommessa]
    INNER JOIN [pfd].[Enti] ON [pfw].[DatiModuloCommessa].FkidEnte = [pfd].[Enti].internalistitutionid
        AND [pfw].[DatiModuloCommessa].FkidTipoSpedizione = '3'
),
raccomandate (FkidEnte, description, vatnumber, FkProdotto, TipoSpedizione, NumeroNotificheNazionali, NumeroNotificheInternazionali, Anno, Mese, AnnoMese) AS (
    SELECT [pfw].[DatiModuloCommessa].FkidEnte, [pfd].[Enti].description, [pfd].[Enti].vatnumber,
        [pfw].[DatiModuloCommessa].FkProdotto, 'analogico AR',
        [pfw].[DatiModuloCommessa].NumeroNotificheNazionali, [pfw].[DatiModuloCommessa].NumeroNotificheInternazionali,
        [pfw].[DatiModuloCommessa].AnnoValidita, [pfw].[DatiModuloCommessa].[MeseValidita],
        CAST([pfw].[DatiModuloCommessa].AnnoValidita AS varchar) + CAST([pfw].[DatiModuloCommessa].[MeseValidita] as varchar)
    FROM [pfw].[DatiModuloCommessa]
    INNER JOIN [pfd].[Enti] ON [pfw].[DatiModuloCommessa].FkidEnte = [pfd].[Enti].internalistitutionid
        AND [pfw].[DatiModuloCommessa].FkidTipoSpedizione = '1'
),
raccomandate890 (FkidEnte, description, vatnumber, FkProdotto, TipoSpedizione, NumeroNotificheNazionali, NumeroNotificheInternazionali, Anno, Mese, AnnoMese) AS (
    SELECT [pfw].[DatiModuloCommessa].FkidEnte, [pfd].[Enti].description, [pfd].[Enti].vatnumber,
        [pfw].[DatiModuloCommessa].FkProdotto, 'analogico 890',
        [pfw].[DatiModuloCommessa].NumeroNotificheNazionali, [pfw].[DatiModuloCommessa].NumeroNotificheInternazionali,
        [pfw].[DatiModuloCommessa].AnnoValidita, [pfw].[DatiModuloCommessa].[MeseValidita],
        CAST([pfw].[DatiModuloCommessa].AnnoValidita AS varchar) + CAST([pfw].[DatiModuloCommessa].[MeseValidita] as varchar)
    FROM [pfw].[DatiModuloCommessa]
    INNER JOIN [pfd].[Enti] ON [pfw].[DatiModuloCommessa].FkidEnte = [pfd].[Enti].internalistitutionid
        AND [pfw].[DatiModuloCommessa].FkidTipoSpedizione = '2'
),
TotaliEconomici (FkidEnte, CategoriaSpedizione, TotaleCategoria, Anno, Mese, Totale, IdTipoContratto, AnnoMese) AS (
    SELECT [FkIdEnte], [FkIdCategoriaSpedizione], [TotaleCategoria], AnnoValidita, [MeseValidita], [Totale], [FkIdTipoContratto],
        CAST(AnnoValidita as varchar) + CAST(MeseValidita as varchar)
    FROM pfw.DatiModuloCommessaTotali
)
SELECT d.FkidEnte as 'IdEnte'
    , d.description as 'RagioneSociale'
    , d.vatnumber as 'CodiceFiscale'
    , d.FkProdotto as 'Prodotto'
    , d.TipoSpedizione as 'TipoSpedizioneDigitale'
    , d.NumeroNotificheNazionali as 'NumeroNotificheNazionaliDigitale'
    , d.NumeroNotificheInternazionali as 'NumeroNotificheInternazionaliDigitale'
    , ISNULL(r.TipoSpedizione, 'analogico AR') as 'TipoSpedizioneAnalogicoAR'
    , ISNULL(r.NumeroNotificheNazionali, 0) as 'NumeroNotificheNazionaliAnalogicoAR'
    , ISNULL(r.NumeroNotificheInternazionali, 0) as 'NumeroNotificheInternazionaliAnalogicoAR'
    , ISNULL(rr.TipoSpedizione, 'analogico 890') as 'TipoSpedizioneAnalogico890'
    , ISNULL(rr.NumeroNotificheNazionali, 0) as 'NumeroNotificheNazionaliAnalogico890'
    , ISNULL(rr.NumeroNotificheInternazionali, 0) as 'NumeroNotificheInternazionaliAnalogico890'
    , ISNULL(te.TotaleCategoria, 0) as 'TotaleCategoriaAnalogico'
    , ISNULL(te2.TotaleCategoria, 0) as 'TotaleCategoriaDigitale'
    , d.Anno as 'Anno'
    , d.Mese As 'Mese'
    , ISNULL(te.Totale, 0) as 'TotaleAnalogicoLordo'
    , ISNULL(te2.Totale, 0) as 'TotaleDigitaleLordo'
    , ISNULL(te.Totale, 0) + ISNULL(te2.Totale, 0) as 'TotaleLordo'
    , d.FKIdTipoContratto as 'IdTipoContratto'
    , d.Stato
FROM digitali d
left JOIN raccomandate r on d.FkidEnte = r.FkidEnte AND d.AnnoMese = r.AnnoMese
left JOIN raccomandate890 rr on d.FkidEnte = rr.FkidEnte AND rr.AnnoMese = d.AnnoMese
left JOIN TotaliEconomici te on d.FkidEnte = te.FkidEnte and te.CategoriaSpedizione = 1 and d.AnnoMese = te.AnnoMese
left JOIN TotaliEconomici te2 on d.FkidEnte = te2.FkidEnte and te2.CategoriaSpedizione = 2 and d.AnnoMese = te2.AnnoMese;
GO
