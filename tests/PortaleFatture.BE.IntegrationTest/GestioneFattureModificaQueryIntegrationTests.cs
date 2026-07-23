using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Integration test su query reale (MediatR + persistence + DB UAT)
/// per la funzionalità usata dagli endpoint:
/// - POST /api/fatture/pagopa/gestione-fatture/modifica/anni
/// - POST /api/fatture/pagopa/gestione-fatture/modifica/mesi
/// </summary>
public class GestioneFattureModificaQueryIntegrationTests
{
    private IMediator _handler;

    [SetUp]
    public void Setup()
    {
        _handler = ServiceProvider.GetRequiredService<IMediator>();
    }

    private static AuthenticationInfo AdminAuth() => new()
    {
        IdEnte = Guid.NewGuid().ToString(),
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };

    [Test]
    /// <summary>
    /// Verifica che la query modifica/anni filtri per Azione + TipologiaFattura
    /// e includa l'anno del seed selezionato dal dataset reale.
    /// </summary>
    public async Task GestioneFattureModificaAnniQuery_WithSeedFilters_ShouldContainSeedYear()
    {
        var seedRows = await ExecuteQueryOrIgnoreMissingView(() =>
            _handler.Send(new GestioneFattureQuery(AdminAuth()) { Page = 1, Size = 200 }));

        var rows = seedRows?.GestioneFatture?.ToList() ?? [];
        var seed = rows.FirstOrDefault(x =>
            !string.IsNullOrWhiteSpace(x.TipologiaFattura) &&
            !string.IsNullOrWhiteSpace(x.Azione));

        if (seed == null)
            Assert.Ignore("Nessun seed con TipologiaFattura/Azione disponibile nel dataset corrente.");

        var years = await ExecuteQueryOrIgnoreMissingView(() =>
            _handler.Send(new GestioneFattureModificaAnniQuery(AdminAuth())
            {
                TipologiaFattura = seed.TipologiaFattura!,
                Azione = seed.Azione!
            }));

        var list = years?.ToList() ?? [];

        Assert.Multiple(() =>
        {
            Assert.That(list, Is.Not.Empty);
            Assert.That(list.Contains(seed.Anno), Is.True);
            Assert.That(list, Is.Ordered.Descending);
        });
    }

    [Test]
    /// <summary>
    /// Verifica che la query modifica/mesi filtri per Azione + TipologiaFattura + Anno
    /// e includa il mese del seed reale.
    /// </summary>
    public async Task GestioneFattureModificaMesiQuery_WithSeedFilters_ShouldContainSeedMonth()
    {
        var seedRows = await ExecuteQueryOrIgnoreMissingView(() =>
            _handler.Send(new GestioneFattureQuery(AdminAuth()) { Page = 1, Size = 200 }));

        var rows = seedRows?.GestioneFatture?.ToList() ?? [];
        var seed = rows.FirstOrDefault(x =>
            !string.IsNullOrWhiteSpace(x.TipologiaFattura) &&
            !string.IsNullOrWhiteSpace(x.Azione));

        if (seed == null)
            Assert.Ignore("Nessun seed con TipologiaFattura/Azione disponibile nel dataset corrente.");

        var months = await ExecuteQueryOrIgnoreMissingView(() =>
            _handler.Send(new GestioneFattureModificaMesiQuery(AdminAuth())
            {
                Anno = seed.Anno.ToString(),
                TipologiaFattura = seed.TipologiaFattura!,
                Azione = seed.Azione!
            }));

        var list = months?.ToList() ?? [];

        Assert.Multiple(() =>
        {
            Assert.That(list, Is.Not.Empty);
            Assert.That(list.Contains(seed.Mese), Is.True);
            Assert.That(list, Is.Ordered.Descending);
        });
    }

    private static async Task<T> ExecuteQueryOrIgnoreMissingView<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (SqlException ex) when (ex.Number == 208 && ex.Message.Contains("vwGestioneFattureGriglia", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Ignore("Vista [be].[vwGestioneFattureGriglia] non disponibile nell'ambiente corrente: test valido solo in UAT con schema completo.");
            throw;
        }
        catch (SqlException ex) when (
            ex.Number == 47073 ||
            ex.Message.Contains("Deny Public Network Access is set to Yes", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Ignore("Connessione SQL non consentita nell'ambiente corrente (Public Network Access disabilitato / VPN non attiva): test valido solo in UAT con accesso DB.");
            throw;
        }
    }
}
