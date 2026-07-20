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

public class LanguageServiceResponseSummarizeText
{
    public string? Text { get; set; } = string.Empty;  

}
