using System.Net;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Infrastructure.Common.Language.Service;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Autenticazione di Azure AI Language: **solo identità Entra ID**, nessuna chiave (dall'11/09/2026).
///
/// Nessuno di questi casi è osservabile dai test HTTP dell'area, che usano un servizio finto, né da
/// quelli contro Azure reale, che girano con l'identità di chi li esegue. Qui si usa il servizio
/// **vero** con una credenziale e un trasporto finti, e si guarda cosa esce davvero sulla rete.
///
/// Le cose che contano sono quattro, e tutte e quattro fallirebbero **solo in produzione**, alla prima
/// chiamata, se si rompessero:
/// <list type="number">
///   <item>la richiesta porta il token dell'identità e **non** una chiave;</item>
///   <item>il token è chiesto per lo scope giusto (con uno scope sbagliato Azure risponde 401);</item>
///   <item>su un endpoint <c>http://</c> il token non parte;</item>
///   <item>un'identità non disponibile diventa un 502 gestito, non un 500.</item>
/// </list>
/// </summary>
[TestFixture]
public class LanguageServiceAutenticazioneTests
{
    private const string Endpoint = "https://esempio.cognitiveservices.azure.com/";

    private static LanguageService Servizio(string endpoint, TokenCredential credenziale, TrasportoFinto trasporto) =>
        new(endpoint, NullLogger<LanguageService>.Instance, credential: credenziale, clientOptions: trasporto.Opzioni());

    [Test]
    public void Chiamata_PortaIlTokenDellIdentita_ENessunaChiave()
    {
        // 401: quanto basta perché la richiesta parta e venga registrata; l'esito non è il punto.
        var trasporto = TrasportoFinto.CheRisponde(HttpStatusCode.Unauthorized);
        var servizio = Servizio(Endpoint, new CredenzialeFinta(), trasporto);

        Assert.CatchAsync<UpstreamServiceException>(async () => await servizio.DetectLanguageAsync("testo qualsiasi"));

        Assert.That(trasporto.Richieste, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(trasporto.Richieste[0].Authorization, Is.EqualTo($"Bearer {CredenzialeFinta.Token}"));
            Assert.That(trasporto.Richieste[0].HaChiaveApim, Is.False,
                "Nessuna chiave deve viaggiare: l'autenticazione è solo con l'identità.");
        });
    }

    /// <summary>
    /// Lo scope lo sceglie l'SDK in base all'audience delle opzioni del client. Uno scope diverso
    /// produrrebbe un token valido ma per un'altra risorsa, che Azure rifiuta con un 401 — cioè un 502
    /// opaco in produzione. Se un domani si imposta un'audience diversa (cloud sovrano) questo test lo
    /// fa notare.
    /// </summary>
    [Test]
    public void Credenziale_VieneInterrogataPerLoScopeDiCognitiveServices()
    {
        var credenziale = new CredenzialeFinta();
        var servizio = Servizio(Endpoint, credenziale, TrasportoFinto.CheRisponde(HttpStatusCode.Unauthorized));

        Assert.CatchAsync<UpstreamServiceException>(async () => await servizio.DetectLanguageAsync("testo qualsiasi"));

        Assert.That(credenziale.Richieste.SelectMany(r => r.Scopes).Distinct(),
            Is.EquivalentTo(new[] { "https://cognitiveservices.azure.com/.default" }));
    }

    /// <summary>
    /// ATTENZIONE Lo schema non è validato alla configurazione: un endpoint <c>http://</c> dà
    /// <c>IsConfigured = true</c>. È l'SDK a rifiutarsi, alla chiamata, di spedire il token in chiaro —
    /// quindi il token non esce, ma ogni chiamata fallisce con un 502 che non dice "hai scritto http".
    /// </summary>
    [Test]
    public void EndpointHttp_IlTokenNonVieneSpeditoInChiaro_EDiventaUn502()
    {
        var trasporto = TrasportoFinto.CheRisponde(HttpStatusCode.OK);
        var servizio = Servizio("http://esempio.cognitiveservices.azure.com/", new CredenzialeFinta(), trasporto);

        Assert.That(servizio.IsConfigured, Is.True, "Lo schema non è controllato in configurazione.");

        var eccezione = Assert.CatchAsync(async () => await servizio.DetectLanguageAsync("testo qualsiasi"));

        Assert.Multiple(() =>
        {
            Assert.That(eccezione, Is.TypeOf<UpstreamServiceException>());
            Assert.That(trasporto.Richieste, Is.Empty,
                "Nessuna richiesta deve partire: porterebbe il token su una connessione in chiaro.");
        });
    }

    public static IEnumerable<TestCaseData> Operazioni()
    {
        yield return new TestCaseData((Func<LanguageService, Task>)(s => s.DetectPersonalIdentifiableInformationAsync("testo qualsiasi")))
            .SetName("IdentitaNonDisponibile_DiventaUpstreamService(pii)");
        yield return new TestCaseData((Func<LanguageService, Task>)(s => s.DetectLanguageAsync("testo qualsiasi")))
            .SetName("IdentitaNonDisponibile_DiventaUpstreamService(detection)");
        yield return new TestCaseData((Func<LanguageService, Task>)(s => s.SummarizeTextAsync("testo qualsiasi")))
            .SetName("IdentitaNonDisponibile_DiventaUpstreamService(summarize)");
    }

    /// <summary>
    /// È il modo in cui fallisce un App Service **senza managed identity**: la credenziale non ottiene
    /// alcun token. Deve uscire come 502 — l'identità è una dipendenza esterna come il servizio — con la
    /// causa reale conservata per i log, e senza che nessuna richiesta parta.
    ///
    /// Il ruolo mancante è un caso diverso: lì il token si ottiene e il 401/403 arriva da Azure, cioè
    /// il percorso del primo test di questa classe.
    /// </summary>
    [TestCaseSource(nameof(Operazioni))]
    public void IdentitaNonDisponibile_DiventaUpstreamService_ENessunaRichiestaParte(Func<LanguageService, Task> operazione)
    {
        var trasporto = TrasportoFinto.CheRisponde(HttpStatusCode.OK);
        var identitaAssente = new CredenzialeFinta(new CredentialUnavailableException("Nessuna identità disponibile."));
        var servizio = Servizio(Endpoint, identitaAssente, trasporto);

        var eccezione = Assert.CatchAsync(async () => await operazione(servizio));

        Assert.Multiple(() =>
        {
            Assert.That(eccezione, Is.TypeOf<UpstreamServiceException>(), "502, non 500 né 504.");
            Assert.That(eccezione!.InnerException, Is.InstanceOf<CredentialUnavailableException>(),
                "La causa reale deve arrivare ai log, altrimenti un'identità mancante sembra un guasto di Azure.");
            Assert.That(trasporto.Richieste, Is.Empty);
        });
    }
}
