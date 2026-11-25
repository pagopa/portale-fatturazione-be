using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Extensions;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// PF-753 end-to-end sul DB seedato (ApiTestFactory: rotte vere, binding, autorizzazione, query vere).
/// Verifica sul file scaricato ciò che gli unit test verificano sui builder:
/// <list type="bullet">
/// <item>Legenda primo sheet, con le intestazioni giuste, e ogni foglio del file descritto in legenda;</item>
/// <item>ogni formula SUBTOTAL coerente (colonna, intervallo, valore in cache = somma) sui dati reali;</item>
/// <item>colonne dello sheet "Non Fatturate" ridimensionate sul contenuto;</item>
/// <item>casi avversari: senza autenticazione, identità aderente, ruolo OPERATOR, tipologia inesistente.</item>
/// </list>
/// Rotte: <c>api/fatture/report</c> (emesse), <c>api/fatture/sospese/report</c> (sospese),
/// <c>api/fatture/pagopa/non-inviate/report</c> (non inviate). Seed: 2026/2 SECONDO SALDO (fattura 8001 per
/// le emesse, tmp 7001/7002 per le sospese); la fattura non inviata 7501 viene completata dal test.
/// Le righe di staging di Gestione Fatture create qui stanno sull'anno riservato 2029 e vengono ripulite.
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
[Category("Fatture")]
public class FattureReportPf753HttpTests
{
    private const string RottaEmesse = "/api/fatture/report";
    private const string RottaSospese = "/api/fatture/sospese/report";
    private const string RottaNonInviate = "/api/fatture/pagopa/non-inviate/report";

    private const string BodySaldo = """{ "anno": 2026, "mese": 2, "tipologiaFattura": ["SECONDO SALDO"] }""";

    private const string IdEnte = "11111111-1111-1111-1111-111111111111";
    private const int AnnoStaging = 2029;                   // riservato a questa classe
    private const string UtenteTest = "integration-test-pf753";

    // Fattura non inviata del seed, completata dal test con riga e REL (come in FattureReportNonFatturateHttpTests).
    private const long IdFatturaNonInviata = 7501;
    private const string EnteNonInviata = "77777777-7777-7777-7777-777777777777";

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

    // ============================================================================ Legenda e SUBTOTAL sui file veri

    public static IEnumerable<TestCaseData> Report()
    {
        yield return new TestCaseData(RottaEmesse, BodySaldo, false).SetName("Pf753_Http_Emesse_SecondoSaldo");
        yield return new TestCaseData(RottaSospese, BodySaldo, false).SetName("Pf753_Http_Sospese_SecondoSaldo");
        yield return new TestCaseData(RottaNonInviate, """{ "tipologiaFattura": ["VAR. SEMESTRALE"], "inviata": 0 }""", true)
            .SetName("Pf753_Http_NonInviate_VarSemestrale");
    }

    [TestCaseSource(nameof(Report))]
    public async Task Report_LegendaPrimoSheet_EFogliDescritti(string rotta, string body, bool nonInviata)
    {
        if (nonInviata) CompletaLaFatturaNonInviata();

        var (stato, workbooks) = await Scarica(rotta, body);

        Assert.That(stato, Is.EqualTo(HttpStatusCode.OK), "Senza file il test non verifica nulla: mancano i dati del seed.");
        Assert.That(workbooks, Is.Not.Empty);
        foreach (var (nome, wb) in workbooks)
        {
            var fogli = wb.Worksheets.Select(w => w.Name).ToList();
            var legenda = wb.Worksheet(1);
            var descritti = legenda.RowsUsed().Skip(1).Select(r => r.Cell(1).GetString()).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(legenda.Name, Is.EqualTo(ReportLegende.SheetName), $"{nome}: la Legenda deve essere il primo foglio.");
                Assert.That(fogli.Count(f => f == ReportLegende.SheetName), Is.EqualTo(1), $"{nome}: una sola Legenda.");
                Assert.That(legenda.Cell(1, 1).GetString(), Is.EqualTo(ReportLegende.ColonnaNomeSheet));
                Assert.That(legenda.Cell(1, 2).GetString(), Is.EqualTo(ReportLegende.ColonnaDescrizione));
                Assert.That(descritti.First(), Is.EqualTo(ReportLegende.SheetName), "La prima riga descrive la Legenda stessa.");
                Assert.That(fogli, Is.SubsetOf(descritti), $"{nome}: ogni foglio generato deve comparire in legenda.");
                Assert.That(legenda.CellsUsed().Any(c => c.HasFormula), Is.False, "La legenda non ha totali.");
            });
        }
    }

    private static readonly Regex Subtotal = new(@"^SUBTOTAL\(9,([A-Z]+)2:([A-Z]+)(\d+)\)$");

    [TestCaseSource(nameof(Report))]
    public async Task Report_Subtotal_CoerenteSuiDatiReali(string rotta, string body, bool nonInviata)
    {
        if (nonInviata) CompletaLaFatturaNonInviata();

        var (stato, workbooks) = await Scarica(rotta, body);
        Assert.That(stato, Is.EqualTo(HttpStatusCode.OK));

        var formule = 0;
        foreach (var (_, wb) in workbooks)
            foreach (var ws in wb.Worksheets)
            {
                var totali = ws.RowsUsed().FirstOrDefault(r => r.Cell(1).GetString() == "Totali:");
                if (totali is null) continue;
                var n = totali.RowNumber();
                foreach (var c in totali.CellsUsed().Where(c => c.HasFormula))
                {
                    formule++;
                    var m = Subtotal.Match(c.FormulaA1);
                    var somma = Enumerable.Range(2, n - 3).Select(r => ws.Cell(r, c.Address.ColumnNumber))
                        .Where(x => x.DataType == XLDataType.Number).Sum(x => x.GetValue<decimal>());
                    Assert.Multiple(() =>
                    {
                        Assert.That(m.Success, Is.True, $"{ws.Name}!{c.Address}: '{c.FormulaA1}'");
                        Assert.That(m.Groups[1].Value, Is.EqualTo(c.Address.ColumnLetter));
                        Assert.That(int.Parse(m.Groups[3].Value), Is.EqualTo(n - 2));
                        Assert.That(Convert.ToDecimal(c.CachedValue.GetNumber()), Is.EqualTo(somma), $"{ws.Name}!{c.Address}");
                    });
                }
            }

        Assert.That(formule, Is.GreaterThan(0), "Sui dati del seed ci sono righe dati: deve esserci almeno un SUBTOTAL.");
    }

    // ============================================================================ "Non Fatturate": resize

    [Test]
    public async Task NonFatturate_ColonneRidimensionateSulContenuto()
    {
        var lunga = "nota di collaudo PF-753 " + new string('x', 40);   // 64 caratteri, più la data
        SeminaStaging("SECONDO SALDO", 1,
            $$"""[{"Data":"2026-11-19T09:00:00","Testo":"breve"},{"Data":"2026-11-20T10:00:00","Testo":"{{lunga}}"}]""");

        var (stato, workbooks) = await Scarica(RottaEmesse, BodySaldo);

        Assert.That(stato, Is.EqualTo(HttpStatusCode.OK));
        var ws = workbooks.Select(w => w.Wb).Single(w => w.Worksheets.Contains("Non Fatturate")).Worksheet("Non Fatturate");
        double Larghezza(string intestazione) =>
            ws.Column(ws.Row(1).CellsUsed().Single(c => c.GetString() == intestazione).Address.ColumnNumber).Width;
        var rigaNota = $"2026-11-20 10:00:00 {lunga}";

        Assert.Multiple(() =>
        {
            Assert.That(ws.CellsUsed().Any(c => c.GetString().Contains(lunga)), Is.True, "La nota seminata è nello sheet.");
            // La riga è di 84 caratteri: oltre il limite di 80 la colonna si ferma e la nota va a capo.
            Assert.That(Larghezza("Note"), Is.GreaterThanOrEqualTo(Math.Min(rigaNota.Length, ExcelExtensions.AutoFitMaxChars)),
                "La colonna Note segue la riga più lunga, fino al limite.");
            Assert.That(Larghezza("Note"), Is.LessThan(ExcelExtensions.AutoFitMaxChars + 15), "Non la somma delle righe della nota.");
            Assert.That(Larghezza("Id Ente"), Is.GreaterThanOrEqualTo(IdEnte.Length), "Il GUID dell'ente si legge per intero.");
            Assert.That(Larghezza("Stato"), Is.GreaterThanOrEqualTo("POSTICIPATA".Length));
        });
    }

    // ============================================================================ avversari: accesso e input

    [TestCase(RottaEmesse)]
    [TestCase(RottaSospese)]
    [TestCase(RottaNonInviate)]
    public async Task SenzaAutenticazione_401(string rotta)
    {
        var resp = await _factory.CreateClientAs(null).PostAsync(_factory.WithNonce(rotta), Json(BodySaldo));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [TestCase(RottaEmesse)]
    [TestCase(RottaSospese)]
    public async Task IdentitaAderente_403(string rotta)
    {
        // Rotte admin (PagoPAPolicy): un utente SelfCare di un ente non deve poter scaricare i report di tutti.
        var client = _factory.CreateClientAs(Ruolo.ADMIN, auth: AuthType.SELFCARE, profilo: Profilo.PubblicaAmministrazione);
        var resp = await client.PostAsync(_factory.WithNonce(rotta), Json(BodySaldo));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task RuoloOperator_PuoScaricare_ConLegenda()
    {
        // Download in sola lettura: aperto a OPERATOR e ADMIN, come le altre letture.
        var (stato, workbooks) = await Scarica(RottaEmesse, BodySaldo, Ruolo.OPERATOR);

        Assert.That(stato, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(workbooks.All(w => w.Wb.Worksheet(1).Name == ReportLegende.SheetName), Is.True);
    }

    [Test]
    public async Task TipologiaInesistente_404_SenzaFile()
    {
        var (stato, workbooks) = await Scarica(RottaEmesse, """{ "anno": 2026, "mese": 2, "tipologiaFattura": ["NON ESISTE"] }""");

        Assert.Multiple(() =>
        {
            Assert.That(stato, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(workbooks, Is.Empty);
        });
    }

    [Test]
    public async Task PeriodoSenzaDati_404_AncheConStagingDelPeriodo()
    {
        // Una posticipata da sola non genera il report: lo sheet "Non Fatturate" accompagna un report esistente.
        SeminaStaging("SECONDO SALDO", 3, "[]", anno: 2030, mese: 3);

        var (stato, _) = await Scarica(RottaEmesse, """{ "anno": 2030, "mese": 3, "tipologiaFattura": ["SECONDO SALDO"] }""");

        Assert.That(stato, Is.EqualTo(HttpStatusCode.NotFound));
    }

    // ============================================================================ PF-1029: report REL

    // Prima del PF-1029 i due report avevano un foglio chiamato "T": ToTablev2 usa nameof(T) come nome di default.

    [Test]
    public async Task ReportQuadratureRel_FoglioConNomeDaSpecifica()
    {
        // pfd.ContestazioniStorico e' nel seed dal 23/11/2026 (DDL reale); la guardia resta per un container precedente.
        SeminaQuadratura("SECONDO SALDO");

        var resp = await _factory.CreateClientAs(Ruolo.ADMIN).PostAsync(
            _factory.WithNonce("/api/rel/pagopa/quadratura/ricerca") + "&binary=true",
            Json("""{ "anno": 2026, "mese": 2, "tipologiaFattura": "SECONDO SALDO" }"""));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Il test semina NotificheCount 2026/2: il report deve esistere.");
        using var wb = new XLWorkbook(new MemoryStream(await resp.Content.ReadAsByteArrayAsync()));
        TestContext.Out.WriteLine($"  fogli: {string.Join(" | ", wb.Worksheets.Select(w => w.Name))}");
        Assert.Multiple(() =>
        {
            Assert.That(wb.Worksheets.Select(w => w.Name), Does.Contain("Quadrature Reg Esecuzione"));
            Assert.That(wb.Worksheets.Select(w => w.Name), Does.Not.Contain("T"));
            Assert.That(wb.Worksheet(1).Name, Is.EqualTo(ReportLegende.SheetName), "PF-1029 voce 10: Legenda come primo sheet.");
            Assert.That(wb.Worksheet("Quadrature Reg Esecuzione").CellsUsed().Any(c => c.HasFormula && c.FormulaA1.StartsWith("SUBTOTAL(9,")),
                Is.True, "Il rename non deve perdere la riga dei totali con SUBTOTAL.");
        });
    }

    [Test]
    public async Task ReportRelNonFatturate_FoglioConNomeDaSpecifica()
    {
        CompletaLaFatturaNonInviata(); // una REL non fatturata in piu' (ente 7, 2026/7), oltre a quelle del seed

        var resp = await _factory.CreateClientAs(Ruolo.ADMIN).GetAsync(_factory.WithNonce("/api/rel/pagopa/nonfatturate"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var wb = new XLWorkbook(new MemoryStream(await resp.Content.ReadAsByteArrayAsync()));
        TestContext.Out.WriteLine($"  fogli: {string.Join(" | ", wb.Worksheets.Select(w => w.Name))}");
        Assert.That(wb.Worksheets.Select(w => w.Name), Is.EqualTo(new[] { "Legenda", "Reg Esecuzioni non fatturate" }));
    }

    // ============================================================================ PF-1029: Quadrature nel PRIMO SALDO (voce 5)

    private const string BodyPrimoSaldo = """{ "anno": 2026, "mese": 2, "tipologiaFattura": ["PRIMO SALDO"] }""";

    [Test]
    public async Task PrimoSaldo_SheetQuadrature_ConDatiDellaQuadraturaESubtotal()
    {
        SeminaQuadratura("PRIMO SALDO");

        var (stato, workbooks) = await Scarica(RottaEmesse, BodyPrimoSaldo);

        Assert.That(stato, Is.EqualTo(HttpStatusCode.OK));
        var wb = workbooks.Single().Wb;
        var fogli = wb.Worksheets.Select(w => w.Name).ToList();
        Assert.That(fogli, Does.Contain(ReportLegende.SheetQuadratureRel));
        var ws = wb.Worksheet(ReportLegende.SheetQuadratureRel);
        Assert.Multiple(() =>
        {
            Assert.That(fogli.IndexOf(ReportLegende.SheetQuadratureRel), Is.EqualTo(fogli.IndexOf("Reg. Esec. PAC non firmate") + 1));
            Assert.That(ws.CellsUsed().Select(c => c.GetString()), Does.Contain(IdEnte), "La riga dell'ente seminata dal test c'e'.");
            Assert.That(ws.CellsUsed().Any(c => c.HasFormula && c.FormulaA1.StartsWith("SUBTOTAL(9,")), Is.True);
            Assert.That(wb.Worksheet(ReportLegende.SheetName).CellsUsed().Select(c => c.GetString()), Does.Contain(ReportLegende.SheetQuadratureRel),
                "Lo sheet e' descritto in legenda.");
        });
    }

    [Test]
    public async Task PrimoSaldo_StessiDatiDelDownloadDiQuadratura()
    {
        // Lo sheet e il download di api/rel/pagopa/quadratura/ricerca nascono dalla stessa query: stesse righe, stessi totali.
        SeminaQuadratura("PRIMO SALDO");

        var (_, workbooks) = await Scarica(RottaEmesse, BodyPrimoSaldo);
        var resp = await _factory.CreateClientAs(Ruolo.ADMIN).PostAsync(
            _factory.WithNonce("/api/rel/pagopa/quadratura/ricerca") + "&binary=true",
            Json("""{ "anno": 2026, "mese": 2, "tipologiaFattura": "PRIMO SALDO" }"""));
        using var download = new XLWorkbook(new MemoryStream(await resp.Content.ReadAsByteArrayAsync()));

        static List<string> Valori(IXLWorksheet ws) => ws.CellsUsed().Select(c => c.HasFormula ? c.FormulaA1 : c.GetString()).ToList();
        Assert.That(Valori(workbooks.Single().Wb.Worksheet(ReportLegende.SheetQuadratureRel)),
            Is.EqualTo(Valori(download.Worksheet(ReportLegende.SheetQuadratureRel))));
    }

    [TestCase("SECONDO SALDO")]
    [TestCase("VAR. SEMESTRALE")]
    [TestCase("SEM. SOSPESI")]
    public async Task AltriSaldi_SenzaSheetQuadrature(string tipologia)
    {
        SeminaQuadratura(tipologia);

        var (stato, workbooks) = await Scarica(RottaEmesse, $$"""{ "anno": 2026, "mese": 2, "tipologiaFattura": ["{{tipologia}}"] }""");

        Assert.That(stato, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(workbooks.Single().Wb.Worksheets.Select(w => w.Name), Does.Not.Contain(ReportLegende.SheetQuadratureRel),
            "La voce 5 riguarda solo il PRIMO SALDO, anche se la quadratura del periodo esiste.");
    }

    // ============================================================================ PF-1029: totali in Non Fatturate (voce 11)

    [Test]
    public async Task NonFatturate_RigaTotaliConSubtotal_SuiDatiReali()
    {
        SeminaStaging("SECONDO SALDO", 3, "[]");

        var (stato, workbooks) = await Scarica(RottaEmesse, BodySaldo);

        Assert.That(stato, Is.EqualTo(HttpStatusCode.OK));
        var ws = workbooks.Single().Wb.Worksheet("Non Fatturate");
        var intestazioni = ws.Row(1).CellsUsed().ToDictionary(c => c.GetString(), c => c.Address.ColumnNumber);
        var totali = ws.RowsUsed().Single(r => r.Cell(1).GetString() == "Totali:");
        Assert.Multiple(() =>
        {
            Assert.That(totali.Cell(intestazioni["Totale Imponibile"]).FormulaA1, Does.StartWith("SUBTOTAL(9,"));
            Assert.That(totali.Cell(intestazioni["Anno"]).IsEmpty(), Is.True, "Anno non si somma.");
            Assert.That(totali.Cell(intestazioni["Mese"]).IsEmpty(), Is.True, "Mese non si somma.");
        });
    }

    // ============================================================================ helper

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    // PF-1029: marcatore delle righe di quadratura scritte dal test, per ripulirle senza toccare il seed.
    private const decimal TotaleMarcatoreQuadratura = 987.65m;

    /// <summary>
    /// PF-1029: semina il "count" dell'ente 1 su 2026/2 e una riga di contestazioni per la tipologia: e' cio' da cui
    /// parte la quadratura (FROM pfd.NotificheCount). Il seed non ha NotificheCount per 2026/2.
    /// </summary>
    private static void SeminaQuadratura(string tipologia)
    {
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "pfd.ContestazioniStorico");
        Esegui($@"
            IF NOT EXISTS (SELECT 1 FROM pfd.NotificheCount WHERE internal_organization_id='{IdEnte}' AND [year]=2026 AND [month]=2)
            INSERT INTO pfd.NotificheCount (internal_organization_id, contract_id, [year], [month],
                TotaleAnalogico, TotaleDigitale, TotaleNotificheAnalogiche, TotaleNotificheDigitali, Totale)
            VALUES ('{IdEnte}', 'TOKEN-E1', 2026, 2, 500.00, 487.65, 5, 3, {TotaleMarcatoreQuadratura.ToString(System.Globalization.CultureInfo.InvariantCulture)});

            IF NOT EXISTS (SELECT 1 FROM pfd.ContestazioniStorico WHERE internal_organization_id='{IdEnte}' AND [year]=2026 AND [month]=2 AND TipologiaFattura='{tipologia}')
            INSERT INTO pfd.ContestazioniStorico (internal_organization_id, contract_id, [year], [month],
                TotaleAnalogico, TotaleDigitale, TotaleNotificheAnalogiche, TotaleNotificheDigitali, Totale, Iva, TipologiaFattura)
            VALUES ('{IdEnte}', 'TOKEN-E1', 2026, 2, 10.00, 0, 1, 0, 10.00, 22, '{tipologia}');");
    }

    /// <summary>Scarica lo zip del report e apre ogni xlsx con ClosedXML.</summary>
    private async Task<(HttpStatusCode Stato, List<(string Nome, XLWorkbook Wb)> Workbooks)> Scarica(string rotta, string body, string ruolo = Ruolo.ADMIN)
    {
        var resp = await _factory.CreateClientAs(ruolo).PostAsync(_factory.WithNonce(rotta), Json(body));
        TestContext.Out.WriteLine($"{rotta} {body} -> {(int)resp.StatusCode}");
        if (resp.StatusCode != HttpStatusCode.OK) return (resp.StatusCode, []);

        var risultato = new List<(string, XLWorkbook)>();
        using var zip = new ZipArchive(new MemoryStream(await resp.Content.ReadAsByteArrayAsync()), ZipArchiveMode.Read);
        foreach (var voce in zip.Entries.Where(e => e.FullName.EndsWith(".xlsx")))
        {
            var buffer = new MemoryStream();
            await voce.Open().CopyToAsync(buffer);
            buffer.Position = 0;
            var wb = new XLWorkbook(buffer);
            TestContext.Out.WriteLine($"  {voce.FullName}: {string.Join(" | ", wb.Worksheets.Select(w => w.Name))}");
            risultato.Add((voce.FullName, wb));
        }
        return (resp.StatusCode, risultato);
    }

    private static void SeminaStaging(string tipologia, int stato, string noteJson, int anno = AnnoStaging, int mese = 1) => Esegui($@"
        INSERT INTO cfg.GestioneFatture
            (FkIdEnte, FkTipologiaFattura, Anno, Mese, DataInserimento, IdUtenteInserimento, Stato, Azione, Note)
        VALUES ('{IdEnte}', '{tipologia}', {anno}, {mese}, GETDATE(), '{UtenteTest}', 0, 'POSTICIPATA', N'{noteJson.Replace("'", "''")}');");

    private static void CompletaLaFatturaNonInviata() => Esegui($@"
        IF NOT EXISTS (SELECT 1 FROM pfd.FattureRighe WHERE FkIdFattura = {IdFatturaNonInviata})
        INSERT INTO pfd.FattureRighe
            (FkIdFattura, NumeroLinea, Testo, CodiceMateriale, Quantita, PrezzoUnitario, Imponibile, RigaBollo, PeriodoRiferimento)
        VALUES ({IdFatturaNonInviata}, 1, 'riga report', 'MAT-A', 1, 2000.00, 2000.00, 0, '07/2026');

        IF NOT EXISTS (SELECT 1 FROM pfd.RelTestata
                       WHERE internal_organization_id='{EnteNonInviata}' AND [year]=2026 AND [month]=7)
        INSERT INTO pfd.RelTestata
            (internal_organization_id, contract_id, TipologiaFattura, [year], [month],
             TotaleAnalogico, TotaleDigitale, TotaleNotificheAnalogiche, TotaleNotificheDigitali,
             Totale, TotaleAnalogicoIva, TotaleDigitaleIva, TotaleIva, Caricata, RelFatturata,
             AsseverazioneTotaleAnalogico, AsseverazioneTotaleDigitale,
             AsseverazioneTotaleNotificheAnalogiche, AsseverazioneTotaleNotificheDigitali,
             AsseverazioneTotale, AsseverazioneTotaleAnalogicoIva, AsseverazioneTotaleDigitaleIva, AsseverazioneTotaleIva)
        VALUES ('{EnteNonInviata}', 'TOKEN-E7', 'VAR. SEMESTRALE', 2026, 7,
                1000.00, 1000.00, 10, 10, 2000.00, 1220.00, 1220.00, 2440.00, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0);");

    private static void Pulisci() => Esegui($@"
        DELETE FROM cfg.GestioneFatture WHERE IdUtenteInserimento='{UtenteTest}';
        DELETE FROM pfd.FattureRighe WHERE FkIdFattura = {IdFatturaNonInviata};
        DELETE FROM pfd.RelTestata WHERE internal_organization_id='{EnteNonInviata}' AND [year]=2026 AND [month]=7;
        DELETE FROM pfd.NotificheCount WHERE internal_organization_id='{IdEnte}' AND [year]=2026 AND [month]=2 AND Totale=987.65;
        IF OBJECT_ID('pfd.ContestazioniStorico','U') IS NOT NULL
            DELETE FROM pfd.ContestazioniStorico WHERE internal_organization_id='{IdEnte}' AND [year]=2026 AND [month]=2 AND Totale=10.00;");

    private static void Esegui(string sql)
    {
        try
        {
            using var conn = new SqlConnection(LocalTestDb.ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
        catch (SqlException ex) { TestContext.Out.WriteLine($"SQL best-effort: {ex.Message}"); }
    }
}
