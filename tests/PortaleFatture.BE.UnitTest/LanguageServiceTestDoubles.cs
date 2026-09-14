using System.Net;
using Azure.AI.TextAnalytics;
using Azure.Core;
using Azure.Core.Pipeline;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Credenziale che restituisce un token fisso senza toccare Entra ID. Serve a esercitare il
/// <c>LanguageService</c> **vero** senza dipendere dall'identita' di chi esegue: con
/// <c>DefaultAzureCredential</c> l'esito cambierebbe fra una macchina con <c>az login</c> e una senza,
/// e la catena di credenziali puo' impiegare decine di secondi prima di arrendersi.
///
/// Con un'eccezione nel costruttore simula invece l'identita' **non disponibile** (managed identity
/// assente, nessun login locale): e' cosi' che fallisce in produzione un App Service senza identita'.
/// </summary>
internal sealed class CredenzialeFinta(Exception? errore = null) : TokenCredential
{
    public const string Token = "token-finto-per-il-test";

    public List<TokenRequestContext> Richieste { get; } = [];

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        lock (Richieste)
            Richieste.Add(requestContext);

        if (errore is not null)
            throw errore;

        return new AccessToken(Token, DateTimeOffset.UtcNow.AddHours(1));
    }

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        => new(GetToken(requestContext, cancellationToken));
}

/// <summary>Cio' che il trasporto ha visto uscire, catturato subito (la richiesta viene poi rilasciata).</summary>
internal sealed record RichiestaRegistrata(Uri? Uri, string? Authorization, bool HaChiaveApim);

/// <summary>
/// Trasporto HTTP finto da agganciare al client dell'SDK: registra le richieste che **partono davvero**
/// e risponde come deciso dal test. Sta sotto l'intera pipeline di Azure.Core, quindi autenticazione,
/// retry e cancellazione restano quelli veri.
/// </summary>
internal sealed class TrasportoFinto : HttpMessageHandler
{
    private readonly Func<CancellationToken, Task<HttpResponseMessage>> _risposta;

    public List<RichiestaRegistrata> Richieste { get; } = [];

    private TrasportoFinto(Func<CancellationToken, Task<HttpResponseMessage>> risposta) => _risposta = risposta;

    public static TrasportoFinto CheRisponde(HttpStatusCode stato) =>
        new(_ => Task.FromResult(new HttpResponseMessage(stato) { Content = new StringContent("{}") }));

    /// <summary>
    /// Accetta la richiesta e **non risponde mai**: il servizio lento, cioe' il caso per cui esiste il
    /// timeout della sintesi. Un endpoint irraggiungibile non servirebbe: fallirebbe subito in
    /// connessione (502) senza esercitare il ramo del 504.
    /// </summary>
    public static TrasportoFinto Muto() => new(async ct =>
    {
        await Task.Delay(Timeout.Infinite, ct);
        throw new InvalidOperationException("Irraggiungibile: il ritardo termina solo per cancellazione.");
    });

    /// <summary>Opzioni del client con questo trasporto e senza retry, per esiti immediati e contabili.</summary>
    public TextAnalyticsClientOptions Opzioni()
    {
        var opzioni = new TextAnalyticsClientOptions { Transport = new HttpClientTransport(new HttpClient(this)) };
        opzioni.Retry.MaxRetries = 0;
        return opzioni;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var authorization = request.Headers.TryGetValues("Authorization", out var valori)
            ? string.Join(",", valori)
            : null;

        lock (Richieste)
            Richieste.Add(new RichiestaRegistrata(
                request.RequestUri, authorization, request.Headers.Contains("Ocp-Apim-Subscription-Key")));

        return _risposta(cancellationToken);
    }
}
