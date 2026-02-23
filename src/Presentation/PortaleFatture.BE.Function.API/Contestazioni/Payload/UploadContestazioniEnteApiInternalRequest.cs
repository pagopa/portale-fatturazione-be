using PortaleFatture.BE.Function.API.Models;

namespace PortaleFatture.BE.Function.API.Contestazioni.Payload;

public sealed class UploadContestazioniEnteApiInternalRequest
{
    public int? Anno { get; set; }
    public int? Mese { get; set; } 
    public Session? Session { get; set; }
} 