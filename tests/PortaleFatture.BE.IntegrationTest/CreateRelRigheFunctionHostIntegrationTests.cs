using System.Net;
using System.Text.Json;
using Azure.Storage.Blobs;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// `CreateRelRighe` presa dalla porta d'ingresso vera: **richiesta HTTP al webhook, orchestrazione,
/// polling sullo stato, CSV sul blob**. Tutto contro l'immagine Docker della SendEmailFunction, il DB
/// seedato e Azurite — cioe' la stessa sequenza che esegue la pipeline del team DATA, in locale e
/// senza toccare nulla di reale.
///
///     POST /api/CreateRelRigheHandler?anno=..&amp;mese=..&amp;tipologiafattura=..
///       -> 202 { message, instanceId, statusQueryGetUri }
///     GET  {statusQueryGetUri}   (finche' runtimeStatus e' Running/Pending)
///       -> { runtimeStatus: "Completed", output: "&lt;JSON dell'activity&gt;" }
///
/// COSA AGGIUNGE rispetto a `CreateRelRigheAttivitaIntegrationTests`, che esercita la sola activity:
///
///  1. il legame **handler -> orchestrator -> activity**, che e' fatto di STRINGHE
///     (`ScheduleNewOrchestrationInstanceAsync("CreateRelRigheOrchestrator")`,
///     `CallActivityAsync("CreateRelRighe")`): il compilatore non le verifica, e un refuso si
///     manifesterebbe solo alla prima chiamata di un aderente;
///  2. il **salto di DTO** fra i due: l'handler schedula un `CreateRelRigheDataRequest`, l'orchestrator
///     rilegge con `GetInput&lt;EmailRelDataRequest&gt;()`. Regge perche' i nomi coincidono e sono tutti
///     `string?`, ma e' un accoppiamento che nessun test puo' provare se non end-to-end;
///  3. il **contratto di polling** completo, compreso il fatto che `output` e' una stringa JSON;
///  4. che l'**immagine** sia sana: DI, template, indicizzazione delle function.
///
/// Non fa parte del giro ordinario (l'immagine pesa ~2,5 GB): senza il profilo `function` alzato i
/// test si auto-ignorano. Si avvia con  .\run-integration.ps1 -Function
/// </summary>
public class CreateRelRigheFunctionHostIntegrationTests
{
    // Periodo seedato con REL e righe: e' il percorso felice, quello che finisce sul blob.
    private const int AnnoConRel = 2026;
    private const int MeseConRel = 5;
    private const string TipologiaConRel = "PRIMO SALDO";
    private const string Ente = "11111111-1111-1111-1111-111111111111";
    private const string Contratto = "TOKEN-E1";

    // Periodo senza alcuna testata: e' il caso corretto da PF-882.
    private const int AnnoSenzaRel = 2031;
    private const int MeseSenzaRel = 7;

    [SetUp]
    public void Setup() => FunctionHost.SkipIfUnavailable();

    // ---------------------------------------------------------------------------------------------
    // Il contratto HTTP dell'handler
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Parametri incompleti: l'handler rifiuta **prima** di schedulare l'orchestrazione. E' l'unico
    /// punto in cui un input mancante viene intercettato — l'activity, da sola, convertirebbe il
    /// null in 0 e lo scambierebbe per un periodo vuoto (v. la fixture sull'activity).
    /// </summary>
    [Test]
    public async Task Handler_SenzaParametri_ShouldRispondere400()
    {
        var risposta = await FunctionHost.Get("/api/CreateRelRigheHandler");

        Assert.That(risposta.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>
    /// L'avvio e' asincrono: 202 con l'indirizzo su cui fare polling. Sono i tre campi che la
    /// pipeline legge subito dopo la chiamata.
    /// </summary>
    [Test]
    public async Task Handler_ConParametri_ShouldRispondere202ConInstanceIdEStatusQueryGetUri()
    {
        var (stato, corpo) = await Avvia(AnnoSenzaRel, MeseSenzaRel, "SECONDO SALDO");

        Assert.Multiple(() =>
        {
            Assert.That(stato, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(corpo.GetProperty("instanceId").GetString(), Is.Not.Null.And.Not.Empty);
            Assert.That(corpo.GetProperty("statusQueryGetUri").GetString(),
                Does.Contain("/runtime/webhooks/durabletask/instances/"));
            Assert.That(corpo.GetProperty("message").GetString(), Is.Not.Null);
        });
    }

    // ---------------------------------------------------------------------------------------------
    // Il caso di PF-882, visto come lo vede il team DATA
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// LA REGRESSIONE DELLA LAVORAZIONE. Prima del 21/09/2026 un periodo senza REL chiudeva in
    /// **Failed** con lo stack trace nel campo di errore; sul SECONDO SALDO quello stato e' invece
    /// normale per tutti i giorni fra il calcolo della REL e la sua promozione. Ora l'orchestrazione
    /// chiude in **Completed** e l'esito si legge in `output`.
    ///
    /// E' l'unico test che lo dimostra attraverso il webhook, cioe' esattamente come lo osserva chi
    /// fa polling dall'altra parte.
    /// </summary>
    [Test]
    public async Task Orchestrazione_PeriodoSenzaRel_ShouldChiudereCompletedConCountZero()
    {
        var esito = await EseguiEAttendi(AnnoSenzaRel, MeseSenzaRel, "SECONDO SALDO");
        var output = FunctionHost.Output(esito);

        Assert.Multiple(() =>
        {
            Assert.That(esito.GetProperty("runtimeStatus").GetString(), Is.EqualTo("Completed"),
                "un periodo senza REL non e' un guasto: non deve piu' chiudere in Failed");
            Assert.That(output.GetProperty("Count").GetInt32(), Is.Zero);
            Assert.That(output.GetProperty("DbConnection").GetBoolean(), Is.True);
            Assert.That(output.GetProperty("Error").GetString(), Does.Contain("Non ci sono rel"));
        });
    }

    // ---------------------------------------------------------------------------------------------
    // Il percorso felice, fino al file
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Il pezzo che nessun altro test puo' coprire: `AddDocument`, cioe' il **nome del blob e il suo
    /// contenuto**. Gira contro Azurite grazie a `StorageRELBlobEndpoint` (senza quell'override il
    /// codice comporrebbe l'endpoint pubblico dell'account e morirebbe sul DNS).
    ///
    /// Il path atteso e' `{anno}/{mese}/{tipologia}/{idEnte}/{idContratto}/Rel_Report di dettaglio_…csv`.
    /// Si cerca per prefisso invece che per nome esatto perche' il nome del file include la ragione
    /// sociale dell'ente, che e' un dato del seed e non una costante del test.
    /// </summary>
    [Test]
    public async Task Orchestrazione_PeriodoConRel_ShouldScrivereIlCsvSuAzurite()
    {
        var container = await PreparaContainerBlob();
        var prefisso = $"{AnnoConRel}/{MeseConRel}/{TipologiaConRel}/";

        var esito = await EseguiEAttendi(AnnoConRel, MeseConRel, TipologiaConRel);
        var output = FunctionHost.Output(esito);

        Assert.That(esito.GetProperty("runtimeStatus").GetString(), Is.EqualTo("Completed"));
        Assert.That(output.GetProperty("Count").GetInt32(), Is.GreaterThan(0),
            "il periodo e' seedato con almeno una testata REL");

        var blob = container.GetBlobs(prefix: prefisso).FirstOrDefault();
        Assert.That(blob, Is.Not.Null, $"nessun file sotto '{prefisso}': l'upload non e' avvenuto");

        // Il path completo e' un contratto, non un dettaglio: e' lo stesso che le rotte
        // api/rel/.../righe/{id} ricompongono per generare il SAS token. Se cambia qui, quelle
        // rispondono 404 senza spiegazione (v. docs/pipeline-dati-send.md).
        Assert.Multiple(() =>
        {
            Assert.That(blob!.Name, Does.StartWith($"{prefisso}{Ente}/{Contratto}/"),
                "alberatura attesa: anno/mese/tipologia/idEnte/idContratto/");
            Assert.That(blob.Name, Does.Contain("/Rel_Report di dettaglio_"));
            Assert.That(blob.Name, Does.EndWith($"_{MeseConRel}_{AnnoConRel}.csv"),
                "il nome del file chiude con _{mese}_{anno}.csv");
        });

        var contenuto = (await container.GetBlobClient(blob.Name).DownloadContentAsync())
            .Value.Content.ToString();

        Assert.Multiple(() =>
        {
            Assert.That(contenuto, Does.Contain("contract_id;"), "intestazione e delimitatore ';'");
            Assert.That(contenuto, Does.Contain("REL-PS-1"), "le righe REL seedate per il periodo");
        });
    }

    // ---------------------------------------------------------------------------------------------
    // Avversariale
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Un guasto vero deve restare visibile come tale. Un `anno` non numerico fa fallire l'activity,
    /// l'orchestrator riavvolge la `TaskFailedException` in `InvalidOperationException` e
    /// l'orchestrazione chiude in **Failed**: e' il segnale su cui la pipeline puo' ancora allarmarsi,
    /// e che il ramo "nessuna REL" non produce piu'.
    /// </summary>
    [Test]
    public async Task Orchestrazione_AnnoNonNumerico_ShouldChiudereFailed()
    {
        var esito = await EseguiEAttendi("duemilaventisei", "6", "SECONDO SALDO");

        Assert.Multiple(() =>
        {
            Assert.That(esito.GetProperty("runtimeStatus").GetString(), Is.EqualTo("Failed"));
            Assert.That(esito.GetProperty("output").GetString(), Does.Contain("CreateRelRighe"),
                "il messaggio di errore nomina l'activity che ha fallito");
        });
    }

    // ---------------------------------------------------------------------------------------------

    private static Task<(HttpStatusCode Stato, JsonElement Corpo)> Avvia(int anno, int mese, string tipologia) =>
        Avvia(anno.ToString(), mese.ToString(), tipologia);

    private static async Task<(HttpStatusCode Stato, JsonElement Corpo)> Avvia(string anno, string mese, string tipologia)
    {
        var risposta = await FunctionHost.Get(
            $"/api/CreateRelRigheHandler?anno={anno}&mese={mese}&tipologiafattura={Uri.EscapeDataString(tipologia)}");

        var corpo = await risposta.Content.ReadAsStringAsync();
        return (risposta.StatusCode, JsonDocument.Parse(corpo).RootElement.Clone());
    }

    private static Task<JsonElement> EseguiEAttendi(int anno, int mese, string tipologia) =>
        EseguiEAttendi(anno.ToString(), mese.ToString(), tipologia);

    private static async Task<JsonElement> EseguiEAttendi(string anno, string mese, string tipologia)
    {
        var (stato, corpo) = await Avvia(anno, mese, tipologia);

        Assert.That(stato, Is.EqualTo(HttpStatusCode.Accepted), "avvio dell'orchestrazione non accettato");

        return await FunctionHost.AttendiEsito(corpo.GetProperty("statusQueryGetUri").GetString()!);
    }

    /// <summary>
    /// Il container dei blob non viene creato dalla function (su Azure esiste gia'): lo crea il test.
    /// Si ripulisce il prefisso del periodo per non asserire su un file lasciato da un giro
    /// precedente.
    /// </summary>
    private static async Task<BlobContainerClient> PreparaContainerBlob()
    {
        var container = new BlobServiceClient(FunctionHost.AzuriteConnectionString)
            .GetBlobContainerClient(FunctionHost.BlobContainer);

        await container.CreateIfNotExistsAsync();

        foreach (var vecchio in container.GetBlobs(prefix: $"{AnnoConRel}/{MeseConRel}/{TipologiaConRel}/"))
            await container.DeleteBlobIfExistsAsync(vecchio.Name);

        return container;
    }
}
