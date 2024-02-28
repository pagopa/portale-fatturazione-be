using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Api.Modules.Tipologie.Payload.Payload.Response;

public sealed class CalendarioContestazioniResponse
{
    public string? DataFine { get; set; } 
    public string? DataInizio { get; set; }   
    public string? MeseContestazione { get; set; } 
    public int AnnoContestazione { get; set; }
}