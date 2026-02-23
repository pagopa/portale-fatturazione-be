using System.Text.Json.Serialization;
using PortaleFatture.BE.Function.API.Models;

namespace PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;

public class RELRigheByIdTestataRequest
{
    [JsonPropertyName("idTestata")]
    public string? IdTestata { get; set; } 
}