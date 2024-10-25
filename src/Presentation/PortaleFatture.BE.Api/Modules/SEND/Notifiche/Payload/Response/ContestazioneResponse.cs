using System.Text.Json.Serialization;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
namespace PortaleFatture.BE.Api.Modules.SEND.Notifiche.Payload.Response;

public class ContestazioneResponse
{
    [JsonPropertyOrder(-1)]
    public Contestazione? Contestazione { get; set; }

    [JsonPropertyOrder(-2)]
    public bool Chiusura { get; set; }

    [JsonPropertyOrder(-3)]
    public bool Modifica { get; set; }

    [JsonPropertyOrder(-3)]
    public bool Risposta { get; set; }
}