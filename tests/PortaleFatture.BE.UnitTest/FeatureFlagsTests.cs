using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Modules.SEND.Auth.Extensions;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Feature flag dell'API (PF-550 APP IO, PF-735 Invio a SAP): regola "flag assente = spento",
/// lettura dalla sezione FeatureManagement e legame flag ↔ moduli. Le rotte registrate o meno sono
/// verificate end-to-end in IntegrationTest/Http/FeatureFlagsHttpTests.
/// </summary>
[Category("FeatureFlags")]
public class FeatureFlagsTests
{
    private static IFeatureManager FeatureManager(Dictionary<string, string?> config)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(config).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddFeatureManagement();
        return services.BuildServiceProvider().GetRequiredService<IFeatureManager>();
    }

    private static IFeatureManager ConUnFlag(string? valore) => FeatureManager(
        valore is null ? [] : new() { [$"FeatureManagement:{FeatureFlags.AppIO}"] = valore });

    // --- valori dell'app setting: cosa accende davvero un flag ---

    [TestCase(null, false, TestName = "Valore_Assente_Spento")]
    [TestCase("true", true)]
    [TestCase("True", true)]
    [TestCase("TRUE", true)]
    [TestCase(" true ", true, TestName = "Valore_TrueConSpazi_Acceso")]
    [TestCase("false", false)]
    [TestCase("False", false)]
    [TestCase("", false, TestName = "Valore_Vuoto_Spento")]
    [TestCase("1", false, TestName = "Valore_Uno_Spento")]
    [TestCase("0", false)]
    [TestCase("yes", false)]
    [TestCase("si", false)]
    [TestCase("on", false)]
    [TestCase("ture", false, TestName = "Valore_Refuso_Spento")]
    public async Task Valore_ShouldAccendere_SoloUnBooleanoTrue(string? valore, bool acceso)
    {
        var accese = await ConUnFlag(valore).FeatureAccese();

        Assert.That(accese.Contains(FeatureFlags.AppIO), Is.EqualTo(acceso),
            $"FeatureManagement:AppIO = {(valore is null ? "<assente>" : $"'{valore}'")}");
    }

    [Test]
    public async Task NomeFlag_ShouldEssere_CaseInsensitive()
    {
        var accese = await FeatureManager(new() { ["featuremanagement:appio"] = "true" }).FeatureAccese();

        Assert.That(accese, Is.EqualTo(new[] { FeatureFlags.AppIO }),
            "le chiavi di configurazione .NET ignorano maiuscole e minuscole, anche negli app setting");
    }

    [Test]
    public async Task VariabileAmbiente_ConDoppioUnderscore_ShouldAccendere_IlFlag()
    {
        // E' la forma dell'app setting sull'App Service: FeatureManagement__InvioSap=true.
        const string variabile = "FeatureManagement__InvioSap";
        var precedente = Environment.GetEnvironmentVariable(variabile);
        try
        {
            Environment.SetEnvironmentVariable(variabile, "true");
            var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddFeatureManagement();
            var manager = services.BuildServiceProvider().GetRequiredService<IFeatureManager>();

            Assert.That(await manager.FeatureAccese(), Does.Contain(FeatureFlags.InvioSap));
        }
        finally
        {
            Environment.SetEnvironmentVariable(variabile, precedente);
        }
    }

    [Test]
    public async Task NessunaConfigurazione_ShouldSpegnere_TuttiIFlag()
    {
        Assert.That(await FeatureManager([]).FeatureAccese(), Is.Empty);
    }

    [Test]
    public async Task FeatureAccese_ShouldIgnorare_FlagSconosciuti_EMantenere_LOrdine()
    {
        var accese = await FeatureManager(new()
        {
            ["FeatureManagement:FlagNonNostro"] = "true",
            [$"FeatureManagement:{FeatureFlags.InvioSapLegacy}"] = "true",
            [$"FeatureManagement:{FeatureFlags.AppIO}"] = "true",
        }).FeatureAccese();

        Assert.That(accese, Is.EqualTo(new[] { FeatureFlags.AppIO, FeatureFlags.InvioSapLegacy }),
            "al FE arrivano solo i flag noti, nell'ordine di FeatureFlags.Tutti");
    }

    [Test]
    public void IsFeatureEnabled_ShouldLeggere_IlFlagDalContainer()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"FeatureManagement:{FeatureFlags.InvioSap}"] = "true",
            [$"FeatureManagement:{FeatureFlags.AppIO}"] = "false",
        }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddFeatureManagement();
        var provider = services.BuildServiceProvider();

        Assert.Multiple(() =>
        {
            Assert.That(provider.IsFeatureEnabled(FeatureFlags.InvioSap), Is.True);
            Assert.That(provider.IsFeatureEnabled(FeatureFlags.AppIO), Is.False);
            Assert.That(provider.IsFeatureEnabled(FeatureFlags.InvioSapLegacy), Is.False, "assente = spento");
        });
    }

    // --- guardrail sul codice: chi aggiunge un flag o un modulo non dimentica un pezzo ---

    [Test]
    public void Tutti_ShouldContenere_OgniCostanteDiFeatureFlags()
    {
        var costanti = typeof(FeatureFlags)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!);

        Assert.Multiple(() =>
        {
            Assert.That(FeatureFlags.Tutti, Is.EquivalentTo(costanti),
                "un flag nuovo va aggiunto a Tutti: altrimenti non arriva al FE e i test non lo accendono");
            Assert.That(FeatureFlags.Tutti, Is.Unique);
        });
    }

    private static readonly Type[] Moduli = typeof(FeatureFlags).Assembly.GetTypes()
        .Where(t => t.IsSubclassOf(typeof(PortaleFatture.BE.Api.Infrastructure.Module)) && !t.IsAbstract)
        .ToArray();

    [Test]
    public void OgniModuloAppIO_ShouldEssere_SottoIlFlagAppIO()
    {
        var appIo = Moduli.Where(t => t.Namespace!.Contains(".Modules.AppIO.")).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(appIo, Is.Not.Empty);
            foreach (var modulo in appIo)
                Assert.That(modulo.GetCustomAttribute<FeatureFlagAttribute>()?.Feature, Is.EqualTo(FeatureFlags.AppIO),
                    $"{modulo.Name}: un modulo APP IO senza [FeatureFlag(FeatureFlags.AppIO)] resterebbe acceso col flag spento");
        });
    }

    /// <summary>
    /// I moduli FUORI dai flag sono la grande maggioranza e restano sempre registrati: solo i due moduli
    /// APP IO hanno l'attributo. (L'Invio a SAP, nuovo e legacy, non spegne FattureModule: ne spegne solo
    /// le rotte, dentro RegisterEndpoints.) Che le loro rotte rispondano come prima con i flag spenti lo
    /// verifica FeatureFlagsHttpTests.FlagSpenti_RotteFuoriDaiFlag_ShouldRispondere_ComeConIFlagAccesi.
    /// </summary>
    [Test]
    public void ModuliFuoriDaiFlag_ShouldEssere_TuttiTranneAppIO()
    {
        var conAttributo = Moduli.Where(t => t.GetCustomAttribute<FeatureFlagAttribute>() is not null)
            .Select(t => t.Name);
        var fuoriDaiFlag = Moduli.Where(t => t.GetCustomAttribute<FeatureFlagAttribute>() is null)
            .Select(t => t.Name).OrderBy(n => n).ToList();
        TestContext.Out.WriteLine($"Moduli fuori dai flag ({fuoriDaiFlag.Count}): {string.Join(", ", fuoriDaiFlag)}");

        Assert.Multiple(() =>
        {
            Assert.That(conAttributo, Is.EquivalentTo(new[] { "AppIoContrattiModule", "AppIoFinancialReportsModule" }),
                "un [FeatureFlag] su un altro modulo ne spegnerebbe TUTTE le rotte: se e' voluto, aggiornare questo test");
            Assert.That(fuoriDaiFlag, Does.Contain("FattureModule"),
                "FattureModule resta fuori: i flag dell'Invio a SAP spengono solo le sue rotte SAP");
            Assert.That(fuoriDaiFlag, Has.Count.EqualTo(Moduli.Length - 2));
        });
    }

    [Test]
    public void OgniFlagDeiModuli_ShouldEssere_UnFlagNoto()
    {
        var flags = Moduli.Select(t => t.GetCustomAttribute<FeatureFlagAttribute>()?.Feature).OfType<string>();

        Assert.That(flags.Distinct(), Is.SubsetOf(FeatureFlags.Tutti), "un nome scritto male spegnerebbe il modulo per sempre");
    }

    // --- profili SelfCare: nessun flag ---

    [Test]
    public void MapperSelfCare_ShouldLasciare_FeaturesNull()
    {
        var profili = new List<AuthenticationInfo>
        {
            new()
            {
                Id = "utente-ente", IdEnte = "ente-1", NomeEnte = "Ente di test", Prodotto = "prod-pn", Profilo = "PA",
                Ruolo = Ruolo.ADMIN, DescrizioneRuolo = "Amministratore", GruppoRuolo = "gruppo-test", Auth = AuthType.SELFCARE
            }
        }.MapperSelfCare(
            new IdentityUsersService(),
            new JwtTokenService("test-audience", "test-issuer", "chiave-di-test-lunga-almeno-32-caratteri!"),
            new AesEncryption("chiave-di-test-32-caratteri-1234"));

        Assert.That(profili.Single().Features, Is.Null, "i flag riguardano solo le funzioni admin");
    }
}
