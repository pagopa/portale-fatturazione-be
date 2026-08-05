using System.Text.Json;
using System.Xml.Linq;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Guardrail sui pin di sicurezza: pacchetti con advisory che arrivano per via TRANSITIVA e che
/// oggi vengono forzati a una versione sana con una PackageReference diretta nel csproj del
/// deployable (PF-784 System.Text.RegularExpressions, PF-785 System.Net.Http,
/// PF-787 Microsoft.Bcl.Memory).
///
/// L'invariante verificata e' sul GRAFO RISOLTO (obj/project.assets.json), non sulla forma della
/// soluzione: "in quel deployable il pacchetto non si risolve a una versione con advisory",
/// comunque ci si arrivi. Conseguenze volute:
///  - togliere un pin diventato superfluo (pacchetto padre aggiornato che porta gia' la transitiva
///    sana, o pacchetto uscito del tutto dal grafo) lascia il test VERDE: il pin non e' un fine in
///    se', e un test che lo pretende per sempre trasforma il rimedio in debito permanente;
///  - una eventuale migrazione a Central Package Management (versioni spostate in
///    Directory.Packages.props, PackageReference senza attributo Version) non rompe il test;
///  - togliere il pin senza che nulla lo sostituisca lo fa fallire, che e' il motivo per cui
///    esiste: nessun codice referenzia questi pacchetti, quindi una pulizia dei csproj li
///    rimuoverebbe riaprendo l'advisory senza che nulla fallisca.
///
/// Non testano comportamento: per alcuni (System.Net.Http su net8.0) il pacchetto e' un facade e i
/// tipi arrivano dal framework condiviso, quindi il pin agisce solo sulla risoluzione del grafo
/// NuGet. Se obj/project.assets.json non esiste (progetto non ancora ripristinato: e' il caso di
/// Function.API, che nessun progetto di test referenzia) si ripiega sulla DICHIARAZIONE letta da
/// csproj/Directory.Packages.props, dichiarandolo nel messaggio di errore.
///
/// Copertura che NON danno: gli advisory su pacchetti non elencati qui. Quella resta a
/// `dotnet list package --vulnerable --include-transitive`. Quando arriva un nuovo alert e si
/// aggiunge un pin, aggiungere qui il relativo TestCase.
/// </summary>
public class DependencyGuardrailsTests
{
    [TestCase(@"src\Presentation\PortaleFatture.BE.Api\PortaleFatture.BE.Api.csproj", "Microsoft.Bcl.Memory", "9.0.14")]
    [TestCase(@"src\Presentation\PortaleFatture.BE.SendEmailFunction\PortaleFatture_BE_SendEmailFunction.csproj", "System.Net.Http", "4.3.4")]
    [TestCase(@"src\Presentation\PortaleFatture.BE.SendEmailFunction\PortaleFatture_BE_SendEmailFunction.csproj", "System.Text.RegularExpressions", "4.3.1")]
    [TestCase(@"src\Presentation\PortaleFatture.BE.Function.API\PortaleFatture.BE.Function.API.csproj", "System.Net.Http", "4.3.4")]
    [TestCase(@"src\Presentation\PortaleFatture.BE.Function.API\PortaleFatture.BE.Function.API.csproj", "System.Text.RegularExpressions", "4.3.1")]
    public void PacchettoConAdvisory_ShouldResolve_AtLeastMinimumVersion(
        string percorsoRelativo, string pacchetto, string versioneMinima)
    {
        var minima = ParseVersione(versioneMinima).Numero!;

        var root = FindRepositoryRoot(AppContext.BaseDirectory);
        var csproj = Path.Combine(root, percorsoRelativo.Replace('\\', Path.DirectorySeparatorChar));
        var progetto = Path.GetFileName(csproj);

        Assert.That(File.Exists(csproj), Is.True, $"Progetto non trovato: {csproj}");

        var dichiarata = LeggiVersioneDichiarata(csproj, pacchetto, root);
        var assets = Path.Combine(Path.GetDirectoryName(csproj)!, "obj", "project.assets.json");

        if (File.Exists(assets))
        {
            var (presente, risolta) = LeggiVersioneRisolta(assets, pacchetto);

            if (!presente)
                Assert.Pass(
                    $"{pacchetto} non compare nel grafo risolto di {progetto}: non e' piu' una dipendenza, "
                    + "nessuna esposizione all'advisory (il pin, se rimosso, era superfluo).");

            Assert.That(risolta.Numero, Is.Not.Null,
                $"Versione di {pacchetto} non interpretabile nel grafo risolto di {progetto}: '{risolta.Testo}'.");

            // Il pin diretto vince sempre sulla risoluzione transitiva ("direct wins"): se la
            // dichiarazione e' a posto ma il grafo no, l'unica spiegazione e' un obj/ non riallineato.
            if (!SoddisfaMinimo(risolta, minima) && SoddisfaMinimo(dichiarata, minima))
                Assert.Fail(
                    $"{pacchetto} risolto a {risolta.Testo} in {progetto} ma dichiarato {dichiarata.Testo}: "
                    + "grafo NuGet non riallineato (tipico dopo un cambio di branch). "
                    + "Eseguire `dotnet restore` e rilanciare, non e' un pin mancante.");

            Assert.That(SoddisfaMinimo(risolta, minima), Is.True,
                $"{pacchetto} si risolve a {risolta.Testo} in {progetto}, atteso >= {versioneMinima}: "
                + "il pacchetto e' esposto all'advisory. Ripristinare il pin diretto (o aggiornare il "
                + "pacchetto padre che lo trascina a quella versione).");

            return;
        }

        // Nessun grafo disponibile: si verifica la sola dichiarazione, che e' un controllo piu' debole.
        Assert.That(dichiarata.Testo, Is.Not.Null,
            $"Pin di {pacchetto} assente in {progetto} (verificata la dichiarazione, "
            + $"non il grafo: {Path.GetFileName(assets)} non presente, eseguire `dotnet build` sulla solution). "
            + "Senza pin il pacchetto tornerebbe a risolversi per via transitiva a una versione con advisory.");

        Assert.That(dichiarata.Numero, Is.Not.Null,
            $"Versione di {pacchetto} non interpretabile in {progetto}: '{dichiarata.Testo}'.");

        Assert.That(SoddisfaMinimo(dichiarata, minima), Is.True,
            $"{pacchetto} dichiarato {dichiarata.Testo} in {progetto}, atteso >= {versioneMinima} "
            + "(verificata la dichiarazione, non il grafo).");
    }

    /// <summary>
    /// Versione con cui il pacchetto entra davvero nel grafo del progetto, letta dai target di
    /// project.assets.json (chiavi nella forma "Nome/Versione"). Se compare in piu' target si
    /// prende la piu' bassa, che e' quella che determina l'esposizione.
    /// </summary>
    private static (bool Presente, VersioneNuGet Versione) LeggiVersioneRisolta(string assets, string pacchetto)
    {
        // File.ReadAllText gestisce l'eventuale BOM, che farebbe fallire il parse dei byte grezzi.
        using var documento = JsonDocument.Parse(File.ReadAllText(assets));

        if (!documento.RootElement.TryGetProperty("targets", out var targets))
            return (false, default);

        var prefisso = pacchetto + "/";
        var trovate = targets.EnumerateObject()
            .SelectMany(target => target.Value.EnumerateObject())
            .Select(libreria => libreria.Name)
            .Where(nome => nome.StartsWith(prefisso, StringComparison.OrdinalIgnoreCase))
            .Select(nome => nome[prefisso.Length..])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(ParseVersione)
            .ToList();

        if (trovate.Count == 0)
            return (false, default);

        var minore = trovate
            .OrderBy(v => v.Numero ?? new Version(0, 0))
            .ThenByDescending(v => v.Prerelease)
            .First();

        return (true, minore);
    }

    /// <summary>
    /// Versione dichiarata per il pacchetto: PackageReference nel csproj, oppure — se l'attributo
    /// Version non c'e' perche' si e' passati a Central Package Management — PackageVersion nel
    /// Directory.Packages.props piu' vicino risalendo verso la root del repository.
    /// </summary>
    private static VersioneNuGet LeggiVersioneDichiarata(string csproj, string pacchetto, string root)
    {
        var daCsproj = XDocument.Load(csproj)
            .Descendants("PackageReference")
            .Where(x => string.Equals((string?)x.Attribute("Include"), pacchetto, StringComparison.OrdinalIgnoreCase))
            .Select(x => (string?)x.Attribute("Version") ?? (string?)x.Element("Version"))
            .FirstOrDefault(versione => !string.IsNullOrWhiteSpace(versione));

        if (daCsproj is not null)
            return ParseVersione(daCsproj);

        var cartella = new DirectoryInfo(Path.GetDirectoryName(csproj)!);
        while (cartella is not null)
        {
            var props = Path.Combine(cartella.FullName, "Directory.Packages.props");
            if (File.Exists(props))
            {
                var daProps = XDocument.Load(props)
                    .Descendants("PackageVersion")
                    .Where(x => string.Equals((string?)x.Attribute("Include"), pacchetto, StringComparison.OrdinalIgnoreCase))
                    .Select(x => (string?)x.Attribute("Version"))
                    .FirstOrDefault(versione => !string.IsNullOrWhiteSpace(versione));

                if (daProps is not null)
                    return ParseVersione(daProps);
            }

            if (string.Equals(cartella.FullName.TrimEnd(Path.DirectorySeparatorChar),
                              root.TrimEnd(Path.DirectorySeparatorChar),
                              StringComparison.OrdinalIgnoreCase))
                break;

            cartella = cartella.Parent;
        }

        return default;
    }

    /// <summary>
    /// Versione NuGet ridotta alla sola parte numerica piu' il flag di prerelease: System.Version
    /// non sa leggere "1.22.1-Preview.1" e lancerebbe.
    /// </summary>
    private readonly record struct VersioneNuGet(string? Testo, Version? Numero, bool Prerelease);

    private static VersioneNuGet ParseVersione(string? testo)
    {
        if (string.IsNullOrWhiteSpace(testo))
            return default;

        var separatore = testo.IndexOfAny(['-', '+']);
        var numero = separatore < 0 ? testo : testo[..separatore];

        return Version.TryParse(numero, out var v)
            ? new VersioneNuGet(testo, v, separatore >= 0)
            : new VersioneNuGet(testo, null, separatore >= 0);
    }

    // Una prerelease con lo stesso numero della soglia (es. 4.3.4-beta) e' considerata minore.
    private static bool SoddisfaMinimo(VersioneNuGet versione, Version minima) =>
        versione.Numero is not null
        && (versione.Numero > minima || (versione.Numero == minima && !versione.Prerelease));

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
