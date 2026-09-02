using System.Net;
using System.Text;
using System.Text.Json;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Test HTTP end-to-end su POST /api/fatture (PostFattureByRicercaAsync) attraverso la pipeline reale
/// — routing + [Authorize] + model binding + nonce + handler + DB seedato. E' l'unico punto dove il
/// contratto verso il client e' esercitato davvero: gli integration test invocano l'handler e BYPASSANO
/// binding/auth. Copre il path del 404 su Cancellata=true (Non Fatturate) e il binding del FE (mese stringa).
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
public class FattureRicercaHttpTests
{
    private const string Rotta = "/api/fatture";
    private ApiTestFactory _factory;

    [OneTimeSetUp]
    public void Setup() => _factory = new ApiTestFactory();

    [OneTimeTearDown]
    public void TearDown() => _factory?.Dispose();

    private async Task<HttpResponseMessage> Post(string body, string? ruolo = Ruolo.ADMIN)
    {
        var client = _factory.CreateClientAs(ruolo);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(_factory.WithNonce(Rotta), content);
        TestContext.Out.WriteLine($"STATUS: {(int)resp.StatusCode} {resp.StatusCode}");
        return resp;
    }

    [Test]
    public async Task NonFatturate_ConDati_2024_2_ShouldReturn200_AndBody()
    {
        var resp = await Post("""{ "cancellata": true, "anno": 2024, "mese": 2 }""");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("5001").Or.Contain("5002"),
            "Il corpo deve contenere le Non Fatturate (Eliminate) del periodo 2024/2.");
    }

    // =============================================================================================
    // `PF-672 TD-21`: col filtro **Stato = "Non Fatturate"** la griglia deve mostrare **sia** le
    // posticipate **sia** le eliminate.
    //
    // Sul backend è `api/fatture` con `cancellata = true`, che legge `be.vwDocumentiEmessiNonFatturati`
    // — una **unione** di due CTE, `FattureEliminate` ∪ `FatturePosticipate`. Il test verifica proprio
    // che l'unione arrivi intera al client: il rischio di una vista fatta di due rami è che uno dei due
    // si rompa in silenzio (una join che non aggancia, un casing diverso) e la griglia mostri metà dei
    // documenti senza alcun errore — è già successo, con il 404 fantasma da casing del GUID ente.
    //
    // Il seed del 2024 contiene entrambe le famiglie: 4001 posticipata (gennaio) e 5001/5002 eliminate
    // (febbraio).
    // =============================================================================================

    [Test]
    public async Task NonFatturate_ShouldMostrareLePosticipate()
    {
        // 4001: posticipata di gennaio 2024, ramo FatturePosticipate della vista.
        var resp = await Post("""{ "cancellata": true, "anno": 2024, "mese": 1 }""");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var idFatture = IdFattureNelCorpo(await resp.Content.ReadAsStringAsync());
        TestContext.Out.WriteLine($"posticipate: {string.Join(", ", idFatture)}");
        Assert.That(idFatture, Does.Contain(4001L));
    }

    [Test]
    public async Task NonFatturate_ShouldMostrareLeEliminate()
    {
        // 5001 e 5002: eliminate di febbraio 2024, ramo FattureEliminate.
        var resp = await Post("""{ "cancellata": true, "anno": 2024, "mese": 2 }""");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var idFatture = IdFattureNelCorpo(await resp.Content.ReadAsStringAsync());
        TestContext.Out.WriteLine($"eliminate: {string.Join(", ", idFatture)}");
        Assert.Multiple(() =>
        {
            Assert.That(idFatture, Does.Contain(5001L));
            Assert.That(idFatture, Does.Contain(5002L), "la 5002 non ha righe: deve comparire lo stesso");
        });
    }

    /// <summary>
    /// Il marker che distingue le due famiglie: la vista espone `fattura.inviata` con **3 = ELIMINATA**
    /// e **4 = POSTICIPATA** — non lo stato reale di invio, che su queste righe non avrebbe senso.
    /// È il valore su cui il portale distingue le due, quindi vale la pena che arrivi giusto.
    /// </summary>
    [Test]
    public async Task NonFatturate_IlMarker_ShouldDistinguereLeDueFamiglie()
    {
        var posticipate = await (await Post("""{ "cancellata": true, "anno": 2024, "mese": 1 }""")).Content.ReadAsStringAsync();
        var eliminate = await (await Post("""{ "cancellata": true, "anno": 2024, "mese": 2 }""")).Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(MarkerNelCorpo(posticipate), Does.Contain(4), "marker POSTICIPATA atteso 4");
            Assert.That(MarkerNelCorpo(eliminate), Does.Contain(3), "marker ELIMINATA atteso 3");
        });
    }

    /// <summary>
    /// ⚠️ **Il filtro "Non Fatturate" lavora per periodo**: senza `mese` la ricerca risponde 404 anche
    /// quando per quell'anno esistono documenti. Non è un difetto — il portale manda sempre il periodo
    /// — ma spiega perché "tutte le posticipate ed eliminate" significa *tutte quelle del periodo
    /// selezionato*, e perché una verifica a mano fatta col solo anno sembra dire che non c'è nulla.
    /// </summary>
    [Test]
    public async Task NonFatturate_SenzaMese_ShouldReturn404_Caratterizzazione()
    {
        var resp = await Post("""{ "cancellata": true, "anno": 2024 }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    private static List<int> MarkerNelCorpo(string corpo)
    {
        var trovati = new List<int>();
        using var doc = JsonDocument.Parse(corpo);
        Raccogli(doc.RootElement);
        return trovati;

        void Raccogli(JsonElement elemento)
        {
            switch (elemento.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var proprieta in elemento.EnumerateObject())
                    {
                        if (proprieta.Name.Equals("inviata", StringComparison.OrdinalIgnoreCase)
                            && proprieta.Value.TryGetInt32(out var marker))
                            trovati.Add(marker);
                        Raccogli(proprieta.Value);
                    }
                    break;
                case JsonValueKind.Array:
                    foreach (var voce in elemento.EnumerateArray()) Raccogli(voce);
                    break;
            }
        }
    }

    /// <summary>Gli `idFattura` presenti nel corpo, cercati ovunque nella struttura annidata.</summary>
    private static List<long> IdFattureNelCorpo(string corpo)
    {
        var trovati = new List<long>();
        using var doc = JsonDocument.Parse(corpo);
        Raccogli(doc.RootElement);
        return trovati;

        void Raccogli(JsonElement elemento)
        {
            switch (elemento.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var proprieta in elemento.EnumerateObject())
                    {
                        if (proprieta.Name.Contains("idfattura", StringComparison.OrdinalIgnoreCase)
                            && proprieta.Value.TryGetInt64(out var id))
                            trovati.Add(id);
                        Raccogli(proprieta.Value);
                    }
                    break;
                case JsonValueKind.Array:
                    foreach (var voce in elemento.EnumerateArray()) Raccogli(voce);
                    break;
            }
        }
    }

    [Test]
    public async Task NonFatturate_PeriodoVuoto_ShouldReturn404()
    {
        var resp = await Post("""{ "cancellata": true, "anno": 1999, "mese": 1 }""");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
            "Contratto attuale: Non Fatturate su periodo vuoto -> 404 (quello che ha disorientato il FE).");
    }

    [Test]
    public async Task Emesse_ConDati_2024_3_ShouldReturn200()
    {
        var resp = await Post("""{ "cancellata": false, "anno": 2024, "mese": 3 }""");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Ricerca_SenzaAutenticazione_ShouldReturn401()
    {
        var resp = await Post("""{ "cancellata": true, "anno": 2024, "mese": 2 }""", ruolo: null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Ricerca_MeseComeStringa_ShouldBind_Non400()
    {
        // Body reale del FE con "mese": "7" (stringa) e array/valori nulli: il binding deve accettarlo
        // (non 400) — e' il body che, sul path del bug casing, dava 404 (non 400), a prova che il bind passa.
        var resp = await Post("""
        { "anno": 2026, "mese": "7", "tipologiaFattura": [], "cancellata": true,
          "idEnti": [], "idTipoContratto": null, "inviata": null }
        """);
        Assert.That(resp.StatusCode, Is.Not.EqualTo(HttpStatusCode.BadRequest),
            "Il body FE con mese stringa NON deve dare 400 nel binding.");
    }

    [Test]
    public async Task NonFatturate_CasingEnteDiverso_2026_7_ShouldReturn200()
    {
        // E2E via HTTP del fix casing: la posticipata 9101 (istitutioID MAIUSCOLO vs pfd.Enti minuscolo)
        // deve comparire grazie al match case-insensitive in FattureQueryRicercaPersistence.
        var resp = await Post("""{ "cancellata": true, "anno": 2026, "mese": 7 }""");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("9101"),
            "La Non Fatturata con casing ente diverso deve essere restituita (match case-insensitive).");
    }
}
