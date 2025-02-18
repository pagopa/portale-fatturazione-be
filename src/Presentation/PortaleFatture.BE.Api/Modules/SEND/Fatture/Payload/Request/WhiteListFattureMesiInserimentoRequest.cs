namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;

public class WhiteListFattureMesiInserimentoRequest
{
    public int[]? Mesi { get; set; }
    public int Anno { get; set; }
    public string? TipologiaFattura { get; set; }
    public string? IdEnte { get; set; }
} 