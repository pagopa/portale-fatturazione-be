using Azure.Storage.Blobs.Models;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Services; 
public interface IBlobStorageNotifiche
{
    Task<BlobContentInfo> UploadStreamAsync(Stream stream, string idEnte, int? anno, int? mese, string instanceId, string filename);
    string? GetSasToken(string idEnte, int? anno, int? mese, string instanceId, string filename);
}