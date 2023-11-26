using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Core.Common
{
    public interface IPortaleFattureOptions
    {
        string? ConnectionString { get; set; }
        string? FattureSchema { get; set; }
        JwtConfiguration? JWT { get; set; }
        string? SelfCareCertEndpoint { get; set; }
        string? SelfCareSchema { get; set; }
        string? SelfCareUri { get; set; }
        string? Vault { get; set; }
    }
}