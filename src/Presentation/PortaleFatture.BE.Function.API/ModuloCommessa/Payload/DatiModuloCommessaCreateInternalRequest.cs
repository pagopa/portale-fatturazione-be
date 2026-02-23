using PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Payload.Request;
using PortaleFatture.BE.Function.API.Models;

namespace PortaleFatture.BE.Function.API.ModuloCommessa.Payload;

public class DatiModuloCommessaCreateInternalRequest
{
    public  DatiModuloCommessaCreateRequest? DatiModuloCommessaCreate { get; set; }
    public Session? Session { get; set; }
}