using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Infrastructure.Gateway;

public sealed class CertificateKey
{
    [JsonPropertyName("alg")]
    public string? Alg { get; set; }

    [JsonPropertyName("kty")]
    public string? Kty { get; set; }

    [JsonPropertyName("use")]
    public string? Use { get; set; }

    [JsonPropertyName("x5c")]
    public List<string>? X5c { get; set; }

    [JsonPropertyName("n")]
    public string? N { get; set; }

    [JsonPropertyName("e")]
    public string? E { get; set; }

    [JsonPropertyName("kid")]
    public string? Kid { get; set; }

    [JsonPropertyName("x5t")]
    public string? X5t { get; set; }
} 