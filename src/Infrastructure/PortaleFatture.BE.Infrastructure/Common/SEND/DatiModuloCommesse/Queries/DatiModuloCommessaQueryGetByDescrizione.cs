using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;
public class DatiModuloCommessaQueryGetByDescrizione(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<ModuloCommessaByRicercaDto>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int? AnnoValidita { get; set; }
    public int? MeseValidita { get; set; }
    public string? Prodotto { get; set; }
    public string[]? IdEnti { get; set; }
}