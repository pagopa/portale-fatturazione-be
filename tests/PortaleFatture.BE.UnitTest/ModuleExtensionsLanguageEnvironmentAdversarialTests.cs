using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Infrastructure.Common.Language.Service;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Input ostili sulle variabili d'ambiente della sezione <c>Language</c>. Non e' un esercizio teorico:
/// il valore lo scrive una persona in un campo di testo del portale Azure, dove finisce spesso per
/// incollatura — con un a-capo attaccato, con la virgola dei decimali, o semplicemente sbagliato.
/// L'unico posto in cui quell'errore puo' essere fermato in modo comprensibile e' qui: piu' avanti
/// diventa un 502 dal servizio esterno o un'eccezione a runtime, entrambi diagnosi molto piu' lente.
///
/// I test si dividono in due gruppi: quelli che verificano un **rifiuto voluto** (il valore ostile non
/// entra in configurazione) e quelli che **caratterizzano** cio' che invece passa — questi ultimi non
/// approvano il comportamento, lo rendono visibile. Se un domani si decide di irrigidire la
/// validazione, sono loro a diventare rossi e a dire esattamente cosa cambia.
/// </summary>
[TestFixture]
[NonParallelizable] // manipola variabili d'ambiente, che sono stato globale del processo
public class ModuleExtensionsLanguageEnvironmentAdversarialTests
{
    private static readonly string[] Variabili =
    [
        "LANGUAGE_ENDPOINT",
        "LANGUAGE_TIMEOUTSECONDS",
        "LANGUAGE_MAXCHARS",
        "LANGUAGE_MAXCHARSSUMMARIZE"
    ];

    [SetUp]
    [TearDown]
    public void PulisciVariabili()
    {
        foreach (var variabile in Variabili)
            Environment.SetEnvironmentVariable(variabile, null);
    }

    // ---------------------------------------------------------------- rifiuti voluti

    /// <summary>
    /// Tutte forme che una persona scrive credendole valide. Nessuna deve entrare in configurazione: il
    /// valore precedente resta, e le rotte continuano a funzionare col default invece di ereditare un
    /// timeout casuale.
    /// </summary>
    [TestCase("45,5", TestName = "decimale con virgola")]
    [TestCase("45.5", TestName = "decimale con punto")]
    [TestCase("1e3", TestName = "notazione esponenziale")]
    [TestCase("0x2D", TestName = "esadecimale")]
    [TestCase("1.000", TestName = "separatore delle migliaia")]
    [TestCase("45 secondi", TestName = "numero con unita di misura")]
    [TestCase("99999999999999999999", TestName = "oltre il range di int")]
    [TestCase("-2147483649", TestName = "sotto il range di int")]
    [TestCase("NaN", TestName = "NaN")]
    [TestCase("\t\n", TestName = "solo spaziatura")]
    public void NumericoOstile_NonEntraInConfigurazione(string valore)
    {
        Environment.SetEnvironmentVariable("LANGUAGE_TIMEOUTSECONDS", valore);
        Environment.SetEnvironmentVariable("LANGUAGE_MAXCHARS", valore);

        var language = new Language { TimeoutSeconds = 45, MaxChars = 5_120 }.ApplyEnvironmentOverrides();

        Assert.Multiple(() =>
        {
            Assert.That(language.TimeoutSeconds, Is.EqualTo(45));
            Assert.That(language.MaxChars, Is.EqualTo(5_120));
        });
    }

    // ---------------------------------------------------------------- caratterizzazioni

    /// <summary>
    /// Forme che <c>int.TryParse</c> accetta di suo. Innocue, ma vale la pena saperle: una app setting
    /// scritta " 30 " o "+30" viene applicata davvero, non ignorata.
    /// </summary>
    [TestCase(" 30 ", 30, TestName = "spazi attorno al numero")]
    [TestCase("+30", 30, TestName = "segno esplicito")]
    [TestCase("0030", 30, TestName = "zeri iniziali")]
    public void NumericoTollerato_VieneApplicato(string valore, int atteso)
    {
        Environment.SetEnvironmentVariable("LANGUAGE_TIMEOUTSECONDS", valore);

        Assert.That(new Language().ApplyEnvironmentOverrides().TimeoutSeconds, Is.EqualTo(atteso));
    }

    /// <summary>
    /// ATTENZIONE Non esiste un tetto: un timeout assurdo viene accettato. Il vincolo reale e' che deve
    /// stare **sotto** il taglio del gateway (~60s), altrimenti la connessione muore fuori
    /// dall'applicazione e il 504 gestito non arriva mai al client. Il valore va quindi scelto con quella
    /// soglia in mente, perche' nessun controllo la impone.
    /// </summary>
    [Test]
    public void TimeoutFuoriScala_VieneAccettato_NessunTettoImposto()
    {
        Environment.SetEnvironmentVariable("LANGUAGE_TIMEOUTSECONDS", int.MaxValue.ToString());

        Assert.That(new Language().ApplyEnvironmentOverrides().TimeoutSeconds, Is.EqualTo(int.MaxValue));
    }

    /// <summary>
    /// Il caso piu' probabile di tutta questa classe, ed e' il motivo per cui esiste il Trim: il valore
    /// di una app setting viene incollato nel portale Azure, spesso con uno spazio o un a-capo ai bordi
    /// che nessuno vede.
    /// </summary>
    [TestCase(" https://esempio.cognitiveservices.azure.com/\n", TestName = "spazio davanti e a-capo in coda")]
    [TestCase("\thttps://esempio.cognitiveservices.azure.com/\r\n", TestName = "tabulazione e fine riga Windows")]
    [TestCase("https://esempio.cognitiveservices.azure.com/   ", TestName = "spazi in coda")]
    public void EndpointConSpaziAttorno_VieneNormalizzatoConTrim(string valore)
    {
        Environment.SetEnvironmentVariable("LANGUAGE_ENDPOINT", valore);

        Assert.That(new Language().ApplyEnvironmentOverrides().Endpoint,
            Is.EqualTo("https://esempio.cognitiveservices.azure.com/"));
    }

    /// <summary>
    /// Il Trim tocca **solo i bordi**: un valore con spaziatura interna resta com'e'. Non e' un caso di
    /// scuola — se un domani la sezione ospitasse un valore composto (piu' campi, un JSON), normalizzarne
    /// l'interno lo corromperebbe.
    /// </summary>
    [Test]
    public void SpaziaturaInterna_RestaIntatta()
    {
        Environment.SetEnvironmentVariable("LANGUAGE_ENDPOINT", "  parte-uno parte-due  ");

        Assert.That(new Language().ApplyEnvironmentOverrides().Endpoint, Is.EqualTo("parte-uno parte-due"));
    }

    [Test]
    public void EndpointMoltoLungo_VieneAccettato()
    {
        var lungo = "https://" + new string('a', 10_000) + ".cognitiveservices.azure.com/";
        Environment.SetEnvironmentVariable("LANGUAGE_ENDPOINT", lungo);

        Assert.That(new Language().ApplyEnvironmentOverrides().Endpoint, Is.EqualTo(lungo));
    }

    // ------------------------------------------- cosa succede a valle, con quella configurazione

    /// <summary>
    /// Il caso che conta davvero, perche' e' l'unico in cui una configurazione sbagliata **solleva**:
    /// un endpoint che non e' un URI assoluto fa esplodere <c>new Uri(...)</c> dentro il costruttore.
    ///
    /// Non impedisce l'avvio dell'applicazione — la registrazione DI e' lazy, quindi l'istanza nasce
    /// alla prima chiamata delle tre rotte — ma il risultato per chi chiama e' un **500**, non il 503
    /// pulito della configurazione assente: "endpoint scritto male" e "servizio non disponibile"
    /// arrivano al client come due cose diverse, ed e' bene sapere quale delle due si sta guardando.
    /// </summary>
    [TestCase("fat-d-app-ls.cognitiveservices.azure.com", TestName = "senza schema")]
    [TestCase("un endpoint qualsiasi", TestName = "testo libero")]
    public void EndpointNonUri_SollevaAllaCostruzioneDelServizio_NonAllAvvio(string endpoint)
    {
        var language = new Language { Endpoint = endpoint };

        Assert.Throws<UriFormatException>(() => _ = CostruisciServizio(language));
    }

    /// <summary>
    /// ATTENZIONE Nessun controllo sullo schema in configurazione: un <c>http://</c> viene accettato e il
    /// servizio si dichiara configurato. Il token non esce comunque — e' l'SDK a rifiutarsi di spedirlo
    /// in chiaro — ma ogni chiamata diventa un 502 che non dice "hai scritto http" (v.
    /// <c>LanguageServiceAutenticazioneTests</c>). Caratterizzato perche' e' un errore di configurazione
    /// plausibile e muto fino alla prima chiamata.
    /// </summary>
    [Test]
    public void EndpointInChiaro_VieneAccettato_NessunControlloSulloSchema()
    {
        var language = new Language { Endpoint = "http://esempio.cognitiveservices.azure.com/" };

        Assert.That(CostruisciServizio(language).IsConfigured, Is.True);
    }

    /// <summary>
    /// Senza endpoint (assente, vuoto o di soli spazi): niente eccezioni, servizio non configurato, 503
    /// sulle sue rotte e il resto dell'applicazione intatto. E' l'invariante per cui tutto questo
    /// meccanismo esiste.
    /// </summary>
    [TestCase(null, TestName = "endpoint assente")]
    [TestCase("", TestName = "endpoint vuoto")]
    [TestCase("   ", TestName = "endpoint di soli spazi")]
    public void SenzaEndpoint_NonSolleva_ServizioNonConfigurato(string? endpoint)
    {
        var language = new Language { Endpoint = endpoint };

        LanguageService? servizio = null;
        Assert.DoesNotThrow(() => servizio = CostruisciServizio(language));
        Assert.That(servizio!.IsConfigured, Is.False);
    }

    /// <summary>
    /// Speculare: l'endpoint **da solo** basta. Dall'11/09/2026 non c'e' una chiave da affiancargli —
    /// l'autenticazione e' l'identita' Entra ID — quindi "endpoint senza chiave" non e' piu' una
    /// configurazione parziale. Se l'identita' manca o non ha il ruolo, lo si scopre alla prima chiamata
    /// come 502, non qui.
    /// </summary>
    [Test]
    public void SoloEndpoint_ServizioConfigurato()
    {
        var language = new Language { Endpoint = "https://esempio.cognitiveservices.azure.com/" };

        Assert.That(CostruisciServizio(language).IsConfigured, Is.True);
    }

    private static LanguageService CostruisciServizio(Language language) => new(
        language.Endpoint,
        NullLogger<LanguageService>.Instance,
        language.TimeoutSeconds,
        language.MaxChars,
        language.MaxCharsSummarize);
}
