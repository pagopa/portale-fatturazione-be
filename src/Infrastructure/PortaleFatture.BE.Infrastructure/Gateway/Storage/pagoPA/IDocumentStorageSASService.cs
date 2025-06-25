using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;

namespace PortaleFatture.BE.Infrastructure.Gateway.Storage.pagoPA
{
    public interface IDocumentStorageSASService
    {
        //pagoPAFinancial
        string GetSASToken(DocumentiFinancialReportSASStorageKey documentKey);

        //contestazioni
        Task<(string, bool)> UploadContestazioni(DocumentiContestazioniSASSStorageKey documentKey, UploadContestazioni upload);
        string BlobContainerContestazioniName { get; }
        string StorageContestazioniName { get; }
    }
}