using Microsoft.AspNetCore.Identity;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.Identity;

public class IdentityRolesService : IRolesService
{
    public IQueryable<IdentityRole> Roles => throw new NotImplementedException();

    public Task CreateDefaultRoleIfNotExistsAsync()
    {
        throw new NotImplementedException();
    }

    public Task<IdentityRole?> GetRoleFromCache(string roleName)
    {
        throw new NotImplementedException();
    }
}
