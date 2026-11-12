using PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Extensions;
using PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Dto;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Extensions;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries.Persistence.Builder;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Documenti emessi APP IO (modello dati dell'11/11/2026, PF-1016): la logica che non dipende dal
/// DB — composizione del filtro, raggruppamento per contratto e trimestre, composizione del
/// dettaglio e mapping. L'esecuzione reale è coperta su DB seedato da Http/AppIoFinancialReportsHttpTests.
/// </summary>
[Category("AppIO")]
public class AppIoFinancialReportTests
{
    private sealed class Filtro : IAppIoFinancialReportFiltro
    {
        public string[]? InstitutionIds { get; init; }
        public string[]? CodiciFiscali { get; init; }
        public string[]? YearQuarter { get; init; }
        public string? Year { get; init; }
    }

    private static IDictionary<string, object?> Parametri(System.Dynamic.ExpandoObject p) => p;

    private static AuthenticationInfo Auth() => new() { Id = "utente-test", Prodotto = "prod-appio", Ruolo = Ruolo.ADMIN };

    // --- Where ---

    /// <summary>
    /// Senza trimestri né anno si cerca il trimestre più recente, ordinando per anno e trimestre numerici.
    /// </summary>
    [Test]
    public void Where_SenzaFiltri_ShouldCercareIlTrimestrePiuRecente()
    {
        var (where, p) = AppIoFinancialReportSQLBuilder.Where(new Filtro());

        Assert.Multiple(() =>
        {
            Assert.That(where, Is.EqualTo(" WHERE f.trimestre = (SELECT TOP 1 trimestre FROM [be].[vwAppioFinalReport] ORDER BY [year] DESC, [quarter] DESC)"));
            Assert.That(Parametri(p), Is.Empty);
        });
    }

    /// <summary>
    /// Con il solo anno si filtra sul prefisso 'AAAA_' di trimestre (trattino basso fra parentesi quadre).
    /// </summary>
    [Test]
    public void Where_SoloAnno_ShouldFiltrarePerPrefissoDellAnno()
    {
        var (where, p) = AppIoFinancialReportSQLBuilder.Where(new Filtro { Year = "2026" });

        Assert.Multiple(() =>
        {
            Assert.That(where, Is.EqualTo(" WHERE f.trimestre LIKE @Year + '[_]%'"));
            Assert.That(Parametri(p)["Year"], Is.EqualTo("2026"));
        });
    }

    /// <summary>
    /// I trimestri prevalgono sull'anno; enti e codici fiscali si aggiungono in AND, ognuno sulla sua colonna.
    /// </summary>
    [Test]
    public void Where_TuttiIFiltri_ShouldPrevalereITrimestriEAggiungereEntiECodiciFiscali()
    {
        var (where, p) = AppIoFinancialReportSQLBuilder.Where(new Filtro
        {
            YearQuarter = ["2026_1"],
            Year = "2025",
            InstitutionIds = ["E1"],
            CodiciFiscali = ["CF1"]
        });

        Assert.Multiple(() =>
        {
            Assert.That(where, Is.EqualTo(" WHERE f.trimestre IN @YearQuarter AND f.internalistitutionid IN @InstitutionIds AND f.institution_taxcode IN @CodiciFiscali"));
            Assert.That(Parametri(p).Keys, Is.EquivalentTo(new[] { "YearQuarter", "InstitutionIds", "CodiciFiscali" }));
        });
    }

    /// <summary>
    /// Le query leggono solo dalle viste be.vwAppioFinalReport*, mai dalle tabelle appio.
    /// </summary>
    [TestCase("SelectRighe", "[be].[vwAppioFinalReport] f")]
    [TestCase("SelectDettaglio", "[be].[vwAppioFinalReportDetails] d")]
    [TestCase("SelectQuarters", "[be].[vwAppioFinalReport]")]
    [TestCase("SelectYears", "[be].[vwAppioFinalReportFiltroAnno]")]
    [TestCase("SelectCodiciFiscali", "[be].[vwAppioFinalReportFiltroCodFiscaleEnte]")]
    public void Select_ShouldLeggereSoloDalleViste(string metodo, string vista)
    {
        var sql = (string)typeof(AppIoFinancialReportSQLBuilder).GetMethod(metodo)!.Invoke(null, null)!;

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain(vista));
            Assert.That(sql, Does.Not.Contain("appio."));
        });
    }

    /// <summary>
    /// Il filtro "Codice Fiscale" legge dalla vista filtro dedicata, in ordine crescente: la regola
    /// "solo enti con documenti" è della vista, il backend non la ricostruisce su altre viste.
    /// </summary>
    [Test]
    public void SelectCodiciFiscali_ShouldLeggereDallaVistaFiltroInOrdine()
    {
        var sql = AppIoFinancialReportSQLBuilder.SelectCodiciFiscali();

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("FROM [be].[vwAppioFinalReportFiltroCodFiscaleEnte]"));
            Assert.That(sql, Does.Not.Contain("JOIN"));
            Assert.That(sql, Does.Not.Contain("WHERE"), "documenti e NULL li filtra la vista");
            Assert.That(sql, Does.EndWith("ORDER BY [CodFiscaleEnte]"));
        });
    }

    /// <summary>
    /// La griglia legge gold_updated_at dalla vista ma non file_path: il percorso del file sul blob
    /// storage non deve arrivare al client.
    /// </summary>
    [Test]
    public void SelectRighe_ShouldLeggereGoldUpdatedAtENonFilePath()
    {
        var sql = AppIoFinancialReportSQLBuilder.SelectRighe();

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("f.[gold_updated_at] AS GoldUpdatedAt"));
            Assert.That(sql, Does.Not.Contain("file_path"));
        });
    }

    /// <summary>
    /// Raggruppando, gold_updated_at resta sulla singola riga di documento (posizione).
    /// </summary>
    [Test]
    public void Raggruppa_ShouldPortareGoldUpdatedAtSullePosizioni()
    {
        var aggiornato = new DateTime(2026, 7, 16, 8, 0, 0);
        AppIoFinancialReportRigaDto[] righe =
        [
            new() { ContractId = "C1", YearQuarter = "2026_2", Numero = "N1", GoldUpdatedAt = aggiornato },
            new() { ContractId = "C1", YearQuarter = "2026_2", Numero = "N2", GoldUpdatedAt = null }
        ];

        var posizioni = righe.Raggruppa().FinancialReports!.Single().Posizioni;

        Assert.That(posizioni.Select(x => x.GoldUpdatedAt), Is.EqualTo(new DateTime?[] { aggiornato, null }));
    }

    /// <summary>
    /// Né la risposta della griglia né i DTO espongono il percorso del file: nessuna proprietà
    /// FilePath, a nessun livello.
    /// </summary>
    [TestCase(typeof(PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Response.AppIoFinancialReportResponse))]
    [TestCase(typeof(PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Response.AppIoFinancialReportPosizioneResponse))]
    [TestCase(typeof(AppIoFinancialReportRigaDto))]
    public void Tipi_ShouldNonEsporreIlPercorsoDelFile(Type tipo)
        => Assert.That(tipo.GetProperties().Select(p => p.Name), Has.None.Contains("FilePath").IgnoreCase);

    /// <summary>
    /// Nella pagina di dettaglio ImportoTotale è in formato valuta: due decimali, arrotondando il
    /// mezzo centesimo per eccesso in valore assoluto; null resta null.
    /// </summary>
    [TestCase("25.000000", "25.00")]
    [TestCase("10.123456", "10.12")]
    [TestCase("10.125", "10.13")]
    [TestCase("-10.125", "-10.13")]
    [TestCase("0.004999", "0.00")]
    [TestCase(null, null)]
    public void MapDettaglio_ImportoTotale_ShouldAvereDueDecimali(string? valore, string? atteso)
    {
        static decimal? D(string? s) => s is null ? null : decimal.Parse(s, System.Globalization.CultureInfo.InvariantCulture);

        var risposta = new AppIoFinancialReportDettaglioDto { ImportoTotale = D(valore) }.Map();

        Assert.That(risposta.ImportoTotale, Is.EqualTo(D(atteso)));
    }

    // --- Raggruppa ---

    private static AppIoFinancialReportRigaDto Riga(string contratto, string trimestre, string numero, string articolo, decimal totale = 25m)
        => new()
        {
            ContractId = contratto,
            InstitutionId = "E-" + contratto,
            Name = "Ente " + contratto,
            CodiceFiscale = "CF-" + contratto,
            YearQuarter = trimestre,
            Year = int.Parse(trimestre[..4]),
            Quarter = int.Parse(trimestre[5..]),
            ImportoTotale = totale,
            Numero = numero,
            CodiceArticolo = articolo
        };

    /// <summary>
    /// Le righe dello stesso contratto e trimestre formano una sola riga padre, anche se sono di
    /// documenti (Numero) diversi: la testata viene dalla prima riga, l'ordine è conservato.
    /// </summary>
    [Test]
    public void Raggruppa_ShouldUnirePerContrattoETrimestre_AncheConNumeriDiversi()
    {
        var lista = new[]
        {
            Riga("C1", "2026_2", "N1", "A"),
            Riga("C1", "2026_2", "N1", "B"),
            Riga("C1", "2026_2", "N2", "A")
        }.Raggruppa();

        var padre = lista.FinancialReports!.Single();
        Assert.Multiple(() =>
        {
            Assert.That(lista.Count, Is.EqualTo(1));
            Assert.That(padre.Key, Is.EqualTo("C1|2026_2"));
            Assert.That(padre.InstitutionId, Is.EqualTo("E-C1"));
            Assert.That(padre.CodiceFiscale, Is.EqualTo("CF-C1"));
            Assert.That(padre.Year, Is.EqualTo(2026));
            Assert.That(padre.Quarter, Is.EqualTo(2));
            Assert.That(padre.ImportoTotale, Is.EqualTo(25m));
            Assert.That(padre.Posizioni.Select(x => x.Numero + x.CodiceArticolo), Is.EqualTo(new[] { "N1A", "N1B", "N2A" }));
        });
    }

    /// <summary>
    /// Lo stesso contratto in trimestri diversi, o contratti diversi nello stesso trimestre, sono
    /// righe padre distinte.
    /// </summary>
    [Test]
    public void Raggruppa_TrimestriOContrattiDiversi_ShouldEssereRighePadreDistinte()
    {
        var lista = new[]
        {
            Riga("C1", "2026_2", "N1", "A"),
            Riga("C1", "2026_1", "N1", "A"),
            Riga("C2", "2026_2", "N1", "A")
        }.Raggruppa();

        Assert.That(lista.FinancialReports!.Select(x => x.Key), Is.EqualTo(new[] { "C1|2026_2", "C1|2026_1", "C2|2026_2" }));
    }

    /// <summary>
    /// Nessuna riga (o null) dà una lista vuota con conteggio zero.
    /// </summary>
    [Test]
    public void Raggruppa_NessunaRiga_ShouldDareListaVuota()
    {
        var lista = ((IEnumerable<AppIoFinancialReportRigaDto>?)null).Raggruppa();

        Assert.Multiple(() =>
        {
            Assert.That(lista.Count, Is.Zero);
            Assert.That(lista.FinancialReports, Is.Empty);
        });
    }

    // --- Componi ---

    /// <summary>
    /// Il dettaglio prende la testata dalla prima riga e mette tutte le righe nelle posizioni.
    /// </summary>
    [Test]
    public void Componi_ShouldPrendereLaTestataDallaPrimaRigaETutteLeRighe()
    {
        var dettaglio = new[]
        {
            new AppIoFinancialReportDettaglioRigaDto { ContractId = "C1", YearQuarter = "2026_2", PartitaIva = "P1", ImportoTotale = 25m, Numero = "N1", DataRiferimento = null },
            new AppIoFinancialReportDettaglioRigaDto { ContractId = "C1", YearQuarter = "2026_2", PartitaIva = "P1", ImportoTotale = 25m, Numero = "N2" }
        }.Componi()!;

        Assert.Multiple(() =>
        {
            Assert.That(dettaglio.ContractId, Is.EqualTo("C1"));
            Assert.That(dettaglio.PartitaIva, Is.EqualTo("P1"));
            Assert.That(dettaglio.ImportoTotale, Is.EqualTo(25m));
            Assert.That(dettaglio.Posizioni.Select(x => x.Numero), Is.EqualTo(new[] { "N1", "N2" }));
        });
    }

    /// <summary>
    /// Nessuna riga dà null, così la rotta risponde 404.
    /// </summary>
    [Test]
    public void Componi_NessunaRiga_ShouldDareNull()
    {
        Assert.That(Array.Empty<AppIoFinancialReportDettaglioRigaDto>().Componi(), Is.Null);
    }

    // --- mapping delle request ---

    /// <summary>
    /// Array vuoti e anno vuoto valgono come filtro assente, così scatta il default sul trimestre più recente.
    /// </summary>
    [Test]
    public void MapRicerca_ArrayVuotiEAnnoVuoto_ShouldDiventareNull()
    {
        var query = new AppIoFinancialReportsRequest { InstitutionIds = [], CodiciFiscali = [], Quarters = [], Year = " " }.Map(Auth());

        Assert.Multiple(() =>
        {
            Assert.That(query.InstitutionIds, Is.Null);
            Assert.That(query.CodiciFiscali, Is.Null);
            Assert.That(query.YearQuarter, Is.Null);
            Assert.That(query.Year, Is.Null);
        });
    }

    /// <summary>
    /// Griglia e download ricevono lo stesso filtro dalla stessa richiesta.
    /// </summary>
    [Test]
    public void MapRicercaEMapExcel_ShouldRiportareGliStessiFiltri()
    {
        var req = new AppIoFinancialReportsRequest { InstitutionIds = ["E1"], CodiciFiscali = ["CF1"], Quarters = ["2026_2"], Year = "2026" };
        IAppIoFinancialReportFiltro griglia = req.Map(Auth());
        IAppIoFinancialReportFiltro excel = req.MapExcel(Auth());

        foreach (var f in new[] { griglia, excel })
            Assert.Multiple(() =>
            {
                Assert.That(f.InstitutionIds, Is.EqualTo(new[] { "E1" }));
                Assert.That(f.CodiciFiscali, Is.EqualTo(new[] { "CF1" }));
                Assert.That(f.YearQuarter, Is.EqualTo(new[] { "2026_2" }));
                Assert.That(f.Year, Is.EqualTo("2026"));
            });
    }

    /// <summary>
    /// Il dettaglio è valido solo con contratto e trimestre entrambi valorizzati.
    /// </summary>
    [TestCase("C1", "2026_2", true)]
    [TestCase("C1", null, false)]
    [TestCase(null, "2026_2", false)]
    [TestCase(" ", "2026_2", false)]
    public void IsValida_ShouldRichiedereContrattoETrimestre(string? contratto, string? trimestre, bool attesa)
    {
        Assert.That(new AppIoFinancialReportsDettaglioRequest { ContractId = contratto, YearQuarter = trimestre }.IsValida(), Is.EqualTo(attesa));
    }

    /// <summary>
    /// Una richiesta di dettaglio assente non è valida.
    /// </summary>
    [Test]
    public void IsValida_RichiestaNull_ShouldEssereFalse()
    {
        Assert.That(((AppIoFinancialReportsDettaglioRequest?)null).IsValida(), Is.False);
    }

    // --- mapping delle risposte ---

    /// <summary>
    /// Gli anni diventano stringhe, dal più recente.
    /// </summary>
    [Test]
    public void MapYears_ShouldDareStringheDalPiuRecente()
    {
        Assert.That(new[] { 2025, 2026 }.MapYears(), Is.EqualTo(new[] { "2026", "2025" }));
    }

    /// <summary>
    /// I trimestri diventano voci "Qn", ordinate per valore.
    /// </summary>
    [Test]
    public void MapQuarters_ShouldRicavareQEOrdinarePerValore()
    {
        var voci = new[] { "2026_2", "2026_1" }.MapQuarters();

        Assert.Multiple(() =>
        {
            Assert.That(voci.Select(x => x.Value), Is.EqualTo(new[] { "2026_1", "2026_2" }));
            Assert.That(voci.Select(x => x.Quarter), Is.EqualTo(new[] { "Q1", "Q2" }));
        });
    }

    /// <summary>
    /// La risposta della griglia riporta chiave, testata e posizioni.
    /// </summary>
    [Test]
    public void MapLista_ShouldRiportareChiaveTestataEPosizioni()
    {
        var resp = new[] { Riga("C1", "2026_2", "N1", "A") }.Raggruppa().Map();
        var padre = resp.FinancialReports!.Single();

        Assert.Multiple(() =>
        {
            Assert.That(resp.Count, Is.EqualTo(1));
            Assert.That(padre.Key, Is.EqualTo("C1|2026_2"));
            Assert.That(padre.ContractId, Is.EqualTo("C1"));
            Assert.That(padre.YearQuarter, Is.EqualTo("2026_2"));
            Assert.That(padre.ImportoTotale, Is.EqualTo(25m));
            Assert.That(padre.Posizioni!.Single().CodiceArticolo, Is.EqualTo("A"));
        });
    }
}
