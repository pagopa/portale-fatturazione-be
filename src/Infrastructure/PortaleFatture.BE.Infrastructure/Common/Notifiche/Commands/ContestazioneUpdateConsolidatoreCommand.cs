using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Notifiche;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;

public class ContestazioneUpdateConsolidatoreCommand(IAuthenticationInfo? authenticationInfo, string? IdNotifica) : IRequest<Contestazione?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;   
    public string? IdNotifica { get; internal set; } = IdNotifica; 
    public string? NoteConsolidatore { get; set; }  
    public DateTime? DataModificaConsolidatore{ get; set; } 
    public DateTime? DataInserimentoConsolidatore { get; set; } 
    public short ExpectedStatoContestazione { get; set; }
    public string? Onere { get; set; }
    public DateTime? DataChiusura { get; set; }
    public short StatoContestazione { get; set; } 
}