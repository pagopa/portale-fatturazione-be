namespace PortaleFatture.BE.Api.Modules.DatiFatturazioni.Payload.Response;
public record TipoContrattoResponse
{
    public long Id { get; set; }
    public string? Descrizione { get; set; }
}