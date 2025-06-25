using Azure.Storage;
using Azure.Storage.Sas;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Resources;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Services;


public class ContestazioniStorageService(IPortaleFattureOptions options,
    IStringLocalizer<Localization> localizer,
    ILogger<ContestazioniStorageService> logger) : IContestazioniStorageService
{
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly ILogger<ContestazioniStorageService> _logger = logger;
    private readonly IPortaleFattureOptions _options = options;
    private string _accountName { get; set; } = options.StorageContestazioni!.AccountName!;
    private string _accountKey { get; set; } = options.StorageContestazioni!.AccountKey!;
    private string _blobContainerName { get; set; } = options.StorageContestazioni!.BlobContainerName!;
    private string _customDNS { get; set; } = options.StorageContestazioni!.CustomDns!;

    public string? GetBlobName(string link, string nomedocumento)
    {
        return $"{link}/{nomedocumento}";
    }

    public string? GetSASToken(string link, string nomedocumento)
    {
        var blobName = GetBlobName(link, nomedocumento);
        blobName = blobName!.Replace($"{_blobContainerName}/", string.Empty);
        var sasToken = GenerateBlobSasToken(_accountName, _accountKey, _blobContainerName, blobName);
        return $"{_customDNS}/{_blobContainerName}/{blobName}?{sasToken}";
    }

    static string GenerateBlobSasToken(string accountName, string accountKey, string containerName, string? blobName)
    {
        var credential = new StorageSharedKeyCredential(accountName, accountKey);

        var sasBuilder = new BlobSasBuilder()
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b", // "b" for blob-level SAS
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
            StartsOn = DateTimeOffset.UtcNow.AddHours(-1)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        return sasBuilder.ToSasQueryParameters(credential).ToString();
    }
}