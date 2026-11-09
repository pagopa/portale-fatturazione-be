using System.Net;
using System.Text;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Download del documento del Modulo Commessa (HTML o PDF), end-to-end sul DB seedato:
///  - GET api/modulocommessa/download/{anno}/{mese}         (aderente, SelfCarePolicy)
///  - GET api/modulocommessa/pagopa/download/{anno}/{mese}  (admin, con idEnte/prodotto/idTipoContratto)
/// Il documento aggrega ente, dati di fatturazione, righe del modulo e la configurazione dei costi
/// (pfw.CostoNotifiche, pfw.PercentualeAnticipo) — DatiModuloCommessaQueryDocumentoHandler + Mapper.
///
/// Dati: un ente DEDICATO (PAL, tipo contratto 1 come il token di test) e il periodo 2027/1, scritti
/// in OneTimeSetUp e rimossi in OneTimeTearDown. La configurazione dei costi nel seed e' vuota:
/// le righe aggiunte qui sono marcate (Descrizione 'PF-997 …') e cancellate a fine fixture.
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
[TestFixture]
[Category("ModuloCommessa")]
public class ModuloCommessaDocumentoDownloadHttpTests
{
    private const string Ente = "99799799-0997-0997-0997-0997099700cc";
    private const string RagioneSociale = "Ente Test PF-997 Commessa";
    private const string Marcatore = "PF-997";
    private const int Anno = 2027;
    private const int Mese = 1;
    private const int TipoContrattoPal = 1;

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

    private Task<HttpResponseMessage> GetAderente(int anno, int mese, string? tipo = null, string idEnte = Ente) =>
        _factory.CreateClientAs(Ruolo.ADMIN, idEnte, AuthType.SELFCARE, Profilo.PubblicaAmministrazione)
            .GetAsync(_factory.WithNonce($"/api/modulocommessa/download/{anno}/{mese}", idEnte) + (tipo is null ? "" : $"&tipo={tipo}"));

    private Task<HttpResponseMessage> GetAdmin(int anno, int mese, string idEnte = Ente, string? tipo = null) =>
        _factory.CreateClientAs(Ruolo.ADMIN)
            .GetAsync(_factory.WithNonce($"/api/modulocommessa/pagopa/download/{anno}/{mese}")
                      + $"&idEnte={idEnte}&prodotto=prod-pn&idTipoContratto={TipoContrattoPal}"
                      + (tipo is null ? "" : $"&tipo={tipo}"));

    // =============================================================================================
    // Percorso felice
    // =============================================================================================

    [Test]
    public async Task Aderente_Html_200ConIDatiDelModulo()
    {
        var resp = await GetAderente(Anno, Mese);
        var html = await resp.Content.ReadAsStringAsync();

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK), html);
        AssertDocumento(resp, html);
    }

    [Test]
    public async Task Admin_Html_StessoDocumentoDellAderente()
    {
        var admin = await GetAdmin(Anno, Mese);
        var html = await admin.Content.ReadAsStringAsync();

        Assert.That(admin.StatusCode, Is.EqualTo(HttpStatusCode.OK), html);
        AssertDocumento(admin, html);
    }

    [Test]
    public async Task Aderente_Pdf_200ApplicationPdf()
    {
        var resp = await GetAderente(Anno, Mese, tipo: "pdf");
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

    [Test]
    public async Task MeseSenzaModulo_500_Caratterizzazione()
    {
        // Le rotte prevedono un 404 (dati == null), ma per un mese senza righe di modulo
        // DatiModuloCommessaQueryGet non restituisce null: il Mapper lavora su una lista vuota e
        // finisce in NullReferenceException -> 500 "Object reference not set…". Il documento non
        // esce, ma l'errore non e' leggibile dal client. Se si corregge, il test va portato a 404.
        Assert.Multiple(async () =>
        {
            Assert.That((await GetAderente(Anno, 2)).StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            Assert.That((await GetAdmin(Anno, 2)).StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        });
    }

    [Test]
    public async Task Aderente_AltroEnte_NonVedeIlModulo()
    {
        // L'IdEnte viene dal token: un altro ente, senza dati di fatturazione, non riceve il documento
        // di questo ente per lo stesso periodo.
        var resp = await GetAderente(Anno, Mese, idEnte: "22222222-2222-2222-2222-222222222222");
        var body = await resp.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.Not.EqualTo(HttpStatusCode.OK));
            Assert.That(body, Does.Not.Contain(RagioneSociale));
        });
    }

    [Test]
    public async Task Admin_SuRottaAderente_403()
    {
        var resp = await _factory.CreateClientAs(Ruolo.ADMIN)
            .GetAsync(_factory.WithNonce($"/api/modulocommessa/download/{Anno}/{Mese}"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task SenzaAutenticazione_401()
    {
        var resp = await _factory.CreateClientAs(null)
            .GetAsync(_factory.WithNonce($"/api/modulocommessa/download/{Anno}/{Mese}"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    // --- helper --------------------------------------------------------------------------------

    private static void AssertDocumento(HttpResponseMessage resp, string html)
    {
        Assert.Multiple(() =>
        {
            Assert.That(resp.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
            Assert.That(html, Does.Contain(RagioneSociale));
            Assert.That(html, Does.Contain("CUP-997"));
            Assert.That(html, Does.Contain("mailto:commessa997@pec.it"));
            Assert.That(html, Does.Contain("contatto997@ente.it"));
            Assert.That(html, Does.Contain("GENNAIO/2027"), "mese di attivita'");
            Assert.That(html, Does.Contain("Totale notifiche da processare"));
            // Le righe per tipo di spedizione prendono l'etichetta da pfw.CostoNotifiche.Descrizione (seminata
            // qui sotto, con [data] sostituito dal mese), NON da pfw.TipoSpedizione. Fino al 06/11/2026 qui si
            // cercava "notifiche da processare in via digitale": passava solo su un volume del DB con righe di
            // CostoNotifiche lasciate da altro, ed e' diventato rosso al primo seed rigenerato da zero.
            Assert.That(html, Does.Contain($"Digitale {Marcatore} GENNAIO/2027"), "riga digitale del modulo");
            Assert.That(html, Does.Contain($"Analogico AR {Marcatore} GENNAIO/2027"), "riga analogico A/R del modulo");
            Assert.That(html, Does.Contain($"Analogico 890 {Marcatore} GENNAIO/2027"), "riga analogico 890 del modulo");
            Assert.That(html, Does.Not.Contain("[Descrizione]").And.Not.Contain("[DatiModuloCommessa]").And.Not.Contain("[Contatti]"));
        });
    }

    // --- dati ----------------------------------------------------------------------------------

    private static void Semina()
    {
        Esegui($"""
            INSERT INTO pfd.Enti (InternalIstitutionId, description) VALUES ('{Ente}', N'{RagioneSociale}');
            INSERT INTO pfd.Contratti (internalistitutionid, product, FkIdTipoContratto, onboardingtokenid)
            VALUES ('{Ente}', 'prod-pn', {TipoContrattoPal}, 'TOKEN-997');

            INSERT INTO pfw.DatiFatturazione
             (Cup, Cig, CodCommessa, DataDocumento, SplitPayment, FkIdEnte, IdDocumento, DataCreazione, FkTipoCommessa, PEC, FkProdotto, NotaLegale)
            VALUES ('CUP-997', NULL, 'COMM-997', '2027-01-01', 0, '{Ente}', 'DOC-997', GETDATE(), '1', 'commessa997@pec.it', 'prod-pn', 0);
            INSERT INTO pfw.DatiFatturazioneContatti (FkIdDatiFatturazione, Email)
            SELECT IdDatiFatturazione, 'contatto997@ente.it' FROM pfw.DatiFatturazione WHERE FkIdEnte = '{Ente}';

            INSERT INTO pfw.DatiModuloCommessa
             (FkIdEnte, FkProdotto, FkIdTipoSpedizione, NumeroNotificheNazionali, NumeroNotificheInternazionali,
              AnnoValidita, MeseValidita, FKIdTipoContratto, FkIdStato, DataCreazione)
            VALUES
             ('{Ente}', 'prod-pn', 1, 600, 10, {Anno}, {Mese}, {TipoContrattoPal}, 'Aperta/Caricato', GETDATE()),
             ('{Ente}', 'prod-pn', 2, 400,  0, {Anno}, {Mese}, {TipoContrattoPal}, 'Aperta/Caricato', GETDATE()),
             ('{Ente}', 'prod-pn', 3, 300, 20, {Anno}, {Mese}, {TipoContrattoPal}, 'Aperta/Caricato', GETDATE());

            INSERT INTO pfw.CostoNotifiche
             (MediaNotificaNazionale, MediaNotificaInternazionale, FkIdTipoSpedizione, FkIdTipoContratto, FkProdotto,
              DataInizioValidita, DataFineValidita, DataCreazione, Descrizione)
            VALUES
             (3.00, 4.00, 1, {TipoContrattoPal}, 'prod-pn', '2020-01-01', NULL, GETDATE(), 'Analogico AR {Marcatore} [data]'),
             (3.00, 4.00, 2, {TipoContrattoPal}, 'prod-pn', '2020-01-01', NULL, GETDATE(), 'Analogico 890 {Marcatore} [data]'),
             (1.00, 1.00, 3, {TipoContrattoPal}, 'prod-pn', '2020-01-01', NULL, GETDATE(), 'Digitale {Marcatore} [data]');

            INSERT INTO pfw.PercentualeAnticipo
             (FkProdotto, FkIdTipoContratto, FkIdCategoriaSpedizione, Percentuale, Descrizione, DataInizioValidita, DataFineValidita, DataCreazione)
            VALUES
             ('prod-pn', {TipoContrattoPal}, 1, 50, 'Anticipo analogico [percent]% {Marcatore} [data]', '2020-01-01', NULL, GETDATE()),
             ('prod-pn', {TipoContrattoPal}, 2, 50, 'Anticipo digitale [percent]% {Marcatore} [data]', '2020-01-01', NULL, GETDATE());
            """);
    }

    private static void Pulisci()
    {
        Esegui($"""
            DELETE FROM pfw.PercentualeAnticipo WHERE Descrizione LIKE '%{Marcatore}%';
            DELETE FROM pfw.CostoNotifiche WHERE Descrizione LIKE '%{Marcatore}%';
            DELETE FROM pfw.DatiModuloCommessa WHERE FkIdEnte = '{Ente}';
            DELETE c FROM pfw.DatiFatturazioneContatti c
              JOIN pfw.DatiFatturazione f ON f.IdDatiFatturazione = c.FkIdDatiFatturazione WHERE f.FkIdEnte = '{Ente}';
            DELETE FROM pfw.DatiFatturazione WHERE FkIdEnte = '{Ente}';
            DELETE FROM pfd.Contratti WHERE internalistitutionid = '{Ente}';
            DELETE FROM pfd.Enti WHERE InternalIstitutionId = '{Ente}';
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
