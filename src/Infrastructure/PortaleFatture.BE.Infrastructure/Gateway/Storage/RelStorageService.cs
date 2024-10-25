using System.IO.Compression;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;

namespace PortaleFatture.BE.Infrastructure.Gateway.Storage;

public class RelStorageService(IPortaleFattureOptions options,
    IStringLocalizer<Localization> localizer,
    ILogger<RelStorageService> logger) : IRelStorageService
{
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly ILogger<RelStorageService> _logger = logger;
    private readonly IPortaleFattureOptions _options = options;
    public async Task AddStringEncodeBase64(
        RelTestataKey idTestata,
        string encodeBase64Content,
        string extension = ".pdf")
    {
        var fileName = RelTestataKey.FileName(idTestata, extension);
        var blobClient = new BlobClient(_options.Storage!.ConnectionString,
            RelTestataKey.Folder(idTestata, _options.Storage.RelFolder!),
            fileName);

        try
        {
            using var memoryStream = new MemoryStream();
            var streamWriter = new StreamWriter(memoryStream);
            streamWriter.Write(encodeBase64Content);
            streamWriter.Flush();
            memoryStream.Position = 0;
            await blobClient.UploadAsync(memoryStream, true);
        }
        catch
        {
            var msg = $"Errore nel caricare il file {fileName}!";
            _logger.LogError(msg);
            throw new DomainException(msg);
        }
    }

    public async Task<string> ReadStringEncodeBase64(RelTestataKey idTestata,
        string extension = ".pdf")
    {
        var fileName = RelTestataKey.FileName(idTestata, extension);
        var blobClient = new BlobClient(_options.Storage!.ConnectionString,
            RelTestataKey.Folder(idTestata, _options.Storage.RelFolder!),
            fileName);
        try
        {
            BlobDownloadResult downloadResult = await blobClient.DownloadContentAsync();
            return downloadResult.Content.ToString();
        }
        catch
        {
            var msg = $"Errore nel legger il file {fileName}!";
            _logger.LogError(msg);
            throw new DomainException(msg);
        }
    }

    public async Task AddDocument(RelTestataKey idTestata,
        MemoryStream memoryStream,
        string extension = ".pdf")
    {
        var fileName = RelTestataKey.FileName(idTestata, extension);
        var headers = new BlobHttpHeaders
        {
            ContentType = "application/pdf",
            ContentLanguage = "it-IT"
        };
        var blobClient = new BlobClient(_options.Storage!.ConnectionString,
            RelTestataKey.Folder(idTestata, _options.Storage.RelFolder!),
            fileName);
        try
        {
            memoryStream.Position = 0;
            await blobClient.UploadAsync(memoryStream, true);
            blobClient.SetHttpHeaders(headers);
        }
        catch
        {
            var msg = $"Errore nel caricare il file {fileName}!";
            _logger.LogError(msg);
            throw new DomainException(msg);
            throw;
        }
    }

    public async Task<byte[]> ReadBytes(RelTestataKey idTestata,
        string extension = ".pdf")
    {
        byte[] bytes;
        var fileName = RelTestataKey.FileName(idTestata, extension);
        var blobClient = new BlobClient(_options.Storage!.ConnectionString,
            RelTestataKey.Folder(idTestata, _options.Storage.RelFolder!),
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

    public async Task<(byte[], Dictionary<string, string>)> ReadZip(
        RelTestataDto testate,
        string extension = ".pdf")
    {
        Dictionary<string, string> fileValues = [];
        byte[] zipBytes;
        using var memoryStreamZip = new MemoryStream();
        {
            using var zipArchive = new ZipArchive(memoryStreamZip, ZipArchiveMode.Create, false);
            {
                memoryStreamZip.Position = 0;
                foreach (var testata in testate.RelTestate!)
                {
                    var key = RelTestataKey.Deserialize(testata.IdTestata!);
                    try
                    {
                        var bytes = await ReadBytes(key);
                        zipArchive.AddFile(testata.NomeTestata + extension, bytes);
                        fileValues.Add(key.ToString(), bytes.GetHashSHA512());
                    }
                    catch
                    {
                        var msg = $"Errore nel legger il file {key}!";
                        _logger.LogError(msg);
                    }
                }
                zipArchive.Dispose();
            }
            zipBytes = memoryStreamZip.ToArray();
            memoryStreamZip.Flush();
        }

        return (zipBytes, fileValues);
    }
}