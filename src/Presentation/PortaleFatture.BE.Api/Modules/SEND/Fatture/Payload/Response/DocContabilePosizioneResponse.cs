using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Response;

public class DocContabilePosizioneResponse
{
    [JsonPropertyName("numeroLinea")]
    public int NumeroLinea { get; set; }

    [JsonPropertyName("testo")]
    public string? Testo { get; set; }

    [JsonPropertyName("codiceMateriale")]
    public string? CodiceMateriale { get; set; }

    [JsonPropertyName("quantita")]
    public int Quantita { get; set; }

    [JsonPropertyName("prezzoUnitario")]
    public decimal PrezzoUnitario { get; set; }

    [JsonPropertyName("imponibile")]
    public decimal Imponibile { get; set; }

    [JsonPropertyName("periodoRiferimento")]
    public string? PeriodoRiferimento { get; set; }
}