using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using PortaleFatture.BE.Core.Auth.SelfCare;

namespace PortaleFatture.BE.Infrastructure.Gateway
{
    public interface ISelfCareHttpClient
    {
        Task<CertificateKey?> GetCertificateByKidAsync(string? kid, CancellationToken ct = default);
        Task<List<CertificateKey>?> GetCertificatesAsync(CancellationToken ct = default); 
        JwtSecurityToken? GetSelfCareTokenAsync(string? selfcareToken, CancellationToken ct = default);
    }
}