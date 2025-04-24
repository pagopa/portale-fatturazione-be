namespace PortaleFatture.BE.Api.Modules.SEND.APIKey.Payload.Request;

public sealed class CreateORModifyApiKeyRequest
{
    public string? ApiKey { get; set; }
    public bool? Attiva { get; set; } = false;   
    public bool? Refresh { get; set; }
} 