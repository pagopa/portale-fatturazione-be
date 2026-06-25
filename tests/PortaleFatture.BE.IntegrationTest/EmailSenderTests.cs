using Microsoft.Extensions.Configuration;
using NUnit.Framework.Legacy;
using PortaleFatture.BE.Infrastructure.Gateway.Email;

namespace PortaleFatture.BE.IntegrationTest;

// Test di integrazione per validare l'invio email di EmailSender (es. dopo un upgrade di MailKit).
// Tutti i test sono [Explicit]: NON partono in CI né in una run normale, vanno lanciati a mano.
//
// COME LANCIARE I SINGOLI TEST (dalla root del repo):
//   - entrambi i casi reali:   dotnet test tests/PortaleFatture.BE.IntegrationTest --filter "Name~RealDelivery"
//   - solo invio a email std:  dotnet test tests/PortaleFatture.BE.IntegrationTest --filter "Name=RealDelivery_EmailNormale"
//   - solo invio a PEC:        dotnet test tests/PortaleFatture.BE.IntegrationTest --filter "Name=RealDelivery_PEC"
//
// SECRET RICHIESTI (User Secrets del progetto, mai committati). Per la prova usare una PEC Aruba
// personale; per la validazione finale ripuntare gli stessi secret alla PEC di produzione:
//   PortaleFattureOptions:SMTP=smtps.pec.aruba.it  SMTP_PORT=465
//   SMTP_AUTH=<pec>  SMTP_PASSWORD=<password>  FROM=<pec> (= SMTP_AUTH)
//   SMTP_TEST_TO=<casella standard>  SMTP_TEST_TO_PEC=<altra PEC>
//   (impostare la password con apici SINGOLI in PowerShell per evitare l'espansione di '$' ecc.)
//
// NOTA S/MIME: aprendo il messaggio in Outlook può comparire "Non è possibile verificare la firma
// S/MIME". NON è un problema del test né di MailKit: EmailSender invia HTML semplice (non firmato);
// la firma è la "busta di trasporto" che il gestore PEC (Aruba) appone, e Outlook non riesce a
// verificarla se sulla macchina manca la catena di CA del gestore. È cosmetico: il messaggio è integro.
public class EmailSenderTests
{
    private IConfiguration _conf;

    [SetUp]
    public void Setup()
    {
        // Configurazione solo-email: niente DI completo (ServiceProvider richiede la connection string del DB).
        _conf = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<EmailSenderTests>() // User Secrets per SMTP, FROM e TO
            .AddEnvironmentVariables() // per eventuali override in CI
            .Build();
    }

    // --- Recapito reale: usa i secret SMTP_* configurati (PEC personale per la prova; PEC di produzione
    //     dopo l'ok del cliente). L'email arriva davvero (nessuna cancellazione). ---

    [Explicit]
    [TestCase(false, TestName = "RealDelivery_EmailNormale")]
    [TestCase(true, TestName = "RealDelivery_PEC")]
    public void SendEmail_RealSmtp_ShouldSucceed(bool pec)
    {
        var sender = BuildRealSender();
        var to = GetRecipient(pec);

        var (msg, esito) = sender.SendEmail(to, BuildSubject("Recapito reale", pec), BuildBody(pec));

        ClassicAssert.IsTrue(esito, $"Invio fallito: {msg}");
    }

    // --- Percorso sandbox ABBANDONATO (lasciato commentato per memoria storica).
    //     Mailtrap non è utilizzabile: tutte le sue porte usano STARTTLS, mentre EmailSender
    //     usa TLS implicito (SslOnConnect) -> handshake "unexpected packet format".
    //
    // [Explicit]
    // [TestCase(false, TestName = "Sandbox_EmailNormale")]
    // [TestCase(true, TestName = "Sandbox_PEC")]
    // public void SendEmail_Sandbox_ShouldSucceed(bool pec)
    // {
    //     var sender = BuildSandboxSender();
    //     var to = GetRecipient(pec);
    //
    //     var (msg, esito) = sender.SendEmail(to, BuildSubject("Sandbox", pec), BuildBody(pec));
    //
    //     ClassicAssert.IsTrue(esito, $"Invio fallito: {msg}");
    // }

    // --- Helper ---


    /// <summary>
    /// Costruisce un EmailSender reale, con i parametri SMTP presi dai secret (vedi commento in cima al file).
    /// </summary>
    /// <returns>Un'istanza di EmailSender configurata con i parametri reali.</returns>
    private EmailSender BuildRealSender() => new(
        smtpSource: Required("PortaleFattureOptions:SMTP"),
        smtpPort: Convert.ToInt32(Required("PortaleFattureOptions:SMTP_PORT")),
        smtpUser: Required("PortaleFattureOptions:SMTP_AUTH"),
        smtpPassword: Required("PortaleFattureOptions:SMTP_PASSWORD"),
        from: Required("PortaleFattureOptions:FROM"));

    // Helper del percorso sandbox abbandonato (vedi sopra), lasciato commentato per memoria storica.
    // private EmailSender BuildSandboxSender() => new(
    //     smtpSource: Required("PortaleFattureOptions:SMTP_SANDBOX"),
    //     smtpPort: Convert.ToInt32(Required("PortaleFattureOptions:SMTP_SANDBOX_PORT")),
    //     smtpUser: Required("PortaleFattureOptions:SMTP_SANDBOX_AUTH"),
    //     smtpPassword: Required("PortaleFattureOptions:SMTP_SANDBOX_PASSWORD"),
    //     from: Required("PortaleFattureOptions:FROM"));

    /// <summary>
    /// Restituisce il destinatario corretto in base al flag pec: se true, prende il secret SMTP_TEST_TO_PEC, altrimenti SMTP_TEST_TO.
    /// </summary>
    /// <param name="pec">Indica se la mail è PEC o normale.</param>
    /// <returns>Il destinatario corretto in base al flag pec.</returns>
    private string GetRecipient(bool pec) =>
        pec ? Required("PortaleFattureOptions:SMTP_TEST_TO_PEC")
            : Required("PortaleFattureOptions:SMTP_TEST_TO");

    /// <summary>
    /// Costruisce l'oggetto della mail di test, includendo la modalità (PEC o email normale) e il timestamp UTC.
    /// </summary>
    /// <param name="modalita">La modalità della mail (ad esempio "Recapito reale" o "Sandbox").</param>
    /// <param name="pec">Indica se la mail è PEC o normale.</param>
    /// <returns>L'oggetto della mail di test.</returns>
    private static string BuildSubject(string modalita, bool pec) =>
        $"Test MailKit 4.16.0 [{modalita}{(pec ? " - PEC" : "")}] - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

    /// <summary>
    /// Costruisce il corpo della mail di test, includendo la modalità (PEC o email normale).
    /// </summary>
    /// <param name="pec">Indica se la mail è PEC o normale.</param>
    /// <returns>Il corpo della mail di test.</returns>
    private static string BuildBody(bool pec) =>
        "<h1>Test MailKit 4.16.0</h1>" +
        $"<p>Email di verifica invio dopo l'upgrade di MailKit ({(pec ? "PEC" : "email normale")}). " +
        "Se la ricevi, la connessione SSL, l'autenticazione e l'invio funzionano.</p>";

    /// <summary>
    /// Restituisce il valore della chiave di configurazione richiesta, lanciando un'eccezione se non è presente o è vuota.
    /// </summary>
    /// <param name="key">La chiave di configurazione da recuperare.</param>
    /// <returns>Il valore della chiave di configurazione.</returns>
    private string Required(string key)
    {
        var value = _conf.GetValue<string>(key);
        ClassicAssert.IsFalse(string.IsNullOrWhiteSpace(value), $"{key} non configurato (User Secrets)");
        return value!;
    }
}
