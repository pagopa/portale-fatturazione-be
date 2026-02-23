using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
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

    public string? GetSASToken(string blobName, BlobSasPermissions permission = BlobSasPermissions.Read)
    { 
        blobName = blobName!.Replace($"{_blobContainerName}/", string.Empty);
        var sasToken = GenerateBlobSasToken(_accountName, _accountKey, _blobContainerName, blobName, permission);
        return $"{_customDNS}/{_blobContainerName}/{blobName}?{sasToken}";
    } 

    static string GenerateBlobSasToken(string accountName, string accountKey, string containerName, string? blobName, BlobSasPermissions permission = BlobSasPermissions.Read)
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

        sasBuilder.SetPermissions(permission);

        return sasBuilder.ToSasQueryParameters(credential).ToString();
    }

    public async Task<BlobContentInfo> UploadStreamAsync(Stream stream,  string idEnte, string instanceId, string filename, string contentType, string prefix = "temp")
    {
        var blobName = $"{prefix}/{idEnte}/{instanceId}/{filename}";
 
        var credential = new StorageSharedKeyCredential(_accountName, _accountKey);

        var blobServiceClient = new BlobServiceClient(
            new Uri($"https://{_accountName}.blob.core.windows.net"), credential);

        var containerClient = blobServiceClient.GetBlobContainerClient(_blobContainerName);
        await containerClient.CreateIfNotExistsAsync();

        var blobClient = containerClient.GetBlobClient(blobName);
        var headers = new BlobHttpHeaders
        {
            ContentType = contentType
        };

        return await blobClient.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = headers
        });
    }
}