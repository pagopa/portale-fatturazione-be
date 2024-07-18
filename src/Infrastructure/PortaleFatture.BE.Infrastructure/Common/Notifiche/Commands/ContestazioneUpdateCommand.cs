using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Notifiche;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;

public class ContestazioneUpdateCommand(IAuthenticationInfo? authenticationInfo, string? IdNotifica) : IRequest<Contestazione?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;   
    public string? IdNotifica { get; internal set; } = IdNotifica;
    public string? NoteEnte { get; set; } 
    public string? RispostaEnte { get; set; } 
    public string? Onere { get; set; }
    public DateTime DataModificaEnte { get; set; } 
    public DateTime? DataChiusura { get; set; }
    public short StatoContestazione { get; set; } 
    public short ExpectedStatoContestazione { get; set; }
}