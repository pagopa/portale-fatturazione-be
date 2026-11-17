using System.Net;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Accesso alla **Integration API containerizzata** (Function.API, profilo `integrationapi` di
/// tests/docker-compose.yml), gemello di <see cref="FunctionHost"/> per la SendEmailFunction: se
/// l'host non risponde i test si **ignorano**, non falliscono.
///
/// Si alza da tests/ con `docker compose --profile integrationapi up -d --build`.
///
/// L'host gira in Release, quindi NON ha il ripiego di sviluppo su 127.0.0.1: l'indirizzo del
/// chiamante arriva solo dalle intestazioni inoltrate (`X-Forwarded-For`), come dietro l'Application
/// Gateway. I test le impostano esplicitamente.
/// </summary>
public static class IntegrationApiHost
{
    public const string Default = "http://localhost:8081";

    /// <summary>La stessa chiave AES del servizio `integrationapi` nel compose e di ServiceProvider: di test, non un segreto.</summary>
    public const string AesKey = "chiave-di-test-32-caratteri-1234";

    public const string EnteAbilitato = "11111111-1111-1111-1111-111111111111";
    public const string EnteNonAbilitato = "22222222-2222-2222-2222-222222222222";

    /// <summary>Rete 198.51.100.0/24 (TEST-NET-2, riservata alla documentazione): non collide con i test su 203.0.113.x.</summary>
    public const string IpAutorizzato = "198.51.100.77";
    public const string IpEstraneo = "198.51.100.200";

    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(60) };
    private static readonly AesEncryption Aes = new(AesKey);

    public static string BaseUrl =>
        ServiceProvider.GetRequiredService<IConfiguration>()["IntegrationTest:IntegrationApiHostUrl"] ?? Default;

    /// <summary>
    /// Sonda senza effetti collaterali: la rotta di autenticazione chiamata senza chiave risponde 401
    /// per costruzione. Un 404 significa che l'host e' su ma le function non sono state indicizzate.
    /// </summary>
    public static void SkipIfUnavailable()
    {
        HttpResponseMessage risposta;
        try
        {
            risposta = Client.GetAsync($"{BaseUrl}/api/v1/authentication").GetAwaiter().GetResult();
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            Assert.Ignore(
                "Integration API non raggiungibile su " + BaseUrl + ": la suite e' stata saltata, non fallita. "
                + "Avviarla da tests/ con  docker compose --profile integrationapi up -d --build. Dettaglio: " + e.Message);
            return;
        }

        if (risposta.StatusCode == HttpStatusCode.NotFound)
            Assert.Ignore("L'host risponde ma le function non risultano indicizzate: controllare i log del container.");
    }

    public static Task<HttpResponseMessage> Invia(
        HttpMethod metodo, string percorso, string? apiKey = null, string? forwardedFor = null,
        string? customForwardedFor = null, string? corpo = null)
    {
        var richiesta = new HttpRequestMessage(metodo, $"{BaseUrl}{percorso}");
        if (apiKey is not null) richiesta.Headers.TryAddWithoutValidation("X-API-KEY", apiKey);
        if (forwardedFor is not null) richiesta.Headers.TryAddWithoutValidation("X-Forwarded-For", forwardedFor);
        if (customForwardedFor is not null) richiesta.Headers.TryAddWithoutValidation("Custom-Forwarded-For", customForwardedFor);
        if (corpo is not null) richiesta.Content = new StringContent(corpo, System.Text.Encoding.UTF8, "application/json");
        return Client.SendAsync(richiesta);
    }

    // ------------------------------------------------------------------ dati di prova

    /// <summary>
    /// Inserisce una chiave attiva per l'ente. Chiavi e IP sono CIFRATI a riposo (v. tests/Data/api_keys.sql):
    /// si scrive il ciphertext, con la stessa chiave AES dell'host.
    /// </summary>
    public static void InserisciChiave(string idEnte, string apiKeyInChiaro)
    {
        using var db = new SqlConnection(LocalTestDb.ConnectionString);
        db.Execute(
            "INSERT INTO pfw.ApiKeys (FkIdEnte, ApiKey, DataCreazione, Attiva) VALUES (@idEnte, @chiave, GETDATE(), 1)",
            new { idEnte, chiave = Aes.EncryptString(apiKeyInChiaro) });
    }

    public static void InserisciIp(string idEnte, string ipInChiaro)
    {
        using var db = new SqlConnection(LocalTestDb.ConnectionString);
        db.Execute(
            "INSERT INTO pfw.ApiKeysIPs (FkIdEnte, DataCreazione, IPAddress) VALUES (@idEnte, GETDATE(), @ip)",
            new { idEnte, ip = Aes.EncryptString(ipInChiaro) });
    }

    /// <summary>Rimuove solo cio' che i test hanno inserito, cercandolo per ciphertext.</summary>
    public static void Pulisci(IEnumerable<string> chiaviInChiaro, IEnumerable<string> ipInChiaro)
    {
        using var db = new SqlConnection(LocalTestDb.ConnectionString);
        db.Execute("DELETE FROM pfw.ApiKeys WHERE ApiKey IN @chiavi", new { chiavi = chiaviInChiaro.Select(Aes.EncryptString).ToArray() });
        db.Execute("DELETE FROM pfw.ApiKeysIPs WHERE IPAddress IN @ip", new { ip = ipInChiaro.Select(Aes.EncryptString).ToArray() });
    }
}
