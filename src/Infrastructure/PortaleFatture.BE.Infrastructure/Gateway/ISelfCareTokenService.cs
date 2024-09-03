using System.Security.Claims; 
using PortaleFatture.BE.Core.Auth.SelfCare;

namespace PortaleFatture.BE.Infrastructure.Gateway
{
    public interface ISelfCareTokenService
    {
        Task<(ClaimsPrincipal?, bool)> Validate(string selfcareToken, bool requireExpirationTime = false, CancellationToken ct = default);
        Task<SelfCareDto?> ValidateContent(string selfcareToken, bool requireExpirationTime = false, CancellationToken ct = default);
    }
}