namespace PortaleFatture.BE.Api.Modules.SEND.DatiFatturazioni.Payload.Response;

public class CategoriaSpedizioneResponse
{
    public long? Id { get; set; }

    public string? Descrizione { get; set; }
    public string? Tipo { get; set; }
    public List<TipoSpedizioneResponse>? TipoSpedizione { get; set; }
}