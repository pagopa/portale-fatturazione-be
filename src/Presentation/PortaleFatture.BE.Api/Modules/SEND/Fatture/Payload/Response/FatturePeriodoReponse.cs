using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Response;

public sealed class FatturePeriodoReponse
{
    [JsonPropertyOrder(-2)]
    public int Mese { get; set; }

    [JsonPropertyOrder(-1)]
    public int Anno { get; set; }
}