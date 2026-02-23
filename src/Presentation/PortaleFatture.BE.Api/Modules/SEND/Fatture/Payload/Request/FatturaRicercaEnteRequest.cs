using System.Text.Json.Serialization;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;

public class FatturaRicercaEnteRequest
{
    [JsonPropertyName("anno")]
    public int? Anno { get; set; }
    [JsonPropertyName("mese")]
    public int? Mese { get; set; }

    private string[]? _tipologiaFattura;

    [JsonPropertyName("tipologiaFattura")]
    public string[]? TipologiaFattura
    {
        get { return _tipologiaFattura; }
        set { _tipologiaFattura = value!.IsNullNotAny() ? null : value; }
    }
}