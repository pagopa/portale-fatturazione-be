namespace PortaleFatture.BE.Api.Modules.DatiModuloCommesse.Payload.Request;
public sealed class EnteRicercaModuloCommessaByDescrizioneRequest
{
    public int? Anno { get; set; } 
    public int? Mese { get; set; }
    public string? Prodotto { get; set; }
    public string? Descrizione { get; set; }
}