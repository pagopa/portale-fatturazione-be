using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Lista contratti APP IO (PF-908): AppIoContrattiQueryGetByRicerca -> persistence ->
/// [be].[vwAppioContracts] sul DB seedato (tests/Data/appio.sql).
///
/// Seed: trimestre più recente 2026_2 con C1, C2, C3; C1 anche in 2026_1 e 2025_4; C4 solo in
/// 2025_4; C3 con vat_group e sdi_code NULL.
/// </summary>
public class AppIoContrattiQueryIntegrationTests
{
    private IMediator _handler = null!;

    /// <summary>
    /// Setup: skip test se il DB non è disponibile o se la view be.vwAppioContracts non esiste.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioContracts");
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
    }

    /// <summary>
    /// Crea una <strong>query con AuthenticationInfo fittizio</strong> (IdEnte random, Prodotto=prod-appio, Ruolo=ADMIN).
    /// </summary>
    /// <returns>Una nuova istanza di AppIoContrattiQueryGetByRicerca con AuthenticationInfo fittizio.</returns>
    private static AppIoContrattiQueryGetByRicerca Query() => new(new AuthenticationInfo
    {
        IdEnte = Guid.NewGuid().ToString(),
        Prodotto = "prod-appio",
        Ruolo = Ruolo.ADMIN
    });

    /// <summary>
    /// Testa il comportamento di default della <strong>query senza parametri</strong>: deve restituire i contratti del trimestre più recente (2026_2) ordinati per contract_id.
    /// </summary>
    /// <returns>Task che rappresenta l'operazione asincrona.</returns>
    [Test]
    public async Task SenzaTrimestre_ShouldRestituireIlTrimestrePiuRecente()
    {
        // Arrange & Act
        var result = await _handler.Send(Query());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Contratti!.Select(x => x.ContractId),
                Is.EqualTo(new[] { "APPIO-C1", "APPIO-C2", "APPIO-C3" }), "ordinati per contract_id");
            Assert.That(result.Contratti!.Select(x => x.YearQuarter), Is.All.EqualTo("2026_2"));
            Assert.That(result.Count, Is.EqualTo(3));
        });
    }

    /// <summary>
    /// Testa il comportamento della query quando viene <strong>specificato un filtro per trimestre</strong>: deve restituire solo i contratti dei trimestri richiesti.
    /// </summary>
    /// <returns>Task che rappresenta l'operazione asincrona.</returns>
    [Test]
    public async Task ConTrimestri_ShouldFiltrareSuQuelliRichiesti()
    {
        // Arrange
        var query = Query();
        query.YearQuarter = ["2025_4", "2026_1"];

        // Act
        var result = await _handler.Send(query);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Contratti!.Select(x => (x.ContractId, x.YearQuarter)), Is.EquivalentTo(new[]
            {
                ("APPIO-C1", "2025_4"), ("APPIO-C1", "2026_1"), ("APPIO-C4", "2025_4")
            }));
            Assert.That(result.Count, Is.EqualTo(3));
        });
    }

    /// <summary>
    /// Testa il comportamento della query quando viene <strong>specificato un filtro per contract_id</strong>: deve restituire solo i contratti richiesti, ma sempre filtrando sul trimestre più recente (2026_2).
    /// </summary>
    /// <returns>Task che rappresenta l'operazione asincrona.</returns>
    [Test]
    public async Task ConContractIds_ShouldFiltrareSulTrimestrePiuRecente()
    {
        var query = Query();
        query.ContractIds = ["APPIO-C2", "APPIO-C4"];

        var result = await _handler.Send(query);

        // C4 esiste solo in 2025_4: il filtro per contratto non scavalca il default sul trimestre
        Assert.Multiple(() =>
        {
            Assert.That(result.Contratti!.Select(x => x.ContractId), Is.EqualTo(new[] { "APPIO-C2" }));
            Assert.That(result.Count, Is.EqualTo(1));
        });
    }

    /// <summary>
    /// Testa il comportamento della query quando vengono <strong>specificati i parametri di paginazione</strong>: deve restituire solo la pagina richiesta, ma il count totale deve essere sul totale dei contratti filtrati (non sulla pagina).
    /// </summary>
    /// <returns>Task che rappresenta l'operazione asincrona.</returns>
    [Test]
    public async Task Paginazione_ShouldRestituireLaPaginaEIlCountTotale()
    {
        var query = Query();
        query.Page = 2;
        query.Size = 2;

        var result = await _handler.Send(query);

        Assert.Multiple(() =>
        {
            Assert.That(result.Contratti!.Select(x => x.ContractId), Is.EqualTo(new[] { "APPIO-C3" }));
            Assert.That(result.Count, Is.EqualTo(3), "il count è sul totale, non sulla pagina");
        });
    }

    /// <summary>
    /// Testa il comportamento della query quando vengono restituiti <strong>contratti con tutti i campi valorizzati e con campi null</strong>: deve popolare correttamente tutte le colonne e tollerare i valori null.
    /// </summary>
    /// <returns>Task che rappresenta l'operazione asincrona.</returns>
    [Test]
    public async Task Mapping_ShouldPopolareTutteLeColonne_ETollerareINull()
    {
        var result = await _handler.Send(Query());
        var c2 = result.Contratti!.Single(x => x.ContractId == "APPIO-C2");
        var c3 = result.Contratti!.Single(x => x.ContractId == "APPIO-C3");

        Assert.Multiple(() =>
        {
            Assert.That(c2.Name, Is.EqualTo("Comune Beta AppIO Test"));
            Assert.That(c2.TaxCode, Is.EqualTo("00000000002"));
            Assert.That(c2.VatCode, Is.EqualTo("00000000002"));
            Assert.That(c2.VatGroup, Is.EqualTo(1m));
            Assert.That(c2.SdiCode, Is.EqualTo("BBBBBB2"));
            Assert.That(c2.YearMonth, Is.EqualTo("202607"));
            Assert.That(c3.VatGroup, Is.Null);
            Assert.That(c3.SdiCode, Is.Null);
        });
    }

    /// <summary>
    /// Testa il comportamento della query quando viene <strong>specificato un trimestre senza dati</strong>: deve restituire una lista vuota e count=0.
    /// </summary>
    /// <returns>Task che rappresenta l'operazione asincrona.</returns>
    [Test]
    public async Task TrimestreSenzaDati_ShouldRestituireListaVuotaECountZero()
    {
        var query = Query();
        query.YearQuarter = ["1999_1"];

        var result = await _handler.Send(query);

        Assert.Multiple(() =>
        {
            Assert.That(result.Contratti, Is.Empty);
            Assert.That(result.Count, Is.EqualTo(0));
        });
    }

    // --- filtro per anno ---

    /// <summary>
    /// Con il solo anno la query restituisce tutti i trimestri di quell'anno, e il count è sul totale.
    /// </summary>
    /// <returns>Task che rappresenta l'operazione asincrona.</returns>
    [Test]
    public async Task ConAnno_ShouldRestituireTuttiITrimestriDellAnno()
    {
        var query = Query();
        query.Year = "2026";

        var result = await _handler.Send(query);

        Assert.Multiple(() =>
        {
            Assert.That(result.Contratti!.Select(x => (x.ContractId, x.YearQuarter)), Is.EquivalentTo(new[]
            {
                ("APPIO-C1", "2026_1"), ("APPIO-C1", "2026_2"), ("APPIO-C2", "2026_2"), ("APPIO-C3", "2026_2")
            }));
            Assert.That(result.Count, Is.EqualTo(4));
        });
    }

    /// <summary>
    /// I trimestri prevalgono sull'anno: con entrambi l'anno viene ignorato, anche se è di un altro anno.
    /// </summary>
    /// <returns>Task che rappresenta l'operazione asincrona.</returns>
    [Test]
    public async Task TrimestriEAnno_ShouldPrevalereITrimestri()
    {
        var query = Query();
        query.YearQuarter = ["2025_4"];
        query.Year = "2026";

        var result = await _handler.Send(query);

        Assert.That(result.Contratti!.Select(x => x.YearQuarter), Is.All.EqualTo("2025_4"));
    }

    /// <summary>
    /// Un anno senza dati restituisce lista vuota: non ricade sul trimestre più recente.
    /// </summary>
    /// <returns>Task che rappresenta l'operazione asincrona.</returns>
    [Test]
    public async Task AnnoSenzaDati_ShouldRestituireListaVuotaECountZero()
    {
        var query = Query();
        query.Year = "1999";

        var result = await _handler.Send(query);

        Assert.Multiple(() =>
        {
            Assert.That(result.Contratti, Is.Empty);
            Assert.That(result.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Anno e contratti si applicano insieme: C4 esiste solo nel 2025, quindi nel 2026 resta solo C1.
    /// </summary>
    /// <returns>Task che rappresenta l'operazione asincrona.</returns>
    [Test]
    public async Task AnnoEContratti_ShouldIntersecare()
    {
        var query = Query();
        query.Year = "2026";
        query.ContractIds = ["APPIO-C1", "APPIO-C4"];

        var result = await _handler.Send(query);

        Assert.That(result.Contratti!.Select(x => (x.ContractId, x.YearQuarter)), Is.EquivalentTo(new[]
        {
            ("APPIO-C1", "2026_1"), ("APPIO-C1", "2026_2")
        }));
    }

    // --- ricerca per nome ---

    /// <summary>
    /// Query per nome con AuthenticationInfo fittizio.
    /// </summary>
    private static AppIoContrattiQueryGetByName QueryNome(string? nome) => new(new AuthenticationInfo
    {
        IdEnte = Guid.NewGuid().ToString(),
        Prodotto = "prod-appio",
        Ruolo = Ruolo.ADMIN
    })
    { Name = nome };

    /// <summary>
    /// Senza trimestri né anno la ricerca per nome cerca in tutti i trimestri: C4, presente solo in
    /// 2025_4, viene trovato anche se non è nel trimestre più recente. Una riga per contratto (GROUP BY),
    /// con il trimestre più recente in cui compare: C1, presente in tre trimestri, esce una volta sola.
    /// </summary>
    /// <returns>Task che rappresenta l'operazione asincrona.</returns>
    [Test]
    public async Task Nome_SenzaPeriodo_ShouldCercareInTuttiITrimestri_UnaRigaPerContratto()
    {
        var result = (await _handler.Send(QueryNome("AppIO Test"))).ToList();

        Assert.That(result.Select(x => (x.ContractId, x.YearQuarter)), Is.EqualTo(new[]
        {
            ("APPIO-C1", "2026_2"), ("APPIO-C2", "2026_2"), ("APPIO-C4", "2025_4")
        }), "ordinati per nome: Alfa, Beta, Delta");
    }

    /// <summary>
    /// Anche con più trimestri richiesti lo stesso contratto esce una volta sola, con il più recente
    /// fra quelli cercati (non il più recente in assoluto).
    /// </summary>
    /// <returns>Task che rappresenta l'operazione asincrona.</returns>
    [Test]
    public async Task Nome_PiuTrimestri_ShouldRestituireUnaRigaPerContratto_ConIlTrimestrePiuRecenteFraQuelliCercati()
    {
        var query = QueryNome("Alfa");
        query.YearQuarter = ["2025_4", "2026_1"];

        var result = (await _handler.Send(query)).ToList();

        Assert.That(result.Select(x => (x.ContractId, x.YearQuarter)), Is.EqualTo(new[] { ("APPIO-C1", "2026_1") }));
    }

    /// <summary>
    /// Con il solo anno la ricerca per nome resta nei trimestri di quell'anno.
    /// </summary>
    /// <returns>Task che rappresenta l'operazione asincrona.</returns>
    [Test]
    public async Task Nome_ConAnno_ShouldCercareNeiTrimestriDellAnno()
    {
        var query = QueryNome("AppIO Test");
        query.Year = "2025";

        var result = (await _handler.Send(query)).ToList();

        Assert.That(result.Select(x => (x.ContractId, x.YearQuarter)), Is.EquivalentTo(new[]
        {
            ("APPIO-C1", "2025_4"), ("APPIO-C4", "2025_4")
        }));
    }

    /// <summary>
    /// Anche nella ricerca per nome i trimestri prevalgono sull'anno.
    /// </summary>
    /// <returns>Task che rappresenta l'operazione asincrona.</returns>
    [Test]
    public async Task Nome_TrimestriEAnno_ShouldPrevalereITrimestri()
    {
        var query = QueryNome("AppIO Test");
        query.YearQuarter = ["2026_2"];
        query.Year = "2025";

        var result = (await _handler.Send(query)).ToList();

        Assert.That(result.Select(x => x.ContractId), Is.EqualTo(new[] { "APPIO-C1", "APPIO-C2" }));
    }

    /// <summary>
    /// Senza nome né periodo la ricerca restituisce tutti i contratti, uno per id (i 4 APPIO-C* del
    /// seed, che ha 6 righe).
    /// </summary>
    /// <returns>Task che rappresenta l'operazione asincrona.</returns>
    [Test]
    public async Task Nome_SenzaFiltri_ShouldRestituireTuttiIContratti_UnoPerId()
    {
        var result = (await _handler.Send(QueryNome(null))).Where(x => x.ContractId!.StartsWith("APPIO-C")).ToList();

        Assert.That(result.Select(x => x.ContractId),
            Is.EquivalentTo(new[] { "APPIO-C1", "APPIO-C2", "APPIO-C3", "APPIO-C4" }));
    }
}
