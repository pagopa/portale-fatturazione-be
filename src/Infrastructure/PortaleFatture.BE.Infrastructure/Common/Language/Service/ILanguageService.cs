using Azure.AI.TextAnalytics;

namespace PortaleFatture.BE.Infrastructure.Common.Language.Service;

public interface ILanguageService
{
    Task<PiiEntityCollection?> DetectPersonalIdentifiableInformationAsync(string text, string language = "it", CancellationToken cancellationToken = default);
}
