using System.Text.RegularExpressions;

namespace PortaleFatture.BE.UnitTest.FunctionApi;

/// <summary>
/// L'isolamento fra aderenti nella Integration API, presidiato dove nasce.
///
/// La catena e': AuthMiddleware risolve la chiave e mette l'IdEnte in FunctionContext.Items ->
/// GetSession() lo impacchetta in una Session -> la Session viaggia nel payload
/// dell'orchestrazione -> ogni activity costruisce da li' l'AuthenticationInfo con cui filtra i
/// dati. Il lato "il chiamante non puo' influenzare l'IdEnte via header" e' coperto da
/// AuthMiddlewareTests; qui si copre l'altro capo, che e' quello piu' facile da rompere per
/// distrazione: **nessuna activity deve prendere l'IdEnte dal corpo della richiesta**.
///
/// Perche' un guardrail sul sorgente e non un test di comportamento: le 32 activity girano dentro
/// il runtime Durable e vogliono un FunctionContext con l'intero container DI. Montarle tutte per
/// verificare una riga costerebbe sproporzionatamente; e soprattutto il difetto che si teme non e'
/// "l'activity si comporta male", e' "qualcuno scrivera' la riga sbagliata nella prossima activity".
/// Un guardrail sul pattern lo intercetta al build, su tutte, comprese quelle non ancora scritte.
/// </summary>
[TestFixture]
public class IsolamentoMultiTenantTests
{
    /// <summary>
    /// 🔒 L'invariante di sicurezza. Un `new AuthenticationInfo { IdEnte = ... }` puo' prendere quel
    /// valore solo dal contesto autenticato — la Session, o gli Items che il middleware ha popolato
    /// dalla risoluzione della chiave — mai dal payload deserializzato dal corpo della richiesta.
    ///
    /// Se una singola activity scrivesse `IdEnte = req.IdEnte`, un aderente potrebbe leggere i dati
    /// di un altro semplicemente mettendone il GUID nel JSON: la chiave sarebbe la propria, valida,
    /// e la whitelist IP soddisfatta. Nessun test funzionale dell'area se ne accorgerebbe, perche'
    /// la richiesta e' legittima sotto ogni altro aspetto.
    ///
    /// Stato al 18/09/2026: 33 costruzioni di AuthenticationInfo, tutte conformi.
    /// </summary>
    [Test]
    public void NessunaAuthenticationInfo_ShouldPrendereLIdEnteDalCorpoDellaRichiesta()
    {
        var costruzioni = CostruzioniDiAuthenticationInfo();

        Assert.That(costruzioni, Is.Not.Empty,
            "Nessun 'new AuthenticationInfo' trovato: la regex non aggancia piu' nulla e il test "
            + "starebbe passando a vuoto.");

        // Vietato: IdEnte preso da req./request./command. — cioe' dal payload deserializzato.
        // Consentito: req.Session.IdEnte, session.IdEnte, la variabile locale letta dagli Items,
        // e resultQueryApiKey.IdEnte nel middleware (che E' la fonte di verita').
        var dalPayload = costruzioni
            .Where(c => Regex.IsMatch(c.Valore, @"^(req|request|command)\s*[!?]*\s*\.", RegexOptions.IgnoreCase)
                     && !c.Valore.Contains("Session", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.That(dalPayload, Is.Empty,
            "L'IdEnte usato per filtrare i dati viene dal corpo della richiesta, non dalla sessione "
            + "autenticata — un aderente potrebbe leggere i dati di un altro:\n"
            + string.Join("\n", dalPayload.Select(c => $"  {c.File}: IdEnte = {c.Valore}")));
    }

    /// <summary>
    /// Contro-prova della regola sopra: e' l'unica activity che ha un IdEnte ANCHE nel payload —
    /// perche' il payload viene serializzato dentro il JSON del report notifiche — e proprio per
    /// questo lo sovrascrive con quello della Session prima di usarlo.
    ///
    /// Quella riga e' l'unica cosa che impedisce a un IdEnte scritto dal chiamante di finire nel
    /// report. Non la vedrebbe nessun altro test: toglierla non rompe la compilazione, non cambia
    /// nessuna firma, e il flusso continua a funzionare — per l'ente sbagliato.
    /// </summary>
    [Test]
    public void NotificheGetByQuery_ShouldSovrascrivereLIdEnteDelPayloadConQuelloDellaSessione()
    {
        var file = SorgentiDellaFunction()
            .FirstOrDefault(s => Path.GetFileName(s.Percorso).Equals("NotificheGetByQuery.cs", StringComparison.OrdinalIgnoreCase));

        Assert.That(file.Testo, Is.Not.Null, "NotificheGetByQuery.cs non trovato: e' stato rinominato?");
        Assert.That(Regex.IsMatch(file.Testo, @"req\.IdEnte\s*=\s*req\.Session!?\.IdEnte"), Is.True,
            "Manca la sovrascrittura dell'IdEnte dal payload: un valore scritto dal chiamante "
            + "finirebbe nel report delle notifiche.");
    }

    /// <summary>
    /// Il payload delle orchestrazioni deve portare la Session: e' il veicolo con cui l'identita'
    /// autenticata arriva alle activity, che girano fuori dal contesto HTTP e non hanno altro modo
    /// di saperla. Un handler che schedulasse un'orchestrazione senza Session lascerebbe l'activity
    /// a valle senza identita' — e, come si vede nel test sul bypass di SkipSwagger, "senza
    /// identita'" non significa "si ferma".
    /// </summary>
    [Test]
    public void OgniHandlerCheSchedulaUnOrchestrazione_ShouldPassareLaSession()
    {
        var senzaSession = SorgentiDellaFunction()
            .Where(s => s.Percorso.Contains("Handlers", StringComparison.OrdinalIgnoreCase))
            .Where(s => s.Testo.Contains("ScheduleNewOrchestrationInstanceAsync", StringComparison.Ordinal))
            // Solo la PROVENIENZA conta, non la forma: alcuni handler usano l'inizializzatore
            // (`Session = context.GetSession()`), altri la passano al mapper
            // (`request.Map(context.GetSession())`). Entrambe vanno bene; pretendere la prima
            // produceva tre falsi allarmi.
            .Where(s => !s.Testo.Contains("context.GetSession(", StringComparison.Ordinal))
            .Select(s => Path.GetFileName(s.Percorso))
            .ToList();

        Assert.That(senzaSession, Is.Empty,
            "Handler che avviano un'orchestrazione senza metterci dentro la Session: "
            + string.Join(", ", senzaSession));
    }

    // ------------------------------------------------------------------------------------------

    private sealed record Costruzione(string File, string Valore);

    private static List<Costruzione> CostruzioniDiAuthenticationInfo() =>
        SorgentiDellaFunction()
            .SelectMany(s => Regex
                .Matches(s.Testo, @"new\s+AuthenticationInfo\s*\(\s*\)\s*\{(.*?)\}", RegexOptions.Singleline)
                .Select(m => Regex.Match(m.Groups[1].Value, @"IdEnte\s*=\s*([^,\n}]+)"))
                .Where(a => a.Success)
                .Select(a => new Costruzione(Path.GetFileName(s.Percorso), a.Groups[1].Value.Trim())))
            .ToList();

    private static List<(string Percorso, string Testo)> SorgentiDellaFunction()
    {
        var cartella = Path.Combine(RadiceRepository(), "src", "Presentation", "PortaleFatture.BE.Function.API");

        Assert.That(Directory.Exists(cartella), Is.True, $"Cartella non trovata: {cartella}");

        return Directory
            .EnumerateFiles(cartella, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(f => (f, File.ReadAllText(f)))
            .ToList();
    }

    private static string RadiceRepository()
    {
        var cartella = new DirectoryInfo(AppContext.BaseDirectory);
        while (cartella is not null && !File.Exists(Path.Combine(cartella.FullName, "PortaleFatture.BE.Api.sln")))
            cartella = cartella.Parent;

        Assert.That(cartella, Is.Not.Null, "Radice del repository non trovata risalendo da " + AppContext.BaseDirectory);
        return cartella!.FullName;
    }
}
