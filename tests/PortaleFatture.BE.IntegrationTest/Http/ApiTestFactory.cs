using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Modules.SEND.Auth.Extensions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Service;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Harness HTTP: avvia l'API in-memory (WebApplicationFactory) e fornisce un HttpClient che attraversa
/// TUTTA la pipeline reale — routing, authorization, model binding, validazione, endpoint, handler, DB.
///
/// Differenze rispetto all'ambiente reale (volute):
///  - DB: punta al container locale seeded (LocalTestDb), non a UAT;
///  - Autenticazione: schema fittizio TestAuthHandler (ruolo via header), cosi' si possono testare
///    davvero [Authorize(Roles=...)] e le policy senza token reali.
///
/// Prerequisito: container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
public class ApiTestFactory : WebApplicationFactory<Program>
{
    private readonly HashSet<string> _featureAccese;

    /// <param name="featureAccese">
    /// true (default) accende tutti i feature flag (<see cref="FeatureFlags"/>), cosi' le suite esistenti
    /// vedono le rotte di APP IO e dell'Invio a SAP, nuovo e legacy. false li spegne tutti: serve ai test
    /// che verificano che con il flag spento le rotte non esistano (v. FeatureFlagsHttpTests).
    /// </param>
    public ApiTestFactory(bool featureAccese = true)
        : this(featureAccese && !FixtureConFlagSpenti() ? FeatureFlags.Tutti : []) { }

    /// <summary>
    /// Proprieta' NUnit che spegne i flag in ogni factory creata senza un elenco esplicito, dentro la
    /// fixture che la porta: e' cosi' che le classi di Http/FlagSpentiHttpTests.cs rieseguono le suite
    /// fuori dai flag con i flag spenti, senza variabili d'ambiente (funziona anche da Esplora test).
    /// </summary>
    public const string ProprietaFlagSpenti = "FeatureFlagSpenti";

    private static bool FixtureConFlagSpenti()
    {
        for (ITest? test = TestExecutionContext.CurrentContext?.CurrentTest; test is not null; test = test.Parent)
            if (test.Properties.ContainsKey(ProprietaFlagSpenti))
                return true;
        return false;
    }

    /// <param name="featureAccese">Flag da accendere; gli altri valgono false.</param>
    public ApiTestFactory(IEnumerable<string> featureAccese) => _featureAccese = [.. featureAccese];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Config minima: la connection string punta al DB seeded locale.
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PortaleFattureOptions:ConnectionString"] = LocalTestDb.ConnectionString,
                ["PortaleFattureOptions:FattureSchema"] = "pfw",
                ["PortaleFattureOptions:SelfCareSchema"] = "pfd",
            });
            // Valore esplicito per OGNI flag, anche quando li si vuole spenti: in Development l'app legge gli
            // User Secrets del progetto API, e un flag acceso li' accenderebbe le rotte anche nei test "spenti".
            cfg.AddInMemoryCollection(FeatureFlags.Tutti.ToDictionary(
                flag => $"FeatureManagement:{flag}",
                flag => (string?)(_featureAccese.Contains(flag) ? "true" : "false")));
        });

        builder.ConfigureTestServices(services =>
        {
            // 1) DB: sostituisce le factory con quelle che puntano al container locale.
            services.RemoveAll<IFattureDbContextFactory>();
            services.RemoveAll<ISelfCareDbContextFactory>();
            services.RemoveAll<IDbContextFactory>();
            services.AddSingleton<IFattureDbContextFactory>(new DbContextFactory(LocalTestDb.ConnectionString, "pfw"));
            services.AddSingleton<ISelfCareDbContextFactory>(new DbContextFactory(LocalTestDb.ConnectionString, "pfd"));
            services.AddSingleton<IDbContextFactory>(new DbContextFactory(LocalTestDb.ConnectionString, "pfw"));

            // 1b) Synapse: nessun test deve avviare una pipeline reale. Il fake registra le chiamate e
            //     permette di simulare il fallimento dell'avvio (v. FakeSynapseService).
            services.RemoveAll<ISynapseService>();
            services.AddSingleton<ISynapseService>(FakeSynapseService.Instance);

            // 1c) Blob storage dei financial report APP IO: nessun test deve chiamare Azure.
            services.RemoveAll<PortaleFatture.BE.Infrastructure.Gateway.Storage.AppIO.IAppIoFinancialReportStorageService>();
            services.AddSingleton<PortaleFatture.BE.Infrastructure.Gateway.Storage.AppIO.IAppIoFinancialReportStorageService>(FakeAppIoFinancialReportStorageService.Instance);

            // 2) Autenticazione di test come schema di default (sostituisce JWT/api-key).
            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                o.DefaultChallengeScheme = TestAuthHandler.Scheme;
                o.DefaultScheme = TestAuthHandler.Scheme;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });
        });
    }

    /// <summary>
    /// Client che invia le richieste con il ruolo indicato (null = anonimo).
    ///
    /// `auth` e `profilo` servono alle rotte lato ADERENTE: SelfCarePolicy pretende auth = SELFCARE
    /// **piu'** un profilo fra quelli ammessi (PA, GSP, SCP, …). Lasciandoli al default si ottiene
    /// l'identita' admin (PAGOPA), che su quelle rotte prende 403 — ed e' il comportamento voluto,
    /// verificato da Http/PolicyAuthorizationHttpTests.
    /// </summary>
    public HttpClient CreateClientAs(string? ruolo, string? idEnte = null, string? auth = null, string? profilo = null)
    {
        var client = CreateClient();
        if (ruolo is not null)
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, ruolo);
        if (idEnte is not null)
            client.DefaultRequestHeaders.Add(TestAuthHandler.IdEnteHeader, idEnte);
        if (auth is not null)
            client.DefaultRequestHeaders.Add(TestAuthHandler.AuthHeader, auth);
        if (profilo is not null)
            client.DefaultRequestHeaders.Add(TestAuthHandler.ProfiloHeader, profilo);
        return client;
    }

    /// <summary>
    /// Nonce valido per il NonceMultiTabsMiddleware: ogni rotta non-whitelist pretende in query string
    /// un nonce cifrato AES che, decifrato, combaci con Id/IdEnte/Prodotto dell'identity autenticata.
    /// Lo generiamo con lo stesso IAesEncryption dell'app, sui valori emessi da TestAuthHandler.
    /// </summary>
    public string Nonce(string? idEnte = null, string prodotto = "prod-pn")
    {
        var encryption = Services.GetRequiredService<IAesEncryption>();
        var payload = new NonceDto
        {
            Id = "integration-test-user",
            IdEnte = idEnte ?? "11111111-1111-1111-1111-111111111111",
            Prodotto = prodotto
        }.Serialize();
        return encryption.EncryptString(payload)!;
    }

    /// <summary>Rotta con il nonce gia' agganciato in query string.</summary>
    public string WithNonce(string route, string? idEnte = null, string prodotto = "prod-pn")
        => $"{route}?nonce={Uri.EscapeDataString(Nonce(idEnte, prodotto))}";
}
