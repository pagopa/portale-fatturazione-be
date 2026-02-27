using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Response;

public class DocContabileEliminateResponse
{
    [JsonPropertyOrder(-1)]
    [JsonPropertyName("importo")]
    public decimal? Totale { get; set; }

    [JsonPropertyOrder(-2)]
    [JsonPropertyName("dettagli")]
    public IEnumerable<DocContabileEliminataDettaglioResponse>? Dettagli { get; set; }
}

public class DocContabileEliminataDettaglioResponse
{
    [JsonPropertyName("fattura")]
    public DocContabileEliminataFatturaResponse? Fattura { get; set; }
}

public class DocContabileEliminataFatturaResponse
{
    [JsonPropertyName("totale")]
    public decimal? Totale { get; set; }

    [JsonPropertyName("numero")]
    public long Progressivo { get; set; }

    [JsonPropertyName("idfattura")]
    public int IdFattura { get; set; }

    [JsonPropertyName("dataFattura")]
    public string? DataFattura { get; set; }

    [JsonPropertyName("prodotto")]
    public string? Prodotto { get; set; }

    [JsonPropertyName("identificativo")]
    public string? PeriodoFatturazione { get; set; }

    [JsonPropertyName("istitutioId")]
    public string? IstitutioId { get; set; }

    [JsonPropertyName("onboardingTokenID")]
    public string? OnboardingTokenId { get; set; }

    [JsonPropertyName("ragioneSociale")]
    public string? RagioneSociale { get; set; }

    [JsonPropertyName("idcontratto")]
    public string? IdContratto { get; set; }

    [JsonPropertyName("tipoDocumento")]
    public string? TipoDocumento { get; set; }

    [JsonPropertyName("tipocontratto")]
    public string? TipoContratto { get; set; }

    [JsonPropertyName("divisa")]
    public string? Divisa { get; set; }

    [JsonPropertyName("metodoPagamento")]
    public string? MetodoPagamento { get; set; }

    [JsonPropertyName("causale")]
    public string? CausaleFattura { get; set; }

    [JsonPropertyName("split")]
    public bool SplitPayment { get; set; }

    [JsonPropertyName("inviata")]
    public int Inviata { get; set; }

    [JsonPropertyName("sollecito")]
    public string? Sollecito { get; set; }

    [JsonPropertyName("stato")]
    public string? Stato { get; set; }

    [JsonPropertyName("datiGeneraliDocumento")]
    public IEnumerable<DocContabileDatiGeneraliResponse>? DatiGeneraliDocumento { get; set; }

    [JsonPropertyName("posizioni")]
    public IEnumerable<DocContabilePosizioneResponse>? Posizioni { get; set; }
}

