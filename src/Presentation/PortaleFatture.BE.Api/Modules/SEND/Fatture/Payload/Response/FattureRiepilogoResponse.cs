using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Response;

public sealed class FattureRiepilogoResponse
{
        [JsonPropertyName("idEnte")]
        public string? IdEnte { get; set; }

        [JsonPropertyName("ragioneSociale")]
        public string? RagioneSociale { get; set; }

        [JsonPropertyName("annoRiferimento")]
        public int? AnnoRiferimento { get; set; }

        [JsonPropertyName("meseRiferimento")]
        public int? MeseRiferimento { get; set; }

        [JsonPropertyName("idTipologiaContratto")]
        public string? IdTipologiaContratto { get; set; }

        [JsonPropertyName("tipologiaContratto")]
        public string? TipologiaContratto { get; set; }

        [JsonPropertyName("anticipo")]
        public decimal? Anticipo { get; set; }

        [JsonPropertyName("anticipoSospeso")]
        public bool? AnticipoSospeso { get; set; }

        [JsonPropertyName("acconto")]
        public decimal? Acconto { get; set; }

        [JsonPropertyName("accontoSospeso")]
        public bool? AccontoSospeso { get; set; }

        [JsonPropertyName("primoSaldo")]
        public decimal? PrimoSaldo { get; set; }

        [JsonPropertyName("primoSaldoSospeso")]
        public bool? PrimoSaldoSospeso { get; set; }

        [JsonPropertyName("secondoSaldo")]
        public decimal? SecondoSaldo { get; set; }
        
        [JsonPropertyName("secondoSaldoSospeso")]
        public bool? SecondoSaldoSospeso { get; set; }
}