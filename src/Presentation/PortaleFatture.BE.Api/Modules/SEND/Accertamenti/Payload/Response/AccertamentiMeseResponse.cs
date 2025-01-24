using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Api.Modules.SEND.Accertamenti.Payload.Response;

public sealed class AccertamentiMeseResponse
{
    [JsonPropertyOrder(-1)]
    public string? Mese { get; set; }

    [JsonPropertyOrder(-2)]
    public string? Descrizione { get; set; }
}