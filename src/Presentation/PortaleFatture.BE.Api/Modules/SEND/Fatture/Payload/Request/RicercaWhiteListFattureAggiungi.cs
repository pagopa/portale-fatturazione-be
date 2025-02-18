namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;

public class RicercaWhiteListFattureAggiungi
{ 
    public int Anno { get; set; }
    public int[]? Mesi { get; set; }
    public string? TipologiaFattura { get; set; }
    public string? IdEnte { get; set; }
} 