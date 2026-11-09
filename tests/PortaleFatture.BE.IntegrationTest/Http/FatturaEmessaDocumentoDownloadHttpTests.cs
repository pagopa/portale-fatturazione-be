using System.Net;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Download del documento di dettaglio di una fattura EMESSA (singola o multipla, HTML o PDF):
///  - GET api/fatture/emesse/dettaglio/download/{id}?idEnte=…  (admin, PagoPAPolicy)
///  - GET api/fatture/ente/emesse/dettaglio/download/{id}      (aderente, SelfCarePolicy)
///
/// ⚠️ Copertura PARZIALE, di proposito. La query legge <c>pfd.vwFatturaEmessaDownloadPDF</c>
/// (FattureQueryRicercaBuilder.SelectDettaglioEmessoPdf), che NON e' nel DB seedato. Per regola la
/// vista non si ricostruisce a mano: serve la DDL reale da DATA (v. docs/test-integrazione-db-seedato.md).
/// Nel frattempo:
///  - i controlli che scattano PRIMA della query (400 senza idEnte, 401, 403) sono verdi;
///  - i test che leggono la vista restano GIALLI (TestDb.SkipSeOggettoAssente) e diventano verdi
///    da soli quando la vista entra nel seed — a quel punto vanno aggiunti i casi con dati
///    (PAC/PAL, singola/multipla, ANTICIPO/ACCONTO -> 404, fattura di un altro ente).
/// La generazione del documento (template, segnaposto, PAC/PAL, multiplo) e' gia' coperta a unit
/// da DocumentBuilderTests.
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
[TestFixture]
[Category("Fatture")]
public class FatturaEmessaDocumentoDownloadHttpTests
{
    private const string Vista = "pfd.vwFatturaEmessaDownloadPDF";
    private const string Ente = "11111111-1111-1111-1111-111111111111";

    private ApiTestFactory _factory = null!;

    [OneTimeSetUp]
    public void Setup() => _factory = new ApiTestFactory();

    [OneTimeTearDown]
    public void TearDown() => _factory?.Dispose();

    [SetUp]
    public void CheckDb() => TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);

    // --- controlli prima della query: verdi anche senza la vista ------------------------------------

    [Test]
    public async Task Admin_SenzaIdEnte_400()
    {
        var resp = await _factory.CreateClientAs(Ruolo.ADMIN)
            .GetAsync(_factory.WithNonce("/api/fatture/emesse/dettaglio/download/8001"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Admin_SuRottaAderente_403()
    {
        var resp = await _factory.CreateClientAs(Ruolo.ADMIN)
            .GetAsync(_factory.WithNonce("/api/fatture/ente/emesse/dettaglio/download/8001"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Aderente_SuRottaAdmin_403()
    {
        var resp = await _factory.CreateClientAs(Ruolo.ADMIN, Ente, AuthType.SELFCARE, Profilo.PubblicaAmministrazione)
            .GetAsync(_factory.WithNonce("/api/fatture/emesse/dettaglio/download/8001", Ente) + $"&idEnte={Ente}");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task SenzaAutenticazione_401()
    {
        var anonimo = _factory.CreateClientAs(null);

        Assert.Multiple(async () =>
        {
            Assert.That((await anonimo.GetAsync(_factory.WithNonce("/api/fatture/emesse/dettaglio/download/8001") + $"&idEnte={Ente}")).StatusCode,
                Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That((await anonimo.GetAsync(_factory.WithNonce("/api/fatture/ente/emesse/dettaglio/download/8001"))).StatusCode,
                Is.EqualTo(HttpStatusCode.Unauthorized));
        });
    }

    // --- casi che leggono la vista: gialli finche' manca la DDL ---------------------------------

    [Test]
    public async Task FatturaInesistente_404()
    {
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, Vista);

        var resp = await _factory.CreateClientAs(Ruolo.ADMIN)
            .GetAsync(_factory.WithNonce("/api/fatture/emesse/dettaglio/download/99799") + $"&idEnte={Ente}");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Aderente_FatturaInesistente_404()
    {
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, Vista);

        var resp = await _factory.CreateClientAs(Ruolo.ADMIN, Ente, AuthType.SELFCARE, Profilo.PubblicaAmministrazione)
            .GetAsync(_factory.WithNonce("/api/fatture/ente/emesse/dettaglio/download/99799", Ente));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
