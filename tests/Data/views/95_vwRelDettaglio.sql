/*
  Data creazione:        29/07/2026
  Data ultima modifica:  29/07/2026
  Descrizione:           Visualizza dettaglio delle REL
  Target utilizzo:       PAgina regolari esecuzioni, cllicando sul dettaglio di una REL prende i dati dalla vista, valido per aderenti e admin
  Versione:              1.0
*/
-- DDL reale fornito dal team DB, riprodotta as-is (solo CREATE -> CREATE OR ALTER per l'hot-apply).
-- Letta da RelTestataSQLBuilder.SelectDettaglio() -> RelTestataQueryGetByIdPersistence, che usa
-- SingleAsync: 0 righe o >1 righe diventano un 500, non un 404. Due punti da tenere d'occhio, gia'
-- documentati in docs/viste-endpoint.md e coperti da RelDettaglioHttpTests:
--   1) TotaliCumulati e' in INNER JOIN e nasce dalle tabelle di staging (MesiFatture + TmpFatture*):
--      un periodo storico con lo staging ripulito sparisce dalla vista -> 500;
--   2) il LEFT JOIN su pfw.DatiFatturazione e' solo su FkIdEnte, senza anno/mese/tipologia: un ente
--      con piu' di una riga produce fan-out -> piu' di una riga -> 500.
CREATE OR ALTER VIEW [be].[vwRelDettaglio]
AS
WITH TotaliCumulati AS (
    SELECT
        mf.FkIdEnte,
        mf.AnnoRiferimento,
        mf.MeseRiferimento,
        mf.FKTipologiaFattura,
        ISNULL(SUM(CASE WHEN tfr.CodiceMateriale = 'STORNO 50% ANT.NA sospeso' OR
                             tfr.CodiceMateriale = 'STORNO ANTICIPO NA sospeso' OR
                             tfr.CodiceMateriale = 'STORNO 50% ANT.NA' OR
                             tfr.CodiceMateriale = 'STORNO ANTICIPO NA' THEN tfr.Imponibile END), 0) AS StornoAnticipoAnalogico,
        ISNULL(SUM(CASE WHEN tfr.CodiceMateriale = 'STORNO 50% ANT.ND sospeso' OR
                             tfr.CodiceMateriale = 'STORNO ANTICIPO ND sospeso' OR
                             tfr.CodiceMateriale = 'STORNO 50% ANT.ND' OR
                             tfr.CodiceMateriale = 'STORNO ANTICIPO ND' THEN tfr.Imponibile END), 0) AS StornoAnticipoDigitale,
        ISNULL(SUM(CASE WHEN tfr.CodiceMateriale = 'STORNO ACCONTO NA sospeso' OR
                             tfr.CodiceMateriale = 'STORNO ACCONTO NA' THEN tfr.Imponibile END), 0) AS StornoAccontoAnalogico,
        ISNULL(SUM(CASE WHEN tfr.CodiceMateriale = 'STORNO ACCONTO ND sospeso' OR
                             tfr.CodiceMateriale = 'STORNO ACCONTO ND' THEN tfr.Imponibile END), 0) AS StornoAccontoDigitale
    FROM pfd.MesiFatture AS mf
    INNER JOIN pfd.TmpFattureTestata AS tft ON mf.FkIdFatturaTmp = tft.IdFattura
    INNER JOIN pfd.TmpFattureRighe AS tfr ON tft.IdFattura = tfr.FkIdFattura
    WHERE mf.FKTipologiaFattura IN ('PRIMO SALDO', 'VAR. SEMESTRALE', 'SECONDO SALDO', 'SEM. SOSPESI')
    GROUP BY mf.FkIdEnte, mf.AnnoRiferimento, mf.MeseRiferimento, mf.FKTipologiaFattura
)
SELECT
    t.internal_organization_id AS IdEnte,
    e.description AS RagioneSociale,
    t.contract_id AS IdContratto,
    t.TipologiaFattura,
    t.year AS anno,
    t.month AS mese,
    t.TotaleAnalogico,
    t.TotaleDigitale,
    t.TotaleNotificheAnalogiche,
    t.TotaleNotificheDigitali,
    tc.StornoAnticipoAnalogico * -1 AS Anticipo_StornoAnalogico,
    tc.StornoAnticipoDigitale * -1 AS Anticipo_StornoDigitale,
    tc.StornoAccontoAnalogico * -1 AS Acconto_StornoAnalogico,
    tc.StornoAccontoDigitale * -1 AS Acconto_StornoDigitale,
    (tc.StornoAnticipoAnalogico + tc.StornoAnticipoDigitale) * -1 AS Anticipo_StornoTotale,
    (tc.StornoAccontoAnalogico + tc.StornoAccontoDigitale) * -1 AS Acconto_StornoTotale,
    (tc.StornoAnticipoAnalogico + tc.StornoAnticipoDigitale + tc.StornoAccontoAnalogico + tc.StornoAccontoDigitale) * -1 AS StornoTotale,
    t.Iva,
    t.TotaleAnalogicoIva,
    t.TotaleDigitaleIva,
    t.TotaleIva,
    t.AsseverazioneTotaleAnalogico,
    t.AsseverazioneTotaleDigitale,
    t.AsseverazioneTotaleNotificheAnalogiche,
    t.AsseverazioneTotaleNotificheDigitali,
    t.AsseverazioneTotale,
    t.AsseverazioneTotaleAnalogicoIva,
    t.AsseverazioneTotaleDigitaleIva,
    t.AsseverazioneTotaleIva,
    f.IdDocumento,
    f.Cup,
    f.DataDocumento,
    CASE WHEN f.IdDatiFatturazione IS NULL THEN 0 ELSE 1 END AS DatiFatturazione,
    t.Caricata,
    t.Totale
FROM (
    SELECT DISTINCT FkIdEnte, AnnoRiferimento, MeseRiferimento, FKTipologiaFattura
    FROM pfd.MesiFatture) AS mf_1
LEFT OUTER JOIN pfd.RelTestata AS t
    ON mf_1.FkIdEnte = t.internal_organization_id
    AND mf_1.AnnoRiferimento = t.year
    AND mf_1.MeseRiferimento = t.month
    AND mf_1.FKTipologiaFattura = t.TipologiaFattura
INNER JOIN pfd.Enti AS e
    ON e.InternalIstitutionId = t.internal_organization_id
LEFT OUTER JOIN pfw.DatiFatturazione AS f
    ON f.FkIdEnte = t.internal_organization_id
INNER JOIN TotaliCumulati AS tc
    ON tc.FkIdEnte = mf_1.FkIdEnte
    AND tc.AnnoRiferimento = mf_1.AnnoRiferimento
    AND tc.MeseRiferimento = mf_1.MeseRiferimento
    AND tc.FKTipologiaFattura = mf_1.FKTipologiaFattura
GO
