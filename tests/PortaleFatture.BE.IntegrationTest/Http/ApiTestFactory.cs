using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.Auth.Extensions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
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

            // 2) Autenticazione di test come schema di default (sostituisce JWT/api-key).
            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                o.DefaultChallengeScheme = TestAuthHandler.Scheme;
                o.DefaultScheme = TestAuthHandler.Scheme;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });
        });
    }

    /// <summary>Client che invia le richieste con il ruolo indicato (null = anonimo).</summary>
    public HttpClient CreateClientAs(string? ruolo, string? idEnte = null)
    {
        var client = CreateClient();
        if (ruolo is not null)
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, ruolo);
        if (idEnte is not null)
            client.DefaultRequestHeaders.Add(TestAuthHandler.IdEnteHeader, idEnte);
        return client;
    }

    /// <summary>
    /// Nonce valido per il NonceMultiTabsMiddleware: ogni rotta non-whitelist pretende in query string
    /// un nonce cifrato AES che, decifrato, combaci con Id/IdEnte/Prodotto dell'identity autenticata.
    /// Lo generiamo con lo stesso IAesEncryption dell'app, sui valori emessi da TestAuthHandler.
    /// </summary>
    public string Nonce(string? idEnte = null)
    {
        var encryption = Services.GetRequiredService<IAesEncryption>();
        var payload = new NonceDto
        {
            Id = "integration-test-user",
            IdEnte = idEnte ?? "11111111-1111-1111-1111-111111111111",
            Prodotto = "prod-pn"
        }.Serialize();
        return encryption.EncryptString(payload)!;
    }

    /// <summary>Rotta con il nonce gia' agganciato in query string.</summary>
    public string WithNonce(string route, string? idEnte = null)
        => $"{route}?nonce={Uri.EscapeDataString(Nonce(idEnte))}";
}
