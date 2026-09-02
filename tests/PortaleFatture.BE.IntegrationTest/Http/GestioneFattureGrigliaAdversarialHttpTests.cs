using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Passata **adversarial** sulla superficie di *lettura* di Gestione Fatture: griglia e download.
///
/// Perché serviva, dato che un file adversarial per quest'area esiste già: quello
/// (`GestioneFattureAdversarialIntegrationTests`) attacca i **command** a livello MediatR — azione
/// nulla, injection sulla tipologia, note ostili, concorrenza, anno/mese estremi. Qui si attacca ciò
/// che i test dei casi di testbook hanno aggiunto e nessuno aveva ancora provato con input ostili: i
/// **filtri**, la **paginazione** e il **download**, attraverso la pipeline HTTP reale.
///
/// **Cosa ha retto** — vale la pena saperlo quanto il resto: i filtri sono tutti parametrizzati
/// (`@identi`, `@azione`, `@tipologiafattura`, …) e il WHERE è composto con un **accumulatore di
/// condizioni**, quindi l'injection resta un valore e non diventa SQL, e non esiste il difetto del
/// "WHERE emesso solo dal filtro sull'anno" che affligge invece la ricerca notifiche. *Il pattern
/// corretto esiste già in casa, a due passi da quello sbagliato.*
///
/// **Cosa non ha retto**: il filtro `note`, che il portale può mandare e che viene **ignorato in
/// silenzio** (v. il test dedicato).
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
public class GestioneFattureGrigliaAdversarialHttpTests
{
    private const string RottaGriglia = "/api/fatture/pagopa/gestione-fatture";
    private const string RottaDownload = "/api/fatture/pagopa/gestione-fatture/download";

    private ApiTestFactory _factory = null!;

    [OneTimeSetUp]
    public void Setup() => _factory = new ApiTestFactory();

    [OneTimeTearDown]
    public void TearDown() => _factory?.Dispose();

    [SetUp]
    public void CheckDb() => TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);

    // =============================================================================================
    // Injection
    // =============================================================================================

    [TestCase("' OR '1'='1", TestName = "Injection(OR 1=1)")]
    [TestCase("'; DROP TABLE cfg.GestioneFatture; --", TestName = "Injection(DROP TABLE)")]
    [TestCase("' UNION SELECT NULL,NULL,NULL --", TestName = "Injection(UNION)")]
    public async Task FiltriStringa_ConInjection_ShouldRestareValori_ELaTabellaSopravvive(string ostile)
    {
        var righeIniziali = RigheInTabella();

        var perAzione = await Griglia($$"""{ "anno": 2025, "azione": {{Json(ostile)}} }""");
        var perTipologia = await Griglia($$"""{ "anno": 2025, "tipologiaFattura": {{Json(ostile)}} }""");

        Assert.Multiple(() =>
        {
            Assert.That(perAzione, Is.Null, "Trattato come valore: nessuna riga ha quell'azione.");
            Assert.That(perTipologia, Is.Null);
            Assert.That(RigheInTabella(), Is.EqualTo(righeIniziali),
                "La tabella deve essere intatta: se qui il numero cambia, l'input e' diventato SQL.");
        });
    }

    [Test]
    public async Task IdEnti_ConInjection_ShouldNonAllargareIlRisultato()
    {
        // La lista finisce in un IN parametrizzato: il valore ostile e' solo un id che non esiste.
        var righe = await Griglia("""{ "anno": 2025, "idEnti": ["' OR 1=1 --"] }""");

        Assert.That(righe, Is.Null, "Nessun ente ha quell'id: la lista non deve diventare un OR vero.");
    }

    // =============================================================================================
    // Il filtro fantasma
    // =============================================================================================

    /// <summary>
    /// CARATTERIZZAZIONE — il campo **`note`** arriva dal client fino alla Query
    /// (`RicercaGestioneFatture.Note` → `GestioneFattureQuery.Note`) ma la persistence **non lo usa
    /// mai**: nessuna condizione, nessun parametro. Mandarlo non filtra nulla.
    ///
    /// È la stessa famiglia del filtro tipo contratto che non legava, con un'aggravante: lì il campo
    /// non arrivava per un nome sbagliato, qui **è cablato lungo tutta la catena**, quindi in review
    /// sembra implementato a tutti gli effetti.
    ///
    /// ATTENZIONE Non è però un difetto da "completare": `note` **non è fra i filtri previsti** dalla pagina
    /// (Anno / Mese / Tipologia fattura / Tipologia contratto / Rag. Sociale / Stato — v. `PF-672
    /// TD-12`). È quindi **codice morto** da rimuovere, non una funzione mancante — motivo per cui non
    /// c'è un `[Ignore]` con l'aspettativa "deve filtrare": sarebbe un requisito inventato.
    /// </summary>
    [Test]
    public async Task FiltroNote_ShouldEssereIgnorato_Caratterizzazione()
    {
        var senzaNote = await Griglia("""{ "anno": 2025 }""");
        var conNoteAssurde = await Griglia("""{ "anno": 2025, "note": "xyz-che-non-esiste-in-nessuna-nota" }""");

        Assert.That(senzaNote, Is.Not.Null);
        Assert.That(conNoteAssurde?.Count, Is.EqualTo(senzaNote!.Count),
            "Il filtro note non ha alcun effetto: stesse righe con e senza. E' cablato fino alla Query "
            + "ma la persistence non lo consuma.");
    }

    // =============================================================================================
    // Valori estremi
    // =============================================================================================

    /// <summary>
    /// DIFETTO APERTO — `page = 0` o negativa producono un **500**. L'OFFSET è calcolato come
    /// `(@page-1)*@size`: con `page = 0` diventa **negativo**, e SQL Server rifiuta un OFFSET negativo.
    ///
    /// Il portale manda sempre `page >= 1`, quindi non si vede dalla pagina; ma è un parametro di query
    /// string, quindi lo controlla il client. L'aspettativa qui sotto è quella corretta: un valore
    /// assurdo è un errore **del client** (400) o al più una pagina vuota — non un errore del server.
    /// </summary>
    [TestCase(0, TestName = "PaginazioneOstile_ShouldNonDareErroreServer(page 0)")]
    [TestCase(-1, TestName = "PaginazioneOstile_ShouldNonDareErroreServer(page negativa)")]
    [Ignore("DIFETTO APERTO — page <= 0 produce un OFFSET negativo ((@page-1)*@size) e SQL Server "
        + "risponde con un errore, che l'endpoint espone come 500. Rimedio: validare page >= 1 a monte "
        + "(400), oppure normalizzarla a 1. V. coverage/test-backlog.md.")]
    public async Task PaginazioneOstile_ShouldNonDareErroreServer(int page)
    {
        var (stato, _) = await GrigliaGrezza("""{ "anno": 2025 }""", page, 50);

        Assert.That((int)stato, Is.LessThan(500));
    }

    [TestCase(0, TestName = "PaginazioneOstile_Oggi500(page 0)")]
    [TestCase(-1, TestName = "PaginazioneOstile_Oggi500(page negativa)")]
    public async Task PaginazioneOstile_OggiDa500_Caratterizzazione(int page)
    {
        var (stato, _) = await GrigliaGrezza("""{ "anno": 2025 }""", page, 50);

        TestContext.Out.WriteLine($"page={page} -> {(int)stato}");
        Assert.That(stato, Is.EqualTo(HttpStatusCode.InternalServerError),
            "Quando questo test diventa rosso la paginazione e' stata irrobustita: togliere l'[Ignore] "
            + "del test gemello e cancellare questa caratterizzazione.");
    }

    [Test]
    public async Task PageSizeEnorme_ShouldNonDareErroreServer()
    {
        var (stato, _) = await GrigliaGrezza("""{ "anno": 2025 }""", 1, int.MaxValue);

        TestContext.Out.WriteLine($"pageSize=int.MaxValue -> {(int)stato}");
        Assert.That((int)stato, Is.LessThan(500));
    }

    [TestCase(0)]
    [TestCase(13)]
    [TestCase(99999)]
    public async Task MeseFuoriRange_ShouldRestituireNulla_NonUnErrore(int mese)
    {
        var righe = await Griglia($$"""{ "anno": 2025, "mesi": [{{mese}}] }""");

        Assert.That(righe, Is.Null, "Un mese inesistente seleziona zero righe, non provoca un errore.");
    }

    /// <summary>
    /// Il limite di **2100 parametri** di SQL Server: una lista lunga in un `IN` parametrizzato lo
    /// supera. Scenario realistico — un admin che incolla l'elenco completo degli aderenti — ed è lo
    /// stesso limite già rilevato sui filtri della ricerca notifiche.
    /// </summary>
    [Test]
    [Ignore("DIFETTO APERTO — una lista di oltre ~2100 valori sfonda il limite dei parametri di SQL "
        + "Server e l'endpoint risponde 500. Scenario realistico: un admin che seleziona tutti gli "
        + "aderenti. Rimedio: rifiutare esplicitamente le liste troppo lunghe, o passarle come TVP. "
        + "Stesso limite gia' rilevato sui filtri della ricerca notifiche. V. coverage/test-backlog.md.")]
    public async Task ListaEntiMoltoLunga_ShouldNonDareErroreServer()
    {
        var enti = string.Join(",", Enumerable.Range(0, 2500).Select(_ => $"\"{Guid.NewGuid()}\""));
        var (stato, _) = await GrigliaGrezza($$"""{ "anno": 2025, "idEnti": [{{enti}}] }""", 1, 50);

        Assert.That((int)stato, Is.LessThan(500));
    }

    [Test]
    public async Task ListaEntiMoltoLunga_OggiDa500_Caratterizzazione()
    {
        var enti = string.Join(",", Enumerable.Range(0, 2500).Select(_ => $"\"{Guid.NewGuid()}\""));
        var (stato, _) = await GrigliaGrezza($$"""{ "anno": 2025, "idEnti": [{{enti}}] }""", 1, 50);

        TestContext.Out.WriteLine($"2500 enti -> {(int)stato}");
        Assert.That(stato, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    [Test]
    public async Task ListaEntiSottoIlLimite_ShouldFunzionare()
    {
        // Contro-prova: il problema e' il LIMITE, non le liste lunghe in se'. Con 2000 valori regge.
        var enti = string.Join(",", Enumerable.Range(0, 2000).Select(_ => $"\"{Guid.NewGuid()}\""));
        var (stato, _) = await GrigliaGrezza($$"""{ "anno": 2025, "idEnti": [{{enti}}] }""", 1, 50);

        TestContext.Out.WriteLine($"2000 enti -> {(int)stato}");
        Assert.That((int)stato, Is.LessThan(500));
    }

    // =============================================================================================
    // Download, stessa superficie
    // =============================================================================================

    [Test]
    public async Task Download_ConFiltriOstili_ShouldNonDareErroreServer()
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var body = """{ "anno": 2025, "tipologiaFattura": "' OR '1'='1", "mesi": [0, 13] }""";
        var resp = await client.PostAsync(_factory.WithNonce(RottaDownload),
            new StringContent(body, Encoding.UTF8, "application/json"));

        TestContext.Out.WriteLine($"download ostile -> {(int)resp.StatusCode}");
        Assert.That((int)resp.StatusCode, Is.LessThan(500));
    }

    // =============================================================================================
    // Helper
    // =============================================================================================

    private static string Json(string valore) => JsonSerializer.Serialize(valore);

    private async Task<List<JsonElement>?> Griglia(string filtri)
    {
        var (stato, corpo) = await GrigliaGrezza(filtri, 1, 200);
        if (stato == HttpStatusCode.NotFound) return null;
        Assert.That(stato, Is.EqualTo(HttpStatusCode.OK), $"Risposta inattesa per {filtri}");

        using var doc = JsonDocument.Parse(corpo);
        return doc.RootElement.GetProperty("gestioneFatture").EnumerateArray().Select(x => x.Clone()).ToList();
    }

    private async Task<(HttpStatusCode stato, string corpo)> GrigliaGrezza(string filtri, int page, int pageSize)
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var rotta = $"{RottaGriglia}?page={page}&pageSize={pageSize}"
                  + $"&nonce={Uri.EscapeDataString(_factory.Nonce())}";
        var resp = await client.PostAsync(rotta, new StringContent(filtri, Encoding.UTF8, "application/json"));
        return (resp.StatusCode, await resp.Content.ReadAsStringAsync());
    }

    private static int RigheInTabella()
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM cfg.GestioneFatture";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
}
