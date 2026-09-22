using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Accesso alla **SendEmailFunction containerizzata** (profilo `function` di tests/docker-compose.yml),
/// gemello di `TestDb` per il DB: se l'host non risponde i test si **ignorano**, non falliscono.
///
/// Si alza con `.\run-integration.ps1 -Function` oppure
/// `docker compose --profile function up -d --build` da tests/.
/// </summary>
public static class FunctionHost
{
    public const string Default = "http://localhost:8080";

    /// <summary>Blob endpoint di Azurite visto DALL'HOST (dentro la rete compose e' http://azurite:10000).</summary>
    public const string AzuriteConnectionString =
        "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;"
        + "AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;"
        + "BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;";

    /// <summary>Lo stesso nome impostato in `StorageRELBlobContainerName` sul servizio `sendemail`.</summary>
    public const string BlobContainer = "fat-test-public";

    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(30) };

    public static string BaseUrl =>
        ServiceProvider.GetRequiredService<IConfiguration>()["IntegrationTest:FunctionHostUrl"] ?? Default;

    /// <summary>
    /// Sonda che non ha effetti collaterali: l'handler chiamato senza parametri risponde 400 per
    /// costruzione. Verifica quindi due cose in una — che l'host sia su e che la function sia stata
    /// **indicizzata** (un errore di indicizzazione darebbe 404, non 400).
    /// </summary>
    public static void SkipIfUnavailable()
    {
        HttpResponseMessage risposta;
        try
        {
            risposta = Client.GetAsync($"{BaseUrl}/api/CreateRelRigheHandler").GetAwaiter().GetResult();
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            Assert.Ignore(
                "Function host non raggiungibile su " + BaseUrl + ": la suite e' stata saltata, non fallita. "
                + "Avviarla da tests/ con  .\\run-integration.ps1 -Function  (oppure "
                + "docker compose --profile function up -d --build). Dettaglio: " + e.Message);
            return;
        }

        if (risposta.StatusCode == HttpStatusCode.NotFound)
            Assert.Ignore("L'host risponde ma la function non risulta indicizzata: controllare i log del container.");
    }

    public static Task<HttpResponseMessage> Get(string percorsoEQuery) =>
        Client.GetAsync($"{BaseUrl}{percorsoEQuery}");

    /// <summary>
    /// Fa polling sullo stato dell'orchestrazione come lo fa la pipeline del team DATA, e restituisce
    /// il JSON finale (quello con `runtimeStatus` e `output`).
    ///
    /// ⚠️ Lo `statusQueryGetUri` viene ricomposto sul BaseUrl tenendo solo path e query: l'host lo
    /// costruisce a partire da `WEBSITE_HOSTNAME`, e schema/porta possono non coincidere con quelli
    /// da cui il test lo raggiunge. Il `code` in query string e' la system key locale generata
    /// dall'host: si riusa com'e', non e' un segreto da custodire.
    /// </summary>
    public static async Task<JsonElement> AttendiEsito(string statusQueryGetUri, int timeoutSecondi = 90)
    {
        var originale = new Uri(statusQueryGetUri);
        var url = $"{BaseUrl}{originale.PathAndQuery}";
        var scadenza = DateTime.UtcNow.AddSeconds(timeoutSecondi);

        while (true)
        {
            var corpo = await (await Client.GetAsync(url)).Content.ReadAsStringAsync();
            var stato = JsonDocument.Parse(corpo).RootElement.Clone();
            var runtimeStatus = stato.GetProperty("runtimeStatus").GetString();

            if (runtimeStatus is not ("Running" or "Pending"))
                return stato;

            if (DateTime.UtcNow > scadenza)
                Assert.Fail($"Orchestrazione ancora '{runtimeStatus}' dopo {timeoutSecondi}s: {corpo}");

            await Task.Delay(500);
        }
    }

    /// <summary>
    /// Il campo `output` e' una **stringa** che contiene a sua volta JSON: l'activity restituisce
    /// `risposta.Serialize()` e Durable lo tratta come valore, non come oggetto. Chi consuma questa
    /// risposta deve quindi deserializzare due volte — vale la pena che sia esplicito anche qui.
    /// </summary>
    public static JsonElement Output(JsonElement stato)
    {
        var output = stato.GetProperty("output");

        Assert.That(output.ValueKind, Is.EqualTo(JsonValueKind.String),
            "l'output dell'activity e' una stringa JSON, non un oggetto");

        return JsonDocument.Parse(output.GetString()!).RootElement.Clone();
    }
}
