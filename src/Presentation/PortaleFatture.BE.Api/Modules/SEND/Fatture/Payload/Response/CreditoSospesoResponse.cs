using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Response;

public class CreditoSospesoResponse
{
    [JsonPropertyOrder(-1)]
    [JsonPropertyName("importoSospeso")]
    public decimal? ImportoSospeso { get; set; }

    [JsonPropertyOrder(-2)]
    [JsonPropertyName("dettagli")]
    public IEnumerable<DettaglioCreditoSospesoResponse>? Dettagli { get; set; }
}

public class FatturaResponse
{
    [JsonPropertyName("totale")] 
    public decimal? ImportoSospesoParziale { get; set; }

    [JsonPropertyName("numero")]
    public int Progressivo { get; set; }

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
    public IEnumerable<DatiGeneraliDocumentoResponse>? DatiGeneraliDocumento { get; set; }

    [JsonPropertyName("posizioni")]
    public IEnumerable<CreditoSospesoPosizioniResponse>? Posizioni { get; set; }
}

public class DatiGeneraliDocumentoResponse
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

public class DettaglioCreditoSospesoResponse
{
    [JsonPropertyName("fattura")]
    public FatturaResponse? Fattura { get; set; }
}

public class CreditoSospesoPosizioniResponse
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