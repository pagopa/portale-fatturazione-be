using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Integration test su query reale (MediatR + persistence + DB UAT) per la funzionalità
/// usata dall'endpoint GET /api/fatture/pagopa/gestione-fatture/anni.
/// </summary>
public class GestioneFattureAnniQueryIntegrationTests
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
    /// Verifica che la query anni sia eseguibile su DB reale e che il risultato
    /// sia ordinato in modo decrescente e privo di duplicati (GROUP BY + ORDER BY DESC).
    /// </summary>
    public async Task GestioneFattureAnniQuery_ShouldReturnDistinctYears_OrderedDescending()
    {
        var result = await ExecuteQueryOrIgnoreMissingView(() =>
            _handler.Send(new GestioneFattureAnniQuery(AdminAuth())));

        Assert.That(result, Is.Not.Null);

        var years = result!.ToList();
        Assume.That(years.Count, Is.GreaterThan(0),
            "Nessun anno disponibile nel dataset corrente: test inconclusivo in questo ambiente.");

        var ordered = years.OrderByDescending(x => x).ToList();
        var distinctCount = years.Distinct().Count();

        Assert.Multiple(() =>
        {
            Assert.That(years.Count, Is.EqualTo(distinctCount));
            Assert.That(years, Is.EqualTo(ordered));
        });
    }

    [Test]
    /// <summary>
    /// Verifica che gli anni ritornati siano plausibili (no valori nulli/zero/negativi,
    /// e dentro un range temporale sensato per il dominio).
    /// </summary>
    public async Task GestioneFattureAnniQuery_ShouldReturnPlausibleYearValues()
    {
        var result = await ExecuteQueryOrIgnoreMissingView(() =>
            _handler.Send(new GestioneFattureAnniQuery(AdminAuth())));

        Assert.That(result, Is.Not.Null);

        var years = result!.ToList();
        Assume.That(years.Count, Is.GreaterThan(0),
            "Nessun anno disponibile nel dataset corrente: test inconclusivo in questo ambiente.");

        var maxAllowed = DateTime.UtcNow.Year + 1;

        Assert.That(years.All(y => y >= 2000 && y <= maxAllowed), Is.True,
            $"Trovato almeno un anno fuori range atteso [2000..{maxAllowed}].");
    }

    /// <summary>
    /// Esegue l'azione passata come parametro, ignorando il test se la vista
    /// necessaria non è disponibile nell'ambiente corrente (es. UAT vs DEV).
    /// </summary>
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
