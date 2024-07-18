using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries;
public class DatiModuloCommessaQueryGet : IRequest<ModuloCommessaDto?>
{
    public IAuthenticationInfo  AuthenticationInfo { get; internal set; }
    public DatiModuloCommessaQueryGet(IAuthenticationInfo  authenticationInfo)
    {
        this.AuthenticationInfo = authenticationInfo;
    }
    public string? Prodotto { get; set; }
    public int? AnnoValidita { get; set; } 
    public int? MeseValidita { get; set; }
} 