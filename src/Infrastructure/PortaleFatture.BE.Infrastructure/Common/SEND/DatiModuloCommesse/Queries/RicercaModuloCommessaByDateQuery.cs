using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;

public class RicercaModuloCommessaByDateQuery(IAuthenticationInfo authenticationInfo) : IRequest<ModuloCommessaPrevisionaleTotaliDateDto>
{
    public IAuthenticationInfo? AuthenticationInfo { get; private set; } = authenticationInfo;
    public DateTime? DataInizioModulo { get; set; } 
    public DateTime? DataFineModulo { get; set; } 
    public string[]? IdEnti { get; set; } 
    public int? IdTipoContratto { get; set; } 
    public DateTime? DataInizioContratto { get; set; } 
    public DateTime? DataFineContratto { get; set; } 
    public int? PageNumber { get; set; } 
    public int? PageSize { get; set; }
}