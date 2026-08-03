using System.Net;
using System.Text;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Test HTTP end-to-end sugli endpoint "form di aggiunta" della Gestione Fatture: modifica/anni e
/// modifica/mesi (leggono vwGestioneFattureFormAnniMesi, con Azione all'IMPERATIVO POSTICIPA/ELIMINA).
/// A livello query erano gia' coperti (GestioneFattureModificaQueryIntegrationTests); qui si esercitano
/// [Authorize] + model binding + nonce via pipeline reale.
///
/// Body = GestioneFattureModificaRequest { anno (stringa), tipologiaFattura, azione }. Richiede il
/// container di test attivo.
/// </summary>
public class GestioneFattureModificaHttpTests
{
    private const string RottaAnni = "/api/fatture/pagopa/gestione-fatture/modifica/anni";
    private const string RottaMesi = "/api/fatture/pagopa/gestione-fatture/modifica/mesi";

    private ApiTestFactory _factory;

    [OneTimeSetUp]
    public void Setup() => _factory = new ApiTestFactory();

    [OneTimeTearDown]
    public void TearDown() => _factory?.Dispose();

    private async Task<HttpResponseMessage> Post(string rotta, string body, string? ruolo = Ruolo.ADMIN)
    {
        var client = _factory.CreateClientAs(ruolo);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(_factory.WithNonce(rotta), content);
        TestContext.Out.WriteLine($"STATUS: {(int)resp.StatusCode} {resp.StatusCode}");
        return resp;
    }

    [Test]
    public async Task ModificaAnni_SenzaAutenticazione_ShouldReturn401()
    {
        var resp = await Post(RottaAnni, """{ "azione": "POSTICIPA", "tipologiaFattura": "SECONDO SALDO" }""", ruolo: null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ModificaMesi_SenzaAutenticazione_ShouldReturn401()
    {
        var resp = await Post(RottaMesi, """{ "azione": "POSTICIPA", "tipologiaFattura": "SECONDO SALDO", "anno": "2025" }""", ruolo: null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ModificaAnni_Admin_BodyValido_ShouldNotReturn400()
    {
        var resp = await Post(RottaAnni, """{ "azione": "POSTICIPA", "tipologiaFattura": "SECONDO SALDO" }""");
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.Not.EqualTo(HttpStatusCode.BadRequest), "Il binding di un body valido non deve dare 400.");
            Assert.That(resp.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(resp.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden));
        });
    }

    [Test]
    public async Task ModificaMesi_Admin_BodyValido_ShouldNotReturn400()
    {
        var resp = await Post(RottaMesi, """{ "azione": "POSTICIPA", "tipologiaFattura": "SECONDO SALDO", "anno": "2025" }""");
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.Not.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(resp.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized));
        });
    }
}
