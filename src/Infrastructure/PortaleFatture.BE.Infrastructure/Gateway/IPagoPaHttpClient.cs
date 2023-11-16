namespace PortaleFatture.BE.Infrastructure.Gateway
{
    public interface IPagoPaHttpClient
    {
        Task<List<CertificateKey>> GetCertificatesAsync(CancellationToken ct = default);
    }
}