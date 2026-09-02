using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Il caso di testbook "**è possibile posticipare una fattura già emessa ma con stato NON INVIATA**",
/// dal form "aggiungi" della pagina Gestione Fatture.
///
/// È il gemello di `GestioneFatturePosticipaBodyRealeHttpTests`, che copre lo stesso flusso su un
/// periodo **senza** fattura (ramo "pre-generazione"). Vanno letti in coppia, perché la differenza è
/// osservabile e non ovvia: nel ramo con fattura esistente la stored procedure fa
///
///     SELECT @IdFattura = IdFattura FROM pfd.FattureTestata WHERE anno/mese/ente/tipologia
///
/// quindi la riga di `cfg.GestioneFatture` nasce con **`FkIdFattura` valorizzato anche se il client ha
/// mandato `"idFattura": null`** — il server risolve la fattura dal periodo. Senza fattura, invece, la
/// stessa richiesta lascia la colonna NULL. Chi legge la griglia vede quindi "Id Fattura" pieno o vuoto
/// a seconda di uno stato del database, non di cosa ha inviato il portale.
///
/// Seed dedicato (`tests/Data/gestione_fatture.sql`): ente isolato + contratto + la fattura 7501,
/// `VAR. SEMESTRALE` 2026/7 con `FatturaInviata = 0`. L'ente è tutto suo apposta: il periodo 2026/7 è
/// usato da altre classi con altre tipologie, e un ente dedicato garantisce che nessun'altra
/// asserzione incontri questa riga.
///
/// ⚠️ Cosa **non** copre: il rifiuto su una fattura **già inviata** (`FatturaInviata = 1`). La SP
/// dovrebbe rifiutarla, ma oggi non lo fa — la guardia è codice morto perché il conteggio calcolato su
/// `FattureTestata` viene sovrascritto da una tabella variabile mai popolata. È già tracciato dal test
/// `[Ignore]` `Posticipa_OnAlreadySentInvoice_ShouldBeRejected`, e va chiuso lì, non qui.
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
public class GestioneFatturePosticipaFatturaEmessaHttpTests
{
    private const string Rotta = "/api/fatture/pagopa/gestione-fatture/azione";

    private const string IdEnte = "77777777-7777-7777-7777-777777777777";
    private const string Tipologia = "VAR. SEMESTRALE";
    private const int Anno = 2026;
    private const int Mese = 7;
    private const long IdFatturaSeed = 7501;

    private const string Testo = "posticipo di una fattura emessa ma non inviata";

    /// <summary>Il body del form "aggiungi": nessun `idFattura`, la chiave è il periodo.</summary>
    private static readonly string BodyReale = $$"""
    {
        "mese": "{{Mese}}",
        "anno": "{{Anno}}",
        "tipologiaFattura": "{{Tipologia}}",
        "idEnte": "{{IdEnte}}",
        "azione": "Posticipa",
        "nota": { "data": "2026-08-31T17:05:00", "testo": "{{Testo}}" },
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
        SaltaSeIlSeedManca();
        Pulisci();
    }

    [TearDown]
    public void TearDown() => Pulisci();

    // =============================================================================================

    [Test]
    public async Task Posticipa_SuFatturaEmessaNonInviata_ShouldReturn200()
    {
        var (stato, corpo) = await Posticipa();

        Assert.That(stato, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(corpo, Is.EqualTo("1"),
            "Una fattura emessa ma NON inviata e' posticipabile: e' il caso d'uso della pagina.");
    }

    [Test]
    public async Task Posticipa_SenzaIdFatturaNelBody_ShouldRisolvereLaFatturaDalPeriodo()
    {
        // È la differenza sostanziale rispetto al caso senza fattura, dove la colonna resta NULL.
        await Posticipa();

        var riga = LeggiRigaDelPeriodo();

        Assert.That(riga, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(riga!.Value.stato, Is.Zero);
            Assert.That(riga.Value.azione, Is.EqualTo("POSTICIPATA"));
            Assert.That(riga.Value.idFattura, Is.EqualTo(IdFatturaSeed),
                "Il client ha mandato idFattura null: e' la stored procedure a risolverlo dal periodo. "
                + "Se qui arrivasse NULL, il legame fra riga di staging e fattura sarebbe perso.");
        });
    }

    [Test]
    public async Task Griglia_PrimaDellaPosticipa_ShouldNonContenereIlPeriodo()
    {
        Assert.That(await RigaInGriglia(), Is.Null);
    }

    [Test]
    public async Task Posticipa_ShouldComparireInGriglia_ConStatoPosticipataEIdFattura()
    {
        await Posticipa();

        var riga = await RigaInGriglia();

        Assert.That(riga, Is.Not.Null,
            "La riga c'e' a DB ma non arriva in griglia: guardare i tre INNER JOIN della vista "
            + "(ente senza contratto o TipoContratto non risolto) prima del codice C#.");
        Assert.Multiple(() =>
        {
            Assert.That(riga!.Value.GetProperty("azione").GetString(), Is.EqualTo("POSTICIPATA"));
            Assert.That(riga.Value.GetProperty("tipologiaFattura").GetString(), Is.EqualTo(Tipologia));
            Assert.That(riga.Value.GetProperty("idFattura").GetInt64(), Is.EqualTo(IdFatturaSeed),
                "In griglia la colonna Id Fattura e' valorizzata, a differenza del caso senza fattura.");
        });
    }

    // =============================================================================================
    // `PF-672 TD-06`: stessa fattura, ALTRO PUNTO DI INGRESSO — il pulsante "Posticipa" sulla riga
    // della fattura in **Documenti Emessi**, dove il frontend conosce la fattura e ne manda l'id.
    //
    // Il caso di testbook si chiude verificando che la fattura compaia poi nella griglia di Gestione
    // Fatture con stato POSTICIPATA — cioè lo stesso esito del form "aggiungi". La proprietà che vale
    // la pena fissare è proprio questa: **le due vie convergono**, e il server non si fida dell'id
    // che riceve.
    // =============================================================================================

    [Test]
    public async Task Posticipa_DallaRigaEmessa_ConIdFattura_ShouldReturn200_EComparireInGriglia()
    {
        var (stato, corpo) = await Posticipa(idFattura: IdFatturaSeed);

        Assert.That(stato, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(corpo, Is.EqualTo("1"));

        var riga = await RigaInGriglia();
        Assert.That(riga, Is.Not.Null);
        Assert.That(riga!.Value.GetProperty("azione").GetString(), Is.EqualTo("POSTICIPATA"));
    }

    [Test]
    public async Task Posticipa_DalFormODallaRigaEmessa_ShouldProdurreLaStessaRiga()
    {
        // Via 1: form "aggiungi", nessun idFattura.
        await Posticipa();
        var dalForm = LeggiRigaDelPeriodo();
        Pulisci();

        // Via 2: pulsante sulla riga in Documenti Emessi, con l'id della fattura.
        await Posticipa(idFattura: IdFatturaSeed);
        var dallaRiga = LeggiRigaDelPeriodo();

        Assert.That(dallaRiga, Is.EqualTo(dalForm),
            "I due punti di ingresso devono convergere sulla stessa riga di staging: stato, azione e "
            + "id fattura. Se divergessero, la pagina da cui si e' agito cambierebbe il dato — e "
            + "l'operatore non avrebbe modo di saperlo.");
    }

    /// <summary>
    /// Il server **non si fida dell'id ricevuto**: nel ramo con fattura esistente la SP lo
    /// sovrascrive con quello risolto dal periodo (ente/tipologia/anno/mese). Mandare l'id di
    /// un'altra fattura non permette quindi di marcarla.
    ///
    /// ⚠️ Attenzione al rovescio della medaglia, che qui **non** si verifica ma va conosciuto: quel
    /// `SELECT @IdFattura = …` è una assegnazione da SELECT, e se non trova righe **lascia la
    /// variabile invariata**. Su un periodo *senza* fattura, quindi, l'id mandato dal client
    /// sopravvive e finisce nella riga di staging — dove punterebbe a una fattura che non esiste.
    /// </summary>
    [Test]
    public async Task Posticipa_ConIdFatturaIncoerente_ShouldRisolvereQuelloDelPeriodo()
    {
        await Posticipa(idFattura: 999999);

        Assert.That(LeggiRigaDelPeriodo()!.Value.idFattura, Is.EqualTo(IdFatturaSeed),
            "L'id mandato dal client viene scartato in favore di quello del periodo.");
    }

    /// <summary>
    /// La POSTICIPA è un'azione di **staging**: sposta la fattura fuori dall'invio a SAP, ma non la
    /// tocca. È il contrario dell'ELIMINA, che sposta fisicamente la riga nelle tabelle `*_Eliminate`.
    /// Se un domani la posticipa iniziasse a modificare `pfd.FattureTestata`, questo test lo direbbe.
    /// </summary>
    [Test]
    public async Task Posticipa_ShouldNonModificareLaFattura()
    {
        var prima = LeggiFattura();
        await Posticipa();
        var dopo = LeggiFattura();

        Assert.Multiple(() =>
        {
            Assert.That(dopo, Is.Not.Null, "La fattura non deve sparire: la posticipa non elimina nulla.");
            Assert.That(dopo!.Value.inviata, Is.False, "FatturaInviata resta 0.");
            Assert.That(dopo.Value.totale, Is.EqualTo(prima!.Value.totale));
        });
    }

    // =============================================================================================
    // Helper
    // =============================================================================================

    /// <summary>
    /// Posticipa col body del form (`idFattura` nullo) oppure, passando un id, con quello che il
    /// frontend manda dalla riga di Documenti Emessi.
    /// </summary>
    private async Task<(HttpStatusCode stato, string corpo)> Posticipa(long? idFattura = null)
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var corpoRichiesta = idFattura is null
            ? BodyReale
            : BodyReale.Replace("\"idFattura\": null", $"\"idFattura\": {idFattura}");
        var content = new StringContent(corpoRichiesta, Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(_factory.WithNonce(Rotta), content);
        var corpo = await resp.Content.ReadAsStringAsync();
        TestContext.Out.WriteLine($"POST azione -> {(int)resp.StatusCode} {resp.StatusCode} | {corpo}");
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
        TestContext.Out.WriteLine($"POST griglia -> {(int)resp.StatusCode} {resp.StatusCode}");

        if (resp.StatusCode == HttpStatusCode.NotFound) return null; // lista vuota => 404, contratto dell'area
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        foreach (var item in doc.RootElement.GetProperty("gestioneFatture").EnumerateArray())
            if (item.GetProperty("anno").GetInt32() == Anno && item.GetProperty("mese").GetInt32() == Mese)
                return item.Clone();

        return null;
    }

    private static (int stato, string? azione, long? idFattura)? LeggiRigaDelPeriodo()
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
            SELECT TOP(1) Stato, Azione, FkIdFattura FROM cfg.GestioneFatture
            WHERE FkIdEnte='{IdEnte}' AND FkTipologiaFattura='{Tipologia}' AND Anno={Anno} AND Mese={Mese}";
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return (reader.GetInt32(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetInt64(2));
    }

    private static (bool inviata, double totale)? LeggiFattura()
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT FatturaInviata, TotaleFattura FROM pfd.FattureTestata WHERE IdFattura={IdFatturaSeed}";
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return (!reader.IsDBNull(0) && reader.GetBoolean(0), reader.GetDouble(1));
    }

    /// <summary>
    /// Guardia contro un falso verde: senza la fattura seedata questi test proverebbero il ramo
    /// "pre-generazione", cioè esattamente l'altro caso, e passerebbero lo stesso senza dire nulla.
    /// </summary>
    private static void SaltaSeIlSeedManca()
    {
        if (LeggiFattura() is null)
            Assert.Ignore($"Manca la fattura {IdFatturaSeed} ({Tipologia} {Anno}/{Mese}, FatturaInviata=0) "
                + "nel DB seedato: senza, questi test verificherebbero il ramo senza fattura. "
                + "Rigenerare il container (da tests/: docker compose down -v && docker compose up -d --build).");
    }

    private static void Pulisci()
    {
        try
        {
            using var conn = new SqlConnection(LocalTestDb.ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"DELETE FROM cfg.GestioneFatture WHERE FkIdEnte='{IdEnte}';";
            cmd.ExecuteNonQuery();
        }
        catch (SqlException) { /* best-effort */ }
    }
}
