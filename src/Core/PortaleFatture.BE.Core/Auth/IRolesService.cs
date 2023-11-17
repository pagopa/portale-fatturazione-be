using Microsoft.AspNetCore.Identity;

namespace PortaleFatture.BE.Core.Auth;

public interface IRolesService
{
    IQueryable<IdentityRole> Roles { get; }
    Task<IdentityRole?> GetRoleFromCache(string roleName);
    Task CreateDefaultRoleIfNotExistsAsync();
}