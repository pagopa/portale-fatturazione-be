using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Core.Common;

public sealed class PortaleFattureOptions : IPortaleFattureOptions
{
    public string? ConnectionString { get; set; }
    public string? SelfCareCertEndpoint { get; set; }
    public string? SelfCareUri { get; set; }
    public string? FattureSchema { get; set; }
    public string? SelfCareSchema { get; set; }
    public string? Vault { get; set; }
    public JwtConfiguration? JWT { get; set; }
}