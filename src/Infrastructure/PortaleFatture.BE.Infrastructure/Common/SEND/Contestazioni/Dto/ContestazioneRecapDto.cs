namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;

public class ContestazioneRecapDto
{ 
    public string? IdEnte { get; set; }
    public int Anno { get; set; }
    public int Mese { get; set; }
    public string? ProdottoPostalizzazione { get; set; }
    public string? TipologiaFattura { get; set; }
    public int IdFlagContestazione { get; set; }
    public string? FlagContestazione { get; set; }
    public int Totale { get; set; }

} 