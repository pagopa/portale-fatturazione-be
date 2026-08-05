using System.Xml.Linq;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Guardrail sui pin di sicurezza: pacchetti con advisory che arrivano per via TRANSITIVA e che
/// vengono forzati a una versione sana con una PackageReference diretta nel csproj del deployable.
/// Non testano comportamento (per alcuni, come System.Net.Http su net8.0, il pacchetto e' un facade
/// e i tipi arrivano dal framework condiviso): il pin agisce sulla risoluzione del grafo NuGet.
/// Servono a impedire che un pin venga rimosso o abbassato durante una pulizia dei csproj, caso in
/// cui il pacchetto tornerebbe a risolversi in modo transitivo alla versione con advisory senza che
/// nulla fallisca. La copertura delle dipendenze SOLO transitive resta a
/// `dotnet list package --vulnerable --include-transitive`.
/// Quando arriva un nuovo alert e si aggiunge un pin, aggiungere qui il relativo TestCase.
/// </summary>
public class DependencyGuardrailsTests
{
    [TestCase(@"src\Presentation\PortaleFatture.BE.Api\PortaleFatture.BE.Api.csproj", "Microsoft.Bcl.Memory", "9.0.14")]
    [TestCase(@"src\Presentation\PortaleFatture.BE.SendEmailFunction\PortaleFatture_BE_SendEmailFunction.csproj", "System.Net.Http", "4.3.4")]
    [TestCase(@"src\Presentation\PortaleFatture.BE.SendEmailFunction\PortaleFatture_BE_SendEmailFunction.csproj", "System.Text.RegularExpressions", "4.3.1")]
    [TestCase(@"src\Presentation\PortaleFatture.BE.Function.API\PortaleFatture.BE.Function.API.csproj", "System.Net.Http", "4.3.4")]
    [TestCase(@"src\Presentation\PortaleFatture.BE.Function.API\PortaleFatture.BE.Function.API.csproj", "System.Text.RegularExpressions", "4.3.1")]
    public void PacchettoConAdvisory_ShouldBePinned_AtLeastMinimumVersion(
        string percorsoRelativo, string pacchetto, string versioneMinima)
    {
        var minima = Version.Parse(versioneMinima);

        var csproj = Path.Combine(
            FindRepositoryRoot(AppContext.BaseDirectory),
            percorsoRelativo.Replace('\\', Path.DirectorySeparatorChar));

        Assert.That(File.Exists(csproj), Is.True, $"Progetto non trovato: {csproj}");

        var versione = XDocument.Load(csproj)
            .Descendants("PackageReference")
            .Where(x => string.Equals((string?)x.Attribute("Include"), pacchetto,
                                      StringComparison.OrdinalIgnoreCase))
            .Select(x => (string?)x.Attribute("Version"))
            .FirstOrDefault();

        Assert.That(versione, Is.Not.Null,
            $"Pin di {pacchetto} assente in {Path.GetFileName(csproj)}: il pacchetto tornerebbe "
            + "a risolversi per via transitiva a una versione con advisory.");

        Assert.That(Version.TryParse(versione, out var v), Is.True,
            $"Versione di {pacchetto} non interpretabile in {Path.GetFileName(csproj)}: '{versione}'.");

        Assert.That(v, Is.GreaterThanOrEqualTo(minima),
            $"{pacchetto} in {Path.GetFileName(csproj)} e' {versione}, atteso >= {minima}.");
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
