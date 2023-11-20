using Microsoft.AspNetCore.Authentication;

namespace PortaleFatture.BE.Infrastructure.Common.Authentication;

internal class ApiKeyDefaults : AuthenticationSchemeOptions
{
    public const string AuthenticationScheme = "ApiKeyScheme";
    public const string HeaderName = "X-loyalty-channel-key";
} 