using System.Text.RegularExpressions;

namespace PortaleFatture.BE.UnitTest;

public class FattureCriticalEndpointsGuardTests
{
    private static readonly string[] CriticalRoutes =
    {
        "api/fatture/ente/download",
        "api/fatture/ente",
        "api/fatture/andamento-sospese/download",
        "api/fatture/ente/tipologia",
        "api/fatture/pagopa/gestione-fatture",
        "api/fatture/pagopa/gestione-fatture/anni",
        "api/fatture/pagopa/gestione-fatture/mesi",
        "api/fatture/pagopa/gestione-fatture/tipologia-fattura",
        "api/fatture/pagopa/gestione-fatture/modifica/anni",
        "api/fatture/pagopa/gestione-fatture/modifica/mesi",
        "api/fatture/pagopa/gestione-fatture/azione",
        "api/fatture/pagopa/gestione-fatture/download"
    };

    [Test]
    public void RegisterEndpoints_ShouldContain_AllCriticalFattureRoutes()
    {
        var repoRoot = FindRepositoryRoot(AppContext.BaseDirectory);
        var endpointsFile = Path.Combine(
            repoRoot,
            "src",
            "Presentation",
            "PortaleFatture.BE.Api",
            "Modules",
            "SEND",
            "Fatture",
            "FattureEndpoints.cs");

        Assert.That(File.Exists(endpointsFile), Is.True,
            $"File endpoint non trovato: {endpointsFile}");

        var content = File.ReadAllText(endpointsFile);
        var mappedRoutes = Regex.Matches(content, "\\.Map(?:Get|Post|Put|Delete|Patch)\\(\"([^\"]+)\"")
            .Select(m => m.Groups[1].Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingRoutes = CriticalRoutes
            .Where(route => !mappedRoutes.Contains(route))
            .ToList();

        Assert.That(missingRoutes, Is.Empty,
            $"Endpoint critici mancanti: {string.Join(", ", missingRoutes)}");
    }

    private static string FindRepositoryRoot(string startPath)
    {
        var current = new DirectoryInfo(startPath);
        while (current is not null)
        {
            var hasSolution = current.GetFiles("PortaleFatture.BE.Api.sln").Any();
            var hasGlobal = current.GetFiles("_global.json").Any();
            if (hasSolution || hasGlobal)
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Impossibile individuare la root repository (PortaleFatture.BE.Api.sln/_global.json).");
    }
}