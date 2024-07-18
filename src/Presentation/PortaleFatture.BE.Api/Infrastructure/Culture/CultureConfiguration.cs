using System.Globalization;

namespace PortaleFatture.BE.Api.Infrastructure.Culture;

public static class CultureConfiguration
{
    private static void SetCulture(string cultureName)
    {
        SetCulture(new CultureInfo(cultureName));
    }

    private static void SetCulture(CultureInfo culture)
    {
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    public static WebApplication UseCulture(this WebApplication application, string defaultCulture = "it-IT")
    {
        SetCulture(defaultCulture);
        return application;
    }
}