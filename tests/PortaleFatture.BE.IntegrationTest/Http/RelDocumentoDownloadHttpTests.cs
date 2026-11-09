using System.Net;
using System.Text;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Download del documento REL (HTML o PDF) dalle due rotte dell'Api, end-to-end sul DB seedato:
///  - GET api/rel/pagopa/documento/download/{id}  (admin, PagoPAPolicy)
///  - GET api/rel/ente/download/{id}              (aderente, SelfCarePolicy)
/// Entrambe leggono la REL da [be].[vwRelDettaglio] (RelTestataQueryGetById, SingleAsync), scartano
/// var/semestrale/annuale con un 404 e generano il documento con DocumentBuilder.
///
/// Nasce dal difetto PF-997 (2.63.6): per SEM. SOSPESI mancava il template e il download dava 500.
///
/// Dati: un ente DEDICATO e un periodo libero (2027/1), con RelTestata + staging (tmpFattureTestata,
/// tmpFattureRighe, MesiFatture) per quattro tipologie, scritti in OneTimeSetUp e rimossi in
/// OneTimeTearDown. La vista vuole lo staging (TotaliCumulati e' in INNER JOIN): senza, la REL non
/// esiste per la rotta. Nessuna riga del seed condiviso viene toccata.
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
[TestFixture]
[Category("Rel")]
public class RelDocumentoDownloadHttpTests
{
    private const string EnteDedicato = "99799799-0997-0997-0997-099709970997";
    private const string AltroEnte = "11111111-1111-1111-1111-111111111111";
    private const string Contratto = "TOKEN-997";
    private const string RagioneSociale = "Comune Test PF-997";
    private const int Anno = 2027;
    private const int Mese = 1;

    // IdFattura delle fatture temporanee di staging, una per tipologia.
    private static readonly (int IdTmp, string Tipologia)[] Rel =
    [
        (99701, "PRIMO SALDO"),
        (99702, "SECONDO SALDO"),
        (99703, "SEM. SOSPESI"),
        (99704, "VAR. SEMESTRALE"),
    ];

    private ApiTestFactory _factory = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        Pulisci();
        Semina();
        _factory = new ApiTestFactory();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _factory?.Dispose();
        try { Pulisci(); } catch { /* container spento: niente da pulire */ }
    }

    [SetUp]
    public void CheckDb() => TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);

    private static string Id(string tipologia, string ente = EnteDedicato) =>
        $"{ente}_{Contratto}_{tipologia.Replace(' ', '-')}_{Anno}_{Mese}";

    private HttpClient Admin() => _factory.CreateClientAs(Ruolo.ADMIN);

    private HttpClient Aderente(string idEnte = EnteDedicato) =>
        _factory.CreateClientAs(Ruolo.ADMIN, idEnte, AuthType.SELFCARE, Profilo.PubblicaAmministrazione);

    private Task<HttpResponseMessage> GetAdmin(string id, string? tipo = null) =>
        Admin().GetAsync(_factory.WithNonce($"/api/rel/pagopa/documento/download/{id}") + (tipo is null ? "" : $"&tipo={tipo}"));

    private Task<HttpResponseMessage> GetAderente(string id, string? tipo = null, string idEnte = EnteDedicato) =>
        Aderente(idEnte).GetAsync(_factory.WithNonce($"/api/rel/ente/download/{id}", idEnte) + (tipo is null ? "" : $"&tipo={tipo}"));

    // =============================================================================================
    // Percorso felice
    // =============================================================================================

    [TestCase("PRIMO SALDO")]
    [TestCase("SECONDO SALDO")]
    [TestCase("SEM. SOSPESI")] // PF-997
    public async Task Admin_Html_200ConIDatiDellaRel(string tipologia)
    {
        var resp = await GetAdmin(Id(tipologia));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK), await resp.Content.ReadAsStringAsync());
        AssertHtmlDellaRel(resp, await resp.Content.ReadAsStringAsync());
    }

    [TestCase("PRIMO SALDO")]
    [TestCase("SECONDO SALDO")]
    [TestCase("SEM. SOSPESI")] // PF-997
    public async Task Aderente_Html_200ConIDatiDellaRel(string tipologia)
    {
        var resp = await GetAderente(Id(tipologia));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK), await resp.Content.ReadAsStringAsync());
        AssertHtmlDellaRel(resp, await resp.Content.ReadAsStringAsync());
    }

    [Test]
    public async Task SemSospesi_HtmlDiversoDaiSaldi()
    {
        var semSospesi = await (await GetAdmin(Id("SEM. SOSPESI"))).Content.ReadAsStringAsync();
        var primo = await (await GetAdmin(Id("PRIMO SALDO"))).Content.ReadAsStringAsync();

        Assert.That(semSospesi, Is.Not.EqualTo(primo), "SEM. SOSPESI deve usare il proprio template.");
    }

    [TestCase("PRIMO SALDO")]
    [TestCase("SEM. SOSPESI")] // PF-997
    public async Task Admin_Pdf_200ApplicationPdf(string tipologia)
    {
        var resp = await GetAdmin(Id(tipologia), tipo: "pdf");

        await AssertPdf(resp);
    }

    [Test]
    public async Task Aderente_Pdf_SemSospesi_200ApplicationPdf()
    {
        var resp = await GetAderente(Id("SEM. SOSPESI"), tipo: "pdf");

        await AssertPdf(resp);
    }

    // =============================================================================================
    // Adversarial
    // =============================================================================================

    [Test]
    public async Task VarSemestrale_404()
    {
        // La REL esiste nella vista, ma la rotta scarta var/semestrale/annuale prima del builder.
        Assert.Multiple(async () =>
        {
            Assert.That((await GetAdmin(Id("VAR. SEMESTRALE"))).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That((await GetAderente(Id("VAR. SEMESTRALE"))).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    [Test]
    public async Task TipoPdfMaiuscolo_RestituisceHtml_Caratterizzazione()
    {
        // tipo == "pdf" e' case-sensitive: "PDF" ricade sul ramo HTML senza errore.
        var resp = await GetAdmin(Id("SEM. SOSPESI"), tipo: "PDF");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(resp.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
    }

    [Test]
    public async Task Aderente_RelDiUnAltroEnte_NonLaScarica()
    {
        // L'aderente autenticato come EnteDedicato chiede la chiave della REL di un altro ente.
        // La Persistence confronta l'IdEnte della chiave con quello del token e lancia una
        // DomainException, che l'handler globale mappa a 500 (non 403): il documento non esce.
        var resp = await GetAderente(Id("PRIMO SALDO", ente: AltroEnte));

        Assert.Multiple(async () =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            Assert.That(await resp.Content.ReadAsStringAsync(), Does.Not.Contain("<html").IgnoreCase);
        });
    }

    [Test]
    public async Task Aderente_ChiaveDelProprioEnteMaContrattoAltrui_NonLaScarica()
    {
        // Stesso ente, contratto inesistente: la vista non ha la riga -> SingleAsync -> 500.
        var resp = await GetAderente($"{EnteDedicato}_TOKEN-ALTRO_SEM.-SOSPESI_{Anno}_{Mese}");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    [Test]
    public async Task Admin_SuRottaAderente_403()
    {
        var resp = await Admin().GetAsync(_factory.WithNonce($"/api/rel/ente/download/{Id("SEM. SOSPESI")}"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Aderente_SuRottaAdmin_403()
    {
        var resp = await Aderente().GetAsync(
            _factory.WithNonce($"/api/rel/pagopa/documento/download/{Id("SEM. SOSPESI")}", EnteDedicato));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task SenzaAutenticazione_401()
    {
        var anonimo = _factory.CreateClientAs(null);

        Assert.Multiple(async () =>
        {
            Assert.That((await anonimo.GetAsync(_factory.WithNonce($"/api/rel/pagopa/documento/download/{Id("SEM. SOSPESI")}"))).StatusCode,
                Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That((await anonimo.GetAsync(_factory.WithNonce($"/api/rel/ente/download/{Id("SEM. SOSPESI")}"))).StatusCode,
                Is.EqualTo(HttpStatusCode.Unauthorized));
        });
    }

    [TestCase("chiave-malformata")]
    [TestCase("x_y_z")]
    public async Task ChiaveMalformata_NonRestituisceUnDocumento(string id)
    {
        var resp = await GetAdmin(id);

        Assert.That(resp.IsSuccessStatusCode, Is.False, $"{id} -> {(int)resp.StatusCode}");
    }

    // --- helper --------------------------------------------------------------------------------

    private static void AssertHtmlDellaRel(HttpResponseMessage resp, string html)
    {
        Assert.Multiple(() =>
        {
            Assert.That(resp.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
            Assert.That(html, Does.Contain(RagioneSociale));
            Assert.That(html, Does.Contain(Contratto));
            Assert.That(html, Does.Contain("GENNAIO"), "Mese in lettere (Map -> GetMonth)");
            Assert.That(html, Does.Contain("1.234,56"), "Totale formattato it-IT");
            Assert.That(html, Does.Not.Contain("[RagioneSociale]").And.Not.Contain("[Totale]").And.Not.Contain("[Mese]"));
        });
    }

    private static async Task AssertPdf(HttpResponseMessage resp)
    {
        var body = await resp.Content.ReadAsByteArrayAsync();
        if (resp.StatusCode == HttpStatusCode.InternalServerError
            && Encoding.UTF8.GetString(body).Contains("DllNotFoundException", StringComparison.Ordinal))
            Assert.Inconclusive("Librerie native PDF non disponibili su questa macchina.");

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK), Encoding.UTF8.GetString(body));
            Assert.That(resp.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/pdf"));
            Assert.That(Encoding.ASCII.GetString(body, 0, Math.Min(5, body.Length)), Is.EqualTo("%PDF-"));
        });
    }

    // --- dati ----------------------------------------------------------------------------------

    private static void Semina()
    {
        var sql = new StringBuilder();
        sql.AppendLine($"INSERT INTO pfd.Enti (InternalIstitutionId, description) VALUES ('{EnteDedicato}', N'{RagioneSociale}');");

        sql.AppendLine("SET IDENTITY_INSERT pfd.tmpFattureTestata ON;");
        foreach (var (id, tipologia) in Rel)
            sql.AppendLine($"""
                INSERT INTO pfd.tmpFattureTestata
                 (IdFattura, FkProdotto, FkIdTipoDocumento, FkTipologiaFattura, FkIdEnte, DataFattura, IdentificativoFattura,
                  TotaleFattura, Divisa, MetodoPagamento, AnnoRiferimento, MeseRiferimento, CodiceContratto, Progressivo, FatturaInviata, FlagFatturata)
                VALUES ({id}, 'prod-pn', 'TD01', '{tipologia}', '{EnteDedicato}', '{Anno}-0{Mese}-01', 'IT-{id}',
                  1234.56, 'EUR', 'MP5', {Anno}, {Mese}, '{Contratto}', {id}, 0, 0);
                INSERT INTO pfd.tmpFattureRighe
                 (FkIdFattura, NumeroLinea, Testo, CodiceMateriale, Quantita, PrezzoUnitario, Imponibile, RigaBollo, PeriodoRiferimento)
                VALUES ({id}, 1, 'consumo del periodo', 'MAT-A', 1, 1234.56, 1234.56, 0, '0{Mese}/{Anno}');
                INSERT INTO pfd.MesiFatture (FkIdEnte, AnnoRiferimento, MeseRiferimento, FKTipologiaFattura, FkIdFattura, FkIdFatturaTmp, FlagEliminata)
                VALUES ('{EnteDedicato}', {Anno}, {Mese}, '{tipologia}', NULL, {id}, 0);
                INSERT INTO pfd.RelTestata
                 (internal_organization_id, contract_id, TipologiaFattura, [year], [month], TotaleAnalogico, TotaleDigitale,
                  TotaleNotificheAnalogiche, TotaleNotificheDigitali, Totale, TotaleAnalogicoIva, TotaleDigitaleIva, TotaleIva, Caricata, RelFatturata,
                  AsseverazioneTotaleAnalogico, AsseverazioneTotaleDigitale, AsseverazioneTotaleNotificheAnalogiche, AsseverazioneTotaleNotificheDigitali,
                  AsseverazioneTotale, AsseverazioneTotaleAnalogicoIva, AsseverazioneTotaleDigitaleIva, AsseverazioneTotaleIva)
                VALUES ('{EnteDedicato}', '{Contratto}', '{tipologia}', {Anno}, {Mese}, 1000.00, 234.56, 70, 30, 1234.56, 1220.00, 286.16, 1506.16, 0, 0,
                  0, 0, 0, 0, 0, 0, 0, 0);
                """);
        sql.AppendLine("SET IDENTITY_INSERT pfd.tmpFattureTestata OFF;");

        Esegui(sql.ToString());
    }

    private static void Pulisci()
    {
        var ids = string.Join(",", Rel.Select(r => r.IdTmp));
        Esegui($"""
            DELETE FROM pfd.MesiFatture WHERE FkIdEnte = '{EnteDedicato}';
            DELETE FROM pfd.tmpFattureRighe WHERE FkIdFattura IN ({ids});
            DELETE FROM pfd.tmpFattureTestata WHERE IdFattura IN ({ids});
            DELETE FROM pfd.RelTestata WHERE internal_organization_id = '{EnteDedicato}';
            DELETE FROM pfd.Enti WHERE InternalIstitutionId = '{EnteDedicato}';
            """);
    }

    private static void Esegui(string sql)
    {
        using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
