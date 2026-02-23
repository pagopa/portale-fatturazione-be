using PortaleFatture.BE.Function.API.Models;

namespace PortaleFatture.BE.Function.API.Contestazioni.Payload;

public sealed class ContestazioniReportsDocumentInternalRequest
{
    public int? IdReport { get; set; }
    public int? Step { get; set; }
    public  Session? Session { get; set; }
} 