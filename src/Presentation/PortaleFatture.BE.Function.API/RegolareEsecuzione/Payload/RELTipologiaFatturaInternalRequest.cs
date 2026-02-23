using PortaleFatture.BE.Function.API.Models;

namespace PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;

public sealed class RELTipologiaFatturaInternalRequest
{
    public int Anno { get; set; } 
    public int Mese { get; set; } 
    public Session? Session { get; set; }
}