using PortaleFatture.BE.Core.Extensions;
namespace PortaleFatture.BE.Function.API.Models;

public sealed class Session
{
    public string? FkIdEnte { get; set; }  
    public DateTime Timestamp { get; set; }  
    public string? FunctionName { get; set; }
    public string? Stage { get; set; }
    public string? Method { get; set; }
    public string? Payload { get; set; }
    public string? Uri { get; set; }
    public string? IpAddress { get; set; }
    public string? Id { get; set; }
} 