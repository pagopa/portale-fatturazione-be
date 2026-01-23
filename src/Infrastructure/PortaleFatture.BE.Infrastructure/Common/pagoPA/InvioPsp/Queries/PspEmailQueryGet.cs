using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.InvioPsp.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.InvioPsp.Queries;

public class PspEmailQueryGet(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<PspEmailDto>>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string[]? ContractIds { get; set; } 
    public string[]? Quarters { get; set; }
    public string? Year { get; set; }
}