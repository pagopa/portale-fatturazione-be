namespace PortaleFatture.BE.Api.Modules.DatiModuloCommesse.Payload;
 
public sealed class EnteRicercaByDescrizioneRequest
{
    public string? Descrizione { get; set; }
    public string? Prodotto { get; set; } 
    public string? Profilo { get; set; }
}