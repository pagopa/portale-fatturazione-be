using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries;
public class DatiModuloCommessaQueryGet : IRequest<ModuloCommessaDto?>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }
    public string? Prodotto { get; set; }
    public long IdTipoContratto { get; set; } 
    public string? IdEnte { get; set; }
    public int AnnoValidita { get; set; } 
    public int MeseValidita { get; set; }
} 