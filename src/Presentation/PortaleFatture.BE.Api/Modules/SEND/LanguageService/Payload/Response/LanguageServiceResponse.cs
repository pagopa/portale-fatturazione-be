using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;

namespace PortaleFatture.BE.Api.Modules.SEND.LanguageService.Payload.Response;

public class LanguageServiceResponsePII
{
    public string? RedactedString { get; set; }
}

public class LanguageServiceResponseLanguageDetection
{
    public string? DetectedLanguage { get; set; }
    public double? ConfidenceScore { get; set; }
    public LanguageServiceResponseLanguageDetection(string? detectedLanguage, double? confidenceScore)
    {
        DetectedLanguage = detectedLanguage;
        ConfidenceScore = confidenceScore;
    }
}

/// <summary>
/// La sintesi del testo, come contratto **nostro**.
///
/// Esisteva già ma non era collegata: la rotta restituiva direttamente
/// `List&lt;AbstractiveSummarizeResultCollection&gt;`, cioè un tipo di `Azure.AI.TextAnalytics`. Esporre
/// un tipo dell'SDK sull'API pubblica significa che un aggiornamento del pacchetto può cambiare il
/// JSON visto dal frontend — e che il client riceve la forma interna del servizio (statistiche,
/// versione del modello, offset) invece di ciò che gli serve.
///
/// `Sintesi` è una **lista** perché il servizio può restituire più di un riassunto per documento; nella
/// pratica ne torna uno solo, ma appiattirlo a stringa nasconderebbe il caso senza guadagno.
/// </summary>
public class LanguageServiceResponseSummarizeText
{
    public IReadOnlyList<string> Sintesi { get; set; } = [];
}
