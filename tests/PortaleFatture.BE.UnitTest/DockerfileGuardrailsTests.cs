using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Guardrail sui Dockerfile dei tre deployable (Api, Function.API, SendEmailFunction).
///
/// Perche' esistono: un Dockerfile non viene compilato da nessuno. Gli errori che ci si fa dentro
/// non li vede ne' il compilatore ne' la suite — si scoprono al primo build dell'immagine, oppure
/// (peggio) al primo utilizzo reale in un ambiente. Nel repository non c'e' una pipeline di test su
/// PR, quindi questi controlli statici sono l'unica rete prima del deploy.
///
/// Cosa NON fanno: non costruiscono l'immagine. Verificano invarianti leggibili dai file
/// (chiusura transitiva dei COPY, pin per digest, coerenza fra codice e librerie native installate).
/// La prova che l'immagine si costruisce davvero resta il `docker build`, che va fatto a mano.
/// </summary>
public class DockerfileGuardrailsTests
{
    private const string Api = @"src\Presentation\PortaleFatture.BE.Api\PortaleFatture.BE.Api.csproj";
    private const string FunctionApi = @"src\Presentation\PortaleFatture.BE.Function.API\PortaleFatture.BE.Function.API.csproj";
    private const string SendEmailFunction = @"src\Presentation\PortaleFatture.BE.SendEmailFunction\PortaleFatture_BE_SendEmailFunction.csproj";

    /// <summary>
    /// Il `dotnet restore` dentro l'immagine gira PRIMA del `COPY . .` (per sfruttare la cache dei
    /// layer), quindi vede solo i .csproj copiati a mano uno per uno: se ne manca uno della chiusura
    /// transitiva delle ProjectReference, il restore fallisce. E' l'errore che si fa aggiungendo una
    /// ProjectReference a un progetto gia' containerizzato — niente in C# lo segnala.
    ///
    /// Onesta' sul valore: il ramo "COPY mancante" e' RUMOROSO (il build dell'immagine fallisce
    /// comunque, v. docs/deployable-e-dipendenze.md), quindi li' il test non evita il difetto, lo
    /// anticipa di qualche minuto — utile soprattutto finche' nessuna pipeline costruisce le tre
    /// immagini. Il ramo che porta valore vero e' l'opposto, il COPY di troppo: non rompe nulla e
    /// nessuno se ne accorge, allarga solo cio' che invalida la cache dei layer.
    /// </summary>
    [TestCase(Api)]
    [TestCase(FunctionApi)]
    [TestCase(SendEmailFunction)]
    public void Dockerfile_ShouldCopiare_EsattamenteLaChiusuraTransitivaDeiProgetti(string csprojRelativo)
    {
        var root = FindRepositoryRoot(AppContext.BaseDirectory);
        var csproj = Assoluto(root, csprojRelativo);
        var dockerfile = PercorsoDockerfile(csprojRelativo);

        Assert.That(File.Exists(dockerfile), Is.True, $"Dockerfile mancante: {dockerfile}");

        var attesi = ChiusuraTransitiva(csproj);
        var copiati = CsprojCopiati(dockerfile);

        // Un COPY che punta a un file inesistente e' l'errore piu' insidioso: la riga sembra giusta
        // a colpo d'occhio. Caso concreto del repository: la cartella della SendEmailFunction usa i
        // punti ma il suo csproj usa i trattini bassi.
        var inesistenti = copiati
            .Where(relativo => !File.Exists(Assoluto(root, relativo)))
            .ToList();

        Assert.That(inesistenti, Is.Empty,
            $"{Path.GetFileName(dockerfile)} copia .csproj che non esistono su disco: "
            + $"{string.Join(", ", inesistenti)}. Verificare il nome esatto del file (attenzione a "
            + "punti e trattini bassi) — il `dotnet restore` nell'immagine fallirebbe.");

        var normalizzati = copiati.Select(relativo => Assoluto(root, relativo)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var mancanti = attesi.Except(normalizzati, StringComparer.OrdinalIgnoreCase)
            .Select(Path.GetFileName)
            .ToList();

        Assert.That(mancanti, Is.Empty,
            $"{Path.GetFileName(csprojRelativo)}: progetti della chiusura transitiva non copiati nel "
            + $"Dockerfile: {string.Join(", ", mancanti)}. Il `dotnet restore` dentro l'immagine "
            + "fallirebbe con un errore di progetto mancante. Aggiungere la riga COPY corrispondente.");

        var eccesso = normalizzati.Except(attesi, StringComparer.OrdinalIgnoreCase)
            .Select(Path.GetFileName)
            .ToList();

        Assert.That(eccesso, Is.Empty,
            $"{Path.GetFileName(csprojRelativo)}: il Dockerfile copia .csproj che non fanno parte "
            + $"della chiusura transitiva: {string.Join(", ", eccesso)}. Non rompe la build, ma "
            + "allarga senza motivo cio' che invalida la cache dei layer.");
    }

    /// <summary>
    /// Convenzione dichiarata in docs/cicd-release.md: le immagini base sono pinnate per digest, non
    /// per tag, cosi' la build e' riproducibile. Un tag mobile (`:8.0`) fa cambiare il contenuto
    /// dell'immagine da un giorno all'altro a parita' di commit.
    /// </summary>
    [TestCase(Api)]
    [TestCase(FunctionApi)]
    [TestCase(SendEmailFunction)]
    public void Dockerfile_ShouldPinnare_LeImmaginiBasePerDigest(string csprojRelativo)
    {
        var dockerfile = PercorsoDockerfile(csprojRelativo);
        Assert.That(File.Exists(dockerfile), Is.True, $"Dockerfile mancante: {dockerfile}");

        var senzaDigest = RigheFrom(dockerfile)
            // Gli stage interni (FROM build AS publish) referenziano uno stage precedente, non
            // un'immagine di registry: si riconoscono perche' non hanno un percorso con '/'.
            .Where(immagine => immagine.Contains('/'))
            .Where(immagine => !immagine.Contains("@sha256:", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.That(senzaDigest, Is.Empty,
            $"{Path.GetFileName(dockerfile)}: immagini base non pinnate per digest: "
            + $"{string.Join(", ", senzaDigest)}. Con il solo tag la build non e' riproducibile.");
    }

    /// <summary>
    /// Presidio della decisione presa creando il Dockerfile della SendEmailFunction (PF-865): non
    /// installare le librerie native di wkhtmltopdf, perche' quel progetto usa solo i metodi HTML
    /// dei builder e non genera PDF.
    ///
    /// La regola verificata e' generale — "se il codice del deployable chiama un metodo che produce
    /// un PDF, il suo Dockerfile deve installare le librerie native" — cosi' il test non pretende
    /// l'assenza del blocco apt-get (aggiungerlo resta lecito) ma diventa rosso nel momento esatto
    /// in cui la decisione va rivista. Senza, la dipendenza si manifesterebbe come una
    /// DllNotFoundException a runtime, in produzione: misurato con `ldd`, libwkhtmltox.so ha sei
    /// dipendenze non risolte in un'immagine che non le installa (libX11, libXrender, libfontconfig,
    /// libfreetype, libjpeg, libpng16).
    ///
    /// Il caso Function.API non e' decorativo: genera PDF davvero, quindi e' la contro-prova che la
    /// rilevazione non e' cieca. Se un giorno smettesse di vedere le chiamate, quel caso fallirebbe
    /// invece di lasciare passare in silenzio anche il caso SendEmailFunction.
    ///
    /// L'Api ha un caso a parte, v. il test subito sotto.
    /// </summary>
    [TestCase(FunctionApi)]
    [TestCase(SendEmailFunction)]
    public void GenerazionePdfELibrerieNative_ShouldEssereCoerenti(string csprojRelativo)
    {
        var dockerfile = PercorsoDockerfile(csprojRelativo);
        Assert.That(File.Exists(dockerfile), Is.True, $"Dockerfile mancante: {dockerfile}");

        var metodi = MetodiCheGeneranoPdf();
        Assert.That(metodi, Is.Not.Empty,
            "Nessun metodo *Pdf trovato sui builder: il test non starebbe verificando nulla.");

        var chiamate = ChiamateAMetodiPdf(Path.GetDirectoryName(PercorsoCsproj(csprojRelativo))!, metodi);
        var installaNative = InstallaLibrerieNativePdf(dockerfile);

        if (chiamate.Count > 0 && !installaNative)
            Assert.Fail(
                $"{Path.GetFileName(csprojRelativo)} genera PDF ({string.Join(", ", chiamate)}) ma il suo "
                + "Dockerfile non installa le librerie native di wkhtmltopdf. Riprendere il blocco "
                + "apt-get dal Dockerfile della Function.API, altrimenti la chiamata fallisce a "
                + "runtime con una DllNotFoundException.");

        Assert.Pass(chiamate.Count > 0
            ? $"{Path.GetFileName(csprojRelativo)}: genera PDF e installa le librerie native."
            : $"{Path.GetFileName(csprojRelativo)}: non genera PDF, le librerie native non servono.");
    }

    /// <summary>
    /// Difetto CONFERMATO, non una svista di questa suite. L'Api genera PDF (RelModule,
    /// DatiModuloCommessaModule, FattureModule) ma il suo Dockerfile non installa nulla, e
    /// l'immagine base `dotnet/aspnet:8.0` non contiene quelle librerie: misurato il 15/09/2026
    /// con `ldd` di libwkhtmltox.so dentro quell'immagine — sei dipendenze non risolte.
    ///
    /// Il 16/09/2026 la deduzione e' diventata un fatto, con una chiamata reale:
    /// GET api/rel/pagopa/documento/download/{id}?tipo=pdf risponde 500 con "Unable to load native
    /// library. The platform may be missing native dependencies (libjpeg62, etc)" — il messaggio
    /// nomina uno dei sei pacchetti misurati. Senza il parametro, la stessa rotta risponde 200 HTML.
    ///
    /// Perche' nessuno se n'era accorto: il ramo PDF e' dietro `tipo == "pdf"` (case-sensitive) e il
    /// portale non lo imbocca mai — delle otto chiamate del frontend, sei non mandano il parametro e
    /// due mandano un valore diverso. Resta pero' raggiungibile da chiunque abbia un token.
    ///
    /// Le due strade sono opposte: portare il blocco apt-get anche nel Dockerfile dell'Api (tocca
    /// l'immagine di produzione dell'applicazione principale), oppure rimuovere i rami tipo=="pdf" e
    /// il pacchetto Haukcode.WkHtmlToPdfDotNet. E' prima di tutto una decisione di prodotto, da
    /// tracciare a parte. Quando sara' presa, togliere l'[Ignore] se si e' scelto il fix; se si e'
    /// scelta la rimozione, sparira' da solo il presupposto (nessuna chiamata a CreatePdf) e questo
    /// test si potra' eliminare.
    /// </summary>
    [Test]
    [Ignore("Difetto confermato il 16/09/2026: l'Api genera PDF ma la sua immagine non ha le "
          + "librerie native di wkhtmltopdf. Una chiamata reale a api/rel/pagopa/documento/download/"
          + "{id}?tipo=pdf risponde 500 'Unable to load native library ... (libjpeg62, etc)'. Il "
          + "ramo non e' raggiungibile dal portale, quindi non e' urgente; la scelta fra fix del "
          + "Dockerfile e rimozione del ramo e' di prodotto e va tracciata a parte.")]
    public void Api_GenerandoPdf_ShouldInstallare_LeLibrerieNative()
    {
        var dockerfile = PercorsoDockerfile(Api);
        var chiamate = ChiamateAMetodiPdf(Path.GetDirectoryName(PercorsoCsproj(Api))!, MetodiCheGeneranoPdf());

        Assert.That(chiamate, Is.Not.Empty, "Se l'Api non generasse piu' PDF, questo test si puo' eliminare.");
        Assert.That(InstallaLibrerieNativePdf(dockerfile), Is.True,
            "Il Dockerfile dell'Api non installa le librerie native richieste da libwkhtmltox.so: "
            + "le rotte che scaricano un PDF falliscono a runtime nel container.");
    }

    /// <summary>
    /// I builder leggono i template dal disco a runtime (percorso ricavato da
    /// Assembly.GetExecutingAssembly().Location), quindi un template che non viene copiato
    /// nell'output non produce alcun errore di build: l'immagine si costruisce, e l'invio esplode
    /// alla prima esecuzione reale. Il nome del file, per di piu', e' una stringa in un campo
    /// privato: rinominarlo o aggiungerne uno nuovo senza la riga corrispondente nel csproj non
    /// rompe niente di visibile.
    ///
    /// Limite noto: il test verifica che ogni template citato esista e sia dichiarato per la copia
    /// in ALMENO un progetto. Non verifica che arrivi nell'output dello specifico deployable che lo
    /// usa — per la SendEmailFunction, ad esempio, 15 dei 26 template arrivano per copia transitiva
    /// dal progetto Api.
    /// </summary>
    [Test]
    public void TemplateCitatiDaiBuilder_ShouldEsistere_EEssereDichiaratiPerLaCopiaInOutput()
    {
        var root = FindRepositoryRoot(AppContext.BaseDirectory);
        var citati = TemplateCitatiDaiBuilder();

        Assert.That(citati, Is.Not.Empty,
            "Nessun template .html trovato sui builder: il test non starebbe verificando nulla.");

        var presenti = Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*.html", SearchOption.AllDirectories)
            .Where(NonEDiBuild)
            .ToList();

        var dichiarati = DeployableCsproj()
            .Select(relativo => Assoluto(root, relativo))
            .Where(File.Exists)
            .SelectMany(TemplateDichiaratiPerLaCopia)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var mancanti = citati
            .Where(nome => !presenti.Any(file => string.Equals(Path.GetFileName(file), nome, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Assert.That(mancanti, Is.Empty,
            $"Template citati dai builder ma assenti dai sorgenti: {string.Join(", ", mancanti)}. "
            + "A runtime la lettura del file fallisce.");

        var nonDichiarati = citati
            .Where(nome => !dichiarati.Contains(nome))
            .ToList();

        Assert.That(nonDichiarati, Is.Empty,
            $"Template presenti nei sorgenti ma non dichiarati con CopyToOutputDirectory in nessun "
            + $"csproj dei deployable: {string.Join(", ", nonDichiarati)}. Non finiscono nell'immagine "
            + "e l'errore si vede solo alla prima generazione del documento.");
    }

    // --- lettura dei Dockerfile ---------------------------------------------------------------

    private static IReadOnlyList<string> CsprojCopiati(string dockerfile) =>
        // Il gruppo si chiude da solo sulla virgoletta successiva: non serve includerla nel pattern.
        Regex.Matches(File.ReadAllText(dockerfile), """COPY\s*\[\s*"([^"]+\.csproj)""", RegexOptions.IgnoreCase)
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static IReadOnlyList<string> RigheFrom(string dockerfile) =>
        File.ReadLines(dockerfile)
            .Select(riga => riga.Trim())
            .Where(riga => riga.StartsWith("FROM ", StringComparison.OrdinalIgnoreCase))
            .Select(riga => riga.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1])
            .ToList();

    /// <summary>
    /// Le sei librerie non risolte che `ldd` segnala su libwkhtmltox.so, nella forma dei pacchetti
    /// Debian che le portano. Ne bastano due come marcatore: il blocco o c'e' tutto o non c'e'.
    /// </summary>
    private static bool InstallaLibrerieNativePdf(string dockerfile)
    {
        var testo = SenzaCommenti(File.ReadAllText(dockerfile));

        return testo.Contains("apt-get", StringComparison.OrdinalIgnoreCase)
            && testo.Contains("libfontconfig1", StringComparison.OrdinalIgnoreCase)
            && testo.Contains("libjpeg62", StringComparison.OrdinalIgnoreCase);
    }

    // Il Dockerfile della SendEmailFunction NOMINA quelle librerie in un commento, per spiegare
    // perche' non le installa: senza questa pulizia il test le scambierebbe per un apt-get vero.
    private static string SenzaCommenti(string testo) =>
        string.Join('\n', testo.Split('\n').Where(riga => !riga.TrimStart().StartsWith('#')));

    // --- lettura dei csproj --------------------------------------------------------------------

    private static ISet<string> ChiusuraTransitiva(string csproj)
    {
        var visitati = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var daVisitare = new Queue<string>([Path.GetFullPath(csproj)]);

        while (daVisitare.Count > 0)
        {
            var corrente = daVisitare.Dequeue();
            if (!visitati.Add(corrente) || !File.Exists(corrente))
                continue;

            var cartella = Path.GetDirectoryName(corrente)!;

            foreach (var riferimento in XDocument.Load(corrente)
                         .Descendants("ProjectReference")
                         .Select(x => (string?)x.Attribute("Include"))
                         .Where(include => !string.IsNullOrWhiteSpace(include)))
            {
                var percorso = Path.GetFullPath(
                    Path.Combine(cartella, riferimento!.Replace('\\', Path.DirectorySeparatorChar)));

                daVisitare.Enqueue(percorso);
            }
        }

        return visitati;
    }

    private static IEnumerable<string> TemplateDichiaratiPerLaCopia(string csproj) =>
        XDocument.Load(csproj)
            .Descendants()
            .Where(x => x.Name.LocalName is "None" or "Content")
            .Where(x => x.Elements().Any(e => e.Name.LocalName == "CopyToOutputDirectory"))
            .Select(x => (string?)x.Attribute("Update") ?? (string?)x.Attribute("Include"))
            .Where(percorso => percorso is not null && percorso.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            .Select(percorso => Path.GetFileName(percorso!.Replace('\\', '/')));

    // --- lettura del codice --------------------------------------------------------------------

    /// <summary>
    /// I metodi dei builder che producono un PDF, ricavati per reflection invece che da un elenco
    /// scritto a mano: un metodo *Pdf aggiunto domani entra automaticamente nel perimetro.
    /// </summary>
    private static IReadOnlyCollection<string> MetodiCheGeneranoPdf() =>
        new[] { typeof(DocumentBuilder), typeof(DocumentPspBuilder) }
            .SelectMany(tipo => tipo.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(metodo => metodo.ReturnType == typeof(byte[])
                          && metodo.Name.EndsWith("Pdf", StringComparison.Ordinal))
            .Select(metodo => metodo.Name)
            .Distinct(StringComparer.Ordinal)
            .ToList();

    private static IReadOnlyCollection<string> ChiamateAMetodiPdf(string cartellaProgetto, IReadOnlyCollection<string> metodi) =>
        Directory.EnumerateFiles(cartellaProgetto, "*.cs", SearchOption.AllDirectories)
            .Where(NonEDiBuild)
            .SelectMany(file =>
            {
                var testo = File.ReadAllText(file);
                return metodi.Where(metodo => testo.Contains(metodo + "(", StringComparison.Ordinal));
            })
            .Distinct(StringComparer.Ordinal)
            .ToList();

    private static IReadOnlyCollection<string> TemplateCitatiDaiBuilder() =>
        new[] { typeof(DocumentBuilder), typeof(DocumentPspBuilder) }
            .SelectMany(tipo => tipo.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(campo => campo.FieldType == typeof(string))
            .Select(campo => campo.GetValue(null) as string)
            .Where(valore => valore is not null && valore.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            .Select(valore => Path.GetFileName(valore!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    // --- percorsi ------------------------------------------------------------------------------

    private static IEnumerable<string> DeployableCsproj() => [Api, FunctionApi, SendEmailFunction];

    private static string PercorsoCsproj(string csprojRelativo) =>
        Assoluto(FindRepositoryRoot(AppContext.BaseDirectory), csprojRelativo);

    private static string PercorsoDockerfile(string csprojRelativo) =>
        Path.Combine(Path.GetDirectoryName(PercorsoCsproj(csprojRelativo))!, "Dockerfile");

    private static string Assoluto(string root, string relativo) =>
        Path.GetFullPath(Path.Combine(root, relativo.Replace('\\', Path.DirectorySeparatorChar)
                                                    .Replace('/', Path.DirectorySeparatorChar)));

    private static bool NonEDiBuild(string percorso) =>
        !percorso.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
        && !percorso.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);

    // Duplicato da DependencyGuardrailsTests: estrarlo in un helper condiviso significherebbe
    // toccare quel file, che e' il presidio dei pin di sicurezza.
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
