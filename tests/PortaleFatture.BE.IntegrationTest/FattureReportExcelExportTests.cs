using ClosedXML.Excel;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Integration test deterministici (NO DB) della generazione Excel dei report REL: verificano che la
/// colonna "Rel Non Firmata" compaia SOLO nei fogli sospesi (DTO -> FattureExtensions -> ExcelExtensions -> ClosedXML).
/// </summary>
public class FattureReportExcelExportTests
{
    private const string Caption = "Rel Non Firmata";
    private const string Month = "Febbraio";

    private static FattureRelExcelDto NonSospesaRow() => new()
    {
        IdEnte = "ENTE-1",
        RagioneSociale = "Ente Test",
        IdContratto = "C1",
        TipologiaFattura = "PRIMO SALDO",
        IdFattura = "1",
        Progressivo = "1",
        TipoDocumento = "TD01",
        Anno = 2026,
        Mese = 2,
        TotaleFatturaImponibile = 100m,
        TipologiaContratto = "AC"
    };

    private static FattureRelSospeseExcelDto SospesaRow(string relNonFirmata) => new()
    {
        IdEnte = "ENTE-1",
        RagioneSociale = "Ente Test",
        IdContratto = "C1",
        TipologiaFattura = "PRIMO SALDO",
        IdFattura = "1",
        Progressivo = "1",
        TipoDocumento = "TD01",
        Anno = 2026,
        Mese = 2,
        TotaleFatturaImponibile = 100m,
        TipologiaContratto = "AC",
        RelNonFirmata = relNonFirmata
    };

    private static bool HasColumn(XLWorkbook wb, string sheetName, string caption)
    {
        Assert.That(wb.TryGetWorksheet(sheetName, out var ws), Is.True,
            $"Foglio '{sheetName}' non trovato. Fogli: {string.Join(" | ", wb.Worksheets.Select(w => w.Name))}");
        return ws.Row(1).CellsUsed().Any(c => string.Equals(c.GetString(), caption, StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public void ReportFattureRel_RelNonFirmata_OnlyInSospesiSheet()
    {
        var fatture = new List<IEnumerable<FattureRelExcelDto>>
        {
            new List<FattureRelExcelDto> { NonSospesaRow() },
            new List<FattureRelExcelDto> { NonSospesaRow() }
        };
        var fattureSospese = new List<IEnumerable<FattureRelSospeseExcelDto>>
        {
            new List<FattureRelSospeseExcelDto> { SospesaRow("SI") },
            new List<FattureRelSospeseExcelDto> { SospesaRow("SI") }
        };
        var relNonFirmate = new List<RelNonFatturataDto>();

        var bytes = fatture.ReportFattureRel(fattureSospese, relNonFirmate, Month, "PRIMO SALDO");

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        Assert.Multiple(() =>
        {
            Assert.That(HasColumn(wb, $"Regolari Esecuzioni {Month}", Caption), Is.False);
            Assert.That(HasColumn(wb, $"Enti Fatt. {Month}", Caption), Is.False);
            Assert.That(HasColumn(wb, $"Enti Fatt. {Month} Sospesi", Caption), Is.True);
        });
    }

    [Test]
    public void ReportFattureSospeseRel_SospeseSheets_HaveRelNonFirmata()
    {
        var fatture = new List<IEnumerable<FattureRelSospeseExcelDto>>
        {
            new List<FattureRelSospeseExcelDto> { SospesaRow("SI") },
            new List<FattureRelSospeseExcelDto> { SospesaRow("SI") }
        };

        var bytes = fatture.ReportFattureSospeseRel(Month, "SECONDO SALDO");

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        Assert.Multiple(() =>
        {
            Assert.That(HasColumn(wb, $"Reg. Esec. Sospese {Month}", Caption), Is.True);
            Assert.That(HasColumn(wb, $"Enti Fatt. Sospese {Month}", Caption), Is.True);
        });
    }
}
