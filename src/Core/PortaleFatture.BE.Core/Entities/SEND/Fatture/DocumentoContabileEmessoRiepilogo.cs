using System;

namespace PortaleFatture.BE.Core.Entities.SEND.Fatture
{
    public class DocumentoContabileEmessoRiepilogo
    {
        public string Anno { get; set; } = string.Empty;
        public string Mese { get; set; } = string.Empty;
        public decimal? Totale { get; set; }
        public decimal? TotaleDigitale { get; set; }
        public decimal? TotaleAnalogico { get; set; }
        public int? TotaleNotificheDigitali { get; set; }
        public int? TotaleNotificheAnalogiche { get; set; }
    }
}
