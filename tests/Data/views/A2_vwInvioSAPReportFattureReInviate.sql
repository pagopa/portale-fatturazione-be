


/*
  Data creazione:        28/10/2026
  Data ultima modifica:  28/10/2026
  Descrizione:           Visualizza tutte le fatture re-inviate con relativo batch completato
  Target utilizzo:       Download report fatture re-inviate Pagina Invio a SAP
  Versione:              1.0
*/

CREATE OR ALTER VIEW [be].[vwInvioSAPReportFattureReInviate]
AS

WITH cte_fattureReInviate AS (

	SELECT 
		d.FkIdFattura
		,count(d.FkIdFattura) as ConteggioInvio
	FROM pfw.DettaglioInvioSAP d
		INNER JOIN pfw.RegistroInvioSAP r
			ON r.BatchId = d.FkBatchId
	WHERE 
		r.Stato = 'COMPLETATO'
		AND r.DataCompletamento IS NOT NULL
	GROUP BY d.FkIdFattura
	HAVING count(d.FkIdFattura) > 1
)
, cte_fatture AS (
	SELECT
		ft.IdFattura
		,ft.FkIdTipoDocumento
		,ft.FkTipologiaFattura
		,ft.FkIdEnte
		,e.description AS [RagioneSociale]
		,ft.DataFattura
		,CAST(ft.TotaleFattura AS decimal(10,2)) AS TotaleFattura
		,ft.AnnoRiferimento
		,ft.MeseRiferimento
		,ft.Progressivo
		,fr.ConteggioInvio
		,(
			SELECT
				d.Nota
			FROM pfw.DettaglioInvioSAP AS d
			WHERE d.FkIdFattura = ft.IdFattura
			ORDER BY d.FkBatchId
			FOR JSON PATH, INCLUDE_NULL_VALUES
		) AS Note
	FROM pfd.FattureTestata ft
		INNER JOIN cte_fattureReInviate fr
			ON fr.FkIdFattura = ft.IdFattura
		INNER JOIN pfd.Enti e
			ON e.InternalIstitutionId = ft.FkIdEnte
)
SELECT
		 IdFattura			AS IdFattura
		,FkIdTipoDocumento	AS TipoDocumento
		,FkTipologiaFattura	AS TipologiaFattura
		,FkIdEnte			AS IdEnte
		,RagioneSociale		AS RagioneSociale
		,DataFattura		AS DataFattura
		,TotaleFattura		AS TotaleFattura
		,AnnoRiferimento	AS AnnoRiferimento
		,MeseRiferimento	AS MeseRiferimento
		,Progressivo		AS Progressivo
		,ConteggioInvio		AS ConteggioInvio
		,Note				AS Note
FROM cte_fatture
GO
