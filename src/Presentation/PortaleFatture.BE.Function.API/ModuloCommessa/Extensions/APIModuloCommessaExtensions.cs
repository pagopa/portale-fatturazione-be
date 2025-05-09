using System.Reflection.Metadata.Ecma335;
using PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Payload.Request;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;

namespace PortaleFatture.BE.Function.API.ModuloCommessa.Extensions;

public static class APIModuloCommessaExtensions
{

    public static DatiModuloCommessaCreateRequest Map(this DatiModuloCommessaCreateInternalRequest model)
    {
        return new DatiModuloCommessaCreateRequest()
        {
            ModuliCommessa = model.ModuliCommessa,
        };
    }
} 