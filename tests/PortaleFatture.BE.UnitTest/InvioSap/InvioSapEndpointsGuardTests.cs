using System.Text.RegularExpressions;

namespace PortaleFatture.BE.UnitTest.InvioSap;

/// <summary>
/// Gemello di <c>FattureCriticalEndpointsGuardTests</c> per le sette rotte di PF-735 (be-invio-sap.md §3):
/// nessuno deve poterle togliere o rinominare per sbaglio da <c>FattureEndpoints.cs</c>, e
/// <c>generazioni/*</c> non deve tornare (D2 ritirata: l'avanzamento si legge dalle viste).
/// </summary>
[Category("InvioSap")]
public class InvioSapEndpointsGuardTests
{
    public static readonly string[] RotteInvioSap =
    [
        "api/fatture/invio-sap",
        "api/fatture/invio-sap/dettaglio",
        "api/fatture/invio-sap/download",
        "api/fatture/invio-sap/download/reinviate",
        "api/fatture/invio-sap/genera",
        "api/fatture/invio-sap/lock",
        "api/fatture/invio-sap/stati",
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
        var extra = mappate.Where(r => r.StartsWith("api/fatture/invio-sap", StringComparison.OrdinalIgnoreCase)
                                    && !RotteInvioSap.Contains(r, StringComparer.OrdinalIgnoreCase)).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(mancanti, Is.Empty, $"Rotte Invio a SAP mancanti: {string.Join(", ", mancanti)}");
            Assert.That(extra, Is.Empty, $"Rotte Invio a SAP non previste dal contratto: {string.Join(", ", extra)}");
        });
    }

    [Test]
    public void RotteLegacy_ShouldEssereAncoraMappate_MaDeprecate()
    {
        // Decisione del 27/10/2026: deprecare, non rimuovere. Se qualcuno le toglie prima del rilascio FE
        // (US5), la pagina Documenti Contabili Emessi in produzione smette di funzionare.
        var endpointsFile = Path.Combine(FindRepositoryRoot(AppContext.BaseDirectory),
            "src", "Presentation", "PortaleFatture.BE.Api", "Modules", "SEND", "Fatture", "FattureEndpoints.cs");
        var content = File.ReadAllText(endpointsFile);

        string[] legacy = ["api/fatture/invio/sap/multiplo", "api/fatture/invio/sap/multiplo/periodo", "api/fatture/invio/pipeline",
                           "api/fatture/resetta/pipeline", "api/fatture/invio/sap"];
        Assert.Multiple(() =>
        {
            foreach (var rotta in legacy)
                Assert.That(content, Does.Contain($"\"{rotta}\""), $"Rotta legacy rimossa troppo presto: {rotta}");
            Assert.That(System.Text.RegularExpressions.Regex.Matches(content, "Deprecated = true").Count, Is.EqualTo(6),
                "Le sei rotte legacy di invio a SAP devono risultare deprecate in OpenAPI.");
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
