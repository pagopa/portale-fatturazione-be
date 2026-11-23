using System.Data;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using MediatR;
using Moq;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// PF-753 — casi avversari e invarianti sui report Excel (nessun DB, IMediator mockato). Completa
/// <see cref="ReportMiglioramentiPf753Tests"/>, che copre i casi di funzionamento, con:
/// <list type="bullet">
/// <item>bordi degli helper del writer (<c>ColumnLetter</c> oltre la Z, <c>DisplayLength</c> su ogni tipo);</item>
/// <item>un invariante su <b>ogni</b> formula SUBTOTAL di <b>ogni</b> report: colonna giusta, intervallo giusto,
/// valore in cache uguale alla somma delle celle;</item>
/// <item>dati ostili nel filtro delle eliminate (stato nullo, periodo nullo, stati sconosciuti, tipologia con
/// maiuscole diverse, risposta nulla della query);</item>
/// <item>testi che sembrano formule (formula injection) nello sheet "Non Fatturate";</item>
/// <item>nomi sheet della legenda entro i limiti di Excel per tutti i dodici mesi;</item>
/// <item>bordi del ridimensionamento automatico (colonne vuote, testi enormi, CRLF, opt-in).</item>
/// </list>
/// </summary>
[Category("Fatture")]
public class ReportMiglioramentiPf753AdversarialTests
{
    private const string Mese = "FEBBRAIO";

    private static AuthenticationInfo AdminAuth() => new()
    {
        IdEnte = Guid.NewGuid().ToString(),
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };

    // ================================================================== helper del writer

    [TestCase(0, "A")]
    [TestCase(25, "Z")]
    [TestCase(26, "AA")]
    [TestCase(51, "AZ")]
    [TestCase(52, "BA")]
    [TestCase(701, "ZZ")]
    [TestCase(702, "AAA")]
    [TestCase(16383, "XFD")]   // ultima colonna di Excel
    public void ColumnLetter_NumerazioneBiiettivaBase26(int indice, string atteso) =>
        Assert.That(ExcelExtensions.ColumnLetter(indice), Is.EqualTo(atteso),
            "Un errore qui (es. 26 → 'A@' o 'BA') farebbe puntare il SUBTOTAL alla colonna sbagliata senza errori visibili.");

    public static IEnumerable<TestCaseData> Lunghezze()
    {
        yield return new TestCaseData(null, 0).SetName("DisplayLength_Null");
        yield return new TestCaseData(DBNull.Value, 0).SetName("DisplayLength_DBNull");
        yield return new TestCaseData("", 0).SetName("DisplayLength_StringaVuota");
        yield return new TestCaseData("abc", 3).SetName("DisplayLength_Stringa");
        yield return new TestCaseData("a\nbbbb\ncc", 4).SetName("DisplayLength_Multiriga_RigaPiuLunga");
        yield return new TestCaseData("a\r\nbbbb", 4).SetName("DisplayLength_Crlf_IlCrNonConta");
        yield return new TestCaseData("\n\n", 0).SetName("DisplayLength_SoloACapo");
        yield return new TestCaseData(1234567.89m, "1,234,567.89".Length + 2).SetName("DisplayLength_Decimale_ConMigliaia");
        yield return new TestCaseData(-5m, "-5.00".Length + 2).SetName("DisplayLength_DecimaleNegativo");
        yield return new TestCaseData(new DateTime(2026, 11, 20, 10, 0, 0), 19).SetName("DisplayLength_Data");
        yield return new TestCaseData(42, 2).SetName("DisplayLength_Intero");
        yield return new TestCaseData(1234567890123L, 13).SetName("DisplayLength_Long");
    }

    [TestCaseSource(nameof(Lunghezze))]
    public void DisplayLength_StimaComeLaCellaVieneMostrata(object? valore, int atteso) =>
        Assert.That(ExcelExtensions.DisplayLength(valore), Is.EqualTo(atteso));

    // ================================================================== invariante SUBTOTAL su ogni report

    private static readonly Regex Subtotal = new(@"^SUBTOTAL\(9,([A-Z]+)2:([A-Z]+)(\d+)\)$", RegexOptions.Compiled);

    /// <summary>
    /// Per ogni foglio con la riga "Totali:" e per ogni formula: la lettera dell'intervallo è quella della
    /// colonna della cella, l'intervallo parte dalla riga 2 e finisce due righe sopra i totali (c'è la riga
    /// vuota), e il valore in cache è la somma delle celle numeriche dell'intervallo. Così un errore di
    /// indice o di intervallo non può passare inosservato su nessun report.
    /// </summary>
    [TestCaseSource(typeof(ReportMiglioramentiPf753Tests), nameof(ReportMiglioramentiPf753Tests.ReportConLegenda))]
    public void Subtotal_OgniFormula_ColonnaIntervalloECacheCoerenti(Func<byte[]> genera, IReadOnlyList<ReportLegende.Voce> _, string __)
    {
        using var wb = new XLWorkbook(new MemoryStream(genera()));
        var formuleViste = 0;

        foreach (var ws in wb.Worksheets)
        {
            var rigaTotali = ws.RowsUsed().FirstOrDefault(r => r.Cell(1).GetString() == "Totali:");
            if (rigaTotali is null) continue;
            var nRiga = rigaTotali.RowNumber();

            foreach (var cella in rigaTotali.CellsUsed().Where(c => c.HasFormula))
            {
                formuleViste++;
                var m = Subtotal.Match(cella.FormulaA1);
                var lettera = cella.Address.ColumnLetter;
                var somma = Enumerable.Range(2, nRiga - 3)
                    .Select(r => ws.Cell(r, cella.Address.ColumnNumber))
                    .Where(c => c.DataType == XLDataType.Number)
                    .Sum(c => c.GetValue<decimal>());

                Assert.Multiple(() =>
                {
                    Assert.That(m.Success, Is.True, $"{ws.Name}!{cella.Address}: formula inattesa '{cella.FormulaA1}'.");
                    Assert.That(cella.Address.ColumnNumber, Is.GreaterThan(1), "La prima colonna è l'etichetta, mai una formula.");
                    Assert.That(m.Groups[1].Value, Is.EqualTo(lettera), $"{ws.Name}: intervallo su un'altra colonna.");
                    Assert.That(m.Groups[2].Value, Is.EqualTo(lettera));
                    Assert.That(int.Parse(m.Groups[3].Value), Is.EqualTo(nRiga - 2),
                        $"{ws.Name}: l'intervallo deve fermarsi all'ultima riga dati (riga totali {nRiga}, riga vuota {nRiga - 1}).");
                    Assert.That(Convert.ToDecimal(cella.CachedValue.GetNumber()), Is.EqualTo(somma),
                        $"{ws.Name}!{cella.Address}: il valore in cache deve essere la somma delle righe dati.");
                });
            }
        }

        Assert.That(formuleViste, Is.GreaterThan(0), "Il report deve avere almeno una formula: altrimenti il test passerebbe sul vuoto.");
    }

    [Test]
    public void Subtotal_ValoriNegativiEDecimali_CacheEsatta()
    {
        var acconto = new List<IEnumerable<FattureAccontoExcelDto>>
        {
            new List<FattureAccontoExcelDto>
            {
                new() { Mese = 2, TotaleFattura = -100.55m, TotaleNotificheDigitali = 1 },
                new() { Mese = 2, TotaleFattura = 0.01m, TotaleNotificheDigitali = 2 },
                new() { Mese = 2, TotaleFattura = 250.50m, TotaleNotificheDigitali = 3 },
            }
        };
        using var wb = new XLWorkbook(new MemoryStream(acconto.ReportFattureAcconto(Mese)));
        var ws = wb.Worksheet($"Acconto {Mese}");
        var formule = ws.RowsUsed().Single(r => r.Cell(1).GetString() == "Totali:").CellsUsed().Where(c => c.HasFormula).ToList();

        Assert.That(formule.Select(c => Convert.ToDecimal(c.CachedValue.GetNumber())), Does.Contain(149.96m),
            "Un totale con negativi e centesimi deve restare esatto (nessun arrotondamento nel passaggio a formula).");
    }

    [Test]
    public void Subtotal_TabellaNonMarcata_NessunaFormula()
    {
        // FillOneTable (es. "Non Fatturate") non ha la riga dei totali: anche con colonne decimali non
        // deve comparire alcuna formula.
        var ds = new DataSet();
        ds.Tables.Add(new[] { new GestioneFattureReportExcelDto { Stato = "POSTICIPATA", TotaleImponibile = 10m } }.FillOneTable("Non Fatturate"));
        using var wb = new XLWorkbook(ds.ToExcel());

        Assert.That(wb.Worksheet("Non Fatturate").CellsUsed().Any(c => c.HasFormula), Is.False);
    }

    [Test]
    public void Subtotal_ToExcelData_NonCambia()
    {
        // La marcatura della tabella è letta solo da ToExcel: ToExcelData, usato da altri export, resta com'era.
        var ds = new DataSet();
        ds.Tables.Add(new[] { new FattureAccontoExcelDto { Mese = 2, TotaleFattura = 5m } }.FillTableWithTotalsRel(0, "T"));
        using var wb = new XLWorkbook(ds.ToExcelData());

        Assert.That(wb.Worksheets.SelectMany(w => w.CellsUsed()).Any(c => c.HasFormula), Is.False);
    }

    // ================================================================== filtro eliminate: dati ostili

    private static FattureRelExcelDto Rel() => new()
    {
        IdEnte = "ENTE-1", RagioneSociale = "Ente", IdContratto = "C1", TipologiaFattura = "SECONDO SALDO",
        IdFattura = "1", Progressivo = "1", TipoDocumento = "TD01", Anno = 2026, Mese = 2,
        TotaleFatturaImponibile = 10m, TipologiaContratto = "PAC"
    };

    private static Mock<IMediator> Mediator(IEnumerable<GestioneFattureReportDto>? gestione)
    {
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<FattureRelExcelQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<IEnumerable<FattureRelExcelDto>>?)[new List<FattureRelExcelDto> { Rel() }]);
        m.Setup(x => x.Send(It.IsAny<RelNonFatturateQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<RelNonFatturataDto>?)new List<RelNonFatturataDto>());
        m.Setup(x => x.Send(It.IsAny<GestioneFattureReportQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<GestioneFattureReportDto>?)gestione?.ToList());
        return m;
    }

    private static GestioneFattureReportDto Gf(string? stato, int? anno, int? mese, string ragione, string tipologia = "SECONDO SALDO") => new()
    {
        IdEnte = "11111111-1111-1111-1111-111111111111", RagioneSociale = ragione, IdContratto = "c1",
        TipoContratto = "PAC", TipologiaFattura = tipologia, Anno = anno, Mese = mese, Stato = stato
    };

    private static async Task<string> TestoNonFatturate(IEnumerable<GestioneFattureReportDto>? gestione)
    {
        var request = new FatturaRicercaRequest { Anno = 2026, Mese = 2, TipologiaFattura = ["SECONDO SALDO"] };
        var reports = await request.ReportFatture(Mediator(gestione).Object, AdminAuth());
        using var wb = new XLWorkbook(new MemoryStream(reports.Single().Value));
        return wb.TryGetWorksheet("Non Fatturate", out var ws) ? string.Join("|", ws.CellsUsed().Select(c => c.GetString())) : string.Empty;
    }

    [Test]
    public async Task Filtro_DatiOstili_ComportamentoDefinito()
    {
        var testo = await TestoNonFatturate(
        [
            Gf(null, 2025, 1, "Stato-Null"),                      // stato assente: trattata come posticipata
            Gf("ELIMINATA", null, null, "Elim-PeriodoNull"),      // eliminata senza periodo: non è "del mese"
            Gf("ELIMINATA", 2026, null, "Elim-MeseNull"),
            Gf("ELIMINATA\t", 2025, 2, "Elim-Tab"),               // Trim toglie anche tab e a capo
            Gf("ELIMINATA", 2026, 2, "Elim-Ok"),
            Gf("ELIMINATE", 2025, 2, "Stato-Sconosciuto"),        // valore diverso da ELIMINATA: entra
            Gf("POSTICIPATA", 2026, 2, "Tipologia-Minuscola", tipologia: "secondo saldo"),
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(testo, Does.Contain("Stato-Null"), "Stato nullo: entra come prima del PF-753.");
            Assert.That(testo, Does.Not.Contain("Elim-PeriodoNull"), "Eliminata senza periodo: non è del mese del report.");
            Assert.That(testo, Does.Not.Contain("Elim-MeseNull"));
            Assert.That(testo, Does.Not.Contain("Elim-Tab"), "Spazi bianchi di ogni tipo non aggirano il filtro.");
            Assert.That(testo, Does.Contain("Elim-Ok"));
            Assert.That(testo, Does.Contain("Stato-Sconosciuto"),
                "CARATTERIZZAZIONE: uno stato sconosciuto è trattato come posticipata (entra). Se la vista introduce nuovi stati, rivedere.");
            Assert.That(testo, Does.Not.Contain("Tipologia-Minuscola"),
                "CARATTERIZZAZIONE: la tipologia è confrontata in modo esatto, come prima del PF-753.");
        });
    }

    [Test]
    public async Task Filtro_QueryNulla_NessunoSheetNessunaEccezione()
    {
        var testo = await TestoNonFatturate(null);

        Assert.That(testo, Is.Empty, "Se la query non restituisce nulla lo sheet non c'è, e il report si genera comunque.");
    }

    // ================================================================== formula injection

    [TestCase("=HYPERLINK(\"http://esempio.invalid\",\"clicca\")")]
    [TestCase("+SUM(1,1)")]
    [TestCase("-2+3")]
    [TestCase("@SUM(A1)")]
    public void NonFatturate_TestoCheSembraFormula_ScrittoComeTesto(string ostile)
    {
        var bytes = new List<IEnumerable<FattureAccontoExcelDto>> { new List<FattureAccontoExcelDto> { new() { Mese = 2, TotaleFattura = 1m } } }
            .ReportFattureAcconto(Mese, [new GestioneFattureReportExcelDto { Stato = "POSTICIPATA", RagioneSociale = ostile, Note = ostile }]);
        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheet($"Non Fatturate {Mese}");

        Assert.Multiple(() =>
        {
            // PF-1029: lo sheet ha ora la riga "Totali:" con SUBTOTAL; sono le uniche formule ammesse.
            Assert.That(ws.CellsUsed().Where(c => c.HasFormula).Select(c => c.FormulaA1), Has.All.StartsWith("SUBTOTAL(9,"),
                "Un dato non deve mai diventare una formula nel file: solo i totali lo sono.");
            Assert.That(ws.CellsUsed().Count(c => c.GetString() == ostile), Is.EqualTo(2), "Il testo resta identico.");
        });
    }

    // ================================================================== legenda: limiti di Excel

    public static IEnumerable<TestCaseData> Legende()
    {
        yield return new TestCaseData(ReportLegende.FattureEmesse).SetName("Legenda_Limiti_FattureEmesse");
        yield return new TestCaseData(ReportLegende.Anticipo).SetName("Legenda_Limiti_Anticipo");
        yield return new TestCaseData(ReportLegende.Acconto).SetName("Legenda_Limiti_Acconto");
        yield return new TestCaseData(ReportLegende.FattureSospese).SetName("Legenda_Limiti_FattureSospese");
        yield return new TestCaseData(ReportLegende.AnticipoSospeso).SetName("Legenda_Limiti_AnticipoSospeso");
        yield return new TestCaseData(ReportLegende.AccontoSospeso).SetName("Legenda_Limiti_AccontoSospeso");
        // PF-1029
        yield return new TestCaseData(ReportLegende.FattureEmessePrimoSaldo).SetName("Legenda_Limiti_FattureEmessePrimoSaldo");
        yield return new TestCaseData(ReportLegende.AndamentoCreditoSospeso).SetName("Legenda_Limiti_AndamentoCreditoSospeso");
        yield return new TestCaseData(ReportLegende.QuadratureRel).SetName("Legenda_Limiti_QuadratureRel");
        yield return new TestCaseData(ReportLegende.RelNonFatturate).SetName("Legenda_Limiti_RelNonFatturate");
    }

    [TestCaseSource(nameof(Legende))]
    public void Legenda_NomiSheet_ValidiPerTuttiIMesi(IReadOnlyList<ReportLegende.Voce> voci)
    {
        var vietati = new[] { '[', ']', ':', '*', '?', '/', '\\' };
        foreach (var mese in Enumerable.Range(1, 12).Select(m => m.GetMonth()).Append(string.Empty))
        {
            var nomi = voci.Select(v => ReportLegende.NomeSheet(v.NomeSheet, mese)).ToList();
            Assert.Multiple(() =>
            {
                Assert.That(nomi, Has.All.Length.LessThanOrEqualTo(31), $"Mese '{mese}': Excel rifiuta nomi oltre 31 caratteri.");
                Assert.That(nomi, Is.Unique, $"Mese '{mese}': due fogli con lo stesso nome renderebbero il file non valido.");
                Assert.That(nomi.Any(n => n.IndexOfAny(vietati) >= 0), Is.False, "Caratteri non ammessi nei nomi dei fogli.");
                Assert.That(nomi, Has.None.Contains("{mese}"));
                Assert.That(nomi, Has.None.Contains("  "), "Senza mese non devono restare spazi doppi.");
                Assert.That(nomi, Has.None.EqualTo(ReportLegende.SheetName), "Nessun foglio dati può chiamarsi Legenda.");
            });
        }
    }

    [Test]
    public void AddLegenda_MeseNullo_NomiSenzaSegnaposto()
    {
        var ds = new DataSet().AddLegenda(ReportLegende.Acconto, null);
        var nomi = ds.Tables[0].Rows.Cast<DataRow>().Select(r => (string)r[0]).ToList();

        Assert.That(nomi, Is.EqualTo(new[] { "Legenda", "Acconto", "Acconto a zero", "Non Fatturate" }));
    }

    [Test]
    public void NomiSheet_SenzaMese_NessunoSpazioDoppioOInCoda()
    {
        // Report delle non inviate: month vuoto. Prima del fix i fogli si chiamavano "Regolari Esecuzioni "
        // e "Enti Fatt.  Sospesi", diversi dalle voci della legenda.
        var rel = new List<IEnumerable<FattureRelExcelDto>> { new List<FattureRelExcelDto> { Rel() }, new List<FattureRelExcelDto> { Rel() } };
        var sospese = new List<IEnumerable<FattureRelSospeseExcelDto>>
        {
            new List<FattureRelSospeseExcelDto>(),
            new List<FattureRelSospeseExcelDto> { new() { IdEnte = "E", TipologiaFattura = "SECONDO SALDO", Anno = 2026, Mese = 2, TotaleFatturaImponibile = 5m, RelNonFirmata = "NO" } }
        };
        using var wb = new XLWorkbook(new MemoryStream(rel.ReportFattureRel(sospese, [], null, "", "SECONDO SALDO")));
        var nomi = wb.Worksheets.Select(w => w.Name).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(nomi, Does.Contain("Regolari Esecuzioni"));
            Assert.That(nomi, Does.Contain("Enti Fatt. Sospesi"));
            Assert.That(nomi.Any(n => n != n.Trim() || n.Contains("  ")), Is.False, string.Join(" | ", nomi));
        });
    }

    [Test]
    public void NormalizzaNomiSheet_NomeCheDiventaDuplicato_Eccezione()
    {
        // Se due fogli diventassero uguali dopo la normalizzazione, meglio fallire che produrre un file con
        // un foglio perso: il rename di DataTableCollection lancia DuplicateNameException.
        var ds = new DataSet();
        ds.Tables.Add(new DataTable("Enti Fatt."));
        ds.Tables.Add(new DataTable("Enti Fatt. "));

        Assert.Throws<DuplicateNameException>(() => ds.NormalizzaNomiSheet());
    }

    [Test]
    public void AddLegenda_DueVolte_Rifiutata()
    {
        // Una seconda legenda non sarebbe più il primo foglio, e il nome duplicato corromperebbe il file.
        var ds = new DataSet().AddLegenda(ReportLegende.Acconto, Mese);

        Assert.Throws<InvalidOperationException>(() => ds.AddLegenda(ReportLegende.Acconto, Mese));
    }

    // ================================================================== ridimensionamento automatico: bordi

    private static double LarghezzaColonna(DataTable table, string intestazione, bool autoFit)
    {
        if (autoFit) table.ExtendedProperties[ExcelExtensions.AutoFitProperty] = true;
        var ds = new DataSet();
        ds.Tables.Add(table);
        using var wb = new XLWorkbook(ds.ToExcel());
        var ws = wb.Worksheet(table.TableName);
        return ws.Column(ws.Row(1).CellsUsed().Single(c => c.GetString() == intestazione).Address.ColumnNumber).Width;
    }

    private static DataTable Tabella(params GestioneFattureReportExcelDto[] righe) => righe.FillOneTable("Non Fatturate");

    [Test]
    public void AutoFit_ColonnaVuota_LargaComeLIntestazione()
    {
        var senza = LarghezzaColonna(Tabella(new GestioneFattureReportExcelDto { Stato = "POSTICIPATA" }), "Ragione Sociale", autoFit: false);
        var con = LarghezzaColonna(Tabella(new GestioneFattureReportExcelDto { Stato = "POSTICIPATA" }), "Ragione Sociale", autoFit: true);

        Assert.That(con, Is.EqualTo(senza), "Celle nulle: la colonna non si restringe sotto la sua intestazione né cresce.");
    }

    [Test]
    public void AutoFit_TestoEnorme_UgualeAlLimite()
    {
        var limite = LarghezzaColonna(Tabella(new GestioneFattureReportExcelDto { RagioneSociale = new string('R', ExcelExtensions.AutoFitMaxChars) }), "Ragione Sociale", true);
        var enorme = LarghezzaColonna(Tabella(new GestioneFattureReportExcelDto { RagioneSociale = new string('R', 10_000) }), "Ragione Sociale", true);

        Assert.That(enorme, Is.EqualTo(limite), "Oltre il limite la larghezza non cresce più.");
    }

    [Test]
    public void AutoFit_NoteCrlf_ContaLaRigaPiuLunga()
    {
        var lf = LarghezzaColonna(Tabella(new GestioneFattureReportExcelDto { Note = "breve\n" + new string('N', 40) }), "Note", true);
        var crlf = LarghezzaColonna(Tabella(new GestioneFattureReportExcelDto { Note = "breve\r\n" + new string('N', 40) }), "Note", true);
        var unaRiga = LarghezzaColonna(Tabella(new GestioneFattureReportExcelDto { Note = new string('N', 40) }), "Note", true);

        Assert.Multiple(() =>
        {
            Assert.That(crlf, Is.EqualTo(lf), "CRLF e LF danno la stessa larghezza.");
            Assert.That(lf, Is.EqualTo(unaRiga), "Conta la riga più lunga, non la somma delle righe.");
        });
    }

    [Test]
    public void AutoFit_SoloSeRichiesto()
    {
        var lunga = new GestioneFattureReportExcelDto { RagioneSociale = new string('R', 60) };
        var senza = LarghezzaColonna(Tabella(lunga), "Ragione Sociale", autoFit: false);
        var con = LarghezzaColonna(Tabella(lunga), "Ragione Sociale", autoFit: true);

        Assert.Multiple(() =>
        {
            Assert.That(senza, Is.LessThan(30), "Senza opt-in resta il calcolo sulla sola intestazione.");
            Assert.That(con, Is.GreaterThanOrEqualTo(60));
        });
    }

    // ================================================================== PF-1029: AddTotali e report nuovi

    private static ReportAndamentoCreditoSospesoExcelDto Andamento(decimal imponibile, string ragione = "Ente") => new()
    {
        IdEnte = "E1", RagioneSociale = ragione, NumFatturaSospesa = 1, Anno = 2026, Mese = 2, ImponibileFattura = imponibile, CreditoCumulato = imponibile
    };

    [Test]
    public void AddTotali_ValoriNegativiDecimaliENull_SommaEsatta()
    {
        // Note di credito (negativi), centesimi e celle vuote: la somma non arrotonda e i null valgono zero.
        var righe = new[]
        {
            new RelNonFatturataDto { IdEnte = "E1", Totale = 100.01m, TotaleIva = 0m },
            new RelNonFatturataDto { IdEnte = "E2", Totale = -40.005m, TotaleIva = 0m },
            new RelNonFatturataDto { IdEnte = "E3", Totale = 0.004m, TotaleIva = 0m },
        };
        using var memory = righe.ReportRelNonFatturate().ToExcel();
        using var wb = new XLWorkbook(new MemoryStream(memory.ToArray()));
        var ws = wb.Worksheet(ReportLegende.SheetRelNonFatturate);
        var colonna = ws.Row(1).CellsUsed().Single(c => c.GetString() == "Totale Imponibile €").Address.ColumnNumber;
        var totale = ws.RowsUsed().Single(r => r.Cell(1).GetString() == "Totali:").Cell(colonna);

        Assert.That(Convert.ToDecimal(totale.CachedValue.GetNumber()), Is.EqualTo(60.009m));
    }

    [Test]
    public void AddTotali_PrimaColonnaNonTesto_Eccezione()
    {
        var table = new DataTable("X");
        table.Columns.Add("Importo", typeof(decimal));

        Assert.Throws<ArgumentException>(() => table.AddTotali("Importo"),
            "L'etichetta \"Totali:\" non entra in una colonna numerica: meglio un errore che un file corrotto.");
    }

    [Test]
    public void AddTotali_DueVolteSullaStessaTabella_DueRigheTotali_SoloLUltimaFormula()
    {
        // Uso scorretto ma possibile: il writer tratta come totali solo l'ultima riga, quindi la prima resta statica
        // e il file resta valido (nessun intervallo che includa una riga di totali).
        var table = new[] { new RelNonFatturataDto { IdEnte = "E1", Totale = 5m } }.FillOneTable(ReportLegende.SheetRelNonFatturate)
            .AddTotali(nameof(RelNonFatturataDto.Totale)).AddTotali(nameof(RelNonFatturataDto.Totale));
        var ds = new DataSet(); ds.Tables.Add(table);
        using var memory = ds.ToExcel();
        using var wb = new XLWorkbook(new MemoryStream(memory.ToArray()));

        Assert.That(wb.Worksheet(1).CellsUsed().Count(c => c.HasFormula), Is.EqualTo(1));
    }

    [TestCase("=HYPERLINK(\"http://esempio.invalid\",\"clicca\")")]
    [TestCase("+SUM(1,1)")]
    [TestCase("@SUM(A1)")]
    public void Andamento_TestoCheSembraFormula_ScrittoComeTesto(string ostile)
    {
        using var memory = new[] { Andamento(1m, ostile) }.ReportAndamentoCreditoSospeso().ToExcel();
        using var wb = new XLWorkbook(new MemoryStream(memory.ToArray()));
        var ws = wb.Worksheet(ReportLegende.SheetAndamentoCreditoSospeso);

        Assert.Multiple(() =>
        {
            Assert.That(ws.CellsUsed().Where(c => c.HasFormula).Select(c => c.FormulaA1), Has.All.StartsWith("SUBTOTAL(9,"));
            Assert.That(ws.CellsUsed().Count(c => c.GetString() == ostile), Is.EqualTo(1));
        });
    }

    [Test]
    public void Quadrature_CampiNull_NessunaEccezioneETotaliCoerenti()
    {
        // La quadratura parte da NotificheCount con LEFT JOIN: REL e contestazioni possono mancare del tutto.
        var righe = new[] { new RelQuadraturaDto { IdEnte = "E1" }, new RelQuadraturaDto { IdEnte = "E2", NotificheTotale = 2.5m } };

        using var memory = righe.ReportQuadratureRel().ToExcel();
        using var wb = new XLWorkbook(new MemoryStream(memory.ToArray()));

        Assert.That(wb.Worksheet(ReportLegende.SheetQuadratureRel).CellsUsed().Any(c => c.HasFormula), Is.True);
    }
}
