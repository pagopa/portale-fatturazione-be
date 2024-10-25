using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Scadenziari;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari.Queries;

public class ScadenziarioQueryGetByTipo(IAuthenticationInfo authenticationInfo, TipoScadenziario tipo, int anno, int mese) : IRequest<(bool, Scadenziario)>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public TipoScadenziario Tipo { get; internal set; } = tipo;
    public int Anno { get; internal set; } = anno;
    public int Mese { get; internal set; } = mese;
}