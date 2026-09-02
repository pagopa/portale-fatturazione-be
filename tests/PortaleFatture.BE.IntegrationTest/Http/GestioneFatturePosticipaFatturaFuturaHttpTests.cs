using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Il caso di testbook `PF-672 TD-03`: **posticipare una fattura futura**, cioè un periodo che il
/// processo DATA non ha ancora calcolato e che quindi non compare in Documenti Emessi.
///
/// Terzo dei tre rami della POSTICIPA, e quello meno intuitivo: si agisce su qualcosa che **non
/// esiste ancora**. I tre vanno letti insieme:
///
/// | Caso | Fattura in `pfd.FattureTestata` | `FkIdFattura` sulla riga di staging |
/// |---|---|---|
/// | `TD-01` / `TD-02` con fattura emessa non inviata | c'è | **valorizzato** (risolto dalla SP dal periodo) |
/// | `TD-03` fattura futura (questo file) | non c'è | **NULL** |
///
/// **Perché funziona, ed è voluto**: `cfg.GestioneFatture` è una tabella di *staging* la cui chiave
/// logica è il **periodo** (ente/tipologia/anno/mese), non l'id della fattura. Prenotare l'esclusione
/// prima che la fattura esista è quindi legittimo — quando il processo DATA la calcolerà, la troverà
/// già marcata da escludere.
///
/// **Il form permette davvero di arrivarci** (verificato, non dedotto): gli anni/mesi di
/// `be.vwGestioneFattureFormAnniMesi` sono **pura date-math**, senza alcuna dipendenza da tabelle —
/// per `PRIMO SALDO` la finestra va da *mese corrente − 2* fino a **dicembre dell'anno prossimo**.
/// Un periodo futuro è quindi offerto dai menu a tendina per costruzione, non per caso.
///
/// ⚠️ Nessuno valida il periodo lato SP (mese 0, mese 99999 e anno negativo vengono accettati e
/// scritti — difetto già tracciato da `ExtremeAnnoMese_ShouldNotCrash_AndNoOp`). Questi test provano
/// che il caso **legittimo** funziona, non che quelli assurdi siano rifiutati: quello è un buco aperto.
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
public class GestioneFatturePosticipaFatturaFuturaHttpTests
{
    private const string Rotta = "/api/fatture/pagopa/gestione-fatture/azione";
    private const string RottaMesi = "/api/fatture/pagopa/gestione-fatture/modifica/mesi";

    // Periodo FUTURO riservato a questa classe: nessuna fattura esiste per ente1/PRIMO SALDO/2026-12,
    // ed è la condizione stessa dello scenario. Se un domani il seed ne aggiungesse una, la guardia
    // in [SetUp] se ne accorge invece di far passare il test provando un altro ramo.
    private const string IdEnte = "11111111-1111-1111-1111-111111111111";
    private const string Tipologia = "PRIMO SALDO";
    private const int Anno = 2026;
    private const int Mese = 12;

    private const string Testo = "prenotazione su periodo non ancora calcolato dal processo DATA";

    private static readonly string BodyReale = $$"""
    {
        "mese": "{{Mese}}",
        "anno": "{{Anno}}",
        "tipologiaFattura": "{{Tipologia}}",
        "idEnte": "{{IdEnte}}",
        "azione": "Posticipa",
        "nota": { "data": "2026-08-31T17:30:00", "testo": "{{Testo}}" },
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
        SaltaSeLaFatturaEsiste();
        Pulisci();
    }

    [TearDown]
    public void TearDown() => Pulisci();

    // =============================================================================================

    [Test]
    public async Task FormAggiungi_ShouldOffrireIlPeriodoFuturo()
    {
        // Senza questo, il resto del capitolo proverebbe una strada che dall'interfaccia non si può
        // percorrere: i menu del form sono l'unico modo per scegliere il periodo.
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var body = $$"""{ "azione": "POSTICIPA", "tipologiaFattura": "{{Tipologia}}", "anno": "{{Anno}}" }""";
        var resp = await client.PostAsync(_factory.WithNonce(RottaMesi),
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // La risposta e' una lista di OGGETTI { mese, descrizione }, col mese come STRINGA — non una
        // lista di interi come lascerebbe pensare la query (`IRequest<IEnumerable<int>>`): e' l'endpoint
        // a proiettarli in FattureMeseResponse aggiungendo il nome del mese per la tendina.
        // Si confronta sui numeri estratti, non sul testo della risposta: "12" come sottostringa
        // matcherebbe anche un 2012 o la descrizione, e darebbe un verde falso.
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var mesi = doc.RootElement.EnumerateArray()
            .Select(e => int.Parse(e.GetProperty("mese").GetString()!))
            .ToArray();
        TestContext.Out.WriteLine($"mesi offerti: {string.Join(",", mesi)}");

        Assert.That(mesi, Does.Contain(Mese),
            $"Il form non offre il mese {Mese}/{Anno} per {Tipologia}: la finestra e' pura date-math "
            + "(da mese corrente -2 fino a dicembre dell'anno prossimo, per il primo saldo). Se questo "
            + "test diventa rosso a fine anno, e' la finestra ad essere scivolata, non un difetto.");
    }

    [Test]
    public async Task Posticipa_PeriodoFuturoSenzaFattura_ShouldReturn200()
    {
        var (stato, corpo) = await Posticipa();

        Assert.That(stato, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(corpo, Is.EqualTo("1"),
            "Si puo' posticipare un periodo non ancora calcolato: la chiave dello staging e' il "
            + "periodo, non la fattura.");
    }

    [Test]
    public async Task Posticipa_PeriodoFuturo_ShouldLasciareIdFatturaNullo()
    {
        await Posticipa();

        var riga = LeggiRigaDelPeriodo();

        Assert.That(riga, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(riga!.Value.stato, Is.Zero);
            Assert.That(riga.Value.azione, Is.EqualTo("POSTICIPATA"));
            Assert.That(riga.Value.idFattura, Is.Null,
                "Non c'e' nulla da risolvere: la fattura non esiste ancora. E' il contrario del caso "
                + "TD-02, dove la SP risolve l'id dal periodo e la colonna risulta valorizzata.");
        });
    }

    [Test]
    public async Task Posticipa_PeriodoFuturo_ShouldNonCreareLaFattura()
    {
        // Prenotare l'esclusione non deve inventare una fattura: quella la creera' il processo DATA.
        await Posticipa();

        Assert.That(FatturePerIlPeriodo(), Is.Zero,
            "La posticipa scrive solo nello staging: pfd.FattureTestata non va toccata.");
    }

    [Test]
    public async Task Posticipa_PeriodoFuturo_ShouldComparireInGriglia()
    {
        await Posticipa();

        var riga = await RigaInGriglia();

        Assert.That(riga, Is.Not.Null,
            "L'admin deve poter rivedere cio' che ha prenotato, anche se la fattura non esiste: la "
            + "vista della griglia non joina FattureTestata, quindi la riga compare comunque.");
        Assert.Multiple(() =>
        {
            Assert.That(riga!.Value.GetProperty("azione").GetString(), Is.EqualTo("POSTICIPATA"));
            Assert.That(riga.Value.GetProperty("idFattura").ValueKind, Is.EqualTo(JsonValueKind.Null),
                "In griglia la colonna Id Fattura resta vuota finche' la fattura non esiste.");
        });
    }

    /// <summary>
    /// Contro-verifica del meccanismo che sul periodo *con* fattura protegge dall'id sbagliato
    /// (v. `GestioneFatturePosticipaFatturaEmessaHttpTests`): lì la SP sovrascrive l'id ricevuto con
    /// quello risolto dal periodo. Qui non c'è nulla da risolvere — e `SELECT @var = …` che non trova
    /// righe **lascia la variabile invariata**, quindi l'id del client sopravvive.
    ///
    /// Questo test **caratterizza** l'esito reale invece di darlo per scontato: se un giorno la SP
    /// azzerasse la variabile prima della SELECT, diventerebbe rosso e ci si accorgerebbe del cambio.
    /// </summary>
    [Test]
    public async Task Posticipa_PeriodoFuturo_ConIdFatturaSpurio_Caratterizzazione()
    {
        await Posticipa(idFattura: 999999);

        var idScritto = LeggiRigaDelPeriodo()!.Value.idFattura;
        TestContext.Out.WriteLine($"FkIdFattura scritto: {(idScritto?.ToString() ?? "NULL")}");

        Assert.That(idScritto, Is.EqualTo(999999L),
            "Su un periodo senza fattura l'id ricevuto NON viene sostituito: resta nella riga di "
            + "staging e punta a una fattura inesistente. Non e' un problema per il flusso reale (il "
            + "portale manda null dal form), ma e' un dato incoerente che nessuno rifiuta: se questo "
            + "test diventa rosso, qualcuno ha irrobustito la SP ed e' una buona notizia.");
    }

    /// <summary>
    /// DIFETTO APERTO (basso impatto, nessuna urgenza) — l'aspettativa **corretta** che fa da coppia
    /// alla caratterizzazione qui sopra: una riga di staging non deve mai puntare a una fattura che
    /// non esiste.
    ///
    /// L'asserzione è volutamente scritta sull'**invariante**, non sul rimedio: chi ripara è libero di
    /// scegliere se rifiutare la richiesta (400) o ignorare l'id incoerente scrivendo NULL — in
    /// entrambi i casi questo test diventa verde. Togliere allora l'`[Ignore]` e cancellare la
    /// caratterizzazione gemella.
    ///
    /// Perché non è urgente: dal form il portale manda `null`, quindi il flusso reale non lo produce;
    /// e le letture/cleanup dell'area vanno per **periodo**, non per `FkIdFattura`, quindi un id
    /// sbagliato resta in gran parte inerte. Resta però input non validato che arriva a database, su
    /// una colonna che per contratto identifica una fattura.
    /// </summary>
    [Test]
    [Ignore("DIFETTO APERTO — un idFattura incoerente col periodo viene scritto tale e quale quando la "
        + "fattura non esiste ancora: la riga di staging finisce con FkIdFattura verso una fattura "
        + "inesistente. Causa: 'SELECT @IdFattura = ... WHERE <periodo>' non trova righe e lascia la "
        + "variabile col valore ricevuto dal client. Rimedio a scelta: rifiutare con 400, oppure "
        + "azzerare la variabile prima della SELECT. V. coverage/test-backlog.md.")]
    public async Task Posticipa_ConIdFatturaIncoerente_ShouldNonPuntareAUnaFatturaInesistente()
    {
        await Posticipa(idFattura: 999999);

        var idScritto = LeggiRigaDelPeriodo()!.Value.idFattura;

        Assert.That(idScritto is null || FatturaEsiste(idScritto.Value), Is.True,
            $"FkIdFattura = {idScritto} non corrisponde ad alcuna fattura.");
    }

    // =============================================================================================
    // Helper
    // =============================================================================================

    private static bool FatturaEsiste(long idFattura)
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM pfd.FattureTestata WHERE IdFattura = {idFattura}";
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private async Task<(HttpStatusCode stato, string corpo)> Posticipa(long? idFattura = null)
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var corpoRichiesta = idFattura is null
            ? BodyReale
            : BodyReale.Replace("\"idFattura\": null", $"\"idFattura\": {idFattura}");
        var resp = await client.PostAsync(_factory.WithNonce(Rotta),
            new StringContent(corpoRichiesta, Encoding.UTF8, "application/json"));
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

        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
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

    private static int FatturePerIlPeriodo()
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"SELECT COUNT(*) FROM pfd.FattureTestata
            WHERE FkIdEnte='{IdEnte}' AND FkTipologiaFattura='{Tipologia}'
              AND AnnoRiferimento={Anno} AND MeseRiferimento={Mese}";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>
    /// Guardia contro un falso verde speculare a quella dell'altro file: qui lo scenario esiste solo
    /// **finché** la fattura non c'è. Se il seed ne aggiungesse una su questo periodo, questi test
    /// proverebbero il ramo di TD-02 continuando a passare.
    /// </summary>
    private static void SaltaSeLaFatturaEsiste()
    {
        if (FatturePerIlPeriodo() > 0)
            Assert.Ignore($"Esiste una fattura per {Tipologia} {Anno}/{Mese}: questo scenario richiede "
                + "un periodo NON ancora calcolato. Spostare i test su un periodo libero.");
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
