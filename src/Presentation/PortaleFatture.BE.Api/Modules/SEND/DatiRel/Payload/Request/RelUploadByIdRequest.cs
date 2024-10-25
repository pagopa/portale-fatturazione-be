namespace PortaleFatture.BE.Api.Modules.SEND.DatiRel.Payload.Request;

public class RelUploadByIdRequest
{
    public int? Anno { get; set; }
    public int? Mese { get; set; }
    public string? TipologiaFattura { get; set; }
    public string? IdContratto { get; set; }
    public string? IdEnte { get; set; }
}