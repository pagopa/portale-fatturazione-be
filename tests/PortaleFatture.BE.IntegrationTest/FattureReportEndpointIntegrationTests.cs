using ClosedXML.Excel;
using MediatR;
using Microsoft.Extensions.Configuration;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Integration test a livello endpoint (extension ReportFatture / ReportFattureSospese con IMediator reale),
/// equivalenti ai casi Postman su /api/fatture/report e /api/fatture/sospese/report.
/// Coprono: auto-popolamento tipologia (body senza TipologiaFattura), filtri NULL, fix NRE,
/// generazione Excel e presenza della colonna "Rel Non Firmata" (verificata con ClosedXML).
///
/// REQUISITI dati (UAT): PortaleFattureOptions:ConnectionString + IntegrationTest:Anno/Mese/TipologiaFattura.
/// I test "strutturali" richiedono solo connettivita' al DB; i "data-dependent" usano Assume.That
/// (Inconclusive senza dati). I report sono restituiti come Dictionary&lt;nomeFile, bytesExcel&gt;.
/// </summary>
public class FattureReportEndpointIntegrationTests
{
    private const string RelNonFirmataCaption = "Rel Non Firmata";
    private const string EntiFattSospeseSheetPrefix = "Enti Fatt. Sospese";
    private const string EntiFattSheetPrefix = "Enti Fatt.";

    private IMediator _handler;
    private IConfiguration _conf;

    [SetUp]
    public void Setup()
    {
        _handler = ServiceProvider.GetRequiredService<IMediator>();
        _conf = ServiceProvider.GetRequiredService<IConfiguration>();
    }

    private static AuthenticationInfo AdminAuth() => new()
    {
        IdEnte = Guid.NewGuid().ToString(),
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };

    private int ConfAnno => int.TryParse(_conf["IntegrationTest:Anno"], out var a) ? a : 2026;
    private int ConfMese => int.TryParse(_conf["IntegrationTest:Mese"], out var m) ? m : 2;
    private string ConfTipologia => _conf["IntegrationTest:TipologiaFattura"] ?? "SECONDO SALDO";

    private static FatturaSospeseRicercaRequest BuildSospeseRequest(
        int anno, int mese, string? tipologia = null, int? idTipoContratto = null, int? inviata = null)
        => new()
        {
            Anno = anno,
            Mese = mese,
            TipologiaFattura = tipologia == null ? null : new[] { tipologia },
            IdTipoContratto = idTipoContratto,
            Inviata = inviata
        };

    private static FatturaRicercaRequest BuildReportRequest(
        int anno, int mese, string? tipologia = null, int? idTipoContratto = null, int? inviata = null)
        => new()
        {
            Anno = anno,
            Mese = mese,
            TipologiaFattura = tipologia == null ? null : new[] { tipologia },
            IdTipoContratto = idTipoContratto,
            Inviata = inviata
        };

    /// <summary>Cerca nel primo foglio che inizia con <paramref name="sheetPrefix"/> la colonna <paramref name="caption"/>.</summary>
    private static bool TryColumnPresence(byte[] excelBytes, string sheetPrefix, string caption, out bool sheetFound)
    {
        sheetFound = false;
        using var ms = new MemoryStream(excelBytes);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.FirstOrDefault(w => w.Name.StartsWith(sheetPrefix, StringComparison.OrdinalIgnoreCase));
        if (ws == null)
            return false;
        sheetFound = true;
        return ws.Row(1).CellsUsed().Any(c => string.Equals(c.GetString(), caption, StringComparison.OrdinalIgnoreCase));
    }

    // ---------------- SOSPESE: strutturali (no data) ----------------

    /// <summary>
    /// Equivalente Postman: /api/fatture/sospese/report per mese, senza TipologiaFattura (auto-popolamento).
    /// Deve eseguire senza eccezioni e restituire un dizionario (eventualmente vuoto = 404 lato endpoint).
    /// </summary>
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public async Task ReportFattureSospese_PerMese_AutoTipologia_ShouldNotThrow(int mese)
    {
        var reports = await BuildSospeseRequest(2026, mese).ReportFattureSospese(_handler, AdminAuth());
        Assert.That(reports, Is.Not.Null);
    }

    /// <summary>Equivalente Postman: 2026.02 SECONDO SALDO.</summary>
    [Test]
    public async Task ReportFattureSospese_SecondoSaldo_ShouldNotThrow()
    {
        var reports = await BuildSospeseRequest(2026, 2, "SECONDO SALDO").ReportFattureSospese(_handler, AdminAuth());
        Assert.That(reports, Is.Not.Null);
    }

    /// <summary>Equivalente Postman: "Altri filtri NULL" (tipologia/idEnti vuoti -> null, filtri null).</summary>
    [Test]
    public async Task ReportFattureSospese_FiltriNull_ShouldNotThrow()
    {
        var request = new FatturaSospeseRicercaRequest
        {
            Anno = 2026,
            Mese = 4,
            TipologiaFattura = Array.Empty<string>(), // setter -> null (auto-popolamento)
            IdEnti = Array.Empty<string>(),           // setter -> null
            Cancellata = false,
            IdTipoContratto = null,
            Inviata = null
        };

        var reports = await request.ReportFattureSospese(_handler, AdminAuth());
        Assert.That(reports, Is.Not.Null);
    }

    /// <summary>
    /// Regressione fix NRE: senza TipologiaFattura su periodo senza dati l'auto-popolamento restituisce
    /// lista vuota; prima del fix il foreach andava in NullReferenceException. Ora ritorna dizionario vuoto.
    /// Data-independent (serve solo connettivita' al DB).
    /// </summary>
    [Test]
    public async Task ReportFattureSospese_AutoTipologia_EmptyPeriod_ShouldReturnEmpty_NoThrow()
    {
        var reports = await BuildSospeseRequest(1900, 1).ReportFattureSospese(_handler, AdminAuth());
        Assert.That(reports, Is.Not.Null);
        Assert.That(reports, Is.Empty);
    }

    // ---------------- SOSPESE: data-dependent (UAT) ----------------

    /// <summary>
    /// Con dati nel periodo configurato, il foglio "Enti Fatt. Sospese" deve contenere la colonna "Rel Non Firmata".
    /// </summary>
    [Test]
    public async Task ReportFattureSospese_WhenDataPresent_ExcelHasRelNonFirmataColumn()
    {
        var reports = await BuildSospeseRequest(ConfAnno, ConfMese, ConfTipologia).ReportFattureSospese(_handler, AdminAuth());

        Assume.That(reports.Count, Is.GreaterThan(0),
            "Nessun report per il periodo: eseguire in UAT valorizzando IntegrationTest:Anno/Mese/TipologiaFattura.");

        var results = new List<bool>();
        foreach (var bytes in reports.Values)
        {
            var hasColumn = TryColumnPresence(bytes, EntiFattSospeseSheetPrefix, RelNonFirmataCaption, out var sheetFound);
            if (sheetFound)
                results.Add(hasColumn);
        }

        Assume.That(results.Count, Is.GreaterThan(0),
            $"Nessun foglio '{EntiFattSospeseSheetPrefix}' generato: eseguire in UAT con dati.");
        Assert.That(results.All(x => x), Is.True,
            $"Il foglio '{EntiFattSospeseSheetPrefix}' deve contenere la colonna '{RelNonFirmataCaption}'.");
    }

    // ---------------- REPORT EMESSE (non sospese): regressione DTO condiviso ----------------

    /// <summary>Equivalente Postman: /api/fatture/report per mese, senza TipologiaFattura (auto-popolamento).</summary>
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public async Task ReportFatture_PerMese_AutoTipologia_ShouldNotThrow(int mese)
    {
        var reports = await BuildReportRequest(2026, mese).ReportFatture(_handler, AdminAuth());
        Assert.That(reports, Is.Not.Null);
    }

    /// <summary>Regressione fix NRE sul ramo ReportFatture (auto-popolamento su periodo vuoto -> nessuna eccezione).</summary>
    [Test]
    public async Task ReportFatture_AutoTipologia_EmptyPeriod_ShouldReturnEmpty_NoThrow()
    {
        var reports = await BuildReportRequest(1900, 1).ReportFatture(_handler, AdminAuth());
        Assert.That(reports, Is.Not.Null);
        Assert.That(reports, Is.Empty);
    }

    /// <summary>
    /// Anche il report emesse usa FattureRelExcelDto: con dati, il foglio "Enti Fatt." deve esporre la colonna
    /// "Rel Non Firmata" (valorizzata vuota per le righe non-sospese).
    /// </summary>
    [Test]
    public async Task ReportFatture_WhenDataPresent_ExcelHasRelNonFirmataColumn()
    {
        var reports = await BuildReportRequest(ConfAnno, ConfMese, ConfTipologia).ReportFatture(_handler, AdminAuth());

        Assume.That(reports.Count, Is.GreaterThan(0),
            "Nessun report per il periodo: eseguire in UAT.");

        var results = new List<bool>();
        foreach (var bytes in reports.Values)
        {
            var hasColumn = TryColumnPresence(bytes, EntiFattSheetPrefix, RelNonFirmataCaption, out var sheetFound);
            if (sheetFound)
                results.Add(hasColumn);
        }

        Assume.That(results.Count, Is.GreaterThan(0),
            $"Nessun foglio '{EntiFattSheetPrefix}' generato: eseguire in UAT con dati.");
        Assert.That(results.All(x => x), Is.True,
            $"Il foglio '{EntiFattSheetPrefix}' deve contenere la colonna '{RelNonFirmataCaption}'.");
    }
}
