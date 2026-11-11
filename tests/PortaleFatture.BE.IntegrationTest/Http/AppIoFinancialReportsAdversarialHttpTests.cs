using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Input ostili sulle rotte api/appio/financialreports* (modello dati dell'11/11/2026, PF-1016).
/// Stessa divisione dei test dell'anagrafica: difese verificate e caratterizzazioni (suffisso
/// <c>_Caratterizzazione</c>) del comportamento attuale dove è discutibile.
///
/// Seed (tests/Data/appio.sql): documenti in 2026_2 (OB1, OB2), 2026_1 (OB1), 2025_4 (OB4).
/// </summary>
[Category("AppIO")]
public class AppIoFinancialReportsAdversarialHttpTests
{
    private const string Base = "/api/appio/financialreports";

    private ApiTestFactory _factory = null!;

    [OneTimeSetUp]
    public void Setup() => _factory = new ApiTestFactory();

    [OneTimeTearDown]
    public void TearDown() => _factory?.Dispose();

    [SetUp]
    public void CheckDb()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioFinalReport");
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioFinalReportDetails");
    }

    private async Task<HttpResponseMessage> Post(string rotta, string body, string prodottoNonce = "prod-pn")
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(_factory.WithNonce(rotta, prodotto: prodottoNonce), content);
        TestContext.Out.WriteLine($"STATUS: {(int)resp.StatusCode} {resp.StatusCode}");
        return resp;
    }

    private static string Json(object o) => JsonSerializer.Serialize(o);

    private static async Task<List<string>> Chiavi(HttpResponseMessage resp)
        => JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement.GetProperty("financialReports")
            .EnumerateArray().Select(x => x.GetProperty("key").GetString()!).ToList();

    // --- injection: deve restare un valore ---

    /// <summary>
    /// Injection in ogni filtro della griglia e del download, e nei campi del dettaglio: resta un
    /// valore parametrizzato, nessuna riga corrisponde, 404.
    /// </summary>
    [Test]
    public async Task Filtri_Injection_ShouldRestareUnValore_404OvunqueVengaUsato()
    {
        const string inj = "' OR 1=1 --";
        var richieste = new[]
        {
            ($"{Base}", Json(new { institutionIds = new[] { inj } })),
            ($"{Base}", Json(new { codiciFiscali = new[] { inj } })),
            ($"{Base}", Json(new { quarters = new[] { inj } })),
            ($"{Base}", Json(new { year = "2026' OR 1=1 --" })),
            ($"{Base}/document", Json(new { institutionIds = new[] { inj } })),
            ($"{Base}/quarters", Json(new { year = "2026' --" })),
            ($"{Base}/detail", Json(new { contractId = inj, yearQuarter = "2026_2" })),
            ($"{Base}/detail/download", Json(new { contractId = "APPIO-OB1", yearQuarter = inj }))
        };

        foreach (var (rotta, body) in richieste)
            Assert.That((await Post(rotta, body)).StatusCode, Is.EqualTo(HttpStatusCode.NotFound), rotta + " " + body);
    }

    /// <summary>Dopo un tentativo di DROP le tabelle esistono ancora con tutte le righe del seed.</summary>
    [Test]
    public async Task Filtri_InjectionDrop_ShouldLasciareIntatteLeTabelle()
    {
        await Post(Base, Json(new { institutionIds = new[] { "'; DROP TABLE appio.messages_final_report; --" } }));
        await Post($"{Base}/detail", Json(new { contractId = "'; DROP TABLE appio.contracts; --", yearQuarter = "2026_2" }));

        await using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(
            "SELECT (SELECT COUNT(*) FROM appio.messages_final_report WHERE contract_id LIKE 'APPIO-OB%') * 100 + (SELECT COUNT(*) FROM appio.contracts WHERE onboardingtokenid LIKE 'APPIO-OB%')", conn);
        Assert.That((int)(await cmd.ExecuteScalarAsync())!, Is.EqualTo(705));
    }

    // --- jolly del LIKE sull'anno ---

    /// <summary>
    /// L'anno finisce in un LIKE senza escape: '%' prende tutti i trimestri, '20_6' fa da jolly sulla
    /// cifra. Stesso comportamento dell'anagrafica di ottobre e dei documenti pagoPA.
    /// </summary>
    [TestCase("%", new[] { "APPIO-OB1|2026_2", "APPIO-OB1|2026_1", "APPIO-OB4|2025_4", "APPIO-OB2|2026_2" })]
    [TestCase("20_6", new[] { "APPIO-OB1|2026_2", "APPIO-OB1|2026_1", "APPIO-OB2|2026_2" })]
    public async Task Griglia_AnnoConJolly_ShouldAllargareIlFiltro_Caratterizzazione(string year, string[] attese)
    {
        var resp = await Post(Base, Json(new { year }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await Chiavi(resp), Is.EqualTo(attese));
    }

    /// <summary>
    /// Un anno non numerico non fa fallire la query (si filtra sulla stringa 'AAAA_T'): 404.
    /// </summary>
    [Test]
    public async Task Griglia_AnnoNonNumerico_ShouldReturn404()
    {
        var resp = await Post(Base, Json(new { year = "abcd" }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// Un anno vuoto o di soli spazi vale come assente: trimestre più recente.
    /// </summary>
    [TestCase("")]
    [TestCase("   ")]
    public async Task Griglia_AnnoVuoto_ShouldUsareIlTrimestrePiuRecente(string year)
    {
        var resp = await Post(Base, Json(new { year }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await Chiavi(resp), Is.EqualTo(new[] { "APPIO-OB1|2026_2", "APPIO-OB2|2026_2" }));
    }

    // --- liste lunghe ---

    /// <summary>
    /// Oltre circa 2100 codici fiscali si supera il limite dei parametri di SQL Server e la rotta
    /// risponde 500; sotto il limite non trova nulla, 404.
    /// </summary>
    [TestCase(2000, HttpStatusCode.NotFound)]
    [TestCase(2100, HttpStatusCode.InternalServerError)]
    public async Task Griglia_ListaLunga_ShouldRispettareIlLimiteDeiParametri_Caratterizzazione(int n, HttpStatusCode atteso)
    {
        var resp = await Post(Base, Json(new { codiciFiscali = Enumerable.Range(0, n).Select(i => $"X{i}").ToArray() }));

        Assert.That(resp.StatusCode, Is.EqualTo(atteso));
    }

    // --- body ---

    /// <summary>
    /// Body vuoto, null o "{}" su griglia, download e trimestri valgono come nessun filtro.
    /// </summary>
    [TestCase("")]
    [TestCase("null")]
    [TestCase("{}")]
    public async Task GrigliaDownloadETrimestri_BodyVuotoONull_ShouldUsareIDefault(string body)
    {
        var griglia = await Post(Base, body);
        var download = await Post($"{Base}/document", body);
        var trimestri = await Post($"{Base}/quarters", body);

        Assert.Multiple(() =>
        {
            Assert.That(griglia.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(download.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(trimestri.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });
    }

    /// <summary>
    /// Body malformato produce un 500 invece di un 400: l'errore di binding viene appiattito dal
    /// gestore globale.
    /// </summary>
    [Test]
    public async Task Griglia_BodyMalformato_ShouldReturn500_Caratterizzazione()
    {
        var resp = await Post(Base, "{ malformato");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    // --- sessione ---

    /// <summary>
    /// Un nonce emesso per un altro prodotto viene rifiutato con 419.
    /// </summary>
    [Test]
    public async Task Griglia_NonceDiUnAltroProdotto_ShouldReturn419()
    {
        var resp = await Post(Base, "{}", prodottoNonce: "prod-appio");

        Assert.That((int)resp.StatusCode, Is.EqualTo(419));
    }
}
