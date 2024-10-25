namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;

public class FatturaPipelineSapRequest
{
    public int? AnnoRiferimento { get; set; }
    public int? MeseRiferimento { get; set; }
    public string? TipologiaFattura { get; set; }
}