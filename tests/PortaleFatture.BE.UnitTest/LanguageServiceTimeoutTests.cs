using System.Net;
using System.Net.Sockets;
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
/// </summary>
[TestFixture]
public class LanguageServiceTimeoutTests
{
    private TcpListener _serverMuto = null!;
    private CancellationTokenSource _stopAccept = null!;
    private readonly List<TcpClient> _connessioniTenuteAperte = [];
    private string _endpoint = null!;

    /// <summary>
    /// Un socket che **accetta** la connessione e non risponde mai: riproduce il servizio lento, che è
    /// il caso per cui il timeout esiste. Un endpoint semplicemente irraggiungibile non servirebbe —
    /// fallirebbe subito in connessione, cioè produrrebbe una UpstreamServiceException (502) e non
    /// eserciterebbe affatto il ramo sotto test.
    /// </summary>
    [OneTimeSetUp]
    public void AvviaServerMuto()
    {
        _stopAccept = new CancellationTokenSource();
        _serverMuto = new TcpListener(IPAddress.Loopback, 0);
        _serverMuto.Start();
        _endpoint = $"http://127.0.0.1:{((IPEndPoint)_serverMuto.LocalEndpoint).Port}";

        _ = Task.Run(async () =>
        {
            try
            {
                while (!_stopAccept.IsCancellationRequested)
                {
                    // Le connessioni vanno TENUTE: chiudendole il client vedrebbe un errore di rete
                    // (502) invece di restare in attesa della risposta (504).
                    _connessioniTenuteAperte.Add(await _serverMuto.AcceptTcpClientAsync(_stopAccept.Token));
                }
            }
            catch (OperationCanceledException) { /* fine fixture */ }
            catch (ObjectDisposedException) { /* listener chiuso */ }
        });
    }

    [OneTimeTearDown]
    public void FermaServerMuto()
    {
        _stopAccept.Cancel();
        foreach (var connessione in _connessioniTenuteAperte)
            connessione.Dispose();
        _serverMuto.Stop();
        _serverMuto.Dispose();
        _stopAccept.Dispose();
    }

    private LanguageService ServizioVerso(string endpoint, int timeoutSeconds) =>
        new(endpoint, "chiave-finta-per-il-test", NullLogger<LanguageService>.Instance, timeoutSeconds);

    [Test]
    public void SummarizeText_ServizioCheNonRisponde_SollevaUpstreamTimeoutENonUpstreamService()
    {
        var servizio = ServizioVerso(_endpoint, timeoutSeconds: 1);

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
        var servizio = ServizioVerso(_endpoint, timeoutSeconds: 120);
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
