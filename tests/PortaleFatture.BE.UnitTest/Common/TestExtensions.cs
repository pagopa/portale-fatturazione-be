using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.UnitTest.Common;

public static class TestExtensions
{
    public static long GetRandomId()
    {
        var rand = new Random();
        return rand.Next(1, int.MaxValue);
    }

    public static string GetRandomIdEnte()
    {
        return Guid.NewGuid().ToString();
    }

    public static AuthenticationInfo GetAuthInfo(string? idEnte, string? prodotto)
    {
        return new AuthenticationInfo()
        {
            IdEnte = idEnte ?? Guid.NewGuid().ToString(),
            Prodotto = prodotto ?? "prod-pn"
        };
    }
}