using System.Reflection;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Stile delle celle decimali negli export Excel "pagoPA" (<see cref="ReflectionExtensions.ToExcel"/>,
/// usato anche da APP IO). Regola: una colonna decimal è a 14 decimali (stile 9), tranne quando il
/// DTO chiede <see cref="XCellStyle.StandardNumberDecimal"/> (stile 3, '#,##0.00'): è il formato
/// valuta di ImportoTotale nei download dei documenti contabili APP IO.
/// Il formato cambia solo la visualizzazione: il valore scritto nella cella resta quello letto dal DB.
/// </summary>
[Category("AppIO")]
public class PagoPAExcelStileDecimaliTests
{
    private sealed class Riga
    {
        [HeaderPagoPA(caption: "Valuta", Order = 1, Style = XCellStyle.StandardNumberDecimal)]
        public decimal? Valuta { get; set; }

        [HeaderPagoPA(caption: "SenzaStile", Order = 2)]
        public decimal? SenzaStile { get; set; }

        [HeaderPagoPA(caption: "AltroStile", Order = 3, Style = XCellStyle.Percent)]
        public decimal? AltroStile { get; set; }

        [HeaderPagoPA(caption: "Intero", Order = 4, Style = XCellStyle.StandardNumberDecimal)]
        public long? Intero { get; set; }

        [HeaderPagoPA(caption: "Testo", Order = 5, Style = XCellStyle.StandardNumberDecimal)]
        public string? Testo { get; set; }
    }

    /// <summary>Celle della riga indicata (1 = intestazione) e formati di cella del foglio di stile.</summary>
    private static (List<Cell> Celle, List<CellFormat> Formati) Leggi(IEnumerable<Riga> righe, int riga = 2)
    {
        using var stream = righe.FillPagoPAOneSheet("Prova").ToExcel();
        using var doc = SpreadsheetDocument.Open(stream, false);
        var celle = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Descendants<Row>()
            .ElementAt(riga - 1).Elements<Cell>().ToList();
        var formati = doc.WorkbookPart.WorkbookStylesPart!.Stylesheet.CellFormats!.Elements<CellFormat>().ToList();
        return (celle, formati);
    }

    private static uint Stile(Cell cella) => cella.StyleIndex!.Value;

    /// <summary>
    /// Solo la colonna decimal che chiede StandardNumberDecimal esce a due decimali; senza stile, o
    /// con uno stile diverso, resta a 14.
    /// </summary>
    [Test]
    public void ToExcel_ColonneDecimal_ShouldUsareDueDecimaliSoloSeRichiesto()
    {
        var (celle, _) = Leggi([new Riga { Valuta = 25m, SenzaStile = 25m, AltroStile = 25m }]);

        Assert.Multiple(() =>
        {
            Assert.That(Stile(celle[0]), Is.EqualTo((uint)XCellStyle.StandardNumberDecimal), "Valuta");
            Assert.That(Stile(celle[1]), Is.EqualTo((uint)XCellStyle.Digit14), "SenzaStile");
            Assert.That(Stile(celle[2]), Is.EqualTo((uint)XCellStyle.Digit14), "AltroStile: solo StandardNumberDecimal cambia il default");
        });
    }

    /// <summary>
    /// Lo stile 3 del foglio di stile è davvero '#,##0.00' (formato predefinito 4 di Excel), e il 9 è
    /// un formato personalizzato: se qualcuno riordina i CellFormat, l'enum punta altrove.
    /// </summary>
    [Test]
    public void ToExcel_FoglioDiStile_ShouldAvereDueDecimaliAllIndice3()
    {
        var (_, formati) = Leggi([new Riga { Valuta = 1m }]);

        Assert.Multiple(() =>
        {
            Assert.That(formati[(int)XCellStyle.StandardNumberDecimal].NumberFormatId!.Value, Is.EqualTo(4u));
            Assert.That(formati[(int)XCellStyle.Digit14].NumberFormatId!.Value, Is.GreaterThanOrEqualTo(164u));
        });
    }

    /// <summary>
    /// Il formato non arrotonda il dato: nella cella c'è il valore intero, con tutti i suoi decimali.
    /// </summary>
    [TestCase("10.123456")]
    [TestCase("0.005")]
    [TestCase("-1234567.899999")]
    [TestCase("0")]
    public void ToExcel_Valuta_ShouldConservareIlValoreSenzaArrotondare(string valore)
    {
        var atteso = decimal.Parse(valore, System.Globalization.CultureInfo.InvariantCulture);

        var (celle, _) = Leggi([new Riga { Valuta = atteso }]);

        Assert.Multiple(() =>
        {
            Assert.That(celle[0].DataType!.Value, Is.EqualTo(CellValues.Number));
            Assert.That(decimal.Parse(celle[0].CellValue!.Text, System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo(atteso));
        });
    }

    /// <summary>
    /// Un importo assente non rompe l'export: cella vuota, in entrambe le colonne decimal.
    /// </summary>
    [Test]
    public void ToExcel_DecimalNull_ShouldScrivereUnaCellaVuota()
    {
        var (celle, _) = Leggi([new Riga()]);

        Assert.Multiple(() =>
        {
            Assert.That(celle, Has.Count.EqualTo(5));
            Assert.That(celle[0].CellValue!.Text, Is.Empty);
            Assert.That(celle[1].CellValue!.Text, Is.Empty);
        });
    }

    /// <summary>
    /// La regola riguarda solo i decimal: un intero resta intero (stile 5) e un testo resta testo,
    /// anche se l'attributo chiede StandardNumberDecimal.
    /// </summary>
    [Test]
    public void ToExcel_StileValutaSuNonDecimal_ShouldNonCambiareInteriETesti()
    {
        var (celle, _) = Leggi([new Riga { Intero = 7, Testo = "12.5" }]);

        Assert.Multiple(() =>
        {
            Assert.That(Stile(celle[3]), Is.EqualTo((uint)XCellStyle.StandardInteger));
            Assert.That(celle[4].DataType!.Value, Is.EqualTo(CellValues.String));
            Assert.That(celle[4].CellValue!.Text, Is.EqualTo("12.5"));
        });
    }

    /// <summary>
    /// Con più righe ogni cella della colonna ha lo stesso stile: la regola è per colonna, non per riga.
    /// </summary>
    [Test]
    public void ToExcel_PiuRighe_ShouldApplicareLoStilePerColonna()
    {
        Riga[] righe = [new() { Valuta = 1m, SenzaStile = 1m }, new() { Valuta = 2.5m, SenzaStile = 2.5m }];

        foreach (var riga in new[] { 2, 3 })
        {
            var (celle, _) = Leggi(righe, riga);
            Assert.Multiple(() =>
            {
                Assert.That(Stile(celle[0]), Is.EqualTo((uint)XCellStyle.StandardNumberDecimal), $"riga {riga}");
                Assert.That(Stile(celle[1]), Is.EqualTo((uint)XCellStyle.Digit14), $"riga {riga}");
            });
        }
    }

    /// <summary>
    /// Nei due export dei documenti contabili APP IO ImportoTotale chiede il formato valuta; Importo
    /// (importo di riga) no, e resta a 14 decimali.
    /// </summary>
    [TestCase(typeof(AppIoFinancialReportDettaglioRigaDto))]
    [TestCase(typeof(AppIoFinancialReportRigaDto))]
    public void DtoAppIo_ImportoTotale_ShouldChiedereIlFormatoValuta(Type dto)
    {
        XCellStyle StileDi(string proprieta) => dto.GetProperty(proprieta)!.GetCustomAttribute<HeaderPagoPAAttribute>()!.Style;

        Assert.Multiple(() =>
        {
            Assert.That(StileDi("ImportoTotale"), Is.EqualTo(XCellStyle.StandardNumberDecimal));
            Assert.That(StileDi("Importo"), Is.EqualTo(XCellStyle.None));
        });
    }
}
