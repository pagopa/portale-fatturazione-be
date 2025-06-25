namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;

public class ContestazioneRecap
{ 
        public string? TipologiaFattura { get; set; }

        public int IdFlagContestazione { get; set; }

        public string? FlagContestazione { get; set; }

        public int Totale { get; set; }

        public int TotaleNotificheAnalogiche { get; set; }

        public int TotaleNotificheDigitali { get; set; } 
} 