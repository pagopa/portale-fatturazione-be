using ClosedXML.Excel;
using MediatR;
using Microsoft.Extensions.Configuration;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Integration test end-to-end (extension ReportFatture / ReportFattureSospese con IMediator reale -> DB):
/// verificano l'invariante colonna sui report generati. Data-dependent: Assume.That -> Inconclusive senza dati (UAT).
/// TipologiaFattura sempre esplicita (il fix NRE sull'auto-popolamento non è su questo branch).
/// </summary>
public class FattureReportEndpointE2ETests
{
    private const string Caption = "Rel Non Firmata";
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
        var request = new FatturaRicercaRequest { Anno = Anno, Mese = Mese, TipologiaFattura = new[] { Tipologia } };
        var reports = await request.ReportFatture(_handler, AdminAuth());

        Assume.That(reports.Count, Is.GreaterThan(0), "Nessun report per il periodo: eseguire in UAT con dati.");

        var nonSospese = InspectSheets(reports)
            .Where(s => s.Sheet.StartsWith("Regolari Esecuzioni", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assume.That(nonSospese.Count, Is.GreaterThan(0), "Nessun foglio 'Regolari Esecuzioni': eseguire in UAT con dati.");

        Assert.That(nonSospese.All(s => !s.HasColumn), Is.True,
            "I fogli non-sospesi ('Regolari Esecuzioni') NON devono contenere 'Rel Non Firmata'.");
    }

    [Test]
    public async Task ReportFattureSospese_SospeseSheets_ShouldHaveRelNonFirmata()
    {
        var request = new FatturaSospeseRicercaRequest { Anno = Anno, Mese = Mese, TipologiaFattura = new[] { Tipologia } };
        var reports = await request.ReportFattureSospese(_handler, AdminAuth());

        Assume.That(reports.Count, Is.GreaterThan(0), "Nessun report per il periodo: eseguire in UAT con dati.");

        var sospese = InspectSheets(reports)
            .Where(s => s.Sheet.StartsWith("Reg. Esec. Sospese", StringComparison.OrdinalIgnoreCase)
                     || s.Sheet.StartsWith("Enti Fatt. Sospese", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assume.That(sospese.Count, Is.GreaterThan(0), "Nessun foglio rel sospese: eseguire in UAT con dati.");

        Assert.That(sospese.All(s => s.HasColumn), Is.True,
            "I fogli rel sospesi devono contenere 'Rel Non Firmata'.");
    }
}
