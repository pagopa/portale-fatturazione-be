using System.Data;
using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Core.Auth.SelfCare; 
public class SelfCareOrganizationDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("fiscal_code")]
    public string? FiscalCode { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("ipaCode")]
    public string? IpaCode { get; set; }

    [JsonPropertyName("roles")]
    public List<SelfCareRoleDto>? Roles { get; set; }

    [JsonPropertyName("groups")]
    public List<string>? Groups { get; set; }
} 