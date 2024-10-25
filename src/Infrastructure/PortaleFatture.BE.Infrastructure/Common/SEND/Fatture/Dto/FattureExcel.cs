using System.Text.Json.Serialization;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

public class FattureExcel
{
    [HeaderAttributev2(caption: "Posizione", Order = 17)]
    public string? Posizione { get; set; }

    [HeaderAttributev2(caption: "Totale €", Order = 18)]
    public string? Totale { get; set; }

    [HeaderAttributev2(caption: "Numero", Order = 7)]

    [JsonPropertyName("numero")]
    public long? Numero { get; set; }

    [HeaderAttributev2(caption: "DataFattura", Order = 8)]

    [JsonPropertyName("dataFattura")]
    public string? DataFattura { get; set; }

    [HeaderAttributev2(caption: "Prodotto", Order = 4)]

    [JsonPropertyName("prodotto")]
    public string? Prodotto { get; set; }

    [HeaderAttributev2(caption: "Identificativo", Order = 4)]

    [JsonPropertyName("identificativo")]
    public string? Identificativo { get; set; }

    [HeaderAttributev2(caption: "Tipologia Fattura", Order = 6)]

    [JsonPropertyName("tipologiaFattura")]
    public string? TipologiaFattura { get; set; }

    [HeaderAttributev2(caption: "Id Ente", Order = 2)]

    [JsonPropertyName("istitutioID")]
    public string? IstitutioID { get; set; }

    [HeaderAttributev2(caption: "Onboarding Token Id", Order = 3)]

    [JsonPropertyName("onboardingTokenID")]
    public string? OnboardingTokenID { get; set; }


    [HeaderAttributev2(caption: "Ragione Sociale", Order = 1)]

    [JsonPropertyName("ragionesociale")]
    public string? RagioneSociale { get; set; }

    [HeaderAttributev2(caption: "Tipo Contratto", Order = 9)]

    [JsonPropertyName("tipocontratto")]
    public string? TipoContratto { get; set; }

    [HeaderAttributev2(caption: "Id Contratto", Order = 10)]

    [JsonPropertyName("idcontratto")]
    public string? IdContratto { get; set; }

    [HeaderAttributev2(caption: "Tipo Documento", Order = 11)]

    [JsonPropertyName("tipoDocumento")]
    public string? TipoDocumento { get; set; }

    [HeaderAttributev2(caption: "Divisa", Order = 12)]

    [JsonPropertyName("divisa")]
    public string? Divisa { get; set; }

    [HeaderAttributev2(caption: "Metodo Pagamento", Order = 13)]

    [JsonPropertyName("metodoPagamento")]
    public string? MetodoPagamento { get; set; }

    [HeaderAttributev2(caption: "Causale", Order = 14)]

    [JsonPropertyName("causale")]
    public string? Causale { get; set; }

    [HeaderAttributev2(caption: "Split", Order = 15)]

    [JsonPropertyName("split")]
    public bool? Split { get; set; }

    //[HeaderAttributev2(caption: "Sollecito", Order = 16)]

    [JsonPropertyName("sollecito")]
    public string? Sollecito { get; set; }
}