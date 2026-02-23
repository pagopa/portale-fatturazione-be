using System.Text.Json.Serialization;
using PortaleFatture.BE.Function.API.Models;

namespace PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;

public class RELDownloadInternalRequest
{
    [JsonPropertyName("idTestata")]
    public string? IdTestata { get; set; }

    [JsonPropertyName("typeDocument")]
    public string? TypeDocument { get; set; } 

    public Session? Session { get; set; }
}