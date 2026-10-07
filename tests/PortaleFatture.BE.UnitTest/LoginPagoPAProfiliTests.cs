using System.IdentityModel.Tokens.Jwt;
using PortaleFatture.BE.Api.Modules.SEND.Auth.Extensions;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Auth.PagoPA;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Login admin (<c>POST api/v2/auth/pagopa/login</c>, PF-908): <see cref="AuthExtensions.MapperPagoPAProfili"/>
/// emette un profilo per prodotto, ciascuno con JWT e nonce propri. Il frontend fa scegliere il prodotto
/// e usa quel token per tutte le chiamate successive, quindi ogni profilo deve portare il SUO prodotto
/// sia nel claim del JWT (letto da <c>api/auth/profilo</c>) sia nel nonce (verificato dal
/// NonceMultiTabsMiddleware): un prodotto sbagliato nel nonce darebbe 419 su ogni chiamata.
/// </summary>
public class LoginPagoPAProfiliTests
{
    private const string Secret = "chiave-di-test-lunga-almeno-32-caratteri!";
    private readonly AesEncryption _encryption = new("chiave-di-test-32-caratteri-1234");

    private static AuthenticationInfo AuthPagoPA() => new()
    {
        Id = "utente-admin-test",
        Ruolo = Ruolo.ADMIN,
        DescrizioneRuolo = "Amministratore",
        Profilo = "PAGOPA",
        GruppoRuolo = "gruppo-test",
        Auth = AuthType.PAGOPA,
        Email = "admin@test.invalid"
    };

    private List<ProfileInfo> Login() => AuthPagoPA().MapperPagoPAProfili(
        new IdentityUsersService(),
        new JwtTokenService("test-audience", "test-issuer", Secret),
        _encryption);

    [Test]
    public void MapperPagoPAProfili_ShouldRestituire_TreProfili_NellOrdine_PagoPA_SEND_AppIO()
    {
        var prodotti = Login().Select(p => p.Prodotto);

        Assert.That(prodotti, Is.EqualTo(new[] { ProductRoles.pagoPA, ProductRoles.SEND, ProductRoles.AppIO }),
            "l'ordine storico (pagoPA, SEND) va preservato: APP IO si aggiunge in coda");
    }

    [Test]
    public void AppIO_ShouldAvere_CodiceProdottoSelfCare()
    {
        Assert.That(ProductRoles.AppIO, Is.EqualTo("prod-appio"));
    }

    [Test]
    public void MapperPagoPAProfili_OgniJwt_ShouldPortare_IlProdottoDelProprioProfilo()
    {
        var handler = new JwtSecurityTokenHandler();

        Assert.Multiple(() =>
        {
            foreach (var profilo in Login())
            {
                var claimProdotto = handler.ReadJwtToken(profilo.JWT)
                    .Claims.Single(c => c.Type == CustomClaim.Prodotto).Value;
                Assert.That(claimProdotto, Is.EqualTo(profilo.Prodotto), $"JWT del profilo {profilo.Prodotto}");
            }
        });
    }

    [Test]
    public void MapperPagoPAProfili_OgniNonce_ShouldVerificare_SoloIlProprioProdotto()
    {
        var profili = Login();

        Assert.Multiple(() =>
        {
            foreach (var profilo in profili)
            {
                var nonce = _encryption.DecryptString(profilo.Nonce!);
                foreach (var prodotto in profili.Select(p => p.Prodotto))
                {
                    var identita = new AuthenticationInfo { Id = profilo.Id, IdEnte = profilo.IdEnte, Prodotto = prodotto };
                    Assert.That(identita.Verify(nonce), Is.EqualTo(prodotto == profilo.Prodotto),
                        $"nonce del profilo {profilo.Prodotto} verificato con il prodotto {prodotto}");
                }
            }
        });
    }

    [Test]
    public void MapperPagoPAProfili_ShouldEmettere_TokenDistinti()
    {
        var jwt = Login().Select(p => p.JWT).ToList();

        Assert.That(jwt, Is.Unique);
    }
}
