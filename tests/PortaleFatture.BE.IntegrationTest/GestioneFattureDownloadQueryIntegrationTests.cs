using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Integration test su query reale (MediatR + persistence + DB UAT) per la funzionalità
/// usata dall'endpoint POST /api/fatture/pagopa/gestione-fatture/download.
/// </summary>
public class GestioneFattureDownloadQueryIntegrationTests
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
    /// Verifica che la query download restituisca righe e count coerenti per un anno esistente,
    /// applicando i filtri derivati da una riga seed reale.
    /// </summary>
    public async Task GestioneFattureDownloadQuery_WithSeedFilters_ShouldReturnRowsAndConsistentCount()
    {
        var anni = await ExecuteQueryOrIgnoreMissingView(() =>
            _handler.Send(new GestioneFattureAnniQuery(AdminAuth())));

        var years = GetYearsOrIgnoreIfEmpty(anni);
        var targetYear = years.First();

        var baseline = await ExecuteQueryOrIgnoreMissingView(() =>
            _handler.Send(new GestioneFattureDownloadQuery(AdminAuth())
            {
                Anno = targetYear
            }));

        Assert.That(baseline, Is.Not.Null);

        var baseRows = baseline!.GestioneFatture?.ToList() ?? [];
        Assume.That(baseRows.Count, Is.GreaterThan(0),
            "Nessuna riga disponibile per l'anno seed: test inconclusivo in questo ambiente.");

        var seed = baseRows.First(x =>
            !string.IsNullOrWhiteSpace(x.Ente) &&
            !string.IsNullOrWhiteSpace(x.TipologiaFattura) &&
            !string.IsNullOrWhiteSpace(x.TipoContratto));

        var filtered = await ExecuteQueryOrIgnoreMissingView(() =>
            _handler.Send(new GestioneFattureDownloadQuery(AdminAuth())
            {
                Anno = targetYear,
                IdEnti = new[] { seed.Ente! },
                Mesi = new[] { seed.Mese },
                TipologiaContratto = seed.IdTipoContratto,
                TipologiaFattura = seed.TipologiaFattura
            }));

        Assert.That(filtered, Is.Not.Null);

        var rows = filtered!.GestioneFatture?.ToList() ?? [];
        Assume.That(rows.Count, Is.GreaterThan(0),
            "Nessuna riga restituita con i filtri seed: test inconclusivo in questo ambiente.");

        Assert.Multiple(() =>
        {
            Assert.That(filtered.Count, Is.EqualTo(rows.Count));
            Assert.That(rows.All(r => r.Anno == targetYear), Is.True);
            Assert.That(rows.All(r => string.Equals(r.Ente, seed.Ente, StringComparison.OrdinalIgnoreCase)), Is.True);
            Assert.That(rows.All(r => r.Mese == seed.Mese), Is.True);
            Assert.That(rows.All(r => r.IdTipoContratto == seed.IdTipoContratto), Is.True);
            Assert.That(rows.All(r => string.Equals(r.TipologiaFattura, seed.TipologiaFattura, StringComparison.OrdinalIgnoreCase)), Is.True);
        });
    }

    [Test]
    /// <summary>
    /// Verifica che, per un anno assente nel dataset, la query download ritorni zero righe e count 0.
    /// </summary>
    public async Task GestioneFattureDownloadQuery_ForAbsentYear_ShouldReturnEmpty()
    {
        var anni = await ExecuteQueryOrIgnoreMissingView(() =>
            _handler.Send(new GestioneFattureAnniQuery(AdminAuth())));

        var years = GetYearsOrIgnoreIfEmpty(anni);
        var absentYear = years.Min() - 1;

        var result = await ExecuteQueryOrIgnoreMissingView(() =>
            _handler.Send(new GestioneFattureDownloadQuery(AdminAuth())
            {
                Anno = absentYear
            }));

        Assert.That(result, Is.Not.Null);

        var rows = result!.GestioneFatture?.ToList() ?? [];
        Assert.Multiple(() =>
        {
            Assert.That(rows, Is.Empty);
            Assert.That(result.Count, Is.EqualTo(0));
        });
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
