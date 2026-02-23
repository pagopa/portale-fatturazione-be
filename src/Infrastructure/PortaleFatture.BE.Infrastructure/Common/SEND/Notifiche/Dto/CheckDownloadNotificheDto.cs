namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;

public sealed class CheckDownloadNotificheDto
{
    public DateTime? DataMassimaContestazione { get; set; }

    public bool? NotificheDaScaricare { get; set; }

    public bool? CiSonoContestazioni { get; set; }
} 