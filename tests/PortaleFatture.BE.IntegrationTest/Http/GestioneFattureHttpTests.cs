using System.Net;
using System.Net.Http.Json;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// PROTOTIPO di test HTTP end-to-end sull'endpoint POST /api/fatture/pagopa/gestione-fatture/azione.
/// A differenza degli unit test sull'handler (che invocano il metodo via reflection e quindi BYPASSANO
/// [Authorize]), qui la richiesta attraversa la pipeline vera: si testano autorizzazione, model binding
/// e validazione come le vede un client.
///
/// Richiede il container di test attivo (tests/: docker compose up -d --build).
/// </summary>
public class GestioneFattureHttpTests
{
    private const string Rotta = "/api/fatture/pagopa/gestione-fatture/azione";
    private const string IdEnte = "11111111-1111-1111-1111-111111111111";

    // Periodo RISERVATO a questi test. cfg.GestioneFatture ha una PK composta su
    // (FkIdEnte, FkTipologiaFattura, Anno, Mese, Stato): una riga lasciata qui rende impossibile
    // qualunque altra POSTICIPA sulla stessa chiave, e i test delle altre classi falliscono con un
    // Result 0 apparentemente inspiegabile. Servono sia il periodo dedicato sia il cleanup: il body
    // non manda idFattura, quindi i cleanup per FkIdFattura degli altri test non ripulirebbero la riga.
    private const int Anno = 2027;
    private const int Mese = 12;

    private ApiTestFactory _factory;

    [OneTimeSetUp]
    public void Setup() => _factory = new ApiTestFactory();

    [OneTimeTearDown]
    public void TearDown()
    {
        Pulisci();
        _factory?.Dispose();
    }

    [TearDown]
    public void AfterEachTest() => Pulisci();

    private static void Pulisci()
    {
        try
        {
            using var conn = new Microsoft.Data.SqlClient.SqlConnection(LocalTestDb.ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            // Due criteri: il periodo riservato, e l'utente con cui TestAuthHandler firma le richieste.
            // Il secondo ripulisce anche eventuali righe lasciate da versioni precedenti di questi test,
            // che altrimenti resterebbero a occupare una chiave per sempre.
            cmd.CommandText =
                $@"DELETE FROM cfg.GestioneFatture WHERE FkIdEnte='{IdEnte}' AND Anno={Anno} AND Mese={Mese};
                   DELETE FROM cfg.GestioneFatture WHERE IdUtenteInserimento='integration-test-user';";
            cmd.ExecuteNonQuery();
        }
        catch (Microsoft.Data.SqlClient.SqlException) { /* best-effort */ }
    }

    /// <summary>
    /// Corpo valido per la rotta, con azione e nota opzionali (nota viene creata se null).
    /// </summary>
    /// <param name="azione">L'azione da eseguire.</param>
    /// <param name="nota">La nota associata all'azione.</param>
    /// <returns>Un oggetto rappresentante il corpo della richiesta.</returns>
    private static object ValidBody(string azione = "POSTICIPA", object? nota = null) => new
    {
        idEnte = IdEnte,
        azione,
        anno = Anno,
        mese = Mese,
        tipologiaFattura = "SECONDO SALDO",
        idFattura = (int?)null,
        nota = nota ?? new { data = "2026-07-01T00:00:00", testo = "nota http test" }
    };

    [Test]
    /// <summary>
    /// Verifica che un utente senza autenticazione riceva un 401.
    /// </summary>
    public async Task Azione_SenzaAutenticazione_ShouldReturn401()
    {
        var client = _factory.CreateClientAs(null); // anonimo
        var resp = await client.PostAsJsonAsync(_factory.WithNonce(Rotta), ValidBody());
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized),
            "Una rotta protetta deve rifiutare l'anonimo: questo NON e' verificabile invocando l'handler via reflection.");
    }

    [Test]
    /// <summary>
    /// Verifica che un utente con ruolo ADMIN riceva un 400 quando la nota è mancante.
    /// </summary>
    public async Task Azione_SenzaNota_ShouldReturn400_ConMessaggio()
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        // body SENZA la proprieta' nota (non basta passarla null all'helper: verrebbe rimpiazzata)
        var senzaNota = new
        {
            idEnte = IdEnte,
            azione = "POSTICIPA",
            anno = Anno,
            mese = Mese,
            tipologiaFattura = "SECONDO SALDO"
        };
        var resp = await client.PostAsJsonAsync(_factory.WithNonce(Rotta), senzaNota);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("nota"), "Il 400 deve spiegare che la nota e' obbligatoria.");
    }

    [Test]
    /// <summary>
    /// Verifica che un utente con ruolo ADMIN riceva un 400 quando l'azione non è valida.
    /// </summary>
    public async Task Azione_ConAzioneNonValida_ShouldReturn400()
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var resp = await client.PostAsJsonAsync(_factory.WithNonce(Rotta), ValidBody(azione: "PIPPO"));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    /// <summary>
    /// Verifica che un utente con ruolo ADMIN possa accedere all'endpoint protetto.
    /// </summary>
    public async Task Azione_ConRuoloAdmin_ShouldPassAuthorization()
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var resp = await client.PostAsJsonAsync(_factory.WithNonce(Rotta), ValidBody());

        // Non asseriamo l'esito applicativo (dipende dai dati), ma che l'autorizzazione sia superata:
        // niente 401/403.
        Assert.That(resp.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(resp.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Azione_ConNumeriComeStringa_ShouldBeAccepted()
    {
        // Body reale inviato da un client: anno/mese come STRINGHE JSON ("8", "2026") anche se il DTO
        // li dichiara int?, e azione in forma mista ("Posticipa").
        // Entrambe le cose sono ACCETTATE, e vale la pena fissarlo:
        //  - ASP.NET Core usa JsonSerializerDefaults.Web, che include NumberHandling.AllowReadingFromString
        //    (con le opzioni di default di System.Text.Json fallirebbe con 400);
        //  - l'endpoint normalizza l'azione con Trim().ToUpperInvariant() prima della whitelist.
        // Se un domani si passasse a opzioni JSON custom, questo test intercetterebbe la rottura del
        // contratto verso i client esistenti.
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var body = $$"""
        {
            "mese": "{{Mese}}",
            "anno": "{{Anno}}",
            "tipologiaFattura": "PRIMO SALDO",
            "idEnte": "{{IdEnte}}",
            "azione": "Posticipa",
            "nota": { "data": "2026-07-24T16:26:25", "testo": "diagnosi numeri come stringa" },
            "idFattura": null
        }
        """;
        var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(_factory.WithNonce(Rotta), content);
        var testo = await resp.Content.ReadAsStringAsync();

        TestContext.Out.WriteLine($"STATUS: {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.Out.WriteLine($"BODY  : {testo}");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            "Numeri come stringa e azione in case misto sono accettati: il binding non e' la causa "
          + "di eventuali errori su questo body.");
        Assert.That(testo, Is.EqualTo("1"), "La SP deve aver creato la riga (Result 1).");
    }

    // ---------------------------------------------------------------------------------------
    // MACCHINA A STATI vista dal client HTTP.
    // Contratto atteso: un'azione ripetuta sullo stesso stato viene rifiutata con 400; le
    // transizioni legittime (posticipata -> ripristinata, ripristinata -> posticipata) riescono e
    // lasciano UNA riga per periodo.
    // ---------------------------------------------------------------------------------------

    private async Task<HttpStatusCode> Azione(HttpClient client, string azione) =>
        (await client.PostAsJsonAsync(_factory.WithNonce(Rotta), ValidBody(azione))).StatusCode;

    private static int RigheDelPeriodo()
    {
        using var conn = new Microsoft.Data.SqlClient.SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            $"SELECT COUNT(*) FROM cfg.GestioneFatture WHERE FkIdEnte='{IdEnte}' AND Anno={Anno} AND Mese={Mese}";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    [Test]
    [Ignore("DIFETTO ENDPOINT (non della SP): la SP applica correttamente la regola e risponde Result 0 "
          + "-- lo verifica Sm_PosticipaSuGiaPosticipata_ShouldReturn0 / Sm_RipristinaSuGiaRipristinata_"
          + "ShouldReturn0, entrambi verdi. E' FattureModule a tradurre 'result.Value == 0' in "
          + "NotFound(), quindi il client riceve un 404 senza messaggio: indistinguibile da una rotta "
          + "sbagliata, e non spiega perche' l'azione e' stata rifiutata. Rimedio: Fix 9 in "
          + "segnalazione-sp-gestione-fatture.md (Result 0 -> BadRequest con messaggio).")]
    public async Task Posticipa_SuGiaPosticipata_ShouldReturn400()
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        Assert.That(await Azione(client, "POSTICIPA"), Is.EqualTo(HttpStatusCode.OK), "la prima posticipa deve riuscire");

        Assert.That(await Azione(client, "POSTICIPA"), Is.EqualTo(HttpStatusCode.BadRequest),
            "Posticipare una gia' POSTICIPATA e' un'azione non ammessa dallo stato corrente: 400.");
        Assert.That(RigheDelPeriodo(), Is.EqualTo(1), "e non deve restare piu' di una riga per il periodo");
    }

    [Test]
    [Ignore("DIFETTO ENDPOINT (non della SP): la SP applica correttamente la regola e risponde Result 0 "
          + "-- lo verifica Sm_PosticipaSuGiaPosticipata_ShouldReturn0 / Sm_RipristinaSuGiaRipristinata_"
          + "ShouldReturn0, entrambi verdi. E' FattureModule a tradurre 'result.Value == 0' in "
          + "NotFound(), quindi il client riceve un 404 senza messaggio: indistinguibile da una rotta "
          + "sbagliata, e non spiega perche' l'azione e' stata rifiutata. Rimedio: Fix 9 in "
          + "segnalazione-sp-gestione-fatture.md (Result 0 -> BadRequest con messaggio).")]
    public async Task Ripristina_SuGiaRipristinata_ShouldReturn400()
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        Assert.That(await Azione(client, "POSTICIPA"), Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await Azione(client, "RIPRISTINA"), Is.EqualTo(HttpStatusCode.OK), "il primo ripristino deve riuscire");

        Assert.That(await Azione(client, "RIPRISTINA"), Is.EqualTo(HttpStatusCode.BadRequest),
            "Ripristinare una gia' RIPRISTINATA e' un'azione non ammessa dallo stato corrente: 400.");
    }

    [Test]
    public async Task Ripristina_SuPosticipata_ShouldSucceed()
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        Assert.That(await Azione(client, "POSTICIPA"), Is.EqualTo(HttpStatusCode.OK));

        Assert.That(await Azione(client, "RIPRISTINA"), Is.EqualTo(HttpStatusCode.OK),
            "Una POSTICIPATA deve poter essere ripristinata.");
        Assert.That(RigheDelPeriodo(), Is.EqualTo(1), "la transizione aggiorna la riga, non ne aggiunge una");
    }

    [Test]
    [Ignore("DIFETTO SP (Fix 8): la transizione RIPRISTINATA -> POSTICIPATA risponde 200 ma crea una "
          + "SECONDA riga per lo stesso periodo invece di riportare a Stato 0 quella esistente. Da li' "
          + "il periodo ha due righe e il ripristino successivo va in violazione di PK. Vedi anche "
          + "Sm_PosticipaSuRipristinata_ShouldReturn1_AndKeepOneRow (stesso difetto, livello MediatR).")]
    public async Task Posticipa_SuRipristinata_ShouldSucceed_AndKeepOneRow()
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        Assert.That(await Azione(client, "POSTICIPA"), Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await Azione(client, "RIPRISTINA"), Is.EqualTo(HttpStatusCode.OK));

        Assert.That(await Azione(client, "POSTICIPA"), Is.EqualTo(HttpStatusCode.OK),
            "Una RIPRISTINATA deve poter essere posticipata di nuovo.");
        Assert.That(RigheDelPeriodo(), Is.EqualTo(1),
            "e deve restare UNA riga per periodo: se diventano due, il ciclo successivo va in "
          + "violazione di PK e la griglia mostra due righe per lo stesso periodo.");
    }

    //TODO: valutare se scrivere un test anche per l'autenticazione del ruolo OPERATOR, che dovrebbe fallire (403).
}
