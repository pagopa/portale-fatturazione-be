namespace PortaleFatture.BE.Core.Entities.SEND.Fatture
{
    public class DocumentoContabileEmesso
    {
        public string? RagioneSociale { get; set; } = string.Empty;
        public string? IdContratto { get; set; } = string.Empty;
        public string Mese { get; set; } = string.Empty;
        public string Anno { get; set; } = string.Empty;
        public decimal Totale { get; set; } = 0;
        public decimal TotaleAnalogico { get; set; } = 0;
        public decimal TotaleDigitale { get; set; } = 0;
        public int TotaleNotificheDigitali { get; set; } = 0;
        public int TotaleNotificheAnalogiche { get; set; } = 0;
        public decimal TotaleAnticipo { get; set; } = 0;
        public decimal AnticipoDigitale { get; set; } = 0;
        public decimal AnticipoAnalogico { get; set; } = 0;
        public decimal TotaleStorno { get; set; } = 0;
        public decimal StornoDigitale { get; set; } = 0;
        public decimal StornoAnalogico { get; set; } = 0;
        public decimal TotaleAcconto { get; set; } = 0;
        public decimal AccontoDigitale { get; set; } = 0;
        public decimal AccontoAnalogico { get; set; } = 0;
        public decimal Imponibile { get; set; } = 0;
        public string? TipologiaFattura { get; set; } = string.Empty;
    }
}