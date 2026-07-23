using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Integration test su query reale (MediatR + persistence + DB UAT) per la funzionalità
/// usata dall'endpoint POST /api/fatture/pagopa/gestione-fatture.
///
/// Nota: questi test non istanziano un host HTTP, ma validano le invarianti dati/paginazione
/// del layer che l'endpoint invoca direttamente.
/// </summary>
public class GestioneFattureQueryIntegrationTests
{
    private IMediator _handler;
    private IConfiguration _conf;

    [SetUp]
    public void Setup()
    {
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
        _conf = ServiceProvider.GetRequiredService<IConfiguration>();
    }

    private static AuthenticationInfo AdminAuth() => new()
    {
        IdEnte = Guid.NewGuid().ToString(),
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };

    private int ConfAnno => int.TryParse(_conf["IntegrationTest:Anno"], out var a) ? a : 2026;

    [Test]
    /// <summary>
    /// Verifica che la query ritorni un DTO valido anche senza parametri di paginazione (Page e Size nulli)
    /// </summary>
    public async Task GestioneFattureQuery_WithoutPaging_ShouldExecute_AndReturnDto()
    {
        var result = await ExecuteQueryOrIgnoreMissingView(() => _handler.Send(new GestioneFattureQuery(AdminAuth())
        {
            Anno = ConfAnno,
            Page = null,
            Size = null
        }));

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.GreaterThanOrEqualTo(0));
        Assert.That(result.GestioneFatture, Is.Not.Null);
    }

    [Test]
    /// <summary>
    /// Verifica che la query rispetti il parametro di paginazione "Size" 
    /// </summary>
    public async Task GestioneFattureQuery_WithPaging_WhenDataPresent_ShouldRespectPageSize()
    {
        const int page = 1;
        const int pageSize = 5;

        var result = await ExecuteQueryOrIgnoreMissingView(() => _handler.Send(new GestioneFattureQuery(AdminAuth())
        {
            Anno = ConfAnno,
            Page = page,
            Size = pageSize
        }));

        Assert.That(result, Is.Not.Null);

        var rows = result!.GestioneFatture?.ToList() ?? [];
        Assume.That(result.Count, Is.GreaterThan(0),
            "Nessun dato trovato per il periodo configurato: impossibile verificare l'invariante di paginazione.");

        Assert.Multiple(() =>
        {
            Assert.That(rows.Count, Is.LessThanOrEqualTo(pageSize));
            Assert.That(result.Count, Is.GreaterThanOrEqualTo(rows.Count));
        });
    }

    [Test]
    /// <summary>
    /// Verifica che la query ritorni zero righe quando il filtro "TipologiaFattura" non corrisponde 
    /// a nessun record
    /// </summary>
    public async Task GestioneFattureQuery_WithNonExistingTipologia_ShouldReturnNoRows()
    {
        var result = await ExecuteQueryOrIgnoreMissingView(() => _handler.Send(new GestioneFattureQuery(AdminAuth())
        {
            Anno = ConfAnno,
            TipologiaFattura = "__NO_MATCH__",
            Page = 1,
            Size = 10
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
    /// Regressione comportamento corrente: il campo Note è valorizzato nella query MediatR ma non viene
    /// applicato come filtro SQL in GestioneFattureQueryPersistance.
    /// </summary>
    public async Task GestioneFattureQuery_WithAndWithoutNote_ShouldReturnSameResult_CurrentBehavior()
    {
        var baseQuery = new GestioneFattureQuery(AdminAuth())
        {
            Anno = ConfAnno,
            Page = 1,
            Size = 20
        };

        var withNoNote = await ExecuteQueryOrIgnoreMissingView(() => _handler.Send(baseQuery));

        var withNote = await ExecuteQueryOrIgnoreMissingView(() => _handler.Send(new GestioneFattureQuery(AdminAuth())
        {
            Anno = ConfAnno,
            Page = 1,
            Size = 20,
            Note = "qualunque-nota"
        }));

        Assert.That(withNoNote, Is.Not.Null);
        Assert.That(withNote, Is.Not.Null);

        var noNoteRows = withNoNote!.GestioneFatture?.ToList() ?? [];
        var withNoteRows = withNote!.GestioneFatture?.ToList() ?? [];

        Assert.Multiple(() =>
        {
            Assert.That(withNoNote.Count, Is.EqualTo(withNote.Count));
            Assert.That(noNoteRows.Count, Is.EqualTo(withNoteRows.Count));
        });
    }

    [Test]
    /// <summary>
    /// Data-dependent (UAT): verifica filtri combinati AND con tutti i campi
    /// [Anno + Mesi + TipologiaFattura + Azione + IdEnti].
    /// Il seed viene ricavato dai dati reali per evitare assunzioni hardcoded.
    /// </summary>
    public async Task GestioneFattureQuery_WithCombinedAndFilters_ShouldReturnOnlyMatchingRows()
    {
        var baseline = await ExecuteQueryOrIgnoreMissingView(() => _handler.Send(new GestioneFattureQuery(AdminAuth())
        {
            Anno = ConfAnno,
            Page = 1,
            Size = 300
        }));

        var seed = PickSeedRowOrInconclusive(baseline!.GestioneFatture);

        var filtered = await ExecuteQueryOrIgnoreMissingView(() => _handler.Send(new GestioneFattureQuery(AdminAuth())
        {
            Anno = ConfAnno,
            Mesi = new[] { seed.Mese },
            TipologiaFattura = seed.TipologiaFattura,
            Azione = seed.Azione,
            IdEnti = new[] { seed.Ente! },
            Page = 1,
            Size = 200
        }));

        var rows = filtered!.GestioneFatture?.ToList() ?? [];
        Assume.That(rows.Count, Is.GreaterThan(0),
            "Nessuna riga con filtri combinati: dataset UAT non copre il seed nel periodo configurato.");

        Assert.Multiple(() =>
        {
            Assert.That(rows.All(r => r.Anno == ConfAnno), Is.True);
            Assert.That(rows.All(r => r.Mese == seed.Mese), Is.True);
            Assert.That(rows.All(r => string.Equals(r.TipologiaFattura, seed.TipologiaFattura, StringComparison.OrdinalIgnoreCase)), Is.True);
            Assert.That(rows.All(r => string.Equals(r.Azione, seed.Azione, StringComparison.OrdinalIgnoreCase)), Is.True);
            Assert.That(rows.All(r => string.Equals(r.Ente, seed.Ente, StringComparison.OrdinalIgnoreCase)), Is.True);
        });
    }

    [Test]
    /// <summary>
    /// Data-dependent (UAT): verifica filtri AND con insiemi multipli per Mesi/IdEnti
    /// mantenendo TipologiaFattura e Azione fissi. Utile per validare semantica IN(...) + AND.
    /// </summary>
    public async Task GestioneFattureQuery_WithCombinedAndFilters_MultiValues_ShouldRespectInClauses()
    {
        var baseline = await ExecuteQueryOrIgnoreMissingView(() => _handler.Send(new GestioneFattureQuery(AdminAuth())
        {
            Anno = ConfAnno,
            Page = 1,
            Size = 500
        }));

        var seed = PickSeedRowOrInconclusive(baseline!.GestioneFatture);
        var baseRows = baseline.GestioneFatture?.ToList() ?? [];

        var candidateMonths = baseRows
            .Where(x => x.Mese > 0)
            .Select(x => x.Mese)
            .Distinct()
            .Take(2)
            .ToList();

        if (!candidateMonths.Contains(seed.Mese))
            candidateMonths.Insert(0, seed.Mese);

        var candidateEnti = baseRows
            .Where(x => !string.IsNullOrWhiteSpace(x.Ente))
            .Select(x => x.Ente!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToList();

        if (!candidateEnti.Contains(seed.Ente!, StringComparer.OrdinalIgnoreCase))
            candidateEnti.Insert(0, seed.Ente!);

        var single = await ExecuteQueryOrIgnoreMissingView(() => _handler.Send(new GestioneFattureQuery(AdminAuth())
        {
            Anno = ConfAnno,
            Mesi = new[] { seed.Mese },
            IdEnti = new[] { seed.Ente! },
            TipologiaFattura = seed.TipologiaFattura,
            Azione = seed.Azione,
            Page = 1,
            Size = 200
        }));

        var multi = await ExecuteQueryOrIgnoreMissingView(() => _handler.Send(new GestioneFattureQuery(AdminAuth())
        {
            Anno = ConfAnno,
            Mesi = candidateMonths.ToArray(),
            IdEnti = candidateEnti.ToArray(),
            TipologiaFattura = seed.TipologiaFattura,
            Azione = seed.Azione,
            Page = 1,
            Size = 200
        }));

        var rows = multi!.GestioneFatture?.ToList() ?? [];
        Assume.That(rows.Count, Is.GreaterThan(0),
            "Nessuna riga con filtri AND multi-value: dataset UAT non copre combinazioni utili nel periodo configurato.");

        Assert.Multiple(() =>
        {
            Assert.That(rows.All(r => r.Anno == ConfAnno), Is.True);
            Assert.That(rows.All(r => candidateMonths.Contains(r.Mese)), Is.True);
            Assert.That(rows.All(r => candidateEnti.Contains(r.Ente!, StringComparer.OrdinalIgnoreCase)), Is.True);
            Assert.That(rows.All(r => string.Equals(r.TipologiaFattura, seed.TipologiaFattura, StringComparison.OrdinalIgnoreCase)), Is.True);
            Assert.That(rows.All(r => string.Equals(r.Azione, seed.Azione, StringComparison.OrdinalIgnoreCase)), Is.True);
            Assert.That(multi.Count, Is.GreaterThanOrEqualTo(single!.Count));
        });
    }

    /// <summary>
    /// Esegue una query e ignora l'errore di vista mancante, che può verificarsi in ambienti di test 
    /// non completi (es. Dev o Test) dove la vista SQL non è stata creata.
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
    }

    private static SimpleGestioneFattureDto PickSeedRowOrInconclusive(IEnumerable<SimpleGestioneFattureDto>? rows)
    {
        var seed = rows?
            .FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x.Ente) &&
                !string.IsNullOrWhiteSpace(x.TipologiaFattura) &&
                !string.IsNullOrWhiteSpace(x.Azione) &&
                x.Mese > 0);

        Assume.That(seed, Is.Not.Null,
            "Dataset UAT non contiene una riga seed con Ente/TipologiaFattura/Azione valorizzati per test AND combinati.");

        return seed!;
    }
}