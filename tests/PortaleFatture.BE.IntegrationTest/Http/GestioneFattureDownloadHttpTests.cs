using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
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

    // =============================================================================================
    // `PF-672 TD-11`: *"il file excel scaricato contiene tutti i risultati visualizzati nella griglia,
    // rispettando i filtri impostati"*.
    //
    // I filtri in sé sono già coperti a livello di query (`GestioneFattureDownloadQueryIntegrationTests`,
    // 7 test). Quello che nessuno verificava è il **file**: che sia davvero un xlsx apribile e che il
    // suo contenuto corrisponda a ciò che la griglia mostra. I test qui sotto lo aprono per davvero —
    // un .xlsx è uno zip, e le stringhe stanno in `xl/sharedStrings.xml`.
    //
    // Il seed 2025 è costruito apposta per rendere l'asserzione discriminante: quattro righe di cui
    // una `CANCELLATA` (`Stato = 2`), che la vista esclude. Se il download non applicasse le stesse
    // regole della griglia, quella riga comparirebbe nel file.
    // =============================================================================================

    [Test]
    public async Task Download_ShouldRestituireUnXlsxApribile()
    {
        var resp = await Post("""{ "anno": 2025 }""");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var bytes = await resp.Content.ReadAsByteArrayAsync();
        Assert.That(bytes, Is.Not.Empty);
        Assert.That(bytes.Take(2).ToArray(), Is.EqualTo(new byte[] { 0x50, 0x4B }).AsCollection,
            "Un .xlsx e' uno zip: deve iniziare con 'PK'. Se qui arriva altro, il file che l'utente "
            + "scarica non si apre in Excel.");

        using var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        Assert.That(zip.GetEntry("xl/workbook.xml"), Is.Not.Null,
            "Lo zip deve avere la struttura di una cartella di lavoro Open XML.");
    }

    [Test]
    public async Task Download_ShouldContenereLeRigheDellaGriglia_ENonLeCancellate()
    {
        var testo = await StringheDelFoglio("""{ "anno": 2025 }""");

        Assert.Multiple(() =>
        {
            Assert.That(testo, Does.Contain("POSTICIPATA"));
            Assert.That(testo, Does.Contain("RIPRISTINATA"));
            Assert.That(testo, Does.Contain("ELIMINATA"));
            Assert.That(testo, Does.Not.Contain("CANCELLATA"),
                "La riga Stato = 2 e' esclusa dalla vista, quindi non deve finire nemmeno nel file: "
                + "download e griglia devono raccontare la stessa cosa.");
        });
    }

    [Test]
    public async Task Download_ShouldRispettareIlFiltroPerTipologia()
    {
        // Anno 2025 + ANTICIPO seleziona la sola riga di Ente Test 3.
        var testo = await StringheDelFoglio("""{ "anno": 2025, "tipologiaFattura": "ANTICIPO" }""");

        Assert.Multiple(() =>
        {
            Assert.That(testo, Does.Contain("Ente Test 3"));
            Assert.That(testo, Does.Not.Contain("Ente Test 1"),
                "Il filtro deve valere anche sul file, non solo sulla griglia.");
            Assert.That(testo, Does.Not.Contain("Ente Test 2"));
        });
    }

    /// <summary>
    /// L'affermazione del testbook presa alla lettera: **tutti** i risultati della griglia devono
    /// esserci nel file. Invece di riscrivere l'insieme atteso a mano, lo si chiede alla griglia con
    /// gli stessi filtri e si verifica riga per riga — così il test resta valido se il seed cambia.
    /// </summary>
    [Test]
    public async Task Download_ShouldContenereTutteLeRagioniSocialiDellaGriglia()
    {
        const string filtri = """{ "anno": 2025 }""";

        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var rottaGriglia = "/api/fatture/pagopa/gestione-fatture?page=1&pageSize=100"
                         + $"&nonce={Uri.EscapeDataString(_factory.Nonce())}";
        var respGriglia = await client.PostAsync(rottaGriglia,
            new StringContent(filtri, Encoding.UTF8, "application/json"));
        Assert.That(respGriglia.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var doc = JsonDocument.Parse(await respGriglia.Content.ReadAsStringAsync());
        var ragioniSociali = doc.RootElement.GetProperty("gestioneFatture").EnumerateArray()
            .Select(x => x.GetProperty("ragioneSociale").GetString())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct()
            .ToArray();

        Assert.That(ragioniSociali, Is.Not.Empty, "Senza righe in griglia il confronto non proverebbe nulla.");

        var testo = await StringheDelFoglio(filtri);
        foreach (var ragioneSociale in ragioniSociali)
            Assert.That(testo, Does.Contain(ragioneSociale!),
                $"'{ragioneSociale}' e' in griglia ma non nel file scaricato.");
    }

    /// <summary>
    /// Il testo del foglio. Un .xlsx è uno zip Open XML, ma **dove** stiano le stringhe dipende da come
    /// è stato scritto: possono essere deduplicate in `xl/sharedStrings.xml` oppure inline nel foglio
    /// (`&lt;is&gt;&lt;t&gt;…`). Questo export usa la seconda forma, quindi si concatena l'XML di tutto
    /// ciò che sta sotto `xl/` invece di puntare a un file solo — così l'asserzione non dipende da una
    /// scelta del writer, e nessuna dipendenza si aggiunge al progetto di test.
    /// </summary>
    private async Task<string> StringheDelFoglio(string filtri)
    {
        var resp = await Post(filtri);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var zip = new ZipArchive(new MemoryStream(await resp.Content.ReadAsByteArrayAsync()), ZipArchiveMode.Read);
        var testo = new StringBuilder();
        foreach (var entry in zip.Entries.Where(e => e.FullName.StartsWith("xl/") && e.FullName.EndsWith(".xml")))
        {
            using var reader = new StreamReader(entry.Open());
            testo.Append(await reader.ReadToEndAsync());
        }

        TestContext.Out.WriteLine($"voci xlsx: {string.Join(", ", zip.Entries.Select(e => e.FullName))}");
        Assert.That(testo.ToString(), Is.Not.Empty, "Il foglio non contiene XML: il file non e' un xlsx valido.");
        return testo.ToString();
    }
}
