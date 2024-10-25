namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

public static class FattureAccontoFatturaExcelBuilder
{
    private static string _sqlAcconto = @"
SELECT f.*, n.*
  FROM [pfd].[vFattureAcconto] f
left outer join pfd.NotificheCount n
ON n.internal_organization_id = f.IdEnte
AND n.year = f.Anno and n.month = f.Mese
";

    public static string SelectAcconto()
    {
        return _sqlAcconto;
    }
}