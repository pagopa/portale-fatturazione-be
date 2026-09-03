using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PortaleFatture.BE.Infrastructure.Common.Language.Service;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Costruisce un <see cref="LanguageService"/> che colpisce **Azure AI Language reale**, per le due
/// famiglie di test che lo richiedono: i funzionali (`LanguageServiceRealeIntegrationTests`) e gli
/// adversarial (`LanguageServiceAdversarialIntegrationTests`).
///
/// Sta qui e non nelle singole fixture perche' la guardia e' la parte che non va sbagliata due volte.
///
/// ⚠️ **`[Explicit]` NON basta a impedire l'esecuzione** — misurato il 03/09/2026 leggendo il `.trx`:
/// se un filtro li seleziona, anche indirettamente, l'adapter NUnit li considera "scelti
/// esplicitamente" e li esegue comunque, `OneTimeSetUp` compreso. E il filtro indiretto qui e' molto
/// facile: il namespace e' `PortaleFatture.BE.IntegrationTest`, quindi un
/// `--filter "FullyQualifiedName~Fatture"` seleziona **l'intera suite** (trappola gia' documentata in
/// `docs/test-integrazione-db-seedato.md`).
///
/// Da qui la regola: **due condizioni indipendenti**, come per i guardrail anti-invio email —
/// l'opt-in <see cref="VariabileOptIn"/> *e* i secrets. Chi non ha chiesto di spendere, non spende.
///
/// ⚠️ **Serve la VPN, e serve l'endpoint giusto** (imparato al primo giro reale, 03/09/2026). La
/// risorsa Azure AI Language sta **dietro VNet** come il resto dell'infrastruttura
/// (v. `docs/architettura.md`), quindi da una macchina non collegata tutte le chiamate falliscono con:
///
/// <code>403 — A Virtual Network is configured for this resource. Please use the correct endpoint…</code>
///
/// Quel messaggio ha **due** cause, da controllare in quest'ordine:
/// 1. l'endpoint configurato e' quello **regionale** (`https://&lt;region&gt;.api.cognitive.microsoft.com/`):
///    una risorsa con restrizione di rete lo rifiuta, e accetta solo il *custom subdomain*
///    (`https://&lt;nome-risorsa&gt;.cognitiveservices.azure.com/`);
/// 2. VPN spenta, o quella di un altro ambiente.
///
/// Non e' un difetto del codice ne' dei test: e' lo stesso fatto per cui senza VPN non risponde nemmeno
/// il database. Se capita, i test diventano **rossi** (non ignorati) di proposito — un 403 e' una
/// risposta del servizio, e distinguerlo da "non configurato" e' esattamente il punto della scala di
/// codici 503/502/504.
///
/// **Sonda da 1 secondo per distinguere rete da credenziale**, senza lanciare la suite e senza
/// consumare quota (la chiave e' finta apposta: se la rete passa, Azure rifiuta la chiave *dopo*
/// averla ricevuta):
///
/// <code>
/// curl -s -o /dev/null -w "%{http_code}\n" -X POST \
///   "https://&lt;risorsa&gt;.cognitiveservices.azure.com/language/:analyze-text?api-version=2023-04-01" \
///   -H "Ocp-Apim-Subscription-Key: chiave-finta" -H "Content-Type: application/json" -d '{}'
/// </code>
///
/// **401** = la rete passa, il problema e' altrove. **403** = sei bloccato fuori.
///
/// ⚠️ **Il 403 puo' essere transitorio** (visto il 03/09/2026: bloccato alle 10:27, passante pochi
/// minuti dopo, a parita' di IP e di endpoint). Una regola di firewall appena aggiunta impiega qualche
/// minuto a diventare effettiva su tutti i front-end del gateway condiviso: prima di indagare, provare
/// la sonda una seconda volta.
/// </summary>
internal static class AzureLanguageTestbed
{
    /// <summary>
    /// Interruttore generale. Vale come variabile d'ambiente oppure come chiave di configurazione
    /// (`PortaleFattureOptions:Language:AbilitaTestReali`), cosi' si puo' accendere per una singola
    /// esecuzione senza modificare i secrets:
    /// <code>PF_RUN_AZURE_LANGUAGE_TESTS=1 dotnet test --filter "FullyQualifiedName~LanguageServiceReale"</code>
    /// </summary>
    public const string VariabileOptIn = "PF_RUN_AZURE_LANGUAGE_TESTS";

    public sealed record Configurato(
        LanguageService Servizio,
        string Endpoint,
        string Key,
        int MaxChars,
        int MaxCharsSummarize,
        int TimeoutSeconds);

    /// <summary>
    /// Restituisce il servizio reale, oppure chiama <c>Assert.Ignore</c> spiegando cosa manca — mai un
    /// rosso: un ambiente senza credenziali non e' un difetto del codice.
    /// </summary>
    /// <summary>
    /// Gli user secrets sono **due store distinti** e la sezione `Language` puo' stare in uno solo dei
    /// due: l'app avviata da <c>ApiTestFactory</c> legge quelli del progetto **API**, mentre
    /// <c>ServiceProvider</c> legge quelli del progetto **IntegrationTest**. Si leggono entrambi (piu'
    /// le variabili d'ambiente) cosi' le credenziali vanno configurate una volta sola, dove si preferisce.
    /// </summary>
    private const string SecretsIntegrationTest = "27e0801e-8863-4e92-af93-631a5685fed4";
    private const string SecretsApi = "072fb2f2-e073-421a-a57d-e0c25e7517fd";

    public static Configurato CostruisciOSalta()
    {
        var conf = new ConfigurationBuilder()
            .AddUserSecrets(SecretsIntegrationTest)
            .AddUserSecrets(SecretsApi)
            .AddEnvironmentVariables()
            .Build();
        var sezione = conf.GetSection("PortaleFattureOptions:Language");

        var optInAmbiente = Environment.GetEnvironmentVariable(VariabileOptIn);
        var optInConfig = sezione["AbilitaTestReali"];
        var abilitato = Attivo(optInAmbiente) || Attivo(optInConfig);

        if (!abilitato)
            Assert.Ignore(
                $"Test contro Azure AI Language reale disattivati. Sono a pagamento e non partono per "
                + $"errore: per eseguirli valorizzare {VariabileOptIn}=1 (variabile d'ambiente) oppure "
                + "PortaleFattureOptions:Language:AbilitaTestReali=true.");

        var endpoint = sezione["Endpoint"];
        var key = sezione["Key"];

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key))
            Assert.Ignore(
                "Sezione PortaleFattureOptions:Language non configurata negli user secrets del progetto "
                + "IntegrationTest (id 27e0801e-8863-4e92-af93-631a5685fed4): servono Endpoint e Key.");

        // I limiti si LEGGONO dalla configurazione invece di ricopiarli nelle fixture: se un domani
        // vengono tarati diversamente, i test verificano i valori nuovi e non una copia dimenticata.
        var maxChars = Intero(sezione["MaxChars"], 5_120);
        var maxCharsSummarize = Intero(sezione["MaxCharsSummarize"], 125_000);
        var timeoutSeconds = Intero(sezione["TimeoutSeconds"], 45);

        // Diagnostica: si stampa SOLO l'host (mai la chiave). Serve a rispondere in fretta alla domanda
        // che ci si pone davanti a un 403 — "sto bussando alla risorsa che credo?" — senza dover
        // ispezionare gli user secrets. Utile anche perche' le credenziali possono arrivare da due
        // store diversi (v. sopra) e non e' ovvio quale abbia vinto.
        TestContext.Out.WriteLine(
            $"Azure AI Language: endpoint {new Uri(endpoint!).Host} | limiti {maxChars}/{maxCharsSummarize} "
            + $"| timeout {timeoutSeconds}s");

        var servizio = new LanguageService(
            endpoint, key, NullLogger<LanguageService>.Instance,
            timeoutSeconds, maxChars, maxCharsSummarize);

        return new Configurato(servizio, endpoint!, key!, maxChars, maxCharsSummarize, timeoutSeconds);
    }

    private static bool Attivo(string? valore) =>
        !string.IsNullOrWhiteSpace(valore)
        && (valore.Equals("1", StringComparison.OrdinalIgnoreCase)
            || valore.Equals("true", StringComparison.OrdinalIgnoreCase));

    private static int Intero(string? valore, int predefinito) =>
        int.TryParse(valore, out var n) && n > 0 ? n : predefinito;
}
