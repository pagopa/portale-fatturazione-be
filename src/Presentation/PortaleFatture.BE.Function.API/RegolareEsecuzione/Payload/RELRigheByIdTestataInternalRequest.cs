using PortaleFatture.BE.Function.API.Models;

namespace PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;

public class RELRigheByIdTestataInternalRequest
{
    public string? IdTestata { get; set; }
    public Session? Session { get; set; }
}