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

        public decimal TotaleAnticipo { get; set; } = 0;
        public decimal AnticipoDigitale { get; set; } = 0;
        public decimal AnticipoAnalogico { get; set; } = 0;
        public decimal TotaleStorno { get; set; } = 0;
        public decimal StornoDigitale { get; set; } = 0;
        public decimal StornoAnalogico { get; set; } = 0;
        public decimal TotaleAcconto { get; set; } = 0;
        public decimal AccontoDigitale { get; set; } = 0;
        public decimal AccontoAnalogico { get; set; } = 0;

        public string? TipologiaContratto { get; set; }
        public string? TipologiaFattura { get; set; }
        public string? TipologiaFatturaSospesa { get; set; }
    }
}
