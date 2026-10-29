using System.Text.RegularExpressions;

namespace PortaleFatture.BE.UnitTest.InvioSap;

/// <summary>
/// Gemello di <c>FattureCriticalEndpointsGuardTests</c> per le sei rotte di PF-735: una volta
/// implementate, nessuno deve poterle togliere o rinominare per sbaglio da <c>FattureEndpoints.cs</c>.
///
/// E' <c>[Explicit]</c> finche' le rotte non esistono (PF-965 fase 2-4): si esegue con
/// <c>--filter "Category=InvioSap"</c>. Quando la fase 2 e' chiusa, togliere l'attributo e spostare le
/// rotte nella lista principale di <c>FattureCriticalEndpointsGuardTests</c>.
/// </summary>
[Category("InvioSap")]
[Explicit("PF-965: le rotte api/fatture/invio-sap/* non sono ancora mappate (fasi 2-4 del piano).")]
public class InvioSapEndpointsGuardTests
{
    public static readonly string[] RotteInvioSap =
    [
        "api/fatture/invio-sap",
        "api/fatture/invio-sap/dettaglio",
        "api/fatture/invio-sap/download",
        "api/fatture/invio-sap/download/reinviate",
        "api/fatture/invio-sap/genera",
        "api/fatture/invio-sap/generazioni/{batchId:guid}",
    ];

    [Test]
    public void FattureEndpoints_ShouldMappare_TutteLeRotteInvioSap()
    {
        var endpointsFile = Path.Combine(FindRepositoryRoot(AppContext.BaseDirectory),
            "src", "Presentation", "PortaleFatture.BE.Api", "Modules", "SEND", "Fatture", "FattureEndpoints.cs");
        Assert.That(File.Exists(endpointsFile), Is.True, $"File endpoint non trovato: {endpointsFile}");

        var content = File.ReadAllText(endpointsFile);
        var mappate = Regex.Matches(content, "\\.Map(?:Get|Post|Put|Delete|Patch)\\(\"([^\"]+)\"")
            .Select(m => m.Groups[1].Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var mancanti = RotteInvioSap.Where(r => !mappate.Contains(r)).ToList();
        Assert.That(mancanti, Is.Empty, $"Rotte Invio a SAP mancanti: {string.Join(", ", mancanti)}");
    }

    [Test]
    public void RotteLegacy_ShouldEssereAncoraMappate_MaDeprecate()
    {
        // Decisione del 27/10/2026: deprecare, non rimuovere. Se qualcuno le toglie prima del rilascio FE
        // (US5), la pagina Documenti Contabili Emessi in produzione smette di funzionare.
        var endpointsFile = Path.Combine(FindRepositoryRoot(AppContext.BaseDirectory),
            "src", "Presentation", "PortaleFatture.BE.Api", "Modules", "SEND", "Fatture", "FattureEndpoints.cs");
        var content = File.ReadAllText(endpointsFile);

        string[] legacy = ["api/fatture/invio/sap/multiplo", "api/fatture/invio/sap/multiplo/periodo", "api/fatture/invio/pipeline"];
        Assert.Multiple(() =>
        {
            foreach (var rotta in legacy)
                Assert.That(content, Does.Contain($"\"{rotta}\""), $"Rotta legacy rimossa troppo presto: {rotta}");
            Assert.That(content, Does.Contain("Deprecated = true"), "Le rotte legacy devono risultare deprecate in OpenAPI.");
        });
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
        throw new DirectoryNotFoundException("Impossibile individuare la root repository.");
    }
}
