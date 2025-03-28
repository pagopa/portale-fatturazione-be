using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.Queries;

public sealed class OrchestratoreByFaseQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<string>>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo; 
}