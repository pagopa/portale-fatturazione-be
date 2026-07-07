using ClosedXML.Excel;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Unit test della generazione Excel (in-memory, nessun DB): verificano che la colonna "Rel Non Firmata"
/// compaia SOLO nei fogli sospesi. Costruiscono i DTO a mano e invocano direttamente i metodi di report.
/// </summary>
public class FattureRelExcelGenerationTests
{
    private const string Caption = "Rel Non Firmata";
    private const string Month = "Febbraio";

    private static FattureRelExcelDto NonSospesaRow(decimal amount = 100m) => new()
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
        TotaleFatturaImponibile = amount,
        TipologiaContratto = "AC"
    };

    private static FattureRelSospeseExcelDto SospesaRow(string relNonFirmata, decimal amount = 100m) => new()
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
        TotaleFatturaImponibile = amount,
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
            new List<FattureRelExcelDto> { NonSospesaRow() }, // -> "Regolari Esecuzioni {m}"
            new List<FattureRelExcelDto> { NonSospesaRow() }  // -> "Enti Fatt. {m}"
        };
        var fattureSospese = new List<IEnumerable<FattureRelSospeseExcelDto>>
        {
            new List<FattureRelSospeseExcelDto> { SospesaRow("SI") },
            new List<FattureRelSospeseExcelDto> { SospesaRow("SI") } // -> "Enti Fatt. {m} Sospesi"
        };
        var relNonFirmate = new List<RelNonFatturataDto>(); // vuoto: evita NRE nel foglio PAC non firmate

        var bytes = fatture.ReportFattureRel(fattureSospese, relNonFirmate, Month, "PRIMO SALDO");

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        Assert.Multiple(() =>
        {
            Assert.That(HasColumn(wb, $"Regolari Esecuzioni {Month}", Caption), Is.False,
                "Il foglio non-sospeso 'Regolari Esecuzioni' NON deve contenere la colonna.");
            Assert.That(HasColumn(wb, $"Enti Fatt. {Month}", Caption), Is.False,
                "Il foglio non-sospeso 'Enti Fatt.' NON deve contenere la colonna.");
            Assert.That(HasColumn(wb, $"Enti Fatt. {Month} Sospesi", Caption), Is.True,
                "Il foglio 'Enti Fatt. Sospesi' DEVE contenere la colonna.");
        });
    }

    [Test]
    public void ReportFattureSospeseRel_SospeseSheets_HaveRelNonFirmata()
    {
        var fatture = new List<IEnumerable<FattureRelSospeseExcelDto>>
        {
            new List<FattureRelSospeseExcelDto> { SospesaRow("SI") }, // -> "Reg. Esec. Sospese {m}"
            new List<FattureRelSospeseExcelDto> { SospesaRow("SI") }  // -> "Enti Fatt. Sospese {m}"
        };

        var bytes = fatture.ReportFattureSospeseRel(Month, "SECONDO SALDO");

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        Assert.Multiple(() =>
        {
            Assert.That(HasColumn(wb, $"Reg. Esec. Sospese {Month}", Caption), Is.True);
            Assert.That(HasColumn(wb, $"Enti Fatt. Sospese {Month}", Caption), Is.True);
        });
    }

    /// <summary>
    /// La colonna non solo esiste ma è valorizzata dal DTO ed è l'ULTIMA colonna del foglio sospeso.
    /// </summary>
    [Test]
    public void ReportFattureSospeseRel_RelNonFirmata_IsLastColumn_AndValueMapped()
    {
        var fatture = new List<IEnumerable<FattureRelSospeseExcelDto>>
        {
            new List<FattureRelSospeseExcelDto> { SospesaRow("SI") },
            new List<FattureRelSospeseExcelDto> { SospesaRow("SI") }
        };

        var bytes = fatture.ReportFattureSospeseRel(Month, "SECONDO SALDO");

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        Assert.That(wb.TryGetWorksheet($"Enti Fatt. Sospese {Month}", out var ws), Is.True);

        var headerCells = ws.Row(1).CellsUsed().ToList();
        var relCell = headerCells.FirstOrDefault(c => string.Equals(c.GetString(), Caption, StringComparison.OrdinalIgnoreCase));
        Assert.That(relCell, Is.Not.Null, "Colonna 'Rel Non Firmata' assente.");

        var col = relCell!.Address.ColumnNumber;
        Assert.Multiple(() =>
        {
            Assert.That(col, Is.EqualTo(headerCells.Max(c => c.Address.ColumnNumber)),
                "'Rel Non Firmata' deve essere l'ultima colonna.");
            Assert.That(ws.Cell(2, col).GetString(), Is.EqualTo("SI"),
                "Il valore in cella deve corrispondere a quello del DTO.");
        });
    }

    /// <summary>
    /// Fogli "a Zero": nel non-sospeso ("Enti Fatt. a Zero") NESSUNA colonna; nel sospeso
    /// ("Fatt. a Zero Sosp.") la colonna DEVE esserci.
    /// </summary>
    [Test]
    public void ZeroSheets_ColumnPresence_FollowsSospesoFlag()
    {
        // ReportFattureRel: bucket non-sospeso con riga a zero -> "Enti Fatt. a Zero {m}"
        var fatture = new List<IEnumerable<FattureRelExcelDto>>
        {
            new List<FattureRelExcelDto> { NonSospesaRow() },
            new List<FattureRelExcelDto> { NonSospesaRow(), NonSospesaRow(amount: 0m) }
        };
        var fattureSospese = new List<IEnumerable<FattureRelSospeseExcelDto>>
        {
            new List<FattureRelSospeseExcelDto> { SospesaRow("SI") },
            new List<FattureRelSospeseExcelDto> { SospesaRow("SI") }
        };
        var relBytes = fatture.ReportFattureRel(fattureSospese, new List<RelNonFatturataDto>(), Month, "PRIMO SALDO");
        using var relWb = new XLWorkbook(new MemoryStream(relBytes));
        Assert.That(HasColumn(relWb, $"Enti Fatt. a Zero {Month}", Caption), Is.False,
            "Il foglio non-sospeso 'Enti Fatt. a Zero' NON deve contenere la colonna.");

        // ReportFattureSospeseRel: bucket sospeso con riga a zero -> "Fatt. a Zero Sosp. {m}"
        var sospese = new List<IEnumerable<FattureRelSospeseExcelDto>>
        {
            new List<FattureRelSospeseExcelDto> { SospesaRow("SI") },
            new List<FattureRelSospeseExcelDto> { SospesaRow("SI"), SospesaRow("NO", amount: 0m) }
        };
        var sospeseBytes = sospese.ReportFattureSospeseRel(Month, "SECONDO SALDO");
        using var sospeseWb = new XLWorkbook(new MemoryStream(sospeseBytes));
        Assert.That(HasColumn(sospeseWb, $"Fatt. a Zero Sosp. {Month}", Caption), Is.True,
            "Il foglio sospeso 'Fatt. a Zero Sosp.' DEVE contenere la colonna.");
    }
}
