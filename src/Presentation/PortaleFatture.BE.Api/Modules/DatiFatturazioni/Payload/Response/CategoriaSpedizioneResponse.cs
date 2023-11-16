namespace PortaleFatture.BE.Api.Modules.DatiFatturazioni.Payload.Response;

public class CategoriaSpedizioneResponse
{ 
    public long? Id { get; set; }

    public string? Descrizione { get; set; }

    public List<TipoSpedizioneResponse>? TipoSpedizione { get; set; }
} 