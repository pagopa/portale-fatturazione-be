using MediatR;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
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
                IdTipoContratto = seed.IdTipoContratto,
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

    [Test]
    /// <summary>
    /// Copre TUTTE le combinazioni (2^5) dei filtri del download: IdEnti, IdTipoContratto, Anno, Mesi,
    /// TipologiaFattura (il download non espone Azione). Invariante AND + presenza della riga target seed
    /// (Ente1/SECONDO SALDO/2025/mese 1/IdTipoContratto=2). Include il caso "solo idTipoContratto".
    /// </summary>
    public async Task GestioneFattureDownloadQuery_TutteLeCombinazioniFiltri_RispettanoAND_eIncludonoTarget()
    {
        const string ente1 = "11111111-1111-1111-1111-111111111111";

        var filters = new (string, Action<GestioneFattureDownloadQuery>, Func<SimpleGestioneFattureDto, bool>)[]
        {
            ("IdEnti",           q => q.IdEnti = new[] { ente1 },           r => string.Equals(r.Ente, ente1, StringComparison.OrdinalIgnoreCase)),
            ("IdTipoContratto",  q => q.IdTipoContratto = 2,                r => r.IdTipoContratto == 2),
            ("Anno",             q => q.Anno = 2025,                        r => r.Anno == 2025),
            ("Mesi",             q => q.Mesi = new[] { 1 },                 r => r.Mese == 1),
            ("TipologiaFattura", q => q.TipologiaFattura = "SECONDO SALDO", r => string.Equals(r.TipologiaFattura, "SECONDO SALDO", StringComparison.OrdinalIgnoreCase)),
        };

        await FilterCombinations.AssertAllSubsets(
            filters,
            () => new GestioneFattureDownloadQuery(AdminAuth()),
            q => ExecuteQueryOrIgnoreMissingView(() => _handler.Send(q)),
            r => string.Equals(r.Ente, ente1, StringComparison.OrdinalIgnoreCase)
                 && string.Equals(r.TipologiaFattura, "SECONDO SALDO", StringComparison.OrdinalIgnoreCase)
                 && r.Anno == 2025 && r.Mese == 1);
    }

    [Test]
    /// <summary>
    /// Body reale del FE con SOLO idTipoContratto valorizzato (idEnti/mesi vuoti, tipologiaFattura/anno/
    /// azione null): deve restituire solo righe con IdTipoContratto=2 ed escludere quelle con
    /// IdTipoContratto=1 (seed: ente3/ANTICIPO).
    /// </summary>
    public async Task GestioneFattureDownloadQuery_SoloIdTipoContratto_EscludeGliAltriTipi()
    {
        var result = await ExecuteQueryOrIgnoreMissingView(() =>
            _handler.Send(new GestioneFattureDownloadQuery(AdminAuth()) { IdTipoContratto = 2 }));

        var rows = result!.GestioneFatture?.ToList() ?? [];
        Assume.That(rows.Count, Is.GreaterThan(0), "Container non seedato come atteso.");

        Assert.Multiple(() =>
        {
            Assert.That(rows.All(r => r.IdTipoContratto == 2), Is.True,
                "Con solo idTipoContratto=2 devono tornare esclusivamente righe IdTipoContratto=2.");
            Assert.That(rows.Any(r => r.IdTipoContratto == 1), Is.False,
                "Le righe con IdTipoContratto=1 (es. ente3/ANTICIPO) devono essere escluse.");
        });
    }

    [Test]
    /// <summary>
    /// IN multi-valore: Mesi[1,2] + IdEnti[ente1,ente2] su Anno 2025 devono restringere alle sole righe
    /// con mese in {1,2} ed ente in {ente1,ente2}, escludendo la riga ente3/mese3.
    /// </summary>
    public async Task GestioneFattureDownloadQuery_MultiValueIn_Mesi_eIdEnti_RestringonoCorrettamente()
    {
        string[] enti = ["11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222"];
        const string ente3 = "33333333-3333-3333-3333-333333333333";

        var res = await ExecuteQueryOrIgnoreMissingView(() => _handler.Send(new GestioneFattureDownloadQuery(AdminAuth())
        { Anno = 2025, Mesi = new[] { 1, 2 }, IdEnti = enti }));

        var rows = res!.GestioneFatture?.ToList() ?? [];
        Assume.That(rows.Count, Is.GreaterThan(0), "Container non seedato come atteso.");

        Assert.Multiple(() =>
        {
            Assert.That(rows.All(r => r.Mese is 1 or 2), Is.True, "IN Mesi[1,2] deve escludere gli altri mesi.");
            Assert.That(rows.All(r => enti.Contains(r.Ente, StringComparer.OrdinalIgnoreCase)), Is.True, "IN IdEnti deve escludere gli altri enti.");
            Assert.That(rows.Any(r => string.Equals(r.Ente, ente3, StringComparison.OrdinalIgnoreCase)), Is.False, "La riga ente3/mese3 non deve comparire.");
        });
    }

    [Test]
    /// <summary>
    /// Combinazione contraddittoria: ente1 (IdTipoContratto=2 nel seed) filtrato per IdTipoContratto=1
    /// deve restituire ZERO righe.
    /// </summary>
    public async Task GestioneFattureDownloadQuery_FiltriContraddittori_RestituisceZero()
    {
        const string ente1 = "11111111-1111-1111-1111-111111111111";
        var res = await ExecuteQueryOrIgnoreMissingView(() => _handler.Send(new GestioneFattureDownloadQuery(AdminAuth())
        { IdEnti = new[] { ente1 }, IdTipoContratto = 1 }));

        var rows = res!.GestioneFatture?.ToList() ?? [];
        Assert.Multiple(() =>
        {
            Assert.That(rows, Is.Empty, "ente1 + IdTipoContratto=1 non deve avere righe.");
            Assert.That(res.Count, Is.EqualTo(0));
        });
    }

    // Chiave completa per OGNI riga seed 2025 (target multipli), lato download: filtri = 5 (no Azione).
    [TestCase("33333333-3333-3333-3333-333333333333", "ANTICIPO",      2025, 3, 1)]
    [TestCase("11111111-1111-1111-1111-111111111111", "SECONDO SALDO", 2025, 1, 2)]
    [TestCase("22222222-2222-2222-2222-222222222222", "PRIMO SALDO",   2025, 2, 2)]
    public async Task GestioneFattureDownloadQuery_ChiaveCompletaPerOgniSeed2025_RestituisceEsattamenteQuellaRiga(
        string ente, string tipologia, int anno, int mese, int idTipo)
    {
        var res = await ExecuteQueryOrIgnoreMissingView(() => _handler.Send(new GestioneFattureDownloadQuery(AdminAuth())
        {
            IdEnti = new[] { ente }, TipologiaFattura = tipologia, Anno = anno, Mesi = new[] { mese }, IdTipoContratto = idTipo
        }));

        var rows = res!.GestioneFatture?.ToList() ?? [];
        Assume.That(rows.Count, Is.GreaterThan(0), "Container non seedato come atteso.");

        Assert.Multiple(() =>
        {
            Assert.That(rows.Count, Is.EqualTo(1), "La chiave completa deve identificare esattamente una riga seed 2025.");
            var r = rows[0];
            Assert.That(string.Equals(r.Ente, ente, StringComparison.OrdinalIgnoreCase), "Ente");
            Assert.That(string.Equals(r.TipologiaFattura, tipologia, StringComparison.OrdinalIgnoreCase), "TipologiaFattura");
            Assert.That(r.Anno, Is.EqualTo(anno), "Anno");
            Assert.That(r.Mese, Is.EqualTo(mese), "Mese");
            Assert.That(r.IdTipoContratto, Is.EqualTo(idTipo), "IdTipoContratto");
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
