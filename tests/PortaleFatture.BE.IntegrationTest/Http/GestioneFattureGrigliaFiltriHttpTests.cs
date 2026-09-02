using System.Net;
using System.Text;
using System.Text.Json;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Il caso di testbook `PF-672 TD-12`: i **filtri della griglia** — Anno, Mese, Tipologia fattura,
/// Tipologia contratto, Rag. Sociale, Stato — tutti **opzionali**.
///
/// **Perché a livello HTTP e non solo di query.** I filtri, come logica, sono già coperti a livello di
/// query (`GestioneFattureQueryIntegrationTests`, `GestioneFattureDownloadQueryIntegrationTests`).
/// Quello che solo un test HTTP può dire è se il **nome JSON che il portale invia lega davvero** sul
/// DTO: un filtro che non lega non fallisce, viene semplicemente **ignorato in silenzio** e la griglia
/// mostra più righe del dovuto. Non è teorico — è successo con `idTipoContratto`, che il frontend
/// mandava mentre il DTO esponeva `TipologiaContratto`: il filtro non ha mai funzionato finché
/// qualcuno non se n'è accorto (rinominato il 29/07/2026). Questi test lo bloccano.
///
/// ⚠️ **Due nomi dell'interfaccia non corrispondono al campo che si manda**, ed è la ragione principale
/// per cui questo file esiste:
///
/// | Etichetta nella pagina | Campo JSON | Nota |
/// |---|---|---|
/// | Rag. Sociale | `idEnti` | la tendina mostra i nomi, il portale invia gli **id** |
/// | Stato | `azione` | e col vocabolario al **passato** (`POSTICIPATA`…), non l'imperativo del form |
///
/// Il seed dell'anno **2025** è la base di questi test: tre righe visibili (una per ente, una per
/// tipologia, una per stato) più una quarta `CANCELLATA` che la vista esclude. È statico e nessun'altra
/// classe lo tocca — questi test **leggono soltanto**, non scrivono nulla.
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
public class GestioneFattureGrigliaFiltriHttpTests
{
    private const string Ente1 = "11111111-1111-1111-1111-111111111111"; // SECONDO SALDO 2025/1, POSTICIPATA, PAC (2)
    private const string Ente2 = "22222222-2222-2222-2222-222222222222"; // PRIMO SALDO   2025/2, RIPRISTINATA, PAC (2)
    private const string Ente3 = "33333333-3333-3333-3333-333333333333"; // ANTICIPO      2025/3, ELIMINATA, PAL (1)

    private ApiTestFactory _factory = null!;

    [OneTimeSetUp]
    public void Setup() => _factory = new ApiTestFactory();

    [OneTimeTearDown]
    public void TearDown() => _factory?.Dispose();

    [SetUp]
    public void CheckDb() => TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);

    // =============================================================================================
    // Opzionalità
    // =============================================================================================

    [Test]
    public async Task SenzaAlcunFiltro_ShouldRestituireLaGriglia()
    {
        var righe = await Griglia("{}");

        Assert.That(righe, Is.Not.Null, "Tutti i filtri sono opzionali: un corpo vuoto e' legittimo.");
        Assert.That(righe!.Count, Is.GreaterThanOrEqualTo(3), "Almeno le tre righe visibili del seed 2025.");
    }

    [Test]
    public async Task ConFiltriNulliEArrayVuoti_ShouldComportarsiComeSenzaFiltri()
    {
        // E' il corpo che il portale invia quando l'utente non seleziona nulla. Un array vuoto significa
        // "non filtrare", non "nessun risultato" — coerente con tutti i filtri a lista del progetto.
        var righe = await Griglia("""
        {
            "idEnti": [], "idTipoContratto": null, "tipologiaFattura": null,
            "anno": null, "mesi": [], "azione": null, "note": null
        }
        """);

        Assert.That(righe, Is.Not.Null);
        Assert.That(righe!.Count, Is.GreaterThanOrEqualTo(3));
    }

    // =============================================================================================
    // I sei filtri, uno per uno
    // =============================================================================================

    [Test]
    public async Task FiltroAnno_ShouldSelezionareLeSoleRigheDelPeriodo()
    {
        var righe = await Griglia("""{ "anno": 2025 }""");

        Assert.That(Enti(righe), Is.EquivalentTo(new[] { Ente1, Ente2, Ente3 }),
            "Tre righe: la quarta del 2025 e' CANCELLATA e la vista la esclude.");
    }

    [Test]
    public async Task FiltroMese_ShouldSelezionareLaSolaRigaDelMese()
    {
        var righe = await Griglia("""{ "anno": 2025, "mesi": [3] }""");

        Assert.That(Enti(righe), Is.EquivalentTo(new[] { Ente3 }));
    }

    [Test]
    public async Task FiltroTipologiaFattura_ShouldSelezionareLaSolaTipologiaChiesta()
    {
        var righe = await Griglia("""{ "anno": 2025, "tipologiaFattura": "ANTICIPO" }""");

        Assert.That(Enti(righe), Is.EquivalentTo(new[] { Ente3 }));
    }

    /// <summary>
    /// REGRESSIONE — è il filtro che non funzionava. Il portale manda `idTipoContratto`; se il DTO
    /// tornasse a esporre un nome diverso, il valore non legherebbe e la griglia mostrerebbe **tutte**
    /// le righe invece di quelle del tipo contratto scelto, senza alcun errore.
    /// </summary>
    [Test]
    public async Task FiltroTipologiaContratto_ShouldLegareEFiltrare()
    {
        var pal = await Griglia("""{ "anno": 2025, "idTipoContratto": 1 }""");
        var pac = await Griglia("""{ "anno": 2025, "idTipoContratto": 2 }""");

        Assert.Multiple(() =>
        {
            Assert.That(Enti(pal), Is.EquivalentTo(new[] { Ente3 }), "Tipo 1 = solo l'ente PAL.");
            Assert.That(Enti(pac), Is.EquivalentTo(new[] { Ente1, Ente2 }), "Tipo 2 = i due PAC.");
        });
    }

    [Test]
    public async Task FiltroRagioneSociale_ShouldViaggiareComeIdEnti()
    {
        // La tendina mostra le ragioni sociali, il portale invia gli id.
        var righe = await Griglia($$"""{ "anno": 2025, "idEnti": ["{{Ente2}}"] }""");

        Assert.That(Enti(righe), Is.EquivalentTo(new[] { Ente2 }));
    }

    [Test]
    public async Task FiltroStato_ShouldViaggiareComeAzione_AlPassato()
    {
        var righe = await Griglia("""{ "anno": 2025, "azione": "POSTICIPATA" }""");

        Assert.That(Enti(righe), Is.EquivalentTo(new[] { Ente1 }),
            "Il filtro etichettato 'Stato' e' il campo `azione`, e vuole il vocabolario al PASSATO: "
            + "mandare 'POSTICIPA' (l'imperativo del form) non selezionerebbe nulla.");
    }

    /// <summary>
    /// CARATTERIZZAZIONE del meccanismo che ha reso invisibile il bug del filtro tipo contratto: un
    /// campo JSON **sconosciuto** al DTO non produce errore, viene semplicemente **ignorato**. Il
    /// portale continuava a mandare `tipologiaContratto` e la griglia rispondeva 200 con tutte le
    /// righe: nessun 400, nessun log, solo un filtro che non filtrava.
    ///
    /// È il comportamento di default di System.Text.Json e non lo si sta chiedendo di cambiare — ma
    /// saperlo spiega perché questa classe di difetti va cercata con un test end-to-end e non si nota
    /// in review.
    /// </summary>
    [Test]
    public async Task FiltroConNomeSconosciuto_ShouldEssereIgnoratoInSilenzio_Caratterizzazione()
    {
        var conNomeVecchio = await Griglia("""{ "anno": 2025, "tipologiaContratto": 1 }""");

        Assert.That(Enti(conNomeVecchio), Is.EquivalentTo(new[] { Ente1, Ente2, Ente3 }),
            "Col nome sbagliato tornano TUTTE le righe: il filtro non ha avuto effetto e nessuno "
            + "l'ha segnalato. Col nome giusto (test qui sopra) ne torna una sola.");
    }

    // =============================================================================================
    // Combinazioni
    // =============================================================================================

    [Test]
    public async Task FiltriCombinati_ShouldIntersecare_NonUnire()
    {
        // ANTICIPO esiste solo sull'ente PAL (tipo 1): chiedendolo insieme al tipo 2 non resta nulla.
        // Se i filtri fossero in OR, tornerebbero righe.
        var righe = await Griglia("""{ "anno": 2025, "tipologiaFattura": "ANTICIPO", "idTipoContratto": 2 }""");

        Assert.That(righe, Is.Null, "Nessun risultato => 404, contratto dell'area.");
    }

    [Test]
    public async Task FiltriCombinati_CoerentiFraLoro_ShouldSelezionareLaRigaAttesa()
    {
        var righe = await Griglia($$"""
        {
            "anno": 2025, "mesi": [2], "tipologiaFattura": "PRIMO SALDO",
            "idTipoContratto": 2, "idEnti": ["{{Ente2}}"], "azione": "RIPRISTINATA"
        }
        """);

        Assert.That(Enti(righe), Is.EquivalentTo(new[] { Ente2 }),
            "Tutti e sei i filtri insieme devono identificare esattamente quella riga.");
    }

    // =============================================================================================
    // `PF-672 TD-13`: *"se non ci sono fatture posticipate / eliminate / ripristinate la griglia non
    // mostra alcuna fattura, bensì il messaggio «non sono presenti documenti»"*.
    //
    // Lato backend il messaggio non esiste: è il frontend a comporlo. Ciò che il backend deve
    // garantire è il **contratto** su cui quel messaggio si regge — lista vuota = **404**, non 200 con
    // un array vuoto. È lo stesso contratto di `api/fatture`, ed è la ragione per cui in tutta questa
    // suite le contro-prove leggono il 404 come "nessuna riga".
    // =============================================================================================

    [Test]
    public async Task Griglia_SenzaRisultati_ShouldReturn404_ENonUnaListaVuota()
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var rotta = "/api/fatture/pagopa/gestione-fatture?page=1&pageSize=50"
                  + $"&nonce={Uri.EscapeDataString(_factory.Nonce())}";
        var resp = await client.PostAsync(rotta,
            new StringContent("""{ "anno": 1990 }""", Encoding.UTF8, "application/json"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
            "E' su questo 404 che il portale mostra 'non sono presenti documenti': se un domani "
            + "l'endpoint rispondesse 200 con lista vuota, la pagina resterebbe muta.");
        Assert.That(await resp.Content.ReadAsStringAsync(), Is.Empty,
            "404 senza corpo: non c'e' un messaggio lato server, lo compone il frontend.");
    }

    /// <summary>
    /// CARATTERIZZAZIONE, non un requisito: il **download** sullo stesso insieme vuoto si comporta come
    /// la griglia (404) invece di produrre un file con le sole intestazioni. Vale la pena saperlo,
    /// perché è ciò che il portale deve gestire per non far scaricare un file inesistente.
    /// </summary>
    [Test]
    public async Task Download_SenzaRisultati_ShouldComportarsiComeLaGriglia()
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var rotta = "/api/fatture/pagopa/gestione-fatture/download"
                  + $"?nonce={Uri.EscapeDataString(_factory.Nonce())}";
        var resp = await client.PostAsync(rotta,
            new StringContent("""{ "anno": 1990 }""", Encoding.UTF8, "application/json"));
        TestContext.Out.WriteLine($"download vuoto -> {(int)resp.StatusCode} {resp.StatusCode}");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
            "Coerente con la griglia: niente file vuoto da scaricare.");
    }

    /// <summary>
    /// ⚠️ Il rovescio del contratto, da conoscere prima di aprire una segnalazione: quel 404 è
    /// **indistinguibile** da quello di una rotta sbagliata o di un nonce non valido. "Non sono
    /// presenti documenti" e "hai chiamato male" arrivano al client identici, senza corpo.
    ///
    /// È la stessa forma del 404 muto sui rifiuti delle stored procedure (v. BE-GF-07): non un difetto
    /// nuovo, ma la stessa scelta di contratto applicata alla lettura.
    /// </summary>
    [Test]
    public async Task Griglia_RottaInesistente_ShouldDareLoStesso404_Caratterizzazione()
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var resp = await client.PostAsync($"/api/fatture/pagopa/gestione-fatture-inesistente"
                  + $"?nonce={Uri.EscapeDataString(_factory.Nonce())}",
            new StringContent("""{ "anno": 2025 }""", Encoding.UTF8, "application/json"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
            "Stesso codice, stesso corpo vuoto del caso 'nessun documento': dal solo HTTP il client "
            + "non puo' distinguere i due casi.");
    }

    // =============================================================================================
    // Helper
    // =============================================================================================

    /// <summary>Le righe della griglia, o `null` se la lista è vuota (che l'endpoint traduce in 404).</summary>
    private async Task<List<JsonElement>?> Griglia(string filtri)
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var rotta = "/api/fatture/pagopa/gestione-fatture?page=1&pageSize=200"
                  + $"&nonce={Uri.EscapeDataString(_factory.Nonce())}";
        var resp = await client.PostAsync(rotta, new StringContent(filtri, Encoding.UTF8, "application/json"));
        TestContext.Out.WriteLine($"{filtri} -> {(int)resp.StatusCode} {resp.StatusCode}");

        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            "Un filtro valido non deve mai dare 400: sono tutti opzionali e il binding li accetta.");

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("gestioneFatture").EnumerateArray()
            .Select(x => x.Clone())
            .ToList();
    }

    /// <summary>Gli enti delle righe del **2025**, che è il perimetro statico su cui si asserisce.</summary>
    private static string[] Enti(List<JsonElement>? righe) =>
        (righe ?? [])
            .Where(x => x.GetProperty("anno").GetInt32() == 2025)
            .Select(x => x.GetProperty("ente").GetString()!)
            .Distinct()
            .ToArray();
}
