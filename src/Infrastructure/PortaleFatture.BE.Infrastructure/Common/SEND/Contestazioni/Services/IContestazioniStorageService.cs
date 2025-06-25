namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Services
{
    public interface IContestazioniStorageService
    {
        string? GetBlobName(string link, string nomedocumento);
        string? GetSASToken(string link, string nomedocumento);
    }
}