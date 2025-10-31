
namespace PortaleFatture.BE.Infrastructure.Gateway.ModuloCommessa
{
    public interface IHttpModuloCommessaEventClient
    {
        void Dispose();
        Task<HttpResponseMessage> SendFileReadyEventAsync(string downloadUrl, string fileVersion);
    }
}