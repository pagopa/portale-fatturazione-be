using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using PortaleFatture.BE.Function.API.Middleware;

namespace PortaleFatture.BE.UnitTest.FunctionApi;

/// <summary>
/// Il contratto che i grandi aderenti leggono davvero.
///
/// Le Integration API non hanno un client generato ne' un consumer-driven contract: l'aderente (INPS
/// e affini) legge lo **Swagger** pubblicato su
/// https://integration.uat.portalefatturazione.pagopa.it/api/swagger/ui e ci costruisce sopra la
/// propria deserializzazione. Quello Swagger e' generato dagli attributi [OpenApi*] sugli handler —
/// attributi che **nessuno verifica**: sono dichiarazioni, non codice eseguito. Se il tipo dichiarato
/// e quello davvero prodotto divergono, non fallisce niente: si accorge il cliente.
///
/// Due invarianti, entrambe su legami che il compilatore NON controlla:
///
///  1. le orchestrazioni si invocano per **stringa** (ScheduleNewOrchestrationInstanceAsync,
///     CallActivityAsync): un refuso rompe solo a runtime, alla chiamata dell'aderente;
///  2. il tipo dichiarato come corpo della risposta 200 deve essere quello che l'orchestrazione
///     produce davvero — perche' e' quello che finisce nel campo `output` del polling.
///
/// Nota sul perche' e' un ibrido riflessione + sorgente: il nome dell'orchestrazione e' una stringa
/// dentro il corpo del metodo, non raggiungibile per riflessione. I TIPI si leggono invece per
/// riflessione, ed e' importante che sia cosi': un confronto per nome darebbe falsi positivi sulla
/// nullabilita' (`T` vs `T?`, identici in JSON) e su FattureListaDto, che **deriva** da
/// List&lt;FatturaDto&gt; e quindi serializza in modo identico.
/// </summary>
[TestFixture]
public class IntegrationApiContractTests
{
    /// <summary>
    /// Handler il cui tipo dichiarato in Swagger NON coincide con quello prodotto, e che sono
    /// difetti noti e aperti. Elenco chiuso di proposito: un disallineamento nuovo fa fallire il
    /// test. Toglierne uno da qui quando viene corretto.
    /// </summary>
    private static readonly string[] DisallineamentiNoti =
    [
        "ModuloCommessaPostByAnnoHandler"
    ];

    // --- 1. i legami per stringa --------------------------------------------------------------

    /// <summary>
    /// Ogni nome di orchestrazione o activity invocato deve corrispondere a un [Function] davvero
    /// dichiarato. E' un guardrail preventivo: oggi le 64 invocazioni combaciano tutte con i 98
    /// [Function] dichiarati, e va bene cosi' — serve a intercettare al build un refuso che
    /// altrimenti si vedrebbe solo in produzione.
    ///
    /// Il rischio non e' teorico: nell'area Contestazioni convivono gia' due grafie della stessa
    /// parola (ScadenziarioContestazioniHandler come classe, ScadenzarioContestazioni* come nomi di
    /// Function), quindi basta copiare il nome dal posto sbagliato.
    /// </summary>
    [Test]
    public void OgniOrchestrazioneOActivityInvocata_ShouldCorrispondereAUnFunctionDichiarato()
    {
        var sorgenti = SorgentiDellaFunction();
        var dichiarati = sorgenti
            .SelectMany(t => Regex.Matches(t.Testo, @"Function\(""([^""]+)""\)").Select(m => m.Groups[1].Value))
            .ToHashSet(StringComparer.Ordinal);

        Assert.That(dichiarati, Is.Not.Empty, "Nessun [Function] trovato: il test non verificherebbe nulla.");

        var invocazioni = sorgenti
            .SelectMany(t => Regex
                .Matches(t.Testo, @"(?:ScheduleNewOrchestrationInstanceAsync|CallActivityAsync<[^(]*)\(\s*""([^""]+)""")
                .Select(m => (Nome: m.Groups[1].Value, File: t.Percorso)))
            .ToList();

        Assert.That(invocazioni, Is.Not.Empty, "Nessuna invocazione trovata: la regex non sta piu' agganciando nulla.");

        var orfane = invocazioni.Where(i => !dichiarati.Contains(i.Nome)).ToList();

        Assert.That(orfane, Is.Empty,
            "Invocazioni senza un [Function] corrispondente (fallirebbero solo a runtime): "
            + string.Join(", ", orfane.Select(o => $"\"{o.Nome}\" in {Path.GetFileName(o.File)}")));
    }

    // --- 2. il contratto pubblicato -----------------------------------------------------------

    [Test]
    public void IlTipoDichiaratoInSwagger_ShouldCoincidereConQuelloProdottoDallOrchestrazione()
    {
        var disallineati = Disallineamenti()
            .Where(d => !DisallineamentiNoti.Contains(d.Handler))
            .ToList();

        Assert.That(disallineati, Is.Empty,
            "Lo Swagger che l'aderente legge dichiara un tipo diverso da quello che l'API restituisce:\n"
            + string.Join("\n", disallineati.Select(d =>
                $"  {d.Handler}: dichiarato {Leggibile(d.Dichiarato)}, prodotto {Leggibile(d.Prodotto)}")));
    }

    /// <summary>
    /// 🔴 Difetto aperto. ModuloCommessaPostByAnnoHandler dichiara in Swagger
    /// IEnumerable&lt;DatiModuloCommessaByAnnoResponse&gt; ma l'orchestrazione produce
    /// IEnumerable&lt;ModuloCommessaPrevisionaleTotaleDto&gt;, che e' un tipo **non imparentato** col
    /// primo e con una forma diversa. Due esempi concreti della differenza:
    ///
    ///  - `Totale` e' `string?` nel tipo dichiarato e `decimal?` in quello prodotto: in JSON uno e'
    ///    "123.45" fra virgolette, l'altro 123.45 come numero. Un client tipizzato sullo Swagger
    ///    fallisce la deserializzazione;
    ///  - i campi non coincidono: il dichiarato ha DataModifica/TotaleDigitale/TotaleAnalogico, il
    ///    prodotto ha RagioneSociale/DataInserimento/DataChiusura (queste ultime DateTime, non
    ///    stringhe).
    ///
    /// Va deciso quale dei due e' la verita': se lo Swagger (allora l'orchestrazione deve mappare
    /// verso la Response) o l'implementazione (allora va corretto l'attributo, ma e' un **cambio di
    /// contratto** verso un aderente esterno e va concordato — stessa regola del 404/400 di
    /// GestioneFatture).
    ///
    /// Quando sara' risolto: togliere "ModuloCommessaPostByAnnoHandler" da DisallineamentiNoti e
    /// l'[Ignore] da qui.
    /// </summary>
    [Test]
    [Ignore("Difetto aperto: ModuloCommessaPostByAnnoHandler dichiara in Swagger un tipo diverso da "
          + "quello prodotto (Totale string vs decimal, campi non coincidenti). Correggerlo cambia un "
          + "contratto pubblico verso l'aderente e va concordato prima.")]
    public void NessunHandler_ShouldAvereUnContrattoSwaggerDisallineato()
    {
        Assert.That(Disallineamenti(), Is.Empty);
    }

    /// <summary>
    /// La lista delle eccezioni note deve restare **accurata**, in entrambe le direzioni. Senza
    /// questo test una voce diventerebbe stantia in silenzio: chi corregge il difetto vedrebbe la
    /// suite verde e non saprebbe di dover togliere la riga, e il confronto continuerebbe a essere
    /// disattivato su un handler ormai sano — cioe' proprio sul punto che si voleva presidiare.
    ///
    /// E' anche la prova che il test principale non passa a vuoto: se il confronto smettesse di
    /// rilevare qualunque disallineamento (regex che non aggancia piu', assegnabilita' troppo
    /// permissiva), questo diventerebbe rosso.
    /// </summary>
    [Test]
    public void LaListaDeiDisallineamentiNoti_ShouldEssereAccurata()
    {
        var reali = Disallineamenti().Select(d => d.Handler).ToHashSet(StringComparer.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(reali, Is.SupersetOf(DisallineamentiNoti),
                "Un disallineamento elencato come noto non esiste piu': e' stato corretto, "
                + "quindi va tolto da DisallineamentiNoti (e va tolto l'[Ignore] se era l'ultimo).");
            Assert.That(reali, Is.Not.Empty,
                "Nessun disallineamento rilevato: se e' davvero cosi' il difetto e' chiuso, "
                + "altrimenti la scansione si e' rotta e il test principale sta passando a vuoto.");
        });
    }

    /// <summary>
    /// Contro-prova che il test precedente stia guardando qualcosa: se la scansione non trovasse piu'
    /// handler — per una rinomina di cartelle, un cambio di attributi — entrambi i test sopra
    /// sarebbero verdi sul vuoto.
    /// </summary>
    [Test]
    public void LaScansione_ShouldTrovareGliHandlerHttpConIlLoroContratto()
    {
        var analizzati = Analizzati().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(analizzati, Has.Count.GreaterThan(20),
                "Attesi ~32 handler HTTP con orchestrazione e bodyType dichiarato.");
            Assert.That(analizzati.All(a => a.Dichiarato is not null && a.Prodotto is not null), Is.True);
        });
    }

    // ------------------------------------------------------------------------------------------

    private sealed record Analisi(string Handler, Type? Dichiarato, Type? Prodotto);

    private static IEnumerable<Analisi> Disallineamenti() =>
        Analizzati().Where(a => !Compatibili(a.Dichiarato!, a.Prodotto!));

    /// <summary>
    /// Compatibilita' come la vede un client JSON, non come la vede il compilatore: basta che il tipo
    /// prodotto sia assegnabile a quello dichiarato. E' il caso di FattureListaDto, che deriva da
    /// List&lt;FatturaDto&gt; senza aggiungere membri e quindi serializza in modo identico.
    /// </summary>
    private static bool Compatibili(Type dichiarato, Type prodotto) =>
        dichiarato.IsAssignableFrom(prodotto) || prodotto.IsAssignableFrom(dichiarato);

    private static IEnumerable<Analisi> Analizzati()
    {
        var assembly = typeof(AuthMiddleware).Assembly;
        var perNomeFunction = assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            .Select(m => (Metodo: m, Function: m.GetCustomAttribute<FunctionAttribute>()))
            .Where(x => x.Function is not null)
            .GroupBy(x => x.Function!.Name, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().Metodo, StringComparer.Ordinal);

        foreach (var (percorso, testo) in SorgentiDellaFunction())
        {
            if (!percorso.Contains("Handlers", StringComparison.OrdinalIgnoreCase))
                continue;

            var orchestrazione = Regex.Match(testo, @"ScheduleNewOrchestrationInstanceAsync\(\s*""([^""]+)""");
            if (!orchestrazione.Success)
                continue; // handler HTTP puro, senza orchestrazione (es. v1/authentication)

            var nomeHandler = Path.GetFileNameWithoutExtension(percorso);
            var nomeFunctionHandler = Regex.Match(testo, @"Function\(""([^""]+)""\)");
            if (!nomeFunctionHandler.Success || !perNomeFunction.TryGetValue(nomeFunctionHandler.Groups[1].Value, out var metodoHandler))
                continue;

            var dichiarato = metodoHandler
                .GetCustomAttributes<OpenApiResponseWithBodyAttribute>()
                .FirstOrDefault(a => a.StatusCode == System.Net.HttpStatusCode.OK)?.BodyType;

            if (dichiarato is null)
                continue; // nessun corpo dichiarato per il 200: niente da confrontare

            if (!perNomeFunction.TryGetValue(orchestrazione.Groups[1].Value, out var metodoOrchestratore))
                continue; // gia' coperto dal guardrail sui nomi

            yield return new Analisi(nomeHandler, dichiarato, SenzaTask(metodoOrchestratore.ReturnType));
        }
    }

    private static Type SenzaTask(Type tipo) =>
        tipo.IsGenericType && tipo.GetGenericTypeDefinition() == typeof(Task<>)
            ? tipo.GetGenericArguments()[0]
            : tipo;

    private static string Leggibile(Type? tipo) => tipo is null
        ? "(nessuno)"
        : tipo.IsGenericType
            ? $"{tipo.Name[..tipo.Name.IndexOf('`')]}<{string.Join(", ", tipo.GetGenericArguments().Select(a => a.Name))}>"
            : tipo.Name;

    private static List<(string Percorso, string Testo)> SorgentiDellaFunction()
    {
        var cartella = Path.Combine(RadiceRepository(), "src", "Presentation", "PortaleFatture.BE.Function.API");

        Assert.That(Directory.Exists(cartella), Is.True, $"Cartella non trovata: {cartella}");

        return Directory
            .EnumerateFiles(cartella, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(f => (f, File.ReadAllText(f)))
            .ToList();
    }

    private static string RadiceRepository()
    {
        var cartella = new DirectoryInfo(AppContext.BaseDirectory);
        while (cartella is not null && !File.Exists(Path.Combine(cartella.FullName, "PortaleFatture.BE.Api.sln")))
            cartella = cartella.Parent;

        Assert.That(cartella, Is.Not.Null, "Radice del repository non trovata risalendo da " + AppContext.BaseDirectory);
        return cartella!.FullName;
    }
}
