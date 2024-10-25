using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

public class FattureWorkFlowQuery(IAuthenticationInfo authenticationInfo, List<FatturaDistinctDto>? workflow) : IRequest<IEnumerable<FattureVerifyModifica>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public List<FatturaDistinctDto>? WorkFlow { get; set; } = workflow;
}