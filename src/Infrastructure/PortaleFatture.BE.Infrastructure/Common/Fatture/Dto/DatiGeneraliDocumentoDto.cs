using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.Dto;

public class DatiGeneraliDocumentoDto
{
    [JsonPropertyName("tipologia")]
    public string? Tipologia { get; set; }

    [JsonPropertyName("riferimentoNumeroLinea")]
    public string? RiferimentoNumeroLinea { get; set; }

    [JsonPropertyName("idDocumento")]
    public string? IdDocumento { get; set; }

    [JsonPropertyName("data")]
    public string? Data { get; set; } 

    [JsonPropertyName("numItem")]
    public string? NumItem { get; set; }

    [JsonPropertyName("codiceCommessaConvenzione")]
    public string? CodiceCommessaConvenzione { get; set; }

    [JsonPropertyName("CUP")]
    public string? CUP { get; set; }

    [JsonPropertyName("CIG")]
    public string? CIG { get; set; }
}