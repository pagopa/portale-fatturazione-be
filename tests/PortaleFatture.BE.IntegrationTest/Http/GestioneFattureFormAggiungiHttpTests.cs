using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Il caso di testbook `PF-672 TD-14`: il form **"Aggiungi"** — campi obbligatori (Azione, Rag.
/// Sociale, Tipologia Fattura, Anno, Mese, Note) e finestre Anno/Mese che dipendono da Azione +
/// Tipologia, *"per garantire che l'azione non crei inconsistenze"*.
///
/// Due metà, che sul backend vivono in posti diversi:
///
/// 1. **l'obbligatorietà** è una validazione dell'endpoint (`POST …/gestione-fatture/azione`) — e non
///    è uniforme: alcuni campi sono verificati esplicitamente, altri no. Qui si documenta quali;
/// 2. **la coerenza delle finestre** vive nella vista `be.vwGestioneFattureFormAnniMesi`, che i menu a
///    tendina interrogano via `modifica/anni` e `modifica/mesi`. È pura date-math per tipologia, senza
///    dipendenze da tabelle, e soprattutto **non espone le combinazioni non ammesse**: è lì che
///    l'inconsistenza viene evitata a monte, impedendo di comporla nel form.
///
/// ATTENZIONE La barriera è però solo nella **tendina**, non nell'endpoint dell'azione: chi chiama l'API
/// direttamente può comporre una combinazione che il form non offrirebbe. Non è un difetto di questa
/// pagina — è la stessa assenza di validazione del periodo già tracciata dagli `[Ignore]` dell'area
/// (mese 0, mese 99999, anno negativo vengono accettati) — ma va saputo prima di dedurre che "il form
/// lo impedisce" significhi "il sistema lo impedisce".
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
public class GestioneFattureFormAggiungiHttpTests
{
    private const string Rotta = "/api/fatture/pagopa/gestione-fatture/azione";
    private const string RottaAnni = "/api/fatture/pagopa/gestione-fatture/modifica/anni";
    private const string RottaMesi = "/api/fatture/pagopa/gestione-fatture/modifica/mesi";

    // Periodo riservato: i test di validazione non devono scrivere, ma se una richiesta malformata
    // passasse la pulizia evita che resti a occupare la chiave.
    private const string IdEnte = "11111111-1111-1111-1111-111111111111";
    private const int Anno = 2027;
    private const int Mese = 6;

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
    // Metà 1 — i campi obbligatori
    // =============================================================================================

    [Test]
    public async Task Azione_ConTuttiICampi_ShouldEssereAccettata()
    {
        // Riferimento positivo: senza, i 400 dei test seguenti potrebbero venire da altro.
        var (stato, _) = await Invia(Corpo());

        Assert.That(stato, Is.EqualTo(HttpStatusCode.OK));
    }

    [TestCase("azione", TestName = "CampoMancante_ShouldReturn400(azione)")]
    [TestCase("idEnte", TestName = "CampoMancante_ShouldReturn400(idEnte)")]
    [TestCase("nota", TestName = "CampoMancante_ShouldReturn400(nota)")]
    public async Task CampoMancante_ShouldReturn400(string campo)
    {
        var (stato, corpo) = await Invia(Corpo(ometti: campo));

        Assert.That(stato, Is.EqualTo(HttpStatusCode.BadRequest),
            $"'{campo}' e' fra i campi che l'endpoint valida esplicitamente.");
        Assert.That(corpo, Is.Not.Empty, "E il 400 dice perche': e' la qualita' di errore che serve al form.");
    }

    /// <summary>
    /// CARATTERIZZAZIONE — i campi che il form dichiara obbligatori **non sono validati tutti allo
    /// stesso modo**: `tipologiaFattura`, `anno` e `mese` non hanno una verifica sull'endpoint.
    /// Ciò che li rifiuta è la stored procedure o il database, quindi la risposta non è un 400 con
    /// messaggio ma qualcosa di più opaco.
    ///
    /// L'esito misurato è un **404 con corpo vuoto**: la richiesta viene respinta — che è la cosa
    /// importante, e il test la pretende esplicitamente — ma senza dire quale campo manchi. Per il
    /// form significa che tre dei sei campi obbligatori danno un messaggio utile e tre no.
    ///
    /// È la stessa famiglia del 404 muto già tracciato sull'area (rifiuti della stored procedure
    /// tradotti in `NotFound()`), quindi si chiuderebbe con lo stesso intervento — la Fix 9, che
    /// cambia il contratto API e va concordata col frontend. Se un domani arrivasse un 400, questo
    /// test diventa rosso ed è il segnale di aggiornarlo alla forma nuova.
    /// </summary>
    [TestCase("tipologiaFattura", TestName = "CampoNonValidatoDallEndpoint(tipologiaFattura)")]
    [TestCase("anno", TestName = "CampoNonValidatoDallEndpoint(anno)")]
    [TestCase("mese", TestName = "CampoNonValidatoDallEndpoint(mese)")]
    public async Task CampoNonValidatoDallEndpoint_ShouldRifiutareSenzaScrivere_MaSenzaSpiegare(string campo)
    {
        var (stato, corpo) = await Invia(Corpo(ometti: campo));

        TestContext.Out.WriteLine($"omesso '{campo}' -> {(int)stato} {stato} | {corpo}");
        Assert.Multiple(() =>
        {
            Assert.That(RigheDelPeriodo(), Is.Zero,
                $"Omettendo '{campo}' la richiesta non deve lasciare righe: se ne restasse una, il form "
                + "potrebbe creare staging incompleto senza che nessuno se ne accorga.");
            Assert.That(stato, Is.EqualTo(HttpStatusCode.NotFound),
                "Caratterizzazione: oggi e' un 404, non un 400 come per azione/idEnte/nota.");
            Assert.That(corpo, Is.Empty, "E senza corpo: il client non sa quale campo manchi.");
        });
    }

    // =============================================================================================
    // Metà 2 — le finestre Anno/Mese per Azione + Tipologia
    // =============================================================================================

    [Test]
    public async Task Form_PerAzioneETipologiaAmmesse_ShouldOffrirePeriodi()
    {
        var anni = await Periodi(RottaAnni, "POSTICIPA", "PRIMO SALDO");

        Assert.That(anni, Is.Not.Null.And.Not.Empty,
            "POSTICIPA su PRIMO SALDO e' una combinazione ammessa: la tendina deve proporre gli anni.");
    }

    [TestCase("ELIMINA", "PRIMO SALDO", TestName = "CombinazioneNonAmmessa(ELIMINA su PRIMO SALDO)")]
    [TestCase("ELIMINA", "SECONDO SALDO", TestName = "CombinazioneNonAmmessa(ELIMINA su SECONDO SALDO)")]
    [TestCase("POSTICIPA", "ANTICIPO", TestName = "CombinazioneNonAmmessa(POSTICIPA su ANTICIPO)")]
    [TestCase("POSTICIPA", "ACCONTO", TestName = "CombinazioneNonAmmessa(POSTICIPA su ACCONTO)")]
    public async Task Form_PerCombinazioniNonAmmesse_ShouldNonOffrireAlcunPeriodo(string azione, string tipologia)
    {
        // E' qui che l'inconsistenza viene impedita: la vista non espone la combinazione, quindi dal
        // form non si puo' nemmeno comporre. (Sull'endpoint dell'azione la barriera non c'e' — v. la
        // nota in testa alla classe.)
        var anni = await Periodi(RottaAnni, azione, tipologia);

        Assert.That(anni, Is.Null,
            $"{azione} su {tipologia} non e' una combinazione ammessa: la tendina non deve proporre nulla.");
    }

    [Test]
    public async Task Form_LaFinestraDeiMesi_ShouldDipendereDallaTipologia()
    {
        // Le finestre partono da mesi diversi a seconda della tipologia (anticipo dal mese corrente,
        // acconto -1, primo saldo -2, secondo saldo -3) e arrivano tutte a dicembre dell'anno prossimo.
        // Sull'anno corrente questo si vede come un numero di mesi diverso.
        var anno = DateTime.Now.Year;
        var anticipo = await Periodi(RottaMesi, "ELIMINA", "ANTICIPO", anno);
        var secondoSaldo = await Periodi(RottaMesi, "POSTICIPA", "SECONDO SALDO", anno);

        Assert.That(anticipo, Is.Not.Null);
        Assert.That(secondoSaldo, Is.Not.Null);
        Assert.That(secondoSaldo!.Count, Is.GreaterThan(anticipo!.Count),
            "Il secondo saldo guarda piu' indietro dell'anticipo (-3 mesi contro 0), quindi sull'anno "
            + "corrente offre piu' mesi. Se le due finestre coincidessero, la differenziazione per "
            + "tipologia sarebbe andata persa.");
    }

    // =============================================================================================
    // `PF-672 TD-16`: *"il form deve permettere l'inserimento delle fatture future rispettando i
    // vincoli temporali della fatturazione, con vincoli differenti per azione e tipologia"*.
    //
    // TD-14 verifica che le finestre **esistano e differiscano**; qui si fissa la **regola** che le
    // genera, perché è quella a incarnare il vincolo di business:
    //
    //     da  (mese corrente − offset della tipologia)   a   dicembre dell'anno PROSSIMO
    //
    // con offset 0 per ANTICIPO, 1 per ACCONTO / VAR. SEMESTRALE / SEM. SOSPESI, 2 per PRIMO SALDO,
    // 3 per SECONDO SALDO — cioè tanto più indietro quanto più tardi arriva il documento nel ciclo.
    //
    // Le attese sono **calcolate** da DateTime.Now come fa la vista, non scritte a mano: una lista di
    // mesi hardcoded sarebbe verde oggi e rossa il mese prossimo, e verrebbe "aggiustata" ogni volta
    // invece di segnalare qualcosa.
    // =============================================================================================

    [TestCase("ELIMINA", "ANTICIPO", 0, TestName = "PrimoMeseOfferto(ANTICIPO, dal mese corrente)")]
    [TestCase("ELIMINA", "ACCONTO", 1, TestName = "PrimoMeseOfferto(ACCONTO, -1)")]
    [TestCase("POSTICIPA", "PRIMO SALDO", 2, TestName = "PrimoMeseOfferto(PRIMO SALDO, -2)")]
    [TestCase("POSTICIPA", "SECONDO SALDO", 3, TestName = "PrimoMeseOfferto(SECONDO SALDO, -3)")]
    [TestCase("POSTICIPA", "VAR. SEMESTRALE", 1, TestName = "PrimoMeseOfferto(VAR. SEMESTRALE, -1)")]
    [TestCase("POSTICIPA", "SEM. SOSPESI", 1, TestName = "PrimoMeseOfferto(SEM. SOSPESI, -1)")]
    public async Task Form_IlPrimoMeseOfferto_ShouldDipendereDallaTipologia(string azione, string tipologia, int offset)
    {
        var oggi = DateTime.Now;
        var mesi = await Periodi(RottaMesi, azione, tipologia, oggi.Year);

        Assert.That(mesi, Is.Not.Null, $"{azione} su {tipologia} deve essere una combinazione ammessa.");

        // Se il mese corrente meno l'offset cade nell'anno precedente, sull'anno in corso la finestra
        // parte comunque da gennaio.
        var atteso = Math.Max(1, oggi.Month - offset);
        Assert.That(mesi!.Select(int.Parse).Min(), Is.EqualTo(atteso),
            $"La finestra di {tipologia} deve partire da {atteso} (mese corrente {oggi.Month} meno "
            + $"{offset}). Un valore diverso significa che l'offset della tipologia e' cambiato.");
    }

    [Test]
    public async Task Form_GliAnniOfferti_ShouldEssereQuelloCorrenteEIlProssimo()
    {
        var oggi = DateTime.Now.Year;
        var anni = await Periodi(RottaAnni, "POSTICIPA", "PRIMO SALDO");

        Assert.That(anni, Is.Not.Null);
        Assert.That(anni!.Select(int.Parse), Is.EquivalentTo(new[] { oggi, oggi + 1 }),
            "Il vincolo temporale arriva a dicembre dell'anno prossimo: ne' oltre, ne' indietro.");
    }

    [Test]
    public async Task Form_PerLAnnoProssimo_ShouldOffrireTuttiIMesi()
    {
        var mesi = await Periodi(RottaMesi, "POSTICIPA", "PRIMO SALDO", DateTime.Now.Year + 1);

        Assert.That(mesi, Is.Not.Null);
        Assert.That(mesi!.Select(int.Parse), Is.EquivalentTo(Enumerable.Range(1, 12)),
            "L'anno prossimo e' interamente dentro la finestra: e' cosi' che si prenotano le fatture "
            + "future (v. TD-03 e TD-10).");
    }

    [Test]
    public async Task Form_OltreLAnnoProssimo_ShouldNonOffrireNulla()
    {
        var mesi = await Periodi(RottaMesi, "POSTICIPA", "PRIMO SALDO", DateTime.Now.Year + 2);

        Assert.That(mesi, Is.Null,
            "Il limite superiore e' dicembre dell'anno prossimo: piu' in la' non si prenota. Se un "
            + "domani la finestra si allargasse, questo test lo direbbe invece di lasciarlo passare.");
    }

    // =============================================================================================
    // Helper
    // =============================================================================================

    /// <summary>Corpo completo del form, eventualmente privato di un campo.</summary>
    private static string Corpo(string? ometti = null)
    {
        var campi = new Dictionary<string, string>
        {
            ["mese"] = $"\"{Mese}\"",
            ["anno"] = $"\"{Anno}\"",
            ["tipologiaFattura"] = "\"PRIMO SALDO\"",
            ["idEnte"] = $"\"{IdEnte}\"",
            ["azione"] = "\"Posticipa\"",
            ["nota"] = """{ "data": "2026-08-31T20:00:00", "testo": "campi obbligatori del form" }""",
            ["idFattura"] = "null"
        };
        if (ometti is not null) campi.Remove(ometti);
        return "{" + string.Join(",", campi.Select(x => $"\"{x.Key}\": {x.Value}")) + "}";
    }

    private async Task<(HttpStatusCode stato, string corpo)> Invia(string body)
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var resp = await client.PostAsync(_factory.WithNonce(Rotta),
            new StringContent(body, Encoding.UTF8, "application/json"));
        return (resp.StatusCode, await resp.Content.ReadAsStringAsync());
    }

    /// <summary>Gli anni o i mesi offerti dalla tendina, o `null` se la combinazione non è ammessa (404).</summary>
    private async Task<List<string>?> Periodi(string rotta, string azione, string tipologia, int? anno = null)
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var body = anno is null
            ? $$"""{ "azione": "{{azione}}", "tipologiaFattura": "{{tipologia}}" }"""
            : $$"""{ "azione": "{{azione}}", "tipologiaFattura": "{{tipologia}}", "anno": "{{anno}}" }""";
        var resp = await client.PostAsync(_factory.WithNonce(rotta),
            new StringContent(body, Encoding.UTF8, "application/json"));
        TestContext.Out.WriteLine($"{azione}/{tipologia}{(anno is null ? "" : $"/{anno}")} -> {(int)resp.StatusCode}");

        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.EnumerateArray()
            .Select(e => e.ValueKind == JsonValueKind.Object
                ? e.GetProperty(rotta.EndsWith("mesi") ? "mese" : "anno").ToString()
                : e.ToString())
            .ToList();
    }

    private static int RigheDelPeriodo()
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM cfg.GestioneFatture WHERE FkIdEnte='{IdEnte}' AND Anno={Anno} AND Mese={Mese}";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static void Pulisci()
    {
        try
        {
            using var conn = new SqlConnection(LocalTestDb.ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"DELETE FROM cfg.GestioneFatture WHERE FkIdEnte='{IdEnte}' AND Anno={Anno} AND Mese={Mese};";
            cmd.ExecuteNonQuery();
        }
        catch (SqlException) { /* best-effort */ }
    }
}
