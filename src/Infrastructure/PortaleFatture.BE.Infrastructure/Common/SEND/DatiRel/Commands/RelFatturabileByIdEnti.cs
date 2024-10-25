using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Commands;

public class RelFatturabileByIdEnti(IAuthenticationInfo authenticationInfo) : IRequest<int?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int Anno { get; set; }
    public int Mese { get; set; }
    public string[]? EntiIds { get; set; }
    public byte? Fatturabile { get; set; }
}