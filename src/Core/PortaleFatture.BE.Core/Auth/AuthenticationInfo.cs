using Microsoft.AspNetCore.Identity;

namespace PortaleFatture.BE.Core.Auth;

public class AuthenticationInfo : IdentityUser, IAuthenticationInfo
{
    public string? Ruolo { get; set; }
    public DateTimeOffset DataCreazione { get; set; }
    public DateTimeOffset DataModifica { get; set; }
} 