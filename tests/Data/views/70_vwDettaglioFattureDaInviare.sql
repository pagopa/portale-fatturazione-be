/*
  Data creazione:        29/07/2026
  Data ultima modifica:  29/07/2026
  Descrizione:           Visualizzazione delle fatture da inviare o in elaborazione
  Target utilizzo:       Pagina /send/inviofatture elenco e dettaglio delle fatture da inviare
  Versione:              1.0
  Nota seed: CREATE OR ALTER (idempotente). Esclude ente PagoPA e le fatture gia' in cfg.GestioneFatture
             (posticipate/eliminate). Vista predisposta per uso futuro.
*/
CREATE OR ALTER VIEW [be].[vwDettaglioFattureDaInviare] AS

SELECT
    [IdFattura] as IdFattura,
    [FkProdotto],
    f.[FkTipologiaFattura] as TipologiaFattura,
    f.[FkIdEnte] as IdEnte,
	e.description as RagioneSociale,
    [DataFattura] as DataFattura,
    [TotaleFattura] as Importo,
    [AnnoRiferimento],
    [MeseRiferimento]
 FROM [pfd].[FattureTestata] f
    INNER JOIN pfd.Enti e
    ON e.InternalIstitutionId = f.FkIdEnte
    LEFT JOIN cfg.GestioneFatture gf
    ON gf.Anno = f.AnnoRiferimento
        AND gf.Mese = f.MeseRiferimento
        AND gf.FkIdEnte = f.FkIdEnte
        AND gf.FkTipologiaFattura = f.FkTipologiaFattura
WHERE (fatturainviata = 0 OR fatturainviata IS NULL)            -- Fatture Non Inviate o In Elaborazione
    AND f.FkIdEnte <> '4a4149af-172e-4950-9cc8-63ccc9a6d865'    -- Esclusione di ente PagoPA
    AND gf.FkIdEnte IS NULL                                     -- Esclusione delle fatture Posticipate presenti in GestioneFatture
GO
