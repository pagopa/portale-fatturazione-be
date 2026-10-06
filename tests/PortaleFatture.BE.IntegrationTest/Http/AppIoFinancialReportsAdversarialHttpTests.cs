using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Input ostili sulle rotte api/appio/financialreports* (PF-908), cioè ciò che può mandare chi chiama
/// l'API direttamente invece di passare dal portale. Le tre rotte che leggono i documenti (griglia,
/// document, detail/download) condividono lo stesso filtro (AppIoFinancialReportSQLBuilder.Where),
/// quindi i casi sul filtro girano su tutte e tre.
///
/// Due famiglie, come per i contratti:
///  - <b>difese verificate</b>: l'injection resta un valore, il nonce di un altro prodotto è rifiutato;
///  - <b>caratterizzazioni</b> (suffisso <c>_Caratterizzazione</c>): fissano il comportamento ATTUALE
///    dove è discutibile — jolly del LIKE sull'anno, 500 su liste oltre il limite dei parametri di SQL
///    Server e su body malformati. Se il comportamento viene corretto diventano rossi: vanno
///    aggiornati, non ripristinati.
///
/// Seed (tests/Data/appio.sql): 5 posizioni APPIO-C% (documenti APPIO-FR-001…004) su 2026_2, 2026_1,
/// 2025_4; 4 righe di financial report.
/// </summary>
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
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioFinancialReportPositions");
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioFinancialReport");
    }

    /// <summary>Le tre rotte che leggono i documenti con lo stesso filtro.</summary>
    private static readonly string[] RotteDocumenti = [Base, $"{Base}/document", $"{Base}/detail/download"];

    /// <summary>POST con il ruolo indicato e nonce valido (salvo un prodotto diverso nel nonce).</summary>
    private async Task<HttpResponseMessage> Post(string rotta, string body, string? ruolo = Ruolo.ADMIN, string prodottoNonce = "prod-pn")
    {
        var client = _factory.CreateClientAs(ruolo);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(_factory.WithNonce(rotta, prodotto: prodottoNonce), content);
        TestContext.Out.WriteLine($"STATUS: {(int)resp.StatusCode} {resp.StatusCode}");
        return resp;
    }

    /// <summary>Serializza un oggetto anonimo come body JSON (con l'escape corretto degli apici).</summary>
    private static string Json(object o) => JsonSerializer.Serialize(o);

    /// <summary>Numeri dei documenti della griglia.</summary>
    private static async Task<List<string>> Numeri(HttpResponseMessage resp)
        => JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement
            .GetProperty("financialReports").EnumerateArray().Select(x => x.GetProperty("numero").GetString()!).ToList();

    private static async Task<int> Conta(string sql)
    {
        await using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    private static string[] Ids(int n) => Enumerable.Range(0, n).Select(i => $"X{i}").ToArray();

    // --- injection: deve restare un valore ---

    /// <summary>
    /// Injection nell'anno, nei trimestri e nei contratti, su tutte e tre le rotte: il valore resta un
    /// parametro, nessun documento corrisponde, 404 — non un 500 e non tutte le righe.
    /// </summary>
    [Test]
    public async Task Filtro_Injection_ShouldRestareUnValore_404OvunqueVengaUsato()
    {
        var bodies = new[]
        {
            Json(new { year = "2026' OR 1=1 --" }),
            Json(new { quarters = new[] { "' OR 1=1 --" } }),
            Json(new { quarters = new[] { "2026_2" }, contractIds = new[] { "x' OR '1'='1" } })
        };

        foreach (var rotta in RotteDocumenti)
            foreach (var body in bodies)
                Assert.That((await Post(rotta, body)).StatusCode, Is.EqualTo(HttpStatusCode.NotFound), $"{rotta} {body}");
    }

    /// <summary>Injection nell'anno dei trimestri: finisce in un LIKE parametrizzato, 404.</summary>
    [Test]
    public async Task Quarters_InjectionNellAnno_ShouldRestareUnValore_404()
    {
        var resp = await Post($"{Base}/quarters", Json(new { year = "2026' --" }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>Dopo i tentativi di DROP le due tabelle esistono ancora con tutte le righe del seed.</summary>
    [Test]
    public async Task Filtro_InjectionDrop_ShouldLasciareIntatteLeTabelle()
    {
        await Post(Base, Json(new { year = "2026'; DROP TABLE appio.FinancialReportPositions; --" }));
        await Post($"{Base}/detail/download", Json(new { contractIds = new[] { "x'; DROP TABLE appio.FinancialReports; --" } }));

        var posizioni = await Conta("SELECT COUNT(*) FROM appio.FinancialReportPositions WHERE contract_id LIKE 'APPIO-C%'");
        var report = await Conta("SELECT COUNT(*) FROM appio.FinancialReports WHERE recipient_id LIKE 'APPIO-C%'");
        Assert.Multiple(() =>
        {
            Assert.That(posizioni, Is.EqualTo(5));
            Assert.That(report, Is.EqualTo(4));
        });
    }

    // --- jolly del LIKE sull'anno: non neutralizzati ---

    /// <summary>
    /// L'anno arriva in un LIKE (year_quarter LIKE @Year + '[_]%') senza escape: '%' prende tutti gli
    /// anni e '_' un carattere qualsiasi. Nessun rischio di injection, ma il filtro non è letterale.
    /// Stesso comportamento delle rotte dei contratti e dei PSP.
    /// </summary>
    [TestCase("%", new[] { "APPIO-FR-001", "APPIO-FR-002", "APPIO-FR-003", "APPIO-FR-004" })]
    [TestCase("20_6", new[] { "APPIO-FR-001", "APPIO-FR-002", "APPIO-FR-003" })]
    public async Task Griglia_AnnoConJolly_ShouldAllargareIlFiltro_Caratterizzazione(string year, string[] attesi)
    {
        var resp = await Post(Base, Json(new { year }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await Numeri(resp), Is.EquivalentTo(attesi));
    }

    /// <summary>
    /// Anno vuoto = nessun anno: si torna al default del trimestre più recente. È voluto
    /// (string.IsNullOrEmpty), lo si fissa perché "" e "%" danno esiti opposti.
    /// </summary>
    [Test]
    public async Task Griglia_AnnoVuoto_ShouldUsareIlTrimestrePiuRecente()
    {
        var resp = await Post(Base, Json(new { year = "" }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await Numeri(resp), Is.EquivalentTo(new[] { "APPIO-FR-001", "APPIO-FR-002" }));
    }

    /// <summary>Sui trimestri '%' e anno vuoto restituiscono tutti i trimestri.</summary>
    [TestCase("%")]
    [TestCase("")]
    public async Task Quarters_AnnoJollyOVuoto_ShouldRestituireTuttiITrimestri_Caratterizzazione(string year)
    {
        var resp = await Post($"{Base}/quarters", Json(new { year }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var valori = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement
            .EnumerateArray().Select(x => x.GetProperty("value").GetString());
        Assert.That(valori, Is.EqualTo(new[] { "2025_4", "2026_1", "2026_2" }));
    }

    // --- liste e valori fuori misura ---

    /// <summary>Lista vuota di contratti = nessun filtro sul contratto, non "nessun contratto".</summary>
    [Test]
    public async Task Griglia_ContractIdsVuoto_ShouldNonFiltrare()
    {
        var resp = await Post(Base, """{ "quarters": ["2026_2"], "contractIds": [] }""");

        Assert.That(await Numeri(resp), Is.EquivalentTo(new[] { "APPIO-FR-001", "APPIO-FR-002" }));
    }

    /// <summary>2000 contratti stanno sotto il limite dei parametri di SQL Server: la query gira, 404.</summary>
    [Test]
    public async Task Griglia_2000ContractIds_ShouldReturn404()
    {
        var resp = await Post(Base, Json(new { contractIds = Ids(2000) }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// Oltre ~2100 elementi Dapper espande l'IN in troppi parametri e SQL Server rifiuta: 500.
    /// Vale per tutte e tre le rotte, che condividono il filtro.
    /// </summary>
    [TestCase(2100)]
    [TestCase(3000)]
    public async Task Filtro_OltreIlLimiteDeiParametri_ShouldReturn500_Caratterizzazione(int n)
    {
        foreach (var rotta in RotteDocumenti)
            Assert.That((await Post(rotta, Json(new { contractIds = Ids(n) }))).StatusCode,
                Is.EqualTo(HttpStatusCode.InternalServerError), rotta);
    }

    /// <summary>Un anno lungo fino a 4000 caratteri resta un parametro: nessun trimestre corrisponde, 404.</summary>
    [Test]
    public async Task Griglia_Anno4000Caratteri_ShouldReturn404()
    {
        var resp = await Post(Base, Json(new { year = new string('9', 4000) }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// Oltre 4000 caratteri Dapper manda il parametro come nvarchar(max), e SQL Server rifiuta il
    /// pattern del LIKE con "String or binary data would be truncated": 500. Stessa causa del 500 sul
    /// nome lunghissimo dei contratti. Nessun effetto sui dati.
    /// </summary>
    [Test]
    public async Task Griglia_Anno100000Caratteri_ShouldReturn500_Caratterizzazione()
    {
        var resp = await Post(Base, Json(new { year = new string('9', 100_000) }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    /// <summary>
    /// Body malformato, vuoto, null o con una stringa al posto dell'array: il binding fallisce e il
    /// gestore globale lo appiattisce in 500 invece di 400 (stesso comportamento delle altre rotte).
    /// </summary>
    [TestCase("{ malformato")]
    [TestCase("")]
    [TestCase("null")]
    [TestCase("""{ "quarters": "2026_2" }""")]
    public async Task Filtro_BodyNonValido_ShouldReturn500_Caratterizzazione(string body)
    {
        foreach (var rotta in RotteDocumenti)
            Assert.That((await Post(rotta, body)).StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError), rotta);
    }

    // --- download con trimestri di anni diversi ---

    /// <summary>
    /// Trimestri di anni diversi nello stesso Financial Report: i fogli portano l'anno nel nome, quindi
    /// il workbook si genera (nel gemello pagoPA due Q1 di anni diversi collidono sul nome del foglio).
    /// </summary>
    [Test]
    public async Task DetailDownload_TrimestriDiAnniDiversi_ShouldGenerareIlFile()
    {
        var resp = await Post($"{Base}/detail/download", """{ "quarters": ["2026_1", "2025_4"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    // --- autenticazione e sessione ---

    /// <summary>Le rotte sono di sola lettura: aperte anche all'OPERATOR.</summary>
    [Test]
    public async Task Operator_ShouldPoterLeggereEScaricare()
    {
        foreach (var rotta in RotteDocumenti)
            Assert.That((await Post(rotta, """{ "quarters": ["2026_2"] }""", ruolo: Ruolo.OPERATOR)).StatusCode,
                Is.EqualTo(HttpStatusCode.OK), rotta);
    }

    /// <summary>Senza autenticazione 401 anche sui download.</summary>
    [Test]
    public async Task Download_SenzaAutenticazione_ShouldReturn401()
    {
        foreach (var rotta in RotteDocumenti.Skip(1))
            Assert.That((await Post(rotta, "{}", ruolo: null)).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), rotta);
    }

    /// <summary>
    /// Un nonce emesso per un altro prodotto (prod-appio contro un token prod-pn) viene rifiutato con
    /// 419 dal NonceMultiTabsMiddleware, come sulle altre rotte admin.
    /// </summary>
    [Test]
    public async Task NonceDiUnAltroProdotto_ShouldReturn419()
    {
        var resp = await Post(Base, "{}", prodottoNonce: "prod-appio");

        Assert.That((int)resp.StatusCode, Is.EqualTo(419));
    }
}
