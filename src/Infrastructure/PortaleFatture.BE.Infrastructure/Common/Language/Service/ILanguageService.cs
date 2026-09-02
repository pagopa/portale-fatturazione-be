using Azure.AI.TextAnalytics;

namespace PortaleFatture.BE.Infrastructure.Common.Language.Service;

public interface ILanguageService
{
    /// <summary>
    /// `false` quando endpoint o chiave non sono configurati: in quel caso il servizio non è
    /// utilizzabile e i tre metodi sollevano <see cref="InvalidOperationException"/>.
    ///
    /// Esiste perché la configurazione di questo servizio è **opzionale**: un ambiente che non usa
    /// Azure AI Language deve poter avviare l'API e far funzionare tutto il resto. Gli endpoint la
    /// interrogano per rispondere **503 Service Unavailable** invece di un errore generico —
    /// "il servizio non è configurato qui" è un'informazione diversa da "la richiesta è sbagliata".
    /// </summary>
    bool IsConfigured { get; }

    /// <remarks>
    /// I tre metodi restituiscono `null` **solo** quando l'elaborazione è riuscita e non ha prodotto
    /// risultati. Se la chiamata al servizio esterno fallisce sollevano invece
    /// <see cref="PortaleFatture.BE.Core.Exceptions.UpstreamServiceException"/> (→ 502): prima
    /// entrambi i casi tornavano `null` e diventavano un 404, quindi "nessuna PII in questo testo" e
    /// "la chiave è scaduta" erano indistinguibili per il client e nei log applicativi.
    /// </remarks>
    Task<PiiEntityCollection?> DetectPersonalIdentifiableInformationAsync(string text, string language = "it", CancellationToken cancellationToken = default);

    Task<DetectedLanguage?> DetectLanguageAsync(string text, CancellationToken cancellationToken = default);

    Task<List<AbstractiveSummarizeResultCollection>?> SummarizeTextAsync(string text, string language = "it", CancellationToken cancellationToken = default);
}
