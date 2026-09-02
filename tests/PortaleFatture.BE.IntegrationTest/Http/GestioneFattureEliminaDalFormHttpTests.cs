using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// I casi di testbook `PF-672 TD-07` e `TD-10`, che sul backend sono **lo stesso ramo** e per questo
/// stanno insieme: **eliminare dal form "aggiungi"** un periodo per cui la fattura **non esiste
/// ancora** — quindi *"le eliminate si vedono in griglia con stato ELIMINATA"* (TD-07) e *"è possibile
/// eliminare una fattura futura, non ancora calcolata dal processo DATA"* (TD-10).
///
/// Il periodo scelto (2027/12) è infatti futuro e privo di fattura: è il ramo **ELSE** della stored
/// procedure, la "pre-eliminazione" della RF06 — si prenota l'esclusione di qualcosa che il processo
/// DATA calcolerà più avanti. Il ramo **distruttivo**, con la fattura già emessa, è coperto da
/// `GestioneFattureEliminaDallaRigaHttpTests` (TD-08/TD-09).
///
/// La coppia rispecchia quella della posticipa: `TD-01`/`TD-03` stanno fra loro come `TD-07`/`TD-10`.
///
/// Completa il quadro degli stati sulla griglia, che a questo punto è tutto coperto e si legge in una
/// riga sola — la vista esclude **solo** `Stato = 2`:
///
/// | Azione | Stato | In griglia |
/// |---|---|---|
/// | Posticipa | 0 POSTICIPATA | resta |
/// | Ripristina | 1 RIPRISTINATA | resta |
/// | Annulla (`CANCELLA`) | 2 CANCELLATA | **sparisce** |
/// | Elimina | 3 ELIMINATA | resta |
///
/// **Cosa aggiunge rispetto a `GestioneFattureEliminaBodyRealeHttpTests`**, che copre già l'ELIMINA:
/// quel file replica il body segnalato dal frontend, con `idFattura` **valorizzato**, e verifica i tre
/// rami della stored procedure (fattura esistente non inviata, fattura assente, fattura già inviata).
/// Qui si entra invece **dal form**, quindi *per periodo* e senza id, e si verifica l'effetto sulla
/// **griglia** — che quel file non guarda.
///
/// ⚠️ **Contratto della riga dopo un'ELIMINA**: `FkIdFattura` resta **NULL** anche quando la fattura
/// esiste (sia l'INSERT sia l'UPDATE della SP azzerano la colonna). La chiave logica è quindi il
/// **periodo**: letture, cleanup e la successiva CANCELLA vanno fatti per periodo, non per id. È il
/// motivo per cui la pulizia di questa classe non filtra su `FkIdFattura`.
///
/// ⚠️ **Tipologie ammesse**: `ANTICIPO` e `ACCONTO`, più `PRIMO SALDO` **solo per l'ente INPS**
/// (eccezione hardcoded, coperta da `GestioneFattureRequisitiIntegrationTests`, qui non duplicata).
/// Su una tipologia di saldo l'azione viene rifiutata — v. il test in fondo.
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
public class GestioneFattureEliminaDalFormHttpTests
{
    private const string Rotta = "/api/fatture/pagopa/gestione-fatture/azione";
    private const string RottaMesi = "/api/fatture/pagopa/gestione-fatture/modifica/mesi";

    // Ente3 (PAL) e non ente1: il periodo 2027/12 e' riservato da GestioneFattureHttpTests, la cui
    // pulizia cancella per ente+anno+mese **ignorando la tipologia** — userebbe via le righe di questa
    // classe. Con un ente diverso le due chiavi non si incrociano.
    private const string IdEnte = "33333333-3333-3333-3333-333333333333";
    private const string Tipologia = "ANTICIPO";
    private const int Anno = 2027;
    private const int Mese = 12;

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
        // Guardia contro un falso verde: lo scenario "fattura futura" esiste solo finche' per quel
        // periodo non c'e' una fattura. Se il seed ne aggiungesse una, questi test proverebbero il
        // ramo distruttivo continuando a passare.
        if (FatturePerIlPeriodo("pfd.FattureTestata") > 0)
            Assert.Ignore($"Esiste una fattura per {Tipologia} {Anno}/{Mese}: questo file copre la "
                + "pre-eliminazione su periodo NON ancora calcolato. Spostare i test su un periodo libero.");
        Pulisci();
    }

    [TearDown]
    public void TearDown() => Pulisci();

    // =============================================================================================

    [Test]
    public async Task Elimina_DalForm_SenzaIdFattura_ShouldReturn200()
    {
        var (stato, corpo) = await Azione("Elimina");

        Assert.That(stato, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(corpo, Is.EqualTo("1"));
    }

    [Test]
    public async Task Elimina_ShouldRegistrareLoStatoEliminata()
    {
        await Azione("Elimina");

        var riga = LeggiRigaDelPeriodo();

        Assert.That(riga, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(riga!.Value.stato, Is.EqualTo(3), "3 = ELIMINATA.");
            Assert.That(riga.Value.azione, Is.EqualTo("ELIMINATA"));
            Assert.That(riga.Value.idFattura, Is.Null,
                "Dopo un'ELIMINA la colonna resta NULL per contratto: la chiave e' il periodo.");
            Assert.That(riga.Value.utenteInserimento, Is.EqualTo("integration-test-user"),
                "L'ELIMINA e' una CREAZIONE (il record nasce eliminato), quindi si traccia nelle "
                + "colonne di inserimento — non in quelle di cancellazione o ripristino, che sono le "
                + "due transizioni.");
        });
    }

    /// <summary>Il cuore di TD-07: l'eliminata resta visibile, con lo stato aggiornato.</summary>
    [Test]
    public async Task Elimina_ShouldComparireInGriglia_ConStatoEliminata()
    {
        Assert.That(await RigaInGriglia(), Is.Null, "Contro-prova: prima dell'azione il periodo non c'e'.");

        await Azione("Elimina");

        var riga = await RigaInGriglia();
        Assert.That(riga, Is.Not.Null,
            "La vista esclude solo Stato = 2: l'eliminata deve restare leggibile in griglia.");
        Assert.That(riga!.Value.GetProperty("azione").GetString(), Is.EqualTo("ELIMINATA"));
    }

    // =============================================================================================
    // `PF-672 TD-10`: la stessa azione letta come "fattura futura". Le due asserzioni qui sotto sono
    // ciò che distingue questo caso da TD-07, che guarda invece la griglia.
    // =============================================================================================

    [Test]
    public async Task FormAggiungi_ShouldOffrireIlPeriodoFuturoPerElimina()
    {
        // Come per la posticipa, i menu del form sono pura date-math: per ELIMINA/ANTICIPO la finestra
        // parte dal mese corrente e arriva a dicembre dell'anno prossimo. Senza questo controllo si
        // automatizzerebbe una strada che dall'interfaccia non si puo' percorrere.
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var body = $$"""{ "azione": "ELIMINA", "tipologiaFattura": "{{Tipologia}}", "anno": "{{Anno}}" }""";
        var resp = await client.PostAsync(_factory.WithNonce(RottaMesi),
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var mesi = doc.RootElement.EnumerateArray()
            .Select(e => int.Parse(e.GetProperty("mese").GetString()!))
            .ToArray();
        TestContext.Out.WriteLine($"mesi offerti per ELIMINA {Tipologia} {Anno}: {string.Join(",", mesi)}");

        Assert.That(mesi, Does.Contain(Mese),
            $"Il form non offre {Mese}/{Anno} per ELIMINA su {Tipologia}: se questo test diventa rosso "
            + "col passare del tempo, e' la finestra date-math ad essere scivolata, non un difetto.");
    }

    [Test]
    public async Task Elimina_PeriodoFuturo_ShouldNonCreareNeSpostareFatture()
    {
        // La pre-eliminazione registra solo lo stato: non esiste una fattura da spostare, e non se ne
        // deve inventare una. E' il contrario del ramo distruttivo (TD-08/TD-09).
        await Azione("Elimina");

        Assert.Multiple(() =>
        {
            Assert.That(FatturePerIlPeriodo("pfd.FattureTestata"), Is.Zero,
                "La fattura la creera' il processo DATA: l'azione non deve materializzarla.");
            Assert.That(FatturePerIlPeriodo("pfd.FattureTestata_Eliminate"), Is.Zero,
                "E non c'e' nulla da spostare in _Eliminate.");
        });
    }

    /// <summary>
    /// L'ELIMINA è ammessa su `ANTICIPO`/`ACCONTO` (più `PRIMO SALDO` per il solo INPS): su una
    /// tipologia di saldo la stored procedure rifiuta.
    ///
    /// ⚠️ Il rifiuto arriva al client come **404 muto**, lo stesso difetto già visto su annulla e
    /// ripristina: la SP fa la sua parte, è l'endpoint a tradurre `Result 0` in `NotFound()` senza
    /// spiegare. Qui si fissa il comportamento attuale.
    /// </summary>
    [Test]
    public async Task Elimina_SuTipologiaDiSaldo_ShouldEssereRifiutata()
    {
        var (stato, corpo) = await Azione("Elimina", tipologia: "SECONDO SALDO");

        Assert.Multiple(() =>
        {
            Assert.That(stato, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(corpo, Is.Empty);
            Assert.That(RigheDelPeriodo("SECONDO SALDO"), Is.Zero,
                "Un rifiuto non deve lasciare righe: se ne restasse una, il periodo risulterebbe "
                + "eliminato pur avendo l'utente ricevuto un errore.");
        });
    }

    // =============================================================================================
    // Helper
    // =============================================================================================

    private async Task<(HttpStatusCode stato, string corpo)> Azione(string azione, string? tipologia = null)
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var body = $$"""
        {
            "mese": "{{Mese}}",
            "anno": "{{Anno}}",
            "tipologiaFattura": "{{tipologia ?? Tipologia}}",
            "idEnte": "{{IdEnte}}",
            "azione": "{{azione}}",
            "nota": { "data": "2026-08-31T19:00:00", "testo": "azione {{azione}} dal form" },
            "idFattura": null
        }
        """;
        var resp = await client.PostAsync(_factory.WithNonce(Rotta),
            new StringContent(body, Encoding.UTF8, "application/json"));
        var corpo = await resp.Content.ReadAsStringAsync();
        TestContext.Out.WriteLine($"{azione} {tipologia ?? Tipologia} -> {(int)resp.StatusCode} {resp.StatusCode} | {corpo}");
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

    private static (int stato, string? azione, long? idFattura, string? utenteInserimento)? LeggiRigaDelPeriodo()
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
            SELECT TOP(1) Stato, Azione, FkIdFattura, IdUtenteInserimento FROM cfg.GestioneFatture
            WHERE FkIdEnte='{IdEnte}' AND FkTipologiaFattura='{Tipologia}' AND Anno={Anno} AND Mese={Mese}";
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return (reader.GetInt32(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetInt64(2),
                reader.IsDBNull(3) ? null : reader.GetString(3));
    }

    private static int FatturePerIlPeriodo(string tabella)
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"SELECT COUNT(*) FROM {tabella}
            WHERE FkIdEnte='{IdEnte}' AND FkTipologiaFattura='{Tipologia}'
              AND AnnoRiferimento={Anno} AND MeseRiferimento={Mese}";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static int RigheDelPeriodo(string? tipologia = null)
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"SELECT COUNT(*) FROM cfg.GestioneFatture
            WHERE FkIdEnte='{IdEnte}' AND FkTipologiaFattura='{tipologia ?? Tipologia}'
              AND Anno={Anno} AND Mese={Mese}";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static void Pulisci()
    {
        try
        {
            using var conn = new SqlConnection(LocalTestDb.ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            // Per PERIODO e per entrambe le tipologie toccate: dopo un'ELIMINA la colonna FkIdFattura
            // e' NULL, quindi un cleanup per id non troverebbe nulla.
            cmd.CommandText = $@"DELETE FROM cfg.GestioneFatture
                WHERE FkIdEnte='{IdEnte}' AND Anno={Anno} AND Mese={Mese};";
            cmd.ExecuteNonQuery();
        }
        catch (SqlException) { /* best-effort */ }
    }
}
