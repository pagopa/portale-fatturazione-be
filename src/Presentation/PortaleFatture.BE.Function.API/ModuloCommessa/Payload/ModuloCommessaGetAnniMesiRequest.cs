using PortaleFatture.BE.Function.API.Models;

namespace PortaleFatture.BE.Function.API.ModuloCommessa.Payload;

public class ModuloCommessaGetAnniMesiRequest 
{  
    public string? Prodotto { get; set; } 
    public Session? Session { get; set; } 
} 