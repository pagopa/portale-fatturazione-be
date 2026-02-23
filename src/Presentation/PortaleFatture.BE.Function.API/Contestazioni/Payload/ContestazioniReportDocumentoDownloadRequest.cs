using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Function.API.Contestazioni.Payload;

public sealed  class ContestazioniReportDocumentoDownloadRequest
{
    [JsonPropertyName("idReport")]
    public  long IdReport { get; set; }

    [JsonPropertyName("tipoReport")]
    public  string? TipoReport { get; set; }
} 