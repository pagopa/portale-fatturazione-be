using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Response;

public class DocContabileSospesoResponse : DocContabileBaseResponse
{
    [JsonPropertyOrder(-1)]
    [JsonPropertyName("importoSospeso")]
    public decimal? ImportoSospeso { get; set; }
}

