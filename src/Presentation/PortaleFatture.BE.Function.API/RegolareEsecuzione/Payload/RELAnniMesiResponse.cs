using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;

public sealed class RELAnniMesiResponse
{
    [JsonPropertyOrder(1)]
    public int? Anno { get; set; }

    [JsonPropertyOrder(-1)]
    public int? Mese { get; set; }

    [JsonPropertyOrder(-2)]
    public string? Descrizione { get; set; }
}