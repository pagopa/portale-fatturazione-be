using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Queries;

public class MessaggioQueryGetById(IAuthenticationInfo authenticationInfo) : IRequest<MessaggioDto?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public long? IdMessaggio { get; set; }
}