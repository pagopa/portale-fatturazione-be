using ClosedXML.Excel;
using MediatR;
using Microsoft.Extensions.Configuration;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Integration test end-to-end (extension ReportFatture / ReportFattureSospese con IMediator reale):
/// verificano l'invariante colonna 'Rel Non Firmata' sui report generati.
/// I test data-dependent SECONDO SALDO girano su DB seedato (_seedHandler) col seed dedicato 2026/2/SECONDO SALDO:
/// la fattura regolare 8001 (+ RelTestata) alimenta il ramo non-sospese (fogli "Regolari Esecuzioni"/"Enti Fatt."
/// SENZA la colonna); il seed tmp* (7001/7002) alimenta il ramo sospese (fogli "Reg. Esec. Sospese"/"Enti Fatt.
/// Sospese" CON la colonna). I test strutturali ANTICIPO/ACCONTO restano su connessione di default (_handler):
/// leggono viste legacy pfd.vFattureAcconto / pfd.vModuloCommessaTotali non presenti nel seed.
/// </summary>
public class FattureReportEndpointE2ETests
{
    private const string Caption = "Rel Non Firmata";
    // Connessione di default (UAT): i test strutturali ANTICIPO/ACCONTO leggono viste legacy
    // (pfd.vFattureAcconto / pfd.vModuloCommessaTotali) non presenti nel DB seedato.
    private IMediator _handler;
    // DB seedato: per i test data-dependent SECONDO SALDO (rel emesse/sospese), deterministici sul seed.
    private IMediator _seedHandler;
    private IConfiguration _conf;

    [SetUp]
    public void Setup()
    {
        _handler = ServiceProvider.GetRequiredService<IMediator>();
        _seedHandler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
        _conf = ServiceProvider.GetRequiredService<IConfiguration>();
    }

    private static AuthenticationInfo AdminAuth() => new()
    {
        IdEnte = Guid.NewGuid().ToString(),
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };

    private int Anno => int.TryParse(_conf["IntegrationTest:Anno"], out var a) ? a : 2026;
    private int Mese => int.TryParse(_conf["IntegrationTest:Mese"], out var m) ? m : 2;
    private string Tipologia => _conf["IntegrationTest:TipologiaFattura"] ?? "SECONDO SALDO";

    private static List<(string Sheet, bool HasColumn)> InspectSheets(Dictionary<string, byte[]> reports)
    {
        var result = new List<(string, bool)>();
        foreach (var bytes in reports.Values)
        {
            using var wb = new XLWorkbook(new MemoryStream(bytes));
            foreach (var ws in wb.Worksheets)
            {
                var has = ws.Row(1).CellsUsed()
                    .Any(c => string.Equals(c.GetString(), Caption, StringComparison.OrdinalIgnoreCase));
                result.Add((ws.Name, has));
            }
        }
        return result;
    }

    [Test]
    public async Task ReportFatture_NonSospeseSheets_ShouldNotHaveRelNonFirmata()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        var request = new FatturaRicercaRequest { Anno = Anno, Mese = Mese, TipologiaFattura = new[] { Tipologia } };
        var reports = await request.ReportFatture(_seedHandler, AdminAuth());

        Assert.That(reports.Count, Is.GreaterThan(0), "Il seed 8001 (2026/2/SECONDO SALDO) deve generare il report emesse.");

        var nonSospese = InspectSheets(reports)
            .Where(s => s.Sheet.StartsWith("Regolari Esecuzioni", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.That(nonSospese.Count, Is.GreaterThan(0), "Il report emesse deve contenere il foglio 'Regolari Esecuzioni'.");

        Assert.That(nonSospese.All(s => !s.HasColumn), Is.True,
            "I fogli non-sospesi ('Regolari Esecuzioni') NON devono contenere 'Rel Non Firmata'.");
    }

    [Test]
    public async Task ReportFattureSospese_SospeseSheets_ShouldHaveRelNonFirmata()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        var request = new FatturaSospeseRicercaRequest { Anno = Anno, Mese = Mese, TipologiaFattura = new[] { Tipologia } };
        var reports = await request.ReportFattureSospese(_seedHandler, AdminAuth());

        Assert.That(reports.Count, Is.GreaterThan(0), "Il seed sospesi (7001/7002 su 2026/2/SECONDO SALDO) deve generare il report sospese.");

        var sospese = InspectSheets(reports)
            .Where(s => s.Sheet.StartsWith("Reg. Esec. Sospese", StringComparison.OrdinalIgnoreCase)
                     || s.Sheet.StartsWith("Enti Fatt. Sospese", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.That(sospese.Count, Is.GreaterThan(0), "Il report sospese deve contenere i fogli rel sospese.");

        Assert.That(sospese.All(s => s.HasColumn), Is.True,
            "I fogli rel sospesi devono contenere 'Rel Non Firmata'.");
    }

    // =================== Rami ANTICIPO / ACCONTO (e2e, solo connettività DB) ===================

    [TestCase("ANTICIPO")]
    [TestCase("ACCONTO")]
    public async Task ReportFatture_AnticipoAcconto_ShouldNotThrow(string tipologia)
    {
        TestDb.SkipIfUnavailable(_conf["PortaleFattureOptions:ConnectionString"]);
        var request = new FatturaRicercaRequest { Anno = Anno, Mese = Mese, TipologiaFattura = new[] { tipologia } };
        var reports = await request.ReportFatture(_handler, AdminAuth());
        Assert.That(reports, Is.Not.Null);
    }

    [TestCase("ANTICIPO")]
    [TestCase("ACCONTO")]
    public async Task ReportFattureSospese_AnticipoAcconto_ShouldNotThrow(string tipologia)
    {
        TestDb.SkipIfUnavailable(_conf["PortaleFattureOptions:ConnectionString"]);
        var request = new FatturaSospeseRicercaRequest { Anno = Anno, Mese = Mese, TipologiaFattura = new[] { tipologia } };
        var reports = await request.ReportFattureSospese(_handler, AdminAuth());
        Assert.That(reports, Is.Not.Null);
    }

    // =================== Più tipologie nello stesso request (e2e) ===================

    [Test]
    public async Task ReportFatture_MultipleTipologie_ShouldNotThrow()
    {
        TestDb.SkipIfUnavailable(_conf["PortaleFattureOptions:ConnectionString"]);
        var request = new FatturaRicercaRequest
        {
            Anno = Anno,
            Mese = Mese,
            TipologiaFattura = new[] { "SECONDO SALDO", "VAR. SEMESTRALE" }
        };
        var reports = await request.ReportFatture(_handler, AdminAuth());
        Assert.That(reports, Is.Not.Null);
    }

    [Test]
    public async Task ReportFattureSospese_MultipleTipologie_ShouldNotThrow()
    {
        TestDb.SkipIfUnavailable(_conf["PortaleFattureOptions:ConnectionString"]);
        var request = new FatturaSospeseRicercaRequest
        {
            Anno = Anno,
            Mese = Mese,
            TipologiaFattura = new[] { "SECONDO SALDO", "VAR. SEMESTRALE" }
        };
        var reports = await request.ReportFattureSospese(_handler, AdminAuth());
        Assert.That(reports, Is.Not.Null);
    }
}
