namespace PortaleFatture.BE.Infrastructure.Gateway.Storage
{
    public interface IManualiStorageSASService
    {
        string GetSASToken(string documentName = "");
    }
}