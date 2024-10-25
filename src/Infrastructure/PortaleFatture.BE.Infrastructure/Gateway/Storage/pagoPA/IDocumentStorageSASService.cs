using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti;

namespace PortaleFatture.BE.Infrastructure.Gateway.Storage.pagoPA;

public interface IDocumentStorageSASService
{
    string GetSASToken(DocumentiSASStorageKey documentKey);
}