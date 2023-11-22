using System.IdentityModel.Tokens.Jwt;
using System.Security;
using System.Security.Claims;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.Identity;

public class IdentityUsersService : IIdentityUsersService
{
    public List<IList<Claim>> GetListUserClaimsFromUserAsync(List<AuthenticationInfo>? authInfos)
    {
        var list = new List<IList<Claim>>();
        if (authInfos!.IsNullNotAny())
            throw new SecurityException();

        foreach (var authInfo in authInfos!)
        {
            var claimList = GetUserClaimsFromUserAsync(authInfo);
            list.Add(claimList);
        }
        return list;
    }

    public IList<Claim> GetUserClaimsFromUserAsync(AuthenticationInfo? authInfo)
    {
        if (authInfo == null)
            throw new SecurityException();

        var claimList = new List<Claim>
                {
                    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new(ClaimTypes.Name, authInfo.Id ?? throw new SecurityException()),
                    new Claim(ClaimTypes.Role, authInfo.Ruolo ?? throw new SecurityException()),
                    new Claim(CustomClaim.DescrizioneRuolo, authInfo.DescrizioneRuolo! ?? throw new SecurityException()),
                    new Claim(CustomClaim.Profilo, authInfo.Profilo ?? throw new SecurityException()),
                    new Claim(CustomClaim.Prodotto, authInfo.Prodotto ?? throw new SecurityException()),
                    new Claim(CustomClaim.IdEnte, authInfo.IdEnte ?? throw new SecurityException())
              };

        if (authInfo.IdTipoContratto != null)
            claimList.Add(new Claim(CustomClaim.IdTipoContratto, authInfo.IdTipoContratto.Value.ToString()));
        if (authInfo.Email != null)
            claimList.Add(new(ClaimTypes.Email, authInfo.Email));

        return claimList;
    }
}