using NUnit.Framework;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Core.Common;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// La sezione <c>Language</c> e' l'unica di <c>PortaleFattureOptions</c> letta con la convenzione flat
/// **senza** sollevare quando la variabile manca. La differenza non e' stilistica: tutte le altre righe
/// di <c>VaultClientSettings</c> fanno <c>?? throw</c>, e copiare quel pattern qui riporterebbe il
/// difetto gia' corretto in PF-775 — un servizio esterno opzionale che impedisce l'avvio dell'intera
/// applicazione (misurato allora: 206 test di integrazione rossi su 568).
///
/// Questi test sono il presidio di quella scelta: se qualcuno "uniformasse" il metodo aggiungendo i
/// throw, o rendesse obbligatoria una delle variabili, diventerebbero rossi.
///
/// <c>VaultClientSettings</c> nel suo complesso resta non testabile (gira solo in produzione e pretende
/// una trentina di variabili d'ambiente): l'override di Language e' stato quindi estratto in un metodo
/// a se', che e' esattamente la parte che si vuole poter verificare.
///
/// Non c'e' una <c>LANGUAGE_KEY</c>: dall'11/09/2026 il servizio si autentica solo con l'identita'
/// Entra ID, quindi l'endpoint da solo e' una configurazione completa.
/// </summary>
[TestFixture]
[NonParallelizable] // manipola variabili d'ambiente, che sono stato globale del processo
public class ModuleExtensionsLanguageEnvironmentTests
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

    [Test]
    public void ApplyEnvironmentOverrides_SenzaAlcunaVariabile_NonSollevaELasciaIDefault()
    {
        var language = new Language();

        Assert.DoesNotThrow(() => language.ApplyEnvironmentOverrides());

        Assert.Multiple(() =>
        {
            Assert.That(language.Endpoint, Is.Null);
            Assert.That(language.TimeoutSeconds, Is.EqualTo(45));
            Assert.That(language.MaxChars, Is.EqualTo(5_120));
            Assert.That(language.MaxCharsSummarize, Is.EqualTo(125_000));
        });
    }

    [Test]
    public void ApplyEnvironmentOverrides_ConEndpoint_LoApplica()
    {
        Environment.SetEnvironmentVariable("LANGUAGE_ENDPOINT", "https://esempio.cognitiveservices.azure.com/");

        var language = new Language().ApplyEnvironmentOverrides();

        Assert.That(language.Endpoint, Is.EqualTo("https://esempio.cognitiveservices.azure.com/"));
    }

    [Test]
    public void ApplyEnvironmentOverrides_ConTuttiINumerici_LiApplica()
    {
        Environment.SetEnvironmentVariable("LANGUAGE_TIMEOUTSECONDS", "30");
        Environment.SetEnvironmentVariable("LANGUAGE_MAXCHARS", "4000");
        Environment.SetEnvironmentVariable("LANGUAGE_MAXCHARSSUMMARIZE", "100000");

        var language = new Language().ApplyEnvironmentOverrides();

        Assert.Multiple(() =>
        {
            Assert.That(language.TimeoutSeconds, Is.EqualTo(30));
            Assert.That(language.MaxChars, Is.EqualTo(4_000));
            Assert.That(language.MaxCharsSummarize, Is.EqualTo(100_000));
        });
    }

    /// <summary>
    /// Su App Service una app setting valorizzata a stringa vuota arriva al container come variabile
    /// **presente e vuota**: se la si applicasse, cancellerebbe una configurazione valida arrivata da
    /// appsettings o dagli user secrets.
    /// </summary>
    [TestCase("")]
    [TestCase("   ")]
    public void ApplyEnvironmentOverrides_StringaVuota_NonSovrascriveIlValoreEsistente(string valore)
    {
        Environment.SetEnvironmentVariable("LANGUAGE_ENDPOINT", valore);

        var language = new Language { Endpoint = "https://gia-configurato/" }.ApplyEnvironmentOverrides();

        Assert.That(language.Endpoint, Is.EqualTo("https://gia-configurato/"));
    }

    /// <summary>
    /// Il caso degli App Service configurati con i nomi nativi (<c>PortaleFattureOptions__Language__*</c>):
    /// il valore arriva dal bind, la variabile flat **non esiste affatto**, e il metodo non deve toccarlo.
    /// E' la situazione reale degli ambienti al 10/09/2026.
    /// </summary>
    [Test]
    public void ApplyEnvironmentOverrides_VariabileAssente_TieneIlValoreDelBind()
    {
        var language = new Language { Endpoint = "https://da-bind-nativo/", MaxChars = 4_000 }
            .ApplyEnvironmentOverrides();

        Assert.Multiple(() =>
        {
            Assert.That(language.Endpoint, Is.EqualTo("https://da-bind-nativo/"));
            Assert.That(language.MaxChars, Is.EqualTo(4_000));
        });
    }

    [TestCase("0")]
    [TestCase("-1")]
    [TestCase("quarantacinque")]
    [TestCase("")]
    public void ApplyEnvironmentOverrides_NumericoNonValido_TieneIlValoreEsistente(string valore)
    {
        Environment.SetEnvironmentVariable("LANGUAGE_TIMEOUTSECONDS", valore);

        var language = new Language { TimeoutSeconds = 20 }.ApplyEnvironmentOverrides();

        Assert.That(language.TimeoutSeconds, Is.EqualTo(20));
    }

    /// <summary>
    /// La precedenza e' l'unica cosa che rende utile questo metodo in produzione: sui container Azure
    /// la sezione arriva **vuota** da appsettings, ma se un domani qualcuno ci scrivesse un valore di
    /// comodo, la variabile d'ambiente deve comunque vincere — altrimenti l'ambiente non sarebbe piu'
    /// configurabile senza ricompilare.
    /// </summary>
    [Test]
    public void ApplyEnvironmentOverrides_VariabilePresente_VinceSulValoreGiaConfigurato()
    {
        Environment.SetEnvironmentVariable("LANGUAGE_ENDPOINT", "https://da-ambiente/");
        Environment.SetEnvironmentVariable("LANGUAGE_TIMEOUTSECONDS", "30");

        var language = new Language
        {
            Endpoint = "https://da-appsettings/",
            TimeoutSeconds = 45
        }.ApplyEnvironmentOverrides();

        Assert.Multiple(() =>
        {
            Assert.That(language.Endpoint, Is.EqualTo("https://da-ambiente/"));
            Assert.That(language.TimeoutSeconds, Is.EqualTo(30));
        });
    }

    /// <summary>
    /// Il metodo non ha stato: applicarlo due volte deve dare lo stesso risultato. Serve perche' e'
    /// invocato dentro <c>VaultClientSettings</c>, che in un test (o in un futuro reload della
    /// configurazione) puo' essere chiamato piu' di una volta sullo stesso model.
    /// </summary>
    [Test]
    public void ApplyEnvironmentOverrides_ApplicatoDueVolte_Idempotente()
    {
        Environment.SetEnvironmentVariable("LANGUAGE_ENDPOINT", "https://da-ambiente/");
        Environment.SetEnvironmentVariable("LANGUAGE_MAXCHARS", "4000");

        var language = new Language().ApplyEnvironmentOverrides().ApplyEnvironmentOverrides();

        Assert.Multiple(() =>
        {
            Assert.That(language.Endpoint, Is.EqualTo("https://da-ambiente/"));
            Assert.That(language.MaxChars, Is.EqualTo(4_000));
        });
    }
}
