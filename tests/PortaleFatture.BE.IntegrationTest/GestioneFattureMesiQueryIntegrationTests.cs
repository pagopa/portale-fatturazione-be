using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Integration test su query reale (MediatR + persistence + DB UAT) per la funzionalità
/// usata dall'endpoint POST /api/fatture/pagopa/gestione-fatture/mesi.
/// </summary>
public class GestioneFattureMesiQueryIntegrationTests
{
    private IMediator _handler;

    [SetUp]
    public void Setup()
    {
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
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
    /// Verifica che, dato un anno esistente, i mesi ritornati siano distinti,
    /// ordinati in modo decrescente e compresi nel range [1..12].
    /// </summary>
    public async Task GestioneFattureMesiQuery_ShouldReturnDistinctMonths_OrderedDescending_ForExistingYear()
    {
        var anni = await ExecuteQueryOrIgnoreMissingView(() =>
            _handler.Send(new GestioneFattureAnniQuery(AdminAuth())));

        var years = GetYearsOrIgnoreIfEmpty(anni);

        var targetYear = years.First();

        var result = await ExecuteQueryOrIgnoreMissingView(() =>
            _handler.Send(new GestioneFattureMesiQuery(AdminAuth()) { Anno = targetYear }));

        Assert.That(result, Is.Not.Null);

        var months = result!.ToList();
        Assume.That(months.Count, Is.GreaterThan(0),
            "Nessun mese disponibile per l'anno seed: test inconclusivo in questo ambiente.");

        var ordered = months.OrderByDescending(x => x).ToList();
        var distinctCount = months.Distinct().Count();

        Assert.Multiple(() =>
        {
            Assert.That(months.Count, Is.EqualTo(distinctCount));
            Assert.That(months, Is.EqualTo(ordered));
            Assert.That(months.All(m => m is >= 1 and <= 12), Is.True);
        });
    }

    [Test]
    /// <summary>
    /// Verifica che, per un anno assente nel dataset, la query ritorni zero mesi.
    /// </summary>
    public async Task GestioneFattureMesiQuery_ForYearOutsideAvailableSet_ShouldReturnEmpty()
    {
        var anni = await ExecuteQueryOrIgnoreMissingView(() =>
            _handler.Send(new GestioneFattureAnniQuery(AdminAuth())));

        var years = GetYearsOrIgnoreIfEmpty(anni);

        var absentYear = years.Min() - 1;

        var result = await ExecuteQueryOrIgnoreMissingView(() =>
            _handler.Send(new GestioneFattureMesiQuery(AdminAuth()) { Anno = absentYear }));

        var months = result?.ToList() ?? [];
        Assert.That(months, Is.Empty);
    }

    private static List<int> GetYearsOrIgnoreIfEmpty(IEnumerable<int>? years)
    {
        var list = years?.ToList() ?? [];
        if (list.Count == 0)
            Assert.Ignore("Nessun anno disponibile nel dataset corrente: test valido solo con dati UAT popolati.");

        return list;
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
