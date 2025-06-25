using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Api.Modules.SEND.Notifiche.Payload.Response;

public class ContestazioniMeseResponse
{
    [JsonPropertyOrder(-1)]
    public string? Mese { get; set; }

    [JsonPropertyOrder(-2)]
    public string? Descrizione { get; set; }
} 