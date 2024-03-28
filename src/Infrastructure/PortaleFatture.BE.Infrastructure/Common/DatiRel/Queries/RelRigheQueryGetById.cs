using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries;

public class RelRigheQueryGetById(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<RigheRelDto>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo; 
    public string? IdTestata { get; set; }  
}