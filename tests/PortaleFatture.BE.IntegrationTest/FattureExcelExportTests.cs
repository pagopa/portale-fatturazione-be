using ClosedXML.Excel;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Test deterministici (NO DB) della generazione Excel del report rel sospesi: verificano che la colonna
/// "Rel Non Firmata" venga esportata nel foglio "Enti Fatt. Sospese" con il valore proveniente dal DTO.
/// Non dipendono dai dati (UAT): costruiscono direttamente i FattureRelExcelDto e invocano l'export.
/// </summary>
public class FattureExcelExportTests
{
    private const string Caption = "Rel Non Firmata";
    private const string EntiFattSospeseSheetPrefix = "Enti Fatt. Sospese";

    private static FattureRelExcelDto SampleRow(string relNonFirmata) => new()
    {
        IdEnte = "ENTE-1",
        RagioneSociale = "Ente Test",
        IdContratto = "C1",
        TipologiaFattura = "SECONDO SALDO",
        IdFattura = "1",
        Progressivo = "1",
        TipoDocumento = "TD01",
        Anno = 2026,
        Mese = 2,
        TotaleFatturaImponibile = 100m, // !=0 per finire nel foglio "Enti Fatt. Sospese"
        TipologiaContratto = "AC",
        RelNonFirmata = relNonFirmata
    };

    [Test]
    public void ReportFattureSospeseRel_ShouldExport_RelNonFirmataColumn_WithValue()
    {
        // bucket 0 -> "Reg. Esec. Sospese", bucket 1 -> "Enti Fatt. Sospese"
        var data = new List<IEnumerable<FattureRelExcelDto>>
        {
            new List<FattureRelExcelDto> { SampleRow("SI") },
            new List<FattureRelExcelDto> { SampleRow("SI") }
        };

        var bytes = data.ReportFattureSospeseRel("Febbraio", "SECONDO SALDO");

        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);

        var ws = wb.Worksheets.FirstOrDefault(w =>
            w.Name.StartsWith(EntiFattSospeseSheetPrefix, StringComparison.OrdinalIgnoreCase));
        Assert.That(ws, Is.Not.Null, $"Foglio '{EntiFattSospeseSheetPrefix}' non generato.");

        var headerCell = ws!.Row(1).CellsUsed()
            .FirstOrDefault(c => string.Equals(c.GetString(), Caption, StringComparison.OrdinalIgnoreCase));
        Assert.That(headerCell, Is.Not.Null, $"Colonna '{Caption}' assente nell'header.");

        // prima riga dati = riga 2; valore atteso = quello del DTO
        var col = headerCell!.Address.ColumnNumber;
        Assert.That(ws.Cell(2, col).GetString(), Is.EqualTo("SI"));
    }

    [Test]
    public void ReportFattureSospeseRel_RelNonFirmata_EmptyValue_ShouldStillHaveColumn()
    {
        var data = new List<IEnumerable<FattureRelExcelDto>>
        {
            new List<FattureRelExcelDto> { SampleRow(string.Empty) },
            new List<FattureRelExcelDto> { SampleRow(string.Empty) }
        };

        var bytes = data.ReportFattureSospeseRel("Febbraio", "SECONDO SALDO");

        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First(w =>
            w.Name.StartsWith(EntiFattSospeseSheetPrefix, StringComparison.OrdinalIgnoreCase));

        var hasColumn = ws.Row(1).CellsUsed()
            .Any(c => string.Equals(c.GetString(), Caption, StringComparison.OrdinalIgnoreCase));
        Assert.That(hasColumn, Is.True, $"La colonna '{Caption}' deve essere presente anche con valore vuoto.");
    }
}
