using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.Dto;

public class TitoloFatturaDto
{
    [JsonPropertyName("totale")]
    public decimal Totale { get; set; }

    [JsonPropertyName("numero")]
    public long Numero { get; set; } 

    [JsonPropertyName("idfattura")]
    public long IdFattura { get; set; }

    [JsonPropertyName("dataFattura")]   
    public string? DataFattura { get; set; }

    [JsonPropertyName("prodotto")]
    public string? Prodotto { get; set; }

    [JsonPropertyName("identificativo")]
    public string? Identificativo { get; set; }

    [JsonPropertyName("tipologiaFattura")]
    public string? TipologiaFattura { get; set; }

    [JsonPropertyName("istitutioID")]
    public string? IstitutioID { get; set; }

    [JsonPropertyName("onboardingTokenID")] 
    public string? OnboardingTokenID { get; set; }

    [JsonPropertyName("ragionesociale")] 
    public string? RagioneSociale { get; set; }

    [JsonPropertyName("tipocontratto")] 
    public string? TipoContratto { get; set; }

    [JsonPropertyName("idcontratto")]
    public string? IdContratto { get; set; }

    [JsonPropertyName("tipoDocumento")] 
    public string? TipoDocumento { get; set; }

    [JsonPropertyName("divisa")]
    public string? Divisa { get; set; }

    [JsonPropertyName("metodoPagamento")] 
    public string? MetodoPagamento { get; set; }

    [JsonPropertyName("causale")]
    public string? Causale { get; set; }

    [JsonPropertyName("split")]
    public bool? Split { get; set; }

    [JsonPropertyName("inviata")] 
    public int? Inviata { get; set; }

    [JsonPropertyName("elaborazione")]
    public int? Elaborazione { get; set; }

    [JsonPropertyName("sollecito")]
    public string? Sollecito { get; set; }

    [JsonPropertyName("datiGeneraliDocumento")] 
    public List<DatiGeneraliDocumentoDto>? DatiGeneraliDocumento { get; set; }

    [JsonPropertyName("posizioni")] 
    public List<PosizioniDto>? Posizioni { get; set; }
}