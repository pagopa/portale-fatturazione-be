using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Auth.PagoPA;
using PortaleFatture.BE.Infrastructure.Common.Identity;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Login admin end-to-end con i feature flag (PF-550, PF-735): il FE riceve i flag accesi nel campo
/// "features" di ogni profilo, e con APP IO spento non riceve il profilo APP IO. Il token Azure AD non
/// viene validato: IProfileService e' sostituito da un fake che restituisce un admin pagoPA.
/// </summary>
[Category("FeatureFlags")]
public class FeatureFlagsLoginHttpTests
{
    private const string LoginV2 = "/api/v2/auth/pagopa/login";
    private const string LoginV1 = "/api/auth/pagopa/login";

    private sealed class FakeProfileService : IProfileService
    {
        public Task<List<AuthenticationInfo>?> GetSelfCareInfo(string? selfcareToken)
            => Task.FromResult<List<AuthenticationInfo>?>(null);

        public Task<AuthenticationInfo?> GetPagoPAInfo(string? pagoPAIdToken, string? azureADAccessToken)
            => Task.FromResult<AuthenticationInfo?>(new AuthenticationInfo
            {
                Id = "utente-admin-test",
                Ruolo = Ruolo.ADMIN,
                DescrizioneRuolo = "Amministratore",
                Profilo = "PAGOPA",
                GruppoRuolo = "gruppo-test",
                Auth = AuthType.PAGOPA,
                Email = "admin@test.invalid"
            });
    }

    private sealed class LoginFactory(IEnumerable<string> featureAccese) : ApiTestFactory(featureAccese)
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IProfileService>();
                services.AddSingleton<IProfileService, FakeProfileService>();
            });
        }
    }

    private static async Task<JsonElement> Login(string[] featureAccese, string rotta)
    {
        using var factory = new LoginFactory(featureAccese);
        var client = factory.CreateClient();
        var body = new StringContent("""{"id_token":"x","access_token":"y"}""", Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(rotta, body);
        var testo = await resp.Content.ReadAsStringAsync();
        TestContext.Out.WriteLine($"{rotta} [{string.Join(",", featureAccese)}] -> {(int)resp.StatusCode}: {testo}");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        return JsonDocument.Parse(testo).RootElement.Clone();
    }

    private static List<string> Prodotti(JsonElement profili)
        => profili.EnumerateArray().Select(p => p.GetProperty("prodotto").GetString()!).ToList();

    private static List<string> Features(JsonElement profilo)
        => profilo.GetProperty("features").EnumerateArray().Select(f => f.GetString()!).ToList();

    [Test]
    public async Task V2_TuttiAccesi_TreProfili_ConTutteLeFeature()
    {
        var profili = await Login(FeatureFlags.Tutti, LoginV2);

        Assert.Multiple(() =>
        {
            Assert.That(Prodotti(profili), Is.EqualTo(new[] { ProductRoles.pagoPA, ProductRoles.SEND, ProductRoles.AppIO }));
            foreach (var profilo in profili.EnumerateArray())
                Assert.That(Features(profilo), Is.EqualTo(FeatureFlags.Tutti), "campo JSON 'features' in camelCase");
        });
    }

    [Test]
    public async Task V2_TuttiSpenti_SenzaProfiloAppIO_FeatureVuote()
    {
        var profili = await Login([], LoginV2);

        Assert.Multiple(() =>
        {
            Assert.That(Prodotti(profili), Is.EqualTo(new[] { ProductRoles.pagoPA, ProductRoles.SEND }));
            foreach (var profilo in profili.EnumerateArray())
                Assert.That(Features(profilo), Is.Empty, "un flag spento non compare nell'elenco");
        });
    }

    [Test]
    public async Task V2_SoloAppIOSpento_SenzaProfiloAppIO_AltreFeaturePresenti()
    {
        var profili = await Login([FeatureFlags.InvioSap, FeatureFlags.InvioSapLegacy], LoginV2);

        Assert.Multiple(() =>
        {
            Assert.That(Prodotti(profili), Does.Not.Contain(ProductRoles.AppIO));
            foreach (var profilo in profili.EnumerateArray())
                Assert.That(Features(profilo), Is.EqualTo(new[] { FeatureFlags.InvioSap, FeatureFlags.InvioSapLegacy }));
        });
    }

    [Test]
    public async Task V2_SoloAppIOAcceso_ProfiloAppIOPresente()
    {
        var profili = await Login([FeatureFlags.AppIO], LoginV2);

        Assert.Multiple(() =>
        {
            Assert.That(Prodotti(profili), Does.Contain(ProductRoles.AppIO));
            Assert.That(Features(profili[0]), Is.EqualTo(new[] { FeatureFlags.AppIO }));
        });
    }

    [Test]
    public async Task V1_ProfiloSEND_ConLeFeatureAccese()
    {
        var profilo = await Login([FeatureFlags.InvioSapLegacy], LoginV1);

        Assert.Multiple(() =>
        {
            Assert.That(profilo.GetProperty("prodotto").GetString(), Is.EqualTo(ProductRoles.SEND));
            Assert.That(Features(profilo), Is.EqualTo(new[] { FeatureFlags.InvioSapLegacy }));
        });
    }
}
