using System.Net;
using System.Text;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Test HTTP end-to-end sul download gestione-fatture (POST /api/fatture/pagopa/gestione-fatture/download),
/// attraverso la pipeline reale — dove viveva il bug del 400.
///
/// Il FE, quando non ci sono filtri selezionati, invia un corpo con array vuoti e "anno": null. Con
/// Anno dichiarato int (non-nullable), il model binding di System.Text.Json non converte null nel value
/// type e la rotta risponde 400. Dopo il fix (Anno int?) il binding passa e la rotta risponde
/// applicativamente (200 con dati / 404 se il periodo e' vuoto), MAI 400.
///
/// A differenza degli unit test sull'endpoint (che invocano il metodo via reflection e BYPASSANO il
/// binding), qui il JSON viene deserializzato dalla pipeline vera: e' l'unico punto in cui il 400 e'
/// realmente riproducibile. Gira sul DB seedato (LocalTestDb via ApiTestFactory).
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
public class GestioneFattureDownloadHttpTests
{
    private const string Rotta = "/api/fatture/pagopa/gestione-fatture/download";

    private ApiTestFactory _factory;

    [OneTimeSetUp]
    public void Setup() => _factory = new ApiTestFactory();

    [OneTimeTearDown]
    public void TearDown() => _factory?.Dispose();

    private async Task<HttpResponseMessage> Post(string body)
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(_factory.WithNonce(Rotta), content);
        TestContext.Out.WriteLine($"STATUS: {(int)resp.StatusCode} {resp.StatusCode}");
        return resp;
    }

    // Payload REALE del FE "nessun filtro": array vuoti + "anno": null. E' quello che dava 400.
    [Test]
    public async Task Download_SenzaFiltri_AnnoNull_ShouldNotReturn400()
    {
        var resp = await Post("""
        {
            "idEnti": [],
            "tipologiaContratto": null,
            "tipologiaFattura": null,
            "anno": null,
            "mesi": [],
            "azione": null
        }
        """);

        Assert.That(resp.StatusCode, Is.Not.EqualTo(HttpStatusCode.BadRequest),
            "Il payload FE con 'anno': null e array vuoti NON deve piu' dare 400 nel binding (fix Anno int?).");
        // sanity: la rotta e' stata raggiunta come ADMIN, non e' un problema di autorizzazione
        Assert.That(resp.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(resp.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden));
    }

    // "Con parametri": deve continuare a passare il binding (non 400).
    [Test]
    public async Task Download_ConFiltri_ShouldNotReturn400()
    {
        var resp = await Post("""
        {
            "anno": 2025,
            "tipologiaFattura": "SECONDO SALDO"
        }
        """);

        Assert.That(resp.StatusCode, Is.Not.EqualTo(HttpStatusCode.BadRequest));
    }
}
