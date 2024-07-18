using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Scadenziari;

namespace PortaleFatture.BE.Infrastructure.Common.Scadenziari;

public interface IScadenziarioService
{
    Task<(bool, Scadenziario)> GetScadenziario(IAuthenticationInfo authenticationInfo, TipoScadenziario tipo, int anno, int mese);
} 