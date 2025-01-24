using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Response; 

public class FattureMeseResponse
{
    [JsonPropertyOrder(-1)]
    public string? Mese { get; set; }

    [JsonPropertyOrder(-2)]
    public string? Descrizione { get; set; }
} 