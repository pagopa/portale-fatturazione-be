using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Function.API.Models;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Commands;

namespace PortaleFatture.BE.Function.API.ModuloCommessa.Payload;

public class ModuloCommessaGetAnniMesiRequest 
{  
    public string? Prodotto { get; set; } = "prod-pn"; 
    public Session? Session { get; set; } 
} 