
/*
  Data creazione:        27/10/2026
  Data ultima modifica:  27/10/2026
  Descrizione:           Visualizza i gruppi delle fatture emesse nella pagina Invio a SAP
  Target utilizzo:       Pagina Invio a SAP
  Versione:              1.0
*/

CREATE OR ALTER VIEW [be].[vwInvioSAPGriglia]
AS

WITH cte_fattureEmesse AS (
	SELECT
		 ft.[FkTipologiaFattura]	AS TipologiaFattura
		,ft.FatturaInviata			AS StatoFattura		-- TODO utilizzare campo stato corretto
		,ft.AnnoRiferimento			AS Anno
		,ft.MeseRiferimento			AS Mese
		,ft.TotaleFattura			AS TotaleFattura
	FROM pfd.FattureTestata ft

)
SELECT 
	 TipologiaFattura		AS [TipologiaFattura]
	,StatoFattura			AS [StatoFattura]
	,COUNT(*)				AS [NumeroFatture]
	,Anno					AS [Anno]
	,Mese					AS [Mese]
	,SUM(TotaleFattura)		AS [Importo Totale]
FROM cte_fattureEmesse
GROUP BY TipologiaFattura, StatoFattura, Anno, Mese

--WHERE ft.AnnoRiferimento = 2024 AND ft.MeseRiferimento = 2 AND FkTipologiaFattura = 'ANTICIPO'
GO
