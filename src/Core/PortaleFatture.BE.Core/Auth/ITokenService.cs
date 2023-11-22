using System.Security.Claims;

namespace PortaleFatture.BE.Core.Auth;

public interface ITokenService
{
    ProfileInfo GenerateJwtToken(IList<Claim> authClaims);
} 