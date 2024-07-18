﻿namespace PortaleFatture.BE.Infrastructure.Common.Fatture.Queries.Persistence.Builder;

public static class FattureModuloCommessaExcelBuilder
{
    private static string _sqlCommesse = @"
SELECT * from
pfd.vModuloCommessaTotali t 
inner join 
pfd.vModuloCommessaParziali p
ON t.FKIdEnte = p.FKIdEnte
AND t.AnnoValidita= p.AnnoValidita
AND t.MeseValidita = p.MeseValidita
left outer join
pfd.vFattureAnticipo f
ON f.Anno = t.AnnoValidita
AND f.Mese = t.MeseValidita
AND f.IdEnte = t.FKIdEnte
"; 

    public static string SelectCommesse()
    {
        return _sqlCommesse;
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
        return _sqlCommesse;
    }
} 