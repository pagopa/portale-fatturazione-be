using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

public sealed class  TipologiaContrattoQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<TipologiaContrattoDto>>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo; 
} 