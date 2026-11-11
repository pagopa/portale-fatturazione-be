using PortaleFatture.BE.Api.Modules.AppIO.Contratti.Extensions;
using PortaleFatture.BE.Api.Modules.AppIO.Contratti.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Dto;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Mapping delle rotte api/appio/contracts* (modello dati dell'11/11/2026, PF-1016): request ->
/// query e risultati -> response. Le persistence e le rotte sono coperte su DB seedato
/// (AppIoContrattiQueryIntegrationTests, Http/AppIoContrattiHttpTests).
/// </summary>
[Category("AppIO")]
public class AppIoContrattiExtensionsTests
{
    private static AuthenticationInfo Auth() => new()
    {
        Id = "utente-test",
        Prodotto = "prod-appio",
        Ruolo = Ruolo.ADMIN
    };

    /// <summary>
    /// La richiesta della griglia riporta enti, tipi ente, paginazione e autenticazione.
    /// </summary>
    [Test]
    public void MapRicerca_ConFiltri_ShouldRiportarliConPaginazioneEAutenticazione()
    {
        var auth = Auth();
        var query = new AppIoContrattiRequest { InstitutionIds = ["E1"], TipiEnte = ["PA"] }.Map(auth, 2, 10);

        Assert.Multiple(() =>
        {
            Assert.That(query.InstitutionIds, Is.EqualTo(new[] { "E1" }));
            Assert.That(query.TipiEnte, Is.EqualTo(new[] { "PA" }));
            Assert.That(query.Page, Is.EqualTo(2));
            Assert.That(query.Size, Is.EqualTo(10));
            Assert.That(query.AuthenticationInfo, Is.SameAs(auth));
        });
    }

    /// <summary>
    /// Array vuoti valgono come filtro assente (null), così la persistence non produce un "IN ()".
    /// </summary>
    [Test]
    public void MapRicerca_ArrayVuoti_ShouldDiventareNull()
    {
        var query = new AppIoContrattiRequest { InstitutionIds = [], TipiEnte = [] }.Map(Auth());

        Assert.Multiple(() =>
        {
            Assert.That(query.InstitutionIds, Is.Null);
            Assert.That(query.TipiEnte, Is.Null);
            Assert.That(query.Page, Is.Null);
            Assert.That(query.Size, Is.Null);
        });
    }

    /// <summary>
    /// La richiesta per nome riporta il nome così com'è (null compreso).
    /// </summary>
    [TestCase("Alfa")]
    [TestCase(null)]
    public void MapNome_ShouldRiportareIlNome(string? nome)
    {
        Assert.That(new AppIoContrattiNameRequest { Name = nome }.Map(Auth()).Name, Is.EqualTo(nome));
    }

    /// <summary>
    /// Le voci del filtro "Nome Ente" riportano id ente e nome.
    /// </summary>
    [Test]
    public void MapNomi_ShouldRiportareIdEnteENome()
    {
        var voce = new[] { new AppIoContrattoNome { InstitutionId = "E1", Name = "Ente" } }.Map().Single();

        Assert.Multiple(() =>
        {
            Assert.That(voce.InstitutionId, Is.EqualTo("E1"));
            Assert.That(voce.Name, Is.EqualTo("Ente"));
        });
    }

    /// <summary>
    /// La lista riporta il conteggio e tutte le colonne del contratto.
    /// </summary>
    [Test]
    public void MapLista_ShouldRiportareCountETutteLeColonne()
    {
        var dto = new AppIoContrattiListDto
        {
            Count = 7,
            Contratti =
            [
                new AppIoContratto
                {
                    ContractId = "C1", InstitutionId = "E1", Name = "Ente", Prodotto = "prod-io",
                    InstitutionType = "PA", EmailPec = "pec@example.test", ContractState = "ACTIVE"
                }
            ]
        };

        var resp = dto.Map();
        var c = resp.Contratti!.Single();

        Assert.Multiple(() =>
        {
            Assert.That(resp.Count, Is.EqualTo(7));
            Assert.That(c.ContractId, Is.EqualTo("C1"));
            Assert.That(c.InstitutionId, Is.EqualTo("E1"));
            Assert.That(c.Name, Is.EqualTo("Ente"));
            Assert.That(c.Prodotto, Is.EqualTo("prod-io"));
            Assert.That(c.InstitutionType, Is.EqualTo("PA"));
            Assert.That(c.EmailPec, Is.EqualTo("pec@example.test"));
            Assert.That(c.ContractState, Is.EqualTo("ACTIVE"));
        });
    }

    /// <summary>
    /// Contratti null restano null nella risposta.
    /// </summary>
    [Test]
    public void MapLista_ContrattiNull_ShouldRestituireContrattiNull()
    {
        Assert.That(new AppIoContrattiListDto { Count = 0 }.Map().Contratti, Is.Null);
    }
}
