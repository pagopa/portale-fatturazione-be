-- be.vwGestioneFattureFormPosticipa: fatture posticipabili (SALDO non inviate, dall'anno precedente).
-- NOTA: nel backend il builder che la usa (SelectGestioneFattureAllFilterPosticipa) NON e' richiamato
-- da alcuna persistence -> vista non usata dai test attuali; qui per completezza del seeded DB.
CREATE OR ALTER VIEW [be].[vwGestioneFattureFormPosticipa] AS
SELECT DISTINCT AnnoRiferimento, MeseRiferimento, FkIdEnte, FkTipologiaFattura, IdFattura
FROM pfd.FattureTestata ft
WHERE ft.FkTipologiaFattura IN ('PRIMO SALDO','SECONDO SALDO','VAR. SEMESTRALE','SEM. SOSPESI')
  AND (ft.FatturaInviata IS NULL OR ft.FatturaInviata = 0)
  AND AnnoRiferimento >= DATEPART(YEAR, GETDATE()) - 1;
