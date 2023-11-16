using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Infrastructure.Gateway;

public sealed class CertificateContainer
{
    [JsonPropertyName("keys")]
    public List<CertificateKey>? Keys { get; set; }
} 