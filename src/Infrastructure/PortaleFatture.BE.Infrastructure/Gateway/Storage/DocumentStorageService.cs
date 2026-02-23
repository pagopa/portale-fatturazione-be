using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Resources;

namespace PortaleFatture.BE.Infrastructure.Gateway.Storage;

public class DocumentStorageService(IPortaleFattureOptions options,
    IStringLocalizer<Localization> localizer,
    ILogger<DocumentStorageService> logger) : IDocumentStorageService
{
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly ILogger<DocumentStorageService> _logger = logger;
    private readonly IPortaleFattureOptions _options = options; 

    public async Task<bool> AddDocumentPagoPA(
         MemoryStream memoryStream,
         DocumentiStorageKey documentKey,
         string? contentType,
         string? documentPath = null,
         string? contentLanguage = "it-IT")
    {
        var fileName = DocumentiStorageKey.FileName(documentKey);
        var headers = new BlobHttpHeaders
        {
            ContentType = contentType,
            ContentLanguage = contentLanguage
        };

        var blobClient = new BlobClient(_options.StorageDocumenti!.ConnectionString,
            DocumentiStorageKey.FolderPagoPA(documentKey, documentPath!),
            fileName);

        try
        {
            memoryStream.Position = 0;
            await blobClient.UploadAsync(memoryStream, true);
            blobClient.SetHttpHeaders(headers);
            memoryStream.Dispose();
            return true;
        }
        catch(Exception ex)
        {
            //var msg = $"Errore nel caricare il file {fileName}!";
            var msg = "RelUp:  " + ex.Message;
            _logger.LogError(msg); 
        }

        return false;
    }

    public async Task<byte[]> ReadBytes(string filePath)
    {
        byte[] bytes;
        var fileName = Path.GetFileName(filePath);
        var dir = Path.GetDirectoryName(filePath);
        var blobClient = new BlobClient(_options.StorageDocumenti!.ConnectionString,
            dir,
            fileName);
        try
        {
            using var memoryStream = new MemoryStream();
            var downloadResult = await blobClient.DownloadToAsync(memoryStream);
            bytes = memoryStream.ToArray();
            return bytes;
        }
        catch
        {
            var msg = $"Errore nel legger il file {fileName}!";
            _logger.LogError(msg);
            throw new DomainException(msg);
        }
    }

    public async Task<bool> AddDocumentMessagePagoPA(
       MemoryStream memoryStream,
       DocumentiStorageKey documentKey,
       string? contentType,
       string? contentLanguage = "it-IT")
    {
        var fileName = DocumentiStorageKey.FileName(documentKey);
        var headers = new BlobHttpHeaders
        {
            ContentType = contentType,
            ContentLanguage = contentLanguage
        };

        var blobClient = new BlobClient(_options.Storage!.ConnectionString,
            DocumentiStorageKey.FolderPagoPA(documentKey, _options.StorageDocumenti!.DocumentiFolder!),
            fileName);

        try
        {
            memoryStream.Position = 0;
            await blobClient.UploadAsync(memoryStream, true);
            blobClient.SetHttpHeaders(headers);
            memoryStream.Dispose();
            return true;
        }
        catch
        {
            var msg = $"Errore nel caricare il file {fileName}!";
            //var msg = "RelUp:  " + ex.Message;
            _logger.LogError(msg); 
        }

        return false;
    }

    public async Task<byte[]> ReadMessageBytes(DocumentiStorageKey key)
    {
        byte[] bytes;
        var fileName = DocumentiStorageKey.FileName(key);
        var blobClient = new BlobClient(_options.Storage!.ConnectionString,
            DocumentiStorageKey.FolderPagoPA(key, _options.StorageDocumenti!.DocumentiFolder!),
            fileName);
        try
        {
            using var memoryStream = new MemoryStream();
            var downloadResult = await blobClient.DownloadToAsync(memoryStream);
            bytes = memoryStream.ToArray();
            return bytes;
        }
        catch
        {
            var msg = $"Errore nel legger il file {fileName}!";
            _logger.LogError(msg);
            throw new DomainException(msg);
        }
    }
}