using System.Web;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Auth.PagoPA;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.Api.Modules.SEND.Auth.Extensions;

public static class AuthExtensions
{
    public static UtenteInfo MapperSelfCare(
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
            GruppoRuolo = info.GruppoRuolo,
            Auth = info.Auth
        };
    }

    public static ProfileInfo MapperPagoPA(
    this AuthenticationInfo authInfo,
    IIdentityUsersService usersService,
    ITokenService tokensService,
    IAesEncryption encryption,
    List<string> featureAccese)
    {
        var profilo = authInfo.MapperPagoPAProdotto(ProductRoles.SEND, usersService, tokensService, encryption);
        profilo.Features = featureAccese;
        return profilo;
    }

    // Prodotti restituiti dal login admin, nell'ordine in cui compaiono nella risposta
    public static readonly string[] ProdottiPagoPA = [ProductRoles.pagoPA, ProductRoles.SEND, ProductRoles.AppIO];

    // Prodotti legati a un feature flag: con il flag spento il login non ne emette il profilo
    private static readonly Dictionary<string, string> FlagProdotto = new() { [ProductRoles.AppIO] = FeatureFlags.AppIO };

    public static List<ProfileInfo> MapperPagoPAProfili(
        this AuthenticationInfo authInfo,
        IIdentityUsersService usersService,
        ITokenService tokensService,
        IAesEncryption encryption,
        List<string> featureAccese)
    {
        return ProdottiPagoPA
            .Where(prodotto => !FlagProdotto.TryGetValue(prodotto, out var flag) || featureAccese.Contains(flag))
            .Select(prodotto =>
            {
                var profilo = authInfo.MapperPagoPAProdotto(prodotto, usersService, tokensService, encryption);
                profilo.Features = featureAccese;
                return profilo;
            })
            .ToList();
    }

    private static ProfileInfo MapperPagoPAProdotto(
        this AuthenticationInfo authInfo,
        string prodotto,
        IIdentityUsersService usersService,
        ITokenService tokensService,
        IAesEncryption encryption)
    {
        var listAuthClaims = usersService.GetUserClaimsFromPagoPAUserAsync(authInfo, prodotto);
        var tokenProfileInfo = tokensService.GenerateJwtToken(listAuthClaims);
        // il nonce legge authInfo.Prodotto: va impostato prima di CreateNonce
        authInfo.Prodotto = prodotto;
        return new ProfileInfo()
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
            Valido = tokenProfileInfo.Valido,
            GruppoRuolo = authInfo.GruppoRuolo,
            Auth = authInfo.Auth
        };
    }

    public static List<ProfileInfo> MapperSelfCare(
        this List<AuthenticationInfo> infos,
        IIdentityUsersService usersService,
        ITokenService tokensService,
        IAesEncryption encryption)
    {
        List<ProfileInfo> profiles = [];

        foreach (var authInfo in infos)
        {
            var listAuthClaims = usersService.GetUserClaimsFromSelfCareUserAsync(authInfo);
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
                Valido = tokenProfileInfo.Valido,
                GruppoRuolo = authInfo.GruppoRuolo,
                Auth = authInfo.Auth
            };
            profiles.Add(profile);
        }
        return profiles;
    }

    private const string _separator = "|";
    private static string CreateNonce(this AuthenticationInfo authInfo)
    {
        return new NonceDto()
        {
            Id = authInfo.Id,
            IdEnte = authInfo.IdEnte ?? string.Empty,
            Prodotto = authInfo.Prodotto ?? string.Empty,
        }.Serialize();
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
        return authInfo.Id == checkInfo.Id && AreEqual(authInfo.IdEnte, checkInfo.IdEnte) && AreEqual(authInfo.Prodotto, checkInfo.Prodotto);
    }

    private static bool AreEqual(string? a, string? b)
    {
        if (string.IsNullOrEmpty(a))
            return string.IsNullOrEmpty(b);
        else
            return string.Equals(a, b);
    }
}