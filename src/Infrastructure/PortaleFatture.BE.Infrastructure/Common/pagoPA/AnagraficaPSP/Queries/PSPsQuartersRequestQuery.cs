using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Queries;

public class PSPsQuartersRequestQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<string>>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string? Year { get; set; }
}