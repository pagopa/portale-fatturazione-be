using System.Text.Json.Serialization;
using DocumentFormat.OpenXml.Drawing.Charts;
using PortaleFatture.BE.Core.Entities.Messaggi;

namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Response;

public class DocumentoContabileDettaglioResponse
{
        [JsonPropertyName("idTestata")]
        public string? IdTestata { get; set; }

        [JsonPropertyName("idEnte")]
        public string? IdEnte { get; set; }

        [JsonPropertyName("ragioneSociale")]
        public string? RagioneSociale { get; set; }

        [JsonPropertyName("dataDocumento")]
        public string? DataDocumento { get; set; }

        [JsonPropertyName("idDocumento")]
        public string? IdDocumento { get; set; }

        [JsonPropertyName("totaleFattura")]
        public decimal? TotaleFattura { get; set; }

        [JsonPropertyName("cup")]
        public string? Cup { get; set; }

        [JsonPropertyName("idContratto")]
        public string? IdContratto { get; set; }

        [JsonPropertyName("tipologiaFattura")]
        public string? TipologiaFattura { get; set; }

        [JsonPropertyName("anno")]
        public string? Anno { get; set; }

        [JsonPropertyName("mese")]
        public string? Mese { get; set; }

        [JsonPropertyName("totaleAnalogico")]
        public decimal? TotaleAnalogico { get; set; }

        [JsonPropertyName("totaleDigitale")]
        public decimal? TotaleDigitale { get; set; }

        [JsonPropertyName("totaleNotificheAnalogiche")]
        public int? TotaleNotificheAnalogiche { get; set; }

        [JsonPropertyName("totaleNotificheDigitali")]
        public int? TotaleNotificheDigitali { get; set; }

        [JsonPropertyName("totale")]
        public decimal? Totale { get; set; }

        [JsonPropertyName("datiFatturazione")]
        public bool? DatiFatturazione { get; set; } // ! TODO: verificare quale tipologia di oggetto 

        [JsonPropertyName("iva")]
        public decimal? Iva { get; set; }

        [JsonPropertyName("totaleAnalogicoIva")]
        public decimal? TotaleAnalogicoIva { get; set; }

        [JsonPropertyName("totaleDigitaleIva")]
        public decimal? TotaleDigitaleIva { get; set; }

        [JsonPropertyName("totaleIva")]
        public decimal? TotaleIva { get; set; }

        [JsonPropertyName("firmata")]
        public string? Firmata { get; set; } // ! TODO :Firmata corrisponde al campo RelFirmata del DTO ? verificare se è un booleano o una stringa

        [JsonPropertyName("caricata")]
        public int? Caricata { get; set; }

        [JsonPropertyName("tipologiaContratto")]
        public string? TipologiaContratto { get; set; }

        [JsonPropertyName("anticipoDigitale")]
        public decimal? AnticipoDigitale { get; set; }

        [JsonPropertyName("anticipoAnalogico")]
        public decimal? AnticipoAnalogico { get; set; }

        [JsonPropertyName("accontoDigitale")]
        public decimal? AccontoDigitale { get; set; }

        [JsonPropertyName("accontoAnalogico")]
        public decimal? AccontoAnalogico { get; set; }

        [JsonPropertyName("stornoDigitale")]
        public decimal? StornoDigitale { get; set; }

        [JsonPropertyName("stornoAnalogico")]
        public decimal? StornoAnalogico { get; set; }
}

public class DocumentoContabileEmessoDettaglioResponse : DocumentoContabileDettaglioResponse
{
        [JsonPropertyOrder(100)]
        [JsonPropertyName("fattureSospese")]
        public IEnumerable<DocumentoContabileSospesoDettaglioResponse>? FattureSospese { get; set; }
}


public class DocumentoContabileSospesoDettaglioResponse
{
        [JsonPropertyName("idFattura")]
        public string? IdFatturaSospesa { get; set; }

        [JsonPropertyName("dataFattura")]
        public string? DataFatturaSospesa { get; set; }

        [JsonPropertyName("progressivo")]
        public long? Progressivo { get; set; }

        [JsonPropertyName("tipoDocumento")]
        public string? TipoDocumento { get; set; }

        [JsonPropertyName("totaleFatturaImponibile")]
        public decimal? TotaleFatturaImponibile { get; set; }

        [JsonPropertyName("totale")]
        public decimal? TotaleFattura { get; set; }

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
}