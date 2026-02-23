using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;

public class RELTestataRicercaExternalRequest
{
    [JsonPropertyName("anno")]
    public int? Anno { get; set; }

    [JsonPropertyName("mese")]
    public int? Mese { get; set; }

    [JsonPropertyName("tipologiaFattura")]
    public string? TipologiaFattura { get; set; }

    //[JsonPropertyName("caricata")]
    //public byte? Caricata { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 10;
} 