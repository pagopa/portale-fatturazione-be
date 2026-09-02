namespace PortaleFatture.BE.Infrastructure.Common.SEND.Banner.Queries.Persistence.Builder;

internal static class BannerSQLBuilder
{

    private static string _sql = @"
SELECT TOP(1)
    cb.[Id],
    cb.[DataInizio],
    cb.[DataFine],
    cb.[Testo],
    cb.[Visibile]
FROM [cfg].[ConfigurazioneBanner] cb
WHERE cb.[Visibile] = 1
ORDER BY cb.[DataInizio] DESC";


    public static string Select()
    {
        return _sql;
    }

}