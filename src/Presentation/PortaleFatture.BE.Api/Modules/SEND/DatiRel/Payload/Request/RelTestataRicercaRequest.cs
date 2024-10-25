namespace PortaleFatture.BE.Api.Modules.SEND.DatiRel.Payload.Request;

public class RelTestataRicercaRequest
{
    public int? Anno { get; set; }
    public int? Mese { get; set; }
    public string? TipologiaFattura { get; set; }
    public string? IdContratto { get; set; }
    public byte? Caricata { get; set; }
}