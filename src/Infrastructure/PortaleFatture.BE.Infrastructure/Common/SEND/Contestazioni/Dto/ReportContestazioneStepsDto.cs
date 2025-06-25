using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;

public class ReportContestazioneStepsDto
{
    public int ReportId { get; set; }
    public int Step { get; set; }
    public string? DescrizioneStep { get; set; } 
    public int? TotaleNotificheAnalogiche890 { get; set; } 

    // AR
    public int? TotaleNotificheAnalogicheARNazionaliAR { get; set; } 
    public int? TotaleNotificheAnalogicheARInternazionaliRIR { get; set; }

    //RS
    public int? TotaleNotificheAnalogicheRSNazionaliRS { get; set; } 
    public int? TotaleNotificheAnalogicheRSInternazionaliRIS { get; set; } 

    public int? TotaleNotificheDigitali { get; set; }
    public int? TotaleNotifiche { get; set; }
    [JsonIgnore]
    public string? Link { get; set; } 
    public int? NonContestataAnnullata { get; set; }
    public int? ContestataEnte { get; set; } 
    public int? RispostaEnte { get; set; }
    public int? Accettata { get; set; }
    public int? RispostaSend { get; set; }
    public int? RispostaRecapitista { get; set; }
    public int? RispostaConsolidatore { get; set; }
    public int? Rifiutata { get; set; } 
    public int? NonFatturabile { get; set; }
    public int? Fatturabile { get; set; }
    [JsonIgnore]
    public string? Storage { get; set; }
    public string? NomeDocumento { get; set; }
    public DateTime? DataCompletamento { get; set; }
} 