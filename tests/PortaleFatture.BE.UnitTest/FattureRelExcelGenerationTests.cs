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

    [TestCase(false)]
    [TestCase(true)]
    public void EmptyLegacyNotesSlot_PreservesWorkbookContentAndTotals(bool combinedReport)
    {
        var rows = new[] { SospesaRow("SI", 100m), SospesaRow("NO", 50m) };
        var sospese = new List<IEnumerable<FattureRelSospeseExcelDto>> { rows, rows };
        var regular = new List<IEnumerable<FattureRelExcelDto>>
        {
            new[] { NonSospesaRow() }, new[] { NonSospesaRow() }
        };
        byte[] Report() => combinedReport
            ? regular.ReportFattureRel(sospese, Array.Empty<RelNonFatturataDto>(), null, Month, "PRIMO SALDO")
            : sospese.ReportFattureSospeseRel(Month, "SECONDO SALDO");
        using var before = new XLWorkbook(new MemoryStream(Report()));
        sospese.Add(Array.Empty<FattureRelSospeseExcelDto>());
        using var after = new XLWorkbook(new MemoryStream(Report()));

        Assert.That(after.Worksheets.Select(x => x.Name), Is.EqualTo(before.Worksheets.Select(x => x.Name)));
        foreach (var sheet in after.Worksheets)
            Assert.That(sheet.RowsUsed().Select(r => r.CellsUsed().Select(c => c.GetString()).ToArray()).ToArray(),
                Is.EqualTo(before.Worksheet(sheet.Name).RowsUsed()
                    .Select(r => r.CellsUsed().Select(c => c.GetString()).ToArray()).ToArray()), sheet.Name);

        var sospeseSheet = after.Worksheet(combinedReport ? $"Enti Fatt. {Month} Sospesi" : $"Enti Fatt. Sospese {Month}");
        var amountColumn = sospeseSheet.Row(1).CellsUsed()
            .Single(c => c.GetString() == "Totale Fattura Imponibile \u20ac").Address.ColumnNumber;
        var relColumn = sospeseSheet.Row(1).CellsUsed()
            .Single(c => c.GetString() == Caption).Address.ColumnNumber;
        var totalsRow = sospeseSheet.RowsUsed().Single(r => r.Cell(1).GetString() == "Totali:").RowNumber();
        Assert.Multiple(() =>
        {
            Assert.That(sospeseSheet.Cell(2, amountColumn).GetValue<decimal>(), Is.EqualTo(100m));
            Assert.That(sospeseSheet.Cell(3, amountColumn).GetValue<decimal>(), Is.EqualTo(50m));
            Assert.That(sospeseSheet.Cell(totalsRow, amountColumn).GetValue<decimal>(), Is.EqualTo(150m));
            Assert.That(sospeseSheet.Cell(2, relColumn).GetString(), Is.EqualTo("SI"));
            Assert.That(sospeseSheet.Cell(3, relColumn).GetString(), Is.EqualTo("NO"));
        });
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

        var bytes = fatture.ReportFattureRel(fattureSospese, relNonFirmate, null, Month, "PRIMO SALDO");

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
        var relBytes = fatture.ReportFattureRel(fattureSospese, new List<RelNonFatturataDto>(), null, Month, "PRIMO SALDO");
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
