namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

public static class FattureModuloCommessaExcelBuilder
{
    private static string _sqlCommesseEliminate = @"
SELECT * from
    pfd.vModuloCommessaTotali t 
    inner join 
    pfd.vModuloCommessaParziali p
    ON t.FKIdEnte = p.FKIdEnte
    AND t.AnnoValidita= p.AnnoValidita
    AND t.MeseValidita = p.MeseValidita
    INNER JOIN pfd.Contratti c
    ON c.internalistitutionid = t.FKIdEnte
    inner join
    pfd.vFattureAnticipoEliminate f
    ON f.Anno = t.AnnoValidita
    AND f.IdEnte = t.FKIdEnte 
    AND f.Mese = t.MeseValidita
    WHERE f.Anno = @anno and f.Mese = @mese 
";

    private static string _sqlCommesse = @"
SELECT * 
FROM pfd.vModuloCommessaTotali t 
INNER JOIN pfd.vModuloCommessaParziali p
    ON t.FKIdEnte = p.FKIdEnte
    AND t.AnnoValidita = p.AnnoValidita
    AND t.MeseValidita = p.MeseValidita
INNER JOIN pfd.Contratti c
    ON c.internalistitutionid = t.FKIdEnte
LEFT OUTER JOIN pfd.vFattureAnticipo f
    ON f.Anno = t.AnnoValidita
    AND f.Mese = t.MeseValidita
    AND f.IdEnte = t.FKIdEnte
WHERE t.AnnoValidita = @anno 
    AND t.MeseValidita = @mese 
";

    public static string SelectCommesse()
    {
        return _sqlCommesse;
    }

    public static string SelectCommesseEliminate()
    {
        return _sqlCommesseEliminate;
    }

    private static string _sqlORderBy = @"
 ORDER BY
  CASE
    WHEN f.IdFattura IS NULL THEN 1
    ELSE 0
  END,
  f.IdFattura, Totale desc;";

    public static string OrderBy()
    {
        return _sqlORderBy;
    }
}