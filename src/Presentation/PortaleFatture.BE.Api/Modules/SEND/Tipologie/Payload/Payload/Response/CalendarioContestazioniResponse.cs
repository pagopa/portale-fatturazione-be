using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Api.Modules.SEND.Tipologie.Payload.Payload.Response;

public sealed class CalendarioContestazioniResponse
{
    public string? DataFine { get; set; }
    public string? DataInizio { get; set; }
    public string? MeseContestazione { get; set; }
    public int AnnoContestazione { get; set; }
    public string? DataRecapitistaFine { get; set; }
    public string? DataRecapitistaInizio { get; set; }
}