using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Queries;
public class PSPQueryGetByName(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<ContractIdPSP>>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string? Name { get; set; }  
}