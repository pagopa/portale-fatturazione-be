using System.Text.Json.Serialization;
using PortaleFatture.BE.Function.API.Models;

namespace PortaleFatture.BE.Function.API.ModuloCommessa.Payload;

public class ModuloCommessaGetByAnnoMeseDettaglioRequest
{
    [JsonPropertyName("anno")]
    public int Anno { get; set; }

    [JsonPropertyName("mese")]
    public int Mese { get; set; }
}

public class ModuloCommessaGetByAnnoMeseDettaglioInternalRequest
{
    [JsonPropertyName("anno")]
    public int Anno { get; set; }

    [JsonPropertyName("mese")]
    public int Mese { get; set; }
    public Session? Session { get; set; }
}