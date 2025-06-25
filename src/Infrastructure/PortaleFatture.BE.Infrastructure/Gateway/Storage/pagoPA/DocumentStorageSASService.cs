using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;


namespace PortaleFatture.BE.Infrastructure.Gateway.Storage.pagoPA;

public class DocumentStorageSASService(
    IPortaleFattureOptions options,
    IStringLocalizer<Localization> localizer,
    ILogger<DocumentStorageSASService> logger) : IDocumentStorageSASService
{
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly ILogger<DocumentStorageSASService> _logger = logger;
    private readonly IPortaleFattureOptions _options = options;

    #region prodotto pagoPA financial reports
    public string GetSASToken(DocumentiFinancialReportSASStorageKey documentKey)
    {
        var blobName = DocumentiFinancialReportSASStorageKey.FileName(documentKey);
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
    #endregion

    #region contestazioni 
    public string BlobContainerContestazioniName { get { return _options.StorageContestazioni!.BlobContainerName!; } } 
    public string StorageContestazioniName { get { return _options.StorageContestazioni!.AccountName!; } }
    public async Task<(string, bool)> UploadContestazioni(DocumentiContestazioniSASSStorageKey documentKey, UploadContestazioni upload)
    {
        try
        {
            var storageSharedKeyCredential = new StorageSharedKeyCredential(_options.StorageContestazioni!.AccountName, _options.StorageContestazioni!.AccountKey);
            var blobServiceClient = new BlobServiceClient(new Uri($"https://{_options.StorageContestazioni!.AccountName}.blob.core.windows.net"), storageSharedKeyCredential);
            var containerClient = blobServiceClient.GetBlobContainerClient(_options.StorageContestazioni!.BlobContainerName!);
            await containerClient.CreateIfNotExistsAsync();

            var fileContestazione = $"{documentKey}";
            fileContestazione = fileContestazione.Replace(".csv", "*.csv");
            var chunkBlobName = $"{fileContestazione}_{upload.ChunkIndex}";
            var chunkBlobClient = containerClient.GetBlobClient(chunkBlobName);

            using var stream = upload.FileChunk!.OpenReadStream();
            await chunkBlobClient.UploadAsync(stream, overwrite: false);

            var blobcount = new List<int>();
            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: fileContestazione))
                blobcount.Add(Convert.ToInt32(blobItem.Name.Replace($"{fileContestazione}_", string.Empty)));

            var chunckCount = blobcount.Count;
            if (chunckCount == upload.TotalChunks)
            {
                var blobClientContestazione = containerClient.GetAppendBlobClient(fileContestazione);
                foreach (var index in blobcount)
                {
                    var chunkBlob = containerClient.GetBlobClient($"{fileContestazione}_{index}");
                    using var chunkStream = await chunkBlob.OpenReadAsync();
                    if (!await blobClientContestazione.ExistsAsync())
                        await blobClientContestazione.CreateAsync();

                    await blobClientContestazione.AppendBlockAsync(chunkStream);
                    await chunkBlob.DeleteAsync();
                }
                return (fileContestazione, true);
            }
            return (chunkBlobName, false);
        }
        catch (Exception ex)
        {
            throw new UploadException(ex.Message);
        }
    }

    #endregion
}