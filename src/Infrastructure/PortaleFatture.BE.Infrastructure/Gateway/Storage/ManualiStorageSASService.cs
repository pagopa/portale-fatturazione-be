using Azure.Storage;
using Azure.Storage.Sas;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Resources;


namespace PortaleFatture.BE.Infrastructure.Gateway.Storage;

public class ManualiStorageSASService(IPortaleFattureOptions options,
    IStringLocalizer<Localization> localizer,
    ILogger<ManualiStorageSASService> logger) : IManualiStorageSASService
{
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly ILogger<ManualiStorageSASService> _logger = logger;
    private readonly IPortaleFattureOptions _options = options;

    private readonly string _manualiFolder = "manuali";
    private readonly string _manualeFile = "ManualeUtentePortaleFatturazione.pdf";
    public string GetSASToken(string documentName = "")
    {
        var blobName = String.IsNullOrEmpty(documentName) ? _manualeFile : documentName;
        try
        {
            BlobSasBuilder sasBuilderDetailed = new()
            {
                BlobContainerName = _manualiFolder,
                BlobName = blobName,
                Resource = "b", // 'b' stands for blob
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };

            var storageSharedKeyCredential = new StorageSharedKeyCredential(_options.StoragePagoPAFinancial!.AccountName, _options.StoragePagoPAFinancial.AccountKey);
            sasBuilderDetailed.SetPermissions(BlobSasPermissions.Read);
            var sasTokenDetailed = sasBuilderDetailed.ToSasQueryParameters(storageSharedKeyCredential).ToString();
            var blobUrlDetailed = $"https://{_options.StoragePagoPAFinancial.AccountName}.blob.core.windows.net/{_manualiFolder}/{blobName}";
            return $"{blobUrlDetailed}?{sasTokenDetailed}";
        }
        catch
        {
            var msg = $"Errore nel generare il SAS token del manuale {documentName}!";
            _logger.LogError(msg);
        }

        return string.Empty;
    }
}