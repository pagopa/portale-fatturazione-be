using System.Net;
using System.Text;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Il caso di testbook `PF-672 TD-15`: *"ogni azione deve richiedere l'inserimento obbligatorio della
/// Nota, nella quale può essere inserito un testo non più lungo di 500 caratteri e con minimo 10"*.
///
/// Il requisito ha tre parti, e sul backend hanno esiti diversi:
///
/// | Parte del requisito | Sul backend |
/// |---|---|
/// | nota **obbligatoria** | ✅ validata: senza nota → 400 con messaggio |
/// | testo **≥ 10** caratteri | ❌ nessun controllo |
/// | testo **≤ 500** caratteri | ❌ nessun controllo |
///
/// I due limiti vivono quindi **solo nel form**. Può essere una scelta deliberata — una validazione di
/// usabilità lato client — ma va saputa per quello che è: **l'API accetta qualunque lunghezza**, e
/// chiunque non passi dalla pagina (o un domani un'altra pagina che riusi la rotta) può scrivere note
/// fuori limite. I test qui sotto fissano il comportamento **reale**; l'aspettativa del testbook resta
/// scritta in un test `[Ignore]`, così se e quando si decidesse di applicare il vincolo lato server
/// diventerebbe verde da solo.
///
/// C'è però un'invariante più importante dei due limiti, e quella **regge**: la nota non viene mai
/// **troncata in silenzio**. Un testo lungo viene rifiutato o memorizzato per intero, mai tagliato —
/// è la stessa proprietà verificata sulle Api Key, ed è ciò che distingue un limite non applicato da
/// una perdita di dati.
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
public class GestioneFattureNotaHttpTests
{
    private const string Rotta = "/api/fatture/pagopa/gestione-fatture/azione";

    private const string IdEnte = "11111111-1111-1111-1111-111111111111";
    private const string Tipologia = "PRIMO SALDO";
    private const int Anno = 2027;
    private const int Mese = 7;

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
    // La parte del requisito che il backend applica
    // =============================================================================================

    [Test]
    public async Task Nota_Assente_ShouldReturn400_ConMessaggio()
    {
        var (stato, corpo) = await Posticipa(testo: null);

        Assert.Multiple(() =>
        {
            Assert.That(stato, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(corpo, Is.Not.Empty, "Qui il 400 spiega: e' uno dei tre campi validati a monte.");
            Assert.That(RigheDelPeriodo(), Is.Zero);
        });
    }

    [Test]
    public async Task Nota_ConTestoDiLunghezzaAmmessa_ShouldEssereAccettata()
    {
        var testo = new string('a', 100);

        var (stato, _) = await Posticipa(testo);

        Assert.That(stato, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(TestoDellaNota(), Is.EqualTo(testo), "La nota va memorizzata cosi' com'e'.");
    }

    // =============================================================================================
    // Le due parti che il backend NON applica — caratterizzazione
    // =============================================================================================

    [TestCase(1, TestName = "NotaFuoriLimite_OggiAccettata(1 carattere)")]
    [TestCase(9, TestName = "NotaFuoriLimite_OggiAccettata(9 caratteri, sotto il minimo)")]
    [TestCase(501, TestName = "NotaFuoriLimite_OggiAccettata(501 caratteri, oltre il massimo)")]
    [TestCase(5000, TestName = "NotaFuoriLimite_OggiAccettata(5000 caratteri)")]
    public async Task NotaFuoriLimite_OggiAccettata_Caratterizzazione(int lunghezza)
    {
        var testo = new string('x', lunghezza);

        var (stato, corpo) = await Posticipa(testo);

        TestContext.Out.WriteLine($"{lunghezza} caratteri -> {(int)stato} {stato} | {corpo}");
        Assert.Multiple(() =>
        {
            Assert.That(stato, Is.EqualTo(HttpStatusCode.OK),
                "Oggi l'API non applica i limiti 10/500 del form: li accetta tutti. Se questo test "
                + "diventa rosso, il vincolo e' stato portato lato server — togliere l'[Ignore] "
                + "del test gemello e cancellare questa caratterizzazione.");
            Assert.That(TestoDellaNota(), Has.Length.EqualTo(lunghezza),
                "E soprattutto: la nota NON viene troncata in silenzio. Un limite non applicato e' un "
                + "problema di validazione; un troncamento silenzioso sarebbe una perdita di dati.");
        });
    }

    /// <summary>
    /// L'aspettativa del testbook, oggi non soddisfatta dal backend: fuori dai limiti 10–500 la
    /// richiesta dovrebbe essere respinta.
    ///
    /// ATTENZIONE **Da confermare col prodotto prima di "correggere"**: può essere una scelta deliberata tenere
    /// il vincolo nel solo form, come regola di usabilità. In quel caso questo test va cancellato e la
    /// caratterizzazione qui sopra resta da sola a documentare che l'API è permissiva. Se invece il
    /// vincolo è di dominio, il rimedio è una validazione sull'endpoint accanto a quelle già presenti
    /// per azione, idEnte e nota.
    /// </summary>
    [TestCase(9, TestName = "NotaFuoriLimite_ShouldReturn400(sotto il minimo)")]
    [TestCase(501, TestName = "NotaFuoriLimite_ShouldReturn400(oltre il massimo)")]
    [Ignore("REQUISITO NON IMPLEMENTATO lato backend — i limiti 10/500 caratteri della nota vivono solo "
        + "nel form: l'API accetta qualunque lunghezza. Da confermare col prodotto se il vincolo debba "
        + "valere anche server-side; in caso affermativo, validazione sull'endpoint. "
        + "V. coverage/test-backlog.md.")]
    public async Task NotaFuoriLimite_ShouldReturn400(int lunghezza)
    {
        var (stato, _) = await Posticipa(new string('x', lunghezza));

        Assert.That(stato, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    // =============================================================================================
    // Helper
    // =============================================================================================

    private async Task<(HttpStatusCode stato, string corpo)> Posticipa(string? testo = "nota di prova")
    {
        var nota = testo is null ? "null" : $$"""{ "data": "2026-08-31T20:30:00", "testo": "{{testo}}" }""";
        var body = $$"""
        {
            "mese": "{{Mese}}",
            "anno": "{{Anno}}",
            "tipologiaFattura": "{{Tipologia}}",
            "idEnte": "{{IdEnte}}",
            "azione": "Posticipa",
            "nota": {{nota}},
            "idFattura": null
        }
        """;
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var resp = await client.PostAsync(_factory.WithNonce(Rotta),
            new StringContent(body, Encoding.UTF8, "application/json"));
        return (resp.StatusCode, await resp.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// Il testo della prima nota dell'array JSON, come è finito a database.
    ///
    /// ATTENZIONE **Non si usa `JSON_VALUE`**, che è la via ovvia: restituisce `nvarchar(4000)` e su un valore
    /// più lungo torna **NULL** invece del testo (in lax mode non solleva errore). Leggendo così, una
    /// nota da 5000 caratteri sembrerebbe persa mentre è memorizzata per intero — ed è esattamente
    /// l'abbaglio che ha preso questo test alla prima esecuzione. Si legge quindi la colonna intera e
    /// si estrae il valore in C#. Vale anche per chi interroga il DB a mano.
    /// </summary>
    private static string? TestoDellaNota()
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"SELECT TOP(1) CAST(Note AS nvarchar(max)) FROM cfg.GestioneFatture
            WHERE FkIdEnte='{IdEnte}' AND FkTipologiaFattura='{Tipologia}' AND Anno={Anno} AND Mese={Mese}";
        var v = cmd.ExecuteScalar();
        if (v == null || v == DBNull.Value) return null;

        using var doc = System.Text.Json.JsonDocument.Parse((string)v);
        return doc.RootElement[0].GetProperty("Testo").GetString();
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
