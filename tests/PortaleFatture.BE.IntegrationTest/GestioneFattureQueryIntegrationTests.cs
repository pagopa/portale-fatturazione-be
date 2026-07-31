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
            Anno = 2025, // griglia seedata: 3 righe deterministiche (niente più dipendenza da dati UAT)
            Page = page,
            Size = pageSize
        }));

        Assert.That(result, Is.Not.Null);

        var rows = result!.GestioneFatture?.ToList() ?? [];
        Assert.That(result.Count, Is.GreaterThan(0),
            "Il seed griglia 2025 deve contenere righe: invariante di paginazione verificabile.");

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
            Anno = 2025, // griglia seedata deterministica
            Page = 1,
            Size = 300
        }));

        var seed = PickSeedRowOrInconclusive(baseline!.GestioneFatture);

        var filtered = await ExecuteQueryOrIgnoreMissingView(() => _handler.Send(new GestioneFattureQuery(AdminAuth())
        {
            Anno = 2025,
            Mesi = new[] { seed.Mese },
            TipologiaFattura = seed.TipologiaFattura,
            Azione = seed.Azione,
            IdEnti = new[] { seed.Ente! },
            Page = 1,
            Size = 200
        }));

        var rows = filtered!.GestioneFatture?.ToList() ?? [];
        Assert.That(rows.Count, Is.GreaterThan(0),
            "Con i filtri combinati sul seed 2025 la riga target deve comparire.");

        Assert.Multiple(() =>
        {
            Assert.That(rows.All(r => r.Anno == 2025), Is.True);
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
            Anno = 2025, // griglia seedata deterministica
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
            Anno = 2025,
            Mesi = new[] { seed.Mese },
            IdEnti = new[] { seed.Ente! },
            TipologiaFattura = seed.TipologiaFattura,
            Azione = seed.Azione,
            Page = 1,
            Size = 200
        }));

        var multi = await ExecuteQueryOrIgnoreMissingView(() => _handler.Send(new GestioneFattureQuery(AdminAuth())
        {
            Anno = 2025,
            Mesi = candidateMonths.ToArray(),
            IdEnti = candidateEnti.ToArray(),
            TipologiaFattura = seed.TipologiaFattura,
            Azione = seed.Azione,
            Page = 1,
            Size = 200
        }));

        var rows = multi!.GestioneFatture?.ToList() ?? [];
        Assert.That(rows.Count, Is.GreaterThan(0),
            "Con i filtri AND multi-value sul seed 2025 devono comparire righe.");

        Assert.Multiple(() =>
        {
            Assert.That(rows.All(r => r.Anno == 2025), Is.True);
            Assert.That(rows.All(r => candidateMonths.Contains(r.Mese)), Is.True);
            Assert.That(rows.All(r => candidateEnti.Contains(r.Ente!, StringComparer.OrdinalIgnoreCase)), Is.True);
            Assert.That(rows.All(r => string.Equals(r.TipologiaFattura, seed.TipologiaFattura, StringComparison.OrdinalIgnoreCase)), Is.True);
            Assert.That(rows.All(r => string.Equals(r.Azione, seed.Azione, StringComparison.OrdinalIgnoreCase)), Is.True);
            Assert.That(multi.Count, Is.GreaterThanOrEqualTo(single!.Count));
        });
    }

    [Test]
    /// <summary>
    /// Fix rename TipologiaContratto -> IdTipoContratto: il filtro per tipo contratto (colonna DB
    /// IdTipoContratto) deve restringere davvero i risultati della griglia sul DB seedato. Seed su
    /// Anno 2025: IdTipoContratto=1 (1 riga) e IdTipoContratto=2 (>=2 righe). Invarianti pollution-proof
    /// (ogni riga filtrata ha esattamente l'IdTipoContratto richiesto) + narrowing.
    /// </summary>
    public async Task GestioneFattureQuery_FiltroIdTipoContratto_RestringeIRisultati()
    {
        const int anno = 2025; // le righe seed della griglia sono su questo anno

        var tutte = await ExecuteQueryOrIgnoreMissingView(() => _handler.Send(new GestioneFattureQuery(AdminAuth())
        { Anno = anno, Page = 1, Size = 500 }));
        var soloTipo1 = await ExecuteQueryOrIgnoreMissingView(() => _handler.Send(new GestioneFattureQuery(AdminAuth())
        { Anno = anno, IdTipoContratto = 1, Page = 1, Size = 500 }));
        var soloTipo2 = await ExecuteQueryOrIgnoreMissingView(() => _handler.Send(new GestioneFattureQuery(AdminAuth())
        { Anno = anno, IdTipoContratto = 2, Page = 1, Size = 500 }));

        var rTutte = tutte!.GestioneFatture?.ToList() ?? [];
        var r1 = soloTipo1!.GestioneFatture?.ToList() ?? [];
        var r2 = soloTipo2!.GestioneFatture?.ToList() ?? [];

        Assume.That(rTutte.Count, Is.GreaterThan(0),
            "Nessuna riga griglia per l'anno seed 2025: container non seedato come atteso.");

        Assert.Multiple(() =>
        {
            // invariante: ogni riga filtrata ha ESATTAMENTE l'IdTipoContratto richiesto (regge anche con pollution)
            Assert.That(r1.All(x => x.IdTipoContratto == 1), Is.True,
                "Il filtro IdTipoContratto=1 deve restituire solo righe con IdTipoContratto=1.");
            Assert.That(r2.All(x => x.IdTipoContratto == 2), Is.True,
                "Il filtro IdTipoContratto=2 deve restituire solo righe con IdTipoContratto=2.");
            Assert.That(r1.Count, Is.GreaterThanOrEqualTo(1), "Il seed garantisce almeno una riga IdTipoContratto=1.");
            Assert.That(r2.Count, Is.GreaterThanOrEqualTo(1), "Il seed garantisce righe IdTipoContratto=2.");
            // narrowing: filtrare per un tipo restituisce meno del totale (il seed contiene entrambi i tipi)
            Assert.That(r1.Count, Is.LessThan(rTutte.Count),
                "Filtrare per un tipo deve restituire meno righe del totale: prova che il filtro applica.");
        });
    }

    [Test]
    /// <summary>
    /// Copre TUTTE le combinazioni (2^6) dei filtri della griglia: IdEnti, IdTipoContratto, Anno, Mesi,
    /// TipologiaFattura, Azione. Per ogni combinazione verifica l'invariante AND (ogni riga restituita
    /// rispetta i filtri attivi) e la presenza della riga target seed
    /// (Ente1/SECONDO SALDO/2025/mese 1/POSTICIPATA/IdTipoContratto=2). Include il caso "solo idTipoContratto".
    /// </summary>
    public async Task GestioneFattureQuery_TutteLeCombinazioniFiltri_RispettanoAND_eIncludonoTarget()
    {
        const string ente1 = "11111111-1111-1111-1111-111111111111";

        var filters = new (string, Action<GestioneFattureQuery>, Func<SimpleGestioneFattureDto, bool>)[]
        {
            ("IdEnti",           q => q.IdEnti = new[] { ente1 },           r => string.Equals(r.Ente, ente1, StringComparison.OrdinalIgnoreCase)),
            ("IdTipoContratto",  q => q.IdTipoContratto = 2,                r => r.IdTipoContratto == 2),
            ("Anno",             q => q.Anno = 2025,                        r => r.Anno == 2025),
            ("Mesi",             q => q.Mesi = new[] { 1 },                 r => r.Mese == 1),
            ("TipologiaFattura", q => q.TipologiaFattura = "SECONDO SALDO", r => string.Equals(r.TipologiaFattura, "SECONDO SALDO", StringComparison.OrdinalIgnoreCase)),
            ("Azione",           q => q.Azione = "POSTICIPATA",             r => string.Equals(r.Azione, "POSTICIPATA", StringComparison.OrdinalIgnoreCase)),
        };

        await FilterCombinations.AssertAllSubsets(
            filters,
            () => new GestioneFattureQuery(AdminAuth()) { Page = 1, Size = 500 },
            q => ExecuteQueryOrIgnoreMissingView(() => _handler.Send(q)),
            r => string.Equals(r.Ente, ente1, StringComparison.OrdinalIgnoreCase)
                 && string.Equals(r.TipologiaFattura, "SECONDO SALDO", StringComparison.OrdinalIgnoreCase)
                 && r.Anno == 2025 && r.Mese == 1);
    }

    [Test]
    /// <summary>
    /// IN multi-valore: Mesi[1,2] + IdEnti[ente1,ente2] su Anno 2025 devono restringere alle sole righe
    /// con mese in {1,2} ed ente in {ente1,ente2}, escludendo la riga ente3/mese3.
    /// </summary>
    public async Task GestioneFattureQuery_MultiValueIn_Mesi_eIdEnti_RestringonoCorrettamente()
    {
        string[] enti = ["11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222"];
        const string ente3 = "33333333-3333-3333-3333-333333333333";

        var res = await ExecuteQueryOrIgnoreMissingView(() => _handler.Send(new GestioneFattureQuery(AdminAuth())
        { Anno = 2025, Mesi = new[] { 1, 2 }, IdEnti = enti, Page = 1, Size = 500 }));

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
    /// deve restituire ZERO righe (i filtri in AND non hanno intersezione).
    /// </summary>
    public async Task GestioneFattureQuery_FiltriContraddittori_RestituisceZero()
    {
        const string ente1 = "11111111-1111-1111-1111-111111111111";
        var res = await ExecuteQueryOrIgnoreMissingView(() => _handler.Send(new GestioneFattureQuery(AdminAuth())
        { IdEnti = new[] { ente1 }, IdTipoContratto = 1, Page = 1, Size = 500 }));

        var rows = res!.GestioneFatture?.ToList() ?? [];
        Assert.Multiple(() =>
        {
            Assert.That(rows, Is.Empty, "ente1 + IdTipoContratto=1 non deve avere righe.");
            Assert.That(res.Count, Is.EqualTo(0));
        });
    }

    // Chiave completa per OGNI riga seed 2025 (target multipli): ogni riga deve essere individuabile con
    // tutti i filtri impostati sui suoi valori, restituendo esattamente quella. Solo Anno 2025 (statico).
    [TestCase("33333333-3333-3333-3333-333333333333", "ANTICIPO",      2025, 3, "ELIMINATA",    1)]
    [TestCase("11111111-1111-1111-1111-111111111111", "SECONDO SALDO", 2025, 1, "POSTICIPATA",  2)]
    [TestCase("22222222-2222-2222-2222-222222222222", "PRIMO SALDO",   2025, 2, "RIPRISTINATA", 2)]
    public async Task GestioneFattureQuery_ChiaveCompletaPerOgniSeed2025_RestituisceEsattamenteQuellaRiga(
        string ente, string tipologia, int anno, int mese, string azione, int idTipo)
    {
        var res = await ExecuteQueryOrIgnoreMissingView(() => _handler.Send(new GestioneFattureQuery(AdminAuth())
        {
            IdEnti = new[] { ente }, TipologiaFattura = tipologia, Anno = anno, Mesi = new[] { mese },
            Azione = azione, IdTipoContratto = idTipo, Page = 1, Size = 500
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
            Assert.That(string.Equals(r.Azione, azione, StringComparison.OrdinalIgnoreCase), "Azione");
            Assert.That(r.IdTipoContratto, Is.EqualTo(idTipo), "IdTipoContratto");
        });
    }

    [Test]
    /// <summary>
    /// Paginazione sul seed: Anno 2025 ha 3 righe griglia statiche. Con Size=2 le due pagine devono
    /// rispettare la dimensione, riportare lo stesso Count totale (3) e coprire tutte le righe senza duplicati.
    /// </summary>
    public async Task GestioneFattureQuery_Paginazione_SuSeed2025_CopreTutteLeRigheSenzaDuplicati()
    {
        static string Id(SimpleGestioneFattureDto r) => $"{r.Ente}|{r.TipologiaFattura}|{r.Anno}|{r.Mese}";

        var p1 = await ExecuteQueryOrIgnoreMissingView(() => _handler.Send(new GestioneFattureQuery(AdminAuth())
        { Anno = 2025, Page = 1, Size = 2 }));
        var p2 = await ExecuteQueryOrIgnoreMissingView(() => _handler.Send(new GestioneFattureQuery(AdminAuth())
        { Anno = 2025, Page = 2, Size = 2 }));

        var r1 = p1!.GestioneFatture?.ToList() ?? [];
        var r2 = p2!.GestioneFatture?.ToList() ?? [];
        Assume.That(p1.Count, Is.EqualTo(3), "Seed atteso: 3 righe griglia per Anno 2025.");

        var union = r1.Select(Id).Concat(r2.Select(Id)).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(r1.Count, Is.LessThanOrEqualTo(2), "Page 1 rispetta Size=2.");
            Assert.That(r2.Count, Is.LessThanOrEqualTo(2), "Page 2 rispetta Size=2.");
            Assert.That(p2.Count, Is.EqualTo(3), "Il Count totale e' indipendente dalla pagina.");
            Assert.That(union.Distinct().Count(), Is.EqualTo(3), "Le pagine coprono tutte le righe senza duplicati.");
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