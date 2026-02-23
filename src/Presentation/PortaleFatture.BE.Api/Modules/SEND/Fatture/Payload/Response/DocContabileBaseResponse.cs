using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Response;

public abstract class DocContabileBaseResponse
{
    [JsonPropertyOrder(-2)]
    [JsonPropertyName("dettagli")]
    public IEnumerable<DocContabileDettaglioResponse>? Dettagli { get; set; }
}

