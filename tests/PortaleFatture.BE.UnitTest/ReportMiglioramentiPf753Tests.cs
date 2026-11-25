using ClosedXML.Excel;
using MediatR;
using Moq;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// PF-753 — miglioramento dei report Excel dei documenti emessi e sospesi (nessun DB, IMediator mockato):
/// <list type="bullet">
/// <item>sheet "Legenda" statico, primo e unico, con una riga per ogni foglio previsto dalla tipologia;</item>
/// <item>ANTICIPO (emesso e sospeso) senza gli sheet "stimate" ed "eliminate fatt.";</item>
/// <item>"Non Fatturate": posticipate tutte, eliminate solo del mese di competenza del report;</item>
/// <item>riga "Totali:" scritta con SUBTOTAL(9, …), con il valore calcolato come cache.</item>
/// </list>
/// </summary>
[Category("Fatture")]
public class ReportMiglioramentiPf753Tests
{
    private const string Mese = "FEBBRAIO";

    private static AuthenticationInfo AdminAuth() => new()
    {
        IdEnte = Guid.NewGuid().ToString(),
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };

    // ------------------------------------------------------------------ dati

    private static FattureRelExcelDto Rel(decimal totale) => new()
    {
        IdEnte = "ENTE-1", RagioneSociale = "Ente", IdContratto = "C1", TipologiaFattura = "SECONDO SALDO",
        IdFattura = "1", Progressivo = "1", TipoDocumento = "TD01", Anno = 2026, Mese = 2,
        TotaleFatturaImponibile = totale, TipologiaContratto = "PAC"
    };

    private static FattureRelSospeseExcelDto RelSospesa(decimal totale) => new()
    {
        IdEnte = "ENTE-1", RagioneSociale = "Ente", IdContratto = "C1", TipologiaFattura = "SECONDO SALDO",
        IdFattura = "1", Progressivo = "1", TipoDocumento = "TD01", Anno = 2026, Mese = 2,
        TotaleFatturaImponibile = totale, TipologiaContratto = "PAC", RelNonFirmata = "NO"
    };

    // Due gruppi (regolari esecuzioni, enti fatturati) con imponibile sia diverso da zero sia zero:
    // cosi' ReportFattureRel produce tutti i fogli che sa produrre.
    private static List<IEnumerable<FattureRelExcelDto>> RelCompleta() =>
        [new List<FattureRelExcelDto> { Rel(100m) }, new List<FattureRelExcelDto> { Rel(100m), Rel(0m) }];

    private static List<IEnumerable<FattureRelSospeseExcelDto>> SospeseComplete() =>
        [new List<FattureRelSospeseExcelDto> { RelSospesa(50m) }, new List<FattureRelSospeseExcelDto> { RelSospesa(50m), RelSospesa(0m) }];

    // I sei gruppi di FattureCommessaExcelQuery, uno per indice.
    private static List<IEnumerable<FattureCommessaExcelDto>> SeiGruppiCommessa() =>
        Enumerable.Range(0, 6)
            .Select(i => (IEnumerable<FattureCommessaExcelDto>)new List<FattureCommessaExcelDto> { new() { MeseValidita = 2, TotaleFattura = 10m * (i + 1) } })
            .ToList();

    private static FattureAccontoExcelDto Acconto(decimal totale, int digitali) => new()
    {
        Mese = 2, TotaleFattura = totale, TotaleNotificheDigitali = digitali
    };

    private static List<IEnumerable<FattureAccontoExcelDto>> AccontoCompleto() =>
        [new List<FattureAccontoExcelDto> { Acconto(100m, 3), Acconto(200m, 4), Acconto(0m, 1) }];

    private static GestioneFattureReportExcelDto GfExcel(string stato) => new() { Stato = stato, RagioneSociale = "Ente-GF" };

    private static GestioneFattureReportDto Gf(string tipologia, string stato, int anno, int mese, string ragione) => new()
    {
        IdEnte = "11111111-1111-1111-1111-111111111111",
        RagioneSociale = ragione,
        IdContratto = "c1",
        TipoContratto = "PAC",
        TipologiaFattura = tipologia,
        Anno = anno,
        Mese = mese,
        Stato = stato
    };

    private static FattureListaDto DettaglioSospese() => [];

    // PF-1029: righe della quadratura REL (sheet "Quadrature Reg Esecuzione" del PRIMO SALDO).
    private static List<RelQuadraturaDto> Quadrature() =>
    [
        new() { IdEnte = "E1", RagioneSociale = "Ente Q1", TipologiaFattura = "PRIMO SALDO", Anno = 2026, Mese = 2, NotificheTotale = 10.5m },
        new() { IdEnte = "E2", RagioneSociale = "Ente Q2", TipologiaFattura = "PRIMO SALDO", Anno = 2026, Mese = 2, NotificheTotale = 4.5m },
    ];

    // ------------------------------------------------------------------ lettura xlsx

    private static List<string> Fogli(byte[] xlsx)
    {
        using var wb = new XLWorkbook(new MemoryStream(xlsx));
        return wb.Worksheets.Select(w => w.Name).ToList();
    }

    private static List<(string Nome, string Descrizione)> RigheLegenda(byte[] xlsx)
    {
        using var wb = new XLWorkbook(new MemoryStream(xlsx));
        var ws = wb.Worksheet(ReportLegende.SheetName);
        return ws.RowsUsed().Skip(1).Select(r => (r.Cell(1).GetString(), r.Cell(2).GetString())).ToList();
    }

    private static string Atteso(string modello, string month) => ReportLegende.NomeSheet(modello, month);

    // ------------------------------------------------------------------ Legenda: un caso per report

    public static IEnumerable<TestCaseData> ReportConLegenda()
    {
        var gestione = new[] { GfExcel("POSTICIPATA") };
        yield return new TestCaseData((Func<byte[]>)(() => RelCompleta().ReportFattureRel(SospeseComplete(), [], gestione, Mese, "SECONDO SALDO")), ReportLegende.FattureEmesse, Mese)
            .SetName("Legenda_FattureEmesse");
        yield return new TestCaseData((Func<byte[]>)(() => RelCompleta().ReportFattureRel(SospeseComplete(), [], gestione, "", "SECONDO SALDO")), ReportLegende.FattureEmesse, "")
            .SetName("Legenda_FattureEmesse_NonInviateSenzaMese");
        yield return new TestCaseData((Func<byte[]>)(() => RelCompleta().ReportFattureRel(SospeseComplete(), [], gestione, Mese, "PRIMO SALDO", Quadrature())), ReportLegende.FattureEmessePrimoSaldo, Mese)
            .SetName("Legenda_FattureEmessePrimoSaldo_ConQuadrature");
        yield return new TestCaseData((Func<byte[]>)(() => SeiGruppiCommessa().ReportFattureModuloCommessa(Mese, gestione)), ReportLegende.Anticipo, Mese)
            .SetName("Legenda_Anticipo");
        yield return new TestCaseData((Func<byte[]>)(() => AccontoCompleto().ReportFattureAcconto(Mese, gestione)), ReportLegende.Acconto, Mese)
            .SetName("Legenda_Acconto");
        yield return new TestCaseData((Func<byte[]>)(() => SospeseComplete().ReportFattureSospeseRel(Mese, "SECONDO SALDO")), ReportLegende.FattureSospese, Mese)
            .SetName("Legenda_FattureSospese");
        yield return new TestCaseData((Func<byte[]>)(() => SospeseComplete().ReportFattureSospeseRel(DettaglioSospese(), Mese, "PRIMO SALDO")), ReportLegende.FattureSospese, Mese)
            .SetName("Legenda_FattureSospese_ConDettaglio");
        yield return new TestCaseData((Func<byte[]>)(() => SeiGruppiCommessa().ReportFattureSospeseModuloCommessa(Mese)), ReportLegende.AnticipoSospeso, Mese)
            .SetName("Legenda_AnticipoSospeso");
        yield return new TestCaseData((Func<byte[]>)(() => AccontoCompleto().ReportFattureSospeseAcconto(Mese, gestione)), ReportLegende.AccontoSospeso, Mese)
            .SetName("Legenda_AccontoSospeso");
        // PF-1029 (voce 10): i tre report fuori dai documenti emessi e sospesi, senza mese.
        yield return new TestCaseData((Func<byte[]>)(() => Xlsx(new[] { Andamento(5m, 5m) }.ReportAndamentoCreditoSospeso())), ReportLegende.AndamentoCreditoSospeso, "")
            .SetName("Legenda_AndamentoCreditoSospeso");
        yield return new TestCaseData((Func<byte[]>)(() => Xlsx(Quadrature().ReportQuadratureRel())), ReportLegende.QuadratureRel, "")
            .SetName("Legenda_QuadratureRel");
        yield return new TestCaseData((Func<byte[]>)(() => Xlsx(new[] { RelNf(10m, 12.2m) }.ReportRelNonFatturate())), ReportLegende.RelNonFatturate, "")
            .SetName("Legenda_RelNonFatturate");
    }

    private static byte[] Xlsx(System.Data.DataSet dataSet)
    {
        using var memory = dataSet.ToExcel();
        return memory.ToArray();
    }

    [TestCaseSource(nameof(ReportConLegenda))]
    public void Legenda_PrimoSheet_UnaVolta_ConIntestazioni(Func<byte[]> genera, IReadOnlyList<ReportLegende.Voce> _, string __)
    {
        var xlsx = genera();
        using var wb = new XLWorkbook(new MemoryStream(xlsx));
        var legenda = wb.Worksheet(1);

        Assert.Multiple(() =>
        {
            Assert.That(legenda.Name, Is.EqualTo("Legenda"), "La legenda deve essere il primo sheet.");
            Assert.That(Fogli(xlsx).Count(f => f == "Legenda"), Is.EqualTo(1), "Una sola legenda per report.");
            Assert.That(legenda.Cell(1, 1).GetString(), Is.EqualTo("Nome sheet"));
            Assert.That(legenda.Cell(1, 2).GetString(), Is.EqualTo("Descrizione"));
            Assert.That(legenda.Cell(1, 3).IsEmpty(), Is.True, "Solo due colonne.");
        });
    }

    [TestCaseSource(nameof(ReportConLegenda))]
    public void Legenda_ElencaLegendaETuttiIFogliPrevisti(Func<byte[]> genera, IReadOnlyList<ReportLegende.Voce> voci, string month)
    {
        var righe = RigheLegenda(genera());

        var attese = new List<(string, string)> { ("Legenda", "Elenco dei fogli del report e descrizione del relativo contenuto") };
        attese.AddRange(voci.Select(v => (Atteso(v.NomeSheet, month), v.Descrizione)));

        Assert.That(righe, Is.EqualTo(attese), "La legenda e' statica: tutti i fogli previsti, nell'ordine del documento.");
    }

    [TestCaseSource(nameof(ReportConLegenda))]
    public void Legenda_OgniFoglioGeneratoEDescritto(Func<byte[]> genera, IReadOnlyList<ReportLegende.Voce> _, string month)
    {
        // Contro-prova di coerenza fra codice e documento: un foglio prodotto ma assente dalla legenda
        // vuol dire che e' cambiato il nome di uno sheet senza aggiornare ReportLegende (o viceversa).
        // Confronto ESATTO, senza normalizzare: una prima versione normalizzava gli spazi e nascondeva che
        // nel report delle non inviate (senza mese) i fogli si chiamavano "Regolari Esecuzioni " con lo
        // spazio in coda, diversi dalla voce della legenda. Oggi i builder chiamano NormalizzaNomiSheet.
        // Eccezione deliberata: i fogli "a zero" dei saldi (ReportLegende.FogliNonInLegenda, 25/11/2026) possono
        // comparire senza riga in legenda. Le fixture li producono di proposito, vedi i test sotto.
        var xlsx = genera();
        var descritti = RigheLegenda(xlsx).Select(r => r.Nome).ToHashSet();
        var esclusi = ReportLegende.FogliNonInLegenda.Select(m => Atteso(m, month)).ToHashSet();

        Assert.That(Fogli(xlsx).Where(f => !esclusi.Contains(f)), Is.SubsetOf(descritti));
    }

    // ------------------------------------------------------------------ Fogli "a zero" fuori dalla legenda (25/11/2026)

    public static IEnumerable<TestCaseData> ReportSaldi()
    {
        var gestione = new[] { GfExcel("POSTICIPATA") };
        yield return new TestCaseData((Func<byte[]>)(() => RelCompleta().ReportFattureRel(SospeseComplete(), [], gestione, Mese, "SECONDO SALDO")), "Enti Fatt. a Zero {mese}", Mese)
            .SetName("FattAZero_Emesse");
        yield return new TestCaseData((Func<byte[]>)(() => RelCompleta().ReportFattureRel(SospeseComplete(), [], gestione, Mese, "PRIMO SALDO", Quadrature())), "Enti Fatt. a Zero {mese}", Mese)
            .SetName("FattAZero_EmessePrimoSaldo");
        yield return new TestCaseData((Func<byte[]>)(() => RelCompleta().ReportFattureRel(SospeseComplete(), [], gestione, "", "SECONDO SALDO")), "Enti Fatt. a Zero {mese}", "")
            .SetName("FattAZero_EmesseNonInviate");
        yield return new TestCaseData((Func<byte[]>)(() => SospeseComplete().ReportFattureSospeseRel(Mese, "SECONDO SALDO")), "Fatt. a Zero Sosp. {mese}", Mese)
            .SetName("FattAZero_Sospese");
        yield return new TestCaseData((Func<byte[]>)(() => SospeseComplete().ReportFattureSospeseRel(DettaglioSospese(), Mese, "PRIMO SALDO")), "Fatt. a Zero Sosp. {mese}", Mese)
            .SetName("FattAZero_SospeseConDettaglio");
    }

    [TestCaseSource(nameof(ReportSaldi))]
    public void FattAZero_NonCompareInLegenda(Func<byte[]> genera, string modello, string month)
    {
        // Segnalazione PF-1029 del 24/11/2026: la legenda elencava un foglio che nei report reali non c'e' mai.
        Assert.That(RigheLegenda(genera()).Select(r => r.Nome), Does.Not.Contain(Atteso(modello, month)));
    }

    [TestCaseSource(nameof(ReportSaldi))]
    public void FattAZero_ConUnaFatturaAZero_IlFoglioVieneAncoraGenerato(Func<byte[]> genera, string modello, string month)
    {
        // Decisione del 25/11/2026 (strada A): si toglie solo la riga di legenda. Se arriva una fattura a zero il
        // foglio c'e' ancora, cosi' il dato non sparisce dal report. Le fixture contengono una fattura a zero.
        Assert.That(Fogli(genera()), Does.Contain(Atteso(modello, month)),
            "La fixture deve contenere una fattura a zero: senza, questo test non dimostra nulla.");
    }

    [Test]
    public void FattAZero_AccontoResta_InLegenda()
    {
        // Le voci "a zero" dell'acconto non erano nella segnalazione: restano.
        Assert.Multiple(() =>
        {
            Assert.That(ReportLegende.Acconto.Select(v => v.NomeSheet), Does.Contain("Acconto a zero {mese}"));
            Assert.That(ReportLegende.AccontoSospeso.Select(v => v.NomeSheet), Does.Contain("Acconto a zero Sosp. {mese}"));
        });
    }

    [Test]
    public void Legenda_SuDataSetNonVuoto_Rifiutata()
    {
        var ds = new System.Data.DataSet();
        ds.Tables.Add(new System.Data.DataTable("Altro"));

        Assert.Throws<InvalidOperationException>(() => ds.AddLegenda(ReportLegende.Acconto, Mese),
            "La legenda deve essere il primo sheet: aggiungerla dopo altri fogli e' un errore di programmazione.");
    }

    [TestCase("Enti Fatt. {mese} Sospesi", "", "Enti Fatt. Sospesi")]
    [TestCase("Non Fatturate {mese}", "", "Non Fatturate")]
    [TestCase("Non Fatturate {mese}", Mese, "Non Fatturate FEBBRAIO")]
    [TestCase("Reg. Esec. PAC non firmate", Mese, "Reg. Esec. PAC non firmate")]
    public void Legenda_NomeSheet_SostituisceIlMese(string modello, string month, string atteso) =>
        Assert.That(ReportLegende.NomeSheet(modello, month), Is.EqualTo(atteso));

    // ------------------------------------------------------------------ ANTICIPO: sheet rimossi

    [Test]
    public void Anticipo_SenzaShetStimateEdEliminate()
    {
        var fogli = Fogli(SeiGruppiCommessa().ReportFattureModuloCommessa(Mese));

        Assert.That(fogli, Is.EqualTo(new[] { "Legenda", "Com. FEBBRAIO", "Com. fatt. FEBBRAIO", "Com. non fatt. FEBBRAIO" }),
            "PF-753: 'Com. stimate', 'Com. stimate fatt.' e 'Com. eliminate fatt.' non vanno piu' prodotti.");
    }

    [Test]
    public void AnticipoSospeso_SenzaShetStimateEdEliminate()
    {
        var fogli = Fogli(SeiGruppiCommessa().ReportFattureSospeseModuloCommessa(Mese));

        Assert.That(fogli, Is.EqualTo(new[] { "Legenda", "Com. Sospese FEBBRAIO", "Com. fatt. Sospese FEBBRAIO", "Com. non fatt. Sosp. FEBBRAIO" }));
    }

    [Test]
    public void Anticipo_GruppiConservanoIlContenuto()
    {
        // I gruppi restano agganciati al loro indice: "Com. fatt." e' il gruppo 3 (totale 40), "Com. non
        // fatt." il 4 (totale 50). Togliere gli sheet non deve far scorrere i dati di un posto.
        using var wb = new XLWorkbook(new MemoryStream(SeiGruppiCommessa().ReportFattureModuloCommessa(Mese)));

        static bool Contiene(IXLWorksheet ws, decimal v) => ws.CellsUsed().Any(c => c.DataType == XLDataType.Number && c.GetValue<decimal>() == v);
        Assert.Multiple(() =>
        {
            Assert.That(Contiene(wb.Worksheet("Com. FEBBRAIO"), 10m), Is.True);
            Assert.That(Contiene(wb.Worksheet("Com. fatt. FEBBRAIO"), 40m), Is.True);
            Assert.That(Contiene(wb.Worksheet("Com. non fatt. FEBBRAIO"), 50m), Is.True);
        });
    }

    // ------------------------------------------------------------------ Non Fatturate: eliminate del mese

    private static Mock<IMediator> Mediator(string tipologia, IEnumerable<GestioneFattureReportDto> gestione)
    {
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<FattureRelExcelQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<IEnumerable<FattureRelExcelDto>>?)RelCompleta());
        m.Setup(x => x.Send(It.IsAny<RelNonFatturateQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<RelNonFatturataDto>?)new List<RelNonFatturataDto>());
        m.Setup(x => x.Send(It.IsAny<FattureCommessaExcelQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<IEnumerable<FattureCommessaExcelDto>>?)SeiGruppiCommessa());
        m.Setup(x => x.Send(It.IsAny<FattureAccontoExcelQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<IEnumerable<FattureAccontoExcelDto>>?)AccontoCompleto());
        m.Setup(x => x.Send(It.IsAny<GestioneFattureReportQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<GestioneFattureReportDto>?)gestione.ToList());
        return m;
    }

    private static string TestoSheet(byte[] xlsx, string nome)
    {
        using var wb = new XLWorkbook(new MemoryStream(xlsx));
        return wb.TryGetWorksheet(nome, out var ws) ? string.Join("|", ws.CellsUsed().Select(c => c.GetString())) : string.Empty;
    }

    [TestCase("PRIMO SALDO", "Non Fatturate")]
    [TestCase("SECONDO SALDO", "Non Fatturate")]
    [TestCase("VAR. SEMESTRALE", "Non Fatturate")]
    [TestCase("SEM. SOSPESI", "Non Fatturate")]
    [TestCase("ANTICIPO", "Non Fatturate FEBBRAIO")]
    [TestCase("ACCONTO", "Non Fatturate FEBBRAIO")]
    public async Task ReportFatture_NonFatturate_PosticipateTutte_EliminateSoloDelMese(string tipologia, string sheet)
    {
        var gestione = new[]
        {
            Gf(tipologia, "POSTICIPATA", 2025, 7, "Post-AltroPeriodo"),
            Gf(tipologia, "POSTICIPATA", 2026, 2, "Post-StessoPeriodo"),
            Gf(tipologia, "ELIMINATA",   2026, 2, "Elim-StessoPeriodo"),
            Gf(tipologia, "ELIMINATA",   2026, 1, "Elim-AltroMese"),
            Gf(tipologia, "ELIMINATA",   2025, 2, "Elim-AltroAnno"),
            Gf(tipologia, " eliminata ", 2025, 2, "Elim-CaseESpazi"),
        };
        var request = new FatturaRicercaRequest { Anno = 2026, Mese = 2, TipologiaFattura = [tipologia] };

        var reports = await request.ReportFatture(Mediator(tipologia, gestione).Object, AdminAuth());
        var testo = TestoSheet(reports.Single().Value, sheet);

        Assert.Multiple(() =>
        {
            Assert.That(testo, Does.Contain("Post-AltroPeriodo"), "Le posticipate restano tutte, anche di altri periodi.");
            Assert.That(testo, Does.Contain("Post-StessoPeriodo"));
            Assert.That(testo, Does.Contain("Elim-StessoPeriodo"), "L'eliminata del mese del report entra.");
            Assert.That(testo, Does.Not.Contain("Elim-AltroMese"), "Eliminata di un altro mese: esclusa.");
            Assert.That(testo, Does.Not.Contain("Elim-AltroAnno"), "Stesso mese, anno diverso: esclusa.");
            Assert.That(testo, Does.Not.Contain("Elim-CaseESpazi"), "Lo stato e' confrontato senza badare a maiuscole e spazi.");
        });
    }

    [Test]
    public async Task ReportFatture_NonFatturate_SoloEliminateDiAltriPeriodi_NessunoSheet()
    {
        var gestione = new[] { Gf("ANTICIPO", "ELIMINATA", 2025, 3, "Elim-Vecchia") };
        var request = new FatturaRicercaRequest { Anno = 2026, Mese = 2, TipologiaFattura = ["ANTICIPO"] };

        var reports = await request.ReportFatture(Mediator("ANTICIPO", gestione).Object, AdminAuth());

        Assert.That(Fogli(reports.Single().Value).Where(f => f.StartsWith("Non Fatturate")), Is.Empty,
            "Filtrate le eliminate di altri periodi, non resta nulla: lo sheet non si produce (la legenda lo elenca comunque).");
    }

    /// <summary>
    /// CARATTERIZZAZIONE — il report delle non inviate non ha un mese, quindi il filtro PF-753 non si
    /// applica: le eliminate entrano tutte, come prima. Se il requisito viene chiarito diversamente,
    /// aggiornare questo test.
    /// </summary>
    [Test]
    public async Task ReportNonInviate_NonFatturate_EliminateTutte_Caratterizzazione()
    {
        var m = Mediator("SECONDO SALDO", [Gf("SECONDO SALDO", "ELIMINATA", 2025, 3, "Elim-Vecchia")]);
        m.Setup(x => x.Send(It.IsAny<NonFatturateTipologiaQueryRicerca>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<AnniMesiTipologiaDto>?)new List<AnniMesiTipologiaDto> { new() { Anno = 2026, Mese = 2, TipologiaFattura = "SECONDO SALDO" } });
        var request = new NonFatturateRicercaRequest { TipologiaFattura = ["SECONDO SALDO"] };

        var reports = await request.ReportNonInviate(m.Object, AdminAuth());

        Assert.That(TestoSheet(reports["Lista SECONDO SALDO"], "Non Fatturate"), Does.Contain("Elim-Vecchia"));
    }

    // ------------------------------------------------------------------ Totali con SUBTOTAL

    private static IXLCell CellaTotale(IXLWorksheet ws, string intestazione)
    {
        var colonna = ws.Row(1).CellsUsed().Single(c => c.GetString() == intestazione).Address.ColumnNumber;
        var rigaTotali = ws.RowsUsed().Single(r => r.Cell(1).GetString() == "Totali:");
        return rigaTotali.Cell(colonna);
    }

    [Test]
    public void Totali_ScrittiConSubtotal_SulleSoleRigheDati()
    {
        using var wb = new XLWorkbook(new MemoryStream(AccontoCompleto().ReportFattureAcconto(Mese)));
        var ws = wb.Worksheet("Acconto FEBBRAIO");   // due righe (totale != 0), riga vuota, riga totali
        var cella = CellaTotale(ws, "N. Notifiche Digitali");
        var lettera = cella.Address.ColumnLetter;

        Assert.Multiple(() =>
        {
            Assert.That(cella.FormulaA1, Is.EqualTo($"SUBTOTAL(9,{lettera}2:{lettera}3)"),
                "PF-753: i totali usano SUBTOTAL, cosi' seguono i filtri di Excel; il range esclude riga vuota e totali.");
            Assert.That(cella.CachedValue.GetNumber(), Is.EqualTo(7d), "Il valore in cache e' quello calcolato prima: 3 + 4.");
        });
    }

    [Test]
    public void Totali_ColonneDecimaliConSubtotal()
    {
        using var wb = new XLWorkbook(new MemoryStream(RelCompleta().ReportFattureRel(null, [], null, Mese, "SECONDO SALDO")));
        var ws = wb.Worksheet($"Enti Fatt. {Mese}");
        var formule = ws.RowsUsed().Single(r => r.Cell(1).GetString() == "Totali:")
            .CellsUsed().Where(c => c.HasFormula).Select(c => c.FormulaA1).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(formule, Is.Not.Empty);
            Assert.That(formule, Has.All.StartsWith("SUBTOTAL(9,"));
            Assert.That(formule, Has.All.EndsWith("2)"), "Una riga dati: range dalla 2 alla 2.");
        });
    }

    [Test]
    public void Totali_SenzaRigheDati_NessunaFormula()
    {
        // Un gruppo vuoto produce solo riga vuota + totali: un range "X2:X1" includerebbe l'intestazione.
        // In quel caso restano i valori statici (zero) come prima.
        var vuoto = new List<IEnumerable<FattureRelExcelDto>> { new List<FattureRelExcelDto>() };
        using var wb = new XLWorkbook(new MemoryStream(vuoto.ReportFattureRel(null, [], null, Mese, "SECONDO SALDO")));
        var ws = wb.Worksheet($"Regolari Esecuzioni {Mese}");

        Assert.That(ws.CellsUsed().Any(c => c.HasFormula), Is.False);
    }

    // ------------------------------------------------------------------ "Non Fatturate": colonne ridimensionate

    private static double Larghezza(IXLWorksheet ws, string intestazione)
    {
        var cella = ws.Row(1).CellsUsed().Single(c => c.GetString() == intestazione);
        return ws.Column(cella.Address.ColumnNumber).Width;
    }

    private static readonly string RagioneLunga = new('R', 50);
    private static readonly string NotaLunga = "2026-11-20 10:00:00 " + new string('N', 40);   // 60 caratteri

    private static GestioneFattureReportExcelDto GfLunga(string? ragione = null) => new()
    {
        Stato = "POSTICIPATA",
        RagioneSociale = ragione ?? RagioneLunga,
        Note = $"2026-11-19 09:00:00 breve\n{NotaLunga}",
        IdEnte = "11111111-1111-1111-1111-111111111111",
        TotaleImponibile = 1234567.89m,
    };

    public static IEnumerable<TestCaseData> ReportConNonFatturate()
    {
        yield return new TestCaseData((Func<GestioneFattureReportExcelDto[], byte[]>)(g => RelCompleta().ReportFattureRel(null, [], g, Mese, "SECONDO SALDO")), "Non Fatturate")
            .SetName("NonFatturate_AutoFit_Saldo");
        yield return new TestCaseData((Func<GestioneFattureReportExcelDto[], byte[]>)(g => SeiGruppiCommessa().ReportFattureModuloCommessa(Mese, g)), $"Non Fatturate {Mese}")
            .SetName("NonFatturate_AutoFit_Anticipo");
        yield return new TestCaseData((Func<GestioneFattureReportExcelDto[], byte[]>)(g => AccontoCompleto().ReportFattureAcconto(Mese, g)), $"Non Fatturate {Mese}")
            .SetName("NonFatturate_AutoFit_Acconto");
        yield return new TestCaseData((Func<GestioneFattureReportExcelDto[], byte[]>)(g => AccontoCompleto().ReportFattureSospeseAcconto(Mese, g)), $"Non Fatturate {Mese}")
            .SetName("NonFatturate_AutoFit_AccontoSospeso");
    }

    [TestCaseSource(nameof(ReportConNonFatturate))]
    public void NonFatturate_ColonneRidimensionateSulContenuto(Func<GestioneFattureReportExcelDto[], byte[]> genera, string sheet)
    {
        using var wb = new XLWorkbook(new MemoryStream(genera([GfLunga()])));
        var ws = wb.Worksheet(sheet);

        Assert.Multiple(() =>
        {
            Assert.That(Larghezza(ws, "Ragione Sociale"), Is.GreaterThanOrEqualTo(RagioneLunga.Length),
                "PF-753: la colonna segue il contenuto, non la sola intestazione (15 caratteri).");
            Assert.That(Larghezza(ws, "Id Ente"), Is.GreaterThanOrEqualTo(36), "Un GUID intero deve essere leggibile.");
            Assert.That(Larghezza(ws, "Note"), Is.GreaterThanOrEqualTo(NotaLunga.Length),
                "Note multiriga: conta la riga piu' lunga.");
            Assert.That(Larghezza(ws, "Note"), Is.LessThan(NotaLunga.Length + 30),
                "Note multiriga: non la somma delle righe (sarebbe ~85).");
            Assert.That(Larghezza(ws, "Totale Imponibile"), Is.GreaterThanOrEqualTo("Totale Imponibile".Length),
                "Mai piu' stretta dell'intestazione.");
        });
    }

    [Test]
    public void NonFatturate_TestoMoltoLungo_LarghezzaLimitata()
    {
        using var wb = new XLWorkbook(new MemoryStream(AccontoCompleto().ReportFattureAcconto(Mese, [GfLunga(new string('R', 300))])));
        var larghezza = Larghezza(wb.Worksheet($"Non Fatturate {Mese}"), "Ragione Sociale");

        Assert.That(larghezza, Is.InRange(80d, 100d),
            "Oltre 80 caratteri la colonna non cresce piu': un testo anomalo non deve produrre una colonna enorme.");
    }

    [Test]
    public void AltriSheet_LarghezzaInvariata_SoloIntestazione()
    {
        // L'autofit e' opt-in: gli sheet diversi da "Non Fatturate" restano dimensionati sull'intestazione.
        var rel = Rel(100m);
        rel.RagioneSociale = RagioneLunga;
        var gruppi = new List<IEnumerable<FattureRelExcelDto>> { new List<FattureRelExcelDto> { rel } };
        using var wb = new XLWorkbook(new MemoryStream(gruppi.ReportFattureRel(null, [], [GfLunga()], Mese, "SECONDO SALDO")));
        var ws = wb.Worksheet($"Regolari Esecuzioni {Mese}");
        var colonna = ws.Row(1).CellsUsed().First(c => c.GetString().Contains("Ragione", StringComparison.OrdinalIgnoreCase));

        Assert.That(ws.Column(colonna.Address.ColumnNumber).Width, Is.LessThan(RagioneLunga.Length));
    }

    [Test]
    public void Legenda_NonHaTotali()
    {
        using var wb = new XLWorkbook(new MemoryStream(AccontoCompleto().ReportFattureAcconto(Mese)));

        Assert.That(wb.Worksheet("Legenda").CellsUsed().Any(c => c.HasFormula || c.GetString() == "Totali:"), Is.False);
    }

    // ------------------------------------------------------------------ PF-1029: nomi dei fogli dei report REL

    [Test]
    public void ReportRel_NomiFoglioDaSpecifica_ValidiPerExcel()
    {
        // I nomi sono quelli della checklist del prodotto (punti 14 e 15): confronto letterale, non con le costanti
        // stesse, cosi' un cambio della costante fa diventare rosso il test.
        Assert.Multiple(() =>
        {
            Assert.That(ReportLegende.SheetQuadratureRel, Is.EqualTo("Quadrature Reg Esecuzione"));
            Assert.That(ReportLegende.SheetRelNonFatturate, Is.EqualTo("Reg Esecuzioni non fatturate"));
            Assert.That(new[] { ReportLegende.SheetQuadratureRel, ReportLegende.SheetRelNonFatturate }.Select(n => n.Length),
                Has.All.LessThanOrEqualTo(31), "Limite Open XML dei nomi sheet.");
        });
    }

    [Test]
    public void Quadrature_FoglioConNomeDaSpecifica_ConSubtotal()
    {
        // Stesso builder di RelModule.GetPagoPAQuadraturaRicercaDocumentAsync. Senza il nome il foglio si chiamava "T".
        var righe = new[]
        {
            new RelQuadraturaDto { IdEnte = "E1", Anno = 2026, Mese = 2, NotificheTotale = 10.5m },
            new RelQuadraturaDto { IdEnte = "E2", Anno = 2026, Mese = 2, NotificheTotale = 4.5m },
        };

        using var memory = righe.ReportQuadratureRel().ToExcel();
        using var wb = new XLWorkbook(new MemoryStream(memory.ToArray()));

        Assert.Multiple(() =>
        {
            Assert.That(wb.Worksheets.Select(w => w.Name), Is.EqualTo(new[] { "Legenda", "Quadrature Reg Esecuzione" }));
            var totale = CellaTotale(wb.Worksheet("Quadrature Reg Esecuzione"), "Consuntivo Imponibile €");
            Assert.That(totale.FormulaA1, Does.StartWith("SUBTOTAL(9,"), "Il nome non deve far perdere i totali con SUBTOTAL.");
        });
    }

    [Test]
    public void RelNonFatturate_FoglioConNomeDaSpecifica()
    {
        // Stesso builder di RelModule.GetRelNonFatturateExcelAsync.
        var righe = new[] { new RelNonFatturataDto { IdEnte = "E1", Anno = 2026, Mese = 2, TipologiaFattura = "PRIMO SALDO" } };

        using var memory = righe.ReportRelNonFatturate().ToExcel();
        using var wb = new XLWorkbook(new MemoryStream(memory.ToArray()));

        Assert.That(wb.Worksheets.Select(w => w.Name), Is.EqualTo(new[] { "Legenda", "Reg Esecuzioni non fatturate" }));
    }

    // ------------------------------------------------------------------ PF-1029: Quadrature nel PRIMO SALDO (voce 5)

    [Test]
    public void PrimoSaldo_ConQuadrature_SheetDopoLeRel_ConSubtotal()
    {
        var xlsx = RelCompleta().ReportFattureRel(SospeseComplete(), [], null, Mese, "PRIMO SALDO", Quadrature());
        var fogli = Fogli(xlsx);
        using var wb = new XLWorkbook(new MemoryStream(xlsx));
        var ws = wb.Worksheet("Quadrature Reg Esecuzione");

        Assert.Multiple(() =>
        {
            Assert.That(fogli.IndexOf("Quadrature Reg Esecuzione"), Is.EqualTo(fogli.IndexOf("Reg. Esec. PAC non firmate") + 1),
                "Subito dopo le REL, nell'ordine della legenda.");
            Assert.That(ws.CellsUsed().Select(c => c.GetString()), Does.Contain("Ente Q1").And.Contain("Ente Q2"));
            var totale = CellaTotale(ws, "Consuntivo Imponibile €");
            Assert.That(totale.FormulaA1, Does.StartWith("SUBTOTAL(9,"));
            Assert.That(totale.CachedValue.GetNumber(), Is.EqualTo(15d), "Cache = somma delle righe (10,5 + 4,5).");
            // Stesse colonne sommate del download di quadratura: Anno e Mese (prima della colonna 6) restano fuori.
            Assert.That(CellaTotale(ws, "Anno").IsEmpty(), Is.True);
            Assert.That(CellaTotale(ws, "Mese").IsEmpty(), Is.True);
        });
    }

    [Test]
    public void PrimoSaldo_QuadratureVuota_NessunoSheet_LegendaDelPrimoSaldo()
    {
        var xlsx = RelCompleta().ReportFattureRel(SospeseComplete(), [], null, Mese, "PRIMO SALDO", []);

        Assert.Multiple(() =>
        {
            Assert.That(Fogli(xlsx), Does.Not.Contain("Quadrature Reg Esecuzione"), "Nessuna riga: lo sheet non si produce.");
            Assert.That(RigheLegenda(xlsx).Select(r => r.Nome), Does.Contain("Quadrature Reg Esecuzione"),
                "La legenda e' statica: elenca lo sheet anche quando nel file non c'e'.");
        });
    }

    [Test]
    public void AltriSaldiENonInviate_SenzaQuadrature_LegendaInvariata()
    {
        // Senza il parametro (SECONDO SALDO, VAR. SEMESTRALE, SEM. SOSPESI, report delle non inviate) nulla cambia.
        var xlsx = RelCompleta().ReportFattureRel(SospeseComplete(), [], null, Mese, "SECONDO SALDO");

        Assert.Multiple(() =>
        {
            Assert.That(Fogli(xlsx), Does.Not.Contain("Quadrature Reg Esecuzione"));
            Assert.That(RigheLegenda(xlsx).Select(r => r.Nome), Does.Not.Contain("Quadrature Reg Esecuzione"));
        });
    }

    [Test]
    public async Task ReportFatture_PrimoSaldo_InterrogaLaQuadraturaSulPeriodoEGliEnti()
    {
        var m = Mediator("PRIMO SALDO", []);
        RelTestataQueryGetByListaEntiQuadratura? inviata = null;
        m.Setup(x => x.Send(It.IsAny<RelTestataQueryGetByListaEntiQuadratura>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<RelTestataQuadraturaDto?>, CancellationToken>((q, _) => inviata = (RelTestataQueryGetByListaEntiQuadratura)q)
            .ReturnsAsync(new RelTestataQuadraturaDto { Quadratura = Quadrature() });
        var request = new FatturaRicercaRequest { Anno = 2026, Mese = 2, TipologiaFattura = ["PRIMO SALDO"], IdEnti = ["E1", "E2"] };

        var reports = await request.ReportFatture(m.Object, AdminAuth());

        Assert.Multiple(() =>
        {
            Assert.That(Fogli(reports.Single().Value), Does.Contain("Quadrature Reg Esecuzione"));
            Assert.That(inviata, Is.Not.Null);
            Assert.That((inviata!.Anno, inviata.Mese, inviata.TipologiaFattura), Is.EqualTo((2026, 2, "PRIMO SALDO")));
            Assert.That(inviata.EntiIds, Is.EqualTo(new[] { "E1", "E2" }), "Stessi enti del report.");
            Assert.That(inviata.Page, Is.Null, "Senza paginazione: tutte le righe del periodo.");
        });
    }

    [TestCase("SECONDO SALDO")]
    [TestCase("VAR. SEMESTRALE")]
    [TestCase("SEM. SOSPESI")]
    public async Task ReportFatture_AltriSaldi_NonInterroganoLaQuadratura(string tipologia)
    {
        var m = Mediator(tipologia, []);

        var reports = await request(tipologia).ReportFatture(m.Object, AdminAuth());

        Assert.That(Fogli(reports.Single().Value), Does.Not.Contain("Quadrature Reg Esecuzione"));
        m.Verify(x => x.Send(It.IsAny<RelTestataQueryGetByListaEntiQuadratura>(), It.IsAny<CancellationToken>()), Times.Never,
            "La voce 5 della checklist riguarda solo il PRIMO SALDO.");

        static FatturaRicercaRequest request(string t) => new() { Anno = 2026, Mese = 2, TipologiaFattura = [t] };
    }

    // ------------------------------------------------------------------ PF-1029: totali in Non Fatturate (voce 11)

    private static GestioneFattureReportExcelDto NonFatturata(decimal imponibile, int analogiche, int anno = 2026, int mese = 2) => new()
    {
        Stato = "POSTICIPATA", IdEnte = "E1", RagioneSociale = "Ente", TipologiaFattura = "PRIMO SALDO", NumeroFattura = 1001,
        Anno = anno, Mese = mese, TotaleNotificheAnalogiche = analogiche, TotaleImponibile = imponibile, TotaleFatturaImponibile = imponibile
    };

    public static IEnumerable<TestCaseData> ReportConNonFatturateTotali()
    {
        var righe = new[] { NonFatturata(100.25m, 3), NonFatturata(50.75m, 2, 2025, 7) };
        yield return new TestCaseData((Func<byte[]>)(() => RelCompleta().ReportFattureRel(SospeseComplete(), [], righe, Mese, "SECONDO SALDO")), "Non Fatturate")
            .SetName("NonFatturate_Totali_Saldi");
        yield return new TestCaseData((Func<byte[]>)(() => SeiGruppiCommessa().ReportFattureModuloCommessa(Mese, righe)), "Non Fatturate FEBBRAIO")
            .SetName("NonFatturate_Totali_Anticipo");
        yield return new TestCaseData((Func<byte[]>)(() => AccontoCompleto().ReportFattureAcconto(Mese, righe)), "Non Fatturate FEBBRAIO")
            .SetName("NonFatturate_Totali_Acconto");
        yield return new TestCaseData((Func<byte[]>)(() => AccontoCompleto().ReportFattureSospeseAcconto(Mese, righe)), "Non Fatturate FEBBRAIO")
            .SetName("NonFatturate_Totali_AccontoSospeso");
    }

    [TestCaseSource(nameof(ReportConNonFatturateTotali))]
    public void NonFatturate_RigaTotali_ConSubtotalSuiSoliImporti(Func<byte[]> genera, string sheet)
    {
        using var wb = new XLWorkbook(new MemoryStream(genera()));
        var ws = wb.Worksheet(sheet);

        Assert.Multiple(() =>
        {
            var imponibile = CellaTotale(ws, "Totale Imponibile");
            Assert.That(imponibile.FormulaA1, Is.EqualTo($"SUBTOTAL(9,{imponibile.Address.ColumnLetter}2:{imponibile.Address.ColumnLetter}3)"),
                "Solo le righe dati (2-3), non la riga vuota ne' quella dei totali.");
            Assert.That(imponibile.CachedValue.GetNumber(), Is.EqualTo(151d));
            Assert.That(CellaTotale(ws, "Totale Notifiche Analogiche").CachedValue.GetNumber(), Is.EqualTo(5d));
            Assert.That(CellaTotale(ws, "Totale Fattura Imponibile").FormulaA1, Does.StartWith("SUBTOTAL(9,"));
            // Anno, Mese e numero fattura non sono importi: sommarli darebbe numeri senza senso.
            Assert.That(CellaTotale(ws, "Anno").IsEmpty(), Is.True, "Anno non si somma.");
            Assert.That(CellaTotale(ws, "Mese").IsEmpty(), Is.True, "Mese non si somma.");
            Assert.That(CellaTotale(ws, "Num. Fattura").IsEmpty(), Is.True, "Il numero fattura non si somma.");
        });
    }

    [Test]
    public void NonFatturate_ConTotali_ColonneAncoraRidimensionate()
    {
        // L'autofit (PF-753) resta attivo anche con la riga dei totali: "Totali:" sta nella colonna Stato.
        var righe = new[] { NonFatturata(1m, 1) };
        righe[0].Note = new string('n', 60);
        using var wb = new XLWorkbook(new MemoryStream(RelCompleta().ReportFattureRel(SospeseComplete(), [], righe, Mese, "SECONDO SALDO")));
        var ws = wb.Worksheet("Non Fatturate");
        var note = ws.Row(1).CellsUsed().Single(c => c.GetString() == "Note").Address.ColumnNumber;

        Assert.That(ws.Column(note).Width, Is.GreaterThan("Note".Length + 10), "Larghezza sul contenuto, non sull'intestazione.");
    }

    // ------------------------------------------------------------------ PF-1029: SUBTOTAL su tutte le sheet

    private static RelNonFatturataDto RelNf(decimal totale, decimal iva) => new()
    {
        IdEnte = "E1", RagioneSociale = "Ente", TipologiaFattura = "PRIMO SALDO", TipoContratto = "PAC", Anno = 2026, Mese = 2,
        Totale = totale, TotaleIva = iva
    };

    private static ReportAndamentoCreditoSospesoExcelDto Andamento(decimal imponibile, decimal cumulato) => new()
    {
        IdEnte = "E1", RagioneSociale = "Ente", TipologiaFattura = "PRIMO SALDO", NumFatturaSospesa = 7001, Anno = 2026, Mese = 2,
        ImponibileFattura = imponibile, CreditoCumulato = cumulato, AnnoREL = 2026, MeseREL = 2
    };

    [Test]
    public void PacNonFirmate_RigaTotali_ImponibileEIvato()
    {
        var xlsx = RelCompleta().ReportFattureRel(SospeseComplete(), [RelNf(100m, 122m), RelNf(50.5m, 61.61m)], null, Mese, "SECONDO SALDO");
        using var wb = new XLWorkbook(new MemoryStream(xlsx));
        var ws = wb.Worksheet("Reg. Esec. PAC non firmate");

        Assert.Multiple(() =>
        {
            Assert.That(CellaTotale(ws, "Totale Imponibile €").FormulaA1, Does.StartWith("SUBTOTAL(9,"));
            Assert.That(CellaTotale(ws, "Totale Imponibile €").CachedValue.GetNumber(), Is.EqualTo(150.5d));
            Assert.That(CellaTotale(ws, "Totale Ivato €").CachedValue.GetNumber(), Is.EqualTo(183.61d).Within(0.001));
            Assert.That(CellaTotale(ws, "Anno").IsEmpty(), Is.True);
            Assert.That(CellaTotale(ws, "Mese").IsEmpty(), Is.True);
        });
    }

    [Test]
    public void ReportRelNonFatturate_RigaTotali_ImponibileEIvato()
    {
        using var memory = new[] { RelNf(10m, 12.2m), RelNf(20m, 24.4m) }.ReportRelNonFatturate().ToExcel();
        using var wb = new XLWorkbook(new MemoryStream(memory.ToArray()));
        var ws = wb.Worksheet("Reg Esecuzioni non fatturate");

        Assert.Multiple(() =>
        {
            Assert.That(CellaTotale(ws, "Totale Imponibile €").FormulaA1, Is.EqualTo($"SUBTOTAL(9,{CellaTotale(ws, "Totale Imponibile €").Address.ColumnLetter}2:{CellaTotale(ws, "Totale Imponibile €").Address.ColumnLetter}3)"));
            Assert.That(CellaTotale(ws, "Totale Imponibile €").CachedValue.GetNumber(), Is.EqualTo(30d));
            Assert.That(CellaTotale(ws, "Anno").IsEmpty(), Is.True);
        });
    }

    [Test]
    public void ReportAndamento_RigaTotali_SoloImponibile_NonIlCreditoCumulato()
    {
        using var memory = new[] { Andamento(5m, 5m), Andamento(3m, 8m) }.ReportAndamentoCreditoSospeso().ToExcel();
        using var wb = new XLWorkbook(new MemoryStream(memory.ToArray()));
        var ws = wb.Worksheet("Andamento Credito Sosp.");

        Assert.Multiple(() =>
        {
            Assert.That(CellaTotale(ws, "Imponibile Fattura €").FormulaA1, Does.StartWith("SUBTOTAL(9,"));
            Assert.That(CellaTotale(ws, "Imponibile Fattura €").CachedValue.GetNumber(), Is.EqualTo(8d));
            Assert.That(CellaTotale(ws, "Credito Cumulato €").IsEmpty(), Is.True,
                "Il credito cumulato e' un saldo progressivo: sommarlo (5 + 8 = 13) darebbe un numero senza significato.");
            foreach (var colonna in new[] { "Num. Fattura Sospesa", "Anno", "Mese", "Anno REL", "Mese REL" })
                Assert.That(CellaTotale(ws, colonna).IsEmpty(), Is.True, $"{colonna} non si somma.");
        });
    }

    /// <summary>
    /// "SUBTOTAL su tutte le sheet di tutti i report" (checklist): ogni foglio dati di ogni report ha la riga "Totali:"
    /// con almeno una formula SUBTOTAL. Escluse la Legenda (solo testo) e "Dett Fatt Sosp", che ha gia' una riga
    /// "totale:" per fattura (un totale di colonna la conterebbe due volte).
    /// </summary>
    [TestCaseSource(nameof(ReportConLegenda))]
    public void TutteLeSheet_ConDati_HannoRigaTotaliConSubtotal(Func<byte[]> genera, IReadOnlyList<ReportLegende.Voce> _, string __)
    {
        using var wb = new XLWorkbook(new MemoryStream(genera()));
        var fogli = wb.Worksheets.Where(w => w.Name != ReportLegende.SheetName && !w.Name.StartsWith("Dett Fatt Sosp")).ToList();

        Assert.That(fogli, Is.Not.Empty);
        Assert.Multiple(() =>
        {
            foreach (var ws in fogli)
            {
                var conDati = ws.RowsUsed().Skip(1).Any(r => r.Cell(1).GetString() != "Totali:");
                if (!conDati) continue;
                var totali = ws.RowsUsed().SingleOrDefault(r => r.Cell(1).GetString() == "Totali:");
                Assert.That(totali, Is.Not.Null, $"'{ws.Name}' senza riga Totali:");
                Assert.That(totali?.CellsUsed().Any(c => c.HasFormula && c.FormulaA1.StartsWith("SUBTOTAL(9,")), Is.True,
                    $"'{ws.Name}': la riga Totali: deve usare SUBTOTAL.");
            }
        });
    }

    [Test]
    public void AddTotali_SommaSoloLeColonneIndicate()
    {
        var table = new[] { RelNf(1m, 2m), RelNf(3m, 4m) }.FillOneTable("T").AddTotali(nameof(RelNonFatturataDto.Totale));
        var totali = table.Rows[^1];

        Assert.Multiple(() =>
        {
            Assert.That(table.Rows.Count, Is.EqualTo(4), "Due righe dati, la riga vuota, la riga Totali:.");
            Assert.That(totali[0], Is.EqualTo("Totali:"));
            Assert.That(totali[nameof(RelNonFatturataDto.Totale)], Is.EqualTo(4m));
            Assert.That(totali[nameof(RelNonFatturataDto.TotaleIva)], Is.EqualTo(DBNull.Value), "Non richiesta: resta vuota.");
            Assert.That(table.ExtendedProperties[ExcelExtensions.SubtotalRowProperty], Is.True);
        });
    }

    [Test]
    public void AddTotali_ColonnaAssenteONonNumerica_Eccezione()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => new[] { RelNf(1m, 1m) }.FillOneTable("T").AddTotali("NonEsiste"));
            Assert.Throws<ArgumentException>(() => new[] { RelNf(1m, 1m) }.FillOneTable("T").AddTotali(nameof(RelNonFatturataDto.RagioneSociale)));
        });
    }

    [Test]
    public void AddTotali_SenzaRigheDati_NessunaFormula()
    {
        using var memory = Array.Empty<ReportAndamentoCreditoSospesoExcelDto>().ReportAndamentoCreditoSospeso().ToExcel();
        using var wb = new XLWorkbook(new MemoryStream(memory.ToArray()));

        Assert.That(wb.Worksheet(ReportLegende.SheetAndamentoCreditoSospeso).CellsUsed().Any(c => c.HasFormula), Is.False,
            "Senza righe dati l'intervallo comprenderebbe l'intestazione: restano i valori statici, come negli altri sheet.");
    }

    [Test]
    public void DettaglioSospese_SenzaRigaTotali_PerNonContareDueVolte()
    {
        // Decisione del 23/11/2026: ogni fattura ha gia' la sua riga "totale:" e lo sheet resta senza totale di colonna. Se si
        // cambia idea, una somma semplice conterebbe ogni importo due volte (aggiornare questo test).
        var xlsx = SospeseComplete().ReportFattureSospeseRel(DettaglioSospese(), Mese, "PRIMO SALDO");
        using var wb = new XLWorkbook(new MemoryStream(xlsx));

        Assert.That(wb.Worksheet($"Dett Fatt Sosp {Mese}").CellsUsed().Any(c => c.GetString() == "Totali:"), Is.False);
    }
}
