using System.Reflection.Metadata;
using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries;

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

    public static AuthenticationInfo GetAuthInfo(string? idEnte, string? prodotto, string? ruolo = Ruolo.ADMIN, long? idTipoContratto = 1)
    {
        return new AuthenticationInfo()
        {
            IdEnte = idEnte ?? Guid.NewGuid().ToString(),
            Prodotto = prodotto ?? "prod-pn",
            Ruolo = ruolo,
            IdTipoContratto = idTipoContratto
        };
    } 
}