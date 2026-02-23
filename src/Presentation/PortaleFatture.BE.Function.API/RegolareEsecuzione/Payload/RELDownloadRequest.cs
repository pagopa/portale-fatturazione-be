using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;

public class RELDownloadRequest
{
    [JsonPropertyName("idTestata")]
    public string? IdTestata { get; set; }

    [JsonPropertyName("tipoDocumentoREL")]
    public string? TipoDocumentoREL { get; set; }  
} 