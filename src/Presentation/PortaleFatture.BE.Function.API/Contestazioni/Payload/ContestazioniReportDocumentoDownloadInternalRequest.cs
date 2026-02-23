using PortaleFatture.BE.Function.API.Models;

namespace PortaleFatture.BE.Function.API.Contestazioni.Payload;

public sealed  class ContestazioniReportDocumentoDownloadInternalRequest
{ 
    public  long IdReport { get; set; } 
    public  string? TipoReport { get; set; } 
    public Session? Session { get; set; }
} 