using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Anagrafica APP IO (modello dati dell'11/11/2026, PF-1016): query MediatR -> handler ->
/// persistence -> viste [be].[vwAppioContracts], [be].[vwAppioContractsFiltroNome] e
/// [be].[vwAppioContractsFiltroTipoEnte] sul DB seedato (tests/Data/appio.sql).
///
/// Seed: OB1 (ENTE-1, "Comune Alfa AppIO Test", PA), OB2 (ENTE-2, "Comune Beta AppIO Test", PA),
/// OB3 (ENTE-3, "Regione Gamma", PA, SUSPENDED, senza PEC), OB4 (ENTE-4, omonimo di OB1, GSP),
/// OB5 (ENTE-5, nome e tipo NULL).
/// </summary>
[Category("AppIO")]
public class AppIoContrattiQueryIntegrationTests
{
    private IMediator _handler = null!;

    [SetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioContracts");
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioContractsFiltroNome");
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioContractsFiltroTipoEnte");
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
    }

    private static AuthenticationInfo Auth() => new()
    {
        IdEnte = Guid.NewGuid().ToString(),
        Prodotto = "prod-appio",
        Ruolo = Ruolo.ADMIN
    };

    /// <summary>
    /// Senza filtri la griglia restituisce tutti i contratti, ordinati per nome e contratto (il nome
    /// NULL di OB5 viene per primo), con il conteggio totale.
    /// </summary>
    [Test]
    public async Task Ricerca_SenzaFiltri_ShouldRestituireTuttiIContrattiInOrdine()
    {
        var lista = await _handler.Send(new AppIoContrattiQueryGetByRicerca(Auth()));

        Assert.Multiple(() =>
        {
            Assert.That(lista.Count, Is.EqualTo(5));
            Assert.That(lista.Contratti!.Select(x => x.ContractId),
                Is.EqualTo(new[] { "APPIO-OB5", "APPIO-OB1", "APPIO-OB4", "APPIO-OB2", "APPIO-OB3" }));
        });
    }

    /// <summary>
    /// Tutte le colonne della vista arrivano sul DTO; la PEC assente nel JSON è null.
    /// </summary>
    [Test]
    public async Task Ricerca_ShouldMappareTutteLeColonne()
    {
        var contratti = (await _handler.Send(new AppIoContrattiQueryGetByRicerca(Auth()))).Contratti!.ToDictionary(x => x.ContractId!);

        var ob1 = contratti["APPIO-OB1"];
        var ob3 = contratti["APPIO-OB3"];
        Assert.Multiple(() =>
        {
            Assert.That(ob1.InstitutionId, Is.EqualTo("APPIO-ENTE-1"));
            Assert.That(ob1.Name, Is.EqualTo("Comune Alfa AppIO Test"));
            Assert.That(ob1.Prodotto, Is.EqualTo("prod-io"));
            Assert.That(ob1.InstitutionType, Is.EqualTo("PA"));
            Assert.That(ob1.EmailPec, Is.EqualTo("alfa@pec.example.test"));
            Assert.That(ob1.ContractState, Is.EqualTo("ACTIVE"));
            Assert.That(ob3.EmailPec, Is.Null);
            Assert.That(ob3.ContractState, Is.EqualTo("SUSPENDED"));
        });
    }

    /// <summary>
    /// Enti e tipi ente si applicano insieme (AND): ENTE-1 ed ENTE-4 sono omonimi, il tipo GSP li separa.
    /// </summary>
    [Test]
    public async Task Ricerca_EntiETipiEnte_ShouldIntersecare()
    {
        var lista = await _handler.Send(new AppIoContrattiQueryGetByRicerca(Auth())
        {
            InstitutionIds = ["APPIO-ENTE-1", "APPIO-ENTE-4"],
            TipiEnte = ["GSP"]
        });

        Assert.Multiple(() =>
        {
            Assert.That(lista.Count, Is.EqualTo(1));
            Assert.That(lista.Contratti!.Single().ContractId, Is.EqualTo("APPIO-OB4"));
        });
    }

    /// <summary>
    /// La paginazione restituisce la pagina richiesta, il conteggio resta quello totale.
    /// </summary>
    [Test]
    public async Task Ricerca_Paginata_ShouldRestituireLaPaginaEIlTotale()
    {
        var lista = await _handler.Send(new AppIoContrattiQueryGetByRicerca(Auth()) { Page = 2, Size = 2 });

        Assert.Multiple(() =>
        {
            Assert.That(lista.Count, Is.EqualTo(5));
            Assert.That(lista.Contratti!.Select(x => x.ContractId), Is.EqualTo(new[] { "APPIO-OB4", "APPIO-OB2" }));
        });
    }

    /// <summary>
    /// La ricerca per nome restituisce una voce per ente: i due omonimi restano distinti per id.
    /// </summary>
    [Test]
    public async Task Nome_ShouldRestituireUnaVocePerEnte_AncheOmonimi()
    {
        var nomi = (await _handler.Send(new AppIoContrattiQueryGetByName(Auth()) { Name = "Alfa" })).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(nomi.Select(x => x.InstitutionId), Is.EqualTo(new[] { "APPIO-ENTE-1", "APPIO-ENTE-4" }));
            Assert.That(nomi.Select(x => x.Name), Is.All.EqualTo("Comune Alfa AppIO Test"));
        });
    }

    /// <summary>
    /// Senza nome si ottengono tutti gli enti con un nome (OB5, senza nome, è escluso dalla vista).
    /// </summary>
    [Test]
    public async Task Nome_SenzaNome_ShouldRestituireTuttiGliEntiConNome()
    {
        var nomi = await _handler.Send(new AppIoContrattiQueryGetByName(Auth()));

        Assert.That(nomi.Select(x => x.InstitutionId),
            Is.EqualTo(new[] { "APPIO-ENTE-1", "APPIO-ENTE-4", "APPIO-ENTE-2", "APPIO-ENTE-3" }));
    }

    /// <summary>
    /// I tipi ente sono distinti, in ordine alfabetico, senza il NULL di OB5.
    /// </summary>
    [Test]
    public async Task TipiEnte_ShouldEssereDistintiInOrdine()
    {
        var tipi = await _handler.Send(new AppIoContrattiTipiEnteQuery(Auth()));

        Assert.That(tipi, Is.EqualTo(new[] { "GSP", "PA" }));
    }
}
