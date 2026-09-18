/*
  Data creazione:        15/09/2026
  Data ultima modifica:  15/09/2026
  Descrizione:           Destinatari PEC della Regolare Esecuzione: una riga per testata REL, con
                         la PEC risolta a cascata fra tre sorgenti possibili.
  Target utilizzo:       Letta da EmailRelService (Azure Function SendEmail) per sapere a chi
                         inviare la notifica REL. Riprodotta nel DB seedato dei test.
  Versione:              1.0
*/

-- Copia della vista di produzione. Le sue quattro sorgenti devono esistere PRIMA: RelTestata,
-- DatiFatturazione ed Enti arrivano dal seed, RelPecEmail da email_rel.sql — che nell'entrypoint
-- gira prima di questo blocco, perche' per le viste SQL Server non fa deferred name resolution.
--
-- Da sapere leggendo la colonna pec: e' una cascata di ISNULL, quindi la sorgente che vince non e'
-- ovvia — pfw.DatiFatturazione batte pfd.RelPecEmail, che batte pfd.Enti.digitalAddress. Se tutte
-- e tre sono vuote la PEC e' NULL e la function salta l'ente in silenzio (if (ente.Pec != null)).
--
-- E il LEFT JOIN su pfw.DatiFatturazione e' solo su FkIdEnte, senza anno/mese/tipologia: un ente
-- con piu' righe li' dentro produce FAN-OUT, cioe' la stessa REL duplicata e piu' PEC inviate.
-- E' la stessa trappola gia' annotata su be.vwRelDettaglio.
CREATE OR ALTER VIEW [pfd].[EmailRel]
AS
SELECT r.internal_organization_id, r.contract_id, r.TipologiaFattura, r.year, r.month,
       ISNULL(ISNULL(f.PEC, em.pec), e.digitalAddress) AS pec,
       e.description AS RagioneSociale, r.Totale, r.FlagConguaglio
FROM     pfd.RelTestata AS r LEFT OUTER JOIN
                  pfw.DatiFatturazione AS f ON f.FkIdEnte = r.internal_organization_id LEFT OUTER JOIN
                  pfd.Enti AS e ON r.internal_organization_id = e.InternalIstitutionId LEFT OUTER JOIN
                  pfd.RelPecEmail AS em ON em.FkIdEnte = r.internal_organization_id
GO
