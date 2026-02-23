using System.Text.Json.Serialization;
using PortaleFatture.BE.Function.API.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace PortaleFatture.BE.Function.API.ModuloCommessa.Payload;

public class ModuloCommessaGetByAnnoRequest
{
    [JsonPropertyName("anno")]
    public int? Anno { get; set; } 
}

public class ModuloCommessaGetByAnnoInternalRequest
{
    [JsonPropertyName("anno")]
    public int? Anno { get; set; } 
    public Session? Session { get; set; }
}