using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Commands;

public class ContestazioneUpdateRecapitistaCommand(IAuthenticationInfo? authenticationInfo, string? IdNotifica) : IRequest<Contestazione?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string? IdNotifica { get; internal set; } = IdNotifica;
    public string? NoteRecapitista { get; set; }
    public DateTime? DataModificaRecapitista { get; set; }
    public DateTime? DataInserimentoRecapitista { get; set; }
    public string? Onere { get; set; }
    public DateTime? DataChiusura { get; set; }
    public short StatoContestazione { get; set; }
    public short ExpectedStatoContestazione { get; set; }
}