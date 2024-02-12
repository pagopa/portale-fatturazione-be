namespace PortaleFatture.BE.Api.Modules.Notifiche.Payload.Request;

public class ContestazionePagoPAUpdateRequest
{
    public string? IdNotifica { get; set; }

    /// <example>PA,RCP,CON</example>
    public string? Onere { get; set; } 
    public string? NoteSend { get; set; }
    public short StatoContestazione { get; set; } 
}