using PortaleFatture.BE.Core.Entities.DatiRel;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Dto;

namespace PortaleFatture.BE.Infrastructure.Gateway.Storage;

public interface IRelStorageService
{
    Task AddDocument(RelTestataKey idTestata, MemoryStream memoryStream, string extension = ".pdf");
    Task AddStringEncodeBase64(RelTestataKey idTestata, string encodeBase64Content, string extension = ".pdf");
    Task<byte[]> ReadBytes(RelTestataKey idTestata, string extension = ".pdf");
    Task<string> ReadStringEncodeBase64(RelTestataKey idTestata, string extension = ".pdf");
    Task<(byte[], Dictionary<string, string>)> ReadZip(RelTestataDto testate, string extension = ".pdf");
}