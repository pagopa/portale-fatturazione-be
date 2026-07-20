using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PortaleFatture.BE.Infrastructure.Common.Language.Service;

public class LanguageService : ILanguageService
{
    private readonly string? _endpoint;
    private readonly string? _key;
    private readonly ILogger<LanguageService> _logger;
    private readonly TextAnalyticsClient _client;


    public LanguageService(string? endpoint, string? key, ILogger<LanguageService> logger)
    {
        _logger = logger;

        _endpoint = endpoint;
        _key = key;

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException($"Missing environment variable [LanguageServiceEndpoint]");
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException($"Missing environment variable [LanguageServiceKey]");
        }

        _client = new TextAnalyticsClient(new Uri(endpoint), new AzureKeyCredential(key));
    }

    public async Task<PiiEntityCollection?> DetectPersonalIdentifiableInformationAsync(string text, string language = "it", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("The text to analyze is required.", nameof(text));
        }

        try
        {
            PiiEntityCollection response = await _client.RecognizePiiEntitiesAsync(text, language, cancellationToken: cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during PII detection.");
            return null;
        }
    }


    public async Task<DetectedLanguage?> DetectLanguageAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("The text to analyze is required.", nameof(text));
        }
        try
        {
            DetectedLanguage response = await _client.DetectLanguageAsync(text, cancellationToken: cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during language detection.");
            return null;
        }
    }

    public async Task<List<AbstractiveSummarizeResultCollection>> SummarizeTextAsync(string text, string language = "it", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("The text to analyze is required.", nameof(text));
        }
        try
        {
            var response = await _client.AbstractiveSummarizeAsync(
                WaitUntil.Completed,
                new[] { text },
                language,
                cancellationToken: cancellationToken);


            // check if response is null then return null
            if (response == null)
            {
                return null;
            }
            //otherwise return the response value
            return response.GetValues().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during text summarization.");
            return null;
        }
    }

}
