using System.Web;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.Api.Modules.Auth.Extensions;

public static class AuthExtensions
{
    public static UtenteInfo Mapper(
        this AuthenticationInfo info,
        DateTime dataPrimo,
        DateTime dataUltimo,
        IAesEncryption encryption)
    {
        return new UtenteInfo()
        {
            DescrizioneRuolo = info.DescrizioneRuolo,
            Email = info.Email,
            Id = info.Id,
            IdEnte = info.IdEnte,
            IdTipoContratto = info.IdTipoContratto,
            NomeEnte = info.NomeEnte,
            Prodotto = info.Prodotto,
            Profilo = info.Profilo,
            Ruolo = info.Ruolo,
            DataPrimo = dataPrimo,
            DataUltimo = dataUltimo,
            Nonce = encryption.EncryptString(info.CreateNonce()),
        };
    }

    public static List<ProfileInfo> Mapper(
        this List<AuthenticationInfo> infos,
        IIdentityUsersService usersService,
        ITokenService tokensService,
        IAesEncryption encryption)
    {
        List<ProfileInfo> profiles = [];

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
                Nonce = encryption.EncryptString(authInfo.CreateNonce()),
                JWT = tokenProfileInfo.JWT,
                Valido = tokenProfileInfo.Valido
            };
            profiles.Add(profile);
        }
        return profiles;
    }

    private const string _separator = "|";
    private static string CreateNonce(this AuthenticationInfo authInfo)
    {
        return (new NonceDto()
        {
            Id = authInfo.Id,
            IdEnte = authInfo.IdEnte,
            Prodotto = authInfo.Prodotto
        }).Serialize();
    }

    private static AuthenticationInfo ReadNonce(this string nonce)
    {
        var value = nonce.Deserialize<NonceDto>();
        return new AuthenticationInfo()
        {
            Id = value.Id,
            IdEnte = value.IdEnte,
            Prodotto = value.Prodotto
        };
    }

    public static bool Verify(this AuthenticationInfo authInfo, string decryptedNonce)
    {
        var checkInfo = decryptedNonce.ReadNonce(); 
        return authInfo.Id == checkInfo.Id && authInfo.IdEnte == checkInfo.IdEnte && authInfo.Prodotto == checkInfo.Prodotto;
    }
}