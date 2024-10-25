using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Queries;
public class PSPQueryGetByRicerca(IAuthenticationInfo authenticationInfo) : IRequest<PSPListDto>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int? Page { get; set; }
    public int? Size { get; set; } 
    public string[]? ContractIds { get; set; }
    public string? MembershipId { get; set; }
    public string? RecipientId { get; set; }
    public string? ABI { get; set; }
}