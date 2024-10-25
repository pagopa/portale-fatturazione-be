using Azure.Storage;
using Azure.Storage.Sas;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti;


namespace PortaleFatture.BE.Infrastructure.Gateway.Storage.pagoPA;

public class DocumentStorageSASService(IPortaleFattureOptions options,
    IStringLocalizer<Localization> localizer,
    ILogger<DocumentStorageSASService> logger) : IDocumentStorageSASService
{
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly ILogger<DocumentStorageSASService> _logger = logger;
    private readonly IPortaleFattureOptions _options = options;


    public string GetSASToken(DocumentiSASStorageKey documentKey)
    {
        var blobName = DocumentiSASStorageKey.FileName(documentKey);
        try
        {
            BlobSasBuilder sasBuilderDetailed = new()
            {
                BlobContainerName = _options.StoragePagoPAFinancial!.BlobContainerName,
                BlobName = blobName,
                Resource = "b", // 'b' stands for blob
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(10)
            };

            var storageSharedKeyCredential = new StorageSharedKeyCredential(_options.StoragePagoPAFinancial.AccountName, _options.StoragePagoPAFinancial.AccountKey);
            sasBuilderDetailed.SetPermissions(BlobSasPermissions.Read);
            var sasTokenDetailed = sasBuilderDetailed.ToSasQueryParameters(storageSharedKeyCredential).ToString();
            var blobUrlDetailed = $"https://{_options.StoragePagoPAFinancial.AccountName}.blob.core.windows.net/{_options.StoragePagoPAFinancial!.BlobContainerName}/{blobName}";
            return $"{blobUrlDetailed}?{sasTokenDetailed}";
        }
        catch
        {
            var msg = $"Errore nel generare il SAS di {documentKey}!";
            _logger.LogError(msg);
        }

        return string.Empty;
    }
}