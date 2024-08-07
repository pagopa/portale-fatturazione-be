﻿using System.Security.Claims;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.Identity
{
    public interface IIdentityUsersService
    {
        List<IList<Claim>> GetListUserClaimsFromUserAsync(List<AuthenticationInfo>? authInfos);
        IList<Claim> GetUserClaimsFromSelfCareUserAsync(AuthenticationInfo? authInfo);
        IList<Claim> GetUserClaimsFromPagoPAUserAsync(AuthenticationInfo? authInfo);
    }
}