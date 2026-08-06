using System.Net;
using System.Text.Json;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Test HTTP su GET api/rel/pagopa/{id} — il dettaglio di una REL, unica rotta che legge
/// [be].[vwRelDettaglio] (v. docs/viste-endpoint.md). Copre BE-SMOKE-02 del testbook backend.
///
/// L'id di rotta e' la RelTestataKey serializzata: {IdEnte}_{IdContratto}_{Tipologia}_{Anno}_{Mese},
/// con gli spazi della tipologia sostituiti da trattini.
///
/// Oltre al caso felice, i due test di caratterizzazione riproducono le due condizioni gia' segnalate
/// come "da tenere d'occhio" sulla vista: entrambe fanno restituire alla vista un numero di righe
/// diverso da 1, e RelTestataQueryGetByIdPersistence usa SingleAsync -> 500, non 404. La scelta di
/// restare su SingleAsync e' deliberata (per una chiave valida il dettaglio DEVE esistere: 0 o >1
/// righe sono anomalie dati reali) — questi test servono a far vedere il sintomo, non a proporre un
/// 404 al suo posto.
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
public class RelDettaglioHttpTests
{
    private const string Ente1 = "11111111-1111-1111-1111-111111111111";
    private const string EnteFanout = "99999999-9999-9999-9999-999999999999";

    private ApiTestFactory _factory;

    [OneTimeSetUp]
    public void Setup() => _factory = new ApiTestFactory();

    [OneTimeTearDown]
    public void TearDown() => _factory?.Dispose();

    [SetUp]
    public void CheckDb() => TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);

    private async Task<HttpResponseMessage> Get(string idTestata)
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var resp = await client.GetAsync(_factory.WithNonce($"/api/rel/pagopa/{idTestata}"));
        TestContext.Out.WriteLine($"{idTestata} -> STATUS: {(int)resp.StatusCode} {resp.StatusCode}");
        return resp;
    }

    [Test]
    public async Task Dettaglio_PeriodoCompleto_ShouldReturn200_ConStorniNegativi()
    {
        // Seed 2026/5 PRIMO SALDO: 4 righe di storno (100/50/30/20) + una riga di consumo che NON
        // deve entrare nei bucket. La vista moltiplica per -1, quindi gli storni escono negativi.
        var resp = await Get($"{Ente1}_TOKEN-E1_PRIMO-SALDO_2026_5");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var r = doc.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(Dec(r, "anticipo_StornoAnalogico"), Is.EqualTo(-100m), "STORNO ANTICIPO NA -> bucket anticipo analogico");
            Assert.That(Dec(r, "anticipo_StornoDigitale"), Is.EqualTo(-50m), "STORNO ANTICIPO ND -> bucket anticipo digitale");
            Assert.That(Dec(r, "acconto_StornoAnalogico"), Is.EqualTo(-30m), "STORNO ACCONTO NA -> bucket acconto analogico");
            Assert.That(Dec(r, "acconto_StornoDigitale"), Is.EqualTo(-20m), "STORNO ACCONTO ND -> bucket acconto digitale");
            Assert.That(Dec(r, "anticipo_StornoTotale"), Is.EqualTo(-150m));
            Assert.That(Dec(r, "acconto_StornoTotale"), Is.EqualTo(-50m));
            Assert.That(Dec(r, "stornoTotale"), Is.EqualTo(-200m),
                "La riga di consumo (MAT-A, 600) non deve finire negli storni.");
            Assert.That(Dec(r, "totale"), Is.EqualTo(1000m), "Totale della RelTestata, indipendente dagli storni.");
        });
    }

    [Test]
    public async Task Dettaglio_ChiaveInesistente_ShouldReturn500_Caratterizzazione()
    {
        // Nessuna riga da nessuna parte: la vista non restituisce nulla e SingleAsync solleva.
        // Documenta che una chiave sbagliata NON produce un 404 leggibile dal client.
        var resp = await Get($"{Ente1}_TOKEN-E1_PRIMO-SALDO_1999_1");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError),
            "Chiave inesistente -> 0 righe -> SingleAsync -> 500 (scelta deliberata, v. commento di classe).");
    }

    [Test]
    public async Task Dettaglio_PeriodoSenzaStaging_ShouldReturn500_Caratterizzazione()
    {
        // Seed 2026/9: la RelTestata C'E', ma non esiste alcuna riga in MesiFatture/TmpFatture*.
        // E' il caso "periodo storico con lo staging ripulito": TotaliCumulati e' un INNER JOIN sulle
        // tabelle temporanee, quindi la REL sparisce dalla vista pur essendo a DB. E' la stessa forma
        // dell'incidente del 29/07 (allora il filtro escludeva le tipologie diverse da PRIMO SALDO).
        var resp = await Get($"{Ente1}_TOKEN-E1_PRIMO-SALDO_2026_9");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError),
            "REL esistente ma senza staging -> la vista non la vede -> 500. Regressione dell'incidente noto.");
    }

    [Test]
    public async Task Dettaglio_EnteConPiuDatiFatturazione_ShouldReturn500_Caratterizzazione()
    {
        // Seed 2026/10 su ente dedicato con DUE righe in pfw.DatiFatturazione: il LEFT JOIN della
        // vista e' solo su FkIdEnte (senza anno/mese/tipologia), quindi duplica la riga di dettaglio.
        // Con >1 righe SingleAsync solleva -> 500. E' il secondo punto "da tenere d'occhio" della vista.
        var resp = await Get($"{EnteFanout}_TOKEN-E9_PRIMO-SALDO_2026_10");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError),
            "Fan-out del LEFT JOIN su DatiFatturazione -> 2 righe -> 500.");
    }

    [Test]
    public async Task Dettaglio_SenzaAutenticazione_ShouldReturn401()
    {
        var client = _factory.CreateClientAs(null);
        var resp = await client.GetAsync(_factory.WithNonce($"/api/rel/pagopa/{Ente1}_TOKEN-E1_PRIMO-SALDO_2026_5"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>Legge una proprieta' decimal dal JSON, tollerante al casing della serializzazione.</summary>
    private static decimal Dec(JsonElement root, string nome)
    {
        foreach (var p in root.EnumerateObject())
            if (string.Equals(p.Name, nome, StringComparison.OrdinalIgnoreCase))
                return p.Value.GetDecimal();

        Assert.Fail($"Proprieta' '{nome}' assente dalla risposta.");
        return 0m;
    }
}
