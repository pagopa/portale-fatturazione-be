using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Api.Modules.DatiFatturazioni.Payload.Response;

public class TipoSpedizioneResponse
{ 
    public int Id { get; set; }
    public string? Descrizione { get; set; }  
    public string? Tipo { get; set; }
} 