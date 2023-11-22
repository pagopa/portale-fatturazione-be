using System.Security.Claims;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Extensions;

public static class IdentityExtensions
{
    public static ProfileInfo Mapper(this IList<Claim> authClaims, string jwt, DateTime validTo)
    {
        return new ProfileInfo()
        {
            DescrizioneRuolo = authClaims.FirstOrDefault(c => c.Type == CustomClaim.DescrizioneRuolo)!.Value,
            Email = authClaims.FirstOrDefault(c => c.Type == ClaimTypes.Email) == null ? null : authClaims.FirstOrDefault(c => c.Type == ClaimTypes.Email)!.Value,
            IdEnte = authClaims.FirstOrDefault(c => c.Type == CustomClaim.IdEnte)!.Value,
            JWT = jwt,
            Prodotto = authClaims.FirstOrDefault(c => c.Type == CustomClaim.Prodotto)!.Value,
            Profilo = authClaims.FirstOrDefault(c => c.Type == CustomClaim.Profilo)!.Value,
            Ruolo = authClaims.FirstOrDefault(c => c.Type == ClaimTypes.Role)!.Value,
            Valido = validTo,
            Id = authClaims.FirstOrDefault(c => c.Type == ClaimTypes.Name)!.Value,
            IdTipoContratto = authClaims.FirstOrDefault(c => c.Type == CustomClaim.IdTipoContratto) == null ? null : Convert.ToInt64(authClaims.FirstOrDefault(c => c.Type == CustomClaim.IdTipoContratto)!.Value),
        };
    }

    public static AuthenticationInfo Mapper(this IEnumerable<Claim> authClaims)
    {
        return new AuthenticationInfo()
        {
            DescrizioneRuolo = authClaims.FirstOrDefault(c => c.Type == CustomClaim.DescrizioneRuolo)!.Value,
            Email = authClaims.FirstOrDefault(c => c.Type == ClaimTypes.Email) == null ? null : authClaims.FirstOrDefault(c => c.Type == ClaimTypes.Email)!.Value,
            IdEnte = authClaims.FirstOrDefault(c => c.Type == CustomClaim.IdEnte)!.Value, 
            Prodotto = authClaims.FirstOrDefault(c => c.Type == CustomClaim.Prodotto)!.Value,
            Profilo = authClaims.FirstOrDefault(c => c.Type == CustomClaim.Profilo)!.Value,
            Ruolo = authClaims.FirstOrDefault(c => c.Type == ClaimTypes.Role)!.Value, 
            Id = authClaims.FirstOrDefault(c => c.Type == ClaimTypes.Name)!.Value,
            IdTipoContratto = authClaims.FirstOrDefault(c => c.Type == CustomClaim.IdTipoContratto) == null ? null: Convert.ToInt64(authClaims.FirstOrDefault(c => c.Type == CustomClaim.IdTipoContratto)!.Value),
        };
    }
}