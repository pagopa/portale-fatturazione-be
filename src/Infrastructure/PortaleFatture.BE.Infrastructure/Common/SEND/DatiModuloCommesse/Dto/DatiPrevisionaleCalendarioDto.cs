namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;

public class DatiPrevisionaleCalendarioDto
{
 
    public int AnnoRiferimento { get; set; } 
    public int MeseRiferimento { get; set; } 
    public string? Source { get; set; } 
    public int Year { get; set; } 
    public int Month { get; set; } 
    public int Quarter { get; set; } 
    public DateTime Datavalidita { get; set; } 
    public DateTime Datavaliditalegale { get; set; }
}