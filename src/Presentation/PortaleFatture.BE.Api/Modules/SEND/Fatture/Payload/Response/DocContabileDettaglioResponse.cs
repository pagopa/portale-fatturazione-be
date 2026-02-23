using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Response;

public class DocContabileDettaglioResponse
{
    [JsonPropertyName("fattura")]
    public DocContabileFatturaResponse? Fattura { get; set; }
}