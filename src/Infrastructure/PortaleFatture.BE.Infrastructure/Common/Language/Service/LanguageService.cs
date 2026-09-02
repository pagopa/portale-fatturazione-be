using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PortaleFatture.BE.Core.Exceptions;

namespace PortaleFatture.BE.Infrastructure.Common.Language.Service;

public class LanguageService : ILanguageService
{
    private readonly string? _endpoint;
    private readonly string? _key;
    private readonly ILogger<LanguageService> _logger;
    private readonly TextAnalyticsClient? _client;
    private readonly TimeSpan _timeout;
    private readonly int _maxChars;
    private readonly int _maxCharsSummarize ;

    /// <inheritdoc />
    public bool IsConfigured => _client is not null; // true se endpoint e key sono configurati, altrimenti false

    /// <summary>
    /// ATTENZIONE Il costruttore **non solleva** se la configurazione manca: espone <see cref="IsConfigured"/>
    /// e lascia decidere al chiamante.
    ///
    /// Prima lanciava, e in combinazione con una registrazione DI *eager* questo impediva l'avvio
    /// dell'intera applicazione in qualunque ambiente senza la sezione `Language` — non le sole tre
    /// rotte che usano il servizio (misurato: 206 test di integrazione rossi su 568). La registrazione
    /// è ora lazy, e questo costruttore non lancia: un servizio esterno **opzionale** non deve poter
    /// impedire il boot di ciò che non lo usa.
    ///
    /// La configurazione mancante resta comunque **visibile**: un warning a ogni avvio del servizio, e
    /// un 503 esplicito a chi chiama le rotte.
    /// </summary>
    public LanguageService(string? endpoint, string? key, ILogger<LanguageService> logger,
        int timeoutSeconds = 45, int maxChars = 5_120, int maxCharsSummarize = 125_000)
    {
        _maxChars = maxChars > 0 ? maxChars : 5_120;
        _maxCharsSummarize = maxCharsSummarize > 0 ? maxCharsSummarize : 125_000;
        _logger = logger;

        _endpoint = endpoint;
        _key = key;
        _timeout = TimeSpan.FromSeconds(timeoutSeconds > 0 ? timeoutSeconds : 45);

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning(
                "Azure AI Language non configurato ({Mancante}): le rotte api/piid, api/language-detection "
                + "e api/summarize-text risponderanno 503. Il resto dell'applicazione non e' impattato.",
                string.IsNullOrWhiteSpace(endpoint) ? "endpoint assente" : "chiave assente");
            return;
        }

        _client = new TextAnalyticsClient(new Uri(endpoint), new AzureKeyCredential(key));
    }

    /// <summary>
    /// Barriera per i tre metodi pubblici. Non è ridondante rispetto alla guardia degli endpoint: la
    /// protegge da un chiamante futuro che dimenticasse di verificare <see cref="IsConfigured"/>, e
    /// fallisce con un messaggio esplicito invece che con una NullReferenceException sul client.
    /// </summary>
    private void CheckConfigured()
    {
        if (_client is null)
            throw new InvalidOperationException(
                "Azure AI Language non configurato: valorizzare PortaleFattureOptions:Language:Endpoint e :Key.");
    }

    /// <summary>
    /// Rifiuta **prima** della chiamata un testo piu' lungo di quanto il servizio accetti.
    ///
    /// Senza questo controllo il limite lo applicava Azure: l'SDK sollevava, il catch traduceva in
    /// <see cref="UpstreamServiceException"/> e il client vedeva un **502** — cioe' "il servizio a monte
    /// ha un problema", mentre il problema e' nella richiesta. E soprattutto la chiamata veniva
    /// **fatta e pagata** prima di scoprirlo.
    ///
    /// Con <see cref="ValidationException"/> diventa un **400** (mappatura gia' esistente nel gestore
    /// globale) e il costo non si sostiene affatto.
    /// </summary>
    private static void CheckTextLength(string text, int massimo, string operazione)
    {
        if (text.Length > massimo)
            throw new ValidationException(
                $"Il testo supera il limite di {massimo:N0} caratteri accettato da Azure AI Language "
                + $"per l'operazione di {operazione} (lunghezza ricevuta: {text.Length:N0}).");
    }

    /// <summary>
    /// Detects personally identifiable information (PII) in the given text.
    /// </summary>
    /// <param name="text">The text to analyze for PII.</param>
    /// <param name="language">The language of the text (default is "it").</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A collection of PII entities if any are found; otherwise, null.</returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<PiiEntityCollection?> DetectPersonalIdentifiableInformationAsync(string text, string language = "it", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("The text to analyze is required.", nameof(text));
        }

        CheckConfigured();
        CheckTextLength(text, _maxChars, "rilevazione PII");

        try
        {
            PiiEntityCollection response = await _client!.RecognizePiiEntitiesAsync(text, language, cancellationToken: cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            // Si logga (come prima) ma NON si restituisce null: un null qui diventerebbe un 404 per il
            // client, indistinguibile da "nessuna PII trovata nel testo". Il fallimento del servizio a
            // monte e' un fatto diverso, e diventa un 502 via UpstreamServiceException.
            _logger.LogError(ex, "Error during PII detection.");
            throw new UpstreamServiceException("Azure AI Language: rilevazione PII non riuscita.", ex);
        }
    }

    /// <summary>
    /// Detects the language of the given text.
    /// </summary>
    /// <param name="text">The text to analyze for language detection.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The detected language if successful; otherwise, null.</returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<DetectedLanguage?> DetectLanguageAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("The text to analyze is required.", nameof(text));
        }

        CheckConfigured();
        CheckTextLength(text, _maxChars, "rilevazione della lingua");

        try
        {
            DetectedLanguage response = await _client!.DetectLanguageAsync(text, cancellationToken: cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during language detection.");
            throw new UpstreamServiceException("Azure AI Language: rilevazione della lingua non riuscita.", ex);
        }
    }

    /// <summary>
    /// Summarizes the given text using abstractive summarization.
    /// </summary>
    /// <param name="text">The text to summarize.</param>
    /// <param name="language">The language of the text (default is "it").</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of abstractive summarization results if successful; otherwise, null.</returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<List<AbstractiveSummarizeResultCollection>?> SummarizeTextAsync(string text, string language = "it", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("The text to analyze is required.", nameof(text));
        }

        CheckConfigured();
        CheckTextLength(text, _maxCharsSummarize, "sintesi del testo");

        // La sintesi e' una long-running operation: con WaitUntil.Completed l'SDK fa polling finche'
        // Azure non risponde, senza alcun limite superiore proprio. Il gateway davanti all'API taglia
        // pero' intorno al minuto (v. docs/autenticazione.md), quindi senza questo limite la richiesta
        // morirebbe FUORI dall'applicazione: connessione tagliata, errore generico per il client e
        // nessuna traccia nei nostri log — pur avendo pagato la chiamata e occupato un thread.
        // Gli altri due metodi non ne hanno bisogno: sono chiamate HTTP singole, gia' coperte dal
        // NetworkTimeout del pipeline di Azure.Core.
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_timeout);

        try
        {
            var response = await _client!.AbstractiveSummarizeAsync(
                WaitUntil.Completed,
                new[] { text },
                language,
                cancellationToken: cts.Token);

            if (response == null)
            {
                return null;
            }

            return response.GetValues().ToList();
        }
        // Il filtro discrimina CHI ha cancellato: se il token del chiamante e' gia' cancellato e' il
        // client ad aver abbandonato la richiesta, e l'eccezione si propaga com'e' — non e' un guasto
        // del servizio a monte e non va contata come tale nel monitoraggio. Se invece e' scaduto solo
        // il nostro, e' un timeout upstream -> 504.
        // ATTENZIONE all'ordine: questo catch deve stare PRIMA di quello generico, altrimenti
        // OperationCanceledException finisce nel ramo 502 e il 504 non esce mai.
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                "Azure AI Language: sintesi del testo non conclusa entro {Timeout} secondi.",
                _timeout.TotalSeconds);
            throw new UpstreamTimeoutException(
                $"Azure AI Language: sintesi del testo non conclusa entro {_timeout.TotalSeconds:N0} secondi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure AI Language: sintesi del testo non riuscita.");
            throw new UpstreamServiceException("Azure AI Language: sintesi del testo non riuscita.", ex);
        }
    }

}
