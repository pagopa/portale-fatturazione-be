using System.Net;
using System.Text;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.IntegrationTest.InvioSap;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Matrice **identita' × rotta** per le sei rotte di Invio a SAP (PF-735 RF-2, RF-11: tutto e solo ADMIN).
///
/// | identita' | atteso | perche' |
/// |---|---|---|
/// | anonimo | 401 | nessuna identita' |
/// | OPERATOR (PAGOPA) | 403 | identita' riconosciuta, ruolo insufficiente: e' la novita' rispetto alle rotte legacy, aperte a OPERATOR |
/// | ADMIN con auth SELFCARE | 403 | il mondo aderente non vede la pagina, nemmeno con ruolo admin |
/// | ADMIN (PAGOPA) | non 401/403 | autorizzato: l'esito dipende dai dati, non dall'identita' |
/// | ADMIN senza nonce | non 2xx | il middleware multi-tab blocca prima dell'endpoint |
///
/// Gli incroci non toccano il DB: l'autorizzazione decide prima che l'endpoint venga invocato, quindi
/// questi test non hanno bisogno del seed ne' della DDL di DATA. Sono pero' <c>[Explicit]</c> finche' le
/// rotte non esistono: oggi risponderebbero 404 a chiunque.
/// </summary>
[Category("InvioSap")]
[Explicit("PF-965: rotte non ancora mappate (fase 2-4). Eseguire con --filter Category=InvioSap.")]
public class InvioSapAutorizzazioneHttpTests
{
    private ApiTestFactory _factory = null!;

    [OneTimeSetUp]
    public void Setup() => _factory = new ApiTestFactory();

    [OneTimeTearDown]
    public void TearDown() => _factory?.Dispose();

    /// <summary>Le sei rotte con verbo e body minimo valido per forma (l'autorizzazione non legge il body).</summary>
    public static IEnumerable<TestCaseData> Rotte()
    {
        const string griglia = """{ "anno": 2026 }""";
        const string dettaglio = """{ "annoRiferimento": 2026, "meseRiferimento": 7, "tipologiaFattura": "SECONDO SALDO", "statoInvio": "DA_INVIARE" }""";
        const string genera = """{ "anno": 2026, "mese": 7, "tipologiaFattura": "SECONDO SALDO", "idFatture": [1001], "nota": "x" }""";

        yield return new TestCaseData(HttpMethod.Post, InvioSapTestData.RottaGriglia, griglia).SetName("{m}(griglia)");
        yield return new TestCaseData(HttpMethod.Post, InvioSapTestData.RottaDettaglio, dettaglio).SetName("{m}(dettaglio)");
        yield return new TestCaseData(HttpMethod.Post, InvioSapTestData.RottaDownload, griglia).SetName("{m}(download)");
        yield return new TestCaseData(HttpMethod.Post, InvioSapTestData.RottaDownloadReinviate, griglia).SetName("{m}(download reinviate)");
        yield return new TestCaseData(HttpMethod.Put, InvioSapTestData.RottaGenera, genera).SetName("{m}(genera)");
        yield return new TestCaseData(HttpMethod.Get, $"{InvioSapTestData.RottaGenerazioni}/{Guid.NewGuid()}", null).SetName("{m}(generazioni/batchId)");
    }

    [TestCaseSource(nameof(Rotte))]
    public async Task Anonimo_ShouldRicevere401(HttpMethod metodo, string rotta, string? body)
    {
        var resp = await Invia(_factory.CreateClientAs(null), metodo, rotta, body);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [TestCaseSource(nameof(Rotte))]
    public async Task Operator_ShouldRicevere403(HttpMethod metodo, string rotta, string? body)
    {
        // E' la differenza con le rotte legacy (OPERATOR, ADMIN): qui l'OPERATOR non vede nulla (RF-11).
        var resp = await Invia(_factory.CreateClientAs(Ruolo.OPERATOR), metodo, rotta, body);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [TestCaseSource(nameof(Rotte))]
    public async Task AdminSelfCare_ShouldRicevere403(HttpMethod metodo, string rotta, string? body)
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN, auth: AuthType.SELFCARE, profilo: Profilo.PubblicaAmministrazione);
        var resp = await Invia(client, metodo, rotta, body);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden),
            "PagoPAPolicy pretende auth = PAGOPA: un admin di ente non entra nel pannello.");
    }

    [TestCaseSource(nameof(Rotte))]
    public async Task AdminPagoPA_ShouldPassareLAutorizzazione(HttpMethod metodo, string rotta, string? body)
    {
        var resp = await Invia(_factory.CreateClientAs(Ruolo.ADMIN), metodo, rotta, body);
        Assert.That((int)resp.StatusCode, Is.Not.EqualTo(401).And.Not.EqualTo(403).And.Not.EqualTo(405),
            "L'admin e' autorizzato: qualunque esito, ma non per identita' o verbo.");
    }

    [TestCaseSource(nameof(Rotte))]
    public async Task AdminSenzaNonce_ShouldEssereBloccato(HttpMethod metodo, string rotta, string? body)
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        using var req = new HttpRequestMessage(metodo, rotta); // niente WithNonce
        if (body is not null) req.Content = new StringContent(body, Encoding.UTF8, "application/json");
        var resp = await client.SendAsync(req);

        Assert.That((int)resp.StatusCode, Is.Not.InRange(200, 299),
            "Senza nonce il middleware multi-tab deve fermare la richiesta prima dell'endpoint.");
    }

    private async Task<HttpResponseMessage> Invia(HttpClient client, HttpMethod metodo, string rotta, string? body)
    {
        using var req = new HttpRequestMessage(metodo, _factory.WithNonce(rotta));
        if (body is not null) req.Content = new StringContent(body, Encoding.UTF8, "application/json");
        var resp = await client.SendAsync(req);
        TestContext.Out.WriteLine($"{metodo} {rotta} -> {(int)resp.StatusCode} {resp.StatusCode}");
        return resp;
    }
}
