using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Core.Entities.SEND.Scadenziari;

namespace PortaleFatture.BE.Core.Entities.SEND.Notifiche.Dto;

public sealed class AzioneNotificaDto
{
    public bool ChiusuraPermessa { get; set; }
    public bool CreazionePermessa { get; set; }
    public bool RispostaPermessa { get; set; }
    public Notifica? Notifica { get; set; }
    public Contestazione? Contestazione { get; set; }
    public CalendarioContestazione? Calendario { get; set; }
}