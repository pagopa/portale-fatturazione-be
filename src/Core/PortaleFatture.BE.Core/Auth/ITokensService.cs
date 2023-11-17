using System.Security.Claims;

namespace PortaleFatture.BE.Core.Auth;

public interface ITokensService
{
    (string token, DateTime validTo) GenerateJwtTokens(string username, IList<Claim> authClaims);
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
} 