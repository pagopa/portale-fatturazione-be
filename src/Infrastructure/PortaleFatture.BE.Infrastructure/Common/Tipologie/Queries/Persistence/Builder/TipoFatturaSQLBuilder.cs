using Dapper;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Core.Entities.Tipologie;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence.Builder;

public static class TipoFatturaSQLBuilder
{
    private static string _sql = @"
SELECT distinct
      [FkTipologiaFattura] 
      FROM [pfd].[FattureTestata]
WHERE [AnnoRiferimento]=@anno AND [MeseRiferimento]=@mese
ORDER BY [FkTipologiaFattura] 
";
    public static string SelectAll()
    {
        return _sql;
    }
}