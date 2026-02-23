using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries;

public class RelRigheSospeseQueryGetById(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<RigheRelDto>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string? IdTestata { get; set; }
    public string? FlagConguaglio { get; set; }
}