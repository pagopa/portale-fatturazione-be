using PortaleFatture.BE.Function.API.Models;

namespace PortaleFatture.BE.Function.API.Contestazioni.Payload;

public sealed class ContestazioniRecapEnteApiInternalRequest
{
    public int? Anno { get; set; } 
    public int? Mese { get; set; } 
    public Session? Session { get; set; }
} 