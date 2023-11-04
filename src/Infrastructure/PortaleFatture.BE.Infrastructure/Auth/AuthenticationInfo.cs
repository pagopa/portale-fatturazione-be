using Microsoft.AspNetCore.Identity;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Auth;

public class AuthenticationInfo : IdentityUser, IAuthenticationInfo
{
    public string? Ruolo { get; set; }
    public DateTimeOffset DataCreazione { get; set; }
    public DateTimeOffset DataModifica { get; set; }
}