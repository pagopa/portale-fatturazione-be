using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Il caso di testbook `PF-672 TD-04`: **annullare una posticipa** col pulsante "Annulla" sulla riga
/// della griglia, dopo di che *"le fatture cancellate non sono più visibili nella griglia risultati"*.
///
/// ATTENZIONE **Terzo disallineamento di vocabolario dell'area**, e l'unico che può far sbagliare una chiamata:
/// il pulsante si chiama **"Annulla"**, l'azione da mandare all'API è **`CANCELLA`**, e lo stato che
/// ne risulta si legge **`CANCELLATA`**. La whitelist dell'endpoint accetta solo
/// `POSTICIPA | ELIMINA | RIPRISTINA | CANCELLA`: mandare la parola dell'interfaccia dà **400**
/// (fissato dal test in fondo). Gli altri due disallineamenti sono imperativo/passato fra form e
/// griglia — v. `docs/testbook-backend.md`, capitolo Gestione Fatture.
///
/// **"Non più visibile" non vuol dire "cancellata"**: la riga resta in `cfg.GestioneFatture` con
/// `Stato = 2`, ed è la vista della griglia a escluderla (`WHERE gf.Stato &lt;&gt; 2`). Lo storico
/// dell'azione — chi ha annullato e quando — resta quindi consultabile a DB, cosa che dalla pagina non
/// si intuisce. È il motivo per cui questi test verificano **entrambe** le cose: sparizione dalla
/// griglia *e* permanenza della riga.
///
/// Periodo riservato a questa classe (2026/11): nel testbook TD-04 prosegue sulla stessa fattura di
/// TD-03, ma qui serve isolamento — la PK di `cfg.GestioneFatture` è per periodo, e due classi sullo
/// stesso periodo si ostacolerebbero al primo run interrotto.
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
public class GestioneFattureAnnullaHttpTests
{
    private const string Rotta = "/api/fatture/pagopa/gestione-fatture/azione";

    private const string IdEnte = "11111111-1111-1111-1111-111111111111";
    private const string Tipologia = "PRIMO SALDO";
    private const int Anno = 2026;
    private const int Mese = 11;

    private ApiTestFactory _factory = null!;

    [OneTimeSetUp]
    public void OneTimeSetup() => _factory = new ApiTestFactory();

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Pulisci();
        _factory?.Dispose();
    }

    [SetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        Pulisci();
    }

    [TearDown]
    public void TearDown() => Pulisci();

    // =============================================================================================

    [Test]
    public async Task Annulla_SuUnaPosticipata_ShouldReturn200()
    {
        await Azione("Posticipa");

        var (stato, corpo) = await Azione("Cancella");

        Assert.That(stato, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(corpo, Is.EqualTo("1"));
    }

    [Test]
    public async Task Annulla_ShouldPortareLoStatoACancellata_TracciandoUtenteEData()
    {
        await Azione("Posticipa");
        await Azione("Cancella");

        var riga = LeggiRigaDelPeriodo();

        Assert.That(riga, Is.Not.Null, "La riga NON viene rimossa: cambia stato.");
        Assert.Multiple(() =>
        {
            Assert.That(riga!.Value.stato, Is.EqualTo(2), "2 = CANCELLATA.");
            Assert.That(riga.Value.azione, Is.EqualTo("CANCELLATA"));
            Assert.That(riga.Value.utenteCancellazione, Is.EqualTo("integration-test-user"),
                "La cancellazione e' una TRANSIZIONE su un record esistente, quindi si traccia in "
                + "IdUtenteCancellazione/DataCancellazione — non nelle colonne di inserimento, che "
                + "restano quelle di chi aveva posticipato.");
            Assert.That(riga.Value.dataCancellazione, Is.Not.Null);
            Assert.That(riga.Value.utenteInserimento, Is.EqualTo("integration-test-user"),
                "Chi aveva posticipato resta registrato: l'annulla non riscrive la storia.");
        });
    }

    /// <summary>Il cuore di TD-04: dopo l'annulla la riga sparisce dalla griglia.</summary>
    [Test]
    public async Task Annulla_ShouldFarSparireLaRigaDallaGriglia()
    {
        await Azione("Posticipa");
        Assert.That(await RigaInGriglia(), Is.Not.Null,
            "Contro-prova: prima dell'annulla la riga deve esserci, altrimenti il test dopo non prova nulla.");

        await Azione("Cancella");

        Assert.That(await RigaInGriglia(), Is.Null,
            "La vista della griglia esclude Stato = 2. Se comparisse ancora, l'operatore vedrebbe una "
            + "riga che crede annullata.");
    }

    [Test]
    public async Task Annulla_ShouldLasciareLaRigaLeggibileADatabase()
    {
        // Distinzione che dalla pagina non si vede: sparire dalla griglia non e' essere eliminati.
        await Azione("Posticipa");
        await Azione("Cancella");

        Assert.That(RigheDelPeriodo(), Is.EqualTo(1),
            "Una sola riga, ancora presente: lo storico di chi ha posticipato e chi ha annullato resta.");
    }

    /// <summary>
    /// CARATTERIZZAZIONE di un difetto noto dell'endpoint: annullare qualcosa che non è stato
    /// posticipato è correttamente rifiutato dalla SP (`Result 0`), ma `FattureModule` traduce quello
    /// zero in `NotFound()` — quindi il client riceve un **404 senza messaggio**, indistinguibile da
    /// una rotta sbagliata e senza dire perché l'azione non è ammessa.
    ///
    /// Il rimedio (400 con messaggio) cambia il contratto API e va concordato col frontend, quindi qui
    /// si fissa il comportamento **attuale**; l'aspettativa corretta vive nei due test `[Ignore]`
    /// gemelli di `GestioneFattureHttpTests`.
    /// </summary>
    [Test]
    public async Task Annulla_SenzaPosticipaPrecedente_ShouldReturn404Muto_Caratterizzazione()
    {
        var (stato, corpo) = await Azione("Cancella");

        Assert.Multiple(() =>
        {
            Assert.That(stato, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(corpo, Is.Empty, "404 senza corpo: il motivo del rifiuto non arriva al client.");
            Assert.That(RigheDelPeriodo(), Is.Zero, "Un rifiuto non deve lasciare righe.");
        });
    }

    [Test]
    public async Task Annulla_ConLaParolaDellInterfaccia_ShouldReturn400()
    {
        // "Annulla" e' l'etichetta del pulsante, non un'azione dell'API: la whitelist accetta
        // POSTICIPA | ELIMINA | RIPRISTINA | CANCELLA. Fissa il confine fra i due vocabolari.
        await Azione("Posticipa");

        var (stato, corpo) = await Azione("Annulla");

        Assert.Multiple(() =>
        {
            Assert.That(stato, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(corpo, Does.Contain("Azione non valida"),
                "Il rifiuto della whitelist **dice perche'**. Utile come metro di paragone: il rifiuto "
                + "della stored procedure, invece, esce come 404 con corpo vuoto (v. il test sopra) — "
                + "stessa pagina, due qualita' di errore molto diverse.");
            Assert.That(LeggiRigaDelPeriodo()!.Value.stato, Is.Zero,
                "La posticipata resta tale: un'azione non riconosciuta non deve avere effetti.");
        });
    }

    // =============================================================================================
    // Helper
    // =============================================================================================

    private async Task<(HttpStatusCode stato, string corpo)> Azione(string azione)
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var body = $$"""
        {
            "mese": "{{Mese}}",
            "anno": "{{Anno}}",
            "tipologiaFattura": "{{Tipologia}}",
            "idEnte": "{{IdEnte}}",
            "azione": "{{azione}}",
            "nota": { "data": "2026-08-31T18:00:00", "testo": "azione {{azione}} da test" },
            "idFattura": null
        }
        """;
        var resp = await client.PostAsync(_factory.WithNonce(Rotta),
            new StringContent(body, Encoding.UTF8, "application/json"));
        var corpo = await resp.Content.ReadAsStringAsync();
        TestContext.Out.WriteLine($"{azione,-10} -> {(int)resp.StatusCode} {resp.StatusCode} | {corpo}");
        return (resp.StatusCode, corpo);
    }

    private async Task<JsonElement?> RigaInGriglia()
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var body = $$"""
        {
            "idEnti": ["{{IdEnte}}"],
            "anno": {{Anno}},
            "mesi": [{{Mese}}],
            "tipologiaFattura": "{{Tipologia}}"
        }
        """;
        var rotta = "/api/fatture/pagopa/gestione-fatture?page=1&pageSize=50"
                  + $"&nonce={Uri.EscapeDataString(_factory.Nonce())}";
        var resp = await client.PostAsync(rotta, new StringContent(body, Encoding.UTF8, "application/json"));

        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        foreach (var item in doc.RootElement.GetProperty("gestioneFatture").EnumerateArray())
            if (item.GetProperty("anno").GetInt32() == Anno && item.GetProperty("mese").GetInt32() == Mese)
                return item.Clone();

        return null;
    }

    private static (int stato, string? azione, string? utenteInserimento,
                    string? utenteCancellazione, DateTime? dataCancellazione)? LeggiRigaDelPeriodo()
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
            SELECT TOP(1) Stato, Azione, IdUtenteInserimento, IdUtenteCancellazione, DataCancellazione
            FROM cfg.GestioneFatture
            WHERE FkIdEnte='{IdEnte}' AND FkTipologiaFattura='{Tipologia}' AND Anno={Anno} AND Mese={Mese}";
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return (reader.GetInt32(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetDateTime(4));
    }

    private static int RigheDelPeriodo()
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"SELECT COUNT(*) FROM cfg.GestioneFatture
            WHERE FkIdEnte='{IdEnte}' AND FkTipologiaFattura='{Tipologia}' AND Anno={Anno} AND Mese={Mese}";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static void Pulisci()
    {
        try
        {
            using var conn = new SqlConnection(LocalTestDb.ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"DELETE FROM cfg.GestioneFatture
                WHERE FkIdEnte='{IdEnte}' AND FkTipologiaFattura='{Tipologia}' AND Anno={Anno} AND Mese={Mese};";
            cmd.ExecuteNonQuery();
        }
        catch (SqlException) { /* best-effort */ }
    }
}
