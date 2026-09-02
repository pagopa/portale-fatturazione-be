using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text.Json;
using System.Xml;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Secondo livello di guardrail sulle dipendenze pinnate, complementare a DependencyGuardrailsTests.
///
/// Quello legge `obj/project.assets.json`, cioè l'INTENZIONE di build: che cosa NuGet ha deciso di
/// risolvere. Questi leggono invece il `.deps.json` dell'output, cioè il manifest che l'host .NET usa
/// davvero per caricare gli assembly, e caricano il tipo a runtime. La differenza conta quando i due
/// divergono: un output non riallineato, o un pacchetto risolto ma non copiato.
///
/// PERCHÉ non c'è un test "sull'uso" delle due librerie: NON SONO USATE DA NESSUN FILE C# del
/// progetto (verificato il 06/08/2026 e riverificato da MicrosoftIdentityWeb_ShouldRemainUnused qui
/// sotto). Scrivere test che fingono di esercitarle sarebbe teatro. È anche il motivo per cui il caso
/// è diverso da Microsoft.Bcl.Memory, il cui codice viene eseguito davvero — è il backport di
/// Base64Url che IdentityModel usa per (de)serializzare i JWT — e che infatti è coperto per la via
/// funzionale da JwtBearerConfigurationTests e Http/JwtBearerPipelineHttpTests.
///
/// L'autenticazione realmente in uso (JWT interno + token AAD validato a mano) non passa da
/// Microsoft.Identity.Web: quella copertura vive in JwtBearerConfigurationTests,
/// PagoPATokenServiceTests e Http/PolicyAuthorizationHttpTests.
/// </summary>
public class DependencyRuntimeGuardrailsTests
{
    /// <summary>
    /// La versione che finisce nel manifest di runtime dell'API, cioè quella che verrà deployata.
    ///
    /// Nota su cosa NON si può asserire: la FileVersion dell'assembly non coincide con la versione
    /// del pacchetto NuGet (il file di System.Security.Cryptography.Xml 8.0.4 riporta 8.0.2926.x),
    /// quindi un controllo su Assembly.GetName().Version darebbe un falso rosso. Il .deps.json è
    /// l'unico posto dove la versione del PACCHETTO sopravvive fino all'output.
    /// </summary>
    [TestCase("System.Security.Cryptography.Xml", "8.0.4")]
    [TestCase("Microsoft.Bcl.Memory", "9.0.14")]
    public void DepsJsonDellApi_ShouldDeclare_AtLeastMinimumVersion(string pacchetto, string versioneMinima)
    {
        var deps = TrovaDepsJsonApi();
        if (deps is null)
        {
            Assert.Ignore(
                "Output dell'API non trovato: compila la solution (dotnet build) perché questo test "
                + "possa leggere PortaleFatture.BE.Api.deps.json.");
            return;
        }

        var risolta = LeggiVersioneDaDeps(deps, pacchetto);

        Assert.That(risolta, Is.Not.Null,
            $"'{pacchetto}' non compare in {Path.GetFileName(deps)}: se è uscito dal grafo, il pin "
            + "è diventato superfluo e questo TestCase va rimosso insieme alla PackageReference.");

        Assert.That(risolta >= Version.Parse(versioneMinima), Is.True,
            $"L'API deploya '{pacchetto}' {risolta}, sotto la soglia {versioneMinima} con advisory. "
            + $"File: {deps}");
    }

    /// <summary>
    /// La libreria pinnata deve essere davvero caricabile e funzionante alla versione risolta.
    ///
    /// Siccome nessun codice del progetto la usa, questo è il massimo che si possa verificare in modo
    /// onesto: un giro completo di firma e verifica XML-DSig. Intercetta un pin sbagliato — versione
    /// inesistente per net8.0, assembly non risolvibile a runtime, o un downgrade silenzioso che
    /// rompesse il caricamento — che il solo controllo sul manifest non vedrebbe.
    /// </summary>
    [Test]
    public void SystemSecurityCryptographyXml_ShouldEssereCaricabileEFunzionante()
    {
        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml("<fattura><progressivo>1</progressivo></fattura>");

        using var rsa = RSA.Create(2048);

        var signedXml = new SignedXml(doc) { SigningKey = rsa };
        var riferimento = new Reference(string.Empty);
        riferimento.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        signedXml.AddReference(riferimento);
        signedXml.ComputeSignature();

        doc.DocumentElement!.AppendChild(doc.ImportNode(signedXml.GetXml(), true));

        var verifica = new SignedXml(doc);
        verifica.LoadXml((XmlElement)doc.GetElementsByTagName("Signature")[0]!);

        Assert.Multiple(() =>
        {
            Assert.That(verifica.CheckSignature(rsa), Is.True,
                "La firma prodotta dalla versione pinnata deve essere verificabile.");

            Assert.That(typeof(SignedXml).Assembly.GetName().Name,
                Is.EqualTo("System.Security.Cryptography.Xml"),
                "Il tipo deve arrivare dal pacchetto, non da un altro assembly.");
        });
    }

    /// <summary>
    /// Microsoft.Identity.Web è dichiarata nel csproj dell'API ma NON è usata da nessun file C#.
    /// Non è una curiosità: è la premessa della TD-3 del backlog tech-debt. Quella libreria trascina
    /// da sola l'intero sottoalbero che genera ENTRAMBI gli advisory pinnati
    /// (System.Security.Cryptography.Xml via Identity.Web.TokenCache e AspNetCore.DataProtection;
    /// Microsoft.Bcl.Memory via IdentityModel 8.9.0), quindi rimuoverla renderebbe superflui due pin.
    ///
    /// Se qualcuno inizia a usarla, quel piano decade — e oggi nulla lo segnalerebbe. Questo test è
    /// il segnale: quando diventa rosso, la decisione va ripresa, non il test aggirato.
    /// </summary>
    [Test]
    public void MicrosoftIdentityWeb_ShouldRemainUnused()
    {
        var src = Path.Combine(FindRepositoryRoot(AppContext.BaseDirectory), "src");
        Assert.That(Directory.Exists(src), Is.True, $"Cartella sorgenti non trovata: {src}");

        string[] spie =
        [
            "using Microsoft.Identity.Web",
            "AddMicrosoftIdentityWebApi",
            "AddMicrosoftIdentityWebApp",
            "ITokenAcquisition",
            "IDownstreamApi",
            "MicrosoftIdentityAppCallsWebApiAuthenticationBuilder"
        ];

        var usi = Directory
            .EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(f => (File: f, Testo: File.ReadAllText(f)))
            .Where(x => spie.Any(s => x.Testo.Contains(s, StringComparison.Ordinal)))
            .Select(x => Path.GetRelativePath(src, x.File))
            .ToList();

        Assert.That(usi, Is.Empty,
            "Microsoft.Identity.Web risulta ora usata: " + string.Join(", ", usi) + ".\n"
            + "Finché non era usata, rimuoverla era la via per eliminare DUE pin di sicurezza (TD-3). "
            + "Se l'uso è voluto, aggiorna docs/architettura.md e il backlog tech-debt: i pin "
            + "Microsoft.Bcl.Memory e System.Security.Cryptography.Xml diventano permanenti.");
    }

    private static string? TrovaDepsJsonApi()
    {
        var bin = Path.Combine(
            FindRepositoryRoot(AppContext.BaseDirectory),
            "src", "Presentation", "PortaleFatture.BE.Api", "bin");

        if (!Directory.Exists(bin))
            return null;

        // Debug o Release, il più recente: il test non deve dipendere dalla configurazione usata.
        return Directory
            .EnumerateFiles(bin, "PortaleFatture.BE.Api.deps.json", SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static Version? LeggiVersioneDaDeps(string depsJson, string pacchetto)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(depsJson));

        if (!doc.RootElement.TryGetProperty("libraries", out var libraries))
            return null;

        foreach (var libreria in libraries.EnumerateObject())
        {
            var parti = libreria.Name.Split('/');
            if (parti.Length == 2
                && parti[0].Equals(pacchetto, StringComparison.OrdinalIgnoreCase)
                && Version.TryParse(parti[1].Split('-', '+')[0], out var v))
                return v;
        }

        return null;
    }

    private static string FindRepositoryRoot(string startPath)
    {
        var current = new DirectoryInfo(startPath);
        while (current is not null)
        {
            if (current.GetFiles("PortaleFatture.BE.Api.sln").Any() || current.GetFiles("_global.json").Any())
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Impossibile individuare la root repository (PortaleFatture.BE.Api.sln/_global.json).");
    }
}
