using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Response;

public sealed class FatturePeriodoReponse
{
    [JsonPropertyName("anno")] 
    [JsonPropertyOrder(1)]
    public int Anno { get; set; }

    [JsonPropertyOrder(2)]
    [JsonPropertyName("mese")]
    public int Mese { get; set; }


    [JsonPropertyOrder(1)]
    [JsonPropertyName("tipologiaFattura")]
    public string? TipologiaFattura { get; set; }

    [JsonPropertyOrder(1)]
    [JsonPropertyName("dataFattura")]
    public DateOnly? DataFattura { get; set; }
}