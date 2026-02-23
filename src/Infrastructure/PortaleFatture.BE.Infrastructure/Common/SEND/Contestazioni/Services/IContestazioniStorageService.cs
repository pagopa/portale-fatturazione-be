using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Services;

public interface IContestazioniStorageService
{
    string? GetBlobName(string link, string nomedocumento);
    string? GetSASToken(string link, string nomedocumento); 
    string? GetSASToken(string blobName, BlobSasPermissions permission = BlobSasPermissions.Read);
    Task<BlobContentInfo> UploadStreamAsync(Stream stream, string idEnte, string instanceId, string filename, string contentType, string prefix = "temp");
}