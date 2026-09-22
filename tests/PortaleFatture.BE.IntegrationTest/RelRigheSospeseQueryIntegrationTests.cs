using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// `RelRigheSospeseQueryGetById` — le righe delle REL **in staging**, cioe' il contenuto del report
/// di dettaglio che `CreateRelSospese` carica sul blob. Come la gemella del flusso ordinario, nessun
/// endpoint la usa: la legge solo quella Function.
///
/// IL PUNTO DI QUESTA FIXTURE, ed e' il motivo per cui non ricalca le attese dell'altra: qui la
/// scelta fra "filtra per semestre" e "filtra per anno/mese" e' ancora quella **TESTUALE**
/// (`contains "var" | "semestrale" | "annuale"`), non allineata alla regola concordata con DATA il
/// 14/09/2026 — ed e' **voluto**: chiarito il 22/09/2026 che quella regola riguarda il solo flusso
/// ordinario (v. `docs/pipeline-dati-send.md`).
///
/// Conseguenza concreta, che senza un test resterebbe affidata alla memoria di qualcuno: una
/// `VAR. ANNUALE` **sospesa** segue il semestre, mentre la stessa tipologia nel flusso ordinario
/// segue anno/mese. Chi un domani "uniformasse" le due persistence per simmetria troverebbe questo
/// test rosso: va letto come una domanda da riportare a DATA, non come un test da aggiustare.
///
/// Il seed vive in `tests/Data/gestione_fatture.sql` (anno **2030**, ente1/TOKEN-E1) ed e' costruito
/// apposta perche' l'unica cosa che puo' cambiare il risultato sia il ramo scelto dal codice: le
/// coppie aprile/maggio di SECONDO SALDO e VAR. ANNUALE sono identiche fra loro, stesso
/// `FlagConguaglio`, e differiscono solo per la tipologia.
/// </summary>
public class RelRigheSospeseQueryIntegrationTests
{
    private const string Ente = "11111111-1111-1111-1111-111111111111";
    private const string Contratto = "TOKEN-E1";
    private const int Anno = 2030;

    private IMediator _handler = null!;

    [SetUp]
    public void Setup()
    {
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "pfd.tmpRelRighe");
        _handler = ServiceProvider.GetRequiredService<IMediator>(LocalTestDb.ConnectionString);
    }

    // ---------------------------------------------------------------------------------------------
    // Il ramo anno/mese
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// `SECONDO SALDO` filtra per anno/mese, e lo fa **benche' la testata abbia il `FlagConguaglio`
    /// valorizzato**: il flag arriva fino alla query (l'handler lo legge dalla testata e lo scrive nel
    /// command) e semplicemente non entra nella WHERE. Senza il flag sul seed, questo test non
    /// distinguerebbe "flag ignorato" da "flag assente".
    /// </summary>
    [Test]
    public async Task SecondoSaldo_ShouldFiltrarePerAnnoMese_IgnorandoIlFlagConguaglio()
    {
        var righe = await Righe("SECONDO SALDO", 4);

        Assert.That(righe.Select(r => r.IdNotifica), Is.EquivalentTo(new[] { "SOSP-SD-APR" }),
            "il seed ha anche SOSP-SD-MAG con lo stesso FlagConguaglio: se compare, il SECONDO SALDO "
            + "sospeso e' finito nel ramo del conguaglio.");
    }

    /// <summary>
    /// `PRIMO SALDO` porta con se' le righe `ASSEVERAZIONE` dello stesso periodo: e' un OR esplicito
    /// nella persistence, presente solo per questa tipologia.
    /// </summary>
    [Test]
    public async Task PrimoSaldo_ShouldIncludereAncheLAsseverazione()
    {
        var righe = await Righe("PRIMO SALDO", 4);

        Assert.That(righe.Select(r => r.IdNotifica),
            Is.EquivalentTo(new[] { "SOSP-PS-APR", "SOSP-ASS-APR" }));
    }

    // ---------------------------------------------------------------------------------------------
    // Il ramo semestre: qui i due flussi divergono, ed e' deliberato
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// LA DIVERGENZA. Nel flusso ordinario `VAR. ANNUALE` filtra per anno/mese dal 14/09/2026; qui
    /// segue ancora il **semestre**, quindi chiedendo aprile tornano anche le righe di maggio.
    ///
    /// Non e' un difetto e non va "corretto" per simmetria: chiarito con DATA il 22/09/2026 che la
    /// regola nuova riguarda il solo flusso ordinario. Se un domani si decidesse di allinearlo, questo
    /// test va **riscritto** insieme alla persistence, non cancellato.
    /// </summary>
    [Test]
    public async Task VarAnnuale_ShouldSeguireIlSemestre_AlContrarioDelFlussoOrdinario()
    {
        var righe = await Righe("VAR. ANNUALE", 4);

        Assert.That(righe.Select(r => r.IdNotifica),
            Is.EquivalentTo(new[] { "SOSP-VA-APR", "SOSP-VA-MAG" }),
            "nel flusso SOSPESE la scelta del ramo e' ancora testuale: contains(\"annuale\") manda "
            + "questa tipologia sul FlagConguaglio. Se torna solo aprile, qualcuno ha allineato la "
            + "persistence a quella ordinaria: verificare con DATA prima di aggiustare il test.");
    }

    /// <summary>
    /// Contro-prova del test precedente sullo stesso dato: chiedendo **maggio** l'insieme non cambia,
    /// perche' il mese non partecipa al filtro sul ramo semestre. E' cio' che distingue "filtra per
    /// semestre" da "filtra per anno/mese" senza doversi fidare del nome del ramo.
    /// </summary>
    [Test]
    public async Task VarAnnuale_ChiestaDaUnMeseDiverso_ShouldRestituireLoStessoInsieme()
    {
        var aprile = await Righe("VAR. ANNUALE", 4);
        var maggio = await Righe("VAR. ANNUALE", 5);

        Assert.That(maggio.Select(r => r.IdNotifica), Is.EquivalentTo(aprile.Select(r => r.IdNotifica)));
    }

    // ---------------------------------------------------------------------------------------------
    // Guardie
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// La persistence rifiuta se l'ente autenticato non e' quello della chiave: e' l'unica barriera
    /// fra un `IdTestata` e i dati di un altro aderente, e vale la pena che sia presidiata.
    ///
    /// ⚠️ La `DomainException` e' sollevata **con messaggio vuoto** (`throw new DomainException("")`):
    /// nei log non resta nulla che spieghi il rifiuto. Caratterizzato qui perche' si veda.
    /// </summary>
    [Test]
    public void EnteDiversoDaQuelloDellaChiave_ShouldSollevareDomainException()
    {
        var chiave = $"22222222-2222-2222-2222-222222222222_{Contratto}_SECONDO-SALDO_{Anno}_4";

        var ex = Assert.ThrowsAsync<DomainException>(async () =>
            await _handler.Send(new RelRigheSospeseQueryGetById(Auth(Ente)) { IdTestata = chiave }));

        Assert.That(ex!.Message, Is.Empty, "oggi il rifiuto non porta con se' alcun messaggio");
    }

    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Il `FlagConguaglio` non si passa: l'handler lo legge dalla testata in `pfd.tmpRelTestata` e
    /// sovrascrive quello del command — esattamente come nel flusso ordinario. Per questo il seed ha
    /// anche le testate 2030/4, senza le quali si otterrebbe una `NullReferenceException`.
    /// </summary>
    private async Task<List<RigheRelDto>> Righe(string tipologia, int mese)
    {
        var chiave = $"{Ente}_{Contratto}_{tipologia.Replace(" ", "-")}_{Anno}_{mese}";

        var righe = await _handler.Send(new RelRigheSospeseQueryGetById(Auth(Ente)) { IdTestata = chiave });

        return righe?.ToList() ?? [];
    }

    private static AuthenticationInfo Auth(string idEnte) => new()
    {
        Id = "integration-test-relrighe-sospese",
        IdEnte = idEnte,
        Prodotto = "prod-pn",
        Ruolo = Ruolo.ADMIN,
        IdTipoContratto = 1
    };
}
