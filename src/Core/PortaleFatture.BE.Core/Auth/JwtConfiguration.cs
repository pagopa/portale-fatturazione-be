namespace PortaleFatture.BE.Core.Auth;

public record JwtConfiguration
{
    public string? ValidIssuer { get; set; }
    public string? ValidAudience { get; set; }
    public string? Secret { get; set; }
}