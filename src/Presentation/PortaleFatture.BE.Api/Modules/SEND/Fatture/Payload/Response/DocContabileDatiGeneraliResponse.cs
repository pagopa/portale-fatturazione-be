using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Response;


public class DocContabileDatiGeneraliResponse
{
    [JsonPropertyName("tipologia")]
    public string? TipologiaFattura { get; set; }

    [JsonPropertyName("riferimentoNumeroLinea")]
    public string? RiferimentoNumeroLinea { get; set; } = string.Empty;

    [JsonPropertyName("idDocumento")]
    public string? IdDocumento { get; set; }

    [JsonPropertyName("data")]
    public string? DataDocumento { get; set; }

    [JsonPropertyName("numItem")]
    public string? NumItem { get; set; }

    [JsonPropertyName("codiceCommessaConvenzione")]
    public string? CodiceCommessaConvenzione { get; set; }

    [JsonPropertyName("CUP")]
    public string? Cup { get; set; }

    [JsonPropertyName("CIG")]
    public string? Cig { get; set; }
}