using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using PortaleFatture.BE.Infrastructure.Gateway.Email;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// EmailSender contro un server SMTP FINTO in container (smtp4dev, profilo "email" del docker-compose),
/// al posto dei provider reali. Dal 06/11/2026 e' il controllo automatico dell'invio: gira in un giro
/// normale di test e si salta da solo se il container non e' acceso.
///
/// Cosa prova che i test su SMTP reale (EmailSenderTests, [Explicit]) non possono: che il messaggio
/// ARRIVATO abbia mittente, destinatario, oggetto e corpo giusti. Lo legge dall'API REST di smtp4dev.
/// Cosa NON prova: il comportamento del provider vero (Aruba PEC, ricevute, S/MIME). Per quello restano
/// i due test [Explicit] su SMTP reale, da lanciare a mano, per esempio dopo un upgrade di MailKit.
///
/// La connessione e' la stessa di produzione: TLS implicito (SslOnConnect), autenticazione e
/// validazione standard del certificato. Nessun ramo di codice e' stato aperto per i test: il server
/// presenta il certificato di sviluppo "localhost" di .NET, che la macchina considera affidabile.
///
/// Avvio (da tests/):
///     dotnet dev-certs https -ep .certs/smtp4dev-localhost.pfx -p smtp4dev-locale
///     docker compose --profile email up -d
/// </summary>
[Category("Email")]
[NonParallelizable] // la casella di smtp4dev e' condivisa: i test la svuotano
public class EmailSenderContainerTests
{
    private const string Host = "localhost";
    private const int PortaSmtp = 2465;
    private static readonly Uri Api = new("http://localhost:5080/");
    private const string Mittente = "noreply@portalefatture.invalid";
    private const string Destinatario = "ente.test@portalefatture.invalid";

    private HttpClient _api = null!;

    [OneTimeSetUp]
    public void VerificaContainer()
    {
        _api = new HttpClient { BaseAddress = Api, Timeout = TimeSpan.FromSeconds(10) };
        try
        {
            using var tcp = new TcpClient();
            if (!tcp.ConnectAsync(Host, PortaSmtp).Wait(TimeSpan.FromSeconds(3)))
                throw new TimeoutException("SMTP non risponde");
            _api.GetAsync("api/messages").GetAwaiter().GetResult().EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Assert.Ignore("smtp4dev non raggiungibile: test saltato, non fallito. Da tests/: "
                + "dotnet dev-certs https -ep .certs/smtp4dev-localhost.pfx -p smtp4dev-locale, poi "
                + $"docker compose --profile email up -d. Dettaglio: {ex.GetBaseException().Message}");
        }
    }

    [OneTimeTearDown]
    public void Chiudi() => _api?.Dispose();

    [SetUp]
    public async Task SvuotaCasella() => await _api.DeleteAsync("api/messages/*");

    private static EmailSender Sender(int porta = PortaSmtp) =>
        new(smtpSource: Host, smtpPort: porta, smtpUser: "utente-test", smtpPassword: "password-test", from: Mittente);

    private static string Oggetto(string caso) => $"[test] {caso} {Guid.NewGuid():N}";

    /// <summary>Attende il messaggio con quell'oggetto: la consegna a smtp4dev non e' sincrona con l'API.</summary>
    private async Task<JsonElement> Ricevuto(string oggetto)
    {
        for (var i = 0; i < 20; i++)
        {
            var pagina = await _api.GetFromJsonAsync<JsonElement>("api/messages?pageSize=50");
            foreach (var m in pagina.GetProperty("results").EnumerateArray())
                if (m.GetProperty("subject").GetString() == oggetto)
                    return m;
            await Task.Delay(250);
        }
        Assert.Fail($"Nessun messaggio con oggetto '{oggetto}' arrivato a smtp4dev.");
        return default;
    }

    private async Task<string> Html(JsonElement messaggio) =>
        await _api.GetStringAsync($"api/messages/{messaggio.GetProperty("id").GetString()}/html");

    [Test]
    public async Task SendEmail_ShouldConsegnareIlMessaggio_ConMittenteDestinatarioEOggetto()
    {
        var oggetto = Oggetto("consegna");

        var (msg, esito) = Sender().SendEmail(Destinatario, oggetto, "<p>Corpo di prova</p>");

        Assert.That(esito, Is.True, $"Invio fallito: {msg}");
        var ricevuto = await Ricevuto(oggetto);
        Assert.Multiple(() =>
        {
            Assert.That(ricevuto.GetProperty("from").GetString(), Does.Contain(Mittente));
            Assert.That(ricevuto.GetProperty("to").EnumerateArray().Select(t => t.GetString()),
                Has.Some.Contains(Destinatario));
        });
    }

    /// <summary>
    /// Le PEC agli enti sono in italiano e in HTML: accenti, simbolo dell'euro e markup devono arrivare
    /// tali e quali, non come "?" o entita' doppiamente codificate.
    /// </summary>
    [Test]
    public async Task SendEmail_CorpoHtmlConAccentiEEuro_ShouldArrivareIntatto()
    {
        var oggetto = Oggetto("caratteri");
        const string corpo = "<h1>Regolare esecuzione</h1><p>Importo dovuto: 1.234,56 € — già calcolato, perché è così.</p>";

        var (msg, esito) = Sender().SendEmail(Destinatario, oggetto, corpo);

        Assert.That(esito, Is.True, $"Invio fallito: {msg}");
        var html = await Html(await Ricevuto(oggetto));
        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("<h1>Regolare esecuzione</h1>"));
            Assert.That(html, Does.Contain("1.234,56 €"));
            Assert.That(html, Does.Contain("già calcolato, perché è così"));
        });
    }

    /// <summary>
    /// Server non raggiungibile: EmailSender non lancia, restituisce (messaggio, false). E' il contratto
    /// su cui contano i chiamanti (log dell'esito, nessuna eccezione non gestita nella function).
    /// </summary>
    [Test]
    public void SendEmail_PortaSenzaServer_ShouldRestituireFalse_SenzaLanciare()
    {
        var (msg, esito) = (string.Empty, true);

        Assert.That(() => (msg, esito) = Sender(porta: 2466).SendEmail(Destinatario, Oggetto("ko"), "<p>x</p>"),
            Throws.Nothing);
        Assert.Multiple(() =>
        {
            Assert.That(esito, Is.False);
            Assert.That(msg, Is.Not.Empty, "Il messaggio d'errore deve dire cosa e' andato storto.");
        });
    }
}
