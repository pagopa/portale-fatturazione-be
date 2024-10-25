namespace PortaleFatture.BE.Api.Modules.SEND.Notifiche.Payload.Request;

public class ContestazionePagoPAUpdateRequest
{
    public string? IdNotifica { get; set; }

    /// <example>PA,REC,CON</example>
    public string? Onere { get; set; }
    public string? NoteSend { get; set; }
    public short StatoContestazione { get; set; }
}