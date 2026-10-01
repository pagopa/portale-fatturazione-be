using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Anagrafica PSP (rotte api/v2/pagopa/psps*): le tre query MediatR — ricerca, ricerca per nome e
/// trimestri — eseguite sul DB seedato contro [ppa].[Contracts] (tests/Data/ppa_contracts.sql).
///
/// Seed: trimestre più recente 2026_1 con T01…T05; T01 anche in 2025_4, T06 solo in 2025_4. Filtro
/// ABI: T01 'ABI01234', T02 'BICBETA1,ABI05678', T03 'ABI01235,BICGAMMA', T04 provider NULL e abi
/// '09999', T05 provider vuoto e abi 'ABI07777'. membership grp-1 (T01, T02), grp-2 (T03, T04).
/// </summary>
public class PSPQueryIntegrationTests
{
    private IMediator _handler = null!;

    /// <summary>
    /// Salta i test se il container non è raggiungibile o se ppa.Contracts non è nel seed.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "ppa.Contracts");
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
    }

    /// <summary>
    /// Informazioni di autenticazione fittizie di un ADMIN sul prodotto pagoPA.
    /// </summary>
    private static AuthenticationInfo Auth() => new()
    {
        IdEnte = Guid.NewGuid().ToString(),
        Prodotto = "prod-pagopa",
        Ruolo = Ruolo.ADMIN
    };

    /// <summary>Esegue la ricerca e restituisce gli id dei contratti trovati, nell'ordine della query.</summary>
    private async Task<List<string?>> Ricerca(Action<PSPQueryGetByRicerca> filtri)
    {
        var query = new PSPQueryGetByRicerca(Auth());
        filtri(query);
        var result = await _handler.Send(query);
        return result.PSPs!.Select(x => x.ContractId).ToList();
    }

    // --- ricerca ---

    /// <summary>
    /// Senza filtri la ricerca restituisce i contratti del trimestre più recente, ordinati per
    /// contract_id, con il conteggio totale.
    /// </summary>
    [Test]
    public async Task Ricerca_SenzaFiltri_ShouldRestituireIlTrimestrePiuRecente()
    {
        var result = await _handler.Send(new PSPQueryGetByRicerca(Auth()));

        Assert.Multiple(() =>
        {
            Assert.That(result.PSPs!.Select(x => x.ContractId),
                Is.EqualTo(new[] { "PSP-T01", "PSP-T02", "PSP-T03", "PSP-T04", "PSP-T05" }));
            Assert.That(result.PSPs!.Select(x => x.YearQuarter), Is.All.EqualTo("2026_1"));
            Assert.That(result.Count, Is.EqualTo(5));
        });
    }

    /// <summary>Con i trimestri indicati la ricerca restituisce solo quelli.</summary>
    [Test]
    public async Task Ricerca_ConTrimestre_ShouldFiltrareSuQuelloRichiesto()
    {
        var ids = await Ricerca(q => q.YearQuarter = ["2025_4"]);

        Assert.That(ids, Is.EqualTo(new[] { "PSP-T01", "PSP-T06" }));
    }

    /// <summary>
    /// Il filtro per contratto non scavalca il default sul trimestre: T06 esiste solo in 2025_4 e
    /// resta fuori.
    /// </summary>
    [Test]
    public async Task Ricerca_ConContractIds_ShouldRestareNelTrimestrePiuRecente()
    {
        var ids = await Ricerca(q => q.ContractIds = ["PSP-T02", "PSP-T06"]);

        Assert.That(ids, Is.EqualTo(new[] { "PSP-T02" }));
    }

    /// <summary>Filtri per membership_id e recipient_id.</summary>
    [Test]
    public async Task Ricerca_ConMembershipERecipient_ShouldFiltrare()
    {
        var perMembership = await Ricerca(q => q.MembershipId = "grp-2");
        var perRecipient = await Ricerca(q => q.RecipientId = "rcp-2");

        Assert.Multiple(() =>
        {
            Assert.That(perMembership, Is.EqualTo(new[] { "PSP-T03", "PSP-T04" }));
            Assert.That(perRecipient, Is.EqualTo(new[] { "PSP-T02" }));
        });
    }

    /// <summary>
    /// Filtro ABI, un caso per ciascun ramo del predicato a cinque OR: codice in provider_names
    /// (da solo, in coda, in testa), codice solo nella colonna abi, e colonna abi con il prefisso
    /// 'ABI'.
    /// </summary>
    [TestCase("01234", "PSP-T01")]
    [TestCase("05678", "PSP-T02")]
    [TestCase("01235", "PSP-T03")]
    [TestCase("09999", "PSP-T04")]
    [TestCase("07777", "PSP-T05")]
    public async Task Ricerca_FiltroAbi_ShouldTrovareIlContratto(string abi, string atteso)
    {
        var ids = await Ricerca(q => q.ABI = abi);

        Assert.That(ids, Is.EqualTo(new[] { atteso }));
    }

    /// <summary>
    /// Paginazione: la seconda pagina restituisce le sole sue righe, il conteggio resta sul totale.
    /// </summary>
    [Test]
    public async Task Ricerca_Paginazione_ShouldRestituireLaPaginaEIlCountTotale()
    {
        var query = new PSPQueryGetByRicerca(Auth()) { Page = 2, Size = 2 };

        var result = await _handler.Send(query);

        Assert.Multiple(() =>
        {
            Assert.That(result.PSPs!.Select(x => x.ContractId), Is.EqualTo(new[] { "PSP-T03", "PSP-T04" }));
            Assert.That(result.Count, Is.EqualTo(5));
        });
    }

    /// <summary>
    /// Il mapping popola tutte le colonne e tollera i NULL (T05 ha signed_date, vat_group e sdi_code
    /// NULL); sdd resta la stringa 'FALSE'.
    /// </summary>
    [Test]
    public async Task Ricerca_Mapping_ShouldPopolareLeColonne_ETollerareINull()
    {
        var result = await _handler.Send(new PSPQueryGetByRicerca(Auth()));
        var t02 = result.PSPs!.Single(x => x.ContractId == "PSP-T02");
        var t05 = result.PSPs!.Single(x => x.ContractId == "PSP-T05");

        Assert.Multiple(() =>
        {
            Assert.That(t02.DocumentName, Is.EqualTo("CI902_B_Banca Beta Test"));
            Assert.That(t02.ProviderNames, Is.EqualTo("BICBETA1,ABI05678"));
            Assert.That(t02.SignedDate, Is.EqualTo(new DateTime(2020, 2, 7)));
            Assert.That(t02.ContractType, Is.EqualTo("B"));
            Assert.That(t02.Abi, Is.EqualTo("05678"));
            Assert.That(t02.VatGroup, Is.EqualTo(0m));
            Assert.That(t02.ReferenteFatturaMail, Is.EqualTo("ref@beta.invalid"));
            Assert.That(t02.Sdd, Is.EqualTo("FALSE"));
            Assert.That(t02.MembershipId, Is.EqualTo("grp-1"));
            Assert.That(t02.RecipientId, Is.EqualTo("rcp-2"));
            Assert.That(t02.YearMonth, Is.EqualTo("202604"));
            Assert.That(t05.SignedDate, Is.Null);
            Assert.That(t05.VatGroup, Is.Null);
            Assert.That(t05.SdiCode, Is.Null);
        });
    }

    /// <summary>Un trimestre senza dati restituisce una lista vuota e conteggio zero.</summary>
    [Test]
    public async Task Ricerca_TrimestreSenzaDati_ShouldRestituireListaVuota()
    {
        var result = await _handler.Send(new PSPQueryGetByRicerca(Auth()) { YearQuarter = ["1999_1"] });

        Assert.Multiple(() =>
        {
            Assert.That(result.PSPs, Is.Empty);
            Assert.That(result.Count, Is.Zero);
        });
    }

    // --- ricerca per nome ---

    /// <summary>
    /// La ricerca per nome cerca per sottostringa nel trimestre più recente, ordinando per
    /// contract_id; T06 ha il nome giusto ma è solo in 2025_4.
    /// </summary>
    [Test]
    public async Task Nome_SenzaTrimestre_ShouldCercareNelPiuRecente()
    {
        var result = await _handler.Send(new PSPQueryGetByName(Auth()) { Name = "Banca" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Select(x => x.ContractId), Is.EqualTo(new[] { "PSP-T01", "PSP-T02" }));
            Assert.That(result.Select(x => x.YearQuarter), Is.All.EqualTo("2026_1"));
        });
    }

    /// <summary>Con un trimestre indicato la ricerca per nome cerca solo lì.</summary>
    [Test]
    public async Task Nome_ConTrimestre_ShouldCercareSoloLi()
    {
        var result = await _handler.Send(new PSPQueryGetByName(Auth()) { Name = "Banca", YearQuarter = ["2025_4"] });

        Assert.That(result.Select(x => x.ContractId), Is.EqualTo(new[] { "PSP-T01", "PSP-T06" }));
    }

    // --- trimestri ---

    /// <summary>Senza anno restituisce tutti i trimestri distinti, dal più recente.</summary>
    [Test]
    public async Task Trimestri_SenzaAnno_ShouldRestituireTuttiDalPiuRecente()
    {
        var result = await _handler.Send(new PSPsQuartersRequestQuery(Auth()));

        Assert.That(result, Is.EqualTo(new[] { "2026_1", "2025_4" }));
    }

    /// <summary>Con l'anno restituisce i soli trimestri di quell'anno.</summary>
    [Test]
    public async Task Trimestri_ConAnno_ShouldRestituireQuelliDellAnno()
    {
        var result = await _handler.Send(new PSPsQuartersRequestQuery(Auth()) { Year = "2025" });

        Assert.That(result, Is.EqualTo(new[] { "2025_4" }));
    }
}
