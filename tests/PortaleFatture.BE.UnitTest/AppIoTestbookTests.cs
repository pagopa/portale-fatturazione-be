using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using PortaleFatture.BE.Api.Modules.AppIO.Contratti.Extensions;
using PortaleFatture.BE.Api.Modules.AppIO.Contratti.Request;
using PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Extensions;
using PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Dto;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Dto;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Extensions;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Il testbook APP IO (PF-550_TESTBOOK_APP-IO, foglio "TESTBOOK Admin") a livello di unità: per ogni
/// caso, la logica del backend che non dipende dal DB — come un filtro della pagina diventa una
/// query, come le righe diventano griglia e dettaglio, cosa finisce negli Excel. Il nome di ogni
/// test inizia con l'ID del caso (T01…T25).
///
/// L'esecuzione sulle rotte reali è in Http/AppIoTestbookHttpTests (DB seedato), gli input ostili in
/// Http/AppIoTestbookAdversarialHttpTests. T26 (pulsante Indietro) è solo UI e non ha test.
/// </summary>
[Category("AppIO")]
[Category("TestbookAppIO")]
public class AppIoTestbookTests
{
    private static AuthenticationInfo Admin() => new() { Id = "utente-test", Prodotto = "prod-appio", Ruolo = Ruolo.ADMIN };

    private sealed class Filtro : IAppIoFinancialReportFiltro
    {
        public string[]? InstitutionIds { get; init; }
        public string[]? CodiciFiscali { get; init; }
        public string[]? YearQuarter { get; init; }
        public string? Year { get; init; }
    }

    /// <summary>Intestazioni e celle (come testo) del primo foglio di un export.</summary>
    private static (List<string> Intestazioni, List<List<Cell>> Righe) Leggi(MemoryStream excel)
    {
        using var doc = SpreadsheetDocument.Open(excel, false);
        var righe = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Descendants<Row>().ToList();
        return (righe[0].Elements<Cell>().Select(c => c.CellValue!.Text).ToList(),
                righe.Skip(1).Select(r => r.Elements<Cell>().ToList()).ToList());
    }

    private static AppIoFinancialReportRigaDto Riga(string contratto, string ente, int anno, int trimestre, string numero, decimal importo, decimal totale) => new()
    {
        ContractId = contratto, InstitutionId = ente, Name = "Ente " + ente, Prodotto = "prod-io", CodiceFiscale = "CF-" + ente,
        Year = anno, Quarter = trimestre, YearQuarter = $"{anno}_{trimestre}", ImportoTotale = totale,
        Numero = numero, Data = new DateTime(anno, 1, 15), CodiceArticolo = "ART-A", Quantita = 100, Importo = importo
    };

    // ===================================================================== Anagrafica AppIO

    /// <summary>
    /// T01 — Senza filtri la griglia chiede tutti i contratti: la richiesta vuota diventa una query
    /// senza filtri, con la paginazione della pagina.
    /// </summary>
    [Test]
    public void T01_NessunFiltro_ShouldDiventareUnaQuerySenzaFiltriPaginata()
    {
        var query = new AppIoContrattiRequest().Map(Admin(), page: 1, pageSize: 10);

        Assert.Multiple(() =>
        {
            Assert.That(query.InstitutionIds, Is.Null);
            Assert.That(query.TipiEnte, Is.Null);
            Assert.That(query.Page, Is.EqualTo(1));
            Assert.That(query.Size, Is.EqualTo(10));
        });
    }

    /// <summary>
    /// T03, T04, T05 — Il profilo scelto arriva alla query così com'è, uno o più valori.
    /// </summary>
    [TestCase("PA")]
    [TestCase("GSP")]
    [TestCase("PSP")]
    [TestCase("PA,GSP,PSP")]
    public void T03_T04_T05_TipoProfilo_ShouldArrivareAllaQuery(string profili)
    {
        var tipi = profili.Split(',');
        var query = new AppIoContrattiRequest { TipiEnte = tipi }.Map(Admin(), 1, 10);

        Assert.Multiple(() =>
        {
            Assert.That(query.TipiEnte, Is.EqualTo(tipi));
            Assert.That(query.InstitutionIds, Is.Null, "nessun filtro sull'ente");
        });
    }

    /// <summary>
    /// T06 — Il testo dell'autocomplete arriva alla query senza essere modificato (né maiuscole né
    /// spazi): il confronto lo fa il database.
    /// </summary>
    [TestCase("acquedotto")]
    [TestCase("ACQUEDOTTO")]
    [TestCase(" acquedotto ")]
    [TestCase("")]
    [TestCase(null)]
    public void T06_TestoDellaRicerca_ShouldArrivareInvariato(string? testo)
        => Assert.That(new AppIoContrattiNameRequest { Name = testo }.Map(Admin()).Name, Is.EqualTo(testo));

    /// <summary>
    /// T06, T07 — Ogni voce dell'autocomplete porta l'id dell'ente: è il valore che la pagina rimanda
    /// alla griglia per selezionarlo. Due enti omonimi restano due voci.
    /// </summary>
    [Test]
    public void T06_T07_VociDellAutocomplete_ShouldPortareLIdDellEnte()
    {
        AppIoContrattoNome[] nomi =
        [
            new() { InstitutionId = "E1", Name = "ACQUEDOTTO TESTBOOK S.P.A." },
            new() { InstitutionId = "E2", Name = "Comune Alfa" },
            new() { InstitutionId = "E3", Name = "Comune Alfa" }
        ];

        var voci = nomi.Map().ToList();
        var query = new AppIoContrattiRequest { InstitutionIds = [voci[0].InstitutionId!] }.Map(Admin(), 1, 10);

        Assert.Multiple(() =>
        {
            Assert.That(voci.Select(x => x.Name), Is.EqualTo(new[] { "ACQUEDOTTO TESTBOOK S.P.A.", "Comune Alfa", "Comune Alfa" }));
            Assert.That(voci.Select(x => x.InstitutionId), Is.EqualTo(new[] { "E1", "E2", "E3" }));
            Assert.That(query.InstitutionIds, Is.EqualTo(new[] { "E1" }));
        });
    }

    /// <summary>
    /// T08 — Profilo ed ente insieme arrivano entrambi alla query (la persistence li mette in AND).
    /// </summary>
    [Test]
    public void T08_TipoProfiloPiuEnte_ShouldArrivareEntrambiAllaQuery()
    {
        var query = new AppIoContrattiRequest { TipiEnte = ["GSP"], InstitutionIds = ["E1"] }.Map(Admin(), 1, 10);

        Assert.Multiple(() =>
        {
            Assert.That(query.TipiEnte, Is.EqualTo(new[] { "GSP" }));
            Assert.That(query.InstitutionIds, Is.EqualTo(new[] { "E1" }));
        });
    }

    /// <summary>
    /// T09 — Un filtro svuotato dalla pagina (lista vuota) vale come filtro assente, non come
    /// "nessun ente": altrimenti togliere l'ultimo ente selezionato svuoterebbe la griglia.
    /// </summary>
    [Test]
    public void T09_FiltroSvuotato_ShouldValereComeAssente()
    {
        var query = new AppIoContrattiRequest { TipiEnte = [], InstitutionIds = [] }.Map(Admin(), 1, 10);

        Assert.Multiple(() =>
        {
            Assert.That(query.TipiEnte, Is.Null);
            Assert.That(query.InstitutionIds, Is.Null);
        });
    }

    /// <summary>
    /// T10, T11 — L'export dei contratti: sette colonne in quest'ordine, una riga per contratto, senza
    /// paginazione. Un contratto senza PEC ha la cella vuota.
    /// </summary>
    [Test]
    public void T10_T11_ExportContratti_ShouldAvereLeSetteColonneEUnaRigaPerContratto()
    {
        AppIoContratto[] contratti =
        [
            new() { ContractId = "C1", InstitutionId = "E1", Name = "ACQUEDOTTO TESTBOOK S.P.A.", Prodotto = "prod-io", InstitutionType = "GSP", EmailPec = "a@pec.test", ContractState = "ACTIVE" },
            new() { ContractId = "C2", InstitutionId = "E2", Name = "Regione Gamma", Prodotto = "prod-io", InstitutionType = "PA", EmailPec = null, ContractState = "CLOSED" }
        ];

        var (intestazioni, righe) = Leggi(contratti.FillPagoPAOneSheet("Contratti AppIO").ToExcel());

        Assert.Multiple(() =>
        {
            Assert.That(intestazioni, Is.EqualTo(new[] { "ContractId", "InstitutionId", "Name", "Prodotto", "InstitutionType", "EmailPec", "ContractState" }));
            Assert.That(righe, Has.Count.EqualTo(2));
            Assert.That(righe[0].Select(c => c.CellValue!.Text), Is.EqualTo(new[] { "C1", "E1", "ACQUEDOTTO TESTBOOK S.P.A.", "prod-io", "GSP", "a@pec.test", "ACTIVE" }));
            Assert.That(righe[1][5].CellValue!.Text, Is.Empty, "PEC assente");
            Assert.That(righe[1][6].CellValue!.Text, Is.EqualTo("CLOSED"), "lo stato del contratto è nell'export");
        });
    }

    /// <summary>
    /// T12 — Colonne della griglia: Ente, ID Contratto ed E-Mail Ref. Fattura arrivano alla risposta,
    /// con count separato dalle righe della pagina.
    /// </summary>
    [Test]
    public void T12_ColonneDellaGriglia_ShouldArrivareAllaRisposta()
    {
        var dto = new AppIoContrattiListDto
        {
            Count = 42,
            Contratti = [new AppIoContratto { ContractId = "C1", InstitutionId = "E1", Name = "ACQUEDOTTO TESTBOOK S.P.A.", Prodotto = "prod-io", InstitutionType = "GSP", EmailPec = "a@pec.test", ContractState = "ACTIVE" }]
        };

        var risposta = dto.Map();
        var riga = risposta.Contratti!.Single();

        Assert.Multiple(() =>
        {
            Assert.That(risposta.Count, Is.EqualTo(42), "count sul totale, non sulla pagina");
            Assert.That(riga.Name, Is.EqualTo("ACQUEDOTTO TESTBOOK S.P.A."), "Ente");
            Assert.That(riga.ContractId, Is.EqualTo("C1"), "ID Contratto");
            Assert.That(riga.EmailPec, Is.EqualTo("a@pec.test"), "E-Mail Ref. Fattura");
            Assert.That(riga.InstitutionType, Is.EqualTo("GSP"));
            Assert.That(riga.ContractState, Is.EqualTo("ACTIVE"));
        });
    }

    // ================================================================== Documenti contabili

    /// <summary>
    /// T13 — Filtro Anno: gli anni arrivano alla pagina come stringhe, dal più recente, qualunque sia
    /// l'ordine di lettura.
    /// </summary>
    [Test]
    public void T13_FiltroAnno_ShouldEssereOrdinatoDalPiuRecente()
        => Assert.That(new[] { 2025, 2027, 2026 }.MapYears(), Is.EqualTo(new[] { "2027", "2026", "2025" }));

    /// <summary>
    /// T13 — Senza filtri la griglia chiede il trimestre più recente.
    /// </summary>
    [Test]
    public void T13_GrigliaSenzaFiltri_ShouldCercareIlTrimestrePiuRecente()
    {
        var query = new AppIoFinancialReportsRequest().Map(Admin());
        var (where, _) = AppIoFinancialReportSQLBuilder.Where(query);

        Assert.That(where, Does.Contain("SELECT TOP 1 trimestre").And.Contain("ORDER BY [year] DESC, [quarter] DESC"));
    }

    /// <summary>
    /// T14 — Anno = 2026: il filtro prende tutti i trimestri dell'anno (prefisso 'AAAA_').
    /// </summary>
    [Test]
    public void T14_Anno_ShouldFiltrareTuttiITrimestriDellAnno()
    {
        var (where, p) = AppIoFinancialReportSQLBuilder.Where(new AppIoFinancialReportsRequest { Year = "2026" }.Map(Admin()));

        Assert.Multiple(() =>
        {
            Assert.That(where, Is.EqualTo(" WHERE f.trimestre LIKE @Year + '[_]%'"));
            Assert.That(((IDictionary<string, object?>)p)["Year"], Is.EqualTo("2026"));
        });
    }

    /// <summary>
    /// T15 — Filtro Trimestre: il valore 'AAAA_T' della vista diventa l'etichetta "Qn" mostrata dalla
    /// pagina, in ordine di trimestre.
    /// </summary>
    [Test]
    public void T15_FiltroTrimestre_ShouldMostrareLEtichettaQn()
    {
        var voci = new[] { "2026_2", "2026_1", "2026_4" }.MapQuarters();

        Assert.Multiple(() =>
        {
            Assert.That(voci.Select(x => x.Quarter), Is.EqualTo(new[] { "Q1", "Q2", "Q4" }));
            Assert.That(voci.Select(x => x.Value), Is.EqualTo(new[] { "2026_1", "2026_2", "2026_4" }));
        });
    }

    /// <summary>
    /// T16 — Trimestre = Q1: alla query arriva il valore della voce ('2026_1'), e prevale sull'anno.
    /// </summary>
    [Test]
    public void T16_Trimestre_ShouldPrevalereSullAnno()
    {
        var (where, p) = AppIoFinancialReportSQLBuilder.Where(new AppIoFinancialReportsRequest { Year = "2026", Quarters = ["2026_1"] }.Map(Admin()));

        Assert.Multiple(() =>
        {
            Assert.That(where, Is.EqualTo(" WHERE f.trimestre IN @YearQuarter"));
            Assert.That(((IDictionary<string, object?>)p).Keys, Is.EqualTo(new[] { "YearQuarter" }), "l'anno non è un parametro quando c'è il trimestre");
        });
    }

    /// <summary>
    /// T17, T18 — Codice fiscale ed ente diventano ciascuno una condizione sulla propria colonna.
    /// </summary>
    [Test]
    public void T17_T18_CodiceFiscaleEdEnte_ShouldFiltrareLaPropriaColonna()
    {
        var (perCodice, _) = AppIoFinancialReportSQLBuilder.Where(new Filtro { Year = "2026", CodiciFiscali = ["CF1"] });
        var (perEnte, _) = AppIoFinancialReportSQLBuilder.Where(new Filtro { Year = "2026", InstitutionIds = ["E1"] });

        Assert.Multiple(() =>
        {
            Assert.That(perCodice, Does.EndWith("AND f.institution_taxcode IN @CodiciFiscali"));
            Assert.That(perEnte, Does.EndWith("AND f.internalistitutionid IN @InstitutionIds"));
        });
    }

    /// <summary>
    /// T19 — Combinazione di filtri: tutte le condizioni sono in AND, nessuna in OR.
    /// </summary>
    [Test]
    public void T19_CombinazioneFiltri_ShouldEssereInAnd()
    {
        var (where, p) = AppIoFinancialReportSQLBuilder.Where(new Filtro { Year = "2026", YearQuarter = ["2026_1"], InstitutionIds = ["E1"], CodiciFiscali = ["CF1"] });

        Assert.Multiple(() =>
        {
            Assert.That(where.Split(" AND "), Has.Length.EqualTo(3));
            Assert.That(where, Does.Not.Contain(" OR "));
            Assert.That(((IDictionary<string, object?>)p).Keys, Is.EquivalentTo(new[] { "YearQuarter", "InstitutionIds", "CodiciFiscali" }));
        });
    }

    /// <summary>
    /// T20 — Il download dei risultati usa lo stesso filtro della griglia ed esporta una riga per riga
    /// di documento, con ImportoTotale e Importo in formato valuta e senza colonne tecniche.
    /// </summary>
    [Test]
    public void T20_DownloadRisultati_ShouldUsareLoStessoFiltroEdEsportareLeRighe()
    {
        var richiesta = new AppIoFinancialReportsRequest { Year = "2026", Quarters = ["2026_1"], InstitutionIds = ["E1"], CodiciFiscali = ["CF1"] };
        var griglia = AppIoFinancialReportSQLBuilder.Where(richiesta.Map(Admin())).Where;
        var download = AppIoFinancialReportSQLBuilder.Where(richiesta.MapExcel(Admin())).Where;
        AppIoFinancialReportRigaDto[] righe = [Riga("C1", "E1", 2026, 1, "N1", 415.5m, 418.25m), Riga("C1", "E1", 2026, 1, "N2", 2.75m, 418.25m)];

        var (intestazioni, celle) = Leggi(righe.FillPagoPAOneSheet("Documenti Contabili").ToExcel());

        Assert.Multiple(() =>
        {
            Assert.That(download, Is.EqualTo(griglia));
            Assert.That(celle, Has.Count.EqualTo(2));
            Assert.That(intestazioni, Does.Contain("ImportoTotale").And.Contain("Importo").And.Contain("Numero"));
            Assert.That(intestazioni, Has.None.Contains("GoldUpdatedAt").And.None.Contains("FilePath"));
            foreach (var riga in celle)
            {
                Assert.That(riga[intestazioni.IndexOf("ImportoTotale")].StyleIndex!.Value, Is.EqualTo((uint)XCellStyle.StandardNumberDecimal));
                Assert.That(riga[intestazioni.IndexOf("Importo")].StyleIndex!.Value, Is.EqualTo((uint)XCellStyle.StandardNumberDecimal));
            }
        });
    }

    // ======================================================== Dettaglio documento contabile

    /// <summary>
    /// T21 — Selezione di un documento dalla griglia: ogni riga padre porta contratto e trimestre, che
    /// sono una richiesta di dettaglio valida.
    /// </summary>
    [Test]
    public void T21_RigaDellaGriglia_ShouldPortareIParametriDelDettaglio()
    {
        AppIoFinancialReportRigaDto[] righe =
        [
            Riga("C1", "E1", 2026, 2, "N1", 10m, 25m), Riga("C1", "E1", 2026, 2, "N2", 15m, 25m), Riga("C2", "E2", 2026, 2, "N3", 2m, 2m)
        ];

        var padri = righe.Raggruppa().Map().FinancialReports!.ToList();
        var richiesta = new AppIoFinancialReportsDettaglioRequest { ContractId = padri[0].ContractId, YearQuarter = padri[0].YearQuarter };
        var query = richiesta.Map(Admin());

        Assert.Multiple(() =>
        {
            Assert.That(padri.Select(x => x.Key), Is.EqualTo(new[] { "C1|2026_2", "C2|2026_2" }), "una riga per contratto e trimestre");
            Assert.That(padri[0].Posizioni!.Count(), Is.EqualTo(2));
            Assert.That(richiesta.IsValida(), Is.True);
            Assert.That(query.ContractId, Is.EqualTo("C1"));
            Assert.That(query.YearQuarter, Is.EqualTo("2026_2"));
        });
    }

    /// <summary>
    /// T21 — Senza contratto o senza trimestre non c'è un documento da aprire.
    /// </summary>
    [TestCase(null, "2026_2")]
    [TestCase("C1", null)]
    [TestCase("", "2026_2")]
    [TestCase("C1", "   ")]
    public void T21_RichiestaDiDettaglioIncompleta_ShouldNonEssereValida(string? contratto, string? trimestre)
        => Assert.That(new AppIoFinancialReportsDettaglioRequest { ContractId = contratto, YearQuarter = trimestre }.IsValida(), Is.False);

    private static AppIoFinancialReportDettaglioRigaDto RigaDettaglio(string numero, string articolo, long quantita, decimal importo, decimal totale) => new()
    {
        Name = "Comune Alfa", ContractId = "C1", Prodotto = "prod-io", DataCreazione = new DateTime(2025, 1, 10), PricingPlan = "FA",
        PartitaIva = "01234567890", CodiceFiscale = "80012345678", UltimoAggiornamento = new DateTime(2026, 3, 1),
        YearQuarter = "2026_2", Year = 2026, Quarter = 2, ImportoTotale = totale,
        Numero = numero, TipoDoc = "TD01", Valuta = "EUR", CodiceArticolo = articolo, Quantita = quantita, Importo = importo
    };

    /// <summary>
    /// T22 — Dati del documento: Ente, Partita IVA, Codice Fiscale, ID Contratto, Prodotto e Piano
    /// Tariffario vengono dalla testata, uguale su tutte le righe.
    /// </summary>
    [Test]
    public void T22_DatiDelDocumento_ShouldVenireDallaTestata()
    {
        var d = new[] { RigaDettaglio("N1", "ART-A", 1000, 10m, 25m), RigaDettaglio("N2", "ART-B", 500, 15m, 25m) }.Componi()!.Map();

        Assert.Multiple(() =>
        {
            Assert.That(d.Name, Is.EqualTo("Comune Alfa"), "Ente");
            Assert.That(d.PartitaIva, Is.EqualTo("01234567890"), "Partita IVA");
            Assert.That(d.CodiceFiscale, Is.EqualTo("80012345678"), "Codice Fiscale");
            Assert.That(d.ContractId, Is.EqualTo("C1"), "ID Contratto");
            Assert.That(d.Prodotto, Is.EqualTo("prod-io"), "Prodotto");
            Assert.That(d.PricingPlan, Is.EqualTo("FA"), "Piano Tariffario");
        });
    }

    /// <summary>
    /// T23 — Dati economici: Trimestre, Data Creazione e Ultimo Aggiornamento dalla testata; Importo
    /// Totale in formato valuta, cioè a due decimali.
    /// </summary>
    [TestCase("25.000000", "25.00")]
    [TestCase("418.255", "418.26")]
    [TestCase("2.754999", "2.75")]
    public void T23_DatiEconomici_ShouldAvereImportoTotaleADueDecimali(string totale, string atteso)
    {
        static decimal D(string s) => decimal.Parse(s, System.Globalization.CultureInfo.InvariantCulture);

        var d = new[] { RigaDettaglio("N1", "ART-A", 1000, 10m, D(totale)) }.Componi()!.Map();

        Assert.Multiple(() =>
        {
            Assert.That(d.YearQuarter, Is.EqualTo("2026_2"), "Trimestre");
            Assert.That(d.ImportoTotale, Is.EqualTo(D(atteso)), "Importo Totale");
            Assert.That(d.DataCreazione, Is.EqualTo(new DateTime(2025, 1, 10)), "Data Creazione");
            Assert.That(d.UltimoAggiornamento, Is.EqualTo(new DateTime(2026, 3, 1)), "Ultimo Aggiornamento");
        });
    }

    /// <summary>
    /// T24 — Sezione Posizioni: una posizione per riga, nell'ordine di lettura, con Codice Articolo,
    /// Quantità e Importo. L'importo di riga non viene arrotondato dal backend.
    /// </summary>
    [Test]
    public void T24_SezionePosizioni_ShouldAvereUnaPosizionePerRiga()
    {
        var d = new[] { RigaDettaglio("N1", "ART-A", 1000, 10.123456m, 25m), RigaDettaglio("N1", "ART-B", 500, 14.876544m, 25m) }.Componi()!.Map();
        var posizioni = d.Posizioni!.ToList();

        Assert.Multiple(() =>
        {
            Assert.That(posizioni.Select(p => p.CodiceArticolo), Is.EqualTo(new[] { "ART-A", "ART-B" }));
            Assert.That(posizioni.Select(p => p.Quantita), Is.EqualTo(new long?[] { 1000, 500 }));
            Assert.That(posizioni.Select(p => p.Importo), Is.EqualTo(new decimal?[] { 10.123456m, 14.876544m }));
            Assert.That(posizioni.Sum(p => p.Importo), Is.EqualTo(d.ImportoTotale));
        });
    }

    /// <summary>
    /// T24 — Un contratto senza righe nel trimestre non ha un dettaglio.
    /// </summary>
    [Test]
    public void T24_NessunaRiga_ShouldNonComporreUnDettaglio()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Array.Empty<AppIoFinancialReportDettaglioRigaDto>().Componi(), Is.Null);
            Assert.That(((IEnumerable<AppIoFinancialReportDettaglioRigaDto>?)null).Componi(), Is.Null);
        });
    }

    /// <summary>
    /// T25 — Download Financial Report, Excel del dettaglio: una riga per posizione, con ImportoTotale
    /// e Importo in formato valuta.
    /// </summary>
    [Test]
    public void T25_ExcelDelDettaglio_ShouldAvereGliImportiInFormatoValuta()
    {
        var righe = new[] { RigaDettaglio("N1", "ART-A", 1000, 415.5m, 418.25m), RigaDettaglio("N1", "ART-B", 500, 2.75m, 418.25m) };

        var (intestazioni, celle) = Leggi(righe.FillPagoPAOneSheet("Dettaglio").ToExcel());

        Assert.Multiple(() =>
        {
            Assert.That(celle, Has.Count.EqualTo(2));
            foreach (var colonna in new[] { "ImportoTotale", "Importo" })
                foreach (var riga in celle)
                    Assert.That(riga[intestazioni.IndexOf(colonna)].StyleIndex!.Value, Is.EqualTo((uint)XCellStyle.StandardNumberDecimal), colonna);
            Assert.That(celle[0][intestazioni.IndexOf("Importo")].CellValue!.Text, Is.EqualTo("415.5"), "il valore non è arrotondato né riscritto");
        });
    }

    /// <summary>
    /// T25 — Download Financial Report dal blob storage: il trimestre del documento aperto
    /// ('AAAA_T') diventa anno e trimestre della richiesta, e il file è consegnato con un nome
    /// costruito dal trimestre.
    /// </summary>
    [TestCase("2026_1", 2026, 1)]
    [TestCase("2025_4", 2025, 4)]
    public void T25_DownloadDalBlob_ShouldUsareAnnoETrimestreDelDocumento(string yearQuarter, int anno, int trimestre)
    {
        var parti = yearQuarter.Split('_');
        var richiesta = new AppIoFinancialReportsFileRequest { Year = int.Parse(parti[0]), Quarter = int.Parse(parti[1]) };
        var percorso = $"abfss://appioexport@account.dfs.core.windows.net/export/sap/{anno}/{trimestre}/appio-final_report-{anno}-q{trimestre}.csv";

        var query = richiesta.Map(Admin());

        Assert.Multiple(() =>
        {
            Assert.That(richiesta.IsValida(), Is.True);
            Assert.That(query.Year, Is.EqualTo(anno));
            Assert.That(query.Quarter, Is.EqualTo(trimestre));
            Assert.That(richiesta.FileName(percorso), Is.EqualTo($"financial-report-appio-{yearQuarter}.csv"));
            Assert.That(percorso.MimeType(), Is.EqualTo("text/csv"));
        });
    }
}
