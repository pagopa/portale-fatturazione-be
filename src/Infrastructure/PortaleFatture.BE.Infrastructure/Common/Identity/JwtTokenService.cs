using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.Identity;

public class JwtTokenService : ITokensService
{
    private readonly string _audience;
    private readonly string _issuer;
    private readonly byte[] _tokenKey;
    public JwtTokenService(string audience, string issuer, string secret)
    { 
        _tokenKey = Encoding.UTF8.GetBytes(secret);
        _audience = audience;
        _issuer = issuer;
    }

    public (string token, DateTime validTo) GenerateJwtTokens(string username, IList<Claim> authClaims)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validTo = DateTime.UtcNow.AddHours(1);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Audience = _audience,
            Issuer = _issuer,
            Subject = new ClaimsIdentity(authClaims),
            Expires = validTo,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(_tokenKey),
                SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return (tokenHandler.WriteToken(token), validTo);
    }

    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(_tokenKey),
            ClockSkew = TimeSpan.Zero
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token");
        }

        return principal;
    }
} 