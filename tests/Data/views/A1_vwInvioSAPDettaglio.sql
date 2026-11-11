


/*
  Data creazione:        27/10/2026
  Data ultima modifica:  11/11/2026
  Descrizione:           Visualizza il dettaglio delle fatture emesse nella pagina Invio a SAP
  Target utilizzo:       Pagina Invio a SAP
  Versione:              1.1
*/

CREATE OR ALTER VIEW [be].[vwInvioSAPDettaglio]
AS

WITH InviiOrdinati AS
(
    SELECT
        ft.IdFattura,
        ft.FkIdTipoDocumento,
        ft.FkTipologiaFattura,
        ft.FkIdEnte,
        e.[description] AS RagioneSociale,
        ft.DataFattura,
        ft.IdentificativoFattura,
        ft.TotaleFattura,
        ft.AnnoRiferimento,
        ft.MeseRiferimento,
        ft.CausaleFattura,
        ft.CodiceContratto,
        ft.IdDocumento,
        ft.DataDocumento,
        ft.Progressivo,
        ft.FatturaInviata,
        ft.Semestre,
        ft.FaseFatturazione,
        rsap.dataCompletamento AS DataUltimaGenerazione,
        rsap.nomeFile AS NomeFile,
        ROW_NUMBER() OVER
        (
            PARTITION BY ft.IdFattura
            ORDER BY rsap.dataCompletamento DESC, rsap.batchId DESC
        ) AS NumeroRiga
    FROM pfd.FattureTestata AS ft
    INNER JOIN pfd.Enti AS e
        ON e.InternalIstitutionId = ft.FkIdEnte
    LEFT JOIN pfw.DettaglioInvioSAP AS dsap
        ON dsap.FkIdFattura = ft.IdFattura
    LEFT JOIN pfw.RegistroInvioSAP AS rsap
        ON rsap.batchId = dsap.FkBatchId
    WHERE NOT EXISTS                            -- Esclusione delle fatture eliminate/posticipate
    (
        SELECT 1
        FROM cfg.GestioneFatture gf
        WHERE gf.FkIdFattura = ft.IdFattura
            AND gf.Stato IN (0,3)
    )
)
SELECT
    i.IdFattura, i.FkIdTipoDocumento, i.FkTipologiaFattura, i.FkIdEnte,
    i.RagioneSociale, i.DataFattura, i.IdentificativoFattura,
    i.TotaleFattura, i.AnnoRiferimento, i.MeseRiferimento,
    i.CausaleFattura, i.CodiceContratto, i.IdDocumento,
    i.DataDocumento, i.Progressivo, i.FatturaInviata,
    i.Semestre, i.FaseFatturazione, i.DataUltimaGenerazione,
    i.NomeFile,
    (
        SELECT
            d.Nota
        FROM pfw.DettaglioInvioSAP AS d
        WHERE d.FkIdFattura = i.IdFattura
        ORDER BY d.FkBatchId
        FOR JSON PATH, INCLUDE_NULL_VALUES
    ) AS NoteInvio
FROM InviiOrdinati AS i
WHERE i.NumeroRiga = 1

--WHERE ft.AnnoRiferimento = 2024 AND ft.MeseRiferimento = 2 AND FkTipologiaFattura = 'ANTICIPO'
GO
