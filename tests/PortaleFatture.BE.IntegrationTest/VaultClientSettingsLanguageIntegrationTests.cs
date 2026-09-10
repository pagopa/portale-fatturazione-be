using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Infrastructure.Common.Language.Service;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Integration test della **catena di configurazione** di Azure AI Language, dall'ambiente fino al
/// servizio risolto da DI: variabili d'ambiente -> <c>VaultClientSettings</c> -> <c>PortaleFattureOptions</c>
/// -> <c>AddGateways</c> -> <c>ILanguageService.IsConfigured</c>. Non tocca DB ne' rete, quindi **non
/// richiede la VPN UAT** ne' il container seedato: e' "integration" nel senso che esercita piu'
/// componenti reali insieme, non perche' parli con un database.
///
/// Perche' non basta il gemello unit (<c>ModuleExtensionsLanguageEnvironmentTests</c>): quello prova il
/// metodo <c>ApplyEnvironmentOverrides</c> in isolamento, e resterebbe **verde** se qualcuno cancellasse
/// la riga che lo invoca dentro <c>VaultClientSettings</c> o il <c>model.Language ??= new()</c> in cima
/// al metodo. Sono due righe sole, in un metodo lungo e ripetitivo che invita alla pulizia, e la loro
/// perdita e' silenziosa: l'applicazione parte lo stesso, le tre rotte rispondono 503 e nessuno se ne
/// accorge finche' non arriva una segnalazione. Questi test sono il presidio di quelle due righe.
///
/// ATTENZIONE <c>VaultClientSettings</c> gira **solo in ambiente Production** (v.
/// <c>ConfigurationExtensions.AddModules</c>), che sui container Azure e' il default perche' il
/// Dockerfile non imposta <c>ASPNETCORE_ENVIRONMENT</c>. In debug locale (Development) il metodo non
/// viene invocato affatto e la configurazione arriva solo da appsettings/user secrets.
/// </summary>
[TestFixture]
[NonParallelizable] // manipola variabili d'ambiente, che sono stato globale del processo
public class VaultClientSettingsLanguageIntegrationTests
{
    /// <summary>
    /// Le variabili che <c>VaultClientSettings</c> pretende con <c>?? throw</c>: senza anche una sola di
    /// queste il metodo solleva prima di arrivare alla sezione Language, e il test proverebbe altro.
    /// L'elenco e' volutamente esplicito e non dedotto: se un domani ne viene aggiunta una, questo test
    /// diventa rosso con il messaggio "Please specify a ..." che dice esattamente quale.
    /// </summary>
    private static readonly string[] VariabiliObbligatorie =
    [
        "SELF_CARE_URI", "SELFCARE_CERT_ENDPOINT", "JWT_VALID_AUDIENCE", "JWT_VALID_ISSUER",
        "CONNECTION_STRING", "JWT_SECRET", "CORS_ORIGINS", "SELF_CARE_TIMEOUT", "SELF_CARE_AUDIENCE",
        "ADMIN_KEY", "STORAGE_REL_FOLDER", "STORAGE_DOCUMENTI_FOLDER", "STORAGE_CONNECTIONSTRING",
        "STORAGE_DOCUMENTI_CONNECTIONSTRING", "APPLICATION_INSIGHTS", "AZUREAD_INSTANCE",
        "AZUREAD_TENANTID", "AZUREAD_CLIENTID", "AZUREAD_ADGROUP", "SYNAPSE_WORKSPACE_NAME",
        "PIPELINE_NAME_SAP", "SYNAPSE_SUBSCRIPTIONID", "SYNAPSE_RESOURCEGROUPNAME",
        "StorageNotificheAccountName", "StorageNotificheAccountKey", "StorageNotificheBlobContainerName",
        "StorageNotificheCustomDNS", "SELFCAREONBOARDING_ENDPOINT", "SELFCAREONBOARDING_URI",
        "SELFCAREONBOARDING_AUTHTOKEN", "SUPPORTAPISERVICE_ENDPOINT", "SUPPORTAPISERVICE_URI",
        "SUPPORTAPISERVICE_AUTHTOKEN", "STORAGE_FINANCIAL_ACCOUNTNAME", "STORAGE_FINANCIAL_ACCOUNTKEY",
        "STORAGE_FINANCIAL_CONTAINERNAME", "StorageRELAccountName", "StorageRELAccountKey",
        "StorageRELBlobContainerName", "StorageRELCustomDns", "AzureFunctionNotificheUri",
        "AzureFunctionAppKey"
    ];

    private static readonly string[] VariabiliLanguage =
    [
        "LANGUAGE_ENDPOINT", "LANGUAGE_KEY", "LANGUAGE_TIMEOUTSECONDS",
        "LANGUAGE_MAXCHARS", "LANGUAGE_MAXCHARSSUMMARIZE"
    ];

    private readonly Dictionary<string, string?> _valoriOriginali = [];

    /// <summary>
    /// I valori sono fittizi ma **sintatticamente plausibili**: nessuno di essi viene usato per aprire
    /// una connessione in questi test, ma <c>AddGateways</c> costruisce davvero alcuni servizi, quindi e'
    /// bene che non siano stringhe assurde.
    /// </summary>
    [SetUp]
    public void ImpostaAmbienteDiProduzioneFittizio()
    {
        foreach (var variabile in VariabiliObbligatorie.Concat(VariabiliLanguage))
        {
            _valoriOriginali[variabile] = Environment.GetEnvironmentVariable(variabile);
            Environment.SetEnvironmentVariable(variabile, null);
        }

        foreach (var variabile in VariabiliObbligatorie)
            Environment.SetEnvironmentVariable(variabile, $"valore-di-test-{variabile}");
    }

    [TearDown]
    public void RipristinaAmbiente()
    {
        foreach (var (variabile, valore) in _valoriOriginali)
            Environment.SetEnvironmentVariable(variabile, valore);
        _valoriOriginali.Clear();
    }

    /// <summary>
    /// Il presidio del <c>model.Language ??= new()</c>: senza nessuna variabile della sezione, il metodo
    /// deve completare e lasciare una sezione **istanziata e ai default**. Se tornasse null, o
    /// sollevasse come fanno tutte le altre letture del metodo, un ambiente che non usa Azure AI Language
    /// non partirebbe piu'.
    /// </summary>
    [Test]
    public async Task VaultClientSettings_SenzaVariabiliLanguage_NonSollevaELasciaLaSezioneAiDefault()
    {
        var options = new PortaleFattureOptions();

        Assert.DoesNotThrowAsync(async () => await options.VaultClientSettings());
        await Task.CompletedTask;

        Assert.Multiple(() =>
        {
            Assert.That(options.Language, Is.Not.Null, "la sezione deve essere istanziata anche quando non e' configurata");
            Assert.That(options.Language!.Endpoint, Is.Null);
            Assert.That(options.Language.Key, Is.Null);
            Assert.That(options.Language.TimeoutSeconds, Is.EqualTo(45));
            Assert.That(options.Language.MaxChars, Is.EqualTo(5_120));
            Assert.That(options.Language.MaxCharsSummarize, Is.EqualTo(125_000));
        });
    }

    /// <summary>
    /// Il presidio della riga <c>model.Language.ApplyEnvironmentOverrides()</c>: e' l'unico test che
    /// diventa rosso se quella chiamata sparisce.
    /// </summary>
    [Test]
    public async Task VaultClientSettings_ConLeVariabiliLanguage_LeRiportaNellaSezione()
    {
        Environment.SetEnvironmentVariable("LANGUAGE_ENDPOINT", "https://fat-d-app-ls.cognitiveservices.azure.com/");
        Environment.SetEnvironmentVariable("LANGUAGE_KEY", "chiave-di-test");
        Environment.SetEnvironmentVariable("LANGUAGE_TIMEOUTSECONDS", "30");
        Environment.SetEnvironmentVariable("LANGUAGE_MAXCHARS", "4000");
        Environment.SetEnvironmentVariable("LANGUAGE_MAXCHARSSUMMARIZE", "100000");

        var options = await new PortaleFattureOptions().VaultClientSettings();

        Assert.Multiple(() =>
        {
            Assert.That(options.Language!.Endpoint, Is.EqualTo("https://fat-d-app-ls.cognitiveservices.azure.com/"));
            Assert.That(options.Language.Key, Is.EqualTo("chiave-di-test"));
            Assert.That(options.Language.TimeoutSeconds, Is.EqualTo(30));
            Assert.That(options.Language.MaxChars, Is.EqualTo(4_000));
            Assert.That(options.Language.MaxCharsSummarize, Is.EqualTo(100_000));
        });
    }

    /// <summary>
    /// Le variabili d'ambiente vincono su quanto gia' presente nel model, che in produzione arriva dal
    /// bind di appsettings. E' l'ordine che rende l'ambiente configurabile senza ricompilare.
    /// </summary>
    [Test]
    public async Task VaultClientSettings_ConSezioneGiaValorizzata_LAmbienteHaLaPrecedenza()
    {
        Environment.SetEnvironmentVariable("LANGUAGE_ENDPOINT", "https://da-ambiente/");

        var options = new PortaleFattureOptions
        {
            Language = new Language { Endpoint = "https://da-appsettings/", Key = "chiave-da-appsettings" }
        };
        await options.VaultClientSettings();

        Assert.Multiple(() =>
        {
            Assert.That(options.Language!.Endpoint, Is.EqualTo("https://da-ambiente/"));
            Assert.That(options.Language.Key, Is.EqualTo("chiave-da-appsettings"),
                "una variabile assente non deve cancellare quanto gia' configurato");
        });
    }

    /// <summary>
    /// Lo scenario di incollatura, lungo tutta la catena: e' cosi' che i valori arrivano davvero nelle
    /// app setting del portale Azure. Con la normalizzazione il servizio risulta configurato; senza,
    /// la chiave partirebbe con l'a-capo attaccato e Azure la rifiuterebbe con un 502 opaco.
    /// </summary>
    [Test]
    public async Task VaultClientSettings_ValoriIncollatiConSpaziatura_VengonoNormalizzati()
    {
        Environment.SetEnvironmentVariable("LANGUAGE_ENDPOINT", " https://fat-d-app-ls.cognitiveservices.azure.com/\r\n");
        Environment.SetEnvironmentVariable("LANGUAGE_KEY", "chiave-di-test\n");

        var options = await new PortaleFattureOptions().VaultClientSettings();

        Assert.Multiple(() =>
        {
            Assert.That(options.Language!.Endpoint, Is.EqualTo("https://fat-d-app-ls.cognitiveservices.azure.com/"));
            Assert.That(options.Language.Key, Is.EqualTo("chiave-di-test"));
        });
        Assert.That(RisolviLanguageService(options).IsConfigured, Is.True);
    }

    /// <summary>
    /// Controprova che la modifica non abbia disturbato il resto del metodo: le altre sezioni devono
    /// continuare ad arrivare dall'ambiente come prima.
    /// </summary>
    [Test]
    public async Task VaultClientSettings_LeAltreSezioni_RestanoValorizzate()
    {
        var options = await new PortaleFattureOptions().VaultClientSettings();

        Assert.Multiple(() =>
        {
            Assert.That(options.ConnectionString, Is.EqualTo("valore-di-test-CONNECTION_STRING"));
            Assert.That(options.JWT!.Secret, Is.EqualTo("valore-di-test-JWT_SECRET"));
            Assert.That(options.AzureAd!.AdGroup, Is.EqualTo("valore-di-test-AZUREAD_ADGROUP"));
            Assert.That(options.Synapse!.PipelineNameSAP, Is.EqualTo("valore-di-test-PIPELINE_NAME_SAP"));
        });
    }

    /// <summary>
    /// L'ultimo anello: che quelle stesse opzioni, passate alla registrazione DI reale
    /// (<c>AddGateways</c>), producano un servizio che si dichiara configurato. E' cio' che separa il
    /// funzionamento delle tre rotte dal 503.
    ///
    /// La registrazione e' **lazy** di proposito: l'istanza nasce qui, alla risoluzione, non all'avvio —
    /// v. il commento in <c>ConfigurationExtensions.AddGateways</c> e il difetto che quella scelta ha
    /// chiuso (206 test di integrazione rossi su 568).
    /// </summary>
    [Test]
    public async Task CatenaCompleta_ConLeVariabili_IlServizioRisoltoDaDiEConfigurato()
    {
        Environment.SetEnvironmentVariable("LANGUAGE_ENDPOINT", "https://fat-d-app-ls.cognitiveservices.azure.com/");
        Environment.SetEnvironmentVariable("LANGUAGE_KEY", "chiave-di-test");

        var servizio = RisolviLanguageService(await new PortaleFattureOptions().VaultClientSettings());

        Assert.That(servizio.IsConfigured, Is.True);
    }

    /// <summary>
    /// Lo speculare: senza variabili la risoluzione **non solleva** e restituisce un servizio non
    /// configurato, che e' esattamente cio' che gli endpoint traducono in 503.
    /// </summary>
    [Test]
    public async Task CatenaCompleta_SenzaVariabili_IlServizioSiRisolveMaNonEConfigurato()
    {
        var options = await new PortaleFattureOptions().VaultClientSettings();

        ILanguageService? servizio = null;
        Assert.DoesNotThrow(() => servizio = RisolviLanguageService(options));
        Assert.That(servizio!.IsConfigured, Is.False);
    }

    private static ILanguageService RisolviLanguageService(PortaleFattureOptions options)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IPortaleFattureOptions>(options);
        services.AddGateways();
        return services.BuildServiceProvider().GetRequiredService<ILanguageService>();
    }
}
