using System.Security.Claims;
using PortaleFatture.BE.Core.Auth.PagoPA;

namespace PortaleFatture.BE.Infrastructure.Gateway
{
    public interface IPagoPATokenService
    {
        Task<(ClaimsPrincipal?, bool)> Validate(string selfcareToken, bool requireExpirationTime = false, CancellationToken ct = default);
        Task<PagoPADto?> ValidateContent(string selfcareToken, string azureADAccessToken, bool requireExpirationTime = false, CancellationToken ct = default);
    }
}