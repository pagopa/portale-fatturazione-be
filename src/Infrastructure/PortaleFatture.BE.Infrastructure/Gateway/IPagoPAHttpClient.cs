using System.IdentityModel.Tokens.Jwt;

namespace PortaleFatture.BE.Infrastructure.Gateway
{
    public interface IPagoPAHttpClient
    {
        Task<CertificateKey?> GetCertificateByKidAsync(string? kid, CancellationToken ct = default);
        Task<List<CertificateKey>?> GetCertificatesAsync(CancellationToken ct = default);
        JwtSecurityToken? GetSelfCareTokenAsync(string? selfcareToken, CancellationToken ct = default);
    }
}