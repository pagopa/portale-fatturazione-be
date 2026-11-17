using PortaleFatture.BE.Infrastructure.Gateway.Email;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// <see cref="PspEmailSender"/> (email ai PSP) e <see cref="AlertSender"/> (alert del team DATA) contro
/// un finto Google (<see cref="FakeGmailServer"/>). Fino al 13/11/2026 i due sender non avevano alcun
/// test: chiamano l'API HTTPS di Gmail, non un server SMTP, e gli indirizzi erano fissi nel codice.
///
/// Cosa provano: l'intero percorso del sender — verifica del token, rinnovo se scaduto, composizione
/// del messaggio, chiamata di invio, esito restituito al chiamante — e che cio' che arriva a "Google"
/// sia il messaggio giusto. Girano sempre: non servono container ne' rete.
/// Cosa NON provano: Google vero (quote, rifiuti reali, consegna). Per quello resta l'invio manuale.
///
/// Nessuna email puo' partire: gli indirizzi sono quelli del server locale, passati dal costruttore.
/// </summary>
[Category("Email")]
[NonParallelizable]
public class GmailSendersFakeServerTests
{
    private const string Mittente = "noreply@portalefatture.invalid";
    private const string NomeMittente = "Portale Fatturazione";
    private const string Destinatario = "psp.test@portalefatture.invalid";
    private const string Applicazione = "portale-fatture-test";

    private FakeGmailServer _gmail = null!;

    [SetUp]
    public void Avvia() => _gmail = new FakeGmailServer();

    [TearDown]
    public void Ferma() => _gmail.Dispose();

    /// <summary>I due sender hanno lo stesso contratto di invio: ogni caso gira su entrambi.</summary>
    public enum Sender { Psp, Alert }

    private (string Messaggio, bool Esito) Invia(
        Sender quale, string accessToken = FakeGmailServer.TokenValido,
        string to = Destinatario, string toName = "PSP di prova",
        string subject = "Oggetto di prova", string body = "<p>Corpo di prova</p>")
        => quale == Sender.Psp
            ? new PspEmailSender(accessToken, "refresh-token-test", "client-id-test", "client-secret-test", Mittente, NomeMittente, _gmail.Endpoints)
                .SendEmail(to, toName, subject, body, Applicazione)
            : new AlertSender(accessToken, "refresh-token-test", "client-id-test", "client-secret-test", Mittente, NomeMittente, _gmail.Endpoints)
                .SendEmail(to, toName, subject, body, Applicazione);

    // ------------------------------------------------------------------ percorso felice

    [Test]
    public void SendEmail_TokenValido_ShouldInviareIlMessaggioERestituireLId([Values] Sender quale)
    {
        var (id, esito) = Invia(quale, subject: "Report trimestrale", body: "<h1>Titolo</h1><p>Importo: 1.234,56 € — già calcolato</p>");

        Assert.That(esito, Is.True, $"Invio fallito: {id}");
        var messaggio = _gmail.UltimoMessaggio();
        Assert.Multiple(() =>
        {
            Assert.That(id, Is.EqualTo("fake-msg-1"), "l'esito porta l'identificativo dato da Gmail");
            Assert.That(_gmail.Invii.Count(), Is.EqualTo(1), "un solo invio");
            Assert.That(_gmail.Rinnovi, Is.Empty, "token valido: nessun rinnovo");
            Assert.That(_gmail.Invii.Single().Percorso, Is.EqualTo("/gmail/v1/users/me/messages/send"));
            Assert.That(_gmail.Invii.Single().Authorization, Is.EqualTo("Bearer " + FakeGmailServer.TokenValido));
            Assert.That(messaggio.From.Mailboxes.Single().Address, Is.EqualTo(Mittente));
            Assert.That(messaggio.From.Mailboxes.Single().Name, Is.EqualTo(NomeMittente));
            Assert.That(messaggio.To.Mailboxes.Single().Address, Is.EqualTo(Destinatario));
            Assert.That(messaggio.To.Mailboxes.Single().Name, Is.EqualTo("PSP di prova"));
            Assert.That(messaggio.Subject, Is.EqualTo("Report trimestrale"));
            Assert.That(messaggio.HtmlBody, Does.Contain("<h1>Titolo</h1>").And.Contain("1.234,56 € — già calcolato"));
        });
    }

    [Test]
    public void SendEmail_TokenScaduto_ShouldRinnovarloEInviareConQuelloNuovo([Values] Sender quale)
    {
        var (msg, esito) = Invia(quale, accessToken: "token-scaduto");

        Assert.That(esito, Is.True, $"Invio fallito: {msg}");
        Assert.Multiple(() =>
        {
            Assert.That(_gmail.Rinnovi.Count(), Is.EqualTo(1), "un solo rinnovo");
            var rinnovo = _gmail.Rinnovi.Single().Corpo;
            Assert.That(rinnovo, Does.Contain("refresh-token-test").And.Contain("client-id-test").And.Contain("client-secret-test"));
            Assert.That(rinnovo, Does.Contain("refresh_token"), "grant_type del rinnovo");
            Assert.That(_gmail.Invii.Single().Authorization, Is.EqualTo("Bearer " + FakeGmailServer.TokenRinnovato),
                "l'invio usa il token rinnovato, non quello scaduto");
        });
    }

    // ------------------------------------------------------------------ errori: mai un'eccezione al chiamante

    /// <summary>
    /// Credenziali revocate: il rinnovo non restituisce un token. Il sender non lancia e non tenta
    /// l'invio: restituisce (messaggio, false), che e' cio' che le function registrano come esito.
    /// </summary>
    [Test]
    public void SendEmail_RinnovoFallito_ShouldRestituireFalse_SenzaInviare([Values] Sender quale)
    {
        _gmail.RinnovoRiesce = false;
        var (msg, esito) = (string.Empty, true);

        Assert.That(() => (msg, esito) = Invia(quale, accessToken: "token-scaduto"), Throws.Nothing);
        Assert.Multiple(() =>
        {
            Assert.That(esito, Is.False);
            Assert.That(msg, Is.Not.Empty);
            Assert.That(_gmail.Invii, Is.Empty, "senza token nessun invio");
        });
    }

    [TestCase(Sender.Psp, 400)]
    [TestCase(Sender.Psp, 403)]
    [TestCase(Sender.Psp, 429)]
    [TestCase(Sender.Alert, 400)]
    [TestCase(Sender.Alert, 403)]
    [TestCase(Sender.Alert, 429)]
    public void SendEmail_GmailRifiuta_ShouldRestituireFalse_ConIlMotivo(Sender quale, int stato)
    {
        _gmail.EsitoInvio = stato;
        var (msg, esito) = (string.Empty, true);

        Assert.That(() => (msg, esito) = Invia(quale), Throws.Nothing);
        Assert.Multiple(() =>
        {
            Assert.That(esito, Is.False);
            Assert.That(msg, Does.Contain("Invio rifiutato dal finto Gmail"), "il motivo dato da Gmail arriva al chiamante");
        });
    }

    [Test]
    public void SendEmail_GoogleNonRaggiungibile_ShouldRestituireFalse_SenzaLanciare([Values] Sender quale)
    {
        _gmail.Dispose(); // nessuno in ascolto su quell'indirizzo
        var (msg, esito) = (string.Empty, true);

        Assert.That(() => (msg, esito) = Invia(quale), Throws.Nothing);
        Assert.Multiple(() =>
        {
            Assert.That(esito, Is.False);
            Assert.That(msg, Is.Not.Empty);
        });
    }

    // ------------------------------------------------------------------ adversarial

    /// <summary>
    /// Header injection dall'anagrafica: un a-capo nell'oggetto o nel nome del destinatario non deve
    /// aggiungere destinatari al messaggio che arriva a Gmail.
    /// </summary>
    [Test]
    public void SendEmail_ACapoInOggettoENome_ShouldNonAggiungereDestinatari([Values] Sender quale)
    {
        var (msg, esito) = Invia(quale,
            subject: "Oggetto\r\nBcc: intruso@portalefatture.invalid",
            toName: "PSP\r\nCc: intruso@portalefatture.invalid");

        Assert.That(esito, Is.True, $"Invio fallito: {msg}");
        var messaggio = _gmail.UltimoMessaggio();
        Assert.Multiple(() =>
        {
            Assert.That(messaggio.To.Mailboxes.Select(m => m.Address), Is.EqualTo(new[] { Destinatario }));
            Assert.That(messaggio.Cc, Is.Empty);
            Assert.That(messaggio.Bcc, Is.Empty);
        });
    }

    /// <summary>Piu' indirizzi nel campo destinatario: rifiutati in composizione, a Gmail non arriva nulla.</summary>
    [TestCase(Sender.Psp, "uno@portalefatture.invalid, due@portalefatture.invalid")]
    [TestCase(Sender.Psp, "uno@portalefatture.invalid; due@portalefatture.invalid")]
    [TestCase(Sender.Alert, "uno@portalefatture.invalid, due@portalefatture.invalid")]
    [TestCase(Sender.Alert, "uno@portalefatture.invalid\r\nBcc: due@portalefatture.invalid")]
    public void SendEmail_PiuIndirizziNelDestinatario_ShouldRestituireFalse_SenzaInviare(Sender quale, string destinatario)
    {
        var (msg, esito) = Invia(quale, to: destinatario);

        Assert.Multiple(() =>
        {
            Assert.That(esito, Is.False);
            Assert.That(msg, Is.Not.Empty);
            Assert.That(_gmail.Invii, Is.Empty);
        });
    }

    [Test]
    public void SendEmail_CorpoConScriptEMoltoGrande_ShouldArrivareIdentico([Values] Sender quale)
    {
        var corpo = "<script>alert('x')</script>" + string.Concat(Enumerable.Repeat("<p>riga di report 1.234,56 €</p>", 5000));

        var (msg, esito) = Invia(quale, body: corpo);

        Assert.That(esito, Is.True, $"Invio fallito: {msg}");
        Assert.That(_gmail.UltimoMessaggio().HtmlBody!.TrimEnd('\r', '\n'), Is.EqualTo(corpo));
    }

    /// <summary>Il token non finisce mai nel messaggio restituito al chiamante, che viene scritto nei log.</summary>
    [Test]
    public void SendEmail_Errore_ShouldNonRiportareIlTokenNelMessaggio([Values] Sender quale)
    {
        _gmail.EsitoInvio = 403;

        var (msg, _) = Invia(quale);

        Assert.That(msg, Does.Not.Contain(FakeGmailServer.TokenValido).And.Not.Contain("client-secret-test").And.Not.Contain("refresh-token-test"));
    }

    // ------------------------------------------------------------------ posta inviata (solo PSP)

    [Test]
    public void SentFolder_ShouldDistinguereIMessaggiTrovatiDaQuelliAssenti()
    {
        _gmail.InviatiNoti.Add("<presente@portalefatture.invalid>");
        var sender = new PspEmailSender(FakeGmailServer.TokenValido, "r", "c", "s", Mittente, NomeMittente, _gmail.Endpoints);

        var esiti = sender.SentFolder(new Dictionary<string, string>
        {
            ["A"] = "<presente@portalefatture.invalid>",
            ["B"] = "<assente@portalefatture.invalid>"
        }, Applicazione);

        Assert.Multiple(() =>
        {
            Assert.That(esiti, Is.EqualTo(new Dictionary<string, bool> { ["A"] = true, ["B"] = false }));
            Assert.That(_gmail.Chiamate.Where(c => c.Percorso.EndsWith("/messages", StringComparison.Ordinal)).Select(c => c.Query),
                Has.All.Contain("labelIds=SENT"), "la ricerca e' limitata alla posta inviata");
            Assert.That(_gmail.Invii, Is.Empty, "la verifica non invia nulla");
        });
    }

    /// <summary>
    /// Caratterizzazione: se Google non risponde, SentFolder ingoia l'errore e restituisce un elenco
    /// VUOTO, non "tutti false". Il chiamante non distingue "nessun messaggio trovato" da "verifica
    /// non riuscita": da tenere presente leggendo gli esiti.
    /// </summary>
    [Test]
    public void SentFolder_GoogleNonRaggiungibile_ShouldRestituireElencoVuoto_Caratterizzazione()
    {
        var sender = new PspEmailSender(FakeGmailServer.TokenValido, "r", "c", "s", Mittente, NomeMittente, _gmail.Endpoints);
        _gmail.Dispose();

        var esiti = sender.SentFolder(new Dictionary<string, string> { ["A"] = "<x@portalefatture.invalid>" }, Applicazione);

        Assert.That(esiti, Is.Empty);
    }

    // ------------------------------------------------------------------ indirizzi predefiniti

    /// <summary>
    /// Senza indirizzi espliciti i sender parlano con Google vero. E' la garanzia che l'apertura fatta
    /// per i test non abbia cambiato la produzione: nessun costruttore dell'applicazione li passa.
    /// </summary>
    [Test]
    public void GmailEndpoints_Predefiniti_ShouldEssereQuelliDiGoogle()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GmailEndpoints.Google.ApiBaseUri, Is.Null, "null = indirizzo predefinito della libreria Google");
            Assert.That(GmailEndpoints.Google.TokenInfoUrl, Is.EqualTo("https://www.googleapis.com/oauth2/v1/tokeninfo"));
            Assert.That(GmailEndpoints.Google.TokenUrl, Is.EqualTo("https://oauth2.googleapis.com/token"));
        });
    }
}
