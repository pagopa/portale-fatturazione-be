


/*
  Data creazione:        27/10/2026
  Data ultima modifica:  27/10/2026
  Descrizione:           Visualizza il dettaglio della fatture emesse nella pagina Invio a SAP
  Target utilizzo:       Pagina Invio a SAP
  Versione:              1.0
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
    INNER JOIN pfw.DettaglioInvioSAP AS dsap
        ON dsap.FkIdFattura = ft.IdFattura
    INNER JOIN pfw.RegistroInvioSAP AS rsap
        ON rsap.batchId = dsap.FkBatchId
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
