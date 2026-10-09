using PortaleFatture.BE.Api.Modules.AppIO.Contratti.Extensions;
using PortaleFatture.BE.Api.Modules.AppIO.Contratti.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Dto;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Mapping delle rotte api/appio/contracts* (PF-908): request -> query, e risultati -> response.
/// Le persistence e le rotte sono coperte su DB seedato (AppIoContrattiQueryIntegrationTests,
/// Http/AppIoContrattiHttpTests); qui la sola logica in memoria, in particolare il trattamento
/// degli array vuoti (che decide se scatta il default sul trimestre più recente) e il parsing di
/// year_quarter nel formato 'AAAA_T'.
/// </summary>
public class AppIoContrattiExtensionsTests
{
    /// <summary>
    /// Informazioni di autenticazione fittizie di un ADMIN sul prodotto APP IO.
    /// </summary>
    private static AuthenticationInfo Auth() => new()
    {
        Id = "utente-test",
        Prodotto = "prod-appio",
        Ruolo = Ruolo.ADMIN
    };

    // --- request -> query ---

    /// <summary>
    /// Verifica che i filtri di ricerca vengano riportati correttamente nella query, insieme a paginazione e autenticazione.
    /// </summary>
    [Test]
    public void MapRicerca_ConFiltri_ShouldRiportarliConPaginazioneEAutenticazione()
    {
        var auth = Auth();
        var req = new AppIoContrattiRequest { ContractIds = ["C1", "C2"], Quarters = ["2026_1"] };

        var query = req.Map(auth, 2, 50);

        Assert.Multiple(() =>
        {
            Assert.That(query.ContractIds, Is.EqualTo(new[] { "C1", "C2" }));
            Assert.That(query.YearQuarter, Is.EqualTo(new[] { "2026_1" }));
            Assert.That(query.Page, Is.EqualTo(2));
            Assert.That(query.Size, Is.EqualTo(50));
            Assert.That(query.AuthenticationInfo, Is.SameAs(auth));
        });
    }

    /// <summary>
    /// Verifica che gli array vuoti vengano convertiti in null, così la persistence li tratta come "nessun filtro" e applica il default sul trimestre più recente.
    /// </summary>
    [Test]
    public void MapRicerca_ArrayVuoti_ShouldDiventareNull()
    {
        // un array vuoto deve valere "nessun filtro": la persistence applica il default sul
        // trimestre più recente solo se YearQuarter è null o vuoto, e un IN () sarebbe SQL invalido
        var query = new AppIoContrattiRequest { ContractIds = [], Quarters = [] }.Map(Auth());

        Assert.Multiple(() =>
        {
            Assert.That(query.ContractIds, Is.Null);
            Assert.That(query.YearQuarter, Is.Null);
        });
    }

    /// <summary>
    /// Verifica che la paginazione resti null se non specificata: senza Page e Size la persistence non applica OFFSET/FETCH e restituisce tutte le righe del filtro.
    /// </summary>
    [Test]
    public void MapRicerca_SenzaPaginazione_ShouldLasciarlaNull()
    {
        // è il caso del download, che esporta tutte le righe del filtro
        var query = new AppIoContrattiRequest().Map(Auth());

        Assert.Multiple(() =>
        {
            Assert.That(query.Page, Is.Null);
            Assert.That(query.Size, Is.Null);
        });
    }

    /// <summary>
    /// Verifica che l'anno venga riportato nelle query di griglia e nome, e che un anno vuoto o di soli
    /// spazi diventi null: altrimenti la persistence filtrerebbe su un LIKE ' [_]%' e non troverebbe nulla.
    /// </summary>
    [TestCase("2026", "2026")]
    [TestCase("", null)]
    [TestCase("   ", null)]
    [TestCase(null, null)]
    public void MapRicercaENome_Anno_ShouldRiportarloOVuotoNull(string? anno, string? atteso)
    {
        var ricerca = new AppIoContrattiRequest { Year = anno }.Map(Auth());
        var nome = new AppIoContrattiNameRequest { Name = "Comune", Year = anno }.Map(Auth());

        Assert.Multiple(() =>
        {
            Assert.That(ricerca.Year, Is.EqualTo(atteso));
            Assert.That(nome.Year, Is.EqualTo(atteso));
        });
    }

    /// <summary>
    /// Verifica che il mapping del nome e dei trimestri venga riportato correttamente nella query, e che un array vuoto di trimestri venga convertito in null.
    /// </summary>
    [Test]
    public void MapNome_ShouldRiportareNomeETrimestri_ArrayVuotoNull()
    {
        var conTrimestri = new AppIoContrattiNameRequest { Name = "Comune", Quarters = ["2025_4"] }.Map(Auth());
        var senzaTrimestri = new AppIoContrattiNameRequest { Name = "Comune", Quarters = [] }.Map(Auth());

        Assert.Multiple(() =>
        {
            Assert.That(conTrimestri.Name, Is.EqualTo("Comune"));
            Assert.That(conTrimestri.YearQuarter, Is.EqualTo(new[] { "2025_4" }));
            Assert.That(senzaTrimestri.YearQuarter, Is.Null);
        });
    }

    /// <summary>
    /// Verifica che l'anno venga riportato correttamente nella query.
    /// </summary>
    [Test]
    public void MapQuarters_ShouldRiportareLAnno()
    {
        var query = new AppIoContrattiQuartersRequest { Year = "2026" }.Map(Auth());

        Assert.That(query.Year, Is.EqualTo("2026"));
    }

    // --- trimestri e anni ---

    /// <summary>
    /// Verifica che i trimestri vengano mappati correttamente, ricavando il trimestre in formato 'Qn' e ordinando per valore.
    /// </summary>
    [Test]
    public void MapTrimestri_ShouldRicavareQEOrdinarePerValore()
    {
        var response = new[] { "2026_2", "2025_4", "2026_1" }.Map();

        Assert.Multiple(() =>
        {
            Assert.That(response.Select(x => x.Value), Is.EqualTo(new[] { "2025_4", "2026_1", "2026_2" }));
            Assert.That(response.Select(x => x.Quarter), Is.EqualTo(new[] { "Q4", "Q1", "Q2" }));
        });
    }

    /// <summary>
    /// Verifica che un valore senza underscore venga mappato come 'Q' seguito dall'intero valore, senza generare eccezioni.
    /// </summary>
    [Test]
    public void MapTrimestri_ValoreSenzaUnderscore_ShouldProdurreQSeguitoDallInteroValore_Caratterizzazione()
    {
        // Il formato atteso è 'AAAA_T'. Se arrivasse un valore senza '_', IndexOf restituisce -1 e
        // il trimestre diventa l'intera stringa: nessuna eccezione, ma un'etichetta senza senso.
        // Fissa il comportamento attuale; se il formato reale dovesse prevederlo, va gestito.
        var response = new[] { "2026" }.Map();

        Assert.That(response.Single().Quarter, Is.EqualTo("Q2026"));
    }

    /// <summary>
    /// Verifica che gli anni vengano mappati correttamente, restituendo solo gli anni distinti e ordinati dal più recente al meno recente.
    /// </summary>
    [Test]
    public void MapYears_ShouldRestituireAnniDistintiDalPiuRecente()
    {
        var anni = new[] { "2025_4", "2026_1", "2026_2", "2024_3" }.MapYears();

        Assert.That(anni, Is.EqualTo(new[] { "2026", "2025", "2024" }));
    }

    /// <summary>
    /// Verifica che un elenco vuoto di trimestri restituisca una lista vuota di anni, senza generare eccezioni.
    /// </summary>
    [Test]
    public void MapYears_ElencoVuoto_ShouldRestituireListaVuota()
    {
        Assert.That(Array.Empty<string>().MapYears(), Is.Empty);
    }

    // --- risultati -> response ---
    /// <summary>
    /// Verifica che il mapping dei nomi dei contratti riporti tutte le proprietà correttamente nella response.
    /// </summary>
    [Test]
    public void MapNomi_ShouldRiportareTutteLeProprieta()
    {
        var response = new[]
        {
            new AppIoContrattoNome { ContractId = "C1", Name = "Comune Alfa", YearQuarter = "2026_2" }
        }.Map().Single();

        Assert.Multiple(() =>
        {
            Assert.That(response.ContractId, Is.EqualTo("C1"));
            Assert.That(response.Name, Is.EqualTo("Comune Alfa"));
            Assert.That(response.YearQuarter, Is.EqualTo("2026_2"));
        });
    }

    /// <summary>
    /// Verifica che il mapping della lista dei contratti riporti correttamente il count totale e tutte le colonne dei contratti nella response.
    /// </summary>
    [Test]
    public void MapLista_ShouldRiportareCountETutteLeColonne()
    {
        // Il count è il totale, non la lunghezza della pagina: la persistence può restituire un numero maggiore di contratti rispetto a quelli presenti nella pagina.
        var dto = new AppIoContrattiListDto
        {
            Count = 42,
            Contratti =
            [
                new AppIoContratto
                {
                    ContractId = "C1", Name = "Comune Alfa", TaxCode = "TAX", VatCode = "VAT",
                    VatGroup = 1m, SdiCode = "SDI1234", YearMonth = "202607", YearQuarter = "2026_2"
                }
            ]
        };

        // Il mapping deve riportare tutte le proprietà dei contratti, e il count totale.
        var response = dto.Map();

        // La response deve avere un solo contratto, e il count totale deve essere 42.
        var c = response.Contratti!.Single();

        // Verifica tutte le proprietà del contratto e il count totale.
        Assert.Multiple(() =>
        {
            Assert.That(response.Count, Is.EqualTo(42), "il count è il totale, non la lunghezza della pagina");
            Assert.That(c.ContractId, Is.EqualTo("C1"));
            Assert.That(c.Name, Is.EqualTo("Comune Alfa"));
            Assert.That(c.TaxCode, Is.EqualTo("TAX"));
            Assert.That(c.VatCode, Is.EqualTo("VAT"));
            Assert.That(c.VatGroup, Is.EqualTo(1m));
            Assert.That(c.SdiCode, Is.EqualTo("SDI1234"));
            Assert.That(c.YearMonth, Is.EqualTo("202607"));
            Assert.That(c.YearQuarter, Is.EqualTo("2026_2"));
        });
    }

    /// <summary>
    /// Verifica che il mapping della lista dei contratti gestisca correttamente il caso in cui la lista dei contratti sia null, restituendo una response con contratti null e count zero.
    /// </summary>
    [Test]
    public void MapLista_ContrattiNull_ShouldRestituireContrattiNull()
    {
        // La persistence restituisce una lista vuota, non null: il caso null è difensivo, e il mapping non deve sollevare eccezioni.
        var response = new AppIoContrattiListDto { Count = 0, Contratti = null }.Map();

        // La response deve riportare contratti null e count zero, non una lista vuota.
        Assert.Multiple(() =>
        {
            Assert.That(response.Contratti, Is.Null);
            Assert.That(response.Count, Is.Zero);
        });
    }
}
