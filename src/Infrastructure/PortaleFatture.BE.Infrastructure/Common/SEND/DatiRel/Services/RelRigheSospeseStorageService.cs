using Azure.Storage.Sas;
using Azure.Storage;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Gateway.Storage;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Services;

public class RelRigheSospeseStorageService(IPortaleFattureOptions options,
    IStringLocalizer<Localization> localizer,
    ILogger<RelRigheSospeseStorageService> logger) : IRelRigheSospeseStorageService
{
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly ILogger<RelRigheSospeseStorageService> _logger = logger;
    private readonly IPortaleFattureOptions _options = options;
    private string _accountName { get; set; } = options.StorageREL!.StorageRELAccountName!;
    private string _accountKey { get; set; } = options.StorageREL!.StorageRELAccountKey!;
    private string _blobContainerName { get; set; } = options.StorageREL!.StorageRELBlobContainerName!;
    private string _customDNS { get; set; } = options.StorageREL!.StorageRELCustomDns!;

    public string? GetBlobName(string sidtestata, string nomeEnte, string? extension = ".csv")
    {
        var idTestata = RelTestataKey.Deserialize(sidtestata);
        return $"{idTestata.Anno}/{idTestata.Mese}/{idTestata.TipologiaFattura}/{idTestata.IdEnte}/{idTestata.IdContratto}/Rel_Report di dettaglio_Sospese_{nomeEnte}_{idTestata.Mese}_{idTestata.Anno}{extension}";
    }

    public string? GetSASToken(string sidtestata, string nomeEnte)
    {
        var blobName = GetBlobName(sidtestata, nomeEnte);
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