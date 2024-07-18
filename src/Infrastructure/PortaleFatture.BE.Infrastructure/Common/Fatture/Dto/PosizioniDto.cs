using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.Dto;

public class PosizioniDto
{
    [JsonPropertyName("numerolinea")]
    public int NumeroLinea { get; set; }

    [JsonPropertyName("testo")]
    public string? Testo { get; set; }

    [JsonPropertyName("codiceMateriale")]
    public string? CodiceMateriale { get; set; }

    [JsonPropertyName("quantita")]
    public int? Quantita { get; set; }

    [JsonPropertyName("prezzoUnitario")]
    public double PrezzoUnitario { get; set; }

    [JsonPropertyName("imponibile")]
    public double Imponibile { get; set; }
}