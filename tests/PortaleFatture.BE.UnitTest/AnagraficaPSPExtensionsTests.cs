using PortaleFatture.BE.Api.Modules.pagoPA.AnagraficaPSP.Extensions;
using PortaleFatture.BE.Api.Modules.pagoPA.AnagraficaPSP.Request;
using PortaleFatture.BE.Core.Auth;
using InfraExtensions = PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Extensions.AnagraficaPSPExtensions;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Rotte api/v2/pagopa/psps* (anagrafica PSP): logica in memoria, senza DB.
///  - i Map del modulo API, che trasformano le request in query;
///  - AddInOrder di Infrastructure, con cui PSPQueryGetByRicercaPersistence compone il WHERE.
/// PSPSQLBuilder è internal e non è visibile dai progetti di test: le query sono coperte su DB
/// seedato dagli integration test dell'area.
/// </summary>
public class AnagraficaPSPExtensionsTests
{
    /// <summary>
    /// Informazioni di autenticazione fittizie di un ADMIN sul prodotto pagoPA.
    /// </summary>
    private static AuthenticationInfo Auth() => new()
    {
        Id = "utente-test",
        Prodotto = "prod-pagopa",
        Ruolo = Ruolo.ADMIN
    };

    // --- request -> query ---

    /// <summary>
    /// Verifica che la ricerca riporti tutti i filtri, la paginazione e l'autenticazione.
    /// </summary>
    [Test]
    public void MapRicerca_ConFiltriEPaginazione_ShouldRiportarliTutti()
    {
        var auth = Auth();
        var req = new PSPRequest
        {
            ContractIds = ["C1"],
            MembershipId = "M1",
            RecipientId = "R1",
            ABI = "03069",
            Quarters = ["2026_1"]
        };

        var query = req.Map(auth, 3, 25);

        Assert.Multiple(() =>
        {
            Assert.That(query.ContractIds, Is.EqualTo(new[] { "C1" }));
            Assert.That(query.MembershipId, Is.EqualTo("M1"));
            Assert.That(query.RecipientId, Is.EqualTo("R1"));
            Assert.That(query.ABI, Is.EqualTo("03069"));
            Assert.That(query.YearQuarter, Is.EqualTo(new[] { "2026_1" }));
            Assert.That(query.Page, Is.EqualTo(3));
            Assert.That(query.Size, Is.EqualTo(25));
            Assert.That(query.AuthenticationInfo, Is.SameAs(auth));
        });
    }

    /// <summary>
    /// Verifica che senza paginazione (overload usato dal download) Page e Size restino null, così la
    /// persistence non applica OFFSET/FETCH e restituisce tutte le righe del filtro.
    /// </summary>
    [Test]
    public void MapRicerca_SenzaPaginazione_ShouldLasciarlaNull()
    {
        var query = new PSPRequest().Map(Auth());

        Assert.Multiple(() =>
        {
            Assert.That(query.Page, Is.Null);
            Assert.That(query.Size, Is.Null);
        });
    }

    /// <summary>
    /// Verifica che un elenco vuoto di contratti diventi null (nessun filtro), mentre l'elenco vuoto
    /// dei trimestri passa così com'è: la persistence tratta comunque null e vuoto allo stesso modo,
    /// applicando il default sul trimestre più recente.
    /// </summary>
    [Test]
    public void MapRicerca_ArrayVuoti_ContractIdsNull_QuartersInvariati()
    {
        var query = new PSPRequest { ContractIds = [], Quarters = [] }.Map(Auth(), 1, 10);

        Assert.Multiple(() =>
        {
            Assert.That(query.ContractIds, Is.Null);
            Assert.That(query.YearQuarter, Is.Empty);
        });
    }

    /// <summary>
    /// Verifica che la ricerca per nome riporti nome e trimestri.
    /// </summary>
    [Test]
    public void MapNome_ShouldRiportareNomeETrimestri()
    {
        var query = new PSPRequestName { Name = "Banca", Quarters = ["2025_4"] }.Map(Auth());

        Assert.Multiple(() =>
        {
            Assert.That(query.Name, Is.EqualTo("Banca"));
            Assert.That(query.YearQuarter, Is.EqualTo(new[] { "2025_4" }));
        });
    }

    /// <summary>
    /// Verifica che la richiesta dei trimestri riporti l'anno.
    /// </summary>
    [Test]
    public void MapQuarters_ShouldRiportareLAnno()
    {
        var query = new PSPsQuartersRequest { Year = "2026" }.Map(Auth());

        Assert.That(query.Year, Is.EqualTo("2026"));
    }

    // --- AddInOrder ---

    /// <summary>
    /// Verifica che AddInOrder inserisca ogni elemento mantenendo la lista ordinata, qualunque sia
    /// l'ordine di inserimento: le condizioni del WHERE escono quindi in ordine alfabetico, non in
    /// quello in cui la persistence le aggiunge. È innocuo (sono tutte in AND), ma va saputo leggendo
    /// la query generata.
    /// </summary>
    [Test]
    public void AddInOrder_ShouldMantenereLaListaOrdinata()
    {
        var where = new List<string>();

        InfraExtensions.AddInOrder(where, " year_quarter IN @YearQuarter");
        InfraExtensions.AddInOrder(where, " contract_id IN @ContractIds");
        InfraExtensions.AddInOrder(where, " membership_id = @MembershipId");

        Assert.That(where, Is.EqualTo(new[]
        {
            " contract_id IN @ContractIds",
            " membership_id = @MembershipId",
            " year_quarter IN @YearQuarter"
        }));
    }

    /// <summary>
    /// Verifica che un duplicato venga inserito comunque (nessuna deduplicazione).
    /// </summary>
    [Test]
    public void AddInOrder_Duplicato_ShouldInserirloComunque()
    {
        var where = new List<string> { "a" };

        InfraExtensions.AddInOrder(where, "a");

        Assert.That(where, Is.EqualTo(new[] { "a", "a" }));
    }
}
