using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Infrastructure.Common.Language.Service;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Il timeout della sintesi non è verificabile dai test HTTP dell'area: quelli usano un servizio finto
/// che solleva l'eccezione direttamente, quindi provano la **mappatura** verso il 504, non che sia il
/// servizio a produrla. La distinzione non è teorica — il timeout era già stato implementato una volta
/// e si è perso in una riscrittura successiva senza che nulla diventasse rosso: la configurazione
/// continuava a promettere `Language:TimeoutSeconds`, la documentazione a descriverlo, e il campo
/// `_timeout` restava assegnato ma non letto (un readonly mai usato non produce warning del
/// compilatore). Questi due test sono il presidio che mancava.
///
/// Fino all'11/09/2026 il servizio lento era un **socket TCP locale** che accettava la connessione e
/// non rispondeva. Con l'autenticazione a identità non è più possibile: l'SDK si rifiuta di spedire un
/// token su <c>http://</c>, e un socket TLS locale richiederebbe un certificato. Si usa quindi un
/// trasporto HTTP che accetta la richiesta e non risponde mai (<see cref="TrasportoFinto.Muto"/>): stesso
/// scenario, stesso <see cref="LanguageService"/> vero, stessa pipeline di Azure.Core — senza rete.
/// </summary>
[TestFixture]
public class LanguageServiceTimeoutTests
{
    private static LanguageService ServizioVersoUnServizioMuto(int timeoutSeconds) => new(
        "https://esempio.cognitiveservices.azure.com/",
        NullLogger<LanguageService>.Instance,
        timeoutSeconds,
        credential: new CredenzialeFinta(),
        clientOptions: TrasportoFinto.Muto().Opzioni());

    [Test]
    public void SummarizeText_ServizioCheNonRisponde_SollevaUpstreamTimeoutENonUpstreamService()
    {
        var servizio = ServizioVersoUnServizioMuto(timeoutSeconds: 1);

        var eccezione = Assert.CatchAsync(async () => await servizio.SummarizeTextAsync("un testo qualsiasi"));

        // Il tipo esatto è il punto del test: UpstreamTimeoutException deriva da
        // UpstreamServiceException, quindi un Is.InstanceOf sarebbe verde anche senza il timeout.
        // È il tipo a decidere se il client riceve 504 ("non ha risposto in tempo") o 502
        // ("ha risposto male") — cause diverse, rimedi diversi per chi legge i log.
        Assert.That(eccezione, Is.TypeOf<UpstreamTimeoutException>(),
            "Senza il timeout applicato alla chiamata, l'attesa finirebbe nel catch generico come 502.");
    }

    [Test]
    public void SummarizeText_ChiamanteCheAnnulla_NonVieneScambiatoPerUnTimeoutDelServizio()
    {
        // Timeout nostro ampio: a cancellare è il chiamante, non noi.
        var servizio = ServizioVersoUnServizioMuto(timeoutSeconds: 120);
        using var annullaSubito = new CancellationTokenSource();
        annullaSubito.CancelAfter(TimeSpan.FromMilliseconds(200));

        var eccezione = Assert.CatchAsync(
            async () => await servizio.SummarizeTextAsync("un testo qualsiasi", cancellationToken: annullaSubito.Token));

        // Copre il filtro `when (!cancellationToken.IsCancellationRequested)`: senza, un utente che
        // chiude la pagina produrrebbe un 504 e falsi allarmi sul monitoraggio del servizio esterno.
        Assert.That(eccezione, Is.Not.TypeOf<UpstreamTimeoutException>(),
            "L'abbandono del client non è un guasto di Azure e non va segnalato come tale.");
    }
}
