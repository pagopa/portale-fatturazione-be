using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Api.Modules.SEND.DatiRel.Payload.Request;

public class RelTipologiaFatturaRequest
{
    [JsonPropertyName("anno")]
    public int Anno { get; set; }

    [JsonPropertyName("mese")]
    public int Mese { get; set; }
}