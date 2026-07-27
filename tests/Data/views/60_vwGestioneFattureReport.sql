/****** Oggetto: View [be].[vwGestioneFattureReport]    Data dello script 24/07/2026 14:37:36 ******/
-- Script autorevole estratto dal DB reale. CREATE OR ALTER per essere riapplicabile a caldo.
-- NOTA: al 2026-07-24 questa vista NON e' ancora referenziata da nessun builder C#
-- (GestioneFattureQueryBuilder cita solo Griglia/Download/FormAnniMesi/FormPosticipa/FormElimina):
-- esiste nel DB in anticipo rispetto al backend, per lo sheet "fatture posticipate" del report
-- documenti emessi. Aggiornare docs/viste-endpoint.md quando verra' agganciata a una rotta.
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

/*
  Data creazione:        17/07/2026
  Data ultima modifica:  17/07/2026
  Descrizione:           Visualizza tutte le fatture posticipate
  Target utilizzo:       Report documenti emessi, sheet dedicato alla visualizzazione delle fatture posticipate
  Versione:              1.0
*/

CREATE OR ALTER view [be].[vwGestioneFattureReport] as
select
	DISTINCT
	gf.FkIdEnte as [IdEnte],
	e.description as [Ragione Sociale],
	c.onboardingtokenid as [IdContratto],
	gf.FkTipologiaFattura as [TipologiaFattura],
	ft.Progressivo as [NumeroFattura],
	ft.FkIdTipoDocumento as [TipoDocumento],
	gf.Anno as Anno,
	gf.Mese as Mese,
	rt.TotaleNotificheAnalogiche as TotaleNotificheAnalogiche,
	rt.TotaleNotificheDigitali as TotaleNotificheDigitali,
	rt.TotaleNotificheAnalogiche + rt.TotaleNotificheDigitali as TotaleNotifiche,
	rt.TotaleAnalogico as TotaleImponibileAnalogico,
	rt.TotaleDigitale as TotaleImponibileDigitale,
	rt.TotaleAnalogico + rt.TotaleDigitale as TotaleImponibile,
	rt.TotaleAnalogicoIva as [TotaleIvatoAnalogico],
	rt.TotaleDigitaleIva as [TotaleIvatoDigitale],
	rt.TotaleAnalogicoIva + rt.TotaleDigitaleIva as [TotaleIvato],

	CASE
		WHEN rt.Caricata = 1 THEN 'Firmata'
		ELSE 'Non Caricata'
	END as Firmata,
	ft.TotaleFattura as [TotaleFatturaImponibile],
	tc.Descrizione as TipoContratto

from cfg.GestioneFatture gf
	INNER JOIN pfd.enti e ON e.InternalIstitutionId = gf.FkIdEnte
	INNER JOIN pfd.Contratti c ON e.InternalIstitutionId = c.internalistitutionid
	INNER JOIN pfw.TipoContratto tc ON c.FkIdTipoContratto = tc.IdTipoContratto
	LEFT JOIN pfd.RelTestata rt ON
		gf.Anno = rt.[year]
		AND gf.Mese = rt.[month]
		AND gf.FkIdEnte = rt.internal_organization_id
		AND gf.FkTipologiaFattura = rt.TipologiaFattura
	LEFT JOIN pfd.FattureTestata ft ON
		gf.FkIdEnte = ft.FkIdEnte
		AND gf.Anno = ft.AnnoRiferimento
		AND gf.Mese = ft.MeseRiferimento
		AND gf.FkTipologiaFattura = ft.FkTipologiaFattura
	LEFT JOIN pfd.FattureTestata_Eliminate fte ON
		gf.FkIdEnte = fte.FkIdEnte
		AND gf.Anno = fte.AnnoRiferimento
		AND gf.Mese = fte.MeseRiferimento
		AND gf.FkTipologiaFattura = fte.FkTipologiaFattura
where Stato IN (0,3) -- Seleziona solo fatture Posticipate oppure Eliminate
--order by Anno desc, mese desc
GO
