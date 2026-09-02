using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Rotte `api/apikey/*` attraverso la pipeline reale. Complementano ApiKeysCommandIntegrationTests,
/// che invocano gli handler via MediatR e quindi **saltano routing, [Authorize] e model binding**:
/// qui si verifica il contratto come lo vede il portale dell'aderente.
///
/// Tutte queste rotte sono `SelfCarePolicy` + ruolo ADMIN: le chiama l'ADERENTE, non l'operatore
/// interno. Serve quindi un client con auth = SELFCARE e un profilo ammesso — un token admin qui
/// deve prendere 403, ed e' uno dei casi sotto.
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
public class ApiKeysHttpTests
{
    private const string EnteAbilitato = "11111111-1111-1111-1111-111111111111";
    private const string EnteNonAbilitato = "22222222-2222-2222-2222-222222222222";
    private const string Ip = "203.0.113.77";

    private ApiTestFactory _factory;

    [OneTimeSetUp]
    public void Setup() => _factory = new ApiTestFactory();

    [OneTimeTearDown]
    public void TearDown() => _factory?.Dispose();

    [SetUp]
    public void Reset()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        Pulisci();
    }

    [TearDown]
    public void Cleanup() => Pulisci();

    /// <summary>Client dell'aderente: auth SELFCARE + profilo ammesso dalla policy.</summary>
    private HttpClient ClientAderente(string idEnte = EnteAbilitato)
        => _factory.CreateClientAs(Ruolo.ADMIN, idEnte, AuthType.SELFCARE, Profilo.PubblicaAmministrazione);

    [Test]
    public async Task PostIps_AderenteAbilitato_ShouldReturn200()
    {
        var resp = await ClientAderente().PostAsJsonAsync(
            _factory.WithNonce("/api/apikey/ips"), new { ipAddress = Ip });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await resp.Content.ReadAsStringAsync(), Does.Contain("true").IgnoreCase);
    }

    [Test]
    public async Task PostIps_Duplicato_ShouldReturn400()
    {
        await ClientAderente().PostAsJsonAsync(_factory.WithNonce("/api/apikey/ips"), new { ipAddress = Ip });

        var resp = await ClientAderente().PostAsJsonAsync(
            _factory.WithNonce("/api/apikey/ips"), new { ipAddress = Ip });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
            "Il -1 del command (violazione di UQ_IPAddress) deve arrivare al client come 400, non come 200 con false.");
    }

    [Test]
    public async Task PostIps_IndirizzoNonValido_ShouldReturn400()
    {
        var resp = await ClientAderente().PostAsJsonAsync(
            _factory.WithNonce("/api/apikey/ips"), new { ipAddress = "non-un-ip" });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
            "La validazione VerifyIp deve fermare l'indirizzo prima del DB (v. ApiKeysVerifyIpTests).");
    }

    [Test]
    public async Task DeleteIps_Esistente_ShouldReturn200()
    {
        await ClientAderente().PostAsJsonAsync(_factory.WithNonce("/api/apikey/ips"), new { ipAddress = Ip });

        var richiesta = new HttpRequestMessage(HttpMethod.Delete, _factory.WithNonce("/api/apikey/ips"))
        {
            Content = JsonContent.Create(new { ipAddress = Ip })
        };
        var resp = await ClientAderente().SendAsync(richiesta);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task GetIps_ShouldRispondereSenzaErroriServer()
    {
        var resp = await ClientAderente().GetAsync(_factory.WithNonce("/api/apikey/ips"));

        // 200 con la lista, oppure 404 se l'aderente non ha ancora IP: entrambi contratti validi.
        Assert.That(resp.StatusCode, Is.AnyOf(HttpStatusCode.OK, HttpStatusCode.NotFound));
    }

    [Test]
    public async Task PostGenera_AderenteAbilitato_ShouldReturn200()
    {
        var resp = await ClientAderente().PostAsJsonAsync(
            _factory.WithNonce("/api/apikey/genera"), new { attiva = true });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(ChiaviDiEnte(EnteAbilitato), Is.EqualTo(1));
    }

    [Test]
    public async Task PostGenera_AderenteNonAbilitato_ShouldReturn401()
    {
        // L'ente non è in pfw.EntiApiKeys con attiva=1: il command solleva SecurityException, che il
        // gestore globale traduce in 401. Non è un problema di token — è un ente non abilitato alle
        // Integration API, e il codice di stato non lo distingue.
        var resp = await ClientAderente(EnteNonAbilitato).PostAsJsonAsync(
            _factory.WithNonce("/api/apikey/genera", EnteNonAbilitato), new { attiva = true });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task PostIps_ConTokenAdmin_ShouldReturn403()
    {
        // Le API key sono self-service dell'aderente: un'utenza interna pagoPA non deve poterle toccare.
        var resp = await _factory.CreateClientAs(Ruolo.ADMIN).PostAsJsonAsync(
            _factory.WithNonce("/api/apikey/ips"), new { ipAddress = Ip });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task PostIps_SenzaAutenticazione_ShouldReturn401()
    {
        var resp = await _factory.CreateClientAs(null).PostAsJsonAsync(
            _factory.WithNonce("/api/apikey/ips"), new { ipAddress = Ip });

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private static int ChiaviDiEnte(string idEnte)
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM pfw.ApiKeys WHERE FkIdEnte = @ente";
        cmd.Parameters.AddWithValue("@ente", idEnte);
        return (int)cmd.ExecuteScalar()!;
    }

    private static void Pulisci()
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
DELETE FROM pfw.ApiKeysIPs WHERE FkIdEnte IN (@e1, @e2);
DELETE FROM pfw.ApiKeys    WHERE FkIdEnte IN (@e1, @e2);
DELETE FROM pfw.Log        WHERE IdUtente = 'integration-test-user';";
        cmd.Parameters.AddWithValue("@e1", EnteAbilitato);
        cmd.Parameters.AddWithValue("@e2", EnteNonAbilitato);
        cmd.ExecuteNonQuery();
    }
}
