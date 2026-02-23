using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Function.API.Contestazioni.Payload;

public  class CalendarioContestazioniStatoResponse
{
    [JsonPropertyName("dataFine")]
    public DateTime? DataFine { get; set; }

    [JsonPropertyName("dataInizio")]
    public DateTime? DataInizio { get; set; }

    [JsonPropertyName("chiusuraContestazioni")]
    public DateTime? ChiusuraContestazioni { get; set; }

    [JsonPropertyName("tempoRisposta")]
    public DateTime? TempoRisposta { get; set; }

    [JsonPropertyName("meseContestazione")]
    public int? MeseContestazione { get; set; } 

    [JsonPropertyName("descrizioneMeseContestazione")]
    public string? DescrizioneMeseContestazione { get; set; }

    [JsonPropertyName("annoContestazione")]
    public int AnnoContestazione { get; set; }

    [JsonIgnore]
    public DateTime? DataRecapitistaFine { get; set; }

    [JsonIgnore]
    public DateTime? DataRecapitistaInizio { get; set; }
}
 
public class FlagConsentiti
{
    [JsonPropertyName("periodoContestazione")]
    public PeriodoFlag PeriodoContestazione { get; set; } = new();

    [JsonPropertyName("periodoRisposta")]
    public PeriodoFlag PeriodoRisposta { get; set; } = new();

    [JsonPropertyName("periodoChiuso")]
    public PeriodoFlag PeriodoChiuso { get; set; } = new();
}

public class PeriodoFlag
{
    [JsonPropertyName("periodo")]
    public string Periodo { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("flagStatoPermessi")]
    public int[] FlagStatoPermessi { get; set; } = Array.Empty<int>();

    [JsonPropertyName("descrizione")]
    public string Descrizione { get; set; } = string.Empty;
}

public class StatoFlag
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("flag")]
    public string Flag { get; set; } = string.Empty;

    [JsonPropertyName("descrizione")]
    public string Descrizione { get; set; } = string.Empty;
}

public class CalendarioContestazioniExtendedResponse : CalendarioContestazioniStatoResponse
{
    [JsonPropertyName("flagConsentiti")]
    public FlagConsentiti FlagConsentiti { get; set; } = new();

    [JsonPropertyName("statiFlag")]
    public StatoFlag[] StatiFlag { get; set; } = [];
} 