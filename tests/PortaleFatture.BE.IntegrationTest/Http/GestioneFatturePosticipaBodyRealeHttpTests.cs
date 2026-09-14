using System.Net;
using System.Text;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Replay end-to-end della **POSTICIPA** su `POST /api/fatture/pagopa/gestione-fatture/azione`, con il
/// body nella forma esatta che il portale invia — provata a mano in UAT e qui riprodotta su dati
/// seedati. Gemello di `GestioneFattureEliminaBodyRealeHttpTests`, che fa lo stesso per l'ELIMINA.
///
/// Il body porta quattro cose che insieme non erano coperte:
///   - `anno`/`mese` come **stringhe** JSON, benche' il DTO li dichiari `int?`;
///   - `azione` in **case misto** ("Posticipa"), che l'endpoint normalizza prima della whitelist;
///   - `idFattura: null` — quindi la riga nasce con `FkIdFattura` NULL e la chiave e' il **periodo**;
///   - la nota **senza** il campo `azione`, che e' la persistence a valorizzare server-side.
///
/// Cosa aggiunge rispetto a quanto gia' c'era: `Azione_ConNumeriComeStringa_ShouldBeAccepted`
/// (in `GestioneFattureHttpTests`) invia la stessa forma ma si ferma alla risposta `"1"`, e gli assert
/// sul contenuto della nota (`GestioneFattureRequisitiIntegrationTests`) leggono la riga **per
/// `FkIdFattura`** — strada che con `idFattura: null` non esiste. Qui si verifica l'effetto a DB
/// leggendo **per periodo**, che e' la chiave logica reale di `cfg.GestioneFatture`.
///
/// ATTENZIONE **Una differenza dichiarata rispetto al caso UAT**: il periodo riservato qui (2026/8) non ha una
/// fattura corrispondente in `pfd.FattureTestata`, quindi la SP prende il ramo "pre-generazione". Oggi
/// l'esito osservabile e' identico a quello del ramo con fattura esistente, perche' nella SP il
/// conteggio su `FattureTestata` viene sovrascritto da una tabella variabile mai popolata e il ramo di
/// rifiuto e' di fatto codice morto (v. `coverage/segnalazione-sp-gestione-fatture.md`). Quando quel
/// difetto verra' corretto i due rami torneranno a divergere, e questa classe andra' rivista: o si
/// semina una fattura sul periodo, o si accetta che copra esplicitamente la pre-generazione.
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
public class GestioneFatturePosticipaBodyRealeHttpTests
{
    private const string Rotta = "/api/fatture/pagopa/gestione-fatture/azione";

    // Ente del seed. Il periodo e' RISERVATO a questa classe: la PK di cfg.GestioneFatture e'
    // (FkIdEnte, FkTipologiaFattura, Anno, Mese, Stato), quindi una riga lasciata qui bloccherebbe
    // qualunque altra POSTICIPA sulla stessa chiave e farebbe fallire altri test con un Result 0
    // inspiegabile. 2026/8 non e' usato da nessun'altra classe (verificato).
    private const string IdEnte = "11111111-1111-1111-1111-111111111111";
    private const string Tipologia = "SECONDO SALDO";
    private const int Anno = 2026;
    private const int Mese = 8;

    // L'utente con cui TestAuthHandler firma le richieste: e' quello che finisce in IdUtenteInserimento.
    private const string Utente = "integration-test-user";

    private const string Testo = "replay del caso provato a mano in UAT";

    /// <summary>
    /// Il body cosi' come arriva dal portale, senza normalizzazioni: numeri fra virgolette, azione in
    /// case misto, `idFattura` nullo, nota priva del campo `azione`.
    /// </summary>
    private static readonly string BodyReale = $$"""
    {
        "mese": "{{Mese}}",
        "anno": "{{Anno}}",
        "tipologiaFattura": "{{Tipologia}}",
        "idEnte": "{{IdEnte}}",
        "azione": "Posticipa",
        "nota": { "data": "2026-08-31T16:40:37", "testo": "{{Testo}}" },
        "idFattura": null
    }
    """;

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
        Pulisci(); // difensivo: un run interrotto lascerebbe la chiave di periodo occupata
    }

    [TearDown]
    public void TearDown() => Pulisci();

    // =============================================================================================

    [Test]
    public async Task Posticipa_BodyReale_ShouldReturn200_ECreareLaRigaPosticipata()
    {
        var resp = await Posticipa();

        Assert.That(resp.stato, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(resp.corpo, Is.EqualTo("1"), "Result 1 = la stored procedure ha scritto la riga.");

        var riga = LeggiRigaDelPeriodo();
        Assert.That(riga, Is.Not.Null, "Nessuna riga sul periodo: la risposta dice 1 ma a DB non c'e' nulla.");
        Assert.Multiple(() =>
        {
            Assert.That(riga!.Value.stato, Is.Zero, "Stato 0 = POSTICIPATA.");
            Assert.That(riga.Value.azione, Is.EqualTo("POSTICIPATA"),
                "La colonna Azione usa il vocabolario al PASSATO (cosa e' stato fatto), mentre l'input "
                + "usa l'imperativo ('Posticipa'). Sono due vocabolari distinti, non un refuso.");
            Assert.That(riga.Value.idFattura, Is.Null,
                "Il body non manda idFattura e la riga resta senza: la chiave logica e' il periodo. "
                + "E' il motivo per cui i cleanup e le letture per FkIdFattura qui non troverebbero nulla.");
            Assert.That(riga.Value.utente, Is.EqualTo(Utente),
                "IdUtenteInserimento e' l'utente autenticato, non un valore mandato dal client.");
        });
    }

    [Test]
    public async Task Posticipa_BodyReale_ShouldScrivereLaNotaComeArrayJson_ConAzioneValorizzataDalServer()
    {
        await Posticipa();

        Assert.Multiple(() =>
        {
            Assert.That(JsonDellaNota("$[0].Testo"), Is.EqualTo(Testo),
                "La nota e' obbligatoria e finisce in un ARRAY JSON, non in una stringa.");
            Assert.That(JsonDellaNota("$[0].Azione"), Is.EqualTo("POSTICIPA"),
                "Il client NON manda 'azione' dentro la nota: la valorizza la persistence, in maiuscolo, "
                + "prima di serializzare. Ogni nota registra cosi' l'azione che l'ha generata.");
        });
    }

    /// <summary>
    /// La ripetizione della stessa POSTICIPA è **idempotente** dal 28/07/2026, da quando la SP usa un
    /// `MERGE`: risponde di nuovo `1` e lascia **una sola** riga. Non è una svista da correggere — è il
    /// comportamento attuale, e il registro dei test disattivati cita ancora un caso "posticipa su già
    /// posticipata → 400" che con questa SP non si verifica più.
    ///
    /// Lo storico però si accumula: la nota viene **appesa** all'array con `JSON_MODIFY … 'append'`.
    /// </summary>
    [Test]
    public async Task Posticipa_Ripetuta_ShouldEssereIdempotente_MaAccumulareLeNote()
    {
        var prima = await Posticipa();
        var seconda = await Posticipa();

        Assert.Multiple(() =>
        {
            Assert.That(prima.stato, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(seconda.stato, Is.EqualTo(HttpStatusCode.OK), "La ripetizione non viene rifiutata.");
            Assert.That(seconda.corpo, Is.EqualTo("1"));
            Assert.That(RigheDelPeriodo(), Is.EqualTo(1), "Il MERGE aggiorna, non aggiunge una seconda riga.");
            Assert.That(JsonDellaNota("$[1].Testo"), Is.EqualTo(Testo),
                "La seconda nota e' appesa in coda: l'array conserva lo storico delle azioni.");
        });
    }

    // =============================================================================================
    // La seconda gamba dello scenario di testbook: "le fatture posticipate vengono visualizzate nella
    // griglia risultati con stato POSTICIPATA".
    //
    // Non è una verifica scontata, perché la griglia non legge `cfg.GestioneFatture`: legge
    // `be.vwGestioneFattureGriglia`, che ci arriva attraverso **tre INNER JOIN** (Enti, Contratti,
    // TipoContratto) e scarta le righe `Stato = 2`. Una riga scritta correttamente può quindi non
    // comparire in griglia — è lo stesso meccanismo per cui una fattura il cui ente non ha contratto
    // sparisce in silenzio dalle liste (v. `docs/viste-endpoint.md`, trappola diagnostica n.1).
    // =============================================================================================

    [Test]
    public async Task Griglia_PrimaDellaPosticipa_ShouldNonContenereIlPeriodo()
    {
        // Contro-prova, senza la quale il test seguente potrebbe passare per una riga preesistente.
        Assert.That(await RigaInGriglia(), Is.Null);
    }

    [Test]
    public async Task Posticipa_ShouldComparireInGriglia_ConAzionePosticipata()
    {
        await Posticipa();

        var riga = await RigaInGriglia();

        Assert.That(riga, Is.Not.Null,
            "La riga esiste in cfg.GestioneFatture ma non arriva in griglia: guardare gli INNER JOIN "
            + "della vista (ente senza contratto, o TipoContratto non risolto) prima del codice C#.");
        Assert.Multiple(() =>
        {
            Assert.That(riga!.Value.GetProperty("azione").GetString(), Is.EqualTo("POSTICIPATA"),
                "È lo stato che l'operatore si aspetta di leggere nella colonna Azione della griglia.");
            Assert.That(riga.Value.GetProperty("tipologiaFattura").GetString(), Is.EqualTo(Tipologia));
            Assert.That(riga.Value.GetProperty("idFattura").ValueKind,
                Is.EqualTo(System.Text.Json.JsonValueKind.Null),
                "Posticipando dal form senza idFattura, la griglia mostra la riga con Id Fattura vuoto: "
                + "è coerente col fatto che la chiave sia il periodo.");
        });
    }

    [Test]
    public async Task Posticipa_ShouldComparireInGriglia_ConLaNotaScritta()
    {
        // La griglia espone anche la colonna Note, che è il JSON completo: è da lì che l'operatore
        // rilegge cosa era stato scritto al momento dell'azione.
        await Posticipa();

        var riga = await RigaInGriglia();

        Assert.That(riga, Is.Not.Null);
        Assert.That(riga!.Value.GetProperty("note").GetString(), Does.Contain(Testo));
    }

    [Test]
    public async Task Posticipa_BodyReale_SenzaAutenticazione_ShouldReturn401()
    {
        var client = _factory.CreateClientAs(null);
        var resp = await client.PostAsync(_factory.WithNonce(Rotta), Contenuto());

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(RigheDelPeriodo(), Is.Zero, "Una richiesta non autenticata non deve lasciare tracce.");
    }

    // =============================================================================================
    // Helper
    // =============================================================================================

    private static StringContent Contenuto() => new(BodyReale, Encoding.UTF8, "application/json");

    private async Task<(HttpStatusCode stato, string corpo)> Posticipa()
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var resp = await client.PostAsync(_factory.WithNonce(Rotta), Contenuto());
        var corpo = await resp.Content.ReadAsStringAsync();
        TestContext.Out.WriteLine($"POST {Rotta} -> {(int)resp.StatusCode} {resp.StatusCode} | {corpo}");
        return (resp.StatusCode, corpo);
    }

    /// <summary>
    /// La riga del periodo come la vede la **griglia** (`POST api/fatture/pagopa/gestione-fatture`),
    /// cioè attraverso `be.vwGestioneFattureGriglia`, non leggendo la tabella. `null` se non c'è.
    ///
    /// Nota sul filtro: si passa lo stesso insieme di criteri che manda il portale (ente, anno, mesi,
    /// tipologia); `page`/`pageSize` vanno in query string perché l'endpoint li dichiara `int` non
    /// nullable — omettendoli si otterrebbe un errore di binding, non una pagina di default.
    /// </summary>
    private async Task<System.Text.Json.JsonElement?> RigaInGriglia()
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
        var rotta = $"/api/fatture/pagopa/gestione-fatture?page=1&pageSize=50"
                  + $"&nonce={Uri.EscapeDataString(_factory.Nonce())}";
        var resp = await client.PostAsync(rotta, new StringContent(body, Encoding.UTF8, "application/json"));
        TestContext.Out.WriteLine($"POST griglia -> {(int)resp.StatusCode} {resp.StatusCode}");

        // Lista vuota => 404: è il contratto dell'area (lo stesso di api/fatture), non un errore.
        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var doc = System.Text.Json.JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        foreach (var item in doc.RootElement.GetProperty("gestioneFatture").EnumerateArray())
            if (item.GetProperty("anno").GetInt32() == Anno && item.GetProperty("mese").GetInt32() == Mese)
                return item.Clone(); // Clone: il JsonDocument viene disposto all'uscita

        return null;
    }

    private static (int stato, string? azione, long? idFattura, string? utente)? LeggiRigaDelPeriodo()
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
            SELECT TOP(1) Stato, Azione, FkIdFattura, IdUtenteInserimento
            FROM cfg.GestioneFatture
            WHERE FkIdEnte='{IdEnte}' AND FkTipologiaFattura='{Tipologia}' AND Anno={Anno} AND Mese={Mese}";
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return (
            reader.GetInt32(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetInt64(2),
            reader.IsDBNull(3) ? null : reader.GetString(3));
    }

    private static int RigheDelPeriodo()
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            $@"SELECT COUNT(*) FROM cfg.GestioneFatture
               WHERE FkIdEnte='{IdEnte}' AND FkTipologiaFattura='{Tipologia}' AND Anno={Anno} AND Mese={Mese}";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>Legge un valore dal JSON della colonna `Note` per path (es. "$[0].Azione").</summary>
    private static string? JsonDellaNota(string path)
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand($@"
            SELECT TOP(1) JSON_VALUE(Note, @path) FROM cfg.GestioneFatture
            WHERE FkIdEnte='{IdEnte}' AND FkTipologiaFattura='{Tipologia}' AND Anno={Anno} AND Mese={Mese}", conn);
        cmd.Parameters.AddWithValue("@path", path);
        var v = cmd.ExecuteScalar();
        return v == null || v == DBNull.Value ? null : (string)v;
    }

    private static void Pulisci()
    {
        try
        {
            using var conn = new SqlConnection(LocalTestDb.ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            // Per PERIODO, non per FkIdFattura: con idFattura nullo la colonna resta NULL e un cleanup
            // per id non troverebbe la riga, lasciandola a occupare la chiave per sempre.
            cmd.CommandText =
                $@"DELETE FROM cfg.GestioneFatture
                   WHERE FkIdEnte='{IdEnte}' AND FkTipologiaFattura='{Tipologia}' AND Anno={Anno} AND Mese={Mese};";
            cmd.ExecuteNonQuery();
        }
        catch (SqlException) { /* best-effort: se il container e' giu' i test si auto-ignorano comunque */ }
    }
}
