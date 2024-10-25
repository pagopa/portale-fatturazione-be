using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Scadenziari;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari;

public interface IScadenziarioService
{
    Task<(bool, Scadenziario)> GetScadenziario(IAuthenticationInfo authenticationInfo, TipoScadenziario tipo, int anno, int mese);
}