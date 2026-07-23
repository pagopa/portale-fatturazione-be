using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Integration test su query reale (MediatR + persistence + DB UAT) per la funzionalità
/// usata dall'endpoint POST /api/fatture/pagopa/gestione-fatture/tipologia-fattura.
/// </summary>
public class GestioneFattureTipologiaFatturaQueryIntegrationTests
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
    /// Verifica che, filtrando per Anno e Mesi, la query ritorni tipologie non vuote,
    /// distinte e contenute nel set ottenuto con il solo filtro Anno.
    /// </summary>
    public async Task GestioneFattureTipologiaFatturaQuery_WithAnnoAndMesi_ShouldReturnDistinctSubsetOfYearFilter()
    {
        var anni = await ExecuteQueryOrIgnoreMissingView(() =>
            _handler.Send(new GestioneFattureAnniQuery(AdminAuth())));

        var years = GetYearsOrIgnoreIfEmpty(anni);
        var targetYear = years.First();

        var mesi = await ExecuteQueryOrIgnoreMissingView(() =>
            _handler.Send(new GestioneFattureMesiQuery(AdminAuth()) { Anno = targetYear }));

        var months = mesi?.ToList() ?? [];
        if (months.Count == 0)
            Assert.Ignore("Nessun mese disponibile per l'anno seed: test valido solo con dati UAT popolati.");

        var monthFilter = months.Take(2).ToArray();

        var byYear = await ExecuteQueryOrIgnoreMissingView(() =>
            _handler.Send(new GestioneFattureTipologiaFatturaQuery(AdminAuth())
            {
                Anno = targetYear
            }));

        var byYearAndMonths = await ExecuteQueryOrIgnoreMissingView(() =>
            _handler.Send(new GestioneFattureTipologiaFatturaQuery(AdminAuth())
            {
                Anno = targetYear,
                Mesi = monthFilter
            }));

        var yearList = byYear?.ToList() ?? [];
        var filteredList = byYearAndMonths?.ToList() ?? [];

        Assume.That(yearList.Count, Is.GreaterThan(0),
            "Nessuna tipologia disponibile per l'anno seed: test inconclusivo in questo ambiente.");
        Assume.That(filteredList.Count, Is.GreaterThan(0),
            "Nessuna tipologia disponibile con filtro Anno+Mesi seed: test inconclusivo in questo ambiente.");

        var yearSet = new HashSet<string>(yearList, StringComparer.OrdinalIgnoreCase);

        Assert.Multiple(() =>
        {
            Assert.That(filteredList.All(x => !string.IsNullOrWhiteSpace(x)), Is.True);
            Assert.That(filteredList.Count, Is.EqualTo(filteredList.Distinct(StringComparer.OrdinalIgnoreCase).Count()));
            Assert.That(filteredList.All(x => yearSet.Contains(x)), Is.True);
            Assert.That(filteredList.Count, Is.LessThanOrEqualTo(yearList.Count));
        });
    }

    [Test]
    /// <summary>
    /// Verifica che, per un anno non presente nel dataset, la query ritorni zero tipologie.
    /// </summary>
    public async Task GestioneFattureTipologiaFatturaQuery_ForAbsentYear_ShouldReturnEmpty()
    {
        var anni = await ExecuteQueryOrIgnoreMissingView(() =>
            _handler.Send(new GestioneFattureAnniQuery(AdminAuth())));

        var years = GetYearsOrIgnoreIfEmpty(anni);
        var absentYear = years.Min() - 1;

        var result = await ExecuteQueryOrIgnoreMissingView(() =>
            _handler.Send(new GestioneFattureTipologiaFatturaQuery(AdminAuth())
            {
                Anno = absentYear
            }));

        var tipologie = result?.ToList() ?? [];
        Assert.That(tipologie, Is.Empty);
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
