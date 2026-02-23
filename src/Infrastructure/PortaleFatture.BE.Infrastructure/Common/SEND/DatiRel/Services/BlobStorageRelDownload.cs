using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Azure.Storage;
using PortaleFatture.BE.Core.Common;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Services;

public class BlobStorageRelDownload : IBlobStorageRelDownload
{
    private readonly string _accountName;
    private readonly string _accountKey;
    private readonly string _containerName;
    private readonly string _customDNS;
    private readonly StorageSharedKeyCredential _credentials;
    public BlobStorageRelDownload(IPortaleFattureOptions options)
    {
        _accountName = options.StorageRelDownload!.AccountName!;
        _accountKey = options.StorageRelDownload!.AccountKey!;
        _containerName = options.StorageRelDownload!.BlobContainerName!;
        _customDNS = options.StorageRelDownload!.CustomDNS!;
        _credentials = new StorageSharedKeyCredential(_accountName, _accountKey);
    }

    public async Task<BlobContentInfo> UploadStreamAsync(Stream stream, string idEnte, int? anno, int? mese, string instanceId, string filename, string contentType)
    {
        var blobName = $"{idEnte}/{anno}/{mese}/{instanceId}/{filename}";
        var blobServiceClient = new BlobServiceClient(
            new Uri($"https://{_accountName}.blob.core.windows.net"), _credentials);

        var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
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

    public string? GetSasToken(string idEnte, int? anno, int? mese, string instanceId, string filename)
    {
        var blobName = $"{idEnte}/{anno}/{mese}/{instanceId}/{filename}";
        var storageSharedKeyCredential = new StorageSharedKeyCredential(_accountName, _accountKey);
        BlobSasBuilder sasBuilderDetailed = new()
        {
            BlobContainerName = _containerName,
            BlobName = blobName,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
        };
        sasBuilderDetailed.SetPermissions(BlobSasPermissions.Read);
        var sasTokenDetailed = sasBuilderDetailed.ToSasQueryParameters(storageSharedKeyCredential).ToString();
        return $"{_customDNS}/{_containerName}/{blobName}?{sasTokenDetailed}";
    }
} 