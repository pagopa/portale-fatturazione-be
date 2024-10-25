using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Commands;

public class ContestazioneCreateCommand(IAuthenticationInfo? authenticationInfo) : IRequest<Contestazione?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int TipoContestazione { get; set; }
    public string? IdNotifica { get; set; }
    public string? NoteEnte { get; set; }
    public DateTime DataInserimentoEnte { get; set; }
    public short StatoContestazione { get; set; }
    public int Anno { get; set; }
    public int Mese { get; set; }
}