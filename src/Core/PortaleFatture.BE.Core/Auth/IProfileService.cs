using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.Identity;

public interface IProfileService
{
    Task<List<AuthenticationInfo>?> GetSelfCareInfo(string? selfcareToken);
    Task<AuthenticationInfo?> GetPagoPAInfo(string? pagoPAIdToken, string?  azureADAccessToken);
} 