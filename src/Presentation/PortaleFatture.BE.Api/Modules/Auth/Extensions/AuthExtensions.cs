using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.Identity;

namespace PortaleFatture.BE.Api.Modules.Auth.Extensions;

public static class AuthExtensions
{
    public static List<ProfileInfo> Mapper(
        this List<AuthenticationInfo> infos,
        IIdentityUsersService usersService,
        ITokenService tokensService)
    {
        List<ProfileInfo> profiles = new();

        foreach (var authInfo in infos)
        {
            var listAuthClaims = usersService.GetUserClaimsFromUserAsync(authInfo);
            var tokenProfileInfo = tokensService.GenerateJwtToken(listAuthClaims);
            var profile = new ProfileInfo()
            {
                DescrizioneRuolo = authInfo.DescrizioneRuolo,
                Email = authInfo.Email,
                Id = authInfo.Id,
                IdEnte = authInfo.IdEnte,
                IdTipoContratto = authInfo.IdTipoContratto,
                NomeEnte = authInfo.NomeEnte,
                Prodotto = authInfo.Prodotto,
                Profilo = authInfo.Profilo,
                Ruolo = authInfo.Ruolo,
                JWT = tokenProfileInfo.JWT,
                Valido = tokenProfileInfo.Valido
            };
            profiles.Add(profile);
        }
        return profiles;
    }
}