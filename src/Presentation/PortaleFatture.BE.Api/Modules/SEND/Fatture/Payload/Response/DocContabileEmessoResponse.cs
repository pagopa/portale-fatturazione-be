using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Response;

public class DocContabileEmessoResponse : DocContabileBaseResponse
{
    [JsonPropertyOrder(-1)]
    [JsonPropertyName("importo")]
    public decimal? Totale { get; set; }
}

