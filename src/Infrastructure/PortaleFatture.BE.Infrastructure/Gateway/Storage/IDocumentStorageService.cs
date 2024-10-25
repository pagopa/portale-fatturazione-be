using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti;

namespace PortaleFatture.BE.Infrastructure.Gateway.Storage
{
    public interface IDocumentStorageService
    {
        Task<bool> AddDocumentMessagePagoPA(MemoryStream memoryStream, DocumentiStorageKey documentKey, string? contentType, string? contentLanguage = "it-IT");
        Task<bool> AddDocumentPagoPA(MemoryStream memoryStream, DocumentiStorageKey documentKey, string? contentType, string? documentPath = null, string? contentLanguage = "it-IT");
        Task<byte[]> ReadBytes(string filePath);
        Task<byte[]> ReadMessageBytes(DocumentiStorageKey key);
    }
}