using MediatR;
using Microsoft.VisualBasic;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Notifiche;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;

public class RelUploadStateCreateCommand(IAuthenticationInfo? authenticationInfo) : IRequest<bool?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string? IdEnte { get; set; }
    public string? IdContratto { get; set; }
    public string? TipologiaFattura { get; set; }
    public int? Anno { get; set; }
    public int? Mese { get; set; }  
}