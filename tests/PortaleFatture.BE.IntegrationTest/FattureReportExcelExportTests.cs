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

        var bytes = fatture.ReportFattureRel(fattureSospese, relNonFirmate, null,Month, "PRIMO SALDO");

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        Assert.Multiple(() =>
        {
            Assert.That(HasColumn(wb, $"Regolari Esecuzioni {Month}", Caption), Is.False);
            Assert.That(HasColumn(wb, $"Enti Fatt. {Month}", Caption), Is.False);
            Assert.That(HasColumn(wb, $"Enti Fatt. {Month} Sospesi", Caption), Is.True);
        });
    }

    // -------------------------------------------------------------------------------------------
    // Sheet "Non Fatturate" (Posticipate + Eliminate) nel report di saldo. Requisito 24/07/2026.
    // -------------------------------------------------------------------------------------------

    // Lo sheet "Non Fatturate" viene aggiunto SOLO nel ramo else del loop (indice >= 2): serve quindi
    // che la lista 'fatture' abbia almeno 3 gruppi. I primi due sono i fogli Regolari/Enti, il terzo
    // e' il segnaposto che fa entrare nel ramo; i suoi dati non sono usati (conta solo gestioneFatture).
    private static List<IEnumerable<FattureRelExcelDto>> TreGruppi() =>
    [
        new List<FattureRelExcelDto> { NonSospesaRow() },
        new List<FattureRelExcelDto> { NonSospesaRow() },
        new List<FattureRelExcelDto> { NonSospesaRow() }
    ];

    private static GestioneFattureReportExcelDto NonFatturataRow(string stato, string? ragione = "Ente Test") => new()
    {
        IdEnte = "ENTE-1",
        RagioneSociale = ragione,
        IdContratto = "C1",
        TipologiaFattura = "PRIMO SALDO",
        NumeroFattura = 2099000002L,
        TipoDocumento = "TD01",
        Anno = 2026,
        Mese = 2,
        TotaleFatturaImponibile = 100m,
        TipoContratto = "PAC",
        Stato = stato
    };

    [Test]
    public void ReportFattureRel_ConGestioneFatture_AggiungeSheetNonFatturate_ConPosticipateEdEliminate()
    {
        var gestione = new List<GestioneFattureReportExcelDto>
        {
            NonFatturataRow("POSTICIPATA"),
            NonFatturataRow("ELIMINATA")
        };

        var bytes = TreGruppi().ReportFattureRel([], new List<RelNonFatturataDto>(), gestione, Month, "PRIMO SALDO");
        using var wb = new XLWorkbook(new MemoryStream(bytes));

        Assert.That(wb.TryGetWorksheet("Non Fatturate", out var ws), Is.True,
            $"Sheet 'Non Fatturate' assente. Fogli: {string.Join(" | ", wb.Worksheets.Select(w => w.Name))}");
        Assert.Multiple(() =>
        {
            Assert.That(HasColumn(wb, "Non Fatturate", "Stato"), Is.True, "colonna Stato attesa");
            Assert.That(HasColumn(wb, "Non Fatturate", "Id Ente"), Is.True);
            // due righe dati + intestazione
            Assert.That(ws.RangeUsed().RowCount(), Is.EqualTo(3));
            var statiInFoglio = ws.Column(ColIndex(ws, "Stato")).CellsUsed()
                .Skip(1).Select(c => c.GetString()).ToList();
            Assert.That(statiInFoglio, Is.EquivalentTo(new[] { "POSTICIPATA", "ELIMINATA" }),
                "Il foglio deve contenere sia le posticipate sia le eliminate (requisito 24/07).");
        });
    }

    [Test]
    public void ReportFattureRel_GestioneFattureNull_NonAggiungeSheetNonFatturate()
    {
        var bytes = TreGruppi().ReportFattureRel([], new List<RelNonFatturataDto>(), null, Month, "PRIMO SALDO");
        using var wb = new XLWorkbook(new MemoryStream(bytes));
        Assert.That(wb.TryGetWorksheet("Non Fatturate", out _), Is.False,
            "Con gestioneFatture null il foglio non deve comparire (nessuna NRE).");
    }

    [Test]
    public void ReportFattureRel_GestioneFattureVuoto_NonAggiungeSheetNonFatturate()
    {
        var bytes = TreGruppi().ReportFattureRel([], new List<RelNonFatturataDto>(),
            new List<GestioneFattureReportExcelDto>(), Month, "PRIMO SALDO");
        using var wb = new XLWorkbook(new MemoryStream(bytes));
        Assert.That(wb.TryGetWorksheet("Non Fatturate", out _), Is.False,
            "Con lista vuota il foglio non deve comparire.");
    }

    [Test]
    public void ReportFattureRel_MenoDiTreGruppi_ConGestionePiena_ProduceComunqueSheetNonFatturate()
    {
        // Dopo lo scioglimento dell'accoppiamento: il foglio "Non Fatturate" dipende SOLO dai dati
        // (gestioneFatture), non dal numero di gruppi in 'fatture'. Con 2 soli gruppi + dati, il foglio
        // deve comparire lo stesso. (Prima della fix questo caso NON produceva il foglio: era il difetto.)
        var dueGruppi = new List<IEnumerable<FattureRelExcelDto>>
        {
            new List<FattureRelExcelDto> { NonSospesaRow() },
            new List<FattureRelExcelDto> { NonSospesaRow() }
        };
        var gestione = new List<GestioneFattureReportExcelDto> { NonFatturataRow("POSTICIPATA") };

        var bytes = dueGruppi.ReportFattureRel([], new List<RelNonFatturataDto>(), gestione, Month, "PRIMO SALDO");
        using var wb = new XLWorkbook(new MemoryStream(bytes));
        Assert.That(wb.TryGetWorksheet("Non Fatturate", out _), Is.True,
            "Il foglio deve dipendere dai dati, non dal numero di gruppi: con dati presenti deve esserci "
          + "anche con meno di 3 gruppi.");
    }

    [Test]
    public void ReportFattureRel_RagioneSocialeConFormula_NonDiventaFormulaAttiva()
    {
        // Adversarial: RagioneSociale che inizia con '=' non deve essere scritta come FORMULA nel foglio
        // (rischio formula/CSV injection all'apertura del file). Caratterizza cosa fa lo strato Excel.
        var gestione = new List<GestioneFattureReportExcelDto>
        {
            NonFatturataRow("POSTICIPATA", ragione: "=1+1")
        };

        var bytes = TreGruppi().ReportFattureRel([], new List<RelNonFatturataDto>(), gestione, Month, "PRIMO SALDO");
        using var wb = new XLWorkbook(new MemoryStream(bytes));
        wb.TryGetWorksheet("Non Fatturate", out var ws);
        var cella = ws.Column(ColIndex(ws, "Ragione Sociale")).CellsUsed().Skip(1).First();

        Assert.That(cella.HasFormula, Is.False,
            "La ragione sociale '=1+1' NON deve essere una formula attiva: se HasFormula e' true, "
          + "c'e' un'esposizione a formula-injection da sanitizzare nello strato di export.");
        Assert.That(cella.GetString(), Does.Contain("1+1"));
    }

    private static int ColIndex(IXLWorksheet ws, string caption)
        => ws.Row(1).CellsUsed().First(c => string.Equals(c.GetString(), caption, StringComparison.OrdinalIgnoreCase)).Address.ColumnNumber;

    // ===========================================================================================
    // Copertura degli ALTRI fogli di ReportFattureRel (export puro, no DB): Regolari, PAC non
    // firmate, Enti Fatt., Enti Fatt. a Zero, Sospesi — con i rispettivi rami condizionali.
    // ===========================================================================================

    private static FattureRelExcelDto FatturaImporto(decimal importo)
    {
        var r = NonSospesaRow();
        r.TotaleFatturaImponibile = importo;
        return r;
    }

    private static RelNonFatturataDto RelNonFirmata(string tipoContratto, byte caricata) => new()
    {
        IdEnte = "ENTE-1",
        RagioneSociale = "Ente Test",
        IdContratto = "C1",
        TipologiaFattura = "PRIMO SALDO",
        Anno = 2026,
        Mese = 2,
        TipoContratto = tipoContratto,
        Caricata = caricata
    };

    // fatture con 2 gruppi: [0] -> Regolari, [1] -> Enti Fatt.
    private static List<IEnumerable<FattureRelExcelDto>> DueGruppi(IEnumerable<FattureRelExcelDto> enti) =>
    [
        new List<FattureRelExcelDto> { NonSospesaRow() },
        enti.ToList()
    ];

    [Test]
    public void ReportFattureRel_FoglioRegolariEsecuzioni_SemprePresente()
    {
        var bytes = DueGruppi([FatturaImporto(100m)])
            .ReportFattureRel([], new List<RelNonFatturataDto>(), null, Month, "PRIMO SALDO");
        using var wb = new XLWorkbook(new MemoryStream(bytes));
        Assert.That(wb.TryGetWorksheet($"Regolari Esecuzioni {Month}", out _), Is.True);
    }

    [Test]
    public void ReportFattureRel_PacNonFirmate_FoglioSempreCreato_ConSoloPacNonCaricate()
    {
        // Il foglio "Reg. Esec. PAC non firmate" viene aggiunto SEMPRE (il guard 'if !IsNullNotAny'
        // e' commentato nel codice): caratterizzazione. Il filtro deve tenere solo TipoContratto
        // contenente "PAC" con Caricata != 1 -> qui 1 sola riga su 3.
        var relNonFirmate = new List<RelNonFatturataDto>
        {
            RelNonFirmata("PAC", caricata: 0), // tenuta
            RelNonFirmata("PAC", caricata: 1), // esclusa (gia' firmata)
            RelNonFirmata("PAL", caricata: 0)  // esclusa (non PAC)
        };

        var bytes = DueGruppi([FatturaImporto(100m)])
            .ReportFattureRel([], relNonFirmate, null, Month, "PRIMO SALDO");
        using var wb = new XLWorkbook(new MemoryStream(bytes));

        Assert.That(wb.TryGetWorksheet("Reg. Esec. PAC non firmate", out var ws), Is.True,
            "Il foglio PAC non firmate e' creato sempre (guard commentato).");
        // FillOneTable: intestazione + righe dati. Una sola riga deve superare il filtro.
        Assert.That(ws.RangeUsed()!.RowCount(), Is.EqualTo(2),
            "Solo la PAC con Caricata=0 deve comparire: PAC gia' firmata e PAL escluse.");
    }

    [Test]
    public void ReportFattureRel_EntiFattAZero_PresenteSoloConFattureAZero()
    {
        // senza fatture a zero: il foglio "a Zero" NON deve esserci
        var soloNonZero = DueGruppi([FatturaImporto(100m)])
            .ReportFattureRel([], new List<RelNonFatturataDto>(), null, Month, "PRIMO SALDO");
        using (var wb = new XLWorkbook(new MemoryStream(soloNonZero)))
            Assert.That(wb.TryGetWorksheet($"Enti Fatt. a Zero {Month}", out _), Is.False,
                "Nessuna fattura a zero -> nessun foglio 'a Zero'.");

        // con una fattura a zero: il foglio compare
        var conZero = DueGruppi([FatturaImporto(100m), FatturaImporto(0m)])
            .ReportFattureRel([], new List<RelNonFatturataDto>(), null, Month, "PRIMO SALDO");
        using (var wb = new XLWorkbook(new MemoryStream(conZero)))
            Assert.That(wb.TryGetWorksheet($"Enti Fatt. a Zero {Month}", out _), Is.True,
                "Presenza di una fattura a zero -> foglio 'a Zero' creato.");
    }

    [Test]
    public void ReportFattureRel_ZeroVsNonZero_SeparateNeiDueFogli()
    {
        // una non-zero (100) e una zero (0): la prima nel foglio principale, la seconda nel 'a Zero'.
        var bytes = DueGruppi([FatturaImporto(100m), FatturaImporto(0m)])
            .ReportFattureRel([], new List<RelNonFatturataDto>(), null, Month, "PRIMO SALDO");
        using var wb = new XLWorkbook(new MemoryStream(bytes));

        wb.TryGetWorksheet($"Enti Fatt. {Month}", out var principale);
        wb.TryGetWorksheet($"Enti Fatt. a Zero {Month}", out var aZero);
        var colP = ColIndex(principale, "Totale Fattura Imponibile €");
        var colZ = ColIndex(aZero, "Totale Fattura Imponibile €");

        // il foglio principale non deve contenere lo zero, il foglio 'a Zero' non deve contenere il 100.
        var importiPrincipale = principale.Column(colP).CellsUsed().Skip(1)
            .Select(c => c.GetValue<double>()).Where(v => v != 0).ToList(); // esclude la riga totali eventuale
        Assert.That(importiPrincipale, Does.Not.Contain(0.0));
        Assert.That(aZero.Column(colZ).CellsUsed().Skip(1).Any(c => c.GetValue<double>() == 100.0), Is.False,
            "Il 100 non deve finire nel foglio 'a Zero'.");
    }

    [Test]
    public void ReportFattureRel_EntiSospesi_AssentiSenzaSospese()
    {
        var bytes = DueGruppi([FatturaImporto(100m)])
            .ReportFattureRel([], new List<RelNonFatturataDto>(), null, Month, "PRIMO SALDO");
        using var wb = new XLWorkbook(new MemoryStream(bytes));
        Assert.That(wb.TryGetWorksheet($"Enti Fatt. {Month} Sospesi", out _), Is.False,
            "Senza fattureSospese (2+ gruppi) il foglio Sospesi non deve comparire.");
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
