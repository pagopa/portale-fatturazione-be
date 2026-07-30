-- be.vwGestioneFattureFormElimina: fatture eliminabili (ANTICIPO/ACCONTO non inviate, dall'anno prec.,
-- + eccezione INPS PRIMO SALDO). NOTA: come FormPosticipa, il builder che la usa NON e' richiamato
-- da alcuna persistence -> vista non usata dai test attuali; qui per completezza.
CREATE OR ALTER VIEW [be].[vwGestioneFattureFormElimina] AS
SELECT DISTINCT AnnoRiferimento, MeseRiferimento, FkIdEnte, FkTipologiaFattura, IdFattura
FROM pfd.FattureTestata ft
WHERE (ft.FkTipologiaFattura IN ('ANTICIPO','ACCONTO')
       OR (ft.FkIdEnte = '53b40136-65f2-424b-acfb-7fae17e35c60' AND ft.FkTipologiaFattura = 'PRIMO SALDO'))
  AND (ft.FatturaInviata IS NULL OR ft.FatturaInviata = 0)
  AND AnnoRiferimento >= DATEPART(YEAR, GETDATE()) - 1;
