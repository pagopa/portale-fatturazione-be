using Azure.Storage.Blobs.Models;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Services
{
    public interface IBlobStorageRelDownload
    {
        string? GetSasToken(string idEnte, int? anno, int? mese, string instanceId, string filename);
        Task<BlobContentInfo> UploadStreamAsync(Stream stream, string idEnte, int? anno, int? mese, string instanceId, string filename, string contentType);
    }
}