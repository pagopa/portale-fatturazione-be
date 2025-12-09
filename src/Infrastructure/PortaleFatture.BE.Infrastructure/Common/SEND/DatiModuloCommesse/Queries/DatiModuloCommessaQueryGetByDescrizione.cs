using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;
public class DatiModuloCommessaQueryGetByDescrizione(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<ModuloCommessaPrevisionaleTotaleDto>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int? AnnoValidita { get; set; }
    public int? MeseValidita { get; set; }
    public string? Prodotto { get; set; }
    public string[]? IdEnti { get; set; } 
    public bool RecuperaRegioni { get; set; } = true; 
    public int? IdTipoContratto { get; set; }
}