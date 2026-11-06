using System.Net;
using System.Text;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Download del documento di dettaglio di una fattura SOSPESA (HTML o PDF), end-to-end sul DB seedato:
///  - GET api/fatture/sospese/dettaglio/download/{id}?idEnte=…  (admin, PagoPAPolicy)
///  - GET api/fatture/ente/sospese/dettaglio/download/{id}      (aderente, SelfCarePolicy)
/// La query (FattureQueryRicercaBuilder.SelectDettaglioSospesoEnte) legge pfd.tmpFattureTestata con
/// tmpRelTestata, pfd.Contratti e pfw.TipoContratto: il template PAC o PAL lo decide il tipo
/// contratto dell'ente. ANTICIPO e ACCONTO danno 404 per scelta della rotta.
///
/// Dati: tre enti DEDICATI (PAC, PAL, senza contratto) e il periodo 2027/1, scritti in
/// OneTimeSetUp e rimossi in OneTimeTearDown. Un ente per tipo contratto perche' la join su
/// pfd.Contratti e' per solo ente: due contratti sullo stesso ente raddoppierebbero le righe.
///
/// Il dettaglio EMESSO non e' qui: legge pfd.vwFatturaEmessaDownloadPDF, che non e' nel seed
/// (serve la DDL reale, v. FatturaEmessaDocumentoDownloadHttpTests).
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
[TestFixture]
public class FatturaSospesaDocumentoDownloadHttpTests
{
    private const string EntePac = "99799799-0997-0997-0997-0997099700ac";
    private const string EntePal = "99799799-0997-0997-0997-0997099700a1";
    private const string EnteSenzaContratto = "99799799-0997-0997-0997-0997099700ff";
    private const string Contratto = "TOKEN-997";
    private const int Anno = 2027;
    private const int Mese = 1;

    private const long SospesaPacSemSospesi = 99711;
    private const long SospesaPalSecondoSaldo = 99712;
    private const long SospesaPacAcconto = 99713;
    private const long SospesaSenzaContratto = 99714;
    private const long SospesaPacAnticipo = 99715;

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
        try { Pulisci(); } catch { /* container spento */ }
    }

    [SetUp]
    public void CheckDb() => TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);

    private Task<HttpResponseMessage> GetAdmin(long id, string? idEnte, string? tipo = null)
    {
        var query = (idEnte is null ? "" : $"&idEnte={idEnte}") + (tipo is null ? "" : $"&tipo={tipo}");
        return _factory.CreateClientAs(Ruolo.ADMIN)
            .GetAsync(_factory.WithNonce($"/api/fatture/sospese/dettaglio/download/{id}") + query);
    }

    private Task<HttpResponseMessage> GetAderente(long id, string idEnte, string? tipo = null) =>
        _factory.CreateClientAs(Ruolo.ADMIN, idEnte, AuthType.SELFCARE, Profilo.PubblicaAmministrazione)
            .GetAsync(_factory.WithNonce($"/api/fatture/ente/sospese/dettaglio/download/{id}", idEnte)
                      + (tipo is null ? "" : $"&tipo={tipo}"));

    // =============================================================================================
    // Percorso felice
    // =============================================================================================

    [Test]
    public async Task Admin_Pac_SemSospesi_HtmlConTemplatePac()
    {
        var resp = await GetAdmin(SospesaPacSemSospesi, EntePac);
        var html = await resp.Content.ReadAsStringAsync();

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK), html);
        Assert.Multiple(() =>
        {
            Assert.That(resp.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
            Assert.That(html, Does.Contain("CERTIFICA"), "template PAC");
            Assert.That(html, Does.Contain("Ente Test PF-997 PAC"));
            Assert.That(html, Does.Contain("01/2027"), "ElencoMesi");
            Assert.That(html, Does.Contain("SEM. SOSPESI"), "tipologia nel titolo");
            Assert.That(html, Does.Contain("1.234,56"), "totale dalla REL temporanea, formattato it-IT");
            Assert.That(html, Does.Not.Contain("[RagioneSociale]").And.Not.Contain("[Totale]"));
        });
    }

    [Test]
    public async Task Aderente_Pal_SecondoSaldo_HtmlConTemplatePal()
    {
        var resp = await GetAderente(SospesaPalSecondoSaldo, EntePal);
        var html = await resp.Content.ReadAsStringAsync();

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK), html);
        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("CONSIDERATO"), "template PAL");
            Assert.That(html, Does.Not.Contain("CERTIFICA"));
            Assert.That(html, Does.Contain("Ente Test PF-997 PAL"));
            Assert.That(html, Does.Contain("SECONDO SALDO Contestazioni risolte"));
        });
    }

    [Test]
    public async Task AdminEAderente_StessoDocumento()
    {
        var admin = await (await GetAdmin(SospesaPacSemSospesi, EntePac)).Content.ReadAsStringAsync();
        var aderente = await (await GetAderente(SospesaPacSemSospesi, EntePac)).Content.ReadAsStringAsync();

        Assert.That(aderente, Is.EqualTo(admin));
    }

    [TestCase(SospesaPacSemSospesi, EntePac)]
    [TestCase(SospesaPalSecondoSaldo, EntePal)]
    public async Task Admin_Pdf_200ApplicationPdf(long id, string ente)
    {
        var resp = await GetAdmin(id, ente, tipo: "pdf");
        var body = await resp.Content.ReadAsByteArrayAsync();

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK), Encoding.UTF8.GetString(body));
            Assert.That(resp.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/pdf"));
            Assert.That(Encoding.ASCII.GetString(body, 0, Math.Min(5, body.Length)), Is.EqualTo("%PDF-"));
        });
    }

    // =============================================================================================
    // Adversarial
    // =============================================================================================

    [TestCase(SospesaPacAcconto)]
    [TestCase(SospesaPacAnticipo)]
    public async Task AccontoEAnticipo_404(long id)
    {
        Assert.Multiple(async () =>
        {
            Assert.That((await GetAdmin(id, EntePac)).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That((await GetAderente(id, EntePac)).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    [Test]
    public async Task Admin_SenzaIdEnte_400()
    {
        var resp = await GetAdmin(SospesaPacSemSospesi, idEnte: null);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Admin_IdEnteNonCoerenteConLaFattura_404()
    {
        // La fattura esiste ma appartiene a un altro ente: la WHERE e' su IdEnte E IdFattura.
        var resp = await GetAdmin(SospesaPacSemSospesi, EntePal);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Aderente_FatturaDiUnAltroEnte_404()
    {
        // L'IdEnte viene dal token, non dalla richiesta: l'aderente PAL non vede la sospesa del PAC.
        var resp = await GetAderente(SospesaPacSemSospesi, EntePal);

        Assert.Multiple(async () =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(await resp.Content.ReadAsStringAsync(), Does.Not.Contain("Ente Test PF-997 PAC"));
        });
    }

    [Test]
    public async Task EnteSenzaContratto_404_Caratterizzazione()
    {
        // pfd.Contratti e' in LEFT JOIN ma pfw.TipoContratto in INNER JOIN: senza contratto la riga
        // sparisce e la rotta risponde 404, non un errore sul tipo contratto mancante.
        var resp = await GetAdmin(SospesaSenzaContratto, EnteSenzaContratto);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task FatturaInesistente_404()
    {
        Assert.That((await GetAdmin(99799, EntePac)).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Admin_SuRottaAderente_403()
    {
        var resp = await _factory.CreateClientAs(Ruolo.ADMIN)
            .GetAsync(_factory.WithNonce($"/api/fatture/ente/sospese/dettaglio/download/{SospesaPacSemSospesi}"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task SenzaAutenticazione_401()
    {
        var resp = await _factory.CreateClientAs(null)
            .GetAsync(_factory.WithNonce($"/api/fatture/sospese/dettaglio/download/{SospesaPacSemSospesi}") + $"&idEnte={EntePac}");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    // --- dati ----------------------------------------------------------------------------------

    private static void Semina()
    {
        var sql = new StringBuilder($"""
            INSERT INTO pfd.Enti (InternalIstitutionId, description) VALUES
             ('{EntePac}', N'Ente Test PF-997 PAC'),
             ('{EntePal}', N'Ente Test PF-997 PAL'),
             ('{EnteSenzaContratto}', N'Ente Test PF-997 senza contratto');
            INSERT INTO pfd.Contratti (internalistitutionid, FkIdTipoContratto, onboardingtokenid) VALUES
             ('{EntePac}', (SELECT IdTipoContratto FROM pfw.TipoContratto WHERE Descrizione = 'PAC'), '{Contratto}'),
             ('{EntePal}', (SELECT IdTipoContratto FROM pfw.TipoContratto WHERE Descrizione = 'PAL'), '{Contratto}');
            SET IDENTITY_INSERT pfd.tmpFattureTestata ON;
            """);

        foreach (var (id, ente, tipologia) in new[]
        {
            (SospesaPacSemSospesi, EntePac, "SEM. SOSPESI"),
            (SospesaPalSecondoSaldo, EntePal, "SECONDO SALDO"),
            (SospesaPacAcconto, EntePac, "ACCONTO"),
            (SospesaSenzaContratto, EnteSenzaContratto, "PRIMO SALDO"),
            (SospesaPacAnticipo, EntePac, "ANTICIPO"),
        })
        {
            sql.AppendLine($"""
                INSERT INTO pfd.tmpFattureTestata
                 (IdFattura, FkProdotto, FkIdTipoDocumento, FkTipologiaFattura, FkIdEnte, DataFattura, IdentificativoFattura,
                  TotaleFattura, Divisa, MetodoPagamento, AnnoRiferimento, MeseRiferimento, CodiceContratto, Progressivo, FatturaInviata, FlagFatturata)
                VALUES ({id}, 'prod-pn', 'TD01', '{tipologia}', '{ente}', '{Anno}-0{Mese}-01', 'IT-{id}',
                  8.11, 'EUR', 'MP5', {Anno}, {Mese}, '{Contratto}', {id}, 0, 0);
                INSERT INTO pfd.tmpRelTestata
                 (internal_organization_id, contract_id, TipologiaFattura, [year], [month], TotaleAnalogico, TotaleDigitale,
                  TotaleNotificheAnalogiche, TotaleNotificheDigitali, Totale, TotaleAnalogicoIva, TotaleDigitaleIva, TotaleIva, Caricata, RelFatturata)
                VALUES ('{ente}', '{Contratto}', '{tipologia}', {Anno}, {Mese}, 1000.00, 234.56, 70, 30, 1234.56, 1220.00, 286.16, 1506.16, 0, 0);
                """);
        }
        sql.AppendLine("SET IDENTITY_INSERT pfd.tmpFattureTestata OFF;");

        Esegui(sql.ToString());
    }

    private static void Pulisci()
    {
        const string enti = $"'{EntePac}','{EntePal}','{EnteSenzaContratto}'";
        Esegui($"""
            DELETE FROM pfd.tmpRelTestata WHERE internal_organization_id IN ({enti});
            DELETE FROM pfd.tmpFattureTestata WHERE FkIdEnte IN ({enti});
            DELETE FROM pfd.Contratti WHERE internalistitutionid IN ({enti});
            DELETE FROM pfd.Enti WHERE InternalIstitutionId IN ({enti});
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
