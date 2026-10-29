using System.Net;
using System.Text;
using System.Text.Json;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.IntegrationTest.InvioSap;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Passata **adversarial** sulla superficie di lettura e download di Invio a SAP: filtri ostili, valori
/// estremi, body sbagliati, identificativi inventati. La regola di giudizio e' sempre la stessa: un
/// input assurdo e' un errore del client (400/404) o un risultato vuoto, **mai un 500**, e non deve
/// allargare il perimetro dei dati ne' toccare le tabelle.
///
/// I casi "solo rotta" (anno assente, stato sconosciuto, GUID malformato) girano senza la DDL di DATA; quelli
/// che leggono le viste restano gialli finche' gli oggetti non ci sono.
/// </summary>
[Category("InvioSap")]
[Explicit("PF-965: rotte non ancora mappate (fase 2-5). Eseguire con --filter Category=InvioSap.")]
public class InvioSapAdversarialHttpTests
{
    private ApiTestFactory _factory = null!;

    [OneTimeSetUp]
    public void Setup() => _factory = new ApiTestFactory();

    [OneTimeTearDown]
    public void TearDown() => _factory?.Dispose();

    [SetUp]
    public void CheckDb() => TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);

    // =============================================================================================
    // Griglia: filtri obbligatori e vocabolario (RF-15)
    // =============================================================================================

    [TestCase("{}", TestName = "Griglia400(body vuoto: anno obbligatorio)")]
    [TestCase("""{ "anno": null }""", TestName = "Griglia400(anno null)")]
    [TestCase("""{ "anno": 1999 }""", TestName = "Griglia400(anno sotto il minimo)")]
    [TestCase("""{ "anno": 2099 }""", TestName = "Griglia400(anno lontano)")]
    [TestCase("""{ "anno": 2026, "mesi": [13] }""", TestName = "Griglia400(mese 13)")]
    [TestCase("""{ "anno": 2026, "mesi": [0] }""", TestName = "Griglia400(mese 0)")]
    [TestCase("""{ "anno": 2026, "mesi": [5, 5] }""", TestName = "Griglia400(mesi ripetuti)")]
    [TestCase("""{ "anno": 2026, "statoInvio": "REINVIATE" }""", TestName = "Griglia400(stato non ammesso)")]
    [TestCase("""{ "anno": 2026, "statoInvio": "'; DROP TABLE pfw.RegistroInvioSAP; --" }""", TestName = "Griglia400(stato ostile)")]
    [TestCase("""{ "anno": 2026, "tipologieFattura": [""] }""", TestName = "Griglia400(tipologia vuota)")]
    public async Task Griglia_FiltriNonValidi_ShouldRispondere400(string body)
    {
        var resp = await Post(InvioSapTestData.RottaGriglia, body);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [TestCase("""{ "anno": "duemilaventisei" }""", TestName = "GrigliaBinding(anno testo)")]
    [TestCase("""{ "anno": 2026, "mesi": "5" }""", TestName = "GrigliaBinding(mesi scalare)")]
    [TestCase("""{ "anno": 2026, "soloReinviate": "si" }""", TestName = "GrigliaBinding(booleano testo)")]
    [TestCase("[]", TestName = "GrigliaBinding(array invece di oggetto)")]
    [TestCase("null", TestName = "GrigliaBinding(null)")]
    public async Task Griglia_BodyMalformato_ShouldRispondere400_Non500(string body)
    {
        var resp = await Post(InvioSapTestData.RottaGriglia, body);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [TestCase("' OR '1'='1", TestName = "GrigliaInjection(OR 1=1)")]
    [TestCase("'; DROP TABLE pfd.FattureTestata; --", TestName = "GrigliaInjection(DROP TABLE)")]
    [TestCase("' UNION SELECT NULL,NULL,NULL --", TestName = "GrigliaInjection(UNION)")]
    [TestCase("PRIMO SALDO' --", TestName = "GrigliaInjection(commento)")]
    public async Task Griglia_TipologiaOstile_ShouldRestareUnValore_ELaTabellaSopravvive(string ostile)
    {
        InvioSapTestData.SkipSeOggettiAssenti(InvioSapTestData.VistaGriglia);
        var fatture = InvioSapTestData.Scalar("SELECT COUNT(*) FROM pfd.FattureTestata");

        var resp = await Post(InvioSapTestData.RottaGriglia, $$"""{ "anno": 2026, "tipologieFattura": [{{JsonSerializer.Serialize(ostile)}}] }""");

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound).Or.EqualTo(HttpStatusCode.OK),
                "Trattato come valore: nessun gruppo ha quella tipologia (404 o lista vuota).");
            Assert.That(InvioSapTestData.Scalar("SELECT COUNT(*) FROM pfd.FattureTestata"), Is.EqualTo(fatture),
                "Se il numero cambia, l'input e' diventato SQL.");
        });
    }

    [Test]
    public async Task Griglia_ListeEnormi_ShouldNonDare500()
    {
        // 10.000 tipologie e tutti i mesi: o 400 (troppi) o un risultato, mai un errore del server.
        var tipologie = string.Join(",", Enumerable.Range(0, 10_000).Select(i => $"\"T{i}\""));
        var resp = await Post(InvioSapTestData.RottaGriglia,
            $$"""{ "anno": 2026, "mesi": [1,2,3,4,5,6,7,8,9,10,11,12], "tipologieFattura": [{{tipologie}}] }""");

        Assert.That((int)resp.StatusCode, Is.LessThan(500));
    }

    [Test]
    public async Task Griglia_UnSoloAnno_ShouldNonMostrareAltriAnni()
    {
        // RF-15: la griglia non mescola mai anni. Si verifica sul risultato, non sulla regola.
        InvioSapTestData.SkipSeOggettiAssenti(InvioSapTestData.VistaGriglia);
        var resp = await Post(InvioSapTestData.RottaGriglia, """{ "anno": 2026 }""");
        if (resp.StatusCode == HttpStatusCode.NotFound) Assert.Ignore("Nessun gruppo 2026 nel seed.");

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var anni = doc.RootElement.EnumerateArray().Select(g => g.GetProperty("annoRiferimento").GetInt32()).Distinct().ToList();

        Assert.That(anni, Is.EqualTo(new[] { 2026 }));
    }

    [Test]
    public async Task Griglia_OgniRigo_ShouldAvereUnoStatoAmmesso_EContatoriCoerenti()
    {
        InvioSapTestData.SkipSeOggettiAssenti(InvioSapTestData.VistaGriglia);
        var resp = await Post(InvioSapTestData.RottaGriglia, """{ "anno": 2026 }""");
        if (resp.StatusCode == HttpStatusCode.NotFound) Assert.Ignore("Nessun gruppo 2026 nel seed.");

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        foreach (var g in doc.RootElement.EnumerateArray())
        {
            Assert.Multiple(() =>
            {
                Assert.That(g.GetProperty("statoInvio").GetString(), Is.AnyOf("DA_INVIARE", "IN_ELABORAZIONE", "INVIATA"));
                Assert.That(g.GetProperty("numeroFatture").GetInt32(), Is.GreaterThan(0), "Un gruppo vuoto non deve comparire.");
                Assert.That(g.GetProperty("numeroReinviate").GetInt32(), Is.LessThanOrEqualTo(g.GetProperty("numeroFatture").GetInt32()));
                Assert.That(g.GetProperty("meseRiferimento").GetInt32(), Is.InRange(1, 12));
            });
        }
    }

    [Test]
    public async Task Griglia_SoloReinviate_ShouldRestituireSoloGruppiConReinviate()
    {
        InvioSapTestData.SkipSeOggettiAssenti(InvioSapTestData.VistaGriglia);
        var resp = await Post(InvioSapTestData.RottaGriglia, """{ "anno": 2026, "soloReinviate": true }""");
        if (resp.StatusCode == HttpStatusCode.NotFound) return; // ammesso: nessuna re-inviata nel seed

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        foreach (var g in doc.RootElement.EnumerateArray())
            Assert.That(g.GetProperty("numeroReinviate").GetInt32(), Is.GreaterThan(0));
    }

    // =============================================================================================
    // Dettaglio: perimetro del gruppo (RF-3, RF-6)
    // =============================================================================================

    [TestCase("{}", TestName = "Dettaglio400(body vuoto)")]
    [TestCase("""{ "annoRiferimento": 2026, "meseRiferimento": 7, "tipologiaFattura": "SECONDO SALDO" }""", TestName = "Dettaglio400(stato assente)")]
    [TestCase("""{ "annoRiferimento": 2026, "meseRiferimento": 7, "statoInvio": "DA_INVIARE" }""", TestName = "Dettaglio400(tipologia assente)")]
    [TestCase("""{ "annoRiferimento": 2026, "tipologiaFattura": "SECONDO SALDO", "statoInvio": "DA_INVIARE" }""", TestName = "Dettaglio400(mese assente)")]
    [TestCase("""{ "annoRiferimento": 2026, "meseRiferimento": 7, "tipologiaFattura": "SECONDO SALDO", "statoInvio": "INVIATE" }""", TestName = "Dettaglio400(stato del filtro, non della fattura)")]
    [TestCase("""{ "annoRiferimento": 2026, "meseRiferimento": 7, "tipologiaFattura": "SECONDO SALDO", "statoInvio": "DA_INVIARE", "page": 0 }""", TestName = "Dettaglio400(page 0)")]
    [TestCase("""{ "annoRiferimento": 2026, "meseRiferimento": 7, "tipologiaFattura": "SECONDO SALDO", "statoInvio": "DA_INVIARE", "pageSize": -1 }""", TestName = "Dettaglio400(pageSize negativa)")]
    public async Task Dettaglio_RichiestaNonValida_ShouldRispondere400(string body)
    {
        // page 0 / size negativa sono il difetto noto della griglia Gestione Fatture (OFFSET negativo → 500):
        // qui si pretende da subito il comportamento giusto.
        var resp = await Post(InvioSapTestData.RottaDettaglio, body);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Dettaglio_ShouldRestituireSoloFattureDelGruppo()
    {
        InvioSapTestData.SkipSeOggettiAssenti(InvioSapTestData.VistaDettaglio);
        var resp = await Post(InvioSapTestData.RottaDettaglio, $$"""
            { "annoRiferimento": {{InvioSapTestData.Anno}}, "meseRiferimento": {{InvioSapTestData.Mese}},
              "tipologiaFattura": "{{InvioSapTestData.Tipologia}}", "statoInvio": "DA_INVIARE" }
            """);
        if (resp.StatusCode == HttpStatusCode.NotFound) Assert.Ignore("Nessuna fattura DA_INVIARE nel gruppo del seed.");

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        foreach (var f in doc.RootElement.EnumerateArray())
        {
            Assert.Multiple(() =>
            {
                Assert.That(f.GetProperty("tipologiaFattura").GetString(), Is.EqualTo(InvioSapTestData.Tipologia));
                Assert.That(f.GetProperty("statoInvio").GetString(), Is.EqualTo("DA_INVIARE"));
                Assert.That(f.GetProperty("dataUltimaGenerazione").ValueKind, Is.EqualTo(JsonValueKind.Null), "Mai processata: vuota (RF-4).");
                Assert.That(f.GetProperty("nomeFile").ValueKind, Is.EqualTo(JsonValueKind.Null));
                Assert.That(f.GetProperty("numeroInvii").GetInt32(), Is.Zero);
                Assert.That(f.GetProperty("idEnte").GetString(), Is.Not.EqualTo(InvioSapTestData.EntePagoPA).IgnoreCase);
            });
        }
    }

    [TestCase("ragioneSociale", "' OR 1=1 --", TestName = "DettaglioInjection(ragioneSociale)")]
    [TestCase("numeroFattura", "1' OR '1'='1", TestName = "DettaglioInjection(numeroFattura)")]
    public async Task Dettaglio_FiltroOstile_ShouldRestareUnValore(string campo, string ostile)
    {
        InvioSapTestData.SkipSeOggettiAssenti(InvioSapTestData.VistaDettaglio);
        var fatture = InvioSapTestData.Scalar("SELECT COUNT(*) FROM pfd.FattureTestata");

        var resp = await Post(InvioSapTestData.RottaDettaglio, $$"""
            { "annoRiferimento": {{InvioSapTestData.Anno}}, "meseRiferimento": {{InvioSapTestData.Mese}},
              "tipologiaFattura": "{{InvioSapTestData.Tipologia}}", "statoInvio": "DA_INVIARE", "{{campo}}": {{JsonSerializer.Serialize(ostile)}} }
            """);

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound).Or.EqualTo(HttpStatusCode.OK));
            Assert.That(InvioSapTestData.Scalar("SELECT COUNT(*) FROM pfd.FattureTestata"), Is.EqualTo(fatture));
        });
        if (resp.StatusCode == HttpStatusCode.OK)
        {
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            Assert.That(doc.RootElement.GetArrayLength(), Is.Zero, "Un OR 1=1 che allarga il risultato e' un'injection riuscita.");
        }
    }

    // =============================================================================================
    // Download (RF-12, RI-12)
    // =============================================================================================

    [TestCase(InvioSapTestData.RottaDownload, TestName = "Download400(anno assente)")]
    [TestCase(InvioSapTestData.RottaDownloadReinviate, TestName = "DownloadReinviate400(anno assente)")]
    public async Task Download_SenzaAnno_ShouldRispondere400(string rotta)
    {
        var resp = await Post(rotta, "{}");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [TestCase(InvioSapTestData.RottaDownload, TestName = "Download(xlsx)")]
    [TestCase(InvioSapTestData.RottaDownloadReinviate, TestName = "DownloadReinviate(xlsx)")]
    public async Task Download_ShouldRestituireUnXlsx_ONotFound(string rotta)
    {
        InvioSapTestData.SkipSeOggettiAssenti(InvioSapTestData.VistaDettaglio);
        var resp = await Post(rotta, """{ "anno": 2026 }""");
        if (resp.StatusCode == HttpStatusCode.NotFound) return; // nessuna riga nel perimetro: ammesso

        var bytes = await resp.Content.ReadAsByteArrayAsync();
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(resp.Content.Headers.ContentType?.MediaType, Does.Contain("spreadsheetml"));
            Assert.That(bytes.Take(2), Is.EqualTo(new byte[] { 0x50, 0x4B }), "Un xlsx e' uno zip: inizia con PK.");
            Assert.That(resp.Content.Headers.ContentDisposition?.FileName, Does.Contain("InvioSAP"));
        });
    }

    [Test]
    public async Task DownloadReinviate_ShouldContenereLaColonnaStorno()
    {
        InvioSapTestData.SkipSeOggettiAssenti(InvioSapTestData.VistaDettaglio);
        var resp = await Post(InvioSapTestData.RottaDownloadReinviate, """{ "anno": 2026 }""");
        if (resp.StatusCode == HttpStatusCode.NotFound) Assert.Ignore("Nessuna re-inviata nel seed.");

        using var stream = await resp.Content.ReadAsStreamAsync();
        using var wb = new ClosedXML.Excel.XLWorkbook(stream);
        var intestazioni = wb.Worksheets.First().Row(1).CellsUsed().Select(c => c.GetString()).ToList();

        Assert.That(intestazioni, Has.Some.Contains("storno").IgnoreCase, "US6: colonna Numero documento di storno.");
    }

    // =============================================================================================
    // Stato batch
    // =============================================================================================

    [TestCase("non-un-guid", TestName = "Generazioni404(testo)")]
    [TestCase("123", TestName = "Generazioni404(numero)")]
    [TestCase("00000000-0000-0000-0000-000000000000", TestName = "Generazioni404(guid vuoto)")]
    [TestCase("../../api/fatture", TestName = "Generazioni404(path traversal)")]
    public async Task Generazioni_IdentificativoNonValido_ShouldRispondere404(string id)
    {
        // Il vincolo {batchId:guid} fa fallire il routing: 404, non 400 e non 500.
        var resp = await _factory.CreateClientAs(Ruolo.ADMIN)
            .GetAsync(_factory.WithNonce($"{InvioSapTestData.RottaGenerazioni}/{id}"));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Generazioni_ShouldNonEsporreDatiDiAltriUtenti_NelMessaggioDiErrore()
    {
        var resp = await _factory.CreateClientAs(Ruolo.ADMIN)
            .GetAsync(_factory.WithNonce($"{InvioSapTestData.RottaGenerazioni}/{Guid.NewGuid()}"));
        var testo = await resp.Content.ReadAsStringAsync();

        Assert.That(testo, Does.Not.Contain("Exception").And.Not.Contain("SqlException").And.Not.Contain("at PortaleFatture"),
            "Un 404 non porta stack trace (T053).");
    }

    // =============================================================================================
    // Helper
    // =============================================================================================

    private async Task<HttpResponseMessage> Post(string rotta, string body)
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var resp = await client.PostAsync(_factory.WithNonce(rotta), new StringContent(body, Encoding.UTF8, "application/json"));
        TestContext.Out.WriteLine($"{rotta} -> {(int)resp.StatusCode}");
        return resp;
    }
}
