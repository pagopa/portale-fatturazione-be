using System.Net;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// `CreateRelSospese` presa dal webhook, come la invoca la pipeline del team DATA: richiesta HTTP,
/// orchestrazione, polling. Gira sull'immagine Docker della SendEmailFunction (profilo `function`),
/// quindi si avvia con  .\run-integration.ps1 -Function  e senza quello si auto-ignora.
///
/// Perche' non basta la fixture sull'activity: in questa catena **tre DTO diversi** si passano lo
/// stesso payload — l'handler schedula un `CreateRelRigheDataRequest` (si', quello della gemella),
/// l'orchestrator rilegge con `GetInput&lt;EmailRelDataRequest&gt;()` e l'activity riceve un
/// `CreateRelSospeseDataRequest`. Regge solo perche' i tre tipi hanno gli stessi nomi di proprieta' e
/// sono tutti `string?`: cambiarne uno in uno solo dei tre romperebbe in produzione **senza rompere
/// la build**. Lo stesso vale per i due nomi passati come stringa
/// (`"CreateRelSospeseOrchestrator"`, `"CreateRelSospese"`), che il compilatore non verifica.
/// </summary>
public class CreateRelSospeseFunctionHostIntegrationTests
{
    // Periodo senza REL sospese: e' il caso corretto il 22/09/2026.
    private const int AnnoSenzaRel = 2031;
    private const int MeseSenzaRel = 8;

    // Periodo seedato CON una REL sospesa: e' il percorso felice, quello che finisce sul blob.
    private const int AnnoConRel = 2030;
    private const int MeseConRel = 4;
    private const string TipologiaConRel = "SECONDO SALDO";
    private const string Ente = "11111111-1111-1111-1111-111111111111";
    private const string Contratto = "TOKEN-E1";

    [SetUp]
    public void Setup() => FunctionHost.SkipIfUnavailable();

    /// <summary>
    /// Parametri incompleti: l'handler rifiuta prima di schedulare l'orchestrazione. E' l'unico punto
    /// in cui un input mancante viene intercettato — l'activity, da sola, convertirebbe il null in 0
    /// e lo scambierebbe per un periodo vuoto.
    /// </summary>
    [Test]
    public async Task Handler_SenzaParametri_ShouldRispondere400()
    {
        var risposta = await FunctionHost.Get("/api/CreateRelSospeseHandler");

        Assert.That(risposta.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>
    /// LA REGRESSIONE, vista da dove la guarda chi fa polling: un periodo senza REL sospese chiude in
    /// **Completed** con `Count = 0`, non piu' in `Failed` con stack trace.
    /// </summary>
    [Test]
    public async Task Orchestrazione_PeriodoSenzaRelSospese_ShouldChiudereCompletedConCountZero()
    {
        // La query delle testate sospese joina pfd.RiepilogoFatturazione_NPF, che nel seed non c'e'
        // ancora: senza, l'activity fallisce sul DB e l'orchestrazione chiude in Failed — cioe' il
        // test sarebbe rosso per una lacuna del seed e non per il comportamento in esame.
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "pfd.RiepilogoFatturazione_NPF");

        var esito = await EseguiEAttendi(AnnoSenzaRel.ToString(), MeseSenzaRel.ToString(), "SECONDO SALDO");
        var output = FunctionHost.Output(esito);

        Assert.Multiple(() =>
        {
            Assert.That(esito.GetProperty("runtimeStatus").GetString(), Is.EqualTo("Completed"),
                "un periodo senza REL sospese non e' un guasto: non deve chiudere in Failed");
            Assert.That(output.GetProperty("Count").GetInt32(), Is.Zero);
            Assert.That(output.GetProperty("DbConnection").GetBoolean(), Is.True);
            Assert.That(output.GetProperty("Error").GetString(), Does.Contain("Non ci sono rel sospese"));
        });
    }

    /// <summary>
    /// IL PERCORSO FELICE, fino al file. Sbloccato il 22/09/2026 da due cose: le REL sospese nel seed
    /// (periodo 2030/4) e l'override `StorageRELBlobEndpoint` portato anche in
    /// `CreateRelSospese.AddDocument`, che prima componeva l'endpoint a mano e quindi non era
    /// dirottabile sull'emulatore.
    ///
    /// Il nome del file e' l'unica cosa che distingue i due report sullo storage — la gemella scrive
    /// `Rel_Report di dettaglio_…`, questa `Rel_Report di dettaglio_**Sospese**_…` — e finisce nella
    /// stessa alberatura `anno/mese/tipologia/idEnte/idContratto/`, cioe' quella che le rotte
    /// `.../rel/.../righe` ricompongono per il SAS token.
    /// </summary>
    [Test]
    public async Task Orchestrazione_PeriodoConRelSospese_ShouldScrivereIlCsvSuAzurite()
    {
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "pfd.tmpRelRighe");

        var container = await PreparaContainerBlob();
        var prefisso = $"{AnnoConRel}/{MeseConRel}/{TipologiaConRel}/";

        var esito = await EseguiEAttendi(AnnoConRel.ToString(), MeseConRel.ToString(), TipologiaConRel);
        var output = FunctionHost.Output(esito);

        Assert.That(esito.GetProperty("runtimeStatus").GetString(), Is.EqualTo("Completed"));
        Assert.That(output.GetProperty("Count").GetInt32(), Is.GreaterThan(0),
            "il periodo 2030/4 ha una testata sospesa nel seed");

        var blob = container.GetBlobs(prefix: prefisso).FirstOrDefault();
        Assert.That(blob, Is.Not.Null, $"nessun file sotto '{prefisso}': l'upload non e' avvenuto");

        Assert.Multiple(() =>
        {
            Assert.That(blob!.Name, Does.StartWith($"{prefisso}{Ente}/{Contratto}/"));
            Assert.That(blob.Name, Does.Contain("/Rel_Report di dettaglio_Sospese_"),
                "e' il solo elemento che distingue questo report da quello del flusso ordinario");
            Assert.That(blob.Name, Does.EndWith($"_{MeseConRel}_{AnnoConRel}.csv"));
        });

        var contenuto = (await container.GetBlobClient(blob!.Name).DownloadContentAsync())
            .Value.Content.ToString();

        Assert.Multiple(() =>
        {
            Assert.That(contenuto, Does.Contain("contract_id;"), "intestazione e delimitatore ';'");
            Assert.That(contenuto, Does.Contain("SOSP-SD-APR"), "la riga sospesa seedata per il periodo");
            Assert.That(contenuto, Does.Not.Contain("SOSP-SD-MAG"),
                "il SECONDO SALDO sospeso filtra per anno/mese: maggio non deve finire nel file di aprile");
        });
    }

    /// <summary>
    /// L'altro lato della distinzione: un guasto vero continua a chiudere in **Failed**, ed e' il
    /// segnale su cui la pipeline puo' ancora allarmarsi. Il messaggio nomina l'activity, perche'
    /// l'orchestrator riavvolge la `TaskFailedException` in `InvalidOperationException`.
    /// </summary>
    [Test]
    public async Task Orchestrazione_AnnoNonNumerico_ShouldChiudereFailed()
    {
        var esito = await EseguiEAttendi("duemilaventisei", "6", "SECONDO SALDO");

        Assert.Multiple(() =>
        {
            Assert.That(esito.GetProperty("runtimeStatus").GetString(), Is.EqualTo("Failed"));
            Assert.That(esito.GetProperty("output").GetString(), Does.Contain("CreateRelSospese"),
                "il messaggio di errore nomina l'activity che ha fallito");
        });
    }

    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Il container dei blob non lo crea la function (su Azure esiste gia'): lo crea il test, che
    /// ripulisce anche il prefisso del periodo per non asserire su un file lasciato da un giro
    /// precedente — l'upload usa `overwrite: true`, quindi senza pulizia un file vecchio sopravvivrebbe
    /// anche a una regressione.
    /// </summary>
    private static async Task<Azure.Storage.Blobs.BlobContainerClient> PreparaContainerBlob()
    {
        var container = new Azure.Storage.Blobs.BlobServiceClient(FunctionHost.AzuriteConnectionString)
            .GetBlobContainerClient(FunctionHost.BlobContainer);

        await container.CreateIfNotExistsAsync();

        foreach (var vecchio in container.GetBlobs(prefix: $"{AnnoConRel}/{MeseConRel}/{TipologiaConRel}/"))
            await container.DeleteBlobIfExistsAsync(vecchio.Name);

        return container;
    }

    private static async Task<System.Text.Json.JsonElement> EseguiEAttendi(string anno, string mese, string tipologia)
    {
        var risposta = await FunctionHost.Get(
            $"/api/CreateRelSospeseHandler?anno={anno}&mese={mese}&tipologiafattura={Uri.EscapeDataString(tipologia)}");

        Assert.That(risposta.StatusCode, Is.EqualTo(HttpStatusCode.Accepted),
            "avvio dell'orchestrazione non accettato");

        var corpo = System.Text.Json.JsonDocument.Parse(await risposta.Content.ReadAsStringAsync()).RootElement;

        return await FunctionHost.AttendiEsito(corpo.GetProperty("statusQueryGetUri").GetString()!);
    }
}
