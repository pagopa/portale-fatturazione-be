using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Api.Modules.SEND.DatiRel.Payload.Response;

public class RelMeseResponse
{
    [JsonPropertyOrder(-1)]
    public string? Mese { get; set; }

    [JsonPropertyOrder(-2)]
    public string? Descrizione { get; set; }
}