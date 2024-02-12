namespace PortaleFatture.BE.Api.Modules.Tipologie.Payload.Payload.Request;

public sealed class EnteRicercaByDescrizioneProfiloRequest
{
    public string? Descrizione { get; set; }
    public string? Prodotto { get; set; }
    public string? Profilo { get; set; }
}