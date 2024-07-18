using System.Text.Json.Serialization; 
namespace PortaleFatture.BE.Core.Auth.SelfCare;

public class SelfCareDto
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("family_name")]
    public string? FamilyName { get; set; }

    [JsonPropertyName("fiscal_number")]
    public string? FiscalNumber { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("spid_level")]
    public string? SpidLevel { get; set; }

    [JsonPropertyName("from_aa")]
    public bool FromAA { get; set; }

    [JsonPropertyName("uid")]
    public string? Uid { get; set; }

    [JsonPropertyName("level")]
    public string? Level { get; set; }

    [JsonPropertyName("iat")]
    public int Iat { get; set; }

    [JsonPropertyName("exp")]
    public int Exp { get; set; }

    [JsonPropertyName("aud")]
    public string? Aud { get; set; }

    [JsonPropertyName("iss")]
    public string? Iss { get; set; }

    [JsonPropertyName("iss")]
    public string? Jti { get; set; }

    [JsonPropertyName("organization")]
    public SelfCareOrganizationDto? Organization { get; set; }

    [JsonPropertyName("desired_exp")]
    public int DesiredExp { get; set; }
} 