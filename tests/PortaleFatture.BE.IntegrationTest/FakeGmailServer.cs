using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MimeKit;
using PortaleFatture.BE.Infrastructure.Gateway.Email;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Finto Google per i sender che usano l'API Gmail (<see cref="PspEmailSender"/>, <see cref="AlertSender"/>):
/// un server HTTP locale che risponde alle tre chiamate che i sender fanno — verifica del token,
/// rinnovo del token, invio del messaggio — piu' la ricerca nella posta inviata.
///
/// E' l'equivalente, per Gmail, del DB seedato: le risposte sono decise dal test (token valido o
/// scaduto, rinnovo riuscito o no, invio accettato o rifiutato) e tutto cio' che i sender spediscono
/// resta registrato, cosi' il test puo' rileggere il messaggio come lo riceverebbe Google.
/// Nessuna chiamata esce dalla macchina.
/// </summary>
public sealed class FakeGmailServer : IDisposable
{
    public const string TokenValido = "token-valido";
    public const string TokenRinnovato = "token-rinnovato";

    private readonly HttpListener _listener = new();
    private readonly Task _ciclo;

    public string BaseUrl { get; }

    /// <summary>Token che la verifica considera validi. Di partenza: quello valido e quello rinnovato.</summary>
    public HashSet<string> TokenAccettati { get; } = [TokenValido, TokenRinnovato];

    /// <summary>Se false, il rinnovo risponde senza access_token (credenziali revocate).</summary>
    public bool RinnovoRiesce { get; set; } = true;

    /// <summary>Codice con cui risponde l'invio. 200 = accettato.</summary>
    public int EsitoInvio { get; set; } = 200;

    /// <summary>Identificativi (Message-Id) che la ricerca nella posta inviata deve trovare.</summary>
    public HashSet<string> InviatiNoti { get; } = [];

    public ConcurrentQueue<Chiamata> Chiamate { get; } = new();

    public sealed record Chiamata(string Metodo, string Percorso, string Query, string? Authorization, string Corpo);

    public FakeGmailServer()
    {
        // Porta libera scelta dal sistema, poi riusata: nessun conflitto con i container dei test.
        var sonda = new TcpListener(IPAddress.Loopback, 0);
        sonda.Start();
        var porta = ((IPEndPoint)sonda.LocalEndpoint).Port;
        sonda.Stop();

        BaseUrl = $"http://127.0.0.1:{porta}/";
        _listener.Prefixes.Add(BaseUrl);
        _listener.Start();
        _ciclo = Task.Run(Servi);
    }

    public GmailEndpoints Endpoints => new(
        ApiBaseUri: BaseUrl,
        TokenInfoUrl: BaseUrl + "tokeninfo",
        TokenUrl: BaseUrl + "token");

    public IEnumerable<Chiamata> Invii => Chiamate.Where(c => c.Percorso.EndsWith("/messages/send", StringComparison.Ordinal));
    public IEnumerable<Chiamata> Rinnovi => Chiamate.Where(c => c.Percorso == "/token");

    /// <summary>Il messaggio dell'ultimo invio, decodificato dal campo "raw" come farebbe Gmail.</summary>
    public MimeMessage UltimoMessaggio()
    {
        var raw = JsonDocument.Parse(Invii.Last().Corpo).RootElement.GetProperty("raw").GetString()!;
        var base64 = raw.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
        using var stream = new MemoryStream(Convert.FromBase64String(base64));
        return MimeMessage.Load(stream);
    }

    private async Task Servi()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext contesto;
            try { contesto = await _listener.GetContextAsync(); }
            catch (Exception e) when (e is HttpListenerException or ObjectDisposedException) { return; }

            try { await Rispondi(contesto); }
            catch { /* una risposta fallita non deve fermare il server: il test vedra' l'errore lato client */ }
        }
    }

    private async Task Rispondi(HttpListenerContext contesto)
    {
        var richiesta = contesto.Request;
        string corpo;
        // La libreria Google comprime il corpo delle richieste (Content-Encoding: gzip), come con Google vero.
        Stream ingresso = string.Equals(richiesta.Headers["Content-Encoding"], "gzip", StringComparison.OrdinalIgnoreCase)
            ? new System.IO.Compression.GZipStream(richiesta.InputStream, System.IO.Compression.CompressionMode.Decompress)
            : richiesta.InputStream;
        using (var lettore = new StreamReader(ingresso, Encoding.UTF8))
            corpo = await lettore.ReadToEndAsync();

        var percorso = richiesta.Url!.AbsolutePath;
        Chiamate.Enqueue(new Chiamata(richiesta.HttpMethod, percorso, richiesta.Url.Query, richiesta.Headers["Authorization"], corpo));

        var (stato, json) = (percorso, richiesta.HttpMethod) switch
        {
            ("/tokeninfo", "GET") => TokenAccettati.Contains(richiesta.QueryString["access_token"] ?? string.Empty)
                ? (200, """{ "expires_in": 3000 }""")
                : (400, """{ "error": "invalid_token" }"""),

            ("/token", "POST") => RinnovoRiesce
                ? (200, $$"""{ "access_token": "{{TokenRinnovato}}", "expires_in": 3599, "token_type": "Bearer" }""")
                : (400, """{ "error": "invalid_grant" }"""),

            (var p, "POST") when p.EndsWith("/messages/send", StringComparison.Ordinal) => EsitoInvio == 200
                ? (200, """{ "id": "fake-msg-1", "threadId": "fake-thread-1", "labelIds": ["SENT"] }""")
                : (EsitoInvio, $$"""{ "error": { "code": {{EsitoInvio}}, "message": "Invio rifiutato dal finto Gmail", "status": "FAILED" } }"""),

            (var p, "GET") when p.EndsWith("/messages", StringComparison.Ordinal) =>
                InviatiNoti.Any(id => (richiesta.QueryString["q"] ?? string.Empty) == $"rfc822msgid:{id}")
                    ? (200, """{ "messages": [ { "id": "fake-msg-1", "threadId": "fake-thread-1" } ], "resultSizeEstimate": 1 }""")
                    : (200, """{ "resultSizeEstimate": 0 }"""),

            _ => (404, """{ "error": "rotta non prevista dal finto Gmail" }""")
        };

        var byteRisposta = Encoding.UTF8.GetBytes(json);
        contesto.Response.StatusCode = stato;
        contesto.Response.ContentType = "application/json; charset=utf-8";
        contesto.Response.ContentLength64 = byteRisposta.Length;
        await contesto.Response.OutputStream.WriteAsync(byteRisposta);
        contesto.Response.Close();
    }

    public void Dispose()
    {
        if (_listener.IsListening)
            _listener.Stop();
        _listener.Close();
        try { _ciclo.Wait(TimeSpan.FromSeconds(2)); } catch { /* chiusura */ }
    }
}
