using ClosedXML.Excel;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// PF-959: una REL PAC non fatturata arriva nella prima lista del report come riga senza fattura
/// (IdFattura, Progressivo, TipoDocumento vuoti, TotaleFatturaImponibile = 0) e con le colonne Rel*
/// valorizzate. Qui si verifica come la scrive `ReportFattureRel` nello sheet "Regolari Esecuzioni",
/// e che non tocchi gli altri sheet. In memoria, senza DB.
/// </summary>
public class FattureRelExcelPacNonFatturateReportTests
{
    private const string Month = "Agosto";
    private const string Sheet = "Regolari Esecuzioni Agosto";

    private static FattureRelExcelDto Emessa() => new()
    {
        IdEnte = "ENTE-EMESSA",
        RagioneSociale = "Ente con fattura",
        IdContratto = "C1",
        TipologiaFattura = "PRIMO SALDO",
        IdFattura = "10",
        Progressivo = "10",
        TipoDocumento = "TD01",
        Anno = 2026,
        Mese = 8,
        RelTotale = 500m,
        RelTotaleIvato = 610m,
        Caricata = 1,
        TotaleFatturaImponibile = 500m,
        TipologiaContratto = "PAC"
    };

    private static FattureRelExcelDto PacNonFatturata(byte caricata = 0) => new()
    {
        IdEnte = "ENTE-PAC",
        RagioneSociale = "Ente PAC non firmato",
        IdContratto = "C2",
        TipologiaFattura = "PRIMO SALDO",
        Anno = 2026,
        Mese = 8,
        RelTotaleNotificheAnalogiche = 3,
        RelTotaleNotificheDigitali = 7,
        RelTotaleNotifiche = 10,
        RelTotaleAnalogico = 30m,
        RelTotaleDigitale = 70m,
        RelTotale = 100m,
        RelTotaleIvato = 122m,
        Caricata = caricata,
        TotaleFatturaImponibile = 0m,
        TipologiaContratto = "PAC"
    };

    private static XLWorkbook Report(IEnumerable<FattureRelExcelDto> primaLista, IEnumerable<FattureRelExcelDto>? entiFatt = null)
    {
        var fatture = new List<IEnumerable<FattureRelExcelDto>>
        {
            primaLista,
            entiFatt ?? new[] { Emessa() },
            Array.Empty<FattureRelExcelDto>()
        };
        var bytes = fatture.ReportFattureRel(null, Array.Empty<RelNonFatturataDto>(), null, Month, "PRIMO SALDO");
        return new XLWorkbook(new MemoryStream(bytes));
    }

    private static int Col(IXLWorksheet ws, string caption) =>
        ws.Row(1).CellsUsed().Single(c => c.GetString() == caption).Address.ColumnNumber;

    private static IXLRow RigaEnte(IXLWorksheet ws, string idEnte) =>
        ws.RowsUsed().Single(r => r.Cell(Col(ws, "IdEnte")).GetString() == idEnte);

    [TestCase((byte)0, "Non Caricata")]
    [TestCase((byte)1, "Firmata")]
    public void RelPacNonFatturata_CompareNelloSheet_SenzaFattura(byte caricata, string firmata)
    {
        using var wb = Report(new[] { Emessa(), PacNonFatturata(caricata) });
        var ws = wb.Worksheet(Sheet);
        var riga = RigaEnte(ws, "ENTE-PAC");

        Assert.Multiple(() =>
        {
            Assert.That(riga.Cell(Col(ws, "N. Fattura")).GetString(), Is.Empty);
            Assert.That(riga.Cell(Col(ws, "TipoDocumento")).GetString(), Is.Empty);
            Assert.That(riga.Cell(Col(ws, "Firmata")).GetString(), Is.EqualTo(firmata));
            Assert.That(riga.Cell(Col(ws, "N. Totale Notifiche")).GetValue<int>(), Is.EqualTo(10));
            Assert.That(riga.Cell(Col(ws, "Totale Imponibile Analogico €")).GetValue<decimal>(), Is.EqualTo(30m));
            Assert.That(riga.Cell(Col(ws, "Totale Imponibile Digitale €")).GetValue<decimal>(), Is.EqualTo(70m));
            Assert.That(riga.Cell(Col(ws, "Totale Imponibile €")).GetValue<decimal>(), Is.EqualTo(100m));
            Assert.That(riga.Cell(Col(ws, "Totale Fattura Imponibile €")).GetValue<decimal>(), Is.EqualTo(0m));
        });
    }

    /// <summary>La riga dei totali somma la REL non fatturata, ma non ne altera il totale fatturato.</summary>
    [Test]
    public void RigaTotali_SommaLaRelNonFatturata_SoloNeiTotaliRel()
    {
        using var wb = Report(new[] { Emessa(), PacNonFatturata() });
        var ws = wb.Worksheet(Sheet);
        var totali = ws.RowsUsed().Single(r => r.Cell(1).GetString() == "Totali:");

        Assert.Multiple(() =>
        {
            Assert.That(totali.Cell(Col(ws, "Totale Imponibile €")).GetValue<decimal>(), Is.EqualTo(600m));
            Assert.That(totali.Cell(Col(ws, "Totale Ivato €")).GetValue<decimal>(), Is.EqualTo(732m));
            Assert.That(totali.Cell(Col(ws, "Totale Fattura Imponibile €")).GetValue<decimal>(), Is.EqualTo(500m));
        });
    }

    /// <summary>Regressione: con o senza la REL non fatturata, gli altri sheet e la riga emessa restano identici.</summary>
    [Test]
    public void AltriSheet_ERigaEmessa_NonCambiano()
    {
        using var prima = Report(new[] { Emessa() });
        using var dopo = Report(new[] { Emessa(), PacNonFatturata() });

        Assert.That(dopo.Worksheets.Select(w => w.Name), Is.EqualTo(prima.Worksheets.Select(w => w.Name)));
        foreach (var ws in dopo.Worksheets.Where(w => w.Name != Sheet))
            Assert.That(Celle(ws), Is.EqualTo(Celle(prima.Worksheet(ws.Name))), ws.Name);

        var wsPrima = prima.Worksheet(Sheet);
        var wsDopo = dopo.Worksheet(Sheet);
        Assert.That(Valori(RigaEnte(wsDopo, "ENTE-EMESSA")), Is.EqualTo(Valori(RigaEnte(wsPrima, "ENTE-EMESSA"))));
        Assert.That(wsDopo.Row(1).CellsUsed().Select(c => c.GetString()),
            Is.EqualTo(wsPrima.Row(1).CellsUsed().Select(c => c.GetString())), "intestazioni e ordine delle colonne");
    }

    /// <summary>
    /// Una REL non fatturata ha TotaleFatturaImponibile = 0, ma resta nello sheet "Regolari Esecuzioni":
    /// il ramo che separa le fatture a zero esiste solo per la seconda lista ("Enti Fatt.").
    /// </summary>
    [Test]
    public void RelNonFatturata_NonFinisceNelloSheetDelleFattureAZero()
    {
        using var wb = Report(new[] { PacNonFatturata() });

        Assert.That(wb.Worksheets.Any(w => w.Name.StartsWith("Enti Fatt. a Zero")), Is.False);
        Assert.That(RigaEnte(wb.Worksheet(Sheet), "ENTE-PAC"), Is.Not.Null);
    }

    private static string[][] Celle(IXLWorksheet ws) =>
        ws.RowsUsed().Select(Valori).ToArray();

    private static string[] Valori(IXLRow r) => r.CellsUsed().Select(c => c.GetString()).ToArray();
}
