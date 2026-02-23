using PortaleFatture.BE.Function.API.Models;

namespace PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;

public class RELTestataRicercaInternalRequest
{
    public int? Anno { get; set; }
    public int? Mese { get; set; }
    public string? TipologiaFattura { get; set; }
    public string? IdContratto { get; set; }
    public byte? Caricata { get; set; } 
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public Session? Session { get; set; }
} 