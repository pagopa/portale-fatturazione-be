using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.Identity;

public interface IProfileService
{
    Task<List<AuthenticationInfo>> GetInfo(string? selfcareToken);
}