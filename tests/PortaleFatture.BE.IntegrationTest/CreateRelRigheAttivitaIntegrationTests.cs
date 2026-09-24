using System.Text.Json;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture_BE_SendEmailFunction;
using PortaleFatture_BE_SendEmailFunction.Models;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// L'activity `CreateRelRighe` ESEGUITA DAVVERO, non una sua ricostruzione: la classe costruisce da
/// sola il proprio container DI leggendo quattro variabili d'ambiente, quindi basta valorizzarle
/// verso il DB seedato per esercitarla come la esegue Azure. E' il motivo per cui questa fixture vive
/// qui e non fra gli unit test — e per cui il progetto integration ha ora una ProjectReference verso
/// la SendEmailFunction, che prima non aveva.
///
/// Perimetro: il ramo "nessuna REL per il periodo" e i rami di errore. Il percorso felice resta fuori
/// di proposito, perche' finirebbe in `AddDocument`, cioe' in un upload su blob storage reale.
/// Nessuno di questi test scrive nulla, ne' sul DB ne' sullo storage.
///
/// Cosa protegge, ed e' il punto della lavorazione PF-882: fino al 21/09/2026 il periodo vuoto
/// sollevava una `DomainException` e l'orchestrazione chiudeva in `Failed`; sul SECONDO SALDO quello
/// stato e' invece NORMALE per tutti i giorni fra il calcolo della REL e la sua promozione (v.
/// `docs/pipeline-dati-send.md`). Ora quel ramo restituisce la risposta, e la pipeline legge l'esito
/// nel campo `output` della risposta di polling sullo stato dell'orchestrazione
/// (`/runtime/webhooks/durabletask/instances/{instanceId}`).
///
/// La forma di quella stringa e' fissata da `RispostaRelRigheContrattoTests` (unit); qui si verifica
/// QUANDO viene prodotta.
/// </summary>
public class CreateRelRigheAttivitaIntegrationTests
{
    // Periodo che nel seed non ha alcuna RelTestata: e' cio' che si vuole esercitare. Volutamente
    // lontano dai periodi usati dalle altre fixture e da quelli dei calendari.
    private const int AnnoSenzaRel = 2031;
    private const int MeseSenzaRel = 7;
    private const string Tipologia = "SECONDO SALDO";

    private static readonly string[] Variabili =
        ["CONNECTION_STRING", "StorageRELAccountName", "StorageRELAccountKey", "StorageRELBlobContainerName"];

    private readonly Dictionary<string, string?> _precedenti = [];
    private string _cn = null!;

    [SetUp]
    public void Setup()
    {
        _cn = LocalTestDb.ConnectionString;

        foreach (var nome in Variabili)
            _precedenti[nome] = Environment.GetEnvironmentVariable(nome, EnvironmentVariableTarget.Process);

        // Le tre variabili di storage devono solo essere non vuote: superano il controllo di
        // configurazione ma non vengono mai usate, perche' AddDocument sta nel ramo che qui non si
        // percorre. Nessuna credenziale reale, quindi, nemmeno per errore.
        Set("CONNECTION_STRING", _cn);
        Set("StorageRELAccountName", "test-non-usato");
        Set("StorageRELAccountKey", "test-non-usato");
        Set("StorageRELBlobContainerName", "test-non-usato");
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var coppia in _precedenti)
            Environment.SetEnvironmentVariable(coppia.Key, coppia.Value, EnvironmentVariableTarget.Process);
    }

    // ---------------------------------------------------------------------------------------------
    // Il ramo corretto da PF-882
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Il caso che in produzione si presentava come `Failed` con stack trace. Deve chiudere senza
    /// sollevare e restituire un esito leggibile: `Count = 0`, `DbConnection = true`, `Error` con il
    /// motivo. E' la regressione principale della lavorazione.
    /// </summary>
    [Test]
    public async Task NessunaRel_ShouldRestituireCountZeroSenzaSollevare()
    {
        TestDb.SkipIfUnavailable(_cn);

        var json = await Esegui(AnnoSenzaRel.ToString(), MeseSenzaRel.ToString(), Tipologia);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("Count").GetInt32(), Is.Zero);
            Assert.That(root.GetProperty("DbConnection").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("Error").GetString(), Does.Contain("Non ci sono rel"));
            Assert.That(root.GetProperty("Anno").GetInt32(), Is.EqualTo(AnnoSenzaRel));
            Assert.That(root.GetProperty("Mese").GetInt32(), Is.EqualTo(MeseSenzaRel));
            Assert.That(root.GetProperty("TipologiaFattura").GetString(), Is.EqualTo(Tipologia));
        });
    }

    /// <summary>
    /// Stessa cosa per una tipologia che non esiste affatto: non c'e' whitelist, il valore finisce
    /// nella WHERE e semplicemente non seleziona nulla. Serve a dire che il ramo "vuoto" non e'
    /// legato al SECONDO SALDO ma a qualunque combinazione senza testate.
    /// </summary>
    [Test]
    public async Task TipologiaInesistente_ShouldRestituireCountZeroSenzaSollevare()
    {
        TestDb.SkipIfUnavailable(_cn);

        var json = await Esegui("2026", "5", "TIPOLOGIA CHE NON ESISTE");

        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.GetProperty("Count").GetInt32(), Is.Zero);
    }

    // ---------------------------------------------------------------------------------------------
    // Avversariali: quello che NON deve essere confuso con il periodo vuoto
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Un `anno` non numerico e' un guasto vero e deve restare tale: `Convert.ToInt32` solleva
    /// `FormatException`, il catch la riavvolge in `DomainException` e l'orchestrazione fallisce.
    ///
    /// Il test esiste per separare due cose che in produzione sono state confuse: il messaggio del
    /// periodo vuoto contiene l'apostrofo escapato (`l'anno`) e qualcuno puo' leggerlo come un
    /// problema di decodifica del parametro `Anno`. Qui si vede che un problema di decodifica VERO ha
    /// tutt'altro aspetto, e non passa mai dal ramo "nessuna rel".
    /// </summary>
    [Test]
    public void AnnoNonNumerico_ShouldSollevareDomainExceptionConMessaggioDiFormato()
    {
        var ex = Assert.ThrowsAsync<DomainException>(async () =>
            await Esegui("duemilaventisei", "6", Tipologia));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Does.Not.Contain("Non ci sono rel"),
                "un anno illeggibile non deve somigliare a un periodo vuoto");
            Assert.That(ex.InnerException, Is.TypeOf<FormatException>());
        });
    }

    /// <summary>
    /// CARATTERIZZAZIONE. `Convert.ToInt32(null)` non solleva: restituisce 0. Un `anno` assente
    /// diventa quindi l'anno zero, la query non trova nulla e si esce dal ramo "nessuna rel" — cioe'
    /// un input mancante e' indistinguibile da un periodo legittimamente vuoto.
    ///
    /// Oggi non si manifesta perche' l'handler HTTP rifiuta con 400 le query string incomplete, ma la
    /// normalizzazione silenziosa vive nell'activity e sopravviverebbe a un secondo chiamante.
    /// </summary>
    [Test]
    public async Task AnnoEMeseAssenti_ShouldDiventareZeroEFinireNelRamoVuoto_Caratterizzazione()
    {
        TestDb.SkipIfUnavailable(_cn);

        var json = await Esegui(null, null, Tipologia);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("Anno").GetInt32(), Is.Zero, "null convertito in 0, senza errore");
            Assert.That(root.GetProperty("Mese").GetInt32(), Is.Zero);
            Assert.That(root.GetProperty("Count").GetInt32(), Is.Zero);
        });
    }

    /// <summary>
    /// CARATTERIZZAZIONE, e una piccola trappola per chi legge i log. Senza configurazione l'activity
    /// solleva prima di toccare qualunque cosa — corretto — ma il messaggio dell'eccezione e' la
    /// stringa secca "Wrong configuration", NON il JSON della risposta: un consumatore che provasse a
    /// deserializzare il messaggio di errore fallirebbe. Il `risposta.DbConnection = false` che il
    /// catch imposta, inoltre, non arriva da nessuna parte, perche' si rilancia con `ex.Message`.
    ///
    /// Non richiede il container: e' l'unico test della fixture che gira sempre.
    /// </summary>
    [Test]
    public void ConfigurazioneAssente_ShouldSollevareDomainExceptionNonSerializzata_Caratterizzazione()
    {
        Environment.SetEnvironmentVariable("CONNECTION_STRING", null, EnvironmentVariableTarget.Process);

        var ex = Assert.ThrowsAsync<DomainException>(async () => await Esegui("2026", "6", Tipologia));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Is.EqualTo("Wrong configuration"));
            Assert.That(() => JsonDocument.Parse(ex.Message), Throws.InstanceOf<JsonException>(),
                "il messaggio di errore non e' JSON: solo il ramo 'nessuna rel' lo e'");
        });
    }

    // ---------------------------------------------------------------------------------------------

    private static async Task<string> Esegui(string? anno, string? mese, string? tipologia)
    {
        var activity = new CreateRelRighe(LoggerFactory.Create(_ => { }));

        return await activity.RunAsync(new CreateRelRigheDataRequest
        {
            Anno = anno,
            Mese = mese,
            TipologiaFattura = tipologia
        });
    }

    private static void Set(string nome, string? valore) =>
        Environment.SetEnvironmentVariable(nome, valore, EnvironmentVariableTarget.Process);
}
