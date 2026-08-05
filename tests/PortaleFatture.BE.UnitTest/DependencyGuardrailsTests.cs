using System.Xml.Linq;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Guardrail sul pin di System.Net.Http (advisory sulle versioni &lt; 4.3.4).
/// Non testa comportamento: su net8.0 il pacchetto e' un facade e i tipi arrivano dal framework
/// condiviso, quindi il pin agisce sulla risoluzione del grafo NuGet e non sul codice eseguito.
/// Serve a impedire che venga rimosso durante una pulizia dei csproj, caso in cui il pacchetto
/// tornerebbe a risolversi per via transitiva a una versione con advisory senza che nulla fallisca.
/// La copertura delle dipendenze SOLO transitive resta a `dotnet list package --vulnerable`.
/// </summary>
public class DependencyGuardrailsTests
{
    private const string PacchettoConAdvisory = "System.Net.Http";
    private static readonly Version VersioneMinima = new("4.3.4");

    [TestCase(@"src\Presentation\PortaleFatture.BE.SendEmailFunction\PortaleFatture_BE_SendEmailFunction.csproj")]
    [TestCase(@"src\Presentation\PortaleFatture.BE.Function.API\PortaleFatture.BE.Function.API.csproj")]
    public void SystemNetHttp_ShouldBePinned_AtLeastMinimumVersion(string percorsoRelativo)
    {
        var csproj = Path.Combine(
            FindRepositoryRoot(AppContext.BaseDirectory),
            percorsoRelativo.Replace('\\', Path.DirectorySeparatorChar));

        Assert.That(File.Exists(csproj), Is.True, $"Progetto non trovato: {csproj}");

        var versione = XDocument.Load(csproj)
            .Descendants("PackageReference")
            .Where(x => string.Equals((string?)x.Attribute("Include"), PacchettoConAdvisory,
                                      StringComparison.OrdinalIgnoreCase))
            .Select(x => (string?)x.Attribute("Version"))
            .FirstOrDefault();

        Assert.That(versione, Is.Not.Null,
            $"Pin di {PacchettoConAdvisory} assente in {Path.GetFileName(csproj)}: il pacchetto tornerebbe "
            + "a risolversi per via transitiva a una versione con advisory.");

        Assert.That(Version.TryParse(versione, out var v), Is.True,
            $"Versione di {PacchettoConAdvisory} non interpretabile in {Path.GetFileName(csproj)}: '{versione}'.");

        Assert.That(v, Is.GreaterThanOrEqualTo(VersioneMinima),
            $"{PacchettoConAdvisory} in {Path.GetFileName(csproj)} e' {versione}, atteso >= {VersioneMinima}.");
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
